using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;

namespace StoryBuilder.ViewModels;

public class NewProjectViewModel : ObservableRecipient
{
    private string _projectName;
    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    private string _parentPathName;
    public string ParentPathName
    {
        get => _parentPathName;
        set => SetProperty(ref _parentPathName, value);
    }

    public NewProjectViewModel()
    {
        ProjectName = string.Empty;
        PreferencesModel _prefs = GlobalData.Preferences;
        ParentPathName = _prefs.ProjectDirectory;
    }
}