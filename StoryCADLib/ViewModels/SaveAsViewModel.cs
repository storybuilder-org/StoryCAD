using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.ViewModels;

public class SaveAsViewModel : ObservableRecipient
{
    #region Properties

    private string _projectName;

    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    private string _parentFolder;

    public string ParentFolder
    {
        get => _parentFolder;
        set => SetProperty(ref _parentFolder, value);
    }

    #endregion
}
