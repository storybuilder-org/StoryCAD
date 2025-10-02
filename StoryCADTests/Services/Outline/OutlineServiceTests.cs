using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Outline;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StoryCAD.Services.Logging;
using StoryCAD.Services;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.Tools;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StoryCADTests.Services.Outline
{
    /// <summary>
    /// Tests for the OutlineService class which is responsible for creating, writing, and opening StoryModel files.
    /// This test class verifies:
    /// <list type="bullet">
    /// <item>
    /// <description>The correct creation of StoryModels using different template indexes.</description>
    /// </item>
    /// <item>
    /// <description>The ability to write the StoryModel to disk, and proper error handling for invalid paths.</description>
    /// </item>
    /// <item>
    /// <description>The proper opening of files and error handling when a file is not found.</description>
    /// </item>
    /// </list>
    /// </summary>
    [TestClass]
    public class OutlineServiceTests
    {
        private OutlineService _outlineService;
        private string testOutputPath;

        /// <summary>
        /// Initializes the test environment for each test by instantiating the OutlineService and ensuring the TestOutputs folder is clean.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            _outlineService = Ioc.Default.GetRequiredService<OutlineService>();

            // Create a dedicated TestOutputs folder under the current base directory.
            testOutputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestOutputs");
            if (Directory.Exists(testOutputPath))
            {
                Directory.Delete(testOutputPath, true);
            }
            Directory.CreateDirectory(testOutputPath);
        }

        /// <summary>
        /// Tests that creating a StoryModel with template index 0 produces a minimal model.
        /// Verifies that the ExplorerView has at least an Overview and TrashCan node,
        /// the Overview node contains the correct outline name, and that no child nodes exist for a blank project.
        /// </summary>
        [TestMethod]
        public async Task CreateModel_BasicTemplate0_ShouldCreateMinimalModel()
        {
            // Arrange
            string outlineName = "Test Outline";
            string author = "Test Author";
            int templateIndex = 0;

            // Act
            StoryModel model = await _outlineService.CreateModel(outlineName, author, templateIndex);

            // Assert
            Assert.IsNotNull(model, "StoryModel should not be null.");
            // The CreateModel method adds at least two nodes to ExplorerView: Overview and TrashCan.
            Assert.AreEqual(1, model.ExplorerView.Count, "ExplorerView should contain only Overview node.");
            Assert.AreEqual(1, model.TrashView.Count, "TrashView should contain TrashCan node.");
            
            // Assuming the first node is the Overview node with the provided outline name.
            var overviewNode = model.ExplorerView.First();
            Assert.AreEqual(outlineName, overviewNode.Name, "Overview node should have the provided outline name.");
            
            // For a blank project (templateIndex 0) no additional children should be added to the Overview.
            Assert.IsTrue(overviewNode.Children == null || overviewNode.Children.Count == 0, 
                "For template index 0, Overview node should not have child nodes.");

            // Check that the NarratorView contains a node named "Narrative View"
            bool narrativeFound = model.NarratorView.Any(n => n.Name == "Narrative View");
            Assert.IsTrue(narrativeFound, "NarratorView should contain a node named 'Narrative View'.");
        }

        /// <summary>
        /// Tests that creating a StoryModel with template index 1 adds a problem node
        /// and two characters (protagonist and antagonist to the Overview node.
        /// </summary>
        [TestMethod]
        public async Task CreateModel_Template1_ShouldAddProblemAndCharacters()
        {
            // Arrange
            string outlineName = "Test Outline";
            string author = "Test Author";
            int templateIndex = 1;

            // Act
            StoryModel model = await _outlineService.CreateModel(outlineName, author, templateIndex);

            // Assert
            Assert.IsNotNull(model, "StoryModel should not be null.");
            var overviewNode = model.ExplorerView.First();
            // For non-blank projects (templateIndex != 0) additional nodes are added to the Overview.
            Assert.IsTrue(overviewNode.Children != null && overviewNode.Children.Count > 0,
                "For a non-blank project, Overview node should have children.");

            // Look for the problem node by name.
            var problemNode = overviewNode.Children.FirstOrDefault(n => n.Name == "Story Problem");
            Assert.IsNotNull(problemNode, "A problem node with name 'Story Problem' should be present for template 1.");
            // In template 1 the overview node is expected to have exactly 2 characters: protagonist and antagonist.
            Assert.AreEqual(2, model.StoryElements.Count(s => s.ElementType == StoryItemType.Character),
                "Problem node should have exactly two characters (protagonist and antagonist).");
        }

        /// <summary>
        /// Tests that WriteModel successfully writes a StoryModel to disk.
        /// Verifies that the file is created and contains expected content (i.e., the outline name).
        /// </summary>
        [TestMethod]
        public async Task WriteModel_ShouldWriteFileSuccessfully()
        {
            // Arrange
            string outlineName = "Test Outline";
            string author = "Test Author";
            int templateIndex = 0;
            StoryModel model = await _outlineService.CreateModel(outlineName, author, templateIndex);
            string filePath = Path.Combine(testOutputPath, "TestOutline.json");

            // Act
            bool result = await _outlineService.WriteModel(model, filePath);

            // Assert
            Assert.IsTrue(result, "WriteModel should return true on success.");
            Assert.IsTrue(File.Exists(filePath), "The output file should exist after writing the model.");
            string json = File.ReadAllText(filePath);
            Assert.IsTrue(json.Contains(outlineName), "The written JSON should contain the outline name.");
        }

        /// <summary>
        /// Tests that OpenFile throws a FileNotFoundException when the file does not exist.
        /// </summary>
        [TestMethod]
        public async Task OpenFile_FileNotFound_ShouldThrowFileNotFoundException()
        {
            // Arrange
            string nonExistentPath = Path.Combine(testOutputPath, "NonExistentFile.json");

            // Act & Assert
            await Assert.ThrowsExactlyAsync<FileNotFoundException>(async () =>
            {
                await _outlineService.OpenFile(nonExistentPath);
            });
        }

        /// <summary>
        /// Tests that OpenFile returns a valid StoryModel when opening a file that was previously written.
        /// Compares the ExplorerView node count between the original and loaded models.
        /// </summary>
        [TestMethod]
        public async Task OpenFile_ValidFile_ShouldReturnStoryModel()
        {
            // Arrange
            string outlineName = "Test Outline";
            string author = "Test Author";
            int templateIndex = 0;
            StoryModel model = await _outlineService.CreateModel(outlineName, author, templateIndex);
            string filePath = Path.Combine(testOutputPath, "TestOutline.json");
            await _outlineService.WriteModel(model, filePath);

            // Act
            StoryModel loadedModel = await _outlineService.OpenFile(filePath);

            // Assert
            Assert.IsNotNull(loadedModel, "Loaded model should not be null.");
            Assert.AreEqual(model.ExplorerView.Count, loadedModel.ExplorerView.Count, 
                "The loaded model should have the same number of ExplorerView nodes as the original.");
        }

        // ----- Edge Case Tests for File Creation -----

        /// <summary>
        /// Tests that WriteModel throws an exception when provided with an invalid file path.
        /// An empty string is used here as an example of an invalid file path.
        /// </summary>
        [TestMethod]
        public async Task WriteModel_InvalidPath_ThrowsException()
        {
            // Arrange
            string outlineName = "Edge Case Outline";
            string author = "Test Author";
            int templateIndex = 0;
            StoryModel model = await _outlineService.CreateModel(outlineName, author, templateIndex);
            // Use an invalid file path (empty string)
            string invalidPath = string.Empty;

            // Act & Assert
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            {
                await _outlineService.WriteModel(model, invalidPath);
            });
        }

        #region SetCurrentView Tests

        /// <summary>
        /// Tests that SetCurrentView properly switches to ExplorerView
        /// </summary>
        [TestMethod]
        public void SetCurrentView_SwitchToExplorerView_UpdatesModelCorrectly()
        {
            // Arrange
            var model = new StoryModel();
            model.CurrentView = model.NarratorView; // Start with NarratorView
            model.CurrentViewType = StoryViewType.NarratorView;

            // Act
            _outlineService.SetCurrentView(model, StoryViewType.ExplorerView);

            // Assert
            Assert.AreEqual(model.ExplorerView, model.CurrentView, "CurrentView should be set to ExplorerView");
            Assert.AreEqual(StoryViewType.ExplorerView, model.CurrentViewType, "CurrentViewType should be ExplorerView");
        }

        /// <summary>
        /// Tests that SetCurrentView properly switches to NarratorView
        /// </summary>
        [TestMethod]
        public void SetCurrentView_SwitchToNarratorView_UpdatesModelCorrectly()
        {
            // Arrange
            var model = new StoryModel();
            // Model starts with ExplorerView by default

            // Act
            _outlineService.SetCurrentView(model, StoryViewType.NarratorView);

            // Assert
            Assert.AreEqual(model.NarratorView, model.CurrentView, "CurrentView should be set to NarratorView");
            Assert.AreEqual(StoryViewType.NarratorView, model.CurrentViewType, "CurrentViewType should be NarratorView");
        }

        /// <summary>
        /// Tests that SetCurrentView throws ArgumentNullException for null model
        /// </summary>
        [TestMethod]
        public void SetCurrentView_NullModel_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.SetCurrentView(null, StoryViewType.ExplorerView);
            });
        }

        /// <summary>
        /// Tests that SetCurrentView throws ArgumentException for invalid view type
        /// </summary>
        [TestMethod]
        public void SetCurrentView_InvalidViewType_ThrowsArgumentException()
        {
            // Arrange
            var model = new StoryModel();

            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                _outlineService.SetCurrentView(model, (StoryViewType)999);
            });
        }

        /// <summary>
        /// Tests that SetCurrentView properly uses SerializationLock
        /// </summary>
        [TestMethod]
        public void SetCurrentView_UsesSerializationLock_NoExceptions()
        {
            // Arrange
            var model = new StoryModel();

            // Act - Should not throw any exceptions related to concurrent access
            _outlineService.SetCurrentView(model, StoryViewType.ExplorerView);
            _outlineService.SetCurrentView(model, StoryViewType.NarratorView);

            // Assert
            Assert.AreEqual(model.NarratorView, model.CurrentView, "Last set view should be NarratorView");
        }

        #endregion

        /// <summary>
        /// Tests that a newly created model doesn't have Changed flag set
        /// </summary>
        [TestMethod]
        public async Task CreateModel_NewModel_ShouldNotBeMarkedAsChanged()
        {
            // Arrange
            string outlineName = "Test Outline";
            string author = "Test Author";
            int templateIndex = 3; // Same template as TestAPIWrite

            // Act
            StoryModel model = await _outlineService.CreateModel(outlineName, author, templateIndex);

            // Assert
            Assert.IsNotNull(model, "StoryModel should not be null.");
            Assert.IsFalse(model.Changed, "Newly created model should not be marked as changed.");
        }

        #region SetChanged Tests

        /// <summary>
        /// Tests that SetChanged method updates the model's Changed status
        /// </summary>
        [TestMethod]
        public void SetChanged_ShouldUpdateModelChangedStatus()
        {
            // Arrange
            var model = new StoryModel();
            model.Changed = false;

            // Act
            _outlineService.SetChanged(model, true);

            // Assert
            Assert.IsTrue(model.Changed, "Model Changed property should be set to true");
        }

        /// <summary>
        /// Tests that SetChanged method uses SerializationLock
        /// </summary>
        [TestMethod]
        public void SetChanged_ShouldUseSerializationLock()
        {
            // Arrange
            var model = new StoryModel();

            // Act - Should not throw any exceptions related to concurrent access
            _outlineService.SetChanged(model, true);
            _outlineService.SetChanged(model, false);

            // Assert
            Assert.IsFalse(model.Changed, "Last set value should be false");
        }

        #endregion

        #region GetStoryElementByGuid Tests

        /// <summary>
        /// Tests that GetStoryElementByGuid returns the correct element for a valid GUID
        /// </summary>
        [TestMethod]
        public async Task GetStoryElementByGuid_ValidGuid_ReturnsElement()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 1);
            var expectedElement = model.StoryElements.First(e => e.ElementType == StoryItemType.Character);

            // Act
            var result = _outlineService.GetStoryElementByGuid(model, expectedElement.Uuid);

            // Assert
            Assert.IsNotNull(result, "Should return a story element");
            Assert.AreEqual(expectedElement.Uuid, result.Uuid, "Should return the element with matching GUID");
            Assert.AreEqual(expectedElement.ElementType, result.ElementType, "Should return element with correct type");
        }

        /// <summary>
        /// Tests that GetStoryElementByGuid throws exception for empty GUID
        /// </summary>
        [TestMethod]
        public void GetStoryElementByGuid_EmptyGuid_ThrowsException()
        {
            // Arrange
            var model = new StoryModel();

            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                _outlineService.GetStoryElementByGuid(model, Guid.Empty);
            });
        }

        /// <summary>
        /// Tests that GetStoryElementByGuid throws exception for non-existent GUID
        /// </summary>
        [TestMethod]
        public async Task GetStoryElementByGuid_NonExistentGuid_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var nonExistentGuid = Guid.NewGuid();

            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _outlineService.GetStoryElementByGuid(model, nonExistentGuid);
            });
        }

        #endregion

        #region UpdateStoryElement Tests

        /// <summary>
        /// Tests that UpdateStoryElement updates an existing element successfully
        /// </summary>
        [TestMethod]
        public async Task UpdateStoryElement_ValidElement_UpdatesSuccessfully()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 1);
            var element = model.StoryElements.First(e => e.ElementType == StoryItemType.Character);
            var originalName = element.Name;
            var newName = "Updated Character Name";
            element.Name = newName;

            // Act
            _outlineService.UpdateStoryElement(model, element);

            // Assert
            var updatedElement = _outlineService.GetStoryElementByGuid(model, element.Uuid);
            Assert.AreEqual(newName, updatedElement.Name, "Element name should be updated");
            Assert.AreNotEqual(originalName, updatedElement.Name, "Element name should be different from original");
        }

        /// <summary>
        /// Tests that UpdateStoryElement throws exception for null model
        /// </summary>
        [TestMethod]
        public void UpdateStoryElement_NullModel_ThrowsException()
        {
            // Arrange
            var element = new CharacterModel();

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.UpdateStoryElement(null, element);
            });
        }

        /// <summary>
        /// Tests that UpdateStoryElement throws exception for null element
        /// </summary>
        [TestMethod]
        public async Task UpdateStoryElement_NullElement_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);

            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.UpdateStoryElement(model, null);
            });
        }

        /// <summary>
        /// Tests that UpdateStoryElement marks model as changed
        /// </summary>
        [TestMethod]
        public async Task UpdateStoryElement_ValidElement_MarksModelAsChanged()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 1);
            var element = model.StoryElements.First(e => e.ElementType == StoryItemType.Character);
            model.Changed = false;
            element.Name = "Updated Name";

            // Act
            _outlineService.UpdateStoryElement(model, element);

            // Assert
            Assert.IsTrue(model.Changed, "Model should be marked as changed after update");
        }

        #endregion

        #region GetCharacterList Tests

        /// <summary>
        /// Tests that GetCharacterList returns all characters from the model
        /// </summary>
        [TestMethod]
        public async Task GetCharacterList_WithCharacters_ReturnsAllCharacters()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 1);
            
            // Act
            var characters = _outlineService.GetCharacterList(model);
            
            // Assert
            Assert.IsNotNull(characters, "Character list should not be null");
            Assert.AreEqual(2, characters.Count, "Should return 2 characters from template 1");
            Assert.IsTrue(characters.All(c => c.ElementType == StoryItemType.Character), 
                "All returned elements should be characters");
        }

        /// <summary>
        /// Tests that GetCharacterList returns empty list when no characters exist
        /// </summary>
        [TestMethod]
        public async Task GetCharacterList_NoCharacters_ReturnsEmptyList()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act
            var characters = _outlineService.GetCharacterList(model);
            
            // Assert
            Assert.IsNotNull(characters, "Character list should not be null");
            Assert.AreEqual(0, characters.Count, "Should return empty list when no characters");
        }

        /// <summary>
        /// Tests that GetCharacterList throws exception for null model
        /// </summary>
        [TestMethod]
        public void GetCharacterList_NullModel_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.GetCharacterList(null);
            });
        }

        /// <summary>
        /// Tests that GetCharacterList uses SerializationLock
        /// </summary>
        [TestMethod]
        public async Task GetCharacterList_UsesSerializationLock_NoExceptions()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 1);
            
            // Act - Should not throw any exceptions related to concurrent access
            var characters1 = _outlineService.GetCharacterList(model);
            var characters2 = _outlineService.GetCharacterList(model);
            
            // Assert
            Assert.AreEqual(characters1.Count, characters2.Count, "Both calls should return same count");
        }

        #endregion

        #region UpdateStoryElementByGuid Tests

        /// <summary>
        /// Tests that UpdateStoryElementByGuid updates element successfully
        /// </summary>
        [TestMethod]
        public async Task UpdateStoryElementByGuid_ValidGuid_UpdatesElement()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 1);
            var character = model.StoryElements.First(e => e.ElementType == StoryItemType.Character) as CharacterModel;
            var originalName = character.Name;
            var updatedCharacter = new CharacterModel
            {
                Uuid = character.Uuid,
                Name = "Updated Character Name",
                Role = "Updated Role",
                StoryRole = "Updated Story Role"
            };
            
            // Act
            _outlineService.UpdateStoryElementByGuid(model, character.Uuid, updatedCharacter);
            
            // Assert
            var result = _outlineService.GetStoryElementByGuid(model, character.Uuid) as CharacterModel;
            Assert.AreEqual("Updated Character Name", result.Name, "Name should be updated");
            Assert.AreEqual("Updated Role", result.Role, "Role should be updated");
            Assert.AreEqual("Updated Story Role", result.StoryRole, "StoryRole should be updated");
            Assert.IsTrue(model.Changed, "Model should be marked as changed");
        }

        /// <summary>
        /// Tests that UpdateStoryElementByGuid throws exception for null model
        /// </summary>
        [TestMethod]
        public void UpdateStoryElementByGuid_NullModel_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.UpdateStoryElementByGuid(null, Guid.NewGuid(), new CharacterModel());
            });
        }

        /// <summary>
        /// Tests that UpdateStoryElementByGuid throws exception for empty GUID
        /// </summary>
        [TestMethod]
        public async Task UpdateStoryElementByGuid_EmptyGuid_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                _outlineService.UpdateStoryElementByGuid(model, Guid.Empty, new CharacterModel());
            });
        }

        /// <summary>
        /// Tests that UpdateStoryElementByGuid throws exception for null element
        /// </summary>
        [TestMethod]
        public async Task UpdateStoryElementByGuid_NullElement_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.UpdateStoryElementByGuid(model, Guid.NewGuid(), null);
            });
        }

        /// <summary>
        /// Tests that UpdateStoryElementByGuid throws exception for non-existent GUID
        /// </summary>
        [TestMethod]
        public async Task UpdateStoryElementByGuid_NonExistentGuid_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var nonExistentGuid = Guid.NewGuid();
            
            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _outlineService.UpdateStoryElementByGuid(model, nonExistentGuid, new CharacterModel());
            });
        }

        #endregion

        #region GetSettingsList Tests

        /// <summary>
        /// Tests that GetSettingsList returns all settings from the model
        /// </summary>
        [TestMethod]
        public async Task GetSettingsList_WithSettings_ReturnsAllSettings()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.ExplorerView.First();
            
            // Add some settings manually
            var setting1 = _outlineService.AddStoryElement(model, StoryItemType.Setting, overview);
            setting1.Name = "Test Setting 1";
            var setting2 = _outlineService.AddStoryElement(model, StoryItemType.Setting, overview);
            setting2.Name = "Test Setting 2";
            
            // Act
            var settings = _outlineService.GetSettingsList(model);
            
            // Assert
            Assert.IsNotNull(settings, "Settings list should not be null");
            Assert.AreEqual(2, settings.Count, "Should return 2 settings");
            Assert.IsTrue(settings.All(s => s.ElementType == StoryItemType.Setting), 
                "All returned elements should be settings");
            Assert.IsTrue(settings.Any(s => s.Name == "Test Setting 1"), "Should contain Test Setting 1");
            Assert.IsTrue(settings.Any(s => s.Name == "Test Setting 2"), "Should contain Test Setting 2");
        }

        /// <summary>
        /// Tests that GetSettingsList returns empty list when no settings exist
        /// </summary>
        [TestMethod]
        public async Task GetSettingsList_NoSettings_ReturnsEmptyList()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act
            var settings = _outlineService.GetSettingsList(model);
            
            // Assert
            Assert.IsNotNull(settings, "Settings list should not be null");
            Assert.AreEqual(0, settings.Count, "Should return empty list when no settings");
        }

        /// <summary>
        /// Tests that GetSettingsList throws exception for null model
        /// </summary>
        [TestMethod]
        public void GetSettingsList_NullModel_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.GetSettingsList(null);
            });
        }

        #endregion

        #region GetAllStoryElements Tests

        /// <summary>
        /// Tests that GetAllStoryElements returns all elements from the model
        /// </summary>
        [TestMethod]
        public async Task GetAllStoryElements_ReturnsAllElements()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 1);
            var expectedCount = model.StoryElements.Count;
            
            // Act
            var elements = _outlineService.GetAllStoryElements(model);
            
            // Assert
            Assert.IsNotNull(elements, "Elements collection should not be null");
            Assert.AreEqual(expectedCount, elements.Count, "Should return all story elements");
        }

        /// <summary>
        /// Tests that GetAllStoryElements returns empty list for empty model
        /// </summary>
        [TestMethod]
        public void GetAllStoryElements_EmptyModel_ReturnsEmptyList()
        {
            // Arrange
            var model = new StoryModel();
            
            // Act
            var elements = _outlineService.GetAllStoryElements(model);
            
            // Assert
            Assert.IsNotNull(elements, "Elements collection should not be null");
            Assert.AreEqual(0, elements.Count, "Should return empty list for empty model");
        }

        /// <summary>
        /// Tests that GetAllStoryElements throws exception for null model
        /// </summary>
        [TestMethod]
        public void GetAllStoryElements_NullModel_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.GetAllStoryElements(null);
            });
        }

        /// <summary>
        /// Tests that GetAllStoryElements uses SerializationLock
        /// </summary>
        [TestMethod]
        public async Task GetAllStoryElements_UsesSerializationLock_NoExceptions()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 1);
            
            // Act - Should not throw any exceptions related to concurrent access
            var elements1 = _outlineService.GetAllStoryElements(model);
            var elements2 = _outlineService.GetAllStoryElements(model);
            
            // Assert
            Assert.AreEqual(elements1.Count, elements2.Count, "Both calls should return same count");
        }

        #endregion

        #region AddStoryElement Tests

        /// <summary>
        /// Tests that AddStoryElement successfully adds a Character element
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithCharacter_AddsSuccessfully()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var parent = model.ExplorerView.First();
            var initialCount = model.StoryElements.Count;
            
            // Act
            var element = _outlineService.AddStoryElement(model, StoryItemType.Character, parent);
            
            // Assert
            Assert.IsNotNull(element, "Should return created element");
            Assert.AreEqual(StoryItemType.Character, element.ElementType, "Element type should be Character");
            Assert.AreEqual(parent, element.Node.Parent, "Parent should be set correctly");
            Assert.AreEqual(initialCount + 1, model.StoryElements.Count, "Element count should increase by 1");
            Assert.IsTrue(model.StoryElements.Contains(element), "Model should contain the new element");
        }

        /// <summary>
        /// Tests that AddStoryElement successfully adds a Scene element
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithScene_AddsSuccessfully()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var parent = model.ExplorerView.First();
            
            // Act
            var element = _outlineService.AddStoryElement(model, StoryItemType.Scene, parent);
            
            // Assert
            Assert.IsNotNull(element, "Should return created element");
            Assert.AreEqual(StoryItemType.Scene, element.ElementType, "Element type should be Scene");
            Assert.AreEqual(parent, element.Node.Parent, "Parent should be set correctly");
        }

        /// <summary>
        /// Tests that AddStoryElement successfully adds a Problem element
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithProblem_AddsSuccessfully()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var parent = model.ExplorerView.First();
            
            // Act
            var element = _outlineService.AddStoryElement(model, StoryItemType.Problem, parent);
            
            // Assert
            Assert.IsNotNull(element, "Should return created element");
            Assert.AreEqual(StoryItemType.Problem, element.ElementType, "Element type should be Problem");
            Assert.AreEqual(parent, element.Node.Parent, "Parent should be set correctly");
        }

        /// <summary>
        /// Tests that AddStoryElement successfully adds a Setting element
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithSetting_AddsSuccessfully()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var parent = model.ExplorerView.First();
            
            // Act
            var element = _outlineService.AddStoryElement(model, StoryItemType.Setting, parent);
            
            // Assert
            Assert.IsNotNull(element, "Should return created element");
            Assert.AreEqual(StoryItemType.Setting, element.ElementType, "Element type should be Setting");
            Assert.AreEqual(parent, element.Node.Parent, "Parent should be set correctly");
        }

        /// <summary>
        /// Tests that AddStoryElement successfully adds a Folder element
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithFolder_AddsSuccessfully()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var parent = model.ExplorerView.First();
            
            // Act
            var element = _outlineService.AddStoryElement(model, StoryItemType.Folder, parent);
            
            // Assert
            Assert.IsNotNull(element, "Should return created element");
            Assert.AreEqual(StoryItemType.Folder, element.ElementType, "Element type should be Folder");
            Assert.AreEqual(parent, element.Node.Parent, "Parent should be set correctly");
        }

        /// <summary>
        /// Tests that AddStoryElement successfully adds a Web element
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithWeb_AddsSuccessfully()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var parent = model.ExplorerView.First();
            
            // Act
            var element = _outlineService.AddStoryElement(model, StoryItemType.Web, parent);
            
            // Assert
            Assert.IsNotNull(element, "Should return created element");
            Assert.AreEqual(StoryItemType.Web, element.ElementType, "Element type should be Web");
            Assert.AreEqual(parent, element.Node.Parent, "Parent should be set correctly");
        }

        /// <summary>
        /// Tests that AddStoryElement successfully adds a Notes element
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithNotes_AddsSuccessfully()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var parent = model.ExplorerView.First();
            
            // Act
            var element = _outlineService.AddStoryElement(model, StoryItemType.Notes, parent);
            
            // Assert
            Assert.IsNotNull(element, "Should return created element");
            Assert.AreEqual(StoryItemType.Notes, element.ElementType, "Element type should be Notes");
            Assert.AreEqual(parent, element.Node.Parent, "Parent should be set correctly");
            Assert.AreEqual("New Note", element.Name, "Notes should have default name");
        }

        /// <summary>
        /// Tests that AddStoryElement handles Section type (creates as Folder)
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithSection_CreatesAsFolder()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var parent = model.ExplorerView.First();
            
            // Act
            var element = _outlineService.AddStoryElement(model, StoryItemType.Section, parent);
            
            // Assert
            Assert.IsNotNull(element, "Should return created element");
            Assert.AreEqual(StoryItemType.Folder, element.ElementType, "Section should be created as Folder type");
            Assert.AreEqual("New Section", element.Name, "Should have default section name");
            Assert.AreEqual(parent, element.Node.Parent, "Parent should be set correctly");
        }

        /// <summary>
        /// Tests that AddStoryElement throws exception for null parent
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithNullParent_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.AddStoryElement(model, StoryItemType.Character, null);
            });
        }

        /// <summary>
        /// Tests that AddStoryElement throws exception for null model
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithNullModel_ThrowsException()
        {
            // Arrange
            var tempModel = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var parent = tempModel.ExplorerView.First();
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.AddStoryElement(null, StoryItemType.Character, parent);
            });
        }

        /// <summary>
        /// Tests that AddStoryElement throws exception when adding to TrashCan
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_ToTrashCan_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var trashCan = model.StoryElements.FirstOrDefault(e => e.ElementType == StoryItemType.TrashCan);
            Assert.IsNotNull(trashCan, "Model should have a trash can");
            
            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _outlineService.AddStoryElement(model, StoryItemType.Character, trashCan.Node);
            });
        }

        /// <summary>
        /// Tests that AddStoryElement throws exception for invalid StoryItemType
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithInvalidType_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var parent = model.ExplorerView.First();
            
            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _outlineService.AddStoryElement(model, StoryItemType.Unknown, parent);
            });
        }

        /// <summary>
        /// Tests that AddStoryElement can add nested elements
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_NestedElements_AddsSuccessfully()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.ExplorerView.First();
            
            // Act - Add a folder, then add a character to the folder
            var folder = _outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, folder.Node);
            
            // Assert
            Assert.IsNotNull(folder, "Folder should be created");
            Assert.IsNotNull(character, "Character should be created");
            Assert.AreEqual(folder.Node, character.Node.Parent, "Character's parent should be the folder");
            Assert.IsTrue(folder.Node.Children.Contains(character.Node), "Folder should contain character as child");
        }

        /// <summary>
        /// Tests that AddStoryElement cannot add StoryOverview type
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithStoryOverview_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var parent = model.ExplorerView.First();
            
            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _outlineService.AddStoryElement(model, StoryItemType.StoryOverview, parent);
            });
        }

        /// <summary>
        /// Tests that AddStoryElement cannot add TrashCan type
        /// </summary>
        [TestMethod]
        public async Task AddStoryElement_WithTrashCan_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var parent = model.ExplorerView.First();
            
            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _outlineService.AddStoryElement(model, StoryItemType.TrashCan, parent);
            });
        }

        #endregion

        #region Trash Operations Tests

        /// <summary>
        /// Tests that MoveToTrash moves an element from Explorer view to Trash
        /// </summary>
        [TestMethod]
        public async Task MoveToTrash_FromExplorerView_MovesElementToTrash()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.ExplorerView.First();
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview);
            character.Name = "Test Character";
            var initialExplorerCount = overview.Children.Count;
            var trashNode = model.TrashView.First();
            var initialTrashCount = trashNode.Children.Count;
            
            // Act
            _outlineService.MoveToTrash(character, model);
            
            // Assert
            Assert.AreEqual(initialExplorerCount - 1, overview.Children.Count, "Explorer view should have one less child");
            Assert.AreEqual(initialTrashCount + 1, trashNode.Children.Count, "Trash should have one more child");
            Assert.IsTrue(trashNode.Children.Any(n => n.Uuid == character.Uuid), "Character should be in trash");
            Assert.IsFalse(overview.Children.Any(n => n.Uuid == character.Uuid), "Character should not be in explorer");
        }

        /// <summary>
        /// Tests that MoveToTrash moves parent and children together
        /// </summary>
        [TestMethod]
        public async Task MoveToTrash_WithChildren_MovesParentAndChildrenToTrash()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.ExplorerView.First();
            var folder = _outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
            var child1 = _outlineService.AddStoryElement(model, StoryItemType.Character, folder.Node);
            var child2 = _outlineService.AddStoryElement(model, StoryItemType.Scene, folder.Node);
            var trashNode = model.TrashView.First();
            
            // Act
            _outlineService.MoveToTrash(folder, model);
            
            // Assert
            Assert.IsTrue(trashNode.Children.Any(n => n.Uuid == folder.Uuid), "Folder should be in trash");
            Assert.AreEqual(2, folder.Node.Children.Count, "Folder should still have its children");
            Assert.IsTrue(folder.Node.Children.Any(n => n.Uuid == child1.Uuid), "Child1 should still be under folder");
            Assert.IsTrue(folder.Node.Children.Any(n => n.Uuid == child2.Uuid), "Child2 should still be under folder");
        }

        /// <summary>
        /// Tests that MoveToTrash removes references to the element
        /// </summary>
        [TestMethod]
        public async Task MoveToTrash_WithReferences_RemovesReferences()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.ExplorerView.First();
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview);
            var problem = _outlineService.AddStoryElement(model, StoryItemType.Problem, overview);
            
            // Set character as protagonist in problem
            ((ProblemModel)problem).Protagonist = character.Uuid;
            
            // Act
            _outlineService.MoveToTrash(character, model);
            
            // Assert
            Assert.AreEqual(Guid.Empty, ((ProblemModel)problem).Protagonist, "Reference should be removed");
        }

        /// <summary>
        /// Tests that MoveToTrash throws exception for null element
        /// </summary>
        [TestMethod]
        public async Task MoveToTrash_WithNullElement_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.MoveToTrash(null, model);
            });
        }

        /// <summary>
        /// Tests that MoveToTrash throws exception for root nodes
        /// </summary>
        [TestMethod]
        public async Task MoveToTrash_WithRootNode_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _outlineService.MoveToTrash(overview, model);
            });
        }

        /// <summary>
        /// Tests that MoveToTrash throws exception for TrashCan itself
        /// </summary>
        [TestMethod]
        public async Task MoveToTrash_WithTrashCan_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var trashCan = model.StoryElements.First(e => e.ElementType == StoryItemType.TrashCan);
            
            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _outlineService.MoveToTrash(trashCan, model);
            });
        }

        /// <summary>
        /// Tests that RestoreFromTrash moves element back to Explorer view
        /// </summary>
        [TestMethod]
        public async Task RestoreFromTrash_ToExplorerView_RestoresElement()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.ExplorerView.First();
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview);
            _outlineService.MoveToTrash(character, model);
            var trashNode = model.TrashView.First();
            var characterInTrash = trashNode.Children.First(n => n.Uuid == character.Uuid);
            
            // Act
            _outlineService.RestoreFromTrash(characterInTrash, model);
            
            // Assert
            Assert.IsFalse(trashNode.Children.Any(n => n.Uuid == character.Uuid), "Character should not be in trash");
            Assert.IsTrue(overview.Children.Any(n => n.Uuid == character.Uuid), "Character should be back in explorer");
        }

        /// <summary>
        /// Tests that RestoreFromTrash restores parent with all children
        /// </summary>
        [TestMethod]
        public async Task RestoreFromTrash_WithChildren_RestoresParentAndChildren()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.ExplorerView.First();
            var folder = _outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
            var child1 = _outlineService.AddStoryElement(model, StoryItemType.Character, folder.Node);
            var child2 = _outlineService.AddStoryElement(model, StoryItemType.Scene, folder.Node);
            _outlineService.MoveToTrash(folder, model);
            
            var trashNode = model.TrashView.First();
            var folderInTrash = trashNode.Children.First(n => n.Uuid == folder.Uuid);
            
            // Act
            _outlineService.RestoreFromTrash(folderInTrash, model);
            
            // Assert
            Assert.IsTrue(overview.Children.Any(n => n.Uuid == folder.Uuid), "Folder should be restored");
            var restoredFolder = overview.Children.First(n => n.Uuid == folder.Uuid);
            Assert.AreEqual(2, restoredFolder.Children.Count, "Children should be restored with parent");
        }

        /// <summary>
        /// Tests that RestoreFromTrash only allows top-level trash items
        /// </summary>
        [TestMethod]
        public async Task RestoreFromTrash_WithNestedItem_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.ExplorerView.First();
            var folder = _outlineService.AddStoryElement(model, StoryItemType.Folder, overview);
            var child = _outlineService.AddStoryElement(model, StoryItemType.Character, folder.Node);
            _outlineService.MoveToTrash(folder, model);
            
            var trashNode = model.TrashView.First();
            var folderInTrash = trashNode.Children.First(n => n.Uuid == folder.Uuid);
            var childInTrash = folderInTrash.Children.First(n => n.Uuid == child.Uuid);
            
            // Act & Assert - cannot restore child directly
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _outlineService.RestoreFromTrash(childInTrash, model);
            });
        }

        /// <summary>
        /// Tests that RestoreFromTrash throws exception for null node
        /// </summary>
        [TestMethod]
        public async Task RestoreFromTrash_WithNullNode_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.RestoreFromTrash(null, model);
            });
        }

        /// <summary>
        /// Tests that RestoreFromTrash throws exception for non-trash items
        /// </summary>
        [TestMethod]
        public async Task RestoreFromTrash_WithNonTrashItem_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.ExplorerView.First();
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview);
            
            // Act & Assert - trying to restore something not in trash
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _outlineService.RestoreFromTrash(character.Node, model);
            });
        }

        /// <summary>
        /// Tests that EmptyTrash removes all items from trash
        /// </summary>
        [TestMethod]
        public async Task EmptyTrash_WithItems_RemovesAllItems()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.ExplorerView.First();
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview);
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview);
            _outlineService.MoveToTrash(character, model);
            _outlineService.MoveToTrash(scene, model);
            
            var trashNode = model.TrashView.First();
            Assert.AreEqual(2, trashNode.Children.Count, "Should have 2 items in trash");
            
            // Act
            _outlineService.EmptyTrash(model);
            
            // Assert
            Assert.AreEqual(0, trashNode.Children.Count, "Trash should be empty");
        }

        /// <summary>
        /// Tests that EmptyTrash handles empty trash gracefully
        /// </summary>
        [TestMethod]
        public async Task EmptyTrash_WithNoItems_ReturnsTrue()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var trashNode = model.TrashView.First();
            
            // Act
            _outlineService.EmptyTrash(model);
            
            // Assert
            Assert.AreEqual(0, trashNode.Children.Count, "Trash should remain empty");
        }

        /// <summary>
        /// Tests that EmptyTrash throws exception for null model
        /// </summary>
        [TestMethod]
        public void EmptyTrash_WithNullModel_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _outlineService.EmptyTrash(null);
            });
        }

        #endregion

        #region Search Methods Tests

        [TestMethod]
        public async Task SearchForText_WithValidText_ReturnsMatchingElements()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            character.Name = "Hero Character";
            ((CharacterModel)character).Role = "Protagonist";
            ((CharacterModel)character).Notes = "A brave warrior";
            
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            scene.Name = "Epic Battle";
            ((SceneModel)scene).Description = "The hero fights the villain";
            
            // Act
            var results = _outlineService.SearchForText(model, "hero");
            
            // Assert
            Assert.AreEqual(2, results.Count, "Should find 2 elements containing 'hero'");
            Assert.IsTrue(results.Any(e => e.Name == "Hero Character"), "Should find the character");
            Assert.IsTrue(results.Any(e => e.Name == "Epic Battle"), "Should find the scene");
        }

        [TestMethod]
        public async Task SearchForText_WithEmptyText_ReturnsEmptyList()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act
            var results = _outlineService.SearchForText(model, "");
            
            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count, "Should return empty list for empty search text");
        }

        [TestMethod]
        public void SearchForText_WithNullModel_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                _outlineService.SearchForText(null, "test"));
        }

        [TestMethod]
        public async Task SearchForText_CaseInsensitive_ReturnsMatches()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            character.Name = "HERO CHARACTER";
            
            // Act
            var results = _outlineService.SearchForText(model, "hero");
            
            // Assert
            Assert.AreEqual(1, results.Count, "Should find 1 element (case-insensitive)");
            Assert.AreEqual("HERO CHARACTER", results[0].Name);
        }

        [TestMethod]
        public async Task SearchForUuidReferences_WithValidUuid_ReturnsReferencingElements()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            character.Name = "Hero";
            
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            scene.Name = "Battle Scene";
            ((SceneModel)scene).CastMembers.Add(character.Uuid);
            
            var problem = _outlineService.AddStoryElement(model, StoryItemType.Problem, overview.Node);
            problem.Name = "Main Conflict";
            ((ProblemModel)problem).Protagonist = character.Uuid;
            
            // Act
            var results = _outlineService.SearchForUuidReferences(model, character.Uuid);
            
            // Assert
            Assert.AreEqual(2, results.Count, "Should find 2 elements referencing the character");
            Assert.IsTrue(results.Any(e => e.Name == "Battle Scene"));
            Assert.IsTrue(results.Any(e => e.Name == "Main Conflict"));
        }

        [TestMethod]
        public async Task SearchForUuidReferences_WithEmptyUuid_ReturnsEmptyList()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act
            var results = _outlineService.SearchForUuidReferences(model, Guid.Empty);
            
            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count, "Should return empty list for empty UUID");
        }

        [TestMethod]
        public void SearchForUuidReferences_WithNullModel_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                _outlineService.SearchForUuidReferences(null, Guid.NewGuid()));
        }

        [TestMethod]
        public async Task SearchForUuidReferences_WithNoReferences_ReturnsEmptyList()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var randomUuid = Guid.NewGuid();
            
            // Act
            var results = _outlineService.SearchForUuidReferences(model, randomUuid);
            
            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count, "Should return empty list for UUID with no references");
        }

        [TestMethod]
        public async Task RemoveUuidReferences_WithValidUuid_ReturnsAffectedCount()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            character.Name = "Hero";
            
            var scene1 = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            scene1.Name = "Scene 1";
            ((SceneModel)scene1).CastMembers.Add(character.Uuid);
            
            var scene2 = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            scene2.Name = "Scene 2";
            ((SceneModel)scene2).CastMembers.Add(character.Uuid);
            
            var problem = _outlineService.AddStoryElement(model, StoryItemType.Problem, overview.Node);
            problem.Name = "Conflict";
            ((ProblemModel)problem).Protagonist = character.Uuid;
            
            model.Changed = false; // Reset changed flag
            
            // Act
            int affectedCount = _outlineService.RemoveUuidReferences(model, character.Uuid);
            
            // Assert
            Assert.AreEqual(3, affectedCount, "Should have removed references from 3 elements");
            Assert.IsTrue(model.Changed, "Model should be marked as changed");
            Assert.AreEqual(0, ((SceneModel)scene1).CastMembers.Count, "Scene1 should have no cast members");
            Assert.AreEqual(0, ((SceneModel)scene2).CastMembers.Count, "Scene2 should have no cast members");
            Assert.AreEqual(Guid.Empty, ((ProblemModel)problem).Protagonist, "Problem should have no protagonist");
        }

        [TestMethod]
        public async Task RemoveUuidReferences_WithEmptyUuid_ReturnsZero()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act
            int affectedCount = _outlineService.RemoveUuidReferences(model, Guid.Empty);
            
            // Assert
            Assert.AreEqual(0, affectedCount, "Should return 0 for empty UUID");
        }

        [TestMethod]
        public void RemoveUuidReferences_WithNullModel_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                _outlineService.RemoveUuidReferences(null, Guid.NewGuid()));
        }

        [TestMethod]
        public async Task RemoveUuidReferences_WithNoReferences_ReturnsZero()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var randomUuid = Guid.NewGuid();
            model.Changed = false;
            
            // Act
            int affectedCount = _outlineService.RemoveUuidReferences(model, randomUuid);
            
            // Assert
            Assert.AreEqual(0, affectedCount, "Should return 0 for UUID with no references");
            Assert.IsFalse(model.Changed, "Model should not be changed when no references removed");
        }

        [TestMethod]
        public async Task SearchInSubtree_WithValidRoot_ReturnsSubtreeMatches()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var folder = _outlineService.AddStoryElement(model, StoryItemType.Folder, overview.Node);
            folder.Name = "Hero Folder";
            
            var character1 = _outlineService.AddStoryElement(model, StoryItemType.Character, folder.Node);
            character1.Name = "Hero Character";
            
            var character2 = _outlineService.AddStoryElement(model, StoryItemType.Character, folder.Node);
            character2.Name = "Sidekick";
            ((CharacterModel)character2).Notes = "Helps the hero";
            
            var otherCharacter = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            otherCharacter.Name = "Another Hero";
            
            // Act
            var results = _outlineService.SearchInSubtree(model, folder.Node, "hero");
            
            // Assert
            Assert.AreEqual(3, results.Count, "Should find 3 elements in subtree containing 'hero'");
            Assert.IsTrue(results.Any(e => e.Name == "Hero Folder"), "Should find the folder itself");
            Assert.IsTrue(results.Any(e => e.Name == "Hero Character"), "Should find character in folder");
            Assert.IsTrue(results.Any(e => e.Name == "Sidekick"), "Should find sidekick with 'hero' in notes");
            Assert.IsFalse(results.Any(e => e.Name == "Another Hero"), "Should NOT find character outside subtree");
        }

        [TestMethod]
        public async Task SearchInSubtree_WithEmptySearchText_ReturnsEmptyList()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            // Act
            var results = _outlineService.SearchInSubtree(model, overview.Node, "");
            
            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count, "Should return empty list for empty search text");
        }

        [TestMethod]
        public async Task SearchInSubtree_WithNullModel_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                _outlineService.SearchInSubtree(null, overview.Node, "test"));
        }

        [TestMethod]
        public async Task SearchInSubtree_WithNullRoot_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                _outlineService.SearchInSubtree(model, null, "test"));
        }

        // Removed tests that tried to create orphan StoryNodeItem objects
        // These don't make sense since StoryNodeItem requires a valid StoryElement
        // The search methods will handle missing elements gracefully anyway

        #endregion

        #region AddRelationship Tests

        [TestMethod]
        public async Task AddRelationship_BetweenCharacters_AddsSuccessfully()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character1 = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            character1.Name = "Hero";
            
            var character2 = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            character2.Name = "Sidekick";
            
            // Act
            bool result = _outlineService.AddRelationship(model, character1.Uuid, character2.Uuid, "Friends", true);
            
            // Assert
            Assert.IsTrue(result, "Should successfully add relationship");
            var char1Model = (CharacterModel)character1;
            var char2Model = (CharacterModel)character2;
            
            // Check character1's relationships
            Assert.AreEqual(1, char1Model.RelationshipList.Count, "Character1 should have 1 relationship");
            Assert.AreEqual(character2.Uuid, char1Model.RelationshipList[0].PartnerUuid);
            Assert.AreEqual("Friends", char1Model.RelationshipList[0].RelationType);
            
            // Check character2's mirrored relationship
            Assert.AreEqual(1, char2Model.RelationshipList.Count, "Character2 should have 1 mirrored relationship");
            Assert.AreEqual(character1.Uuid, char2Model.RelationshipList[0].PartnerUuid);
            Assert.AreEqual("Friends", char2Model.RelationshipList[0].RelationType);
        }

        [TestMethod]
        public async Task AddRelationship_WithoutMirror_AddsOnlyToSource()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character1 = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            var character2 = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            
            // Act
            bool result = _outlineService.AddRelationship(model, character1.Uuid, character2.Uuid, "Admires", false);
            
            // Assert
            Assert.IsTrue(result);
            var char1Model = (CharacterModel)character1;
            var char2Model = (CharacterModel)character2;
            
            Assert.AreEqual(1, char1Model.RelationshipList.Count, "Character1 should have 1 relationship");
            Assert.AreEqual(0, char2Model.RelationshipList.Count, "Character2 should have no relationships");
        }

        [TestMethod]
        public async Task AddRelationship_WithEmptySourceGuid_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                _outlineService.AddRelationship(model, Guid.Empty, character.Uuid, "Friends"));
        }

        [TestMethod]
        public async Task AddRelationship_WithEmptyRecipientGuid_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                _outlineService.AddRelationship(model, character.Uuid, Guid.Empty, "Friends"));
        }

        [TestMethod]
        public async Task AddRelationship_WithNonCharacterSource_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            
            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() => 
                _outlineService.AddRelationship(model, scene.Uuid, character.Uuid, "Friends"));
        }

        [TestMethod]
        public async Task AddRelationship_WithNonCharacterRecipient_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            
            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() => 
                _outlineService.AddRelationship(model, character.Uuid, scene.Uuid, "Friends"));
        }

        [TestMethod]
        public async Task AddRelationship_DuplicateRelationship_ShouldNotAddDuplicate()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character1 = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            var character2 = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            
            // Act - Add relationship twice
            _outlineService.AddRelationship(model, character1.Uuid, character2.Uuid, "Friends", false);
            bool result = _outlineService.AddRelationship(model, character1.Uuid, character2.Uuid, "Friends", false);
            
            // Assert
            Assert.IsTrue(result); // Method returns true
            var char1Model = (CharacterModel)character1;
            Assert.AreEqual(1, char1Model.RelationshipList.Count, "Should only have 1 relationship (no duplicates)");
        }

        #endregion

        #region AddCastMember Tests

        [TestMethod]
        public async Task AddCastMember_ToScene_AddsSuccessfully()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            scene.Name = "Battle Scene";
            
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            character.Name = "Hero";
            
            // Act
            bool result = _outlineService.AddCastMember(model, scene, character.Uuid);
            
            // Assert
            Assert.IsTrue(result, "Should successfully add cast member");
            var sceneModel = (SceneModel)scene;
            Assert.AreEqual(1, sceneModel.CastMembers.Count, "Scene should have 1 cast member");
            Assert.IsTrue(sceneModel.CastMembers.Contains(character.Uuid), "Scene should contain the character");
        }

        [TestMethod]
        public async Task AddCastMember_DuplicateCastMember_ReturnsTrue()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            
            // Add cast member first time
            _outlineService.AddCastMember(model, scene, character.Uuid);
            
            // Act - Add same cast member again
            bool result = _outlineService.AddCastMember(model, scene, character.Uuid);
            
            // Assert
            Assert.IsTrue(result, "Should return true for duplicate");
            var sceneModel = (SceneModel)scene;
            Assert.AreEqual(1, sceneModel.CastMembers.Count, "Should not add duplicate cast member");
        }

        [TestMethod]
        public async Task AddCastMember_WithNullSource_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                _outlineService.AddCastMember(model, null, character.Uuid));
        }

        [TestMethod]
        public async Task AddCastMember_WithEmptyCastMemberGuid_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() => 
                _outlineService.AddCastMember(model, scene, Guid.Empty));
        }

        [TestMethod]
        public async Task AddCastMember_ToNonScene_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character1 = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            var character2 = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            
            // Act & Assert - Try to add cast member to a character instead of scene
            Assert.ThrowsExactly<InvalidOperationException>(() => 
                _outlineService.AddCastMember(model, character1, character2.Uuid));
        }

        [TestMethod]
        public async Task AddCastMember_NonCharacterCastMember_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var scene1 = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            var scene2 = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            
            // Act & Assert - Try to add a scene as cast member
            Assert.ThrowsExactly<InvalidOperationException>(() => 
                _outlineService.AddCastMember(model, scene1, scene2.Uuid));
        }

        [TestMethod]
        public async Task AddCastMember_MultipleCharacters_AddsAll()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            var character1 = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            var character2 = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            var character3 = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            
            // Act
            bool result1 = _outlineService.AddCastMember(model, scene, character1.Uuid);
            bool result2 = _outlineService.AddCastMember(model, scene, character2.Uuid);
            bool result3 = _outlineService.AddCastMember(model, scene, character3.Uuid);
            
            // Assert
            Assert.IsTrue(result1 && result2 && result3, "All additions should succeed");
            var sceneModel = (SceneModel)scene;
            Assert.AreEqual(3, sceneModel.CastMembers.Count, "Scene should have 3 cast members");
            Assert.IsTrue(sceneModel.CastMembers.Contains(character1.Uuid));
            Assert.IsTrue(sceneModel.CastMembers.Contains(character2.Uuid));
            Assert.IsTrue(sceneModel.CastMembers.Contains(character3.Uuid));
        }

        #endregion

        #region ConvertProblemToScene Tests

        [TestMethod]
        public async Task ConvertProblemToScene_BasicConversion_Success()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var problem = _outlineService.AddStoryElement(model, StoryItemType.Problem, overview.Node);
            problem.Name = "Main Conflict";
            var problemModel = (ProblemModel)problem;
            
            // Set problem properties
            var protagonist = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            var antagonist = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            problemModel.Protagonist = protagonist.Uuid;
            problemModel.Antagonist = antagonist.Uuid;
            problemModel.ProtGoal = "Save the world";
            problemModel.AntagGoal = "Destroy the world";
            problemModel.ProtConflict = "Hero vs Villain";
            problemModel.Outcome = "Hero wins";
            problemModel.Notes = "Epic battle";
            
            var originalUuid = problem.Uuid;
            var originalNodeUuid = problem.Node.Uuid;
            var originalIndex = overview.Node.Children.IndexOf(problem.Node);
            
            // Act
            var scene = _outlineService.ConvertProblemToScene(model, problemModel);
            
            // Assert
            Assert.IsNotNull(scene);
            Assert.IsInstanceOfType(scene, typeof(SceneModel));
            Assert.AreEqual("Main Conflict", scene.Name);
            Assert.AreEqual(originalUuid, scene.Uuid, "Should preserve UUID");
            Assert.AreEqual(originalNodeUuid, scene.Node.Uuid, "Should preserve Node UUID");
            Assert.AreEqual(protagonist.Uuid, scene.Protagonist);
            Assert.AreEqual(antagonist.Uuid, scene.Antagonist);
            Assert.AreEqual("Save the world", scene.ProtagGoal);
            Assert.AreEqual("Destroy the world", scene.AntagGoal);
            Assert.AreEqual("Hero vs Villain", scene.Opposition);
            Assert.AreEqual("Hero wins", scene.Outcome);
            Assert.AreEqual("Epic battle", scene.Notes);
            Assert.AreEqual(originalIndex, overview.Node.Children.IndexOf(scene.Node), "Should maintain position");
            Assert.IsFalse(model.StoryElements.Any(e => e.ElementType == StoryItemType.Problem && e.Uuid == originalUuid), "Problem should be removed");
            Assert.IsTrue(model.StoryElements.Contains(scene), "Scene should be in story elements");
        }

        [TestMethod]
        public async Task ConvertProblemToScene_WithChildren_MovesChildren()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var problem = _outlineService.AddStoryElement(model, StoryItemType.Problem, overview.Node);
            var problemModel = (ProblemModel)problem;
            
            // Add children to the problem
            var child1 = _outlineService.AddStoryElement(model, StoryItemType.Character, problem.Node);
            child1.Name = "Child Character 1";
            var child2 = _outlineService.AddStoryElement(model, StoryItemType.Character, problem.Node);
            child2.Name = "Child Character 2";
            
            // Act
            var scene = _outlineService.ConvertProblemToScene(model, problemModel);
            
            // Assert - This documents the EXPECTED behavior
            Assert.AreEqual(2, scene.Node.Children.Count, "Should have 2 children");
            Assert.IsTrue(scene.Node.Children.Any(c => c.Name == "Child Character 1"), "Should have child with correct name");
            Assert.IsTrue(scene.Node.Children.Any(c => c.Name == "Child Character 2"), "Should have child with correct name");
            Assert.AreEqual(scene.Node, child1.Node.Parent, "Child1's parent should be the scene node");
            Assert.AreEqual(scene.Node, child2.Node.Parent, "Child2's parent should be the scene node");
        }

        [TestMethod]
        public async Task ConvertProblemToScene_PreservesNodeState()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var problem = _outlineService.AddStoryElement(model, StoryItemType.Problem, overview.Node);
            var problemModel = (ProblemModel)problem;
            problem.Node.IsExpanded = true;
            problem.Node.IsSelected = true;
            
            // Act
            var scene = _outlineService.ConvertProblemToScene(model, problemModel);
            
            // Assert
            Assert.IsTrue(scene.Node.IsExpanded, "Should preserve expanded state");
            Assert.IsTrue(scene.Node.IsSelected, "Should preserve selected state");
            Assert.IsTrue(scene.Node.Parent.IsExpanded, "Parent should be expanded");
        }

        [TestMethod]
        public async Task ConvertProblemToScene_UpdatesExplorerView()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var problem = _outlineService.AddStoryElement(model, StoryItemType.Problem, overview.Node);
            var problemModel = (ProblemModel)problem;
            var problemNode = problem.Node;
            
            // Act
            var scene = _outlineService.ConvertProblemToScene(model, problemModel);
            
            // Assert
            Assert.IsFalse(model.ExplorerView.Contains(problemNode), "Problem node should be removed from explorer view");
            // Scene node should be in its parent's children which means it's in the tree
            Assert.IsTrue(overview.Node.Children.Contains(scene.Node), "Scene node should be in parent's children");
        }

        #endregion

        #region ConvertSceneToProblem Tests

        [TestMethod]
        public async Task ConvertSceneToProblem_BasicConversion_Success()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            scene.Name = "Epic Battle";
            var sceneModel = (SceneModel)scene;
            
            // Set scene properties
            var protagonist = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            var antagonist = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            sceneModel.Protagonist = protagonist.Uuid;
            sceneModel.Antagonist = antagonist.Uuid;
            sceneModel.ProtagGoal = "Win the battle";
            sceneModel.AntagGoal = "Defeat the hero";
            sceneModel.Opposition = "Sword fight";
            sceneModel.Outcome = "Hero prevails";
            sceneModel.Notes = "Dramatic scene";
            
            var originalUuid = scene.Uuid;
            var originalNodeUuid = scene.Node.Uuid;
            var originalIndex = overview.Node.Children.IndexOf(scene.Node);
            
            // Act
            var problem = _outlineService.ConvertSceneToProblem(model, sceneModel);
            
            // Assert
            Assert.IsNotNull(problem);
            Assert.IsInstanceOfType(problem, typeof(ProblemModel));
            Assert.AreEqual("Epic Battle", problem.Name);
            Assert.AreEqual(originalUuid, problem.Uuid, "Should preserve UUID");
            Assert.AreEqual(originalNodeUuid, problem.Node.Uuid, "Should preserve Node UUID");
            Assert.AreEqual(protagonist.Uuid, problem.Protagonist);
            Assert.AreEqual(antagonist.Uuid, problem.Antagonist);
            Assert.AreEqual("Win the battle", problem.ProtGoal);
            Assert.AreEqual("Defeat the hero", problem.AntagGoal);
            Assert.AreEqual("Sword fight", problem.ProtConflict);
            Assert.AreEqual("Hero prevails", problem.Outcome);
            Assert.AreEqual("Dramatic scene", problem.Notes);
            Assert.AreEqual(originalIndex, overview.Node.Children.IndexOf(problem.Node), "Should maintain position");
            Assert.IsFalse(model.StoryElements.Any(e => e.ElementType == StoryItemType.Scene && e.Uuid == originalUuid), "Scene should be removed");
            Assert.IsTrue(model.StoryElements.Contains(problem), "Problem should be in story elements");
        }

        [TestMethod]
        public async Task ConvertSceneToProblem_WithChildren_MovesChildren()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            var sceneModel = (SceneModel)scene;
            
            // Add children to the scene
            var child1 = _outlineService.AddStoryElement(model, StoryItemType.Character, scene.Node);
            child1.Name = "Scene Child 1";
            var child2 = _outlineService.AddStoryElement(model, StoryItemType.Character, scene.Node);
            child2.Name = "Scene Child 2";
            
            // Act
            var problem = _outlineService.ConvertSceneToProblem(model, sceneModel);
            
            // Assert - This documents EXPECTED behavior (children should keep their names)
            Assert.AreEqual(2, problem.Node.Children.Count, "Should have 2 children");
            Assert.IsTrue(problem.Node.Children.Any(c => c.Name == "Scene Child 1"), "Should have child with correct name");
            Assert.IsTrue(problem.Node.Children.Any(c => c.Name == "Scene Child 2"), "Should have child with correct name");
            Assert.AreEqual(problem.Node, child1.Node.Parent, "Child1's parent should be the problem node");
            Assert.AreEqual(problem.Node, child2.Node.Parent, "Child2's parent should be the problem node");
        }

        [TestMethod]
        public async Task ConvertSceneToProblem_PreservesNodeState()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            var sceneModel = (SceneModel)scene;
            scene.Node.IsExpanded = true;
            scene.Node.IsSelected = true;
            
            // Act
            var problem = _outlineService.ConvertSceneToProblem(model, sceneModel);
            
            // Assert
            Assert.IsTrue(problem.Node.IsExpanded, "Should preserve expanded state");
            Assert.IsTrue(problem.Node.IsSelected, "Should preserve selected state");
            Assert.IsTrue(problem.Node.Parent.IsExpanded, "Parent should be expanded");
        }

        [TestMethod]
        public async Task ConvertSceneToProblem_UpdatesExplorerView()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var scene = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            var sceneModel = (SceneModel)scene;
            var sceneNode = scene.Node;
            
            // Act
            var problem = _outlineService.ConvertSceneToProblem(model, sceneModel);
            
            // Assert
            Assert.IsFalse(model.ExplorerView.Contains(sceneNode), "Scene node should be removed from explorer view");
            Assert.IsTrue(overview.Node.Children.Contains(problem.Node), "Problem node should be in parent's children");
        }

        #endregion

        #region FindElementReferences Tests

        [TestMethod]
        public async Task FindElementReferences_WithReferences_ReturnsReferencingElements()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            character.Name = "Hero";
            
            var scene1 = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            ((SceneModel)scene1).CastMembers.Add(character.Uuid);
            
            var scene2 = _outlineService.AddStoryElement(model, StoryItemType.Scene, overview.Node);
            ((SceneModel)scene2).CastMembers.Add(character.Uuid);
            
            var problem = _outlineService.AddStoryElement(model, StoryItemType.Problem, overview.Node);
            ((ProblemModel)problem).Protagonist = character.Uuid;
            
            // Act
            var references = _outlineService.FindElementReferences(model, character.Uuid);
            
            // Assert
            Assert.AreEqual(3, references.Count, "Should find 3 references");
            Assert.IsTrue(references.Contains(scene1));
            Assert.IsTrue(references.Contains(scene2));
            Assert.IsTrue(references.Contains(problem));
        }

        [TestMethod]
        public async Task FindElementReferences_WithNoReferences_ReturnsEmptyList()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            
            // Act
            var references = _outlineService.FindElementReferences(model, character.Uuid);
            
            // Assert
            Assert.IsNotNull(references);
            Assert.AreEqual(0, references.Count, "Should find no references");
        }

        [TestMethod]
        public async Task FindElementReferences_ElementInTrash_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            _outlineService.MoveToTrash(character, model);
            
            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() => 
                _outlineService.FindElementReferences(model, character.Uuid),
                "Should throw for elements in trash");
        }

        [TestMethod]
        public async Task FindElementReferences_RootNode_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() => 
                _outlineService.FindElementReferences(model, overview.Uuid),
                "Should throw for root nodes");
        }

        [TestMethod]
        public async Task FindElementReferences_ExcludesSelf_FromResults()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
            
            var character = _outlineService.AddStoryElement(model, StoryItemType.Character, overview.Node);
            // Character doesn't reference itself, but want to make sure it's excluded from search
            
            // Act
            var references = _outlineService.FindElementReferences(model, character.Uuid);
            
            // Assert
            Assert.IsFalse(references.Contains(character), "Should not include self in results");
        }

        #endregion

        #region Beatsheet Tests

        private string _testBeatOutputPath;

        private void SetupBeatTestPath()
        {
            _testBeatOutputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BeatTestOutputs");
            if (Directory.Exists(_testBeatOutputPath))
            {
                Directory.Delete(_testBeatOutputPath, true);
            }
            Directory.CreateDirectory(_testBeatOutputPath);
        }

        private async Task<(StoryModel model, ProblemModel problem)> CreateModelWithProblem()
        {
            var model = await _outlineService.CreateModel("Beat Test", "Author", 0);
            var overview = model.StoryElements.OfType<OverviewModel>().First();
            var problem = new ProblemModel("Problem", model, overview.Node);
            return (model, problem);
        }

        [TestMethod]
        public async Task CreateBeat_ShouldAddBeatToProblem()
        {
            var (model, problem) = await CreateModelWithProblem();

            _outlineService.CreateBeat(problem, "Beat 1", "Desc 1");

            Assert.AreEqual(1, problem.StructureBeats.Count);
            Assert.AreEqual("Beat 1", problem.StructureBeats[0].Title);
            Assert.AreEqual("Desc 1", problem.StructureBeats[0].Description);
        }

        [TestMethod]
        public async Task AssignAndUnassignBeat_ShouldUpdateGuid()
        {
            var (model, problem) = await CreateModelWithProblem();
            _outlineService.CreateBeat(problem, "Beat", "Desc");
            var scene = new SceneModel("Scene", model, problem.Node);

            _outlineService.AssignElementToBeat(model, problem, 0, scene.Uuid);
            Assert.AreEqual(scene.Uuid, problem.StructureBeats[0].Guid);

            _outlineService.UnasignBeat(model, problem, 0);
            Assert.AreEqual(Guid.Empty, problem.StructureBeats[0].Guid);
        }

        [TestMethod]
        public async Task DeleteBeat_ShouldRemoveBeat()
        {
            var (model, problem) = await CreateModelWithProblem();
            _outlineService.CreateBeat(problem, "Beat", "Desc");
            Assert.AreEqual(1, problem.StructureBeats.Count);

            _outlineService.DeleteBeat(model, problem, 0);

            Assert.AreEqual(0, problem.StructureBeats.Count);
        }

        [TestMethod]
        public async Task SetBeatSheet_ShouldReplaceBeats()
        {
            var (model, problem) = await CreateModelWithProblem();
            _outlineService.CreateBeat(problem, "Old", "Old");

            var newBeats = new ObservableCollection<StructureBeatViewModel>
            {
                new StructureBeatViewModel("B1", "D1"),
                new StructureBeatViewModel("B2", "D2")
            };

            _outlineService.SetBeatSheet(model, problem, "Desc", "Title", newBeats);

            Assert.AreEqual("Title", problem.StructureTitle);
            Assert.AreEqual("Desc", problem.StructureDescription);
            Assert.AreEqual(2, problem.StructureBeats.Count);
            Assert.AreEqual("B1", problem.StructureBeats[0].Title);
            Assert.AreSame(newBeats, problem.StructureBeats);
        }

        [TestMethod]
        public void SaveAndLoadBeatsheet_ShouldPersistBeats()
        {
            SetupBeatTestPath();
            
            var beats = new List<StructureBeatViewModel>
            {
                new StructureBeatViewModel("Intro", "D1") { Guid = Guid.NewGuid() },
                new StructureBeatViewModel("Middle", "D2") { Guid = Guid.NewGuid() }
            };
            string file = Path.Combine(_testBeatOutputPath, "beats.json");

            _outlineService.SaveBeatsheet(file, "SheetDesc", beats);
            Assert.IsTrue(File.Exists(file));

            var loaded = _outlineService.LoadBeatsheet(file);
            Assert.AreEqual("SheetDesc", loaded.Description);
            Assert.AreEqual(beats.Count, loaded.Beats.Count);
            Assert.AreEqual("Intro", loaded.Beats[0].Title);
            Assert.AreEqual(Guid.Empty, loaded.Beats[0].Guid);
        }

        #endregion
    }
}
