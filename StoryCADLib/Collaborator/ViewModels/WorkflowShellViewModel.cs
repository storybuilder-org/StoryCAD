using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using StoryCADLib.Services.Collaborator.Contracts;

namespace StoryCADLib.Collaborator.ViewModels;

/// <summary>
/// ViewModel for WorkflowShell - manages the navigation menu and shell-level operations
/// </summary>
public partial class WorkflowShellViewModel : ObservableRecipient
{
    public WorkflowShellViewModel()
    {
        MenuItems = new ObservableCollection<NavigationViewItem>();
        SaveCommand = new RelayCommand(SaveOutline);
        ExitCommand = new RelayCommand(ExitCollaborator);
    }

    #region Properties

    public ObservableCollection<NavigationViewItem> MenuItems { get; set; }

    public Frame ContentFrame { get; set; }

    public NavigationView NavView { get; set; }

    /// <summary>
    /// Callback invoked when a workflow is selected in the navigation menu.
    /// Collaborator sets this to handle navigation to WorkflowPage.
    /// Async to support element gathering via dialogs before navigation.
    /// </summary>
    public Func<object, Task> OnWorkflowSelected { get; set; }

    /// <summary>
    /// Current Collaborator settings. Set by Collaborator on open.
    /// </summary>
    public CollaboratorSettings CurrentSettings { get; set; } = CollaboratorSettings.Default;

    /// <summary>
    /// Callback invoked when user changes settings in the dialog.
    /// Collaborator sets this to update its internal settings.
    /// </summary>
    public Action<CollaboratorSettings> OnSettingsChanged { get; set; }

    /// <summary>
    /// Callback invoked when user clicks Save button.
    /// Collaborator sets this to save the outline via API.
    /// </summary>
    public Action OnSave { get; set; }

    /// <summary>
    /// Callback invoked when user clicks Exit button.
    /// Collaborator sets this to handle cleanup before window close.
    /// </summary>
    public Action OnExit { get; set; }

    private NavigationViewItem _currentItem;
    public NavigationViewItem CurrentItem
    {
        get => _currentItem;
        set => SetProperty(ref _currentItem, value);
    }

    public string Title { get; set; } = "Story Collaborator";

    #endregion

    #region Navigation Methods

    public async void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        CurrentItem = args.SelectedItem as NavigationViewItem;
        if (CurrentItem?.Tag != null && OnWorkflowSelected != null)
        {
            await OnWorkflowSelected(CurrentItem.Tag);
        }
    }

    public Task LoadWorkflowMenuAsync()
    {
        MenuItems.Clear();
        MenuItems.Add(new NavigationViewItem { Content = "Workflow", Tag = "Workflow" });
        return Task.CompletedTask;
    }

    #endregion

    #region Commands

    public RelayCommand SaveCommand { get; }

    public RelayCommand ExitCommand { get; }

    private void SaveOutline()
    {
        OnSave?.Invoke();
    }

    private void ExitCollaborator()
    {
        OnExit?.Invoke();
        if (NavView != null)
        {
            NavView.SelectionChanged -= NavView_SelectionChanged;
        }
        MenuItems.Clear();
    }

    #endregion
}
