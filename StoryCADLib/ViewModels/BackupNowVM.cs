using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.ViewModels;

/// <summary>
///     Viewmodel for the backup view model.
/// </summary>
public class BackupNowVM : ObservableRecipient
{
    private string _location;
    private string _name;

    /// <summary>
    ///     Name of backup
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>
    ///     Location of backup
    /// </summary>
    public string Location
    {
        get => _location;
        set => SetProperty(ref _location, value);
    }
}
