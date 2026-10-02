using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCADLib.Collaborator.Models;
using StoryCADLib.Collaborator.ViewModels;

namespace StoryCADLib.Collaborator.Views;

/// <summary>
/// Outline gaps page — each missing field links to its helper workflow or host element (#107 phase 6).
/// </summary>
public sealed partial class GapWorkflowPage : Page
{
    public GapWorkflowPage()
    {
        InitializeComponent();
        DataContext = new GapWorkflowViewModel();
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(GapWorkflowViewModel.HasGroups) or nameof(GapWorkflowViewModel.Groups))
                UpdateEmptyVisibility();
        };
        UpdateEmptyVisibility();
    }

    public GapWorkflowViewModel ViewModel => DataContext as GapWorkflowViewModel;

    private void UpdateEmptyVisibility()
    {
        if (ViewModel == null) return;
        EmptyMessageText.Visibility = ViewModel.HasGroups ? Visibility.Collapsed : Visibility.Visible;
        GroupsList.Visibility = ViewModel.HasGroups ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ElementLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton { Tag: Guid guid })
            ViewModel?.OpenElementCommand.Execute(guid);
    }

    private void FieldLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton { Tag: GapFieldLink field } && ViewModel != null)
            _ = ViewModel.OpenFieldCommand.ExecuteAsync(field);
    }
}
