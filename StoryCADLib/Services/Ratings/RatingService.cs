#if HAS_UNO_WINUI
using Windows.Services.Store;
using WinRT.Interop;
#endif
using Windows.System;

namespace StoryCADLib.Services.Ratings;

/// <summary>
///     Service for spawning the ratings prompt.
/// </summary>
public class RatingService
{
    private readonly AppState _appState;
    private readonly ILogService _logService;
    private readonly PreferenceService _preferenceService;
    private readonly Windowing _windowing;

    // TODO: Replace with actual Mac App Store ID when app is published
    // Format: https://apps.apple.com/app/id{APP_ID}?action=write-review
    private const string MacAppStoreId = "YOUR_MAC_APP_STORE_ID";

    public RatingService(AppState appState, PreferenceService preferenceService, Windowing windowing,
        ILogService logService)
    {
        _appState = appState;
        _preferenceService = preferenceService;
        _windowing = windowing;
        _logService = logService;
    }

    /// <summary>
    ///     returns True if we should show the rating prompt
    ///     false if it is inappropriate to ask for a review
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

        _logService.Log(LogLevel.Info, "Checking if we should ask for a review");
        //Dev / Don't ask me check
        if (_appState.DeveloperBuild || _preferenceService.Model.HideRatingPrompt)
        {
            _logService.Log(LogLevel.Info,
                "User has already reviewed us or is a dev, not showing rate dia_logService.");
            return false;
        }

        //Don't ask for sixty days after a review.
        if ((DateTime.Now - _preferenceService.Model.LastReviewDate).TotalDays < 180)
        {
            _logService.Log(LogLevel.Info,
                $"User reviewed us {(DateTime.Now - _preferenceService.Model.LastReviewDate).TotalDays} " +
                "days ago, not showing rate dialog");
            return false;
        }

        //Check user has used StoryCAD for over an hour.
        if (_preferenceService.Model.CumulativeTimeUsed < 3600)
        {
            _logService.Log(LogLevel.Info, "User hasn't used StoryCAD for an hour, not showing rate dia_logService.");
            return false;
        }

        //Don't show rating prompt if the user has only just updated.
        if (_appState.LoadedWithVersionChange)
        {
            _logService.Log(LogLevel.Info, "Not showing rate dialog since this is the first using this version.");
            return false;
        }

        //If all of the above checks have passed, ask for review.
        _logService.Log(LogLevel.Info, "Showing user rate dia_logService.");
        return true;
    }

    public async void OpenRatingPrompt()
    {
        if (OperatingSystem.IsMacOS())
        {
            // macOS: Use deep link to open Mac App Store
            try
            {
                _logService.Log(LogLevel.Info, "Asking user to rate StoryCAD (Mac App Store)");

                // Build the Mac App Store URL with review action
                var appStoreUrl = $"https://apps.apple.com/app/id{MacAppStoreId}?action=write-review";
                _logService.Log(LogLevel.Info, $"Opening Mac App Store URL: {appStoreUrl}");

                // Launch the URL using Windows.System.Launcher (cross-platform API)
                var uri = new Uri(appStoreUrl);
                var success = await Launcher.LaunchUriAsync(uri);

                if (success)
                {
                    _logService.Log(LogLevel.Info, "Successfully opened Mac App Store");
                    // Mark as reviewed so we don't prompt again for 180 days
                    _preferenceService.Model.HideRatingPrompt = true;
                    _preferenceService.Model.LastReviewDate = DateTime.Now;
                }
                else
                {
                    _logService.Log(LogLevel.Warn, "Failed to open Mac App Store URL");
                }
            }
            catch (Exception ex)
            {
                _logService.LogException(LogLevel.Error, ex, "Error opening Mac App Store for rating.");
            }
        }
        else if (OperatingSystem.IsWindows())
        {
#if HAS_UNO_WINUI
            // Windows: Use Windows Store API for in-app rating prompt
            try
            {
                _logService.Log(LogLevel.Info, "Asking user to rate StoryCAD (Windows Store)");

                // We need HWND for Windows Store API
                var _storeContext = StoreContext.GetDefault();
                InitializeWithWindow.Initialize(_storeContext, _windowing.WindowHandle);
                _logService.Log(LogLevel.Info, "Opening Rate prompt");
                var result = await _storeContext.RequestRateAndReviewAppAsync();
                _logService.Log(LogLevel.Info, $"Prompt closed, status {result.Status}," +
                                               $" updated {result.WasUpdated}");

                // Don't prompt user to rate again if they close or post a review.
                // If something went wrong we will prompt them again another time.
                switch (result.Status)
                {
                    // Set so we don't ask the user again until the next update or 60 days, whichever occurs last.
                    case StoreRateAndReviewStatus.Succeeded:
                    case StoreRateAndReviewStatus.CanceledByUser:
                        _preferenceService.Model.HideRatingPrompt = true;
                        _preferenceService.Model.LastReviewDate = DateTime.Now;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logService.LogException(LogLevel.Error, ex, "Error in rating prompt.");
            }
#else
            _logService.Log(LogLevel.Warn, "Windows Store rating API not available in this build");
#endif
        }
        else
        {
            _logService.Log(LogLevel.Warn, "Rating prompt not supported on this platform");
        }
    }
}
