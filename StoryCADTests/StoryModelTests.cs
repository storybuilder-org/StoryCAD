using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCADTests;

[TestClass]
public class StoryModelTests
{
    private StoryModel _storyModel;

    [TestMethod]
    public void StoryModelConstructorTest()
    {
        _storyModel = new StoryModel();
        Assert.IsNotNull(_storyModel);
        Assert.IsNotNull(_storyModel.StoryElements);
        Assert.AreEqual(0, _storyModel.StoryElements.Count);
        Assert.IsNotNull(_storyModel.ExplorerView);
        Assert.AreEqual(0, _storyModel.ExplorerView.Count);
        Assert.IsNotNull(_storyModel.NarratorView);
        Assert.AreEqual(0, _storyModel.NarratorView.Count);
        Assert.IsFalse(_storyModel.Changed);
    }
}
