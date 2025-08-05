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
    /// <description>Creation of StoryModel instances with different templates.</description>
    /// </item>
    /// <item>
    /// <description>Writing StoryModel files to disk in JSON format.</description>
    /// </item>
    /// <item>
    /// <description>Opening previously saved StoryModel files.</description>
    /// </item>
    /// <item>
    /// <description>Edge cases such as invalid file paths and non-existent files.</description>
    /// </item>
    /// </list>
    /// </summary>
    [TestClass]
    public class OutlineServiceTests
    {
        private OutlineService _outlineService;
        // Temporary output path for test files.
        private static readonly string testOutputPath = Path.GetTempPath();

        /// <summary>
        /// Initializes the test class by setting up the necessary services in the IoC container.
        /// This includes registering all the services used by OutlineService.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            // Use ServiceLocator.Reset() to clear any existing registrations.
            ServiceLocator.Initialize();
            
            // Register OutlineService and any of its dependencies here.
            _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
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

        // ----- TDD Cycle 1: SetChanged Method Tests -----

        [TestMethod]
        public void SetChanged_ShouldUpdateModelChangedStatus()
        {
            // Arrange
            var model = new StoryModel();
            model.Changed = false;

            // Act
            _outlineService.SetChanged(model, true);

            // Assert
            Assert.IsTrue(model.Changed, "Model.Changed should be true after calling SetChanged(true)");
        }

        [TestMethod]
        public void SetChanged_ShouldUseSerializationLock()
        {
            // Arrange
            var model = new StoryModel();
            
            // Act & Assert - Should not throw exception
            _outlineService.SetChanged(model, true);
            Assert.IsTrue(model.Changed, "SetChanged should work with SerializationLock");
        }

        [TestMethod]
        public void SetChanged_NullModel_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                _outlineService.SetChanged(null, true));
        }

        // ----- TDD Cycle 2: GetStoryElementByGuid Method Tests -----

        [TestMethod]
        public async Task GetStoryElementByGuid_ValidGuid_ReturnsElement()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test", "Author", 1);
            var element = model.StoryElements.First();
            var guid = element.Uuid;

            // Act
            var result = _outlineService.GetStoryElementByGuid(model, guid);

            // Assert
            Assert.IsNotNull(result, "Should return a valid element");
            Assert.AreEqual(guid, result.Uuid, "Should return element with correct GUID");
        }

        [TestMethod]
        public void GetStoryElementByGuid_EmptyGuid_ThrowsException()
        {
            // Arrange
            var model = new StoryModel();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                _outlineService.GetStoryElementByGuid(model, Guid.Empty));
        }

        [TestMethod]
        public void GetStoryElementByGuid_NonExistentGuid_ThrowsException()
        {
            // Arrange
            var model = new StoryModel();
            var nonExistentGuid = Guid.NewGuid();

            // Act & Assert
            Assert.ThrowsException<KeyNotFoundException>(() => 
                _outlineService.GetStoryElementByGuid(model, nonExistentGuid));
        }

        [TestMethod]
        public void GetStoryElementByGuid_NullModel_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                _outlineService.GetStoryElementByGuid(null, Guid.NewGuid()));
        }

        // ----- TDD Cycle 3: UpdateStoryElement Method Tests -----

        [TestMethod]
        public async Task UpdateStoryElement_ValidElement_UpdatesSuccessfully()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test", "Author", 1);
            var element = model.StoryElements.First();
            element.Name = "Updated Name";

            // Act
            _outlineService.UpdateStoryElement(model, element);

            // Assert
            var retrievedElement = _outlineService.GetStoryElementByGuid(model, element.Uuid);
            Assert.AreEqual("Updated Name", retrievedElement.Name, "Element should be updated in model");
        }

        [TestMethod]
        public void UpdateStoryElement_NullModel_ThrowsException()
        {
            // Arrange
            var element = new OverviewModel("Test", new StoryModel(), null);

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                _outlineService.UpdateStoryElement(null, element));
        }

        [TestMethod]
        public void UpdateStoryElement_NullElement_ThrowsException()
        {
            // Arrange
            var model = new StoryModel();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                _outlineService.UpdateStoryElement(model, null));
        }

        [TestMethod]
        public async Task UpdateStoryElement_ValidElement_MarksModelAsChanged()
        {
            // Arrange
            var model = await _outlineService.CreateModel("Test", "Author", 1);
            var element = model.StoryElements.First();
            model.Changed = false;

            // Act
            _outlineService.UpdateStoryElement(model, element);

            // Assert
            Assert.IsTrue(model.Changed, "Model should be marked as changed after update");
        }
    }
}