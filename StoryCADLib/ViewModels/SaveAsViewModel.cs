using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCADLib.ViewModels;

[Microsoft.UI.Xaml.Data.Bindable]
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
