using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCADLib.Models.StoryWorld;

/// <summary>
/// Entry for the People/Species list tab.
/// Represents a species, race, or people group.
/// </summary>
public partial class SpeciesEntry : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _physicalTraits = string.Empty;
    [ObservableProperty] private string _lifespan = string.Empty;
    [ObservableProperty] private string _origins = string.Empty;
    [ObservableProperty] private string _socialStructure = string.Empty;
    [ObservableProperty] private string _diversity = string.Empty;
}
