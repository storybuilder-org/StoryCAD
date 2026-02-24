using StoryCADLib.Services;

namespace StoryCADLib.ViewModels;

/// <summary>
///     ViewModel for HelpPage. Computes manual URLs from the
///     UseBetaDocumentation preference and a constant table of relative paths.
/// </summary>
public class HelpPageViewModel
{
    private static readonly Uri ProductionBase = new("https://storybuilder-org.github.io/StoryCAD/");
    private static readonly Uri BetaBase = new("https://storybuilder-org.github.io/BetaManual/");

    private const string TutorialRelativePath = "docs/Tutorial%20Creating%20a%20Story/Tutorial_Creating_a_Story.html";

    private readonly Uri _baseUri;

    public HelpPageViewModel()
    {
        var prefs = Ioc.Default.GetService<PreferenceService>();
        var useBeta = prefs?.Model.UseBetaDocumentation ?? AppState.IsBetaDistribution;
        _baseUri = useBeta ? BetaBase : ProductionBase;
    }

    public Uri ManualUri => _baseUri;
    public Uri TutorialUri => new(_baseUri, TutorialRelativePath);
}
