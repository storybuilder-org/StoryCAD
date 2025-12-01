using System.IO.Compression;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.DAL;
using StoryCADLib.Services;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Outline;

namespace StoryCADLib.ViewModels;

/// <summary>
///     FileOpenMenu is the file open menu for StoryCAD.
///     It's shown when StoryCAD is first loaded besides from PreferenceInitialization
///     which is shown first if Preferences.Initialized is false.
///     FileOpen Menu shows the most recent files, sample stories and
///     allows a user create a new story.
/// </summary>
public class FileOpenVM : ObservableRecipient
{
    private readonly FileCreateService _fileCreateService;
    private readonly FileOpenService _fileOpenService;
    private readonly ILogService _logger;
    private readonly PreferenceService _preferences;
    private readonly Windowing _windowing;

    public FileOpenVM(ILogService logger, FileOpenService fileOpenService, FileCreateService fileCreateService,
        PreferenceService preferences, Windowing windowing)
    {
        _logger = logger;
        _fileOpenService = fileOpenService;
        _fileCreateService = fileCreateService;
        _preferences = preferences;
        _windowing = windowing;

        SelectedRecentIndex = -1;
        OutlineName = string.Empty;
        OutlineFolder = _preferences.Model.ProjectDirectory;

        //Gets all samples in samples dir, paths is the manifest resource path
        _samplePaths = Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(name => name.Contains("StoryCADLib.Assets.Install.samples")).ToList();

        //User friendly names
        SampleNames = _samplePaths.Select(name => name.Split('.')[4].Replace('_', ' '))
            .ToList();

        //Initialize project template names
        ProjectTemplateNames =
        [
            "Blank Outline",
            "Overview and Story Problem",
            "Folders",
            "External and Internal Problems",
            "Protagonist and Antagonist",
            "Problems and Characters"
        ];

        // Initialize the ShowFilePickerOnStartup property from preferences
        _showFilePickerOnStartup = _preferences.Model.ShowFilePickerOnStartup;
    }

    /// <summary>
    ///     Opens a sample file from the embedded resources
    /// </summary>
    /// <returns>filepath opened</returns>
    public async Task<string> OpenSample()
    {
        if (SelectedSampleIndex == -1)
        {
            return null;
        }

        _logger.Log(LogLevel.Info, "Opening sample file: " + SampleNames[SelectedSampleIndex]);
        var resourceName = _samplePaths[SelectedSampleIndex];
        //Get file stream
        await using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        var content = await new StreamReader(resourceStream!).ReadToEndAsync();

        //Write to disk
        var filePath = Path.Combine(Path.GetTempPath(), $"{SampleNames[SelectedSampleIndex]}.stbx");
        await File.WriteAllTextAsync(filePath, content);

        //Open sample
        await _fileOpenService.OpenFile(filePath);
        NavigateToOverview();
        return filePath;
    }


    /// <summary>
    ///     Load a story from file
    /// </summary>
    public async void LoadStoryFromFile()
    {
        Close();
        await _fileOpenService.OpenFile();
        NavigateToOverview();
    }


    /// <summary>
    ///     This updates preferences.RecentFiles
    /// </summary>
    public async Task UpdateRecents(string path)
    {
        await _fileOpenService.UpdateRecents(path);
    }

    /// <summary>
    ///     Browse click for new project
    /// </summary>
    public async void Browse_Click(object sender, RoutedEventArgs e)
    {
        // Find a home for the new project
        ShowWarning = false;
        var folder = await Ioc.Default.GetRequiredService<Windowing>().ShowFolderPicker();
        if (folder != null)
        {
            //Test we have written perms
            try
            {
                var file = await folder.CreateFileAsync("StoryCAD" + DateTimeOffset.Now.ToUnixTimeSeconds());
#if WINDOWS10_0_18362_0_OR_GREATER
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
#else
                await file.DeleteAsync();
#endif
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
    ///     Handles closing menu and handling user action
    /// </summary>
    public async void ConfirmClicked()
    {
        //Track name of file we are opening
        string filePath;
        switch (CurrentTab.Tag)
        {
            //Open recent file and update list
            case "Recent":
                if (SelectedRecentIndex != -1)
                {
                    filePath = _preferences.Model.RecentFiles[SelectedRecentIndex];
                    await _fileOpenService.OpenFile(filePath);
                    NavigateToOverview();
                }
                else
                {
                    return;
                }

                break;

            case "New":
                filePath = await CreateFile();
                if (filePath == "")
                {
                    //Show warning if we have a bad file name
                    ShowWarning = true;
                    WarningText = "Your outline name or folder has disallowed characters";
                    return;
                }

                NavigateToOverview();
                break;

            case "Sample": //Open Sample
                filePath = await OpenSample();
                break;
            case "Backup":
                if (SelectedBackupIndex != -1)
                {
                    filePath = BackupPaths[SelectedBackupIndex];
                    OpenBackup(filePath);
                }
                else
                {
                    return;
                }

                break;
            default:
                throw new NotImplementedException("Unexpected tag " + CurrentTab.Tag);
        }

        if (filePath != null && File.Exists(filePath))
        {
            await UpdateRecents(filePath);
        }

        Close(); //Stop more dialog clicks
    }

    public async Task<string> CreateFile()
    {
        var filePath = Path.Combine(OutlineFolder, OutlineName);
        if (StoryIO.IsValidPath(filePath))
        {
            _preferences.Model.LastSelectedTemplate = SelectedTemplateIndex;
            var createdPath = await _fileCreateService.CreateFile(OutlineFolder, OutlineName, SelectedTemplateIndex);
            return createdPath ?? "";
        }

        return "";
    }

    public void Close() => Ioc.Default.GetRequiredService<Windowing>().CloseContentDialog();

    /// <summary>
    ///     Navigate to the Overview node after successfully opening a file
    /// </summary>
    private void NavigateToOverview()
    {
        var shellVm = Ioc.Default.GetRequiredService<ShellViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        if (appState.CurrentDocument?.Model?.ExplorerView?.Count > 0)
        {
            // Reset view state to Explorer View in case we were in Narrative View
            shellVm.SelectedView = "Story Explorer View";
            shellVm.CurrentView = "Story Explorer View";
            shellVm.CurrentViewType = StoryViewType.ExplorerView;

            shellVm.TreeViewNodeClicked(appState.CurrentDocument.Model.ExplorerView[0]);
        }
    }

    private async void OpenBackup(string zipPath)
    {
        // make a timestamped temp folder
        var tempRoot = Path.GetTempPath();
        var unpackDir = Path.Combine(
            tempRoot,
            Path.GetFileNameWithoutExtension(zipPath)
            + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
        );
        Directory.CreateDirectory(unpackDir);

        try
        {
            // unpack
            ZipFile.ExtractToDirectory(zipPath, unpackDir);

            // find the one file inside (recursive in case of subfolders)
            var files = Directory.GetFiles(unpackDir, "*", SearchOption.AllDirectories);
            if (files.Length != 1)
            {
                _logger.Log(LogLevel.Warn, $"Invalid backup {zipPath}, {files.Length} files found");
                _windowing.GlobalDispatcher.TryEnqueue(() =>
                    Messenger.Send(new StatusChangedMessage(new StatusMessage(
                        "Backup archive is invalid or contains multiple files.",
                        LogLevel.Warn))));
                return;
            }

            //Open backup
            await _fileOpenService.OpenFile(files[0]);
            NavigateToOverview();
        }
        catch (Exception ex)
        {
            _windowing.GlobalDispatcher.TryEnqueue(() =>
                Messenger.Send(new StatusChangedMessage(new StatusMessage(
                    "Failed to open backup. The file may be corrupt.",
                    LogLevel.Error))));
            _logger.LogException(LogLevel.Error, ex, $"Error opening backup {zipPath}");
        }
    }

    #region Properties

    public Visibility RecentsTabContentVisibility { get; set; }
    public Visibility SamplesTabContentVisibility { get; set; }
    public Visibility NewTabContentVisibility { get; set; }
    public Visibility BackupTabContentVisibility { get; set; }

    private bool _showFilePickerOnStartup;

    /// <summary>
    ///     Controls whether the file picker dialog is shown on startup
    /// </summary>
    public bool ShowFilePickerOnStartup
    {
        get => _showFilePickerOnStartup;
        set
        {
            if (SetProperty(ref _showFilePickerOnStartup, value))
            {
                // Update the preference immediately when changed
                _preferences.Model.ShowFilePickerOnStartup = value;
                Task.Run(async () =>
                {
                    var preferencesIo = new PreferencesIo();
                    await preferencesIo.WritePreferences(_preferences.Model);
                });
            }
        }
    }

    /// <summary>
    ///     Used to track which backup to open if needed.
    /// </summary>
    public string[] BackupPaths;

    /// <summary>
    ///     Internal names of samples
    ///     (used to resolve sample names to actual files)
    /// </summary>
    private readonly List<string> _samplePaths;

    private List<string> _sampleNames;

    /// <summary>
    ///     List of all installed samples
    /// </summary>
    public List<string> SampleNames
    {
        get => _sampleNames;
        set => SetProperty(ref _sampleNames, value);
    }

    private List<string> _projectTemplateNames;

    /// <summary>
    ///     List of project template names for creating new outlines
    /// </summary>
    public List<string> ProjectTemplateNames
    {
        get => _projectTemplateNames;
        set => SetProperty(ref _projectTemplateNames, value);
    }

    private string _titleText;

    /// <summary>
    ///     Controls the text in the title bar of the menu
    /// </summary>
    public string TitleText
    {
        get => _titleText;
        set => SetProperty(ref _titleText, value);
    }

    private string _confirmButtonText;

    /// <summary>
    ///     Controls the text in the title bar of the menu
    /// </summary>
    public string ConfirmButtonText
    {
        get => _confirmButtonText;
        set => SetProperty(ref _confirmButtonText, value);
    }

    private string _warningText;

    /// <summary>
    ///     Controls the text in the warning
    /// </summary>
    public string WarningText
    {
        get => _warningText;
        set => SetProperty(ref _warningText, value);
    }

    private bool _showWarning;

    /// <summary>
    ///     Controls the visibility of the warning
    /// </summary>
    public bool ShowWarning
    {
        get => _showWarning;
        set => SetProperty(ref _showWarning, value);
    }

    private int _selectedSampleIndex;

    /// <summary>
    ///     Index of currently selected sample in the UI
    /// </summary>
    public int SelectedSampleIndex
    {
        get => _selectedSampleIndex;
        set => SetProperty(ref _selectedSampleIndex, value);
    }

    private int _selectedRecentIndex;

    /// <summary>
    ///     Index of currently selected recent file in the UI
    /// </summary>
    public int SelectedRecentIndex
    {
        get => _selectedRecentIndex;
        set => SetProperty(ref _selectedRecentIndex, value);
    }

    private int _selectedTemplateIndex;

    /// <summary>
    ///     Index of currently selected template in the UI
    /// </summary>
    public int SelectedTemplateIndex
    {
        get => _selectedTemplateIndex;
        set => SetProperty(ref _selectedTemplateIndex, value);
    }

    private int _selectedBackupIndex;

    /// <summary>
    ///     Index of currently selected template in the UI
    /// </summary>
    public int SelectedBackupIndex
    {
        get => _selectedBackupIndex;
        set => SetProperty(ref _selectedBackupIndex, value);
    }

    private string _outlineName;

    /// <summary>
    ///     Project file name
    /// </summary>
    public string OutlineName
    {
        get => _outlineName;
        set => SetProperty(ref _outlineName, value);
    }

    private string _outlineFolder;

    /// <summary>
    ///     Project path (containing folder)
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

    private List<StackPanel> _backupUI = [];

    public List<StackPanel> BackupUI
    {
        get => _backupUI;
        set => SetProperty(ref _backupUI, value);
    }

    private NavigationViewItem _currentTab;

    /// <summary>
    ///     Currently selected Tab
    /// </summary>
    public NavigationViewItem CurrentTab
    {
        get => _currentTab;
        set
        {
            // Ignore items without tags (like the footer preference item)
            if (value?.Tag == null)
            {
                return;
            }

            switch (value.Tag)
            {
                case "Recent":
                    TitleText = "Recently opened outlines";
                    ConfirmButtonText = "Open Outline";
                    RecentsTabContentVisibility = Visibility.Visible;
                    SamplesTabContentVisibility = Visibility.Collapsed;
                    NewTabContentVisibility = Visibility.Collapsed;
                    ShowWarning = false;
                    BackupTabContentVisibility = Visibility.Collapsed;
                    break;
                case "Sample":
                    TitleText = "Sample outlines";
                    ConfirmButtonText = "Open sample";
                    WarningText = "Sample edits will be lost unless you save them elsewhere.";
                    ShowWarning = true;
                    RecentsTabContentVisibility = Visibility.Collapsed;
                    SamplesTabContentVisibility = Visibility.Visible;
                    NewTabContentVisibility = Visibility.Collapsed;
                    BackupTabContentVisibility = Visibility.Collapsed;
                    break;
                case "New":
                    TitleText = "New outline";
                    ConfirmButtonText = "Create outline";
                    ShowWarning = false;
                    RecentsTabContentVisibility = Visibility.Collapsed;
                    SamplesTabContentVisibility = Visibility.Collapsed;
                    NewTabContentVisibility = Visibility.Visible;
                    BackupTabContentVisibility = Visibility.Collapsed;
                    break;
                case "Backup":
                    TitleText = "Restore a backup";
                    WarningText = "Sample edits will be lost unless you save them elsewhere.";
                    ShowWarning = true;
                    ConfirmButtonText = "Open backup";
                    RecentsTabContentVisibility = Visibility.Collapsed;
                    SamplesTabContentVisibility = Visibility.Collapsed;
                    NewTabContentVisibility = Visibility.Collapsed;
                    BackupTabContentVisibility = Visibility.Visible;
                    break;
                case "OpenFile":
                    LoadStoryFromFile();
                    return; // Don't update tab, just open file picker
                default:
                    throw new NotImplementedException("Unexpected tag " + value.Tag);
            }

            OnPropertyChanged(nameof(RecentsTabContentVisibility));
            OnPropertyChanged(nameof(SamplesTabContentVisibility));
            OnPropertyChanged(nameof(NewTabContentVisibility));
            OnPropertyChanged(nameof(BackupTabContentVisibility));
            SetProperty(ref _currentTab, value);
        }
    }

    #endregion
}
