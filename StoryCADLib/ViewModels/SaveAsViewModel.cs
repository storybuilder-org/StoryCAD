using Windows.Storage;
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

    private string _projectPathName;
    public string ProjectPathName
    {
        get => _projectPathName;
        set => SetProperty(ref _projectPathName, value);
    }
        
    private StorageFolder _parentFolder;
    public StorageFolder ParentFolder 
    {
        get => _parentFolder;
        set => _parentFolder = value; 
    }

    private string _saveAsProjectFolderPath;
    public string SaveAsProjectFolderPath 
    {
        get => _saveAsProjectFolderPath;
        set => _saveAsProjectFolderPath = value;
    }

    #endregion
}