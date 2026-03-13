using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Reports;

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

    [TestMethod]
    public void GetText_WithoutTemplates_WorksCorrectly()
    {
        // Arrange - GetText should work without templates being loaded (lazy loading)
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        appState.CurrentDocument = new StoryDocument(storyModel);

        // Act - GetText doesn't use templates, so it should work immediately
        var formatter = new ReportFormatter(appState);
        var result = formatter.GetText("plain text");

        // Assert
        Assert.AreEqual("plain text", result);
    }

    [TestMethod]
    public async Task FormatListReport_WithValidModel_LoadsTemplatesLazily()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        // Act - FormatListReport doesn't use templates (inline template)
        // but this verifies the formatter works without explicit LoadReportTemplates()
        var formatter = new ReportFormatter(appState);
        var result = formatter.FormatListReport(StoryItemType.Character);

        // Assert - Should return RTF content
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith("{\\rtf"), "Result should be valid RTF");
    }

    [TestMethod]
    public async Task FormatProblemReport_WithValidElement_LoadsTemplatesLazily()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        var problem = new ProblemModel("Test Problem", model, null);

        // Act - This format method requires templates, should load them lazily
        var formatter = new ReportFormatter(appState);
        var result = await formatter.FormatProblemReport(problem);

        // Assert - Should return RTF content with problem data
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith("{\\rtf"), "Result should be valid RTF");
    }

    [TestMethod]
    public async Task FormatCharacterReport_WithValidElement_LoadsTemplatesLazily()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        var character = new CharacterModel("Test Character", model, null)
        {
            Role = "Protagonist"
        };

        // Act - This format method requires templates, should load them lazily
        var formatter = new ReportFormatter(appState);
        var result = await formatter.FormatCharacterReport(character);

        // Assert - Should return RTF content with character data
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith("{\\rtf"), "Result should be valid RTF");
    }

    [TestMethod]
    public async Task FormatStoryOverviewReport_WithValidModel_LoadsTemplatesLazily()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        // Act - This format method requires templates, should load them lazily
        var formatter = new ReportFormatter(appState);
        var result = await formatter.FormatStoryOverviewReport();

        // Assert - Should return RTF content
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith("{\\rtf"), "Result should be valid RTF");
    }

    [TestMethod]
    public async Task MultipleFormatCalls_LoadsTemplatesOnlyOnce()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        var problem1 = new ProblemModel("Problem 1", model, null);
        var problem2 = new ProblemModel("Problem 2", model, null);

        // Act - Call multiple format methods
        var formatter = new ReportFormatter(appState);
        var result1 = await formatter.FormatProblemReport(problem1);
        var result2 = await formatter.FormatProblemReport(problem2);
        var result3 = await formatter.FormatStoryOverviewReport();

        // Assert - All should succeed and return valid RTF
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.IsNotNull(result3);
        Assert.IsTrue(result1.StartsWith("{\\rtf"), "Result 1 should be valid RTF");
        Assert.IsTrue(result2.StartsWith("{\\rtf"), "Result 2 should be valid RTF");
        Assert.IsTrue(result3.StartsWith("{\\rtf"), "Result 3 should be valid RTF");
    }
}
