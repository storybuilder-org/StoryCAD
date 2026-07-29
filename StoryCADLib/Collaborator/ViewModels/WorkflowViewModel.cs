using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoryCADLib.Collaborator.Models;

namespace StoryCADLib.Collaborator.ViewModels;

/// <summary>
/// ViewModel for WorkflowPage - manages the execution of a single workflow.
///
/// Navigation Data Pattern:
/// - Navigation passes WorkflowModel as data via NavigateRouteAsync
/// - Initialize(WorkflowModel) is called from WorkflowPage.OnNavigatedTo
/// - Do NOT pull from service in constructor - receive data from navigation
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class WorkflowViewModel : ObservableRecipient
{
    public WorkflowViewModel()
    {
        ConversationList = new ObservableCollection<ChatMessage>();
        PendingUpdateItems = new ObservableCollection<PendingUpdateItem>();

        // Existing commands
        AcceptCommand = new RelayCommand(SaveOutputs);
        SendCommand = new RelayCommand(async () => await SendButtonClicked());

        // Property update commands
        AcceptAllCommand = new RelayCommand(ExecuteAcceptAll);
        ReviewEachCommand = new RelayCommand(ExecuteReviewEach);
        TryAgainCommand = new RelayCommand(async () => await ExecuteTryAgain());

        // Review mode commands
        AcceptCurrentCommand = new RelayCommand(ExecuteAcceptCurrent);
        SkipCurrentCommand = new RelayCommand(ExecuteSkipCurrent);
        AcceptRemainingCommand = new RelayCommand(ExecuteAcceptRemaining);
    }

    public async Task InitializeAsync(object workflow)
    {
        if (workflow is null)
        {
            return;
        }

        Title = workflow.ToString();
        Description = string.Empty;
        Explanation = string.Empty;
        await ProcessWorkflow();
    }

    #region Properties

    public string Title { get; set; }

    public string Description { get; set; }

    public string Explanation { get; set; }

    public ObservableCollection<ChatMessage> ConversationList { get; set; }

    /// <summary>
    /// Callback invoked when user sends a chat message.
    /// Collaborator sets this to handle chat via Semantic Kernel.
    /// Returns the assistant's response.
    /// </summary>
    public Func<string, Task<string>> OnSendMessage { get; set; }

    private string _inputText;
    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    private string _promptOutput;
    public string PromptOutput
    {
        get => _promptOutput;
        set => SetProperty(ref _promptOutput, value);
    }

    private string _selectedElementsSummary;
    /// <summary>
    /// Summary of elements selected for this workflow (e.g., "Problem: Herold wants Greta")
    /// </summary>
    public string SelectedElementsSummary
    {
        get => _selectedElementsSummary;
        set => SetProperty(ref _selectedElementsSummary, value);
    }

    #endregion

    #region Pending Updates Properties

    /// <summary>
    /// Classified property updates for the panel (issue #116).
    /// </summary>
    public ObservableCollection<PendingUpdateItem> PendingUpdateItems { get; set; }

    /// <summary>True if there are updates to display (pending or applied).</summary>
    public bool HasUpdates => PendingUpdateItems?.Count > 0;

    /// <summary>True if updates exist and haven't been fully applied yet.</summary>
    public bool HasPendingUpdates => HasUpdates && !UpdatesApplied;

    public Microsoft.UI.Xaml.Visibility ReviewModeVisibility =>
        IsInReviewMode ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    private bool _isInReviewMode;
    /// <summary>True when user is reviewing updates one at a time.</summary>
    public bool IsInReviewMode
    {
        get => _isInReviewMode;
        set
        {
            if (SetProperty(ref _isInReviewMode, value))
            {
                OnPropertyChanged(nameof(ReviewModeVisibility));
            }
        }
    }

    private int _currentReviewIndex;
    /// <summary>Index of the property currently being reviewed (0-based).</summary>
    public int CurrentReviewIndex
    {
        get => _currentReviewIndex;
        set => SetProperty(ref _currentReviewIndex, value);
    }

    public string CurrentReviewKey =>
        IsInReviewMode && PendingUpdateItems?.Count > CurrentReviewIndex
            ? PendingUpdateItems[CurrentReviewIndex].Key
            : string.Empty;

    /// <summary>Proposed value for the row under review.</summary>
    public string CurrentReviewValue =>
        IsInReviewMode && PendingUpdateItems?.Count > CurrentReviewIndex
            ? PendingUpdateItems[CurrentReviewIndex].ProposedDisplay
            : string.Empty;

    /// <summary>Current outline value for the row under review.</summary>
    public string CurrentReviewExisting =>
        IsInReviewMode && PendingUpdateItems?.Count > CurrentReviewIndex
            ? (string.IsNullOrEmpty(PendingUpdateItems[CurrentReviewIndex].CurrentDisplay)
                ? "(empty)"
                : PendingUpdateItems[CurrentReviewIndex].CurrentDisplay)
            : string.Empty;

    public string CurrentReviewCraft =>
        IsInReviewMode && PendingUpdateItems?.Count > CurrentReviewIndex
            ? PendingUpdateItems[CurrentReviewIndex].CraftExplanation ?? string.Empty
            : string.Empty;

    public bool CurrentReviewHasCraft => !string.IsNullOrWhiteSpace(CurrentReviewCraft);

    public Microsoft.UI.Xaml.Visibility CurrentReviewCraftVisibility =>
        CurrentReviewHasCraft ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public string ReviewProgress => IsInReviewMode
        ? $"{CurrentReviewIndex + 1} of {PendingUpdateItems?.Count ?? 0}"
        : string.Empty;

    public Action OnAcceptAll { get; set; }
    public Func<Task> OnTryAgain { get; set; }
    public Action<string> OnAcceptProperty { get; set; }
    public Action<string> OnSkipProperty { get; set; }

    /// <summary>
    /// Accept remaining Fill/Refresh only; leave Protect rows (issue #116).
    /// </summary>
    public Action OnAcceptRemainingFree { get; set; }

    private bool _updatesApplied;
    public bool UpdatesApplied
    {
        get => _updatesApplied;
        set
        {
            if (SetProperty(ref _updatesApplied, value))
            {
                OnPropertyChanged(nameof(HasPendingUpdates));
            }
        }
    }

    #endregion

    #region Visibility Bindings

    private Microsoft.UI.Xaml.Visibility _acceptVisibility = Microsoft.UI.Xaml.Visibility.Visible;
    public Microsoft.UI.Xaml.Visibility AcceptVisibility
    {
        get => _acceptVisibility;
        set => SetProperty(ref _acceptVisibility, value);
    }

    private Microsoft.UI.Xaml.Visibility _progressVisibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility ProgressVisibility
    {
        get => _progressVisibility;
        set => SetProperty(ref _progressVisibility, value);
    }

    #endregion

    #region Commands

    public RelayCommand AcceptCommand { get; }
    public RelayCommand SendCommand { get; }

    // Property update commands
    public RelayCommand AcceptAllCommand { get; }
    public RelayCommand ReviewEachCommand { get; }
    public RelayCommand TryAgainCommand { get; }

    // Review mode commands
    public RelayCommand AcceptCurrentCommand { get; }
    public RelayCommand SkipCurrentCommand { get; }
    public RelayCommand AcceptRemainingCommand { get; }

    #endregion

    #region Workflow Processing

    private async Task ProcessWorkflow()
    {
        await Task.CompletedTask;
    }

    public async Task SendButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;

        var userMessage = InputText;
        InputText = string.Empty;

        ProgressVisibility = Microsoft.UI.Xaml.Visibility.Visible;
        ConversationList.Add(ChatMessage.FromUser(userMessage));

        try
        {
            if (OnSendMessage != null)
            {
                var response = await OnSendMessage(userMessage);
                ConversationList.Add(ChatMessage.FromCollaborator(response));
            }
            else
            {
                ConversationList.Add(ChatMessage.FromCollaborator("Chat not connected."));
            }
        }
        catch (Exception ex)
        {
            ConversationList.Add(ChatMessage.Error(ex.Message));
        }
        finally
        {
            ProgressVisibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    private void SaveOutputs()
    {
        // Host stub
    }

    #endregion

    #region Property Update Command Handlers

    private void ExecuteAcceptAll()
    {
        if (!HasPendingUpdates) return;

        // Collaborator applies free rows, may leave Protect, and updates this collection.
        OnAcceptAll?.Invoke();
    }

    private void ExecuteReviewEach()
    {
        if (!HasPendingUpdates) return;

        CurrentReviewIndex = 0;
        IsInReviewMode = true;
        NotifyReviewProperties();
    }

    private async Task ExecuteTryAgain()
    {
        if (!HasPendingUpdates) return;

        ClearPendingUpdates();
        if (OnTryAgain != null)
            await OnTryAgain();
    }

    private void ExecuteAcceptCurrent()
    {
        if (!IsInReviewMode || !HasPendingUpdates) return;

        var key = CurrentReviewKey;
        OnAcceptProperty?.Invoke(key);
        AdvanceReview();
    }

    private void ExecuteSkipCurrent()
    {
        if (!IsInReviewMode || !HasPendingUpdates) return;

        var key = CurrentReviewKey;
        OnSkipProperty?.Invoke(key);
        AdvanceReview();
    }

    private void ExecuteAcceptRemaining()
    {
        if (!IsInReviewMode) return;

        // #116: only free updates; Protect stays for Accept/Skip.
        if (OnAcceptRemainingFree != null)
            OnAcceptRemainingFree.Invoke();
        else
        {
            // Fallback: accept free-looking rows only (no Protect label).
            var keys = PendingUpdateItems
                .Where(i => !i.IsProtected)
                .Select(i => i.Key)
                .ToList();
            foreach (var key in keys)
                OnAcceptProperty?.Invoke(key);
        }

        if (PendingUpdateItems == null || PendingUpdateItems.Count == 0)
        {
            ClearPendingUpdates();
            return;
        }

        CurrentReviewIndex = 0;
        NotifyReviewProperties();
    }

    /// <summary>
    /// After accept/skip, Collaborator removes the current key from the list.
    /// The next item slides into the same index — do not increment (#115).
    /// </summary>
    private void AdvanceReview()
    {
        if (PendingUpdateItems == null || PendingUpdateItems.Count == 0)
        {
            IsInReviewMode = false;
            ClearPendingUpdates();
            return;
        }

        if (CurrentReviewIndex >= PendingUpdateItems.Count)
        {
            IsInReviewMode = false;
            if (PendingUpdateItems.Count == 0)
                ClearPendingUpdates();
            return;
        }

        NotifyReviewProperties();
    }

    private void NotifyReviewProperties()
    {
        OnPropertyChanged(nameof(CurrentReviewKey));
        OnPropertyChanged(nameof(CurrentReviewValue));
        OnPropertyChanged(nameof(CurrentReviewExisting));
        OnPropertyChanged(nameof(CurrentReviewCraft));
        OnPropertyChanged(nameof(CurrentReviewHasCraft));
        OnPropertyChanged(nameof(CurrentReviewCraftVisibility));
        OnPropertyChanged(nameof(ReviewProgress));
    }

    public void ClearPendingUpdates()
    {
        PendingUpdateItems.Clear();
        IsInReviewMode = false;
        UpdatesApplied = false;
        CurrentReviewIndex = 0;
        OnPropertyChanged(nameof(HasUpdates));
        OnPropertyChanged(nameof(HasPendingUpdates));
    }

    /// <summary>
    /// Receives classified pending updates from Collaborator after workflow execution (#116).
    /// </summary>
    public void SetPendingUpdates(IReadOnlyList<PendingUpdateItem> items)
    {
        PendingUpdateItems.Clear();
        if (items != null)
        {
            foreach (var item in items)
                PendingUpdateItems.Add(item);
        }

        UpdatesApplied = false;
        OnPropertyChanged(nameof(PendingUpdateItems));
        OnPropertyChanged(nameof(HasUpdates));
        OnPropertyChanged(nameof(HasPendingUpdates));
        NotifyReviewProperties();
    }

    /// <summary>Called by Collaborator after all free updates are applied.</summary>
    public void MarkUpdatesApplied()
    {
        UpdatesApplied = true;
        IsInReviewMode = false;
        PendingUpdateItems.Clear();
        OnPropertyChanged(nameof(HasUpdates));
        OnPropertyChanged(nameof(HasPendingUpdates));
    }

    #endregion
}
