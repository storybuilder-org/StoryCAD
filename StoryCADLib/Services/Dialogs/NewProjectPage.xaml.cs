using System;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.ViewModels;
using WinRT;
using System.Text.RegularExpressions;
using Windows.Networking;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;

namespace StoryCAD.Services.Dialogs;

public sealed partial class NewProjectPage : Page
{
    public NewProjectPage(UnifiedVM vm)
    {
        InitializeComponent();
        UnifiedVM = vm;
    }

    public UnifiedVM UnifiedVM;

    public bool BrowseButtonClicked { get; set; }
    public bool ProjectFolderExists { get; set; }
    public StorageFolder ParentFolder { get; set; }
    public string ParentFolderPath { get; set; }
    public string ProjectFolderPath { get; set; }

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        // Find a home for the new project
        StorageFolder folder = await Ioc.Default.GetService<Windowing>().ShowFolderPicker();
        if (folder != null)
        {
            ParentFolderPath = folder.Path;
            UnifiedVM.ProjectPath = folder.Path;
        }
    }

    private void CheckValidity(object sender, RoutedEventArgs e)
    {

        //Checks file path validity
        try { Directory.CreateDirectory(ProjectPathName.Text); }
        catch
        {
            ProjectPathName.Text = "";
            ProjectPathName.PlaceholderText = "You can't put files here!";
            return;
        }

        //Checks file name validity
        //NOTE: File.Create() Strips out anything past an illegal character (or throws an exception if it's just an illegal character)
        try
        {
            string testfile = Path.Combine(ProjectPathName.Text, ProjectName.Text);
            var x = Path.GetInvalidPathChars();
            //Check validity
            Regex BadChars = new("[" + Regex.Escape(Path.GetInvalidPathChars().ToString()) + "]");
            if (BadChars.IsMatch(testfile))
            {
                throw new FileNotFoundException("the filename is invalid.");
            }

            //Try creating file and then checking it exists.
            using (FileStream fs = File.Create(testfile)) { }

            if (!File.Exists(testfile))
                throw new FileNotFoundException("the filename was not found.");
            else
                File.Delete(testfile);

        }
        catch
        {
            ProjectName.Text = "";
            ProjectName.PlaceholderText = "You can't call your file that!";
            return;
        }

        UnifiedVM.MakeProject();
    }
}