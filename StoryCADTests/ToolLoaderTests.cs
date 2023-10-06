using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using StoryCAD.Models;
using StoryCAD.Models.Tools;

namespace StoryCADTests
{
    [TestClass]
    public class ToolLoaderTests
    {
        [TestMethod]
        public void TestToolCounts()
        {
            ToolsData TD = Ioc.Default.GetService<ToolsData>();
            Assert.AreEqual(5, TD.KeyQuestionsSource.Keys.Count);
            Assert.AreEqual(11, TD.StockScenesSource.Keys.Count);
            Assert.AreEqual(9, TD.TopicsSource.Count);
            Assert.AreEqual(22, TD.MasterPlotsSource.Count);
            Assert.AreEqual(36, TD.DramaticSituationsSource.Count);
            //TODO: Test some details (subcounts)
        }
    }
}
