using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using StoryCADLib.Services.Collaborator.Contracts;

namespace StoryCADLib.Collaborator.ViewModels;

/// <summary>
/// ViewModel for WorkflowShell - manages the navigation menu and shell-level operations
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class WorkflowShellViewModel : ObservableRecipient
{
    public WorkflowShellViewModel()
    {
        MenuItems = new ObservableCollection<NavigationViewItem>();
        SaveCommand = new RelayCommand(SaveOutline);
        ExitCommand = new RelayCommand(ExitCollaborator);
        TogglePaneCommand = new RelayCommand(TogglePane);
        AcceptAllCommand = new RelayCommand(() => OnAcceptAll?.Invoke());
        ReviewEachCommand = new RelayCommand(() => OnReviewEach?.Invoke());
        TryAgainCommand = new RelayCommand(async () =>
        {
            if (OnTryAgain != null)
                await OnTryAgain();
        });
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

    /// <summary>
    /// Short workflow name on the top bar (left). Uses the registry label
    /// (e.g. Premise), not the long ideation path title. Empty until a workflow is selected.
    /// </summary>
    private string _activeWorkflowName = string.Empty;
    public string ActiveWorkflowName
    {
        get => _activeWorkflowName;
        set => SetProperty(ref _activeWorkflowName, value ?? string.Empty);
    }

    /// <summary>
    /// True when the current workflow page has pending property updates.
    /// Enables Accept All / Review Each / Try Again on the top bar.
    /// </summary>
    private bool _hasPendingUpdates;
    public bool HasPendingUpdates
    {
        get => _hasPendingUpdates;
        set => SetProperty(ref _hasPendingUpdates, value);
    }

    /// <summary>
    /// Workflow list pane open (bound to NavigationView.IsPaneOpen).
    /// Toggled by the top-bar hamburger; built-in NavView toggle is hidden.
    /// </summary>
    private bool _isPaneOpen = true;
    public bool IsPaneOpen
    {
        get => _isPaneOpen;
        set => SetProperty(ref _isPaneOpen, value);
    }

    /// <summary>Wired by Collaborator to the active WorkflowViewModel actions.</summary>
    public Action OnAcceptAll { get; set; }
    public Action OnReviewEach { get; set; }
    public Func<Task> OnTryAgain { get; set; }

    /// <summary>
    /// Shell-level status (bottom status bar InfoBar). Visible when the content frame
    /// is empty — e.g. after gather cancel when chat is not available (#123).
    /// </summary>
    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set
        {
            if (SetProperty(ref _statusText, value ?? string.Empty))
                OnPropertyChanged(nameof(HasStatus));
        }
    }

    /// <summary>True when <see cref="StatusText"/> should show on the shell InfoBar.</summary>
    public bool HasStatus => !string.IsNullOrEmpty(StatusText);

    #endregion

    #region Navigation Methods

    public async void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        CurrentItem = args.SelectedItem as NavigationViewItem;
        var tag = CurrentItem?.Tag;
        if (tag == null || OnWorkflowSelected == null)
            return;

        // Re-selecting the tag we are already on is a restore, not a user request:
        // RestoreSelection re-highlights the same workflow after the menu is rebuilt,
        // and running it again there would re-execute the workflow on every rebuild.
        if (IsSameTag(tag, _selectedTag))
            return;

        _selectedTag = tag;
        await OnWorkflowSelected(tag);
    }

    /// <summary>
    /// Re-selects the menu item carrying <paramref name="tag"/> after the menu has been
    /// rebuilt. Rebuilding replaces every NavigationViewItem, so the previously selected
    /// container is gone and the pane would otherwise show nothing highlighted.
    /// </summary>
    public void RestoreSelection(object tag)
    {
        if (tag == null)
            return;

        foreach (var item in MenuItems)
        {
            if (IsSameTag(item.Tag, tag))
            {
                CurrentItem = item;
                return;
            }

            foreach (var child in item.MenuItems.OfType<NavigationViewItem>())
            {
                if (IsSameTag(child.Tag, tag))
                {
                    // Group must be open or the selected child is hidden.
                    item.IsExpanded = true;
                    CurrentItem = child;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Workflow tags are the shared <c>WorkflowRegistry</c> instances (reference equality);
    /// the outline-gaps tag is a string, so compare by value too.
    /// </summary>
    private static bool IsSameTag(object a, object b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is string sa && b is string sb)
            return string.Equals(sa, sb, StringComparison.Ordinal);
        return false;
    }

    /// <summary>Tag of the workflow last navigated to; guards restore against re-running it.</summary>
    private object _selectedTag;

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

    public RelayCommand TogglePaneCommand { get; }

    public RelayCommand AcceptAllCommand { get; }

    public RelayCommand ReviewEachCommand { get; }

    public RelayCommand TryAgainCommand { get; }

    private void TogglePane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

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
        HasPendingUpdates = false;
        ActiveWorkflowName = string.Empty;
        _selectedTag = null;
        OnAcceptAll = null;
        OnReviewEach = null;
        OnTryAgain = null;
    }

    #endregion
}
