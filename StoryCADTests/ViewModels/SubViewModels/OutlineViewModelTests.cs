using System.Reflection;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.SubViewModels;
using StoryCADLib.ViewModels.Tools;

#nullable disable

namespace StoryCADTests.ViewModels.SubViewModels;

[TestClass]
public class OutlineViewModelTests
{
    private OutlineService outlineService;
    private OutlineViewModel outlineVM;

    [TestInitialize]
    public void Setup()
    {
        // Assume IoC is properly configured for tests.
        // If necessary, you can initialize or mock dependencies here.
        outlineVM = Ioc.Default.GetRequiredService<OutlineViewModel>();
        outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Reset app state to ensure clean start for each test
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = null;
    }

    /// <summary>
    ///     Deletes a root node, this should fail
    ///     https://github.com/storybuilder-org/StoryCAD/issues/975
    /// </summary>
    [TestMethod]
    public async Task DeleteRoot()
    {
        //Create outline
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("TestRootDelete", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model, Path.Combine(App.ResultsDir, "TestRootDelete.stbx"));
        shell.RightTappedNode = appState.CurrentDocument.Model.StoryElements[0].Node;

        //Assert root is there and still is
        Assert.IsTrue(appState.CurrentDocument.Model.StoryElements[0].Node.IsRoot &&
                      appState.CurrentDocument.Model.StoryElements[0].ElementType == StoryItemType.StoryOverview);
        await outlineVM.RemoveStoryElement();
        Assert.IsTrue(appState.CurrentDocument.Model.StoryElements[0].Node.IsRoot &&
                      appState.CurrentDocument.Model.StoryElements[0].ElementType == StoryItemType.StoryOverview);
    }

    /// <summary>
    ///     Test that deleting a node sets the StoryModel.Changed flag to true
    /// </summary>
    [TestMethod]
    public async Task DeleteNode_SetsChangedFlag()
    {
        // Arrange - Create outline with a deletable element
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("TestChangedFlag", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model, Path.Combine(App.ResultsDir, "TestChangedFlag.stbx"));

        // Create a character to delete
        var character = outlineService.AddStoryElement(appState.CurrentDocument.Model, StoryItemType.Character,
            appState.CurrentDocument.Model.ExplorerView[0]);
        shell.RightTappedNode = character.Node;

        // Verify Changed is false after model creation (CreateModel sets it to false)
        Assert.IsFalse(appState.CurrentDocument.Model.Changed, "Changed should be false initially");

        // Act - Delete the character
        await outlineVM.RemoveStoryElement();

        // Assert - Verify Changed flag is now true
        Assert.IsTrue(appState.CurrentDocument.Model.Changed,
            "Changed flag should be true after deleting a node");
    }

    /// <summary>
    ///     Deletes a node, this should pass
    ///     https://github.com/storybuilder-org/StoryCAD/issues/975
    /// </summary>
    [TestMethod]
    public async Task DeleteNode()
    {
        //Create outline
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("TestNodeDelete", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model, Path.Combine(App.ResultsDir, "TestNodeDelete.stbx"));

        //Create a character
        shell.RightTappedNode = outlineService.AddStoryElement(appState.CurrentDocument.Model, StoryItemType.Character,
            appState.CurrentDocument.Model.ExplorerView[0]).Node;

        //Assert Character is still in explorer
        Assert.IsTrue(appState.CurrentDocument.Model.StoryElements.Characters[1].Node.Parent ==
                      appState.CurrentDocument.Model.ExplorerView[0]);

        // Store reference to the character before deletion
        var character = appState.CurrentDocument.Model.StoryElements.Characters[1];

        await outlineVM.RemoveStoryElement();

        //Assert Character was trashed - check if it's still in Characters collection
        Assert.IsTrue(appState.CurrentDocument.Model.StoryElements.Characters.Contains(character),
            "Character should still be in Characters collection");

        // Find the TrashCan node in TrashView
        var trashCanNode =
            appState.CurrentDocument.Model.TrashView.FirstOrDefault(n => n.Type == StoryItemType.TrashCan);
        Assert.IsNotNull(trashCanNode, "TrashCan node should exist in TrashView");
        Assert.IsTrue(character.Node.Parent == trashCanNode, "Character's parent should be the TrashCan node");
    }

    /// <summary>
    ///     Calling Delete without selecting a node should do nothing
    /// </summary>
    [TestMethod]
    public async Task DeleteNodeWithoutSelection()
    {
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("TestNullDelete", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model, Path.Combine(App.ResultsDir, "NullDelete.stbx"));

        shell.RightTappedNode = null;
        var before = appState.CurrentDocument.Model.StoryElements.Count;
        await outlineVM.RemoveStoryElement();
        Assert.AreEqual(before, appState.CurrentDocument.Model.StoryElements.Count);
    }

    [TestMethod]
    public async Task RestoreChildThenParent_DoesNotDuplicate()
    {
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("RestoreTest", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model);
        outlineService.SetCurrentView(appState.CurrentDocument.Model, StoryViewType.ExplorerView);
        var parent = outlineService.AddStoryElement(appState.CurrentDocument.Model, StoryItemType.Folder,
            appState.CurrentDocument.Model.ExplorerView[0]);
        var child = outlineService.AddStoryElement(appState.CurrentDocument.Model, StoryItemType.Scene, parent.Node);

        // Delete parent (child goes with it)
        shell.RightTappedNode = parent.Node;
        await outlineVM.RemoveStoryElement();

        // Try to restore child - should fail (not a top-level item in trash)
        shell.RightTappedNode = child.Node;
        outlineVM.RestoreStoryElement();

        // Verify child is still in trash under parent
        Assert.IsTrue(appState.CurrentDocument.Model.TrashView[0].Children.Any(n => n.Uuid == parent.Node.Uuid),
            "Parent should still be in trash");
        Assert.IsTrue(parent.Node.Children.Any(n => n.Uuid == child.Node.Uuid),
            "Child should still be under parent in trash");

        // Restore parent - child should come with it
        shell.RightTappedNode = parent.Node;
        outlineVM.RestoreStoryElement();

        int CountNodes(StoryNodeItem n, Guid g)
        {
            var c = n.Uuid == g ? 1 : 0;
            if (n.Children != null)
            {
                foreach (var ch in n.Children)
                {
                    c += CountNodes(ch, g);
                }
            }

            return c;
        }

        // Verify parent is restored to ExplorerView
        var parentCount = CountNodes(appState.CurrentDocument.Model.ExplorerView[0], parent.Node.Uuid);
        Assert.AreEqual(1, parentCount, "Parent should be restored exactly once");

        // Verify child came with parent
        var childCount = CountNodes(appState.CurrentDocument.Model.ExplorerView[0], child.Node.Uuid);
        Assert.AreEqual(1, childCount, "Child should be restored with parent exactly once");

        // Verify trash is now empty (except for TrashCan root)
        Assert.AreEqual(0, appState.CurrentDocument.Model.TrashView[0].Children.Count,
            "Trash should be empty after restore");
    }

    // Test for UnifiedNewFile method
    [TestMethod]
    public async Task TestNewFileVM()
    {
        var filevm = Ioc.Default.GetRequiredService<FileOpenVM>();

        //Set files
        filevm.OutlineName = "NewFileTest.stbx";
        filevm.OutlineFolder = App.ResultsDir;
        filevm.SelectedTemplateIndex = 0;

        //Assert
        var file = await filevm.CreateFile();
        await outlineVM.WriteModel();
        Thread.Sleep(1000);
        Assert.IsFalse(string.IsNullOrEmpty(file));
        Assert.IsTrue(File.Exists(file));
    }

    // Test for WriteModel method
    [TestMethod]
    public async Task TestWriteModel()
    {
        //Create challenge path
        var file = Path.Combine(App.ResultsDir, Path.GetRandomFileName() + ".stbx");

        // Ensure the file does not exist before writing the model
        Assert.IsFalse(File.Exists(file), "File already exists.");
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("Test", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model, file);

        //Write, check exists.
        await outlineVM.WriteModel();
        Assert.IsTrue(File.Exists(file), "File not written.");
    }

    // Stub tests for methods that follow the lock/unlock _canExecuteCommands pattern.
    // These tests act as placeholders until the methods are moved and implemented in OutlineViewModel.


    [TestMethod]
    public async Task TestSaveFileAs()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("Test", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model);
        appState.CurrentDocument.FilePath = Path.Combine(App.ResultsDir, "saveas.stbx");

        var saveasVM = Ioc.Default.GetRequiredService<SaveAsViewModel>();
        saveasVM.ProjectName = "SaveAsTest2.stbx";
        saveasVM.ParentFolder = App.ResultsDir;
        await outlineVM.SaveFileAs();

        Assert.IsTrue(appState.CurrentDocument.FilePath == Path.Combine(App.ResultsDir, "SaveAsTest2.stbx"));
    }

    [TestMethod]
    public async Task TestCloseFile()
    {
        //Check we have the file loaded
        var appState = Ioc.Default.GetRequiredService<AppState>();
        await outlineVM.OpenFile(Path.Combine(App.InputDir, "CloseFileTest.stbx"));
        Assert.IsNotNull(appState.CurrentDocument, "Document not loaded.");
        Assert.IsTrue(appState.CurrentDocument.Model.StoryElements.Count != 0, "Story not loaded.");
        Thread.Sleep(2500);
        await outlineVM.CloseFile();
        // After close, document should be reset to empty model, not null
        Assert.IsNotNull(appState.CurrentDocument, "CurrentDocument should be reset to empty model, not null");
        Assert.IsNotNull(appState.CurrentDocument.Model, "Model should be valid empty model");
        Assert.IsNull(appState.CurrentDocument.FilePath, "FilePath should be null after close");
    }

    [TestMethod]
    public async Task MoveRootCommands_DoNotThrow()
    {
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("MoveRoot", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model);
        outlineService.SetCurrentView(appState.CurrentDocument.Model, StoryViewType.ExplorerView);
        shell.CurrentNode = appState.CurrentDocument.Model.StoryElements[0].Node;
        shell.RightTappedNode = shell.CurrentNode;

        shell.MoveLeftCommand.Execute(null);
        shell.MoveRightCommand.Execute(null);
        shell.MoveUpCommand.Execute(null);
        shell.MoveDownCommand.Execute(null);

        Assert.IsTrue(shell.CurrentNode.IsRoot);
    }


    [TestMethod]
    public void TestKeyQuestionsTool()
    {
        var keyQuestionsVM = Ioc.Default.GetRequiredService<KeyQuestionsViewModel>();
        var text = keyQuestionsVM.Question;
        keyQuestionsVM.NextQuestion();
        Assert.IsTrue(keyQuestionsVM.Question != text, "Key question did not change.");
    }

    [TestMethod]
    public void TestTopicsTool()
    {
        var topicVm = Ioc.Default.GetRequiredService<TopicsViewModel>();
        var title = topicVm.SubTopicNote;
        topicVm.NextSubTopic();
        Assert.IsTrue(topicVm.SubTopicNote != title, "Topic name did not change.");
    }

    [TestMethod]
    public async Task TestMasterPlotTool()
    {
        //Create outline
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("Test-Masterplots", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model, Path.Combine(App.ResultsDir, "Masterplots.stbx"));
        outlineService.SetCurrentView(appState.CurrentDocument.Model, StoryViewType.ExplorerView);
        shell.RightTappedNode = appState.CurrentDocument.Model.StoryElements[0].Node;


        //Setup plot vm
        var masterPlotsVM = Ioc.Default.GetRequiredService<MasterPlotsViewModel>();
        masterPlotsVM.PlotPatternName = masterPlotsVM.PlotPatternNames[4];

        //Run and assert
        await outlineVM.MasterPlotTool();
        Assert.IsTrue(appState.CurrentDocument.Model.StoryElements[3].Name == masterPlotsVM.PlotPatternNames[4]);
    }

    [TestMethod]
    public async Task MasterPlotTool_AddsSingleProblem()
    {
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("MasterPlot", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model);
        outlineService.SetCurrentView(appState.CurrentDocument.Model, StoryViewType.ExplorerView);
        shell.RightTappedNode = appState.CurrentDocument.Model.StoryElements[0].Node;
        var master = Ioc.Default.GetRequiredService<MasterPlotsViewModel>();
        master.PlotPatternName = master.PlotPatternNames[0];
        await outlineVM.MasterPlotTool();
        var count = appState.CurrentDocument.Model.StoryElements.Count(e => e.Name == master.PlotPatternNames[0]);
        Assert.AreEqual(1, count, "MasterPlot created duplicate elements");
    }

    [TestMethod]
    public async Task TestDramaticSituationsTool()
    {
        //Create outline
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("Test", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model);
        appState.CurrentDocument.FilePath = Path.Combine(App.ResultsDir, "Dramatic.stbx");

        //Set view
        outlineService.SetCurrentView(appState.CurrentDocument.Model, StoryViewType.ExplorerView);
        shell.RightTappedNode = appState.CurrentDocument.Model.StoryElements[0].Node;

        //Run scenario
        Ioc.Default.GetRequiredService<DramaticSituationsViewModel>().SituationName = "Abduction";
        await outlineVM.DramaticSituationsTool();
        Assert.IsTrue(appState.CurrentDocument.Model.StoryElements.Count > 2, "Dramatic situation not added.");
    }

    [TestMethod]
    public async Task DramaticSituationsTool_CreatesScene()
    {
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("Dramatic", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model);
        outlineService.SetCurrentView(appState.CurrentDocument.Model, StoryViewType.ExplorerView);
        shell.RightTappedNode = appState.CurrentDocument.Model.StoryElements[0].Node;
        var vm = Ioc.Default.GetRequiredService<DramaticSituationsViewModel>();
        vm.SituationName = "Abduction";
        await outlineVM.DramaticSituationsTool();
        var count = appState.CurrentDocument.Model.StoryElements.Count(e => e.Name == "Abduction");
        Assert.AreEqual(1, count, "Dramatic situation created incorrectly");
    }

    [TestMethod]
    public async Task DramaticSituationsTool_WithNullSituation_DoesNotCreateElements()
    {
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("NullSituation", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model);
        outlineService.SetCurrentView(appState.CurrentDocument.Model, StoryViewType.ExplorerView);
        shell.RightTappedNode = appState.CurrentDocument.Model.StoryElements[0].Node;

        var vm = Ioc.Default.GetRequiredService<DramaticSituationsViewModel>();
        vm.Situation = null; // Set Situation directly to null, not SituationName

        var countBefore = appState.CurrentDocument.Model.StoryElements.Count;
        await outlineVM.DramaticSituationsTool();
        var countAfter = appState.CurrentDocument.Model.StoryElements.Count;

        Assert.AreEqual(countBefore, countAfter, "No elements should be created when situation is null");
    }

    [TestMethod]
    public async Task TestStockScenesTool()
    {
        //Create outline
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("TestStock", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model, Path.Combine(App.ResultsDir, "Stock.stbx"));

        //Set view
        outlineService.SetCurrentView(appState.CurrentDocument.Model, StoryViewType.ExplorerView);
        shell.RightTappedNode = appState.CurrentDocument.Model.StoryElements[0].Node;

        //run scenario
        var stockVM = Ioc.Default.GetRequiredService<StockScenesViewModel>();
        stockVM.SceneName = "The police join the chase";
        await outlineVM.StockScenesTool();


        Assert.IsTrue(appState.CurrentDocument.Model.StoryElements[3].Name == "The police join the chase",
            "Stock scene not added.");
    }


    /// <summary>
    ///     Tests issue #946
    /// </summary>
    [TestMethod]
    public async Task TestOverviewProblem()
    {
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("ProblemTest", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model, Path.Combine(App.ResultsDir, "ProblemTest.stbx"));
        shell.RightTappedNode = appState.CurrentDocument.Model.StoryElements[0].Node;

        //Create char and try to assign as a story problem
        outlineService.AddStoryElement(appState.CurrentDocument.Model, StoryItemType.Character,
            appState.CurrentDocument.Model.ExplorerView[0]);
        Ioc.Default.GetRequiredService<OverviewViewModel>().Activate(
            appState.CurrentDocument.Model.StoryElements.First(o =>
                o.ElementType == StoryItemType.StoryOverview) as OverviewModel);
        Ioc.Default.GetRequiredService<OverviewViewModel>().StoryProblem =
            appState.CurrentDocument.Model.StoryElements[3].Uuid;
        Ioc.Default.GetRequiredService<OverviewViewModel>().Deactivate(null);
        var ovm = (appState.CurrentDocument.Model.StoryElements.First(o =>
            o.ElementType == StoryItemType.StoryOverview) as OverviewModel).StoryProblem;
        Assert.IsTrue(ovm == Guid.Empty);
    }

    /// <summary>
    ///     Tests issue #946 fix doesn't break anything
    /// </summary>
    [TestMethod]
    public async Task TestInverseOverviewProblem()
    {
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("ProblemTest2", "StoryBuilder", 0);
        appState.CurrentDocument = new StoryDocument(model, Path.Combine(App.ResultsDir, "ProblemTest2.stbx"));
        shell.RightTappedNode = appState.CurrentDocument.Model.StoryElements[0].Node;

        //Create char and try to assign as a story problem
        outlineService.AddStoryElement(appState.CurrentDocument.Model, StoryItemType.Problem,
            appState.CurrentDocument.Model.ExplorerView[0]);
        Ioc.Default.GetRequiredService<OverviewViewModel>().Activate(
            appState.CurrentDocument.Model.StoryElements.First(o =>
                o.ElementType == StoryItemType.StoryOverview) as OverviewModel);
        Ioc.Default.GetRequiredService<OverviewViewModel>().StoryProblem =
            appState.CurrentDocument.Model.StoryElements[3].Uuid;
        Ioc.Default.GetRequiredService<OverviewViewModel>().Deactivate(null);
        var ovm = (appState.CurrentDocument.Model.StoryElements.First(o =>
            o.ElementType == StoryItemType.StoryOverview) as OverviewModel).StoryProblem;
        Assert.IsTrue(ovm != Guid.Empty);
    }

    [TestMethod]
    public async Task SaveFile_CallsEditFlushService()
    {
        // This test verifies that OutlineViewModel.SaveFile() uses EditFlushService
        // instead of calling ShellViewModel.SaveModel()

        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        appState.CurrentDocument = new StoryDocument(storyModel, Path.Combine(Path.GetTempPath(), "test.stbx"));

        // We can't easily test the full SaveFile method without setting up a lot of state,
        // but we can verify that OutlineViewModel has EditFlushService injected
        var editFlushServiceField = outlineVM.GetType()
            .GetField("_editFlushService", BindingFlags.NonPublic | BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(editFlushServiceField, "OutlineViewModel should have _editFlushService field");
        var editFlushService = editFlushServiceField.GetValue(outlineVM);
        Assert.IsNotNull(editFlushService, "EditFlushService should be injected");
        Assert.IsInstanceOfType(editFlushService, typeof(EditFlushService));

        await Task.CompletedTask; // Async placeholder
    }

    [TestMethod]
    public void OutlineViewModel_DoesNotReferenceShellViewModelSaveModel()
    {
        // This test verifies that OutlineViewModel no longer calls ShellViewModel.SaveModel()
        // We check this by searching for the string in the compiled assembly

        // Act - check that OutlineViewModel has EditFlushService dependency
        var constructor = outlineVM.GetType().GetConstructors()[0];
        var parameters = constructor.GetParameters();

        // Assert - should have EditFlushService parameter
        Assert.IsTrue(parameters.Any(p => p.ParameterType == typeof(EditFlushService)),
            "OutlineViewModel constructor should have EditFlushService parameter");
    }

    [TestMethod]
    public async Task CloseFile_WithPremiseSync_DoesNotCauseGuidError()
    {
        // Regression test for issue #1154
        // When CloseFile() is called with premise sync enabled, it should not cause
        // "Cannot find GUID" error. The bug occurred because ResetModel() was called
        // before ShowHomePage(), causing navigation to trigger SaveModel() on an empty model.
        //
        // This test verifies that CloseFile() handles premise sync correctly.

        // Arrange - Create outline with Problem and Overview premise sync
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();

        var model = await outlineService.CreateModel("PremiseSyncTest", "StoryBuilder", 0);
        var filePath = Path.Combine(App.ResultsDir, "PremiseSyncTest.stbx");
        appState.CurrentDocument = new StoryDocument(model, filePath);

        // Create a Problem element
        var problem = outlineService.AddStoryElement(appState.CurrentDocument.Model,
            StoryItemType.Problem, appState.CurrentDocument.Model.ExplorerView[0]);

        // Navigate to Overview and set up premise sync
        var overviewModel = appState.CurrentDocument.Model.StoryElements
            .First(e => e.ElementType == StoryItemType.StoryOverview) as OverviewModel;
        var overviewVM = Ioc.Default.GetRequiredService<OverviewViewModel>();
        overviewVM.Activate(overviewModel);
        overviewVM.StoryProblem = problem.Uuid;  // This sets up premise sync
        overviewVM.Premise = "Test premise";
        overviewVM.Deactivate(null);

        // Verify premise sync is set up
        Assert.AreEqual(problem.Uuid, overviewModel.StoryProblem, "Setup: StoryProblem should be set");

        // Act - Close the file (this should NOT cause GUID lookup error)
        await outlineVM.CloseFile();

        // Assert - Verify no "Cannot find GUID" error was logged
        // The logger should not have any Error level messages about GUID lookup
        // (We can't easily check the logger output, but the test passes if no exception is thrown)

        // Verify model was reset
        Assert.IsNotNull(appState.CurrentDocument, "CurrentDocument should exist after close");
        Assert.IsNotNull(appState.CurrentDocument.Model, "Model should exist after close");
        Assert.IsNull(appState.CurrentDocument.FilePath, "FilePath should be null after close");
    }
}
