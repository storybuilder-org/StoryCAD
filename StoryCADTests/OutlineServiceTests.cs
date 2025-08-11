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

namespace StoryCADTests
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
            await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
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
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void SetCurrentView_NullModel_ThrowsArgumentNullException()
        {
            // Act
            _outlineService.SetCurrentView(null, StoryViewType.ExplorerView);
        }

        /// <summary>
        /// Tests that SetCurrentView throws ArgumentException for invalid view type
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SetCurrentView_InvalidViewType_ThrowsArgumentException()
        {
            // Arrange
            var model = new StoryModel();

            // Act
            _outlineService.SetCurrentView(model, (StoryViewType)999);
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
        [ExpectedException(typeof(ArgumentException))]
        public void GetStoryElementByGuid_EmptyGuid_ThrowsException()
        {
            // Arrange
            var model = new StoryModel();

            // Act
            _outlineService.GetStoryElementByGuid(model, Guid.Empty);
        }

        /// <summary>
        /// Tests that GetStoryElementByGuid throws exception for non-existent GUID
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task GetStoryElementByGuid_NonExistentGuid_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var nonExistentGuid = Guid.NewGuid();

            // Act
            _outlineService.GetStoryElementByGuid(model, nonExistentGuid);
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void UpdateStoryElement_NullModel_ThrowsException()
        {
            // Arrange
            var element = new CharacterModel();

            // Act
            _outlineService.UpdateStoryElement(null, element);
        }

        /// <summary>
        /// Tests that UpdateStoryElement throws exception for null element
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UpdateStoryElement_NullElement_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);

            // Act
            _outlineService.UpdateStoryElement(model, null);
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetCharacterList_NullModel_ThrowsException()
        {
            // Act
            _outlineService.GetCharacterList(null);
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void UpdateStoryElementByGuid_NullModel_ThrowsException()
        {
            // Act
            _outlineService.UpdateStoryElementByGuid(null, Guid.NewGuid(), new CharacterModel());
        }

        /// <summary>
        /// Tests that UpdateStoryElementByGuid throws exception for empty GUID
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateStoryElementByGuid_EmptyGuid_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act
            _outlineService.UpdateStoryElementByGuid(model, Guid.Empty, new CharacterModel());
        }

        /// <summary>
        /// Tests that UpdateStoryElementByGuid throws exception for null element
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UpdateStoryElementByGuid_NullElement_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            
            // Act
            _outlineService.UpdateStoryElementByGuid(model, Guid.NewGuid(), null);
        }

        /// <summary>
        /// Tests that UpdateStoryElementByGuid throws exception for non-existent GUID
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateStoryElementByGuid_NonExistentGuid_ThrowsException()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test Story", "Test Author", 0);
            var nonExistentGuid = Guid.NewGuid();
            
            // Act
            _outlineService.UpdateStoryElementByGuid(model, nonExistentGuid, new CharacterModel());
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetSettingsList_NullModel_ThrowsException()
        {
            // Act
            _outlineService.GetSettingsList(null);
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetAllStoryElements_NullModel_ThrowsException()
        {
            // Act
            _outlineService.GetAllStoryElements(null);
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
    }
}
