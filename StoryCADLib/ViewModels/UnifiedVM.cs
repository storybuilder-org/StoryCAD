using System.Reflection;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StoryCAD.DAL;
using StoryCAD.Services;
using StoryCAD.Services.Dialogs;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.ViewModels;

/// <summary>
/// UnifiedMenu/UnifiedVM is the file open menu for StoryCAD.
/// It's shown when StoryCAD is first loaded besides from PreferenceInitialization
/// which will be shown first if Preferences.Initialized is false.
///
/// Unified Menu shows the most recent files, sample stories and
/// allows a user create a new story. 
/// </summary>
public class FileOpenVM : ObservableRecipient
{
	private readonly LogService Logger = Ioc.Default.GetRequiredService<LogService>();
	private readonly ShellViewModel _shell = Ioc.Default.GetService<ShellViewModel>();
    private readonly OutlineViewModel _outlineVm = Ioc.Default.GetService<OutlineViewModel>();
    private readonly PreferenceService Preferences = Ioc.Default.GetService<PreferenceService>();

    private Visibility _ProjectNameErrorVisibility;
    public Visibility ProjectNameErrorVisibility
    {
	    get => _ProjectNameErrorVisibility;
	    set => SetProperty(ref _ProjectNameErrorVisibility, value);
	}
    private Visibility _ProjectFolderErrorVisibilty;
    public Visibility ProjectFolderErrorVisibilty
	{
	    get => _ProjectFolderErrorVisibilty;
	    set => SetProperty(ref _ProjectFolderErrorVisibilty, value);
    }
    
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


    private NavigationViewItem _currentTab;
    public NavigationViewItem CurrentTab
    {
        get => _currentTab;
        set
        {
            switch (value.Tag)
            {
                case "Recent":
                    RecentsTabContentVisibilty = Visibility.Visible;
                    SamplesTabContentVisibilty = Visibility.Collapsed;
                    NewTabContentVisibilty = Visibility.Collapsed;
                    break;
                case "Sample":
                    RecentsTabContentVisibilty = Visibility.Collapsed;
                    SamplesTabContentVisibilty = Visibility.Visible;
                    NewTabContentVisibilty = Visibility.Collapsed;
                    break;
                case "New":
                    RecentsTabContentVisibilty = Visibility.Collapsed;
                    SamplesTabContentVisibilty = Visibility.Collapsed;
                    NewTabContentVisibilty = Visibility.Visible;
                    break;
                default:
                    throw new NotImplementedException("Unexpected tag" + value.Tag);
            }

            OnPropertyChanged(nameof(RecentsTabContentVisibilty));
            OnPropertyChanged(nameof(SamplesTabContentVisibilty));
            OnPropertyChanged(nameof(NewTabContentVisibilty));
            SetProperty(ref _currentTab, value);
        }
    }

    public FileOpenVM()
    {
        SelectedRecentIndex = -1;
        ProjectName = string.Empty;
        ProjectPath = Ioc.Default.GetRequiredService<PreferenceService>().Model.ProjectDirectory;

        //Gets all samples in CadLib/Assets/Install/samples
        SamplePaths = Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(name => name.Contains("StoryCAD.Assets.Install.samples")).ToList();
        SampleNames = SamplePaths .Select(name => name.Split('.')[4].Replace('_', ' '))
            .ToList();
    }

    public async Task OpenSample()
    {
        if (SelectedSampleIndex == -1)
            return;

        var resourceName = SamplePaths[SelectedSampleIndex];
        await using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(resourceStream);
        var content = await reader.ReadToEndAsync();

        var filePath = Path.Combine(Path.GetTempPath(), $"{SampleNames[SelectedSampleIndex]}.stbx");
        await File.WriteAllTextAsync(filePath, content);

        await Ioc.Default.GetService<OutlineViewModel>()!.OpenFile(filePath);
    }

    public UnifiedMenuPage.UpdateContentDelegate UpdateContent;

    /// <summary>
    /// Hides this menu.
    /// </summary>
    public void Hide()
    {
        _shell.CloseUnifiedCommand.Execute(null);
    }

    /// <summary>
    /// This controls the frame and sets it content.
    /// </summary>c
    /// <returns></returns>
    private StackPanel _contentView;
    public StackPanel ContentView
    {
        get => _contentView;
        set => SetProperty(ref _contentView, value);
    }

    public async void LoadStory()
    {
        await _outlineVm.OpenFile(); //Calls the open file in shell so it can load the file
        Hide();
    }

    /// <summary>
    /// Loads a story from the recent page
    /// </summary>
    public async void LoadRecentStory()
    {
        switch (SelectedRecentIndex)
        {
            case 0: await _outlineVm.OpenFile(Preferences.Model.LastFile1); break;
            case 1: await _outlineVm.OpenFile(Preferences.Model.LastFile2); break;
            case 2: await _outlineVm.OpenFile(Preferences.Model.LastFile3); break;
            case 3: await _outlineVm.OpenFile(Preferences.Model.LastFile4); break;
            case 4: await _outlineVm.OpenFile(Preferences.Model.LastFile5); break;
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
        Preferences.Model.LastSelectedTemplate = SelectedTemplateIndex;

        PreferencesIo _loader = new();
        await _loader.WritePreferences(Preferences.Model);
        await _shell.OutlineManager.CreateFile(this);
        Hide();

    }

    /// <summary>
    /// This updates preferences.RecentFiles 1 through 5
    /// </summary>
    public async void UpdateRecents(string path)
    {
		//TODO: Clean up this mess of a code below.
        if (path != Preferences.Model.LastFile1 && path != Preferences.Model.LastFile2 && path != Preferences.Model.LastFile3 && path != Preferences.Model.LastFile4 && path != Preferences.Model.LastFile5)
        {
            Preferences.Model.LastFile5 = Preferences.Model.LastFile4;
            Preferences.Model.LastFile4 = Preferences.Model.LastFile3;
            Preferences.Model.LastFile3 = Preferences.Model.LastFile2;
            Preferences.Model.LastFile2 = Preferences.Model.LastFile1;
            Preferences.Model.LastFile1 = path;
        }
        else //This shuffle the file used to the top
        {
            string[] _newRecents = Array.Empty<string>();
            if (path == Preferences.Model.LastFile2) { _newRecents = new[] { Preferences.Model.LastFile2, Preferences.Model.LastFile1, Preferences.Model.LastFile3, Preferences.Model.LastFile4, Preferences.Model.LastFile5 }; }
            else if (path == Preferences.Model.LastFile3) { _newRecents = new[] { Preferences.Model.LastFile3, Preferences.Model.LastFile1, Preferences.Model.LastFile2, Preferences.Model.LastFile4, Preferences.Model.LastFile5 }; }
            else if (path == Preferences.Model.LastFile4) { _newRecents = new[] { Preferences.Model.LastFile4, Preferences.Model.LastFile1, Preferences.Model.LastFile2, Preferences.Model.LastFile3, Preferences.Model.LastFile5 }; }
            else if (path == Preferences.Model.LastFile5) { _newRecents = new[] { Preferences.Model.LastFile5, Preferences.Model.LastFile1, Preferences.Model.LastFile2, Preferences.Model.LastFile3, Preferences.Model.LastFile4 }; }
                
            if (_newRecents.Length > 0)
            {
                Preferences.Model.LastFile1 = _newRecents[0];
                Preferences.Model.LastFile2 = _newRecents[1];
                Preferences.Model.LastFile3 = _newRecents[2];
                Preferences.Model.LastFile4 = _newRecents[3];
                Preferences.Model.LastFile5 = _newRecents[4];
            }
        }

        PreferencesIo _loader = new();
        await _loader.WritePreferences(Preferences.Model);
    }

    /// <summary>
    /// Checks the project filename/path are valid and then creates the project if valid.
    /// </summary>
    public void CheckValidity(object sender, RoutedEventArgs e)
	{
		Logger.Log(LogLevel.Info, $"Testing filename validity for {ProjectPath}\\{ProjectName}");

        if (StoryIO.IsValidPath(Path.Combine(ProjectPath, ProjectName)))
        {
            MakeProject();
        }
        else
        {
            ProjectName = "";
            ProjectPath = Preferences.Model.ProjectDirectory;
            ProjectNameErrorVisibility = Visibility.Visible;
        }
    }
}