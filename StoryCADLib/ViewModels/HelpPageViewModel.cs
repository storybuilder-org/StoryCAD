
namespace StoryCADLib.ViewModels;

/// <summary>
///     ViewModel for HelpPage. Computes manual URLs from AppState.ManualBaseUrl
///     and a constant table of relative paths.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class HelpPageViewModel
{
    private const string TutorialRelativePath = "docs/Tutorial%20Creating%20a%20Story/Tutorial_Creating_a_Story.html";

    private readonly Uri _baseUri;

    public HelpPageViewModel()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        _baseUri = new Uri(appState.ManualBaseUrl);
    }

    public Uri ManualUri => _baseUri;
    public Uri TutorialUri => new(_baseUri, TutorialRelativePath);
}
