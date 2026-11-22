using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.ViewModels;

/// <summary>
///     Headless tests for <see cref="FileUnavailableViewModel" />, the data of the "File unavailable."
///     dialog of StoryIO.CheckFileAvailability (#1638). The dialog XAML itself is not exercised here.
/// </summary>
[TestClass]
public class FileUnavailableViewModelTests
{
    [TestMethod]
    public void HandleResult_WithPrimary_RequestsHelpArticle()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<FileUnavailableViewModel>();
        vm.Initialize(Path.Combine(App.ResultsDir, "Unavailable.stbx"));

        // Act
        vm.HandleResult(ContentDialogResult.Primary);

        // Assert
        Assert.IsTrue(vm.HelpArticleRequested);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var expected = new Uri(new Uri(appState.ManualBaseUrl),
            "docs/Miscellaneous/Troubleshooting_Cloud_Storage_Providers.html");
        Assert.AreEqual(expected, vm.HelpArticleUri);
    }

    [TestMethod]
    public void HandleResult_WithSecondary_RequestsNoHelpArticle()
    {
        // Arrange
        var vm = Ioc.Default.GetRequiredService<FileUnavailableViewModel>();
        vm.Initialize(Path.Combine(App.ResultsDir, "Unavailable.stbx"));

        // Act
        vm.HandleResult(ContentDialogResult.Secondary);

        // Assert
        Assert.IsFalse(vm.HelpArticleRequested);
    }

    [TestMethod]
    public void Initialize_AfterEarlierRequest_ClearsRequest()
    {
        // Arrange - the view model is a singleton, so a new file must not inherit the last answer
        var vm = Ioc.Default.GetRequiredService<FileUnavailableViewModel>();
        vm.Initialize("a.stbx");
        vm.HandleResult(ContentDialogResult.Primary);

        // Act
        vm.Initialize("b.stbx");

        // Assert
        Assert.AreEqual("b.stbx", vm.FilePath);
        Assert.IsFalse(vm.HelpArticleRequested);
    }

    [TestMethod]
    public void Texts_AreTheTextsOfTheDialogBeforeTheChange()
    {
        // Arrange - copied word for word from StoryIO.cs lines 420 to 428 before #1638
        const string title = "File unavailable.";
        const string message = """
                               The story outline you are trying to open is stored on a cloud service, and isn't available currently.
                               Try going online and syncing the file, then try again.

                               Click show help article for more information.
                               """;
        const string primaryButtonText = "Show help article";
        const string secondaryButtonText = "Close";

        // Act
        var vm = Ioc.Default.GetRequiredService<FileUnavailableViewModel>();

        // Assert
        Assert.AreEqual(title, vm.Title);
        Assert.AreEqual(message, vm.Message);
        Assert.AreEqual(primaryButtonText, vm.PrimaryButtonText);
        Assert.AreEqual(secondaryButtonText, vm.SecondaryButtonText);
    }
}
