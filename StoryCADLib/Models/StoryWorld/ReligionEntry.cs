using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCADLib.Models.StoryWorld;

/// <summary>
/// Entry for the Religions list tab.
/// Represents a religion or belief system.
/// </summary>
public partial class ReligionEntry : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _deities = string.Empty;
    [ObservableProperty] private string _beliefs = string.Empty;
    [ObservableProperty] private string _practices = string.Empty;
    [ObservableProperty] private string _organizations = string.Empty;
    [ObservableProperty] private string _creationMyths = string.Empty;
}
