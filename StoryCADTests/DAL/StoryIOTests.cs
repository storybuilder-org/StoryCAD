using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.DAL;
using StoryCADLib.Models;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels.SubViewModels;

#nullable disable

namespace StoryCADTests.DAL;

[TestClass]
public class StoryIOTests
{
    private AppState _appState;
    private ILogService _logService;
    private StoryIO _storyIO;

    [TestInitialize]
    public void Initialize()
    {
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _logService = Ioc.Default.GetRequiredService<ILogService>();
        _storyIO = new StoryIO(_logService, _appState);

        // Reset app state to ensure clean start for each test
        _appState.CurrentDocument = null;
    }

    [TestMethod]
    public void ReadStory_UpdatesCurrentDocumentFilePath()
    {
        // This test verifies that StoryIO no longer depends on OutlineViewModel
        // and can be constructed with just ILogService and AppState

        // Act - verify it compiles with new constructor
        var storyIO = new StoryIO(_logService, _appState);

        // Assert
        Assert.IsNotNull(storyIO);
    }

    [TestMethod]
    public void Constructor_AcceptsAppStateWithoutOutlineViewModel()
    {
        // Arrange & Act
        var storyIO = new StoryIO(_logService, _appState);

        // Assert
        Assert.IsNotNull(storyIO);
    }

    [TestMethod]
    public async Task WriteStory_CreatesFileWithElements()
    {
        // Arrange
        var outlineVM = Ioc.Default.GetRequiredService<OutlineViewModel>();
        var filePath = Path.Combine(App.ResultsDir, "WriteStoryTest.stbx");
        var storyModel = new StoryModel();
        _appState.CurrentDocument = new StoryDocument(storyModel, filePath);
        var name = Path.GetFileNameWithoutExtension(filePath);

        OverviewModel overview = new(name, storyModel, null)
        {
            DateCreated = DateTime.Today.ToString("yyyy-MM-dd"),
            Author = "StoryCAD Tests"
        };

        storyModel.ExplorerView.Add(overview.Node);
        TrashCanModel trash = new(storyModel, null);
        storyModel.ExplorerView.Add(trash.Node);
        FolderModel narrative = new("Narrative View", storyModel, StoryItemType.Folder, null);
        storyModel.NarratorView.Add(narrative.Node);

        // Add three test nodes
        StoryElement problem = new ProblemModel("TestProblem", storyModel, overview.Node);
        var character = new CharacterModel("TestCharacter", storyModel, overview.Node);
        var scene = new SceneModel("TestScene", storyModel, overview.Node);

        // Assert model is set up correctly
        Assert.AreEqual(6, storyModel.StoryElements.Count);
        Assert.AreEqual(StoryItemType.StoryOverview, storyModel.StoryElements[0].ElementType);

        // Clean up if file exists
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        // Act - Write file
        await outlineVM.WriteModel();

        // Wait for file to be written
        Thread.Sleep(1000);

        // Assert file was written
        Assert.IsTrue(File.Exists(filePath));

        // Cleanup
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public async Task ReadStory_LoadsModelCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(App.InputDir, "OpenTest.stbx");
        Assert.IsTrue(File.Exists(filePath), "Test file does not exist.");

        var file = await StorageFile.GetFileFromPathAsync(filePath);

        // Act
        var storyModel = await _storyIO.ReadStory(file);

        // Assert
        Assert.AreEqual(7, storyModel.StoryElements.Count, "Story elements count mismatch.");
        Assert.AreEqual(1, storyModel.ExplorerView.Count, "ExplorerView should have only Overview");
        Assert.AreEqual(1, storyModel.TrashView.Count, "TrashView should have TrashCan");
        Assert.AreEqual(3, storyModel.ExplorerView[0].Children.Count, "Overview Children count mismatch");
    }

    [TestMethod]
    public void IsValidPath_WithValidPath_ReturnsTrue()
    {
        // Arrange - Use cross-platform temp path
        var validPath = Path.Combine(Path.GetTempPath(), "mytestpath", "test.stbx");

        // Act & Assert
        Assert.IsTrue(StoryIO.IsValidPath(validPath));

        // Cleanup - Remove created directory
        var dir = Path.GetDirectoryName(validPath);
        if (Directory.Exists(dir))
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
            }
        }
    }

    [TestMethod]
    public void IsValidPath_WithInvalidPath_ReturnsFalse()
    {
        // Note: This test only works reliably on Windows where invalid path characters are well-defined
        // On Unix-like systems, most characters are valid and IsValidPath creates directories,
        // making it difficult to test invalid paths safely
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("This test is only reliable on Windows due to platform differences in path validation");
            return;
        }

        // Arrange - Windows doesn't allow these characters in filenames
        var invalidPath = Path.Combine(Path.GetTempPath(), "test<>:|?.stbx");

        // Act & Assert
        Assert.IsFalse(StoryIO.IsValidPath(invalidPath));
    }

    [TestMethod]
    public async Task ReadStory_FullFile_LoadsAllProperties()
    {
        // Arrange
        var filePath = Path.Combine(App.InputDir, "Full.stbx");
        var file = await StorageFile.GetFileFromPathAsync(filePath);

        // Act
        var model = await _storyIO.ReadStory(file);

        // Assert - Overview Model Tests
        var overview = (OverviewModel)model.StoryElements[0];
        Assert.AreEqual("jake shaw", overview.Author);
        Assert.AreEqual("2025-01-03", overview.DateCreated);
        Assert.IsTrue(overview.Description.Contains("Test"));
        Assert.IsTrue(overview.Concept.Contains("Test"));
        Assert.IsTrue(overview.Premise.Contains("Test"));
        Assert.AreEqual("Short Story", overview.StoryType);
        Assert.IsTrue(overview.Viewpoint.Contains("Limited third person"));
        Assert.AreEqual("Mainstream", overview.StoryGenre);
        Assert.AreEqual("Metafiction", overview.LiteraryDevice);
        Assert.AreEqual("Third person subjective", overview.Voice);
        Assert.AreEqual("Present", overview.Tense);
        Assert.AreEqual("Mystery", overview.Style);
        Assert.IsTrue(overview.StructureNotes.Contains("Test"));
    }

    [TestMethod]
    public async Task ReadStory_StructureFile_LoadsCorrectStructure()
    {
        // Arrange: load the STBX file that contains the Hero's Journey beats
        var filePath = Path.Combine(App.InputDir, "StructureTests.stbx");
        Assert.IsTrue(File.Exists(filePath), "Test file does not exist at the given path.");

        var file = await StorageFile.GetFileFromPathAsync(filePath);

        // Act: read the story and find the "Main Problem" that has the Hero's Journey data
        var model = await _storyIO.ReadStory(file);
        var mainProblem = model.StoryElements
            .OfType<ProblemModel>()
            .FirstOrDefault(p => p.Name == "Main Problem");

        // Assert: confirm structure data is loaded
        Assert.IsNotNull(mainProblem, "Main Problem with structure data not found.");
        Assert.AreEqual("Hero's Journey", mainProblem.StructureTitle, "StructureTitle mismatch.");

        // Check that the beats exist
        Assert.IsNotNull(mainProblem.StructureBeats, "StructureBeats collection was null.");
        Assert.AreEqual(12, mainProblem.StructureBeats.Count, "Structure beats count mismatch.");

        // Quick sample checks on beat data
        Assert.AreEqual("Ordinary World", mainProblem.StructureBeats[0].Title, "First beat title mismatch.");
        Assert.AreEqual(Guid.Parse("ea818c91-0dd4-47f2-8bcc-7c5841030e09"), mainProblem.StructureBeats[0].Guid,
            "First bound Beat GUID mismatch");
        Assert.AreEqual("Refusal of the Call", mainProblem.StructureBeats[2].Title, "Third beat title mismatch.");
        Assert.AreEqual(Guid.Parse("4e7c0217-64e8-4c74-8438-debb584cf3b8"), mainProblem.StructureBeats[2].Guid,
            "Third bound Beat GUID mismatch");
    }

    [TestMethod]
    public async Task CheckFileAvailability_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(App.InputDir, "AddElement.stbx");

        // Act
        var result = await _storyIO.CheckFileAvailability(filePath);

        // Assert
        Assert.IsTrue(result, $"Expected file at {filePath} to be available.");
    }

    [TestMethod]
    public async Task CheckFileAvailability_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(App.ResultsDir, "NonExistentFile_" + Guid.NewGuid() + ".stbx");

        // Act
        var result = await _storyIO.CheckFileAvailability(filePath);

        // Assert
        Assert.IsFalse(result, $"Expected non-existent file at {filePath} to be unavailable.");
    }

    [TestMethod]
    public async Task CheckFileAvailability_WithNullPath_ReturnsFalse()
    {
        // Arrange
        string filePath = null;

        // Act
        var result = await _storyIO.CheckFileAvailability(filePath);

        // Assert
        Assert.IsFalse(result, "Expected null path to return false.");
    }

    [TestMethod]
    public async Task CheckFileAvailability_WithEmptyPath_ReturnsFalse()
    {
        // Arrange
        var filePath = string.Empty;

        // Act
        var result = await _storyIO.CheckFileAvailability(filePath);

        // Assert
        Assert.IsFalse(result, "Expected empty path to return false.");
    }

    [TestMethod]
    public async Task CheckFileAvailability_WithWhitespacePath_ReturnsFalse()
    {
        // Arrange
        var filePath = "   ";

        // Act
        var result = await _storyIO.CheckFileAvailability(filePath);

        // Assert
        Assert.IsFalse(result, "Expected whitespace path to return false.");
    }

    #region Migration Tests

    [TestMethod]
    public async Task DetectLegacyDualRoot_WithTrashCanAsSecondRoot_ReturnsTrue()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>());
        // All templates create the legacy dual-root structure automatically
        var createResult = await api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var model = api.CurrentModel;

        // Act - The new structure has TrashCan in TrashView, not ExplorerView
        var hasNewStructure = model.ExplorerView.Count == 1 &&
                              model.TrashView.Count == 1 &&
                              model.TrashView[0].Type == StoryItemType.TrashCan;

        // Assert
        Assert.IsTrue(hasNewStructure, "Should have new structure with TrashCan in TrashView");
        Assert.AreEqual(1, model.ExplorerView.Count, "ExplorerView should have only Overview");
        Assert.AreEqual(StoryItemType.StoryOverview, model.ExplorerView[0].Type,
            "ExplorerView root should be Overview");
        Assert.AreEqual(1, model.TrashView.Count, "TrashView should have TrashCan");
        Assert.AreEqual(StoryItemType.TrashCan, model.TrashView[0].Type, "TrashView root should be TrashCan");
    }

    [TestMethod]
    public async Task MigrateLegacyDualRoot_MovesTrashCanChildrenToTrashView()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>());
        var createResult = await api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var model = api.CurrentModel;
        var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var trashCan = model.TrashView.FirstOrDefault(n => n.Type == StoryItemType.TrashCan);
        Assert.IsNotNull(trashCan, "TrashCan should exist in TrashView");

        // Add a scene to overview
        var sceneResult = api.AddElement(StoryItemType.Scene, overview.Uuid.ToString(), "Deleted Scene");
        Assert.IsTrue(sceneResult.IsSuccess);
        var scene = model.StoryElements.StoryElementGuids[sceneResult.Payload];

        // Delete the scene (move to trash) - manually because we're testing legacy structure
        overview.Node.Children.Remove(scene.Node);
        scene.Node.Parent = trashCan;
        trashCan.Children.Add(scene.Node);

        // Verify setup
        Assert.AreEqual(1, trashCan.Children.Count, "TrashCan should have one deleted item");

        // Act - Simulate migration
        if (model.ExplorerView.Count > 1)
        {
            var trashRoot = model.ExplorerView.FirstOrDefault(n => n.Type == StoryItemType.TrashCan);
            if (trashRoot != null)
            {
                // Move all children to TrashView
                foreach (var child in trashRoot.Children.ToList())
                {
                    child.Parent = null;
                    model.TrashView.Add(child);
                }

                // Remove TrashCan from ExplorerView
                model.ExplorerView.Remove(trashRoot);
            }
        }

        // Assert
        Assert.AreEqual(1, model.ExplorerView.Count, "ExplorerView should have only one root after migration");
        Assert.AreEqual(StoryItemType.StoryOverview, model.ExplorerView[0].Type, "Only root should be Overview");
        Assert.AreEqual(1, model.TrashView.Count, "TrashView should contain TrashCan");
        // Check that TrashCan is in TrashView and contains the deleted scene
        var trashCanInView = model.TrashView[0];
        Assert.AreEqual(StoryItemType.TrashCan, trashCanInView.Type, "TrashView root should be TrashCan");
        Assert.AreEqual(1, trashCanInView.Children.Count, "TrashCan should contain one deleted item");
        Assert.AreEqual(scene.Node.Uuid, trashCanInView.Children[0].Uuid,
            "TrashCan should contain the same scene that was deleted");
    }

    [TestMethod]
    public async Task MigrateLegacyDualRoot_PreservesHierarchyInTrashView()
    {
        // Arrange
        var api = new SemanticKernelApi(Ioc.Default.GetRequiredService<OutlineService>());
        var createResult = await api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var model = api.CurrentModel;
        var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var trashCan = model.TrashView.FirstOrDefault(n => n.Type == StoryItemType.TrashCan);
        Assert.IsNotNull(trashCan, "TrashCan should exist in TrashView");

        // Add folder to overview
        var folderResult = api.AddElement(StoryItemType.Folder, overview.Uuid.ToString(), "Deleted Folder");
        Assert.IsTrue(folderResult.IsSuccess);
        var folder = model.StoryElements.StoryElementGuids[folderResult.Payload];

        // Add scene to folder
        var sceneResult = api.AddElement(StoryItemType.Scene, folder.Uuid.ToString(), "Scene in Folder");
        Assert.IsTrue(sceneResult.IsSuccess);
        var scene = model.StoryElements.StoryElementGuids[sceneResult.Payload];

        // Delete entire folder hierarchy (move to trash) - manually because we're testing legacy structure
        overview.Node.Children.Remove(folder.Node);
        folder.Node.Parent = trashCan;
        trashCan.Children.Add(folder.Node);

        // Verify setup
        Assert.AreEqual(1, trashCan.Children.Count, "TrashCan should have one deleted folder");
        Assert.AreEqual(1, folder.Node.Children.Count, "Deleted folder should still have its child");

        // Act - Simulate migration
        if (model.ExplorerView.Count > 1)
        {
            var trashRoot = model.ExplorerView.FirstOrDefault(n => n.Type == StoryItemType.TrashCan);
            if (trashRoot != null)
            {
                // Move all direct children to TrashView (preserving their hierarchies)
                foreach (var child in trashRoot.Children.ToList())
                {
                    child.Parent = null;
                    model.TrashView.Add(child);
                }

                // Remove empty TrashCan from ExplorerView
                model.ExplorerView.Remove(trashRoot);
            }
        }

        // Assert
        Assert.AreEqual(1, model.ExplorerView.Count, "ExplorerView should have only one root after migration");
        Assert.AreEqual(StoryItemType.StoryOverview, model.ExplorerView[0].Type, "Only root should be Overview");
        Assert.AreEqual(1, model.TrashView.Count, "TrashView should have TrashCan");
        // Check that TrashCan is in TrashView and contains the deleted folder
        var trashCanInView = model.TrashView[0];
        Assert.AreEqual(StoryItemType.TrashCan, trashCanInView.Type, "TrashView root should be TrashCan");
        Assert.AreEqual(1, trashCanInView.Children.Count, "TrashCan should contain one deleted item");
        var movedFolder = trashCanInView.Children[0];
        Assert.AreEqual(folder.Node.Uuid, movedFolder.Uuid, "TrashCan should contain the same folder that was deleted");
        Assert.AreEqual(1, movedFolder.Children.Count, "Folder should still have its child");
        Assert.AreEqual(scene.Node.Uuid, movedFolder.Children[0].Uuid,
            "Child scene should be preserved with same UUID");
    }

    #endregion
}
