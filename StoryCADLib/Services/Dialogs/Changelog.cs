using Octokit;

namespace StoryCADLib.Services.Dialogs;

/// <summary>
///     This class handles showing Change logs for the app
/// </summary>
public class Changelog
{
    private readonly AppState _appDat;
    private readonly GitHubClient _client = new(new ProductHeaderValue("STB"));
    private readonly ILogService _logger;

    public Changelog(ILogService logger, AppState appState)
    {
        _logger = logger;
        _appDat = appState;
    }

    /// <summary>
    ///     This access the changelog for the latest version
    /// </summary>
    /// <returns>The changelog text if successful</returns>
    public async Task<string> GetChangelogText()
    {
        try
        {
            if (_appDat.Version.Contains("Built on:")) //Checks user isn't running a development version of StoryCAD
            {
                return "Changelogs are unavailable for development versions of StoryCAD.";
            }

            //Returns body of release
            var version = _appDat.Version.Replace("Version: ", "") + ".0";

            return (await _client.Repository.Release.Get("StoryBuilder-org", "StoryCAD", version)).Body;
        }
        catch (Exception _e)
        {
            if (_e.Source!.Contains("Net"))
            {
                _logger.Log(LogLevel.Info,
                    "Error with network, user probably isn't connected to wifi or is using an auto build");
            }

            if (_e.Source!.Contains("Octokit.NotFoundException"))
            {
                _logger.Log(LogLevel.Info, "Error finding changelog for this version");
            }

            //Return error message
            return
                """
                Failed to get changelog for this version, this because either:
                 - You are using a development version of StoryCAD
                 - An issue was encountered connecting to GitHub to get the release information
                """;
        }
    }

    /// <summary>
    ///     Shows a changelog content dialog/
    /// </summary>
    public async Task ShowChangeLog()
    {
        //Don't Show changelog on dev build's since its pointless.
        if (_appDat.DeveloperBuild)
        {
            return;
        }

        try
        {
            ContentDialog _changelogUI = new()
            {
                Width = 800,
                Content = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new TextBlock
                    {
                        TextWrapping = TextWrapping.Wrap,
                        Text = await GetChangelogText()
                    }
                },
                Title = "What's new in StoryCAD " + _appDat.Version,
                PrimaryButtonText = "Okay"
            };
            await Ioc.Default.GetService<Windowing>().ShowContentDialog(_changelogUI);
        }
        catch (Exception _e)
        {
            _logger.LogException(LogLevel.Error, _e, "Error in ShowChangeLog()");
        }
    }
}
