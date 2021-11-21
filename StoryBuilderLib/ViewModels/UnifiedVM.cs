using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Dialogs;

namespace StoryBuilder.ViewModels
{
    public class UnifiedVM : ObservableRecipient
    {
        //Since the original menu was designed with the shell in mind we need to call some commands on the ShellVM so it can be done correctly
        ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();

        private int _selectedRecentIndex;
        public int SelectedRecentIndex
        {
            get => _selectedRecentIndex;
            set { SetProperty(ref _selectedRecentIndex, value); }
        }

        private int _selectedSampleIndex;
        public int SelectedSampleIndex
        {
            get => _selectedSampleIndex;
            set { SetProperty(ref _selectedSampleIndex, value); }
        }

        private string _selectedTemplate;
        public string SelectedTemplate
        {
            get => _selectedTemplate;
            set { SetProperty(ref _selectedTemplate, value); }
        }
        private string _projectName;
        public string ProjectName
        {
            get => _projectName;
            set { SetProperty(ref _projectName, value); }
        }

        private string _ProjectPath;
        public string ProjectPath
        {
            get => _ProjectPath;
            set { SetProperty(ref _ProjectPath, value); }
        }

        private ListBoxItem _currentTab;
        public ListBoxItem CurrentTab
        {
            get => _currentTab;
            set { SetProperty(ref _currentTab, value); }

        }

        public UnifiedVM()
        {
            SelectedRecentIndex = -1;
            ProjectName = string.Empty;
            PreferencesModel prefs = GlobalData.Preferences;
            ProjectPath = prefs.ProjectDirectory;
        }

        public UnifiedMenu.HideDelegate HideOpen;
        /// <summary>
        /// This controls the frame and sets it content.
        /// </summary>
        /// <returns></returns>
        private Frame _contentView = new();
        public Frame ContentView
        {
            get => _contentView;
            set { SetProperty(ref _contentView, value); }
        }

        /// <summary>
        /// This changes the content of the frame in unifiedUI.xaml depending on the selected option on the sidebar.
        /// </summary>
        public void SidebarChange(object sender, SelectionChangedEventArgs e)
        {
            switch (CurrentTab.Name)
            {
                case "Recent":
                    ContentView.Content = new Frame() { Content = new RecentFiles() };
                    break;
                case "New":
                    ContentView.Content = new Frame() { Content = new NewProjectPage() };
                    break;
                case "Example":
                    ContentView.Content = new Frame() { Content = new SamplePage() };
                    break;
            }
        }

        public void OpenPDFManual()
        {
            string ManualPath = System.IO.Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "Storybuilder", "Manual", "StoryBuilder User Manual.pdf");
            System.Diagnostics.Process PDF = new();
            PDF.StartInfo.UseShellExecute = true; //uses default app
            PDF.StartInfo.FileName = ManualPath;
            PDF.Start();
            HideOpen();
        }

        public async void LoadStory()
        {
            await shell.OpenFile(); //Calls the open file in shell so it can load the file

            HideOpen();
        }


     
        /// <summary>
        /// Loads a story from the recents page
        /// </summary>
        public async void LoadRecentStory()
        {
            switch (SelectedRecentIndex)
            {
                case 0: await shell.OpenFileFromPath(GlobalData.Preferences.LastFile1); UpdateRecents(GlobalData.Preferences.LastFile1); break;
                case 1: await shell.OpenFileFromPath(GlobalData.Preferences.LastFile2); UpdateRecents(GlobalData.Preferences.LastFile2); break;
                case 2: await shell.OpenFileFromPath(GlobalData.Preferences.LastFile3); UpdateRecents(GlobalData.Preferences.LastFile3); break;
                case 3: await shell.OpenFileFromPath(GlobalData.Preferences.LastFile4); UpdateRecents(GlobalData.Preferences.LastFile4); break;
                case 4: await shell.OpenFileFromPath(GlobalData.Preferences.LastFile5); UpdateRecents(GlobalData.Preferences.LastFile5); break;
            }
            if (SelectedRecentIndex != -1)
            {
                //Closing = true;
                HideOpen();
            }
        }

        /// <summary>
        /// Makes project and then closes UnifiedMenu
        /// </summary>
        public async void MakeProject()
        {
            await shell.UnifiedNewFile();
            UpdateRecents(System.IO.Path.Combine(ProjectPath, ProjectName));
            //Closing = true;
            HideOpen();
        }

        /// <summary>
        /// This updates prefs.RecentFiles1 through 5
        /// </summary>
        public async void UpdateRecents(string Path)
        {
            if (Path != GlobalData.Preferences.LastFile1 && Path != GlobalData.Preferences.LastFile2 && Path != GlobalData.Preferences.LastFile3 && Path != GlobalData.Preferences.LastFile4 && Path != GlobalData.Preferences.LastFile5)
            {
                GlobalData.Preferences.LastFile5 = GlobalData.Preferences.LastFile4;
                GlobalData.Preferences.LastFile4 = GlobalData.Preferences.LastFile3;
                GlobalData.Preferences.LastFile3 = GlobalData.Preferences.LastFile2;
                GlobalData.Preferences.LastFile2 = GlobalData.Preferences.LastFile1;
                GlobalData.Preferences.LastFile1 = Path;
            }
            else //This shuffle the file used to the top
            {
                List<String> NewRecents = new();
                if (Path == GlobalData.Preferences.LastFile1) { } //Do nothing since its the latest file loaded
                else if (Path == GlobalData.Preferences.LastFile2) { NewRecents = new List<string>() { GlobalData.Preferences.LastFile2, GlobalData.Preferences.LastFile1, GlobalData.Preferences.LastFile3, GlobalData.Preferences.LastFile4, GlobalData.Preferences.LastFile5 }; }
                else if (Path == GlobalData.Preferences.LastFile3) { NewRecents = new List<string>() { GlobalData.Preferences.LastFile3, GlobalData.Preferences.LastFile1, GlobalData.Preferences.LastFile2, GlobalData.Preferences.LastFile4, GlobalData.Preferences.LastFile5 }; }
                else if (Path == GlobalData.Preferences.LastFile4) { NewRecents = new List<string>() { GlobalData.Preferences.LastFile4, GlobalData.Preferences.LastFile1, GlobalData.Preferences.LastFile2, GlobalData.Preferences.LastFile3, GlobalData.Preferences.LastFile5 }; }
                else if (Path == GlobalData.Preferences.LastFile5) { NewRecents = new List<string>() { GlobalData.Preferences.LastFile5, GlobalData.Preferences.LastFile1, GlobalData.Preferences.LastFile2, GlobalData.Preferences.LastFile3, GlobalData.Preferences.LastFile4 }; }
            }
            string path = GlobalData.Preferences.InstallationDirectory;
            PreferencesIO loader = new PreferencesIO(GlobalData.Preferences, path);
            await loader.UpdateFile();
        }

    }
}