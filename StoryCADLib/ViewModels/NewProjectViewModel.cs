using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.Services;

namespace StoryCAD.ViewModels;

public class NewProjectViewModel : ObservableRecipient
{
    private readonly PreferenceService _preferenceService;

    private string _parentPathName;
    private string _projectName;

    // Constructor for XAML compatibility - will be removed later
    public NewProjectViewModel() : this(Ioc.Default.GetRequiredService<PreferenceService>())
    {
    }

    public NewProjectViewModel(PreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
        ProjectName = string.Empty;
        var _prefs = _preferenceService.Model;
        ParentPathName = _prefs.ProjectDirectory;
    }

    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    public string ParentPathName
    {
        get => _parentPathName;
        set => SetProperty(ref _parentPathName, value);
    }
}
