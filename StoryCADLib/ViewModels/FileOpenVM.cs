﻿using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using StoryCAD.DAL;
using StoryCAD.Services;
using StoryCAD.ViewModels.SubViewModels;
using Windows.Storage;

namespace StoryCAD.ViewModels;

/// <summary>
/// FileOpenMenu is the file open menu for StoryCAD.
/// It's shown when StoryCAD is first loaded besides from PreferenceInitialization
/// which is shown first if Preferences.Initialized is false.
///
/// FileOpen Menu shows the most recent files, sample stories and
/// allows a user create a new story. 
/// </summary>
public class FileOpenVM : ObservableRecipient
{
	private readonly LogService _logger = Ioc.Default.GetRequiredService<LogService>();
    private readonly OutlineViewModel _outlineVm = Ioc.Default.GetService<OutlineViewModel>();
    private readonly PreferenceService _preferences = Ioc.Default.GetService<PreferenceService>();

    #region Properties
    public Visibility RecentsTabContentVisibility { get; set; }
    public Visibility SamplesTabContentVisibility { get; set; }
    public Visibility NewTabContentVisibility { get; set; }

    /// <summary>
    /// Internal names of samples
    /// (used to resolve sample names to actual files)
    /// </summary>
    private readonly List<string> _samplePaths;

    private List<string> _sampleNames;
    /// <summary>
    /// List of all installed samples
    /// </summary>
    public List<string> SampleNames
    {
        get => _sampleNames;
        set => SetProperty(ref _sampleNames, value);
    }

    private string _titleText;
    /// <summary>
    /// Controls the text in the title bar of the menu
    /// </summary>
    public string TitleText
    {
        get => _titleText;
        set => SetProperty(ref _titleText, value);
    }

    private string _confirmButtonText;
    /// <summary>
    /// Controls the text in the title bar of the menu
    /// </summary>
    public string ConfirmButtonText
    {
        get => _confirmButtonText;
        set => SetProperty(ref _confirmButtonText, value);
    }

    private string _warningText;
    /// <summary>
    /// Controls the text in the warning
    /// </summary>
    public string WarningText
    {
        get => _warningText;
        set => SetProperty(ref _warningText, value);
    }

    private bool _showWarning;
    /// <summary>
    /// Controls the visibility of the warning
    /// </summary>
    public bool ShowWarning
    {
        get => _showWarning;
        set => SetProperty(ref _showWarning, value);
    }

    private int _selectedSampleIndex;
    /// <summary>
    /// Index of currently selected sample in the UI
    /// </summary>
    public int SelectedSampleIndex
    {
        get => _selectedSampleIndex;
        set => SetProperty(ref _selectedSampleIndex, value);
    }
    private int _selectedRecentIndex;
    /// <summary>
    /// Index of currently selected recent file in the UI
    /// </summary>
    public int SelectedRecentIndex
    {
        get => _selectedRecentIndex;
        set => SetProperty(ref _selectedRecentIndex, value);
    }

    private int _selectedTemplateIndex;
    /// <summary>
    /// Index of currently selected template in the UI
    /// </summary>
    public int SelectedTemplateIndex
    {
        get => _selectedTemplateIndex;
        set => SetProperty(ref _selectedTemplateIndex, value);
    }

    private string _outlineName;
    /// <summary>
    /// Project file name
    /// </summary>
    public string OutlineName
    {
        get => _outlineName;
        set => SetProperty(ref _outlineName, value);
    }

    private string _outlineFolder;
    /// <summary>
    /// Project path (containing folder)
    /// </summary>
    public string OutlineFolder
    {
        get => _outlineFolder;
        set => SetProperty(ref _outlineFolder, value);
    }

    private List<StackPanel> _recentsUI = [];
    public List<StackPanel> RecentsUI
    {
        get => _recentsUI;
        set => SetProperty(ref _recentsUI, value);
    }

    private NavigationViewItem _currentTab;

    /// <summary>
    /// Currently selected Tab
    /// </summary>
    public NavigationViewItem CurrentTab
    {
        get => _currentTab;
        set
        {
            switch (value.Tag)
            {
                case "Recent":
                    TitleText = "Recently opened outlines";
                    ConfirmButtonText = "Open Outline";
                    RecentsTabContentVisibility = Visibility.Visible;
                    SamplesTabContentVisibility = Visibility.Collapsed;
                    NewTabContentVisibility = Visibility.Collapsed;
                    ShowWarning = false;
                    break;
                case "Sample":
                    TitleText = "Sample outlines";
                    ConfirmButtonText = "Open sample";
                    WarningText = "Sample edits will be lost unless you save them elsewhere.";
                    ShowWarning = true;
                    RecentsTabContentVisibility = Visibility.Collapsed;
                    SamplesTabContentVisibility = Visibility.Visible;
                    NewTabContentVisibility = Visibility.Collapsed;
                    break;
                case "New":
                    TitleText = "New outline";
                    ConfirmButtonText = "Create outline";
                    ShowWarning = false;
                    RecentsTabContentVisibility = Visibility.Collapsed;
                    SamplesTabContentVisibility = Visibility.Collapsed;
                    NewTabContentVisibility = Visibility.Visible;
                    break;
                default:
                    throw new NotImplementedException("Unexpected tag " + value.Tag);
            }

            OnPropertyChanged(nameof(RecentsTabContentVisibility));
            OnPropertyChanged(nameof(SamplesTabContentVisibility));
            OnPropertyChanged(nameof(NewTabContentVisibility));
            SetProperty(ref _currentTab, value);
        }
    }

    #endregion

    public FileOpenVM()
    {
        SelectedRecentIndex = -1;
        OutlineName = string.Empty;
        OutlineFolder = Ioc.Default.GetRequiredService<PreferenceService>().Model.ProjectDirectory;

        //Gets all samples in samples dir, paths is the manifest resource path
        _samplePaths = Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(name => name.Contains("StoryCAD.Assets.Install.samples")).ToList();

        //User friendly names
        SampleNames = _samplePaths.Select(name => name.Split('.')[4].Replace('_', ' '))
            .ToList();
    }

    /// <summary>
    /// Opens a sample file from the embedded resources
    /// </summary>
    /// <returns>filepath opened</returns>
    public async Task<string> OpenSample()
    {
        if (SelectedSampleIndex == -1)
            return null;

        _logger.Log(LogLevel.Info, "Opening sample file: " + SampleNames[SelectedSampleIndex]);
        var resourceName = _samplePaths[SelectedSampleIndex];
        //Get file stream
        await using Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        var content = await new StreamReader(resourceStream!).ReadToEndAsync();

        //Write to disk
        var filePath = Path.Combine(Path.GetTempPath(), $"{SampleNames[SelectedSampleIndex]}.stbx");
        await File.WriteAllTextAsync(filePath, content);

        //Open sample
        await Ioc.Default.GetService<OutlineViewModel>()!.OpenFile(filePath);
        return filePath;
    }


    /// <summary>
    /// Load a story from file
    /// </summary>
    public async Task LoadStoryFromFile()
    {
        Close();
        await _outlineVm.OpenFile();
    }


    /// <summary>
    /// This updates preferences.RecentFiles
    /// </summary>
    public async Task UpdateRecents(string path)
    {
        //If file is in list, remove it
        if (_preferences.Model.RecentFiles.Contains(path))
        {
            _preferences.Model.RecentFiles.Remove(path);
        }

        //Add to top of list.
        _preferences.Model.RecentFiles.Insert(0, path);

        //Cap at 25.
        if (_preferences.Model.RecentFiles.Count > 25)
        {
            _preferences.Model.RecentFiles = _preferences.Model.RecentFiles.Take(25).ToList();
        }

        //Persist.
        PreferencesIo loader = new();
        await loader.WritePreferences(_preferences.Model);
    }

    /// <summary>
    /// Browse click for new project
    /// </summary>
    public async Task Browse_Click(object sender, RoutedEventArgs e)
    {
        // Find a home for the new project
        ShowWarning = false;
        StorageFolder folder = await Ioc.Default.GetRequiredService<Windowing>().ShowFolderPicker();
        if (folder != null)
        {
            //Test we have written perms
            try
            {
                StorageFile file = await folder.CreateFileAsync("StoryCAD" + DateTimeOffset.Now.ToUnixTimeSeconds());
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch
            {
                //No perms, force user to pick different folder
                ShowWarning = false;
                WarningText = "You can't save outlines to that folder";
                OutlineFolder = "";
                return;
            }

            OutlineFolder = folder.Path;
        }
    }

    /// <summary>
    /// Handles closing menu and handling user action
    /// </summary>
    public async Task ConfirmClicked()
    {

        //Track name of file we are opening
        string filePath;
        switch (CurrentTab.Tag)
        {
            //Open recent file and update list
            case "Recent":
                filePath = _preferences.Model.RecentFiles[SelectedRecentIndex];
                await _outlineVm.OpenFile(filePath);
                break;

            case "New":
                filePath = Path.Combine(OutlineFolder, OutlineName);
                if (StoryIO.IsValidPath(filePath))
                {
                    _preferences.Model.LastSelectedTemplate = SelectedTemplateIndex;
                    await _outlineVm.CreateFile(this);
                }
                else
                {
                    OutlineName = "";
                    OutlineFolder = _preferences.Model.ProjectDirectory;
                    ShowWarning = true;
                    WarningText = "Your outline name or folder has disallowed characters";
                    return;
                }
                break;

            case "Sample": //Open Sample
                filePath = await OpenSample();
                break;
            default:
                throw new NotImplementedException("Unexpected tag " + CurrentTab.Tag);
        }

        if (filePath != null && File.Exists(filePath))
        {
            await UpdateRecents(filePath);
        }

        Close(); //Stop more dialog clicks

        PreferencesIo loader = new();
        await loader.WritePreferences(_preferences.Model);
    }

    public void Close() => Ioc.Default.GetRequiredService<Windowing>().CloseContentDialog();
}