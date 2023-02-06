using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using StoryBuilder.Models;

namespace StoryBuilderTests
{
    [TestClass]
    public class ListLoaderTests
    {
        private Dictionary<string, ObservableCollection<string>> lists = GlobalData.ListControlSource;

        [TestMethod]
        public void TestListLoaderLists()
        {
            Assert.AreEqual(65,lists.Count);
            // OverViewModel lists
            Assert.IsTrue(lists.ContainsKey("StoryType"));
            Assert.IsTrue(lists.ContainsKey("Voice"));
            Assert.IsTrue(lists.ContainsKey("Tense"));
            Assert.IsTrue(lists.ContainsKey("Genre"));
            Assert.IsTrue(lists.ContainsKey("Viewpoint"));
            Assert.IsTrue(lists.ContainsKey("LanguageStyle"));
            Assert.IsTrue(lists.ContainsKey("LiteraryTechnique"));
            Assert.IsTrue(lists.ContainsKey("LiteraryStyle"));
            // ProblemViewModel lists
            Assert.IsTrue(lists.ContainsKey("ProblemType"));
            Assert.IsTrue(lists.ContainsKey("ConflictType"));
            Assert.IsTrue(lists.ContainsKey("ProblemSubject"));
            Assert.IsTrue(lists.ContainsKey("ProblemSource"));
            Assert.IsTrue(lists.ContainsKey("Motive"));
            Assert.IsTrue(lists.ContainsKey("Goal"));
            Assert.IsTrue(lists.ContainsKey("Outcome"));
            Assert.IsTrue(lists.ContainsKey("Method"));
            Assert.IsTrue(lists.ContainsKey("Theme"));
            // CharacterViewModel lists
            Assert.IsTrue(lists.ContainsKey("Role"));
            Assert.IsTrue(lists.ContainsKey("StoryRole"));
            Assert.IsTrue(lists.ContainsKey("Archetype"));
            Assert.IsTrue(lists.ContainsKey("Build"));
            Assert.IsTrue(lists.ContainsKey("Country"));
            Assert.IsTrue(lists.ContainsKey("EyeColor"));
            Assert.IsTrue(lists.ContainsKey("HairColor"));
            Assert.IsTrue(lists.ContainsKey("Complexion"));
            Assert.IsTrue(lists.ContainsKey("Race"));
            Assert.IsTrue(lists.ContainsKey("Enneagram"));
            Assert.IsTrue(lists.ContainsKey("Intelligence"));
            Assert.IsTrue(lists.ContainsKey("Value"));
            Assert.IsTrue(lists.ContainsKey("MentalIllness"));
            Assert.IsTrue(lists.ContainsKey("Focus"));
            Assert.IsTrue(lists.ContainsKey("Adventurous"));
            Assert.IsTrue(lists.ContainsKey("Aggressiveness"));
            Assert.IsTrue(lists.ContainsKey("Confidence"));
            Assert.IsTrue(lists.ContainsKey("Conscientiousness"));
            Assert.IsTrue(lists.ContainsKey("Creativeness"));
            Assert.IsTrue(lists.ContainsKey("Dominance"));
            Assert.IsTrue(lists.ContainsKey("Enthusiasm"));
            Assert.IsTrue(lists.ContainsKey("Assurance"));
            Assert.IsTrue(lists.ContainsKey("Sensitivity"));
            Assert.IsTrue(lists.ContainsKey("Shrewdness"));
            Assert.IsTrue(lists.ContainsKey("Sociability"));
            Assert.IsTrue(lists.ContainsKey("Stability"));
            Assert.IsTrue(lists.ContainsKey("WoundCategory"));
            Assert.IsTrue(lists.ContainsKey("Wound"));
            // SettingViewModel lists
            Assert.IsTrue(lists.ContainsKey("Locale"));
            Assert.IsTrue(lists.ContainsKey("Season"));
            // PlotPointViewModel lists
            Assert.IsTrue(lists.ContainsKey("Viewpoint"));
            Assert.IsTrue(lists.ContainsKey("SceneType"));
            Assert.IsTrue(lists.ContainsKey("ScenePurpose"));
            Assert.IsTrue(lists.ContainsKey("StoryRole"));
            Assert.IsTrue(lists.ContainsKey("Emotion"));
            Assert.IsTrue(lists.ContainsKey("Goal"));
            Assert.IsTrue(lists.ContainsKey("Opposition"));
            Assert.IsTrue(lists.ContainsKey("Outcome"));
            Assert.IsTrue(lists.ContainsKey("Viewpoint"));
            Assert.IsTrue(lists.ContainsKey("ValueExchange"));
            //TODO: Test some specific key counts
        }
    }
}
