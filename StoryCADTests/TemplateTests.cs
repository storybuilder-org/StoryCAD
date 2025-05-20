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
        FileOpenVM fileVm = Ioc.Default.GetRequiredService<FileOpenVM>();

        // Validate templates
        for (int index = 0; index <= 5; index++)
        {
            var model = await outline!.CreateModel($"Test{index}", "I Robot", index);
            Assert.IsNotNull(model);
            foreach (StoryNodeItem item in model.ExplorerView[0].Children)
            {
                CheckChildren(item);
            }
        }

        // Validate bundled sample projects
        for (int i = 0; i < fileVm.SampleNames.Count; i++)
        {
            fileVm.SelectedSampleIndex = i;
            await fileVm.OpenSample();
            var model = Ioc.Default.GetRequiredService<OutlineViewModel>().StoryModel;
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
