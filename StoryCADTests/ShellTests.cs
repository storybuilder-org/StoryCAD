using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.SubViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryCADTests;

[TestClass]
public class ShellTests
{
    /// <summary>
    /// This tests a fix from PR #1056 where moving a node after deletion
    /// crashes storycad.
    /// </summary>
    [TestMethod]
    public async Task TestDeleteMove()
    {
        //Create test outline
        var outlineService = Ioc.Default.GetService<OutlineService>();
        var outlineVM = Ioc.Default.GetService<OutlineViewModel>();
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        outlineVM.StoryModel = await outlineService.CreateModel("Test1056", "StoryBuilder", 2);
        outlineVM.StoryModelFile = Path.Combine(App.ResultsDir, "NullDelete.stbx");
        shell.DataSource = new() { outlineVM.StoryModel.StoryElements[0].Node,
            outlineVM.StoryModel.StoryElements
            .First(t => t.ElementType == StoryCAD.Models.StoryItemType.TrashCan).Node };

        //Create node to be deleted
        shell.CurrentNode = outlineVM.StoryModel.StoryElements
            .First(e => e.ElementType == StoryCAD.Models.StoryItemType.Folder
            && e.Name != "Narrative View").Node;
        shell.RightTappedNode = shell.CurrentNode;
        outlineVM.RemoveStoryElement();
        outlineVM.EmptyTrash();

        //Assert we have cleared the stuff that could go wrong
        Assert.IsNull(shell.CurrentNode);
        Assert.IsNull(shell.RightTappedNode);

    }
}
