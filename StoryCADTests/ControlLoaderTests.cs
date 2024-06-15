using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.ViewModels;

namespace StoryCADTests;

[TestClass]
public class ControlLoaderTests
{
    ControlData data = Ioc.Default.GetRequiredService<ControlData>();

    [TestMethod]
    public void TestConflictTypes()
    {
        Assert.AreEqual(8, data.ConflictTypes.Keys.Count);
        Assert.AreEqual(65, data.RelationTypes.Count);
    }
}
