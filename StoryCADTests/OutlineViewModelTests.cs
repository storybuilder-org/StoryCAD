using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.ViewModels;

namespace StoryCADTests
{
    [TestClass]
    public class OutlineViewModelTests
    {
        private OutlineViewModel outlineVM;

        [TestInitialize]
        public void Setup()
        {
            // Assume IoC is properly configured for tests.
            // If necessary, you can initialize or mock dependencies here.
            outlineVM = Ioc.Default.GetRequiredService<OutlineViewModel>();
        }

        // Test for UnifiedNewFile method
        [TestMethod]
        public async Task TestUnifiedNewFile()
        {
            // Arrange
            // Create a stubbed UnifiedVM instance with test properties.
            UnifiedVM dialogVm = new UnifiedVM
            {
                ProjectName = "TestProject",
                ProjectPath = System.IO.Path.GetTempPath() // Use temp path for testing
            };

            // Act
            await outlineVM.CreateFile(dialogVm);

            // Assert
            // TODO: Add assertions to verify the StoryModel is reset,
            // the file is created at the expected location, etc.
            Assert.Inconclusive("UnifiedNewFile test not implemented.");
        }

        // Test for WriteModel method
        [TestMethod]
        public async Task TestWriteModel()
        {
            // Act
            await outlineVM.WriteModel();

            // Assert
            // TODO: Verify that the StoryModel was written to the expected file path,
            // e.g. by checking file existence or contents.
            Assert.Inconclusive("WriteModel test not implemented.");
        }

        // Stub tests for methods that follow the lock/unlock _canExecuteCommands pattern.
        // These tests act as placeholders until the methods are moved and implemented in OutlineViewModel.

        [TestMethod]
        public void TestOpenFile()
        {
            // TODO: Invoke outlineVM.OpenFile (or its replacement) and verify behavior.
            Assert.Inconclusive("Test for OpenFile not implemented.");
        }

        [TestMethod]
        public void TestSaveFile()
        {
            // TODO: Invoke outlineVM.SaveFile and check that changes are saved.
            Assert.Inconclusive("Test for SaveFile not implemented.");
        }

        [TestMethod]
        public void TestSaveFileAs()
        {
            // TODO: Invoke outlineVM.SaveFileAs and verify the file path update.
            Assert.Inconclusive("Test for SaveFileAs not implemented.");
        }

        [TestMethod]
        public async Task TestCloseFile()
        {
            //Check we have the file loaded
            await outlineVM.OpenFile(Path.Combine(App.InputDir, "Full.stbx")); 
            Assert.IsTrue(outlineVM.StoryModel.StoryElements.Count != 0, "Story not loaded.");
            await outlineVM.CloseFile();
            Assert.IsTrue(outlineVM.StoryModel.StoryElements.Count == 0, "Story not closed.");
        }

        [TestMethod]
        public void TestExitApp()
        {
            // TODO: Simulate an exit scenario and verify any cleanup logic.
            Assert.Inconclusive("Test for ExitApp not implemented.");
        }

        [TestMethod]
        public void TestOpenUnifiedMenu()
        {
            // TODO: Invoke outlineVM.OpenUnifiedMenu and check that the UI dialog is handled.
            Assert.Inconclusive("Test for OpenUnifiedMenu not implemented.");
        }

        [TestMethod]
        public async Task TestPrintCurrentNodeAsync()
        {
            // TODO: Setup a selected node and invoke PrintCurrentNodeAsync.
            Assert.Inconclusive("Test for PrintCurrentNodeAsync not implemented.");
        }

        [TestMethod]
        public void TestKeyQuestionsTool()
        {
            // TODO: Invoke outlineVM.KeyQuestionsTool and verify expected changes.
            Assert.Inconclusive("Test for KeyQuestionsTool not implemented.");
        }

        [TestMethod]
        public void TestTopicsTool()
        {
            // TODO: Invoke outlineVM.TopicsTool and assert its side effects.
            Assert.Inconclusive("Test for TopicsTool not implemented.");
        }

        [TestMethod]
        public void TestMasterPlotTool()
        {
            // TODO: Invoke outlineVM.MasterPlotTool and validate that the master plot is inserted.
            Assert.Inconclusive("Test for MasterPlotTool not implemented.");
        }

        [TestMethod]
        public void TestDramaticSituationsTool()
        {
            // TODO: Invoke outlineVM.DramaticSituationsTool and check for proper handling.
            Assert.Inconclusive("Test for DramaticSituationsTool not implemented.");
        }

        [TestMethod]
        public void TestStockScenesTool()
        {
            // TODO: Invoke outlineVM.StockScenesTool and verify the scene insertion logic.
            Assert.Inconclusive("Test for StockScenesTool not implemented.");
        }

        [TestMethod]
        public void TestGenerateScrivenerReports()
        {
            // TODO: Invoke outlineVM.GenerateScrivenerReports and validate report generation.
            Assert.Inconclusive("Test for GenerateScrivenerReports not implemented.");
        }

        [TestMethod]
        public void TestLaunchGitHubPages()
        {
            // TODO: Invoke outlineVM.LaunchGitHubPages and verify that the correct URL is launched.
            Assert.Inconclusive("Test for LaunchGitHubPages not implemented.");
        }

        [TestMethod]
        public void TestSearchNodes()
        {
            // TODO: Invoke outlineVM.SearchNodes and check that nodes matching the filter are highlighted.
            Assert.Inconclusive("Test for SearchNodes not implemented.");
        }
    }
}
