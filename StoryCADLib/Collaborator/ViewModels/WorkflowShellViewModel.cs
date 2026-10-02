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
    /// Current Collaborator settings. Set by Collaborator on open, and by the settings
    /// dialog on Save. Assigning it re-reads <see cref="CollaboratorSettings.ShowCostDetails"/>
    /// into <see cref="IsCostVisible"/>, so the cost line appears or disappears immediately
    /// rather than waiting for Collaborator to be reopened.
    /// </summary>
    private CollaboratorSettings _currentSettings = CollaboratorSettings.Default;
    public CollaboratorSettings CurrentSettings
    {
        get => _currentSettings;
        set
        {
            _currentSettings = value ?? CollaboratorSettings.Default;
            IsCostVisible = _currentSettings.ShowCostDetails;
        }
    }

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

    /// <summary>
    /// Per-run cost line for the bottom status bar, formatted by CollaboratorLib's
    /// WorkflowCostTracker and passed across as a plain string — StoryCADLib cannot see
    /// ProxyCostInfo (the dependency runs CollaboratorLib -> StoryCADLib, never back).
    /// Cleared at the start of every run: a stale figure describing the previous run is
    /// worse than a blank. See devdocs/collaborator_workflow_cost_display_design.md.
    /// </summary>
    private string _costSummary = string.Empty;
    public string CostSummary
    {
        get => _costSummary;
        set
        {
            if (SetProperty(ref _costSummary, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasCost));
                OnPropertyChanged(nameof(CostVisibility));
            }
        }
    }

    /// <summary>
    /// Whether the cost line is shown at all, mirroring
    /// <see cref="CollaboratorSettings.ShowCostDetails"/> (off by default). Not gated on
    /// developer builds: this is a shipped user-facing option, so any user who wants to
    /// watch their credit spend can. Set through <see cref="CurrentSettings"/> in
    /// production; the internal setter exists so tests can pin either state directly.
    /// </summary>
    private bool _isCostVisible;
    internal bool IsCostVisible
    {
        get => _isCostVisible;
        set
        {
            if (SetProperty(ref _isCostVisible, value))
            {
                OnPropertyChanged(nameof(HasCost));
                OnPropertyChanged(nameof(CostVisibility));
            }
        }
    }

    /// <summary>True when <see cref="CostSummary"/> should show on the shell status bar.</summary>
    public bool HasCost => IsCostVisible && !string.IsNullOrEmpty(CostSummary);

    /// <summary>
    /// <see cref="HasCost"/> as a Visibility for x:Bind, which does not coerce bool and
    /// would otherwise need a converter added solely for this one line.
    /// </summary>
    public Microsoft.UI.Xaml.Visibility CostVisibility =>
        HasCost ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Runs the invoked workflow. Bound to ItemInvoked rather than SelectionChanged because
    /// SelectionChanged does not fire when the user taps the row they are already on, which
    /// left re-running a workflow on the next element impossible without first detouring
    /// through a different one (Try Again only repeats the same inputs). ItemInvoked fires on
    /// every tap, and — unlike SelectionChanged — is not raised by programmatic selection, so
    /// <see cref="RestoreSelection" /> re-highlighting after a menu rebuild cannot reach here.
    /// </summary>
    public async void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        // A star toggle can still invoke the item underneath it.
        if (SuppressWorkflowNavigation)
            return;

        var invoked = args.InvokedItemContainer as NavigationViewItem;
        var tag = invoked?.Tag;
        if (!ShouldRunWorkflowForSelection(tag))
            return;

        // Moved only for a real workflow row. Element-type group headers are invoked to expand
        // and collapse a band and carry no tag; parking CurrentItem on one would take the pane
        // highlight off the workflow the user is on, and RebuildWorkflowMenu reads
        // CurrentItem.Tag to put that highlight back after a run.
        CurrentItem = invoked;

        await OnWorkflowSelected(tag);
    }

    /// <summary>
    /// Decides whether an invoke is a genuine request to run a workflow. Separated from
    /// <see cref="NavView_ItemInvoked" /> because NavigationViewItemInvokedEventArgs cannot be
    /// constructed in a test, and the cost of getting this wrong is running a billed LLM call
    /// the user never asked for.
    ///
    /// There is deliberately no "same tag as last time" guard. Re-running the workflow you are
    /// already on is the normal way to walk one workflow down a tree of elements, and the guard
    /// that used to sit here made that click do nothing. It was only ever needed because
    /// SelectionChanged also fires for programmatic selection, which ItemInvoked does not.
    /// </summary>
    public bool ShouldRunWorkflowForSelection(object tag)
    {
        if (tag == null || OnWorkflowSelected == null)
            return false;

        // A star toggle is in flight; the invoke it produced is a side effect of the click,
        // not a workflow choice.
        if (SuppressWorkflowNavigation)
            return false;

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
            NavView.ItemInvoked -= NavView_ItemInvoked;
        }
        MenuItems.Clear();
        StarEntries.Clear();
        HasPendingUpdates = false;
        ActiveWorkflowName = string.Empty;
        SuppressWorkflowNavigation = false;
        OnStarsChanged = null;
        OnAcceptAll = null;
        OnReviewEach = null;
        OnTryAgain = null;
    }

    #endregion
}
