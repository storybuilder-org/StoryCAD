using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCADLib.Models.StoryWorld;

/// <summary>
/// Entry for the Physical World list tab.
/// Represents a world, planet, or realm in multi-world stories.
/// </summary>
public partial class PhysicalWorldEntry : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _geography = string.Empty;
    [ObservableProperty] private string _climate = string.Empty;
    [ObservableProperty] private string _naturalResources = string.Empty;
    [ObservableProperty] private string _flora = string.Empty;
    [ObservableProperty] private string _fauna = string.Empty;
    [ObservableProperty] private string _astronomy = string.Empty;
}
