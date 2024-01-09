using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StoryCADTests;

[TestClass]
public class TemplateTests
{
    /// <summary>
    /// This tests if all the samples child nodes have parents
    /// </summary>
    [TestMethod]
    public void TestSamples()
    {
        ShellViewModel ShellVM = Ioc.Default.GetService<ShellViewModel>();
        AppState State = Ioc.Default.GetService<AppState>();
        for (int index = 0; index >= 5; index++) 
        {
            ShellVM.StoryModel = new();
            Task.Run(async () =>
            {
                await ShellVM.CreateTemplate($"Test{index}", State.RootDirectory, index);
            });

            Assert.IsNotNull(ShellVM.StoryModel);

            foreach (var item in ShellVM.StoryModel.ExplorerView)
            {
                Assert.IsTrue(ShellViewModel.RootNodeType(item) == StoryItemType.StoryOverview);
            }
        }
    }
}
