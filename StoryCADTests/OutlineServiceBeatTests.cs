using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StoryCADTests
{
    [TestClass]
    public class OutlineServiceBeatTests
    {
        private OutlineService _outlineService;
        private string _testOutputPath;

        [TestInitialize]
        public void Setup()
        {
            _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
            _testOutputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BeatTestOutputs");
            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, true);
            }
            Directory.CreateDirectory(_testOutputPath);
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
                new StructureBeatViewModel { Title = "B1", Description = "D1" },
                new StructureBeatViewModel { Title = "B2", Description = "D2" }
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
            var beats = new List<StructureBeatViewModel>
            {
                new StructureBeatViewModel { Title = "Intro", Description = "D1", Guid = Guid.NewGuid() },
                new StructureBeatViewModel { Title = "Middle", Description = "D2", Guid = Guid.NewGuid() }
            };
            string file = Path.Combine(_testOutputPath, "beats.json");

            _outlineService.SaveBeatsheet(file, "SheetDesc", beats);
            Assert.IsTrue(File.Exists(file));

            var loaded = _outlineService.LoadBeatsheet(file);
            Assert.AreEqual("SheetDesc", loaded.Description);
            Assert.AreEqual(beats.Count, loaded.Beats.Count);
            Assert.AreEqual("Intro", loaded.Beats[0].Title);
            Assert.AreEqual(Guid.Empty, loaded.Beats[0].Guid);
        }
    }
}
