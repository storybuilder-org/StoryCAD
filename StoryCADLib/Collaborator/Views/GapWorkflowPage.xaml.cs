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
            if (e.PropertyName is nameof(GapWorkflowViewModel.HasGroups) or nameof(GapWorkflowViewModel.Groups)
                or nameof(GapWorkflowViewModel.HasMissingPieces) or nameof(GapWorkflowViewModel.ShowEmptyMessage))
                UpdateEmptyVisibility();
        };
        UpdateEmptyVisibility();
    }

    public GapWorkflowViewModel ViewModel => DataContext as GapWorkflowViewModel;

    private void UpdateEmptyVisibility()
    {
        if (ViewModel == null) return;
        EmptyMessageText.Visibility = ViewModel.ShowEmptyMessage ? Visibility.Visible : Visibility.Collapsed;
        GroupsList.Visibility = ViewModel.HasGroups ? Visibility.Visible : Visibility.Collapsed;
        // Collaborator #261: the heading and the list show only when there are missing pieces.
        var pieces = ViewModel.HasMissingPieces ? Visibility.Visible : Visibility.Collapsed;
        MissingPiecesHeading.Visibility = pieces;
        MissingPiecesList.Visibility = pieces;
    }

    private void MissingPieceElementLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton { Tag: Guid guid })
            ViewModel?.OpenElementCommand.Execute(guid);
    }

    private void MissingPieceWorkflowLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton { Tag: GapMissingPieceLink piece } && ViewModel != null)
            _ = ViewModel.OpenMissingPieceCommand.ExecuteAsync(piece);
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
