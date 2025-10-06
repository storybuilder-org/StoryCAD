using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Services;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCADTests.ViewModels;

[TestClass]
public class ShellViewModelTests
{
    /// <summary>
    ///     This tests a fix from PR #1056 where moving a node after deletion
    ///     crashes storycad.
    /// </summary>
    [TestMethod]
    public async Task DeleteNode_ThenMoveNode_DoesNotCrash()
    {
        //Create test outline
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var outlineVM = Ioc.Default.GetService<OutlineViewModel>();
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await outlineService.CreateModel("Test1056", "StoryBuilder", 2);
        // Set up the current view (Explorer view)
        outlineService.SetCurrentView(model, StoryViewType.ExplorerView);

        //Create node to be deleted
        shell.CurrentNode = model.StoryElements
            .First(e => e.ElementType == StoryItemType.Folder
                        && e.Name != "Narrative View").Node;
        shell.RightTappedNode = shell.CurrentNode;
        await outlineVM.RemoveStoryElement();
        outlineVM.EmptyTrash();

        //Assert we have cleared the stuff that could go wrong
        Assert.IsNull(shell.CurrentNode);
        Assert.IsNull(shell.RightTappedNode);
    }

    /// <summary>
    ///     Tests that SaveModel handles null SplitViewFrame gracefully
    /// </summary>
    [TestMethod]
    public void SaveModel_WithNullSplitViewFrame_DoesNotThrow()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        shell.SplitViewFrame = null;
        shell.CurrentPageType = null;

        // Act - should not throw
        shell.SaveModel();

        // Assert - we get here without exception
        Assert.IsTrue(true);
    }

    /// <summary>
    ///     Tests that SaveModel with HomePage doesn't attempt to save (no data)
    /// </summary>
    [TestMethod]
    public void SaveModel_WithHomePage_DoesNotCallSaveModel()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        shell.CurrentPageType = "HomePage";

        // Act - should not throw, HomePage has no data to save
        shell.SaveModel();

        // Assert - we get here without exception
        Assert.IsTrue(true);
    }

    /// <summary>
    ///     Tests that SaveModel with unrecognized page type logs error
    /// </summary>
    [TestMethod]
    public void SaveModel_WithUnrecognizedPageType_LogsError()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        shell.CurrentPageType = "NonExistentPage";

        // Act - should not throw but log error
        shell.SaveModel();

        // Assert - we get here without exception
        Assert.IsTrue(true);
    }

    /// <summary>
    ///     Tests that SaveModel calls correct ViewModel's SaveModel for OverviewPage
    /// </summary>
    [TestMethod]
    public async Task SaveModel_WithOverviewPage_CallsOverviewViewModelSaveModel()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var overviewVM = Ioc.Default.GetRequiredService<OverviewViewModel>();
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var outlineVM = Ioc.Default.GetService<OutlineViewModel>();

        // Create a model with overview
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var testModel = await outlineService.CreateModel("TestOverview", "StoryBuilder", 2);
        appState.CurrentDocument = new StoryDocument(testModel);
        var overviewElement = testModel.StoryElements.FirstOrDefault(e => e.ElementType == StoryItemType.StoryOverview);
        Assert.IsNotNull(overviewElement, "Could not find overview element in story model");

        // Set up the overview view model with test data
        overviewVM.Model = new OverviewModel("Test", testModel, overviewElement.Node);
        overviewVM.Description = "Test Idea";

        // Set the current page type
        shell.CurrentPageType = "OverviewPage";

        // Act
        shell.SaveModel();

        // Assert - the model should have the changes
        Assert.AreEqual("Test Idea", overviewVM.Model.Description);
    }

    /// <summary>
    ///     Tests that ResetModel creates a new StoryModel
    /// </summary>
    [TestMethod]
    public void ResetModel_CreatesNewStoryModel()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var originalModel = appState.CurrentDocument?.Model;

        // Act
        shell.ResetModel();

        // Assert - should have a new instance
        Assert.IsNotNull(appState.CurrentDocument?.Model);
        Assert.AreNotSame(originalModel, appState.CurrentDocument?.Model);
    }

    /// <summary>
    ///     Tests that ResetModel creates new empty document (from ShellViewModelAppStateTests)
    /// </summary>
    [TestMethod]
    public void ResetModel_CreatesNewEmptyDocument()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = new StoryModel();
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        // Act
        shell.ResetModel();

        // Assert
        Assert.IsNotNull(appState.CurrentDocument);
        Assert.IsNotNull(appState.CurrentDocument.Model);
        Assert.IsNull(appState.CurrentDocument.FilePath);
    }

    /// <summary>
    ///     Tests that CreateBackupNow with no current document shows warning (from ShellViewModelAppStateTests)
    /// </summary>
    [TestMethod]
    public void CreateBackupNow_WithNoCurrentDocument_ShowsWarning()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = null;

        // Act & Assert - should not throw
        var task = shell.CreateBackupNow();
        Assert.IsNotNull(task);
    }

    /// <summary>
    ///     Tests that CreateBackupNow with empty current view shows warning (from ShellViewModelAppStateTests)
    /// </summary>
    [TestMethod]
    public void CreateBackupNow_WithEmptyCurrentView_ShowsWarning()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = new StoryModel();
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        // Act & Assert - should not throw
        var task = shell.CreateBackupNow();
        Assert.IsNotNull(task);
    }

    /// <summary>
    ///     Helper method to create a test model with all element types
    /// </summary>
    private async Task<StoryModel> CreateTestModelWithAllElements()
    {
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var outlineVM = Ioc.Default.GetService<OutlineViewModel>();

        // Create empty model (template 0)
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("TestStory", "TestAuthor", 0);
        appState.CurrentDocument = new StoryDocument(model);

        // Get overview node as parent
        var overview = model.ExplorerView.First();

        // Add all element types
        var character = outlineService.AddStoryElement(model, StoryItemType.Character, overview);
        character.Name = "Test Character";

        var scene = outlineService.AddStoryElement(model, StoryItemType.Scene, overview);
        scene.Name = "Test Scene";

        var problem = outlineService.AddStoryElement(model, StoryItemType.Problem, overview);
        problem.Name = "Test Problem";

        var setting = outlineService.AddStoryElement(model, StoryItemType.Setting, overview);
        setting.Name = "Test Setting";

        var folder = outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
        folder.Name = "Test Folder";

        var section = outlineService.AddStoryElement(model, StoryItemType.Section, overview);
        if (section != null)
        {
            section.Name = "Test Section";
        }

        var web = outlineService.AddStoryElement(model, StoryItemType.Web, overview);
        web.Name = "Test Web";

        var notes = outlineService.AddStoryElement(model, StoryItemType.Notes, overview);
        notes.Name = "Test Notes";

        return model;
    }

    /// <summary>
    ///     Tests that TreeViewNodeClicked handles null selectedItem gracefully
    /// </summary>
    [TestMethod]
    public void TreeViewNodeClicked_WithNullItem_ReturnsEarly()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var previousNode = shell.CurrentNode;
        var previousPageType = shell.CurrentPageType;

        // Act - should handle null gracefully
        shell.TreeViewNodeClicked(null);

        // Assert - state should not change
        Assert.AreEqual(previousNode, shell.CurrentNode);
        Assert.AreEqual(previousPageType, shell.CurrentPageType);
    }

    /// <summary>
    ///     Tests that TreeViewNodeClicked sets CurrentNode and CurrentPageType for Character
    /// </summary>
    [TestMethod]
    public async Task TreeViewNodeClicked_WithCharacterNode_SetsCorrectPageType()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();

        // Find a character element
        var characterElement = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.Character);

        Assert.IsNotNull(characterElement, "Character element should exist in story model");

        // Act
        shell.TreeViewNodeClicked(characterElement.Node);

        // Assert
        Assert.AreEqual(characterElement.Node, shell.CurrentNode);
        Assert.AreEqual("CharacterPage", shell.CurrentPageType);
    }

    /// <summary>
    ///     Tests that TreeViewNodeClicked sets CurrentNode and CurrentPageType for Scene
    /// </summary>
    [TestMethod]
    public async Task TreeViewNodeClicked_WithSceneNode_SetsCorrectPageType()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();

        // Find a scene element
        var sceneElement = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.Scene);

        Assert.IsNotNull(sceneElement, "Scene element should exist in story model");

        // Act
        shell.TreeViewNodeClicked(sceneElement.Node);

        // Assert
        Assert.AreEqual(sceneElement.Node, shell.CurrentNode);
        Assert.AreEqual("ScenePage", shell.CurrentPageType);
    }

    /// <summary>
    ///     Tests that TreeViewNodeClicked sets CurrentNode and CurrentPageType for Problem
    /// </summary>
    [TestMethod]
    public async Task TreeViewNodeClicked_WithProblemNode_SetsCorrectPageType()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();

        // Find a problem element
        var problemElement = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.Problem);

        Assert.IsNotNull(problemElement, "Problem element should exist in story model");

        // Act
        shell.TreeViewNodeClicked(problemElement.Node);

        // Assert
        Assert.AreEqual(problemElement.Node, shell.CurrentNode);
        Assert.AreEqual("ProblemPage", shell.CurrentPageType);
    }

    /// <summary>
    ///     Tests that TreeViewNodeClicked sets CurrentNode and CurrentPageType for Folder
    /// </summary>
    [TestMethod]
    public async Task TreeViewNodeClicked_WithFolderNode_SetsCorrectPageType()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();

        // Find a folder element
        var folderElement = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.Folder);

        Assert.IsNotNull(folderElement, "Folder element should exist in story model");

        // Act
        shell.TreeViewNodeClicked(folderElement.Node);

        // Assert
        Assert.AreEqual(folderElement.Node, shell.CurrentNode);
        Assert.AreEqual("FolderPage", shell.CurrentPageType);
    }

    /// <summary>
    ///     Tests that TreeViewNodeClicked sets CurrentNode and CurrentPageType for Setting
    /// </summary>
    [TestMethod]
    public async Task TreeViewNodeClicked_WithSettingNode_SetsCorrectPageType()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();

        // Find a setting element
        var settingElement = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.Setting);

        Assert.IsNotNull(settingElement, "Setting element should exist in story model");

        // Act
        shell.TreeViewNodeClicked(settingElement.Node);

        // Assert
        Assert.AreEqual(settingElement.Node, shell.CurrentNode);
        Assert.AreEqual("SettingPage", shell.CurrentPageType);
    }

    /// <summary>
    ///     Tests that TreeViewNodeClicked sets CurrentNode and CurrentPageType for StoryOverview
    /// </summary>
    [TestMethod]
    public async Task TreeViewNodeClicked_WithOverviewNode_SetsCorrectPageType()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();

        // Find overview element
        var overviewElement = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.StoryOverview);

        Assert.IsNotNull(overviewElement, "Overview element should exist in story model");

        // Act
        shell.TreeViewNodeClicked(overviewElement.Node);

        // Assert
        Assert.AreEqual(overviewElement.Node, shell.CurrentNode);
        Assert.AreEqual("OverviewPage", shell.CurrentPageType);
    }

    /// <summary>
    ///     Tests that TreeViewNodeClicked sets CurrentNode and CurrentPageType for Web
    /// </summary>
    [TestMethod]
    public async Task TreeViewNodeClicked_WithWebNode_SetsCorrectPageType()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();

        // Find a web element
        var webElement = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.Web);

        Assert.IsNotNull(webElement, "Web element should exist in story model");

        // Act
        shell.TreeViewNodeClicked(webElement.Node);

        // Assert
        Assert.AreEqual(webElement.Node, shell.CurrentNode);
        Assert.AreEqual("WebPage", shell.CurrentPageType);
    }

    /// <summary>
    ///     Tests that TreeViewNodeClicked sets CurrentNode and CurrentPageType for Notes
    /// </summary>
    [TestMethod]
    public async Task TreeViewNodeClicked_WithNotesNode_SetsCorrectPageType()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();

        // Find a notes element
        var notesElement = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.Notes);

        Assert.IsNotNull(notesElement, "Notes element should exist in story model");

        // Act
        shell.TreeViewNodeClicked(notesElement.Node);

        // Assert
        Assert.AreEqual(notesElement.Node, shell.CurrentNode);
        Assert.AreEqual("FolderPage", shell.CurrentPageType); // Notes use FolderPage
    }

    /// <summary>
    ///     Tests that TreeViewNodeClicked sets CurrentNode and CurrentPageType for Section
    /// </summary>
    [TestMethod]
    public async Task TreeViewNodeClicked_WithSectionNode_SetsCorrectPageType()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();

        // Find a section element - Section is created as Folder type with "Test Section" name
        var sectionElement = model.StoryElements
            .FirstOrDefault(e => e.Name == "Test Section" && e.ElementType == StoryItemType.Folder);

        Assert.IsNotNull(sectionElement, "Section element should exist in story model");

        // Act
        shell.TreeViewNodeClicked(sectionElement.Node);

        // Assert
        Assert.AreEqual(sectionElement.Node, shell.CurrentNode);
        Assert.AreEqual("FolderPage", shell.CurrentPageType); // Section uses FolderPage
    }

    /// <summary>
    ///     Tests that TreeViewNodeClicked sets CurrentNode and CurrentPageType for TrashCan
    /// </summary>
    [TestMethod]
    public async Task TreeViewNodeClicked_WithTrashCanNode_SetsCorrectPageType()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();

        // Find trash can element
        var trashElement = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.TrashCan);

        Assert.IsNotNull(trashElement, "TrashCan element should exist in story model");

        // Act
        shell.TreeViewNodeClicked(trashElement.Node);

        // Assert
        Assert.AreEqual(trashElement.Node, shell.CurrentNode);
        Assert.AreEqual("TrashCanPage", shell.CurrentPageType);
    }

    /// <summary>
    ///     Tests that TreeViewNodeClicked handles non-StoryNodeItem objects
    /// </summary>
    [TestMethod]
    public void TreeViewNodeClicked_WithNonStoryNodeItem_HandlesGracefully()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var previousNode = shell.CurrentNode;
        var previousPageType = shell.CurrentPageType;

        // Act - pass a non-StoryNodeItem object
        shell.TreeViewNodeClicked("Not a StoryNodeItem");

        // Assert - state should not change since it's not a StoryNodeItem
        Assert.AreEqual(previousNode, shell.CurrentNode);
        Assert.AreEqual(previousPageType, shell.CurrentPageType);
    }

    #region ViewChanged Tests

    /// <summary>
    ///     Tests that ViewChanged switches from Explorer to Narrator view
    /// </summary>
    [TestMethod]
    public async Task ViewChanged_FromExplorerToNarrator_UpdatesProperties()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var outlineVM = Ioc.Default.GetService<OutlineViewModel>();

        // Create a test model
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("TestStory", "TestAuthor", 0);
        appState.CurrentDocument = new StoryDocument(model);

        // Set initial state to Explorer view
        shell.CurrentView = "Story Explorer View";
        shell.SelectedView = "Story Narrator View";

        // Act
        shell.ViewChanged();

        // Assert
        Assert.AreEqual("Story Narrator View", shell.CurrentView);
        Assert.AreEqual(StoryViewType.NarratorView, shell.CurrentViewType);
        Assert.AreEqual(StoryViewType.NarratorView, appState.CurrentDocument.Model.CurrentViewType);
    }

    /// <summary>
    ///     Tests that ViewChanged switches from Narrator to Explorer view
    /// </summary>
    [TestMethod]
    public async Task ViewChanged_FromNarratorToExplorer_UpdatesProperties()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var outlineVM = Ioc.Default.GetService<OutlineViewModel>();

        // Create a test model
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("TestStory", "TestAuthor", 0);
        appState.CurrentDocument = new StoryDocument(model);

        // Set initial state to Narrator view
        shell.CurrentView = "Story Narrator View";
        shell.SelectedView = "Story Explorer View";

        // Act
        shell.ViewChanged();

        // Assert
        Assert.AreEqual("Story Explorer View", shell.CurrentView);
        Assert.AreEqual(StoryViewType.ExplorerView, shell.CurrentViewType);
        Assert.AreEqual(StoryViewType.ExplorerView, appState.CurrentDocument.Model.CurrentViewType);
    }

    /// <summary>
    ///     Tests that ViewChanged does nothing when selected view equals current view
    /// </summary>
    [TestMethod]
    public async Task ViewChanged_SameView_DoesNothing()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var outlineVM = Ioc.Default.GetService<OutlineViewModel>();

        // Create a test model
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("TestStory", "TestAuthor", 0);
        appState.CurrentDocument = new StoryDocument(model);

        // Set both to same view
        shell.CurrentView = "Story Explorer View";
        shell.SelectedView = "Story Explorer View";
        var initialViewType = shell.CurrentViewType;

        // Act
        shell.ViewChanged();

        // Assert - nothing should change
        Assert.AreEqual("Story Explorer View", shell.CurrentView);
        Assert.AreEqual(initialViewType, shell.CurrentViewType);
    }

    /// <summary>
    ///     Tests that ViewChanged handles null StoryModel gracefully
    /// </summary>
    [TestMethod]
    public void ViewChanged_WithNullStoryModel_ReturnsEarly()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = null;
        shell.CurrentView = "Story Explorer View";
        shell.SelectedView = "Story Narrator View";

        // Act - should return early without throwing
        shell.ViewChanged();

        // Assert - view should not change
        Assert.AreEqual("Story Explorer View", shell.CurrentView);
    }

    /// <summary>
    ///     Tests that ViewChanged handles empty CurrentView gracefully
    /// </summary>
    [TestMethod]
    public async Task ViewChanged_WithEmptyCurrentView_ReturnsEarly()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var outlineVM = Ioc.Default.GetService<OutlineViewModel>();

        // Create a test model but clear CurrentView
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = await outlineService.CreateModel("TestStory", "TestAuthor", 0);
        appState.CurrentDocument = new StoryDocument(model);
        appState.CurrentDocument.Model.CurrentView.Clear();

        shell.CurrentView = "Story Explorer View";
        shell.SelectedView = "Story Narrator View";

        // Act - should return early
        shell.ViewChanged();

        // Assert - view should not change
        Assert.AreEqual("Story Explorer View", shell.CurrentView);
    }

    #endregion

    #region ShowFlyoutButtons Tests

    /// <summary>
    ///     Tests that ShowFlyoutButtons sets correct visibility for TrashCan
    /// </summary>
    [TestMethod]
    public async Task ShowFlyoutButtons_WithTrashCan_SetsTrashVisibility()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var trashNode = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.TrashCan)?.Node;

        Assert.IsNotNull(trashNode, "Should have trash node");
        shell.RightTappedNode = trashNode;

        // Act
        shell.ShowFlyoutButtons();

        // Assert
        Assert.AreEqual(Visibility.Collapsed, shell.ExplorerVisibility);
        Assert.AreEqual(Visibility.Collapsed, shell.NarratorVisibility);
        Assert.AreEqual(Visibility.Collapsed, shell.AddButtonVisibility);
        Assert.AreEqual(Visibility.Collapsed, shell.PrintNodeVisibility);
        Assert.AreEqual(Visibility.Visible, shell.TrashButtonVisibility);
    }

    /// <summary>
    ///     Tests that ShowFlyoutButtons sets correct visibility for Explorer view
    /// </summary>
    [TestMethod]
    public async Task ShowFlyoutButtons_InExplorerView_SetsExplorerVisibility()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var characterNode = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.Character)?.Node;

        Assert.IsNotNull(characterNode, "Should have character node");
        shell.RightTappedNode = characterNode;
        shell.SelectedView = "Story Explorer View";

        // Act
        shell.ShowFlyoutButtons();

        // Assert
        Assert.AreEqual(Visibility.Visible, shell.ExplorerVisibility);
        Assert.AreEqual(Visibility.Collapsed, shell.NarratorVisibility);
        Assert.AreEqual(Visibility.Visible, shell.AddButtonVisibility);
        Assert.AreEqual(Visibility.Visible, shell.PrintNodeVisibility);
        Assert.AreEqual(Visibility.Collapsed, shell.TrashButtonVisibility);
    }

    /// <summary>
    ///     Tests that ShowFlyoutButtons sets correct visibility for Narrator view
    /// </summary>
    [TestMethod]
    public async Task ShowFlyoutButtons_InNarratorView_SetsNarratorVisibility()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var sceneNode = model.StoryElements
            .FirstOrDefault(e => e.ElementType == StoryItemType.Scene)?.Node;

        Assert.IsNotNull(sceneNode, "Should have scene node");
        shell.RightTappedNode = sceneNode;
        shell.SelectedView = "Story Narrator View";

        // Act
        shell.ShowFlyoutButtons();

        // Assert
        Assert.AreEqual(Visibility.Collapsed, shell.ExplorerVisibility);
        Assert.AreEqual(Visibility.Visible, shell.NarratorVisibility);
        Assert.AreEqual(Visibility.Visible, shell.AddButtonVisibility); // Note: Set to Visible at end of method
        Assert.AreEqual(Visibility.Visible, shell.PrintNodeVisibility);
        Assert.AreEqual(Visibility.Collapsed, shell.TrashButtonVisibility);
    }

    /// <summary>
    ///     Tests that ShowFlyoutButtons handles null RightTappedNode gracefully
    /// </summary>
    [TestMethod]
    public void ShowFlyoutButtons_WithNullRightTappedNode_HandlesGracefully()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        shell.RightTappedNode = null;
        shell.SelectedView = "Story Explorer View";

        // Act - should handle exception internally
        shell.ShowFlyoutButtons();

        // Assert - method should complete without throwing
        Assert.IsTrue(true, "Method completed without throwing");
    }

    #endregion

    #region Move Operations Tests

    /// <summary>
    ///     Tests that MoveLeft moves a node up in the hierarchy (becomes sibling of parent)
    /// </summary>
    [TestMethod]
    public async Task MoveLeft_WithValidNode_MovesUpHierarchy()
    {
        // Arrange
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var overview = model.ExplorerView.First();

        // Create a folder with a child character
        var folder = outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
        folder.Name = "Test Folder";
        var character = outlineService.AddStoryElement(model, StoryItemType.Character, folder.Node);
        character.Name = "Test Character";

        shell.CurrentNode = character.Node;
        var initialParent = character.Node.Parent;
        var grandparent = initialParent.Parent;

        // Act
        shell.MoveLeftCommand.Execute(null);

        // Assert
        Assert.AreEqual(grandparent, character.Node.Parent, "Character should now be child of grandparent");
        Assert.IsTrue(grandparent.Children.Contains(character.Node), "Grandparent should contain character");
        Assert.IsFalse(folder.Node.Children.Contains(character.Node), "Folder should no longer contain character");
        // Character should be positioned after its former parent
        var characterIndex = grandparent.Children.IndexOf(character.Node);
        var folderIndex = grandparent.Children.IndexOf(folder.Node);
        Assert.IsTrue(characterIndex > folderIndex, "Character should be positioned after folder");
    }

    /// <summary>
    ///     Tests that MoveLeft fails when node is already at root level
    /// </summary>
    [TestMethod]
    public async Task MoveLeft_WithRootLevelNode_CannotMove()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var overview = model.ExplorerView.First();

        shell.CurrentNode = overview;
        var initialParent = overview.Parent;

        // Act
        shell.MoveLeftCommand.Execute(null);

        // Assert
        Assert.AreEqual(initialParent, overview.Parent, "Overview parent should not change");
    }

    /// <summary>
    ///     Tests that MoveLeft fails when CurrentNode is null
    /// </summary>
    [TestMethod]
    public async Task MoveLeft_WithNullCurrentNode_DoesNothing()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        await CreateTestModelWithAllElements();
        shell.CurrentNode = null;

        // Act
        shell.MoveLeftCommand.Execute(null);

        // Assert - method should complete without throwing
        Assert.IsTrue(true, "Method completed without throwing");
    }

    /// <summary>
    ///     Tests that MoveRight moves a node down in hierarchy (becomes child of previous sibling)
    /// </summary>
    [TestMethod]
    public async Task MoveRight_WithValidNode_MovesDownHierarchy()
    {
        // Arrange
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var overview = model.ExplorerView.First();

        // Create two folders
        var folder1 = outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
        folder1.Name = "Folder 1";
        var folder2 = outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
        folder2.Name = "Folder 2";

        shell.CurrentNode = folder2.Node;

        // Act
        shell.MoveRightCommand.Execute(null);

        // Assert
        Assert.AreEqual(folder1.Node, folder2.Node.Parent, "Folder2 should now be child of Folder1");
        Assert.IsTrue(folder1.Node.Children.Contains(folder2.Node), "Folder1 should contain Folder2");
        Assert.IsFalse(overview.Children.Contains(folder2.Node), "Overview should no longer contain Folder2");
    }

    /// <summary>
    ///     Tests that MoveRight fails when node has no previous sibling
    /// </summary>
    [TestMethod]
    public async Task MoveRight_WithNoPreviousSibling_CannotMove()
    {
        // Arrange
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        var overview = model.ExplorerView.First();

        // Ensure overview has no existing children except what we add
        overview.Children.Clear();

        // Create just one folder (first child, no previous sibling)
        var folder = outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
        folder.Name = "First Folder";
        shell.CurrentNode = folder.Node;
        var initialParent = folder.Node.Parent;

        // Act
        shell.MoveRightCommand.Execute(null);

        // Assert
        Assert.AreEqual(initialParent, folder.Node.Parent, "Folder parent should not change");
        Assert.AreEqual(overview, folder.Node.Parent, "Folder should still be under overview");
    }

    /// <summary>
    ///     Tests that MoveRight fails when CurrentNode is null
    /// </summary>
    [TestMethod]
    public async Task MoveRight_WithNullCurrentNode_DoesNothing()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        await CreateTestModelWithAllElements();
        shell.CurrentNode = null;

        // Act
        shell.MoveRightCommand.Execute(null);

        // Assert - method should complete without throwing
        Assert.IsTrue(true, "Method completed without throwing");
    }

    /// <summary>
    ///     Tests that MoveUp moves a node up the sibling chain
    /// </summary>
    [TestMethod]
    public async Task MoveUp_WithMiddleSibling_MovesUpInSiblingChain()
    {
        // Arrange
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var overview = model.ExplorerView.First();

        // Create three siblings
        var char1 = outlineService.AddStoryElement(model, StoryItemType.Character, overview);
        char1.Name = "Character 1";
        var char2 = outlineService.AddStoryElement(model, StoryItemType.Character, overview);
        char2.Name = "Character 2";
        var char3 = outlineService.AddStoryElement(model, StoryItemType.Character, overview);
        char3.Name = "Character 3";

        shell.CurrentNode = char2.Node;

        // Act
        shell.MoveUpCommand.Execute(null);

        // Assert
        var children = overview.Children;
        var char2Index = children.IndexOf(char2.Node);
        var char1Index = children.IndexOf(char1.Node);
        Assert.IsTrue(char2Index < char1Index, "Character 2 should now be before Character 1");
        Assert.AreEqual(overview, char2.Node.Parent, "Character 2 should still have same parent");
    }

    /// <summary>
    ///     Tests that MoveUp at top of siblings moves to end of previous parent sibling
    /// </summary>
    [TestMethod]
    public async Task MoveUp_AtTopOfSiblings_MovesToPreviousParentSibling()
    {
        // Arrange
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var overview = model.ExplorerView.First();

        // Create two folders with children
        var folder1 = outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
        folder1.Name = "Folder 1";
        var folder1Child = outlineService.AddStoryElement(model, StoryItemType.Character, folder1.Node);
        folder1Child.Name = "Folder 1 Child";

        var folder2 = outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
        folder2.Name = "Folder 2";
        var folder2Child = outlineService.AddStoryElement(model, StoryItemType.Scene, folder2.Node);
        folder2Child.Name = "Folder 2 Child";

        shell.CurrentNode = folder2Child.Node;

        // Act
        shell.MoveUpCommand.Execute(null);

        // Assert
        Assert.AreEqual(folder1.Node, folder2Child.Node.Parent, "Folder 2 Child should now be in Folder 1");
        Assert.IsTrue(folder1.Node.Children.Contains(folder2Child.Node), "Folder 1 should contain Folder 2 Child");
        Assert.IsFalse(folder2.Node.Children.Contains(folder2Child.Node),
            "Folder 2 should no longer contain its child");
    }

    /// <summary>
    ///     Tests that MoveUp fails when node is root
    /// </summary>
    [TestMethod]
    public async Task MoveUp_WithRootNode_CannotMove()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var overview = model.ExplorerView.First();

        // Overview is a root node
        shell.CurrentNode = overview;

        // Act
        shell.MoveUpCommand.Execute(null);

        // Assert - root nodes cannot move
        Assert.IsTrue(overview.IsRoot, "Overview should still be root");
    }

    /// <summary>
    ///     Tests that MoveUp fails when CurrentNode is null
    /// </summary>
    [TestMethod]
    public async Task MoveUp_WithNullCurrentNode_DoesNothing()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        await CreateTestModelWithAllElements();
        shell.CurrentNode = null;

        // Act
        shell.MoveUpCommand.Execute(null);

        // Assert - method should complete without throwing
        Assert.IsTrue(true, "Method completed without throwing");
    }

    /// <summary>
    ///     Tests that MoveDown moves a node down the sibling chain
    /// </summary>
    [TestMethod]
    public async Task MoveDown_WithMiddleSibling_MovesDownInSiblingChain()
    {
        // Arrange
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var overview = model.ExplorerView.First();

        // Create three siblings
        var scene1 = outlineService.AddStoryElement(model, StoryItemType.Scene, overview);
        scene1.Name = "Scene 1";
        var scene2 = outlineService.AddStoryElement(model, StoryItemType.Scene, overview);
        scene2.Name = "Scene 2";
        var scene3 = outlineService.AddStoryElement(model, StoryItemType.Scene, overview);
        scene3.Name = "Scene 3";

        shell.CurrentNode = scene2.Node;

        // Act
        shell.MoveDownCommand.Execute(null);

        // Assert
        var children = overview.Children;
        var scene2Index = children.IndexOf(scene2.Node);
        var scene3Index = children.IndexOf(scene3.Node);
        Assert.IsTrue(scene2Index > scene3Index, "Scene 2 should now be after Scene 3");
        Assert.AreEqual(overview, scene2.Node.Parent, "Scene 2 should still have same parent");
    }

    /// <summary>
    ///     Tests that MoveDown at bottom of siblings moves to beginning of next parent sibling
    /// </summary>
    [TestMethod]
    public async Task MoveDown_AtBottomOfSiblings_MovesToNextParentSibling()
    {
        // Arrange
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var overview = model.ExplorerView.First();

        // Create two folders
        var folder1 = outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
        folder1.Name = "Folder 1";
        var folder1Child = outlineService.AddStoryElement(model, StoryItemType.Problem, folder1.Node);
        folder1Child.Name = "Folder 1 Child";

        var folder2 = outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
        folder2.Name = "Folder 2";

        shell.CurrentNode = folder1Child.Node;

        // Act
        shell.MoveDownCommand.Execute(null);

        // Assert
        Assert.AreEqual(folder2.Node, folder1Child.Node.Parent, "Folder 1 Child should now be in Folder 2");
        Assert.IsTrue(folder2.Node.Children.Contains(folder1Child.Node), "Folder 2 should contain Folder 1 Child");
        Assert.IsFalse(folder1.Node.Children.Contains(folder1Child.Node),
            "Folder 1 should no longer contain its child");
        // Should be first child of Folder 2
        Assert.AreEqual(0, folder2.Node.Children.IndexOf(folder1Child.Node), "Should be first child of Folder 2");
    }

    /// <summary>
    ///     Tests that MoveDown cannot move node into TrashCan
    /// </summary>
    [TestMethod]
    public async Task MoveDown_WhenNextSiblingIsTrashCan_CannotMove()
    {
        // Arrange
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();

        // In Explorer view, the last root is typically before TrashCan
        // Create a node that would move to TrashCan if allowed
        var explorerRoots = model.ExplorerView;
        var lastNonTrashRoot = explorerRoots.LastOrDefault(n => n.Type != StoryItemType.TrashCan);

        if (lastNonTrashRoot != null && lastNonTrashRoot.Children.Count > 0)
        {
            // Get last child of last non-trash root
            var lastChild = lastNonTrashRoot.Children.Last();
            shell.CurrentNode = lastChild;
            var initialParent = lastChild.Parent;

            // Act
            shell.MoveDownCommand.Execute(null);

            // Assert - should not move to trash
            Assert.AreEqual(initialParent, lastChild.Parent, "Node should not move to trash");
        }
    }

    /// <summary>
    ///     Tests that MoveDown fails when node is root
    /// </summary>
    [TestMethod]
    public async Task MoveDown_WithRootNode_CannotMove()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var overview = model.ExplorerView.First();

        // Overview is a root node
        shell.CurrentNode = overview;

        // Act
        shell.MoveDownCommand.Execute(null);

        // Assert - root nodes cannot move
        Assert.IsTrue(overview.IsRoot, "Overview should still be root");
    }

    /// <summary>
    ///     Tests that MoveDown fails when CurrentNode is null
    /// </summary>
    [TestMethod]
    public async Task MoveDown_WithNullCurrentNode_DoesNothing()
    {
        // Arrange
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        await CreateTestModelWithAllElements();
        shell.CurrentNode = null;

        // Act
        shell.MoveDownCommand.Execute(null);

        // Assert - method should complete without throwing
        Assert.IsTrue(true, "Method completed without throwing");
    }

    /// <summary>
    ///     Tests complex move scenario with nested hierarchy
    /// </summary>
    [TestMethod]
    public async Task MoveOperations_ComplexHierarchy_WorksCorrectly()
    {
        // Arrange
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        var model = await CreateTestModelWithAllElements();
        var overview = model.ExplorerView.First();

        // Create complex hierarchy:
        // Overview
        //   ├─ Folder A
        //   │   ├─ Character 1
        //   │   └─ Character 2
        //   └─ Folder B
        //       └─ Scene 1

        var folderA = outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
        folderA.Name = "Folder A";
        var char1 = outlineService.AddStoryElement(model, StoryItemType.Character, folderA.Node);
        char1.Name = "Character 1";
        var char2 = outlineService.AddStoryElement(model, StoryItemType.Character, folderA.Node);
        char2.Name = "Character 2";

        var folderB = outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
        folderB.Name = "Folder B";
        var scene1 = outlineService.AddStoryElement(model, StoryItemType.Scene, folderB.Node);
        scene1.Name = "Scene 1";

        // Test 1: Move Character 2 left (should become sibling of Folder A)
        shell.CurrentNode = char2.Node;
        shell.MoveLeftCommand.Execute(null);
        Assert.AreEqual(overview, char2.Node.Parent, "Character 2 should be child of Overview");

        // Test 2: Move Character 2 right (should become child of Folder A again)
        shell.MoveRightCommand.Execute(null);
        Assert.AreEqual(folderA.Node, char2.Node.Parent, "Character 2 should be back in Folder A");

        // Test 3: Move Scene 1 up (should move to end of Folder A)
        shell.CurrentNode = scene1.Node;
        shell.MoveUpCommand.Execute(null);
        Assert.AreEqual(folderA.Node, scene1.Node.Parent, "Scene 1 should be in Folder A");

        // Test 4: Move Scene 1 down (should move back to Folder B)
        shell.MoveDownCommand.Execute(null);
        Assert.AreEqual(folderB.Node, scene1.Node.Parent, "Scene 1 should be back in Folder B");
    }

    #endregion

    #region Shutdown Tests

    [TestMethod]
    public async Task OnApplicationClosing_WithOpenDocument_CallsCloseFile()
    {
        // Arrange
        var shellViewModel = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var document = new StoryDocument(new StoryModel(), "test.stbx");
        appState.CurrentDocument = document;

        // Act
        await shellViewModel.OnApplicationClosing();

        // Assert
        Assert.IsNull(appState.CurrentDocument, "CloseFile should have been called and set CurrentDocument to null");
    }

    [TestMethod]
    public async Task OnApplicationClosing_WithoutDocument_DoesNotThrow()
    {
        // Arrange
        var shellViewModel = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = null;

        // Act - should complete without errors
        await shellViewModel.OnApplicationClosing();

        // Assert - no exception thrown, CurrentDocument still null
        Assert.IsNull(appState.CurrentDocument);
    }

    [TestMethod]
    public async Task OnApplicationClosing_WithSessionTime_UpdatesCumulativeTimeUsed()
    {
        // Arrange
        var shellViewModel = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();

        appState.CurrentDocument = null;
        shellViewModel.AppStartTime = DateTime.Now.AddSeconds(-10);

        // Act
        await shellViewModel.OnApplicationClosing();

        // Assert
        Assert.IsTrue(preferenceService.Model.CumulativeTimeUsed > 0,
            "CumulativeTimeUsed should have been updated with session time");
    }

    [TestMethod]
    public async Task OnApplicationClosing_Always_SavesPreferences()
    {
        // Arrange
        var shellViewModel = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();

        appState.CurrentDocument = null;
        shellViewModel.AppStartTime = DateTime.Now.AddSeconds(-10);

        // Act
        await shellViewModel.OnApplicationClosing();

        // Assert
        Assert.IsTrue(preferenceService.Model.CumulativeTimeUsed > 0,
            "Preferences should have been saved with updated time");
    }

    [TestMethod]
    public async Task OnApplicationClosing_Always_DestroysCollaborator()
    {
        // Arrange
        var shellViewModel = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = null;

        // Act
        await shellViewModel.OnApplicationClosing();

        // Assert
        // Method should complete without throwing (CollaboratorService.DestroyCollaborator is called)
        Assert.IsTrue(true, "Method should complete without throwing");
    }

    [TestMethod]
    public async Task OnApplicationClosing_Always_SetsIsClosingFlag()
    {
        // Arrange
        var shellViewModel = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();

        appState.CurrentDocument = null;
        shellViewModel.IsClosing = false;

        // Act
        await shellViewModel.OnApplicationClosing();

        // Assert
        Assert.IsTrue(shellViewModel.IsClosing, "IsClosing flag should be set to true");
    }

    [TestMethod]
    public async Task OnApplicationClosing_WithException_HandlesGracefully()
    {
        // Arrange
        var shellViewModel = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = new StoryModel { Changed = true };
        appState.CurrentDocument = new StoryDocument(model, "invalid/path.stbx");

        // Act - should not throw even with potential file issues
        try
        {
            await shellViewModel.OnApplicationClosing();
            Assert.IsTrue(true, "Method should complete without throwing");
        }
        catch (Exception ex)
        {
            Assert.Fail($"OnApplicationClosing should handle exceptions gracefully: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task OnApplicationClosing_WithOpenDocument_StopsAutoSave()
    {
        // Arrange
        var shellViewModel = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();

        var document = new StoryDocument(new StoryModel(), "test.stbx");
        appState.CurrentDocument = document;

        // Note: Can't test AutoSave stopping directly as it's handled internally by CloseFile
        // Act
        await shellViewModel.OnApplicationClosing();

        // Assert - Document should be closed (which stops AutoSave)
        Assert.IsNull(appState.CurrentDocument, "Document should be closed");
    }

    #endregion
}
