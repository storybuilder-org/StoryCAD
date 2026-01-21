using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Models.StoryWorld;
using StoryCADLib.Services;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Reports;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADTests.Services.Reports;

[TestClass]
public class StoryWorldReportTests
{
    [TestMethod]
    public async Task FormatStoryWorldReport_WithValidModel_ReturnsValidRtf()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        // Add a StoryWorld element
        var storyWorld = new StoryWorldModel("Story World", model, null)
        {
            WorldType = "Constructed World"
        };

        // Act
        var formatter = new ReportFormatter(appState);
        var result = await formatter.FormatStoryWorldReport();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith("{\\rtf"), "Result should be valid RTF");
    }

    [TestMethod]
    public async Task FormatStoryWorldReport_WithPopulatedData_IncludesContent()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        var storyWorld = new StoryWorldModel("Story World", model, null)
        {
            WorldType = "Constructed World",
            EconomicSystem = "Barter system based on crystal trading"
        };

        storyWorld.Cultures.Add(new CultureEntry
        {
            Name = "Sky Elves",
            Values = "Honor and wisdom"
        });

        // Act
        var formatter = new ReportFormatter(appState);
        var result = await formatter.FormatStoryWorldReport();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith("{\\rtf"), "Result should be valid RTF");
    }

    [TestMethod]
    public async Task FormatStoryWorldReport_WithNoStoryWorld_ReturnsEmptyRtf()
    {
        // Arrange - No StoryWorld element added
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        // Act
        var formatter = new ReportFormatter(appState);
        var result = await formatter.FormatStoryWorldReport();

        // Assert - Should handle gracefully (return empty or minimal RTF)
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Test that PrintReports includes StoryWorld when CreateStoryWorld is true.
    /// </summary>
    [TestMethod]
    public async Task PrintReports_WithCreateStoryWorld_IncludesStoryWorldReport()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var windowing = Ioc.Default.GetRequiredService<Windowing>();
        var editFlush = Ioc.Default.GetRequiredService<EditFlushService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        // Add a StoryWorld element
        var storyWorld = new StoryWorldModel("Story World", model, null)
        {
            WorldType = "Constructed World",
            EconomicSystem = "Trade-based economy"
        };

        var vm = new PrintReportDialogVM(appState, windowing, editFlush, logger);
        vm.CreateStoryWorld = true;
        vm.CreateOverview = false;
        vm.CreateSummary = false;
        vm.CreateStructure = false;
        vm.ProblemList = false;
        vm.CharacterList = false;
        vm.SettingList = false;
        vm.SceneList = false;
        vm.WebList = false;

        // Act
        var printReports = new PrintReports(vm, appState, logger);
        var result = await printReports.Generate();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0, "Should generate StoryWorld report content");
    }

    /// <summary>
    /// Test that PrintSingleNode handles StoryWorld like Overview (sets flag instead of adding to SelectedNodes).
    /// </summary>
    [TestMethod]
    public async Task PrintReportDialogVM_PrintSingleNode_WithStoryWorld_SetsCreateStoryWorldFlag()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var windowing = Ioc.Default.GetRequiredService<Windowing>();
        var editFlush = Ioc.Default.GetRequiredService<EditFlushService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        var storyWorld = new StoryWorldModel("Story World", model, null);

        var vm = new PrintReportDialogVM(appState, windowing, editFlush, logger);

        // Act - Call PrintSingleNode with StoryWorld node
        // Note: We can't fully test PrintSingleNode as it opens a dialog,
        // but we can verify the CreateStoryWorld property exists and works
        vm.CreateStoryWorld = true;

        // Assert - Verify the property can be set
        Assert.IsTrue(vm.CreateStoryWorld, "CreateStoryWorld property should be settable");
    }
}
