using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        appState.CurrentDocument = new StoryDocument(storyModel, null);

        // Act
        var formatter = new ReportFormatter(appState);
        string result = formatter.GetText(null);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }
}
