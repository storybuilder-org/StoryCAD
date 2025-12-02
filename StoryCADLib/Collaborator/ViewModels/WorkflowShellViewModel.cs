using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace StoryCADLib.Collaborator.ViewModels;

/// <summary>
/// ViewModel for WorkflowShell - manages the navigation menu and shell-level operations
/// </summary>
public partial class WorkflowShellViewModel : ObservableRecipient
{
    public WorkflowShellViewModel()
    {
        MenuItems = new ObservableCollection<NavigationViewItem>();
        ExitCommand = new RelayCommand(ExitCollaborator);
    }

    #region Properties

    public ObservableCollection<NavigationViewItem> MenuItems { get; set; }

    public Frame ContentFrame { get; set; }

    public NavigationView NavView { get; set; }

    private NavigationViewItem _currentItem;
    public NavigationViewItem CurrentItem
    {
        get => _currentItem;
        set => SetProperty(ref _currentItem, value);
    }

    public string Title { get; set; } = "Story Collaborator";

    #endregion

    #region Navigation Methods

    public void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        CurrentItem = args.SelectedItem as NavigationViewItem;
    }

    public Task LoadWorkflowMenuAsync()
    {
        MenuItems.Clear();
        MenuItems.Add(new NavigationViewItem { Content = "Workflow", Tag = "Workflow" });
        return Task.CompletedTask;
    }

    #endregion

    #region Commands

    public RelayCommand ExitCommand { get; }

    private void ExitCollaborator()
    {
        if (NavView != null)
        {
            NavView.SelectionChanged -= NavView_SelectionChanged;
        }
        MenuItems.Clear();
    }

    #endregion
}
