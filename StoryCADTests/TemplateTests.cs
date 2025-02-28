using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels;
using Windows.Storage;

namespace StoryCADTests;

[TestClass]
public class TemplateTests
{
    /// <summary>
    /// This tests if all the samples child nodes have parents
    /// </summary>
    [TestMethod]
    public async Task TestSamplesAsync()
    {
        OutlineService outline = Ioc.Default.GetService<OutlineService>();
        for (int index = 0; index <= 5; index++) 
        {
            StoryModel model = new();
            string path = Ioc.Default.GetRequiredService<AppState>().RootDirectory;
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            await outline!.CreateModel(file, $"Test{index}","I Robot", index);

            Assert.IsNotNull(model);

            foreach (StoryNodeItem item in model.ExplorerView[0].Children)
            {
                CheckChildren(item);
            }
        }
    }

    private void CheckChildren(StoryNodeItem item)
    {
        Assert.IsTrue(StoryNodeItem.RootNodeType(item) != StoryItemType.Unknown);
        foreach (var child in item.Children)
        {
            CheckChildren(child);
        }
    }
}
