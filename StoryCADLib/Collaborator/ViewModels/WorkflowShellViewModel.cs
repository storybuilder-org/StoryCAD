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

    /// <summary>
    /// Every registry workflow with its current starred state, for the Customize workflows
    /// dialog. Collaborator refreshes this whenever it rebuilds the menu.
    /// </summary>
    public ObservableCollection<WorkflowStarEntry> StarEntries { get; } = new();

    /// <summary>
    /// Callback invoked with the complete set of starred labels when the user toggles a star or
    /// saves the Customize workflows dialog. Collaborator persists the set and rebuilds the menu.
    /// </summary>
    public Func<IEnumerable<string>, Task> OnStarsChanged { get; set; }

    /// <summary>
    /// Set while a star toggle is being handled, to stop the toggle from being treated as a
    /// workflow choice. Clicking a control inside a NavigationViewItem can still invoke the
    /// item, and WinUI and Skia do not agree on whether it does; invoking a workflow item runs
    /// the workflow, which is a billed LLM call the user did not ask for. The flag makes the
    /// outcome the same on both.
    /// </summary>
    public bool SuppressWorkflowNavigation { get; set; }

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
        // A star toggle can still invoke the item underneath it. Leaving CurrentItem alone keeps
        // the pane highlight — and the tag the following rebuild restores — on the workflow the
        // user is actually on, instead of moving it to whatever row they starred.
        if (SuppressWorkflowNavigation)
            return;

        CurrentItem = args.SelectedItem as NavigationViewItem;
        var tag = CurrentItem?.Tag;
        if (!ShouldRunWorkflowForSelection(tag))
            return;

        await OnWorkflowSelected(tag);
    }

    /// <summary>
    /// Decides whether a selection change is a genuine request to run a workflow, and records
    /// the tag when it is. Separated from <see cref="NavView_SelectionChanged" /> because
    /// NavigationViewSelectionChangedEventArgs cannot be constructed in a test, and the cost of
    /// getting this wrong is running a billed LLM call the user never asked for.
    /// </summary>
    public bool ShouldRunWorkflowForSelection(object tag)
    {
        if (tag == null || OnWorkflowSelected == null)
            return false;

        // A star toggle is in flight; the selection it produced is a side effect of the click,
        // not a workflow choice.
        // _selectedTag is deliberately left alone: recording this tag would make the user's next
        // genuine click on the same workflow look like a restore and silently do nothing.
        if (SuppressWorkflowNavigation)
            return false;

        // Re-selecting the tag we are already on is a restore, not a user request:
        // RestoreSelection re-highlights the same workflow after the menu is rebuilt,
        // and running it again there would re-execute the workflow on every rebuild.
        if (IsSameTag(tag, _selectedTag))
            return false;

        _selectedTag = tag;
        return true;
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
        StarEntries.Clear();
        HasPendingUpdates = false;
        ActiveWorkflowName = string.Empty;
        SuppressWorkflowNavigation = false;
        _selectedTag = null;
        OnStarsChanged = null;
        OnAcceptAll = null;
        OnReviewEach = null;
        OnTryAgain = null;
    }

    #endregion
}
