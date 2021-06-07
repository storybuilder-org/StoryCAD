using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

namespace StoryBuilderTests
{
    [TestClass]
    public class PlotPointViewModelTests
    {
        [TestMethod]
        public void TestConstructor()
        {
            //PlotPointViewModel vm = Ioc.Default.GetService<PlotPointViewModel>();
            PlotPointViewModel vm = new PlotPointViewModel();
            Assert.IsNotNull(vm);
            Assert.AreEqual(string.Empty,vm.Viewpoint);
            Assert.AreEqual(string.Empty,vm.Date);
            Assert.AreEqual(string.Empty,vm.Time);
            Assert.AreEqual(string.Empty,vm.Setting);
            Assert.AreEqual(string.Empty,vm.SceneType);
            Assert.AreEqual(string.Empty,vm.Char1);
            Assert.AreEqual(string.Empty,vm.Char2);
            Assert.AreEqual(string.Empty,vm.Char3);
            Assert.AreEqual(string.Empty,vm.Role1);
            Assert.AreEqual(string.Empty,vm.Role2);
            Assert.AreEqual(string.Empty,vm.Role3);
            Assert.AreEqual(string.Empty,vm.ScenePurpose);
            Assert.AreEqual(string.Empty,vm.ValueExchange);
            Assert.AreEqual(string.Empty,vm.Remarks);
            Assert.AreEqual(string.Empty,vm.Protagonist);
            Assert.AreEqual(string.Empty,vm.ProtagEmotion);
            Assert.AreEqual(string.Empty,vm.ProtagGoal);
            Assert.AreEqual(string.Empty,vm.Antagonist);
            Assert.AreEqual(string.Empty,vm.AntagEmotion);
            Assert.AreEqual(string.Empty,vm.AntagGoal);
            Assert.AreEqual(string.Empty,vm.Opposition);
            Assert.AreEqual(string.Empty,vm.Outcome);
            Assert.AreEqual(string.Empty,vm.Emotion);
            Assert.AreEqual(string.Empty,vm.NewGoal);
            Assert.AreEqual(string.Empty,vm.Events);
            Assert.AreEqual(string.Empty,vm.Consequences);
            Assert.AreEqual(string.Empty,vm.Significance);
            Assert.AreEqual(string.Empty,vm.Realization);
            Assert.AreEqual(string.Empty,vm.Review);

            Assert.AreEqual(vm.Notes,string.Empty);
            Assert.IsNotNull(vm.ViewpointList);
            Assert.IsNotNull(vm.SceneTypeList);
            Assert.IsNotNull(vm.ScenePurposeList);
            Assert.IsNotNull(vm.StoryRoleList);
            Assert.IsNotNull(vm.EmotionList);
            Assert.IsNotNull(vm.GoalList);
            Assert.IsNotNull(vm.OppositionList);
            Assert.IsNotNull(vm.OutcomeList);
            Assert.IsNotNull(vm.ViewpointList);
            Assert.IsNotNull(vm.ValueExchangeList);
            // Validation of list contents are in ListLoaderTests

            Assert.IsNotNull(vm.CharacterList1);
            Assert.AreEqual(0, vm.CharacterList1.Count);
            Assert.IsNotNull(vm.SettingList);
            Assert.AreEqual(0, vm.SettingList.Count);
            // Character and Setting lists are empty at this point
        }
    }
}