using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCADLib.Models.StoryWorld;

/// <summary>
/// Entry for the Governments list tab.
/// Represents a government, faction, or power structure.
/// </summary>
public partial class GovernmentEntry : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _type = string.Empty;
    [ObservableProperty] private string _powerStructures = string.Empty;
    [ObservableProperty] private string _laws = string.Empty;
    [ObservableProperty] private string _classStructure = string.Empty;
    [ObservableProperty] private string _foreignRelations = string.Empty;
}
