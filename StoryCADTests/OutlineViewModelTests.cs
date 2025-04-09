using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.ViewModels.SubViewModels;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
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
        public async Task TestNewFileVM()
        {
            var filevm = Ioc.Default.GetRequiredService<FileOpenVM>();

            //Set files
            filevm.OutlineName = "NewFileTest.stbx";
            filevm.OutlineFolder = App.ResultsDir;
            filevm.SelectedTemplateIndex = 0;

            //Assert
            string file = await filevm.CreateFile();
            Assert.IsFalse(string.IsNullOrEmpty(file));
            Assert.IsTrue(File.Exists(file));
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
        public async Task TestSaveFileAs()
        {
            var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
            outlineVM.StoryModel = await outlineService.CreateModel("Test", "StoryBuilder", 0);
            outlineVM.StoryModelFile = Path.Combine(App.ResultsDir, "saveas.stbx");

            var saveasVM = Ioc.Default.GetRequiredService<SaveAsViewModel>();
            saveasVM.ProjectName = "SaveAsTest2.stbx";
            saveasVM.ProjectPathName = App.ResultsDir;
            outlineVM.SaveFileAs();

            Assert.IsTrue(outlineVM.StoryModelFile == Path.Combine(App.ResultsDir, "SaveAsTest2.stbx"));
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
        public async Task TestPrintCurrentNodeAsync()
        {
            // TODO: Setup a selected node and invoke PrintCurrentNodeAsync.
            Assert.Inconclusive("Test for PrintCurrentNodeAsync not implemented.");
        }

        [TestMethod]
        public void TestKeyQuestionsTool()
        {
            var keyQuestionsVM = Ioc.Default.GetRequiredService<KeyQuestionsViewModel>();
            string text = keyQuestionsVM.Question;
            keyQuestionsVM.NextQuestion();
            Assert.IsTrue(keyQuestionsVM.Question != text, "Key question did not change.");
        }

        [TestMethod]
        public void TestTopicsTool()
        {
            var topicVm = Ioc.Default.GetRequiredService<TopicsViewModel>();
            string title =  topicVm.SubTopicNote;
            topicVm.NextSubTopic();
            Assert.IsTrue(topicVm.SubTopicNote != title, "Topic name did not change.");
        }

        [TestMethod]
        public async Task TestMasterPlotTool()
        {
            //Create outline
            var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
            outlineVM.StoryModel = await outlineService.CreateModel("Test-Masterplots", "StoryBuilder", 0);
            outlineVM.StoryModelFile = Path.Combine(App.ResultsDir, "Masterplots.stbx");
            shell.SetCurrentView(StoryViewType.ExplorerView);
            shell.RightTappedNode = outlineVM.StoryModel.StoryElements[0].Node;


            //Setup plot vm
            var masterPlotsVM = Ioc.Default.GetRequiredService<MasterPlotsViewModel>();
            masterPlotsVM.PlotPatternName = masterPlotsVM.PlotPatternNames[4];

            //Run and assert
            await outlineVM.MasterPlotTool();
            Assert.IsTrue(outlineVM.StoryModel.StoryElements[3].Name  == masterPlotsVM.PlotPatternNames[4]);
        }

        [TestMethod]
        public async Task TestDramaticSituationsTool()
        {
            //Create outline
            var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
            outlineVM.StoryModel = await outlineService.CreateModel("Test", "StoryBuilder", 0);
            outlineVM.StoryModelFile = Path.Combine(App.ResultsDir, "Dramatic.stbx");

            //Set view
            shell.SetCurrentView(StoryViewType.ExplorerView);
            shell.RightTappedNode = outlineVM.StoryModel.StoryElements[0].Node;

            //Run scenario
            Ioc.Default.GetRequiredService<DramaticSituationsViewModel>().SituationName = "Abduction";
            await outlineVM.DramaticSituationsTool();
            Assert.IsTrue(outlineVM.StoryModel.StoryElements.Count > 2, "Dramatic situation not added.");
        }

        [TestMethod]
        public async Task TestStockScenesTool()
        {
            //Create outline
            var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
            outlineVM.StoryModel = await outlineService.CreateModel("TestStock", "StoryBuilder", 0);
            outlineVM.StoryModelFile = Path.Combine(App.ResultsDir, "Stock.stbx");

            //Set view
            shell.SetCurrentView(StoryViewType.ExplorerView);
            shell.RightTappedNode = outlineVM.StoryModel.StoryElements[0].Node;

            //run scenario
            var stockVM = Ioc.Default.GetRequiredService<StockScenesViewModel>();
            stockVM.SceneName = "The police join the chase";
            await outlineVM.StockScenesTool();


            Assert.IsTrue(outlineVM.StoryModel.StoryElements[3].Name == "The police join the chase",
                "Stock scene not added.");
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
