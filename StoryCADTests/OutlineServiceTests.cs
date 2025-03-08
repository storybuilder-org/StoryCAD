using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Outline;
using System;
using Windows.Storage;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StoryCADTests
{
    [TestClass]
    public class OutlineServiceTests
    {
        private OutlineService _outlineService;
        private string testOutputPath;

        [TestInitialize]
        public void TestInitialize()
        {
            _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
            testOutputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestOutputs");

            // Ensure the TestOutputs directory is empty or recreated
            if (Directory.Exists(testOutputPath))
            {
                Directory.Delete(testOutputPath, true); // Delete the directory and all its contents
            }
            Directory.CreateDirectory(testOutputPath); // Recreate the directory
        }

        [TestMethod]
        public async Task CreateModel_ValidInput_CreatesModelSuccessfully()
        {
            // Arrange
            string projectName = "DefaultElementsTestProject";
            string projectPath = Path.Combine(testOutputPath, projectName);
            Directory.CreateDirectory(projectPath);
            StorageFile file = await StorageFile.GetFileFromPathAsync(Path.Combine(projectPath, "DefaultElementsTestProject.stbx"));
            string author = "Test Author";
            int templateIndex = 0;

            // Act
            StoryModel result = await _outlineService.CreateModel(projectName, author, templateIndex);

            // Assert
            Assert.IsNotNull(result, "StoryModel should not be null.");
            Assert.IsTrue(result.StoryElements.Count == 2, "StoryElements should contain exactly 2 elements.");
            Assert.IsTrue(result.StoryElements.Any(e => e.ElementType == StoryItemType.StoryOverview), "StoryOverview should be present.");
            Assert.IsTrue(result.StoryElements.Any(e => e.ElementType == StoryItemType.TrashCan), "Trash should be present.");
            Assert.IsFalse(result.StoryElements.Any(e => e.ElementType == StoryItemType.Folder), "Folder should not be present.");
            Assert.IsFalse(result.StoryElements.Any(e => e.ElementType == StoryItemType.Character), "Character should not be present.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task CreateModel_InvalidProjectName_ThrowsArgumentException()
        {
            // Arrange
            string invalidProjectName = string.Empty;
            string projectPath = Path.Combine(testOutputPath, "InvalidProject");
            StorageFile file = await StorageFile.GetFileFromPathAsync(Path.Combine(projectPath, "InvalidProject.stbx"));
            string author = "Test Author";
            int templateIndex = 0;

            // Act
            await _outlineService.CreateModel(invalidProjectName, author, templateIndex);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task CreateModel_InvalidProjectPath_ThrowsArgumentException()
        {
            // Arrange
            string projectName = "TestProject";
            string invalidProjectPath = string.Empty;
            StorageFile file = await StorageFile.GetFileFromPathAsync(Path.Combine(invalidProjectPath, "TestProject.stbx"));
            string author = "Test Author";
            int templateIndex = 0;

            // Act
            await _outlineService.CreateModel(projectName, author, templateIndex);
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public async Task CreateModel_DuplicateProjectPath_ThrowsIOException()
        {
            // Arrange
            string projectName = "DuplicateTestProject";
            string projectPath = Path.Combine(testOutputPath, projectName);
            Directory.CreateDirectory(projectPath);
            StorageFile file = await StorageFile.GetFileFromPathAsync(Path.Combine(projectPath, "DuplicateTestProject.stbx"));
            string author = "Test Author";
            int templateIndex = 0;

            // Act
            await _outlineService.CreateModel(projectName, author, templateIndex);

            // Attempt to create another model with the same path
            await _outlineService.CreateModel(projectName, author, templateIndex);
        }
    }
}
