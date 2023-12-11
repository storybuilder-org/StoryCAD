using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using StoryCAD.Models;
using StoryCAD.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

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
        ShellVM.StoryModel.ExplorerView.Add(_overviewNode);
        TrashCanModel _trash = new(ShellVM.StoryModel);
        StoryNodeItem _trashNode = new(_trash, null);
        ShellVM.StoryModel.ExplorerView.Add(_trashNode);     // The trashcan is the second root
        FolderModel _narrative = new("Narrative View", ShellVM.StoryModel, StoryItemType.Folder);
        StoryNodeItem _narrativeNode = new(_narrative, null) { IsRoot = true };
        ShellVM.StoryModel.NarratorView.Add(_narrativeNode);

        //Add three test nodes.
        CharacterModel _character = new("TestCharacter", ShellVM.StoryModel);
        ProblemModel _problem = new("TestProblem", ShellVM.StoryModel);
        SceneModel _scene = new(ShellVM.StoryModel) {Name="TestScene" };
        ShellVM.StoryModel.ExplorerView.Add(new(_character, _overviewNode,StoryItemType.Character));
        ShellVM.StoryModel.ExplorerView.Add(new(_problem, _overviewNode,StoryItemType.Problem));
        ShellVM.StoryModel.ExplorerView.Add(new(_scene, _overviewNode,StoryItemType.Scene));


        //Check is loaded correcly
        Assert.IsTrue(ShellVM.StoryModel.StoryElements.Count == 6);
        Assert.IsTrue(ShellVM.StoryModel.StoryElements[0].Type == StoryItemType.StoryOverview);

        //Becuase we have created a file in this way we must populate ProjectFolder and ProjectFile.
        Directory.CreateDirectory(ShellVM.StoryModel.ProjectPath);

        ShellVM.StoryModel.ProjectFolder = StorageFolder.GetFolderFromPathAsync(ShellVM.StoryModel.ProjectPath).GetAwaiter().GetResult();
        ShellVM.StoryModel.ProjectFile = ShellVM.StoryModel.ProjectFolder.CreateFileAsync("TestProject.stbx", CreationCollisionOption.ReplaceExisting).GetAwaiter().GetResult();


        ShellVM.SaveFile();
    }

}
                                                                                                                                                                                                                                                                    