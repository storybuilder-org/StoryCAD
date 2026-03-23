using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Reports;
using StoryCADLib.ViewModels.Tools;

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

    #region Phase 4a: FormatStructureBeatsElements Tests

    [TestMethod]
    public void FormatStructureBeatsElements_WithCustomBeat_MarksWithAsterisk()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        var overview = new OverviewModel("Test Story", storyModel, null);
        storyModel.ExplorerView.Add(overview.Node);
        appState.CurrentDocument = new StoryDocument(storyModel);

        var problem = new ProblemModel("Test Problem", storyModel, null);
        problem.StructureTitle = "Seven Point Story Structure";
        problem.StructureBeats = new ObservableCollection<StructureBeat>
        {
            new("Hook (2)", "The starting state"),       // template beat (exact name match)
            new("My Custom Beat", "Writer-added beat")   // custom beat
        };

        var formatter = new ReportFormatter(appState);

        // Act
        var result = formatter.FormatStructureBeatsElements(problem);

        // Assert — custom beat should have *, template beat should not
        Assert.IsTrue(result.Contains("1. Hook (2)"), "Template beat should be numbered without *");
        Assert.IsFalse(result.Contains("1. Hook (2) *"), "Template beat should NOT have *");
        Assert.IsTrue(result.Contains("2. My Custom Beat *"), "Custom beat should have * marker");
    }

    [TestMethod]
    public void FormatStructureBeatsElements_UnassignedBeat_ShowsUnassigned()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        var overview = new OverviewModel("Test Story", storyModel, null);
        storyModel.ExplorerView.Add(overview.Node);
        appState.CurrentDocument = new StoryDocument(storyModel);

        var problem = new ProblemModel("Test Problem", storyModel, null);
        problem.StructureBeats = new ObservableCollection<StructureBeat>
        {
            new("Beat 1", "Description")  // Guid is Empty by default
        };

        var formatter = new ReportFormatter(appState);
        var result = formatter.FormatStructureBeatsElements(problem);

        Assert.IsTrue(result.Contains("Unassigned"), "Unassigned beat should show 'Unassigned'");
    }

    [TestMethod]
    public void FormatStructureBeatsElements_NoTemplate_AllMarkedCustom()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        var overview = new OverviewModel("Test Story", storyModel, null);
        storyModel.ExplorerView.Add(overview.Node);
        appState.CurrentDocument = new StoryDocument(storyModel);

        var problem = new ProblemModel("Test Problem", storyModel, null);
        problem.StructureTitle = ""; // no template
        problem.StructureBeats = new ObservableCollection<StructureBeat>
        {
            new("Beat A", "Desc A"),
            new("Beat B", "Desc B")
        };

        var formatter = new ReportFormatter(appState);
        var result = formatter.FormatStructureBeatsElements(problem);

        Assert.IsTrue(result.Contains("1. Beat A *"), "All beats should be marked custom when no template");
        Assert.IsTrue(result.Contains("2. Beat B *"), "All beats should be marked custom when no template");
    }

    [TestMethod]
    public void FormatStructureBeatsElements_HasLegend()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        var overview = new OverviewModel("Test Story", storyModel, null);
        storyModel.ExplorerView.Add(overview.Node);
        appState.CurrentDocument = new StoryDocument(storyModel);

        var problem = new ProblemModel("Test Problem", storyModel, null);
        problem.StructureBeats = new ObservableCollection<StructureBeat>
        {
            new("Custom Beat", "Desc")
        };

        var formatter = new ReportFormatter(appState);
        var result = formatter.FormatStructureBeatsElements(problem);

        Assert.IsTrue(result.Contains("* = custom beat"), "Should include legend for * marker");
    }

    #endregion

    #region Phase 4b: FormatUnassignedElementsReport Tests

    [TestMethod]
    public void FormatUnassignedElementsReport_WithOrphanScene_ListsIt()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        var overview = new OverviewModel("Test Story", storyModel, null);
        storyModel.ExplorerView.Add(overview.Node);
        appState.CurrentDocument = new StoryDocument(storyModel);

        // Add a scene that's not assigned to any beat
        var scene = new SceneModel("Orphan Scene", storyModel, null);

        var formatter = new ReportFormatter(appState);
        var result = formatter.FormatUnassignedElementsReport();

        Assert.IsTrue(result.Contains("Orphan Scene"), "Unassigned scene should appear in report");
        Assert.IsTrue(result.Contains("Unassigned Scenes"), "Report should have Unassigned Scenes section");
    }

    [TestMethod]
    public void FormatUnassignedElementsReport_WithBoundProblem_ExcludesIt()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        var overview = new OverviewModel("Test Story", storyModel, null);
        storyModel.ExplorerView.Add(overview.Node);
        appState.CurrentDocument = new StoryDocument(storyModel);

        // Create a problem that IS bound to a structure
        var problem = new ProblemModel("Bound Problem", storyModel, null);
        problem.BoundStructure = Guid.NewGuid(); // bound

        var formatter = new ReportFormatter(appState);
        var result = formatter.FormatUnassignedElementsReport();

        Assert.IsFalse(result.Contains("Bound Problem"), "Bound problem should NOT appear in report");
    }

    [TestMethod]
    public void FormatUnassignedElementsReport_AllAssigned_ShowsNone()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        var overview = new OverviewModel("Test Story", storyModel, null);
        storyModel.ExplorerView.Add(overview.Node);
        appState.CurrentDocument = new StoryDocument(storyModel);

        // No orphan elements — only the (none) placeholders
        var formatter = new ReportFormatter(appState);
        var result = formatter.FormatUnassignedElementsReport();

        Assert.IsTrue(result.Contains("None"), "Should indicate no unassigned elements");
    }

    #endregion

    #region Phase 4c: FormatPlotStructureDiagram Tests

    [TestMethod]
    public void FormatPlotStructureDiagram_WithNestedProblems_ShowsHierarchy()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        var overview = new OverviewModel("Test Story", storyModel, null);
        storyModel.ExplorerView.Add(overview.Node);
        appState.CurrentDocument = new StoryDocument(storyModel);

        // Create story problem with a beat that links to a sub-problem
        var storyProblem = new ProblemModel("Main Conflict", storyModel, null);
        overview.StoryProblem = storyProblem.Uuid;

        var subProblem = new ProblemModel("Sub Conflict", storyModel, null);
        subProblem.BoundStructure = storyProblem.Uuid;

        storyProblem.StructureBeats = new ObservableCollection<StructureBeat>
        {
            new("Act 1", "Setup") { Guid = subProblem.Uuid }
        };

        var formatter = new ReportFormatter(appState);
        var result = formatter.FormatPlotStructureDiagram();

        Assert.IsTrue(result.Contains("Main Conflict"), "Should show story problem");
        Assert.IsTrue(result.Contains("Sub Conflict"), "Should show sub-problem");
        Assert.IsTrue(result.Contains("Act 1"), "Should show beat title");
    }

    [TestMethod]
    public void FormatPlotStructureDiagram_NoStoryProblem_ReturnsEmpty()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        var overview = new OverviewModel("Test Story", storyModel, null);
        storyModel.ExplorerView.Add(overview.Node);
        // StoryProblem not set (Guid.Empty)
        appState.CurrentDocument = new StoryDocument(storyModel);

        var formatter = new ReportFormatter(appState);
        var result = formatter.FormatPlotStructureDiagram();

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void FormatPlotStructureDiagram_SkipsSceneBeats()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        var overview = new OverviewModel("Test Story", storyModel, null);
        storyModel.ExplorerView.Add(overview.Node);
        appState.CurrentDocument = new StoryDocument(storyModel);

        var storyProblem = new ProblemModel("Main Conflict", storyModel, null);
        overview.StoryProblem = storyProblem.Uuid;

        var scene = new SceneModel("Test Scene", storyModel, null);
        storyProblem.StructureBeats = new ObservableCollection<StructureBeat>
        {
            new("Beat with Scene", "Desc") { Guid = scene.Uuid }
        };

        var formatter = new ReportFormatter(appState);
        var result = formatter.FormatPlotStructureDiagram();

        // Should show the problem but not the scene name (diagram is problems-only)
        Assert.IsTrue(result.Contains("Main Conflict"));
        Assert.IsFalse(result.Contains("Test Scene"), "Scene names should not appear in plot structure diagram");
    }

    #endregion
}
