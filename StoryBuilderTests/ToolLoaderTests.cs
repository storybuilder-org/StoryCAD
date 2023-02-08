using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using StoryBuilder.Models;

namespace StoryBuilderTests
{
    [TestClass]
    public class ToolLoaderTests
    {
        [TestMethod]
        public void TestToolCounts()
        {
            Assert.AreEqual(5,GlobalData.KeyQuestionsSource.Keys.Count);
            Assert.AreEqual(11,GlobalData.StockScenesSource.Keys.Count);
            Assert.AreEqual(9,GlobalData.TopicsSource.Count);
            Assert.AreEqual(22,GlobalData.MasterPlotsSource.Count);
            Assert.AreEqual(36,GlobalData.DramaticSituationsSource.Count);
            //TODO: Test some details (subcounts)
        }
    }
}
