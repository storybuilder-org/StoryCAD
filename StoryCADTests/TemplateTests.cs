using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using StoryCAD.Models;
using StoryCAD.ViewModels;

namespace StoryCADTests;

[TestClass]
public class TemplateTests
{
    /// <summary>
    /// This tests if all the samples child nodes have parents
    /// </summary>
    [UITestMethod]
    public void TestSamples()
    {
        ShellViewModel ShellVM = Ioc.Default.GetService<ShellViewModel>();
        AppState State = Ioc.Default.GetService<AppState>();
        for (int index = 0; index <= 5; index++) 
        {
            ShellVM.StoryModel = new();
            ShellVM.CreateTemplate($"Test{index}", index);

            Assert.IsNotNull(ShellVM.StoryModel);

            foreach (StoryNodeItem item in ShellVM.StoryModel.ExplorerView[0].Children)
            {
                CheckChildren(item);
            }
        }
    }

    private void CheckChildren(StoryNodeItem item)
    {
        Assert.IsTrue(ShellViewModel.RootNodeType(item) != StoryItemType.Unknown);
        foreach (var child in item.Children)
        {
            CheckChildren(child);
        }
    }
}
