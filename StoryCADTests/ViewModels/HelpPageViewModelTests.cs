using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Services;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.ViewModels;

/// <summary>
///     Issue #1592: the startup help dialog links to the manual's StoryCAD Collaborator section,
///     on the production or the beta manual as Use beta documentation selects.
/// </summary>
[TestClass]
public class HelpPageViewModelTests
{
    [TestMethod]
    public void CollaboratorUri_ProductionManual_PointsAtCollaboratorSection()
    {
        Assert.AreEqual("https://manual.storybuilder.org/docs/StoryCAD%20Collaborator/",
            CollaboratorUriWith(useBetaDocumentation: false));
    }

    [TestMethod]
    public void CollaboratorUri_BetaManual_PointsAtCollaboratorSection()
    {
        Assert.AreEqual("https://beta.manual.storybuilder.org/docs/StoryCAD%20Collaborator/",
            CollaboratorUriWith(useBetaDocumentation: true));
    }

    private static string CollaboratorUriWith(bool useBetaDocumentation)
    {
        var preferences = Ioc.Default.GetRequiredService<PreferenceService>();
        var original = preferences.Model.UseBetaDocumentation;
        try
        {
            preferences.Model.UseBetaDocumentation = useBetaDocumentation;
            return new HelpPageViewModel().CollaboratorUri.AbsoluteUri;
        }
        finally
        {
            preferences.Model.UseBetaDocumentation = original;
        }
    }
}
