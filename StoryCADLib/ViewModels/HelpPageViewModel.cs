
namespace StoryCADLib.ViewModels;

/// <summary>
///     ViewModel for HelpPage. Computes manual URLs from AppState.ManualBaseUrl
///     and a constant table of relative paths.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class HelpPageViewModel
{
    private const string TutorialRelativePath = "docs/Tutorial%20Creating%20a%20Story/Tutorial_Creating_a_Story.html";
    private const string CollaboratorRelativePath = "docs/StoryCAD%20Collaborator/";

    private readonly Uri _baseUri;

    public HelpPageViewModel()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        _baseUri = new Uri(appState.ManualBaseUrl);
    }

    public Uri ManualUri => _baseUri;
    public Uri TutorialUri => new(_baseUri, TutorialRelativePath);
    // Issue #1592: the manual's Collaborator section; its landing page links to Getting Started.
    // Same path as Collaborator's own Help link (WorkflowShell.xaml.cs).
    public Uri CollaboratorUri => new(_baseUri, CollaboratorRelativePath);
}
