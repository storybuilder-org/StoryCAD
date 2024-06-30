using Windows.Services.Store;

namespace StoryCAD.Services.Ratings;

/// <summary>
/// Service for spawning the ratings prompt.
/// </summary>
public class RatingService
{
	AppState State = Ioc.Default.GetService<AppState>();
	PreferenceService prefs = Ioc.Default.GetService<PreferenceService>();
	Windowing Windowing = Ioc.Default.GetService<Windowing>();
	LogService log = Ioc.Default.GetService<LogService>();

	/// <summary>
	/// returns True if we should show the rating prompt
	/// false if it is inappropriate to ask for a review
	/// </summary>
	public bool AskForRatings()
	{
		/* Don't ask the following people to rate
		 * - Devs
		 * - Unstable build users
		 * - People who have explicitly asked not see the rating prompt
		 * - People who have used StoryCAD Less than an hour 
		 * - People who have reviewed StoryCAD within the last sixty days.
		 */

		log.Log(LogLevel.Info, "Checking if we should ask for a review");
		//Dev / Don't ask me check
		if (State.DeveloperBuild || prefs.Model.HideRatingPrompt) 
		{
			log.Log(LogLevel.Info, "User has already reviewed us or is a dev, not showing rate dialog.");
			return false; 
		}
		
		//Don't ask for sixty days after a review.
		if ((DateTime.Now - prefs.Model.LastReviewDate).TotalDays < 180) 
		{
			log.Log(LogLevel.Info, 
				$"User reviewed us {(DateTime.Now - prefs.Model.LastReviewDate).TotalDays} " +
				"days ago, not showing rate dialog");
			return false;
		}

		//Check user has used StoryCAD for over an hour.
		if (prefs.Model.CumulativeTimeUsed < 3600) 
		{
			log.Log(LogLevel.Info, "User hasn't used StoryCAD for an hour, not showing rate dialog.");
			return false;
		}

		//Don't show rating prompt if the user has only just updated.
		if (State.LoadedWithVersionChange)
		{
			log.Log(LogLevel.Info, "Not showing rate dialog since this is the first using this version.");
			return false;
		}

		//If all of the above checks have passed, ask for review.
		log.Log(LogLevel.Info, "Showing user rate dialog.");
		return true;
	}

	public async void OpenRatingPrompt()
	{
		try
		{
			log.Log(LogLevel.Info, "Asking user to rate StoryCAD");

			//We need HWIND
			StoreContext _storeContext = StoreContext.GetDefault();
			WinRT.Interop.InitializeWithWindow.Initialize(_storeContext, Windowing.WindowHandle);
			log.Log(LogLevel.Info, "Opening Rate prompt");
			StoreRateAndReviewResult result = await _storeContext.RequestRateAndReviewAppAsync();
			log.Log(LogLevel.Info, $"Prompt closed, status {result.Status}," +
				$" updated {result.WasUpdated}");

			//Don't prompt user to rate again if they close or post a review.
			//If something went wrong we will prompt them again another time.
			switch (result.Status)
			{
				//Set so we don't ask the user again until the next update or 60 days, whichever occurs last.
				case StoreRateAndReviewStatus.Succeeded:
				case StoreRateAndReviewStatus.CanceledByUser:
					prefs.Model.HideRatingPrompt = true;
					prefs.Model.LastReviewDate = DateTime.Now;
					break;
			}
		}
		catch (Exception ex)
		{
			log.LogException(LogLevel.Error, ex, "Error in rating prompt.");
		}

	}
}
