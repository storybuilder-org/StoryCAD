using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.Models.Tools;
using StoryCAD.Services;

namespace StoryCAD.ViewModels;

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

    private readonly PreferenceService _preferenceService;

    // Constructor for XAML compatibility - will be removed later
    public NewProjectViewModel() : this(Ioc.Default.GetRequiredService<PreferenceService>())
    {
    }

    public NewProjectViewModel(PreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
        ProjectName = string.Empty;
        PreferencesModel _prefs = _preferenceService.Model;
        ParentPathName = _prefs.ProjectDirectory;
    }
}