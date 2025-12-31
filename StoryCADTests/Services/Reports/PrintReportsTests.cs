using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Reports;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADTests.Services.Reports;

/// <summary>
/// Tests for PrintReports service (issue #1226 regression test).
/// </summary>
[TestClass]
public class PrintReportsTests
{
    /// <summary>
    /// Regression test for issue #1226: Print Report Crash.
    ///
    /// Before the fix, if a node's element was missing (GetStoryElementByGuid threw InvalidOperationException),
    /// the code would crash with NullReferenceException when attempting to use the null element.
    ///
    /// After the fix, the code catches InvalidOperationException, leaves element as null,
    /// and continues processing without crashing (skipping that node).
    /// </summary>
    [TestMethod]
    public async Task Generate_WithNodeHavingMissingElement_ContinuesWithoutCrashing()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var windowing = Ioc.Default.GetRequiredService<Windowing>();
        var editFlush = Ioc.Default.GetRequiredService<EditFlushService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Create a test story model
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        // Create PrintReportDialogVM with one valid and one invalid node
        var vm = new PrintReportDialogVM(appState, windowing, editFlush, logger);

        // Add a valid character to the model
        var character = new CharacterModel("Valid Character", model, null);
        var validNode = character.Node;

        // Create a character and then REMOVE it from the model to simulate a missing element
        // This simulates the bug scenario where a node exists but its element was deleted
        var orphanCharacter = new CharacterModel("Missing Character", model, null);
        var invalidNode = orphanCharacter.Node;
        var invalidGuid = orphanCharacter.Uuid;
        model.StoryElements.Remove(orphanCharacter); // Remove element to simulate missing element

        // Set up selected nodes with both valid and invalid nodes
        vm.SelectedNodes = new List<StoryNodeItem> { validNode, invalidNode };
        vm.CreateOverview = false; // Disable other report types to focus on selected nodes
        vm.CreateSummary = false;
        vm.CreateStructure = false;
        vm.ProblemList = false;
        vm.CharacterList = false;
        vm.SettingList = false;
        vm.SceneList = false;
        vm.WebList = false;

        // Act - This should NOT crash even with the invalid node
        var printReports = new PrintReports(vm, appState, logger);
        var result = await printReports.Generate();

        // Assert
        // The method should complete without throwing an exception
        Assert.IsNotNull(result, "Generate should return a non-null result");
        // Result should contain content from the valid character
        // (The invalid node should be silently skipped)
        Assert.IsTrue(result.Length > 0, "Should generate content for valid nodes");
    }

    /// <summary>
    /// Test that Generate handles all nodes having missing elements gracefully.
    /// Should return empty string and log warning when no valid nodes are processed.
    /// </summary>
    [TestMethod]
    public async Task Generate_WithAllNodesMissingElements_ReturnsEmptyString()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var windowing = Ioc.Default.GetRequiredService<Windowing>();
        var editFlush = Ioc.Default.GetRequiredService<EditFlushService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        var vm = new PrintReportDialogVM(appState, windowing, editFlush, logger);

        // Create characters and then remove them from the model to simulate missing elements
        var orphan1 = new CharacterModel("Missing Character 1", model, null);
        var invalidNode1 = orphan1.Node;
        model.StoryElements.Remove(orphan1); // Remove to simulate missing element

        var orphan2 = new CharacterModel("Missing Character 2", model, null);
        var invalidNode2 = orphan2.Node;
        model.StoryElements.Remove(orphan2); // Remove to simulate missing element

        vm.SelectedNodes = new List<StoryNodeItem> { invalidNode1, invalidNode2 };
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
        Assert.AreEqual("", result, "Should return empty string when all nodes have missing elements");
    }

    /// <summary>
    /// Test that Generate works correctly with all valid nodes (baseline test).
    /// </summary>
    [TestMethod]
    public async Task Generate_WithAllValidNodes_ReturnsContent()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var windowing = Ioc.Default.GetRequiredService<Windowing>();
        var editFlush = Ioc.Default.GetRequiredService<EditFlushService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        var vm = new PrintReportDialogVM(appState, windowing, editFlush, logger);

        // Create valid characters in the model
        var character1 = new CharacterModel("Character 1", model, null) { Role = "Protagonist" };
        var character2 = new CharacterModel("Character 2", model, null) { Role = "Antagonist" };
        // Characters are automatically added to model.StoryElements in constructor

        var node1 = character1.Node;
        var node2 = character2.Node;

        vm.SelectedNodes = new List<StoryNodeItem> { node1, node2 };
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
        Assert.IsTrue(result.Length > 0, "Should generate content for valid nodes");
        Assert.IsTrue(result.Contains("\\PageBreak"), "Should contain page break markers");
    }
}
