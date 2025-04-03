using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using StoryCAD.DAL;
using StoryCAD.Services;
using StoryCAD.Services.Dialogs;
using StoryCAD.ViewModels.SubViewModels;
using Windows.Storage;
using StoryCAD.Services.Outline;

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
	private readonly LogService Logger = Ioc.Default.GetRequiredService<LogService>();
	private readonly ShellViewModel _shell = Ioc.Default.GetService<ShellViewModel>();
    private readonly OutlineViewModel _outlineVm = Ioc.Default.GetService<OutlineViewModel>();
    private readonly PreferenceService Preferences = Ioc.Default.GetService<PreferenceService>();

    #region Properties
    public Visibility RecentsTabContentVisibilty { get; set; }
    public Visibility SamplesTabContentVisibilty { get; set; }
    public Visibility NewTabContentVisibilty { get; set; }

    /// <summary>
    /// Internal names of samples
    /// (used to resolve sample names to actual files)
    /// </summary>
    private List<string> SamplePaths;

    private List<string> _SampleNames;
    /// <summary>
    /// List of all installed samples
    /// </summary>
    public List<string> SampleNames
    {
        get => _SampleNames;
        set => SetProperty(ref _SampleNames, value);
    }

    private string _TitleText;
    /// <summary>
    /// Controls the text in the title bar of the menu
    /// </summary>
    public string TitleText
    {
        get => _TitleText;
        set => SetProperty(ref _TitleText, value);
    }

    private string _ConfirmButtonText;
    /// <summary>
    /// Controls the text in the title bar of the menu
    /// </summary>
    public string ConfirmButtonText
    {
        get => _ConfirmButtonText;
        set => SetProperty(ref _ConfirmButtonText, value);
    }

    private string _WarningText;
    /// <summary>
    /// Controls the text in the warning
    /// </summary>
    public string WarningText
    {
        get => _WarningText;
        set => SetProperty(ref _WarningText, value);
    }

    private bool _ShowWarning;
    /// <summary>
    /// Controls the visibility of the warning
    /// </summary>
    public bool ShowWarning
    {
        get => _ShowWarning;
        set => SetProperty(ref _ShowWarning, value);
    }


    private int _selectedSampleIndex;
    public int SelectedSampleIndex
    {
        get => _selectedSampleIndex;
        set => SetProperty(ref _selectedSampleIndex, value);
    }
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

    public List<string> RecentlyOpenedFiles = new();

    private NavigationViewItem _currentTab;
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
                    RecentsTabContentVisibilty = Visibility.Visible;
                    SamplesTabContentVisibilty = Visibility.Collapsed;
                    NewTabContentVisibilty = Visibility.Collapsed;
                    ShowWarning = false;
                    break;
                case "Sample":
                    TitleText = "Sample outlines";
                    ConfirmButtonText = "Open sample";
                    WarningText = "Sample edits will be lost unless you save them elsewhere.";
                    ShowWarning = true;
                    RecentsTabContentVisibilty = Visibility.Collapsed;
                    SamplesTabContentVisibilty = Visibility.Visible;
                    NewTabContentVisibilty = Visibility.Collapsed;
                    break;
                case "New":
                    TitleText = "New outline";
                    ConfirmButtonText = "Create outline";
                    ShowWarning = false;
                    RecentsTabContentVisibilty = Visibility.Collapsed;
                    SamplesTabContentVisibilty = Visibility.Collapsed;
                    NewTabContentVisibilty = Visibility.Visible;
                    break;
                default:
                    throw new NotImplementedException("Unexpected tag " + value.Tag);
            }

            OnPropertyChanged(nameof(RecentsTabContentVisibilty));
            OnPropertyChanged(nameof(SamplesTabContentVisibilty));
            OnPropertyChanged(nameof(NewTabContentVisibilty));
            SetProperty(ref _currentTab, value);
        }
    }

    public string ParentFolderPath { get; set; }
    #endregion

    public FileOpenVM()
    {
        SelectedRecentIndex = -1;
        ProjectName = string.Empty;
        ProjectPath = Ioc.Default.GetRequiredService<PreferenceService>().Model.ProjectDirectory;

        //Gets all samples in CadLib/Assets/Install/samples
        SamplePaths = Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(name => name.Contains("StoryCAD.Assets.Install.samples")).ToList();
        SampleNames = SamplePaths.Select(name => name.Split('.')[4].Replace('_', ' '))
            .ToList();


        var preferences = Ioc.Default.GetRequiredService<PreferenceService>();

        string[] RecentFiles =
        [
            preferences.Model.LastFile1,
            preferences.Model.LastFile2,
            preferences.Model.LastFile3,
            preferences.Model.LastFile4,
            preferences.Model.LastFile5
        ];
        foreach (string File in RecentFiles)
        {
            if (!string.IsNullOrWhiteSpace(File))
            {
                if (System.IO.File.Exists(File))
                {
                    //StackPanel Item = new();
                    //ToolTipService.SetToolTip(Item, File);
                    //Item.Width = 300;
                    //Item.Children.Add(new TextBlock { Text = Path.GetFileNameWithoutExtension(File), FontSize = 20 });
                    //Item.Children.Add(new TextBlock { Text = "Last edited: " + System.IO.File.GetLastWriteTime(File), FontSize = 10, VerticalAlignment = VerticalAlignment.Center });
                    RecentlyOpenedFiles.Add(File);
                }
            }
        }
    }

    public async Task<string> OpenSample()
    {
        if (SelectedSampleIndex == -1)
            return null;

        var resourceName = SamplePaths[SelectedSampleIndex];
        await using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(resourceStream);
        var content = await reader.ReadToEndAsync();

        var filePath = Path.Combine(Path.GetTempPath(), $"{SampleNames[SelectedSampleIndex]}.stbx");
        await File.WriteAllTextAsync(filePath, content);

        await Ioc.Default.GetService<OutlineViewModel>()!.OpenFile(filePath);
        return filePath;
    }


    public async void LoadStory()
    {
        await _outlineVm.OpenFile(); //Calls the open file in shell so it can load the file
    }


    /// <summary>
    /// This updates preferences.RecentFiles 1 through 5
    /// </summary>
    public async Task UpdateRecents(string path)
    {
        //If file is in list, remove it
        if (Preferences.Model.RecentFiles.Contains(path))
        {
            Preferences.Model.RecentFiles.Remove(path);
        }

        //Add to top of list.
        Preferences.Model.RecentFiles.Insert(0, path);

        //Cap at 25.
        if (Preferences.Model.RecentFiles.Count > 25)
        {
            Preferences.Model.RecentFiles = Preferences.Model.RecentFiles.Take(25).ToList();
        }

        //Persist.
        PreferencesIo _loader = new();
        await _loader.WritePreferences(Preferences.Model);
    }



    /// <summary>
    /// Browse click for new project
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void Browse_Click(object sender, RoutedEventArgs e)
    {
        // Find a home for the new project
        ShowWarning = false;
        StorageFolder folder = await Ioc.Default.GetRequiredService<Windowing>().ShowFolderPicker();
        if (folder != null)
        {
            //Test we have written perms
            try
            {
                var file = await folder.CreateFileAsync("StoryCAD" + DateTimeOffset.Now.ToUnixTimeSeconds());
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch
            {
                //No perms, force user to pick different folder
                ShowWarning = false;
                WarningText = "You can't save outlines to that folder";
                ProjectPath = "";
                return;
            }

            ParentFolderPath = folder.Path;
            ProjectPath = folder.Path;
        }
    }

    /// <summary>
    /// Handles closing menu and handling user action
    /// </summary>
    public async Task ConfirmClicked()
    {
        Close(); //Stop more dialog clicks

        //Track name of file we are opening
        string? FilePath;
        switch (CurrentTab.Tag)
        {
            //Open recent file and update list
            case "Recent":
                FilePath = RecentlyOpenedFiles[SelectedRecentIndex];
                await _outlineVm.OpenFile(FilePath);
                break;

            case "New":
                FilePath = Path.Combine(ProjectPath, ProjectName);
                if (StoryIO.IsValidPath(FilePath))
                {
                    Preferences.Model.LastSelectedTemplate = SelectedTemplateIndex;
                    await _outlineVm.CreateFile(this);
                }
                else
                {
                    ProjectName = "";
                    ProjectPath = Preferences.Model.ProjectDirectory;
                    ShowWarning = true;
                    WarningText = "Your outline name or folder has disallowed characters";
                    return;
                }
                break;

            case "Sample": //Open Sample
                FilePath = await OpenSample();
                break;
            default:
                throw new NotImplementedException("Unexpected tag " + CurrentTab.Tag);
        }

        if (FilePath != null && File.Exists(FilePath))
        {
            await UpdateRecents(FilePath);
        }

        PreferencesIo _loader = new();
        await _loader.WritePreferences(Preferences.Model);
    }

    public void Close() => Ioc.Default.GetRequiredService<Windowing>().CloseContentDialog();
}