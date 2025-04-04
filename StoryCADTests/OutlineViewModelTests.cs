using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.ViewModels.SubViewModels;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.Tools;
using StoryCAD.Models;

namespace StoryCADTests
{
    [TestClass]
    public class OutlineViewModelTests
    {
        private OutlineViewModel outlineVM;
        private OutlineService outlineService;

        [TestInitialize]
        public void Setup()
        {
            // Assume IoC is properly configured for tests.
            // If necessary, you can initialize or mock dependencies here.
            outlineVM = Ioc.Default.GetRequiredService<OutlineViewModel>();
            outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        }

        // Test for UnifiedNewFile method
        [TestMethod]
        public async Task TestUnifiedNewFile()
        {
            // Arrange
            // Create a stubbed UnifiedVM instance with test properties.
            FileOpenVM dialogVm = new FileOpenVM
            {
                OutlineName = "TestProject",
                OutlineFolder = System.IO.Path.GetTempPath() // Use temp path for testing
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
            //Create challenge path
            string file = Path.Combine(App.ResultsDir, Path.GetRandomFileName() + ".stbx");

            // Ensure the file does not exist before writing the model
            Assert.IsFalse(File.Exists(file), "File already exists.");
            outlineVM.StoryModelFile = file;
            outlineVM.StoryModel = await outlineService.CreateModel("Test", "StoryBuilder", 0);

            //Write, check exists.
            await outlineVM.WriteModel();
            Assert.IsTrue(File.Exists(file), "File not written.");
        }

        // Stub tests for methods that follow the lock/unlock _canExecuteCommands pattern.
        // These tests act as placeholders until the methods are moved and implemented in OutlineViewModel.


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
            await outlineVM.OpenFile(Path.Combine(App.InputDir, "CloseFileTest.stbx")); 
            Assert.IsTrue(outlineVM.StoryModel.StoryElements.Count != 0, "Story not loaded.");
            await outlineVM.CloseFile();
            Assert.IsTrue(outlineVM.StoryModel.StoryElements.Count == 0, "Story not closed.");
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
        public async Task TestDramaticSituationsTool()
        {
            var Shell = Ioc.Default.GetRequiredService<ShellViewModel>();
            outlineVM.StoryModel = await outlineService.CreateModel("Test", "StoryBuilder", 0);
            outlineVM.StoryModelFile = Path.Combine(App.ResultsDir, "Dramatic.stbx");
            Shell.SetCurrentView(StoryViewType.ExplorerView);
            //await outlineVM.OpenFile(Path.Combine(App.InputDir, "Full3.stbx"));
            Shell.RightTappedNode = outlineVM.StoryModel.StoryElements[0].Node;
            Ioc.Default.GetRequiredService<DramaticSituationsViewModel>().SituationName = "Abduction";
            await outlineVM.DramaticSituationsTool();
            Assert.IsTrue(outlineVM.StoryModel.StoryElements.Count > 2, "Dramatic situation not added.");
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
        public void TestSearchNodes()
        {
            // TODO: Invoke outlineVM.SearchNodes and check that nodes matching the filter are highlighted.
            Assert.Inconclusive("Test for SearchNodes not implemented.");
        }
    }
}
