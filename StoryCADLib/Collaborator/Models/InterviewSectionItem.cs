using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCADLib.Collaborator.Models;

/// <summary>
/// One row in the interview section picker (#119). Mirrors CollaboratorLib's
/// InterviewSection; lives here because the XAML binds it, same as PendingUpdateItem.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class InterviewSectionItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Blurb { get; set; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
