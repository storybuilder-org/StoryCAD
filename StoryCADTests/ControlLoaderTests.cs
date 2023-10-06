using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;

namespace StoryCADTests
{
    [TestClass]
    public class ControlLoaderTests
    {
        [TestMethod]
        public void TestConflictTypes()
        {
            Assert.AreEqual(8, GlobalData.ConflictTypes.Keys.Count);
            Assert.AreEqual(65, GlobalData.RelationTypes.Count);
        }
    }
}
