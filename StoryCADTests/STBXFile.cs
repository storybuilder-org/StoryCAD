using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using StoryCAD.Models;
using StoryCAD.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryCADTests;

[TestClass]
public class STBXFile
{

    [UITestMethod]
    public void FileCreation()
    {
        //Get ShellVM and clear the StoryModel
        ShellViewModel ShellVM = Ioc.Default.GetService<ShellViewModel>();
        ObservableCollection<string> Col = new();
        ShellVM.StoryModel = new()
        {
            ProjectFilename ="TestProject.stbx",
            ProjectPath = Ioc.Default.GetRequiredService<AppState>().RootDirectory
        };

        OverviewModel _overview = new(Path.GetFileNameWithoutExtension("TestProject"), ShellVM.StoryModel)
        { DateCreated = DateTime.Today.ToString("yyyy-MM-dd"), Author = "StoryCAD Tests" };

        StoryNodeItem _overviewNode = new(_overview, null, StoryItemType.StoryOverview) {IsRoot = true };
        //ShellVM.StoryModel.ExplorerView.Add(_overviewNode);
        TrashCanModel _trash = new(ShellVM.StoryModel);
        StoryNodeItem _trashNode = new(_trash, null);
        ShellVM.StoryModel.ExplorerView.Add(_trashNode);     // The trashcan is the second root
        FolderModel _narrative = new("Narrative View", ShellVM.StoryModel, StoryItemType.Folder);
        StoryNodeItem _narrativeNode = new(_narrative, null) { IsRoot = true };
        ShellVM.StoryModel.NarratorView.Add(_narrativeNode);

        //Check is loaded correcly
        Assert.IsTrue(ShellVM.StoryModel.StoryElements.Count == 3);
        Assert.IsTrue(ShellVM.StoryModel.StoryElements[0].Type == StoryItemType.StoryOverview);
    }

}
                                                                                                                                                                                                                                                                    