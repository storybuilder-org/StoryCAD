using System.Reflection;

namespace StoryCADLib.Services.Dialogs;

/// <summary>
///     This class handles showing Change logs for the app
/// </summary>
public class Changelog
{
    private readonly AppState _appDat;
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
            await using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("StoryCADLib.Assets.Install.changelog.txt");

            if (stream is null)
            {
                _logger.Log(LogLevel.Warn, "changelog.txt embedded resource not found");
                return "Changelog is unavailable.";
            }

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception e)
        {
            _logger.LogException(LogLevel.Error, e, "Error reading embedded changelog");
            return "Failed to load changelog.";
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
