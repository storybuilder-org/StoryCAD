using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Services.Reports;

namespace StoryCADTests.Services.Reports;

[TestClass]
public class ReportFormatterTests
{
    [TestMethod]
    public void GetText_NullInput_ReturnsEmpty()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        appState.CurrentDocument = new StoryDocument(storyModel);

        // Act
        var formatter = new ReportFormatter(appState);
        var result = formatter.GetText(null);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }
}
