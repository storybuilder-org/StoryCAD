using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Outline;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            Assert.IsTrue(model.ExplorerView.Count >= 2, "ExplorerView should contain at least two nodes (Overview and TrashCan).");
            
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
    }
}
