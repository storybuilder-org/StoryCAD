using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace StoryCADTests;

[TestClass]
public class FileTests
{

    /// <summary>
    /// This creates a new STBX File to assure file creation works.
    /// </summary>
    [TestMethod]
    public void FileCreation()
    {
        //Get ShellVM and clear the StoryModel
        StoryModel StoryModel = new()
        {
            ProjectFilename ="TestProject.stbx",
            ProjectPath = Ioc.Default.GetRequiredService<AppState>().RootDirectory
        };

        OverviewModel _overview = new(Path.GetFileNameWithoutExtension("TestProject"), StoryModel)
        { DateCreated = DateTime.Today.ToString("yyyy-MM-dd"), Author = "StoryCAD Tests" };

        StoryNodeItem _overviewNode = new(_overview, null, StoryItemType.StoryOverview) {IsRoot = true };
        StoryModel.ExplorerView.Add(_overviewNode);
        TrashCanModel _trash = new(StoryModel);
        StoryNodeItem _trashNode = new(_trash, null);
        StoryModel.ExplorerView.Add(_trashNode);     // The trashcan is the second root
        FolderModel _narrative = new("Narrative View", StoryModel, StoryItemType.Folder);
        StoryNodeItem _narrativeNode = new(_narrative, null) { IsRoot = true };
        StoryModel.NarratorView.Add(_narrativeNode);

        //Add three test nodes.
        CharacterModel _character = new("TestCharacter", StoryModel);
        ProblemModel _problem = new("TestProblem", StoryModel);
        SceneModel _scene = new(StoryModel) {Name="TestScene" };
        StoryModel.ExplorerView.Add(new(_character, _overviewNode,StoryItemType.Character));
        StoryModel.ExplorerView.Add(new(_problem, _overviewNode,StoryItemType.Problem));
        StoryModel.ExplorerView.Add(new(_scene, _overviewNode,StoryItemType.Scene));


        //Check is loaded correctly
        Assert.IsTrue(StoryModel.StoryElements.Count == 6);
        Assert.IsTrue(StoryModel.StoryElements[0].Type == StoryItemType.StoryOverview);

        //Because we have created a file in this way we must populate ProjectFolder and ProjectFile.
        Directory.CreateDirectory(StoryModel.ProjectPath);

        //Populate file/folder vars.
        StoryModel.ProjectFolder = StorageFolder.GetFolderFromPathAsync(StoryModel.ProjectPath).GetAwaiter().GetResult();
        StoryModel.ProjectFile = StoryModel.ProjectFolder.CreateFileAsync("TestProject.stbx", CreationCollisionOption.ReplaceExisting).GetAwaiter().GetResult();

        //Write file.
        StoryWriter _wtr = Ioc.Default.GetRequiredService<StoryWriter>();
        _wtr.WriteFile(StoryModel.ProjectFile, StoryModel).GetAwaiter().GetResult();

        //Sleep to ensure file is written.
        Thread.Sleep(10000);

        //Check file was really written to the disk.
        Assert.IsTrue(File.Exists(Path.Combine(StoryModel.ProjectPath, StoryModel.ProjectFilename)));
    }


    /*
     * TODO: Finish this test.
    /// <summary>
    /// This tests a file load to ensure file creation works.
    /// </summary>
    [TestMethod]
    public async Task FileLoad()
    {
        // Arrange
        string filePath = @"C:\Users\RARI\Desktop\OpenTest.stbx"; // Ensure this file exists and is accessible
        Assert.IsTrue(System.IO.File.Exists(filePath), "Test file does not exist.");

        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
        StoryReader _rdr = Ioc.Default.GetRequiredService<StoryReader>();

        // Act
        StoryModel storyModel = await _rdr.ReadFile(file);

        // Assert
        Assert.AreEqual(3, storyModel.StoryElements.Count, "Story elements count mismatch."); 
    }*/

}
