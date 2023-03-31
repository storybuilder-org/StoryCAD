using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using StoryCAD.Models;

namespace StoryCADTests
{
    [TestClass]
    public class ControlLoaderTests
    {
        [TestMethod]
        public void TestConflictTypes()
        {
            Assert.AreEqual(6, GlobalData.ConflictTypes.Keys.Count);
            Assert.AreEqual(65, GlobalData.RelationTypes.Count);
        }
    }
}
