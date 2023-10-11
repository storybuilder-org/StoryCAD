using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.ViewModels;

namespace StoryCADTests
{
    [TestClass]
    public class ControlLoaderTests
    {
        [TestMethod]
        public void TestConflictTypes()
        {
            ControlData data =  Ioc.Default.GetRequiredService<ControlData>();
            Assert.AreEqual(8, data.ConflictTypes.Keys.Count);
            Assert.AreEqual(65, data.RelationTypes.Count);
        }
    }
}
