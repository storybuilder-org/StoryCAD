using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCADLib.Models.StoryWorld;

/// <summary>
/// Entry for the Cultures list tab.
/// Represents a culture, milieu, or social environment.
/// For Consensus Reality stories, each entry is a milieu (e.g., Wall Street, police precinct).
/// </summary>
public partial class CultureEntry : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _values = string.Empty;
    [ObservableProperty] private string _customs = string.Empty;
    [ObservableProperty] private string _taboos = string.Empty;
    [ObservableProperty] private string _art = string.Empty;
    [ObservableProperty] private string _dailyLife = string.Empty;
    [ObservableProperty] private string _entertainment = string.Empty;
}
