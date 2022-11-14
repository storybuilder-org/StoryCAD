using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Dialogs;

namespace StoryBuilder.ViewModels;

public class UnifiedVM : ObservableRecipient
{
    private ShellViewModel _shell = Ioc.Default.GetService<ShellViewModel>();

    private int _selectedRecentIndex;
    public int SelectedRecentIndex
    {
        get => _selectedRecentIndex;
        set => SetProperty(ref _selectedRecentIndex, value);
    }

    private int _selectedTemplateIndex;
    public int SelectedTemplateIndex
    {
        get => _selectedTemplateIndex;
        set => SetProperty(ref _selectedTemplateIndex, value);
    }

    private string _selectedTemplate;
    public string SelectedTemplate
    {
        get => _selectedTemplate;
        set => SetProperty(ref _selectedTemplate, value);
    }
    private string _projectName;
    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    private string _projectPath;
    public string ProjectPath
    {
        get => _projectPath;
        set => SetProperty(ref _projectPath, value);
    }

    /// <summary>
    /// This makes the UI one consistent color
    /// </summary>
    private SolidColorBrush _adjustmentColor;
    public SolidColorBrush AdjustmentColor
    {
        get => _adjustmentColor;
        set => SetProperty(ref _adjustmentColor, value);
    }

    private ListBoxItem _currentTab;
    public ListBoxItem CurrentTab
    {
        get => _currentTab;
        set => SetProperty(ref _currentTab, value);
    }

    public UnifiedVM()
    {
        SelectedRecentIndex = -1;
        ProjectName = string.Empty;
        PreferencesModel _prefs = GlobalData.Preferences;
        ProjectPath = _prefs.ProjectDirectory;
    }

    public UnifiedMenuPage.UpdateContentDelegate UpdateContent;

    public void Hide()
    {
        _shell.CloseUnifiedCommand.Execute(null);
    }

    /// <summary>
    /// This controls the frame and sets it content.
    /// </summary>
    /// <returns></returns>
    private StackPanel _contentView = new();
    public StackPanel ContentView
    {
        get => _contentView;
        set => SetProperty(ref _contentView, value);
    }

    /// <summary>
    /// This changes the content of the frame in unifiedUI.xaml depending on the selected option on the sidebar.
    /// </summary>
    public void SidebarChange(object sender, SelectionChangedEventArgs e)
    {
        ContentView.Children.Clear();
        UpdateContent();
    }

    public async void LoadStory()
    {
        await _shell.OpenFile(); //Calls the open file in shell so it can load the file
        Hide();
    }

    /// <summary>
    /// Loads a story from the recent page
    /// </summary>
    public async void LoadRecentStory()
    {
        switch (SelectedRecentIndex)
        {
            case 0: await _shell.OpenFile(GlobalData.Preferences.LastFile1); break;
            case 1: await _shell.OpenFile(GlobalData.Preferences.LastFile2); break;
            case 2: await _shell.OpenFile(GlobalData.Preferences.LastFile3); break;
            case 3: await _shell.OpenFile(GlobalData.Preferences.LastFile4); break;
            case 4: await _shell.OpenFile(GlobalData.Preferences.LastFile5); break;
        }
        if (SelectedRecentIndex != -1)
        {
            Hide();
        }
    }

    /// <summary>
    /// Makes project and then closes UnifiedMenu
    /// </summary>
    public async void MakeProject()
    {
        GlobalData.Preferences.LastSelectedTemplate = SelectedTemplateIndex;

        PreferencesIo _loader = new(GlobalData.Preferences, GlobalData.RootDirectory);
        await _loader.UpdateFile();
        await _shell.UnifiedNewFile(this);
        Hide();

    }

    /// <summary>
    /// This updates preferences.RecentFiles 1 through 5
    /// </summary>
    public async void UpdateRecents(string path)
    {
        if (path != GlobalData.Preferences.LastFile1 && path != GlobalData.Preferences.LastFile2 && path != GlobalData.Preferences.LastFile3 && path != GlobalData.Preferences.LastFile4 && path != GlobalData.Preferences.LastFile5)
        {
            GlobalData.Preferences.LastFile5 = GlobalData.Preferences.LastFile4;
            GlobalData.Preferences.LastFile4 = GlobalData.Preferences.LastFile3;
            GlobalData.Preferences.LastFile3 = GlobalData.Preferences.LastFile2;
            GlobalData.Preferences.LastFile2 = GlobalData.Preferences.LastFile1;
            GlobalData.Preferences.LastFile1 = path;
        }
        else //This shuffle the file used to the top
        {
            string[] _newRecents = Array.Empty<string>();
            if (path == GlobalData.Preferences.LastFile2) { _newRecents = new[] { GlobalData.Preferences.LastFile2, GlobalData.Preferences.LastFile1, GlobalData.Preferences.LastFile3, GlobalData.Preferences.LastFile4, GlobalData.Preferences.LastFile5 }; }
            else if (path == GlobalData.Preferences.LastFile3) { _newRecents = new[] { GlobalData.Preferences.LastFile3, GlobalData.Preferences.LastFile1, GlobalData.Preferences.LastFile2, GlobalData.Preferences.LastFile4, GlobalData.Preferences.LastFile5 }; }
            else if (path == GlobalData.Preferences.LastFile4) { _newRecents = new[] { GlobalData.Preferences.LastFile4, GlobalData.Preferences.LastFile1, GlobalData.Preferences.LastFile2, GlobalData.Preferences.LastFile3, GlobalData.Preferences.LastFile5 }; }
            else if (path == GlobalData.Preferences.LastFile5) { _newRecents = new[] { GlobalData.Preferences.LastFile5, GlobalData.Preferences.LastFile1, GlobalData.Preferences.LastFile2, GlobalData.Preferences.LastFile3, GlobalData.Preferences.LastFile4 }; }
                
            if (_newRecents.Length > 0)
            {
                GlobalData.Preferences.LastFile1 = _newRecents[0];
                GlobalData.Preferences.LastFile2 = _newRecents[1];
                GlobalData.Preferences.LastFile3 = _newRecents[2];
                GlobalData.Preferences.LastFile4 = _newRecents[3];
                GlobalData.Preferences.LastFile5 = _newRecents[4];
            }
        }

        PreferencesIo _loader = new(GlobalData.Preferences, GlobalData.RootDirectory);
        await _loader.UpdateFile();
    }

}