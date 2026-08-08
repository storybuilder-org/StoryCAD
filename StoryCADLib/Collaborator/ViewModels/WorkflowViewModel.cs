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
        ConversationList.CollectionChanged += OnConversationChanged;
        PendingUpdateItems = new ObservableCollection<PendingUpdateItem>();

        // Existing commands
        AcceptCommand = new RelayCommand(SaveOutputs);
        SendCommand = new RelayCommand(async () => await SendButtonClicked());

        // Property update commands (#140: Accept paths may await overwrite confirm)
        AcceptAllCommand = new RelayCommand(async () => await ExecuteAcceptAllAsync());
        ReviewEachCommand = new RelayCommand(ExecuteReviewEach);
        TryAgainCommand = new RelayCommand(async () => await ExecuteTryAgain());

        // Review mode commands
        AcceptCurrentCommand = new RelayCommand(async () => await ExecuteAcceptCurrentAsync());
        SkipCurrentCommand = new RelayCommand(async () => await ExecuteSkipCurrentAsync());
        AcceptRemainingCommand = new RelayCommand(async () => await ExecuteAcceptRemainingAsync());
    }

    /// <summary>
    /// Shows the sender label once per run of consecutive same-sender bubbles;
    /// status groups never carry a label (#129 chat cleanup).
    /// </summary>
    private void OnConversationChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add || e.NewItems == null)
            return;

        foreach (ChatMessage added in e.NewItems)
        {
            var index = ConversationList.IndexOf(added);
            var previous = index > 0 ? ConversationList[index - 1] : null;
            added.ShowSender = !added.IsStatusGroup
                && (previous == null || previous.IsUser != added.IsUser);
        }
    }

    /// <summary>
    /// Adds a workflow progress line, rolling consecutive lines into one
    /// collapsed status group instead of a bubble each (#129 chat cleanup).
    /// </summary>
    public void AddStatusMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var line = message.Trim().Trim('-').Trim();
        if (line.Length == 0)
            return;

        if (ConversationList.Count > 0
            && ConversationList[^1] is { IsStatusGroup: true } group)
        {
            group.AddStep(line);
            return;
        }

        var newGroup = ChatMessage.StatusGroup();
        newGroup.AddStep(line);
        ConversationList.Add(newGroup);
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

    private string _description = string.Empty;
    /// <summary>Brief workflow purpose (registry description).</summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value ?? string.Empty);
    }

    private string _explanation = string.Empty;
    /// <summary>
    /// Topical work-column context (#129): selected elements and what to do next
    /// (pending accept/review). Not the long static registry essay.
    /// </summary>
    public string Explanation
    {
        get => _explanation;
        set => SetProperty(ref _explanation, value ?? string.Empty);
    }

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

    private bool _isChatEnabled;
    /// <summary>
    /// Collaborator #145: Send enabled only after workflow proposals are seeded into chat.
    /// </summary>
    public bool IsChatEnabled
    {
        get => _isChatEnabled;
        set => SetProperty(ref _isChatEnabled, value);
    }

    private string _chatPlaceholder = "Waiting for proposals…";
    public string ChatPlaceholder
    {
        get => _chatPlaceholder;
        set => SetProperty(ref _chatPlaceholder, value ?? string.Empty);
    }

    private string _promptOutput;
    public string PromptOutput
    {
        get => _promptOutput;
        set => SetProperty(ref _promptOutput, value);
    }

    private string _selectedElementsSummary = string.Empty;
    /// <summary>
    /// Gathered elements for this run (e.g. "Overview: Schrodinger's Computer").
    /// Surfaced via <see cref="Explanation"/>, not a separate card.
    /// </summary>
    public string SelectedElementsSummary
    {
        get => _selectedElementsSummary;
        set => SetProperty(ref _selectedElementsSummary, value ?? string.Empty);
    }

    /// <summary>
    /// Rebuilds topical Explanation from selected elements + pending/review state.
    /// </summary>
    public void RefreshTopicalExplanation()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(SelectedElementsSummary))
        {
            var oneLine = SelectedElementsSummary.Replace("\r\n", "; ").Replace("\n", "; ");
            parts.Add($"Selected: {oneLine}");
        }

        if (IsInReviewMode && PendingUpdateItems != null && PendingUpdateItems.Count > 0)
        {
            parts.Add(
                $"Reviewing {CurrentReviewIndex + 1} of {PendingUpdateItems.Count}: {CurrentReviewDisplayName}. " +
                "Accept to apply, Skip to keep yours.");
        }
        else if (HasPendingUpdates && PendingUpdateItems != null)
        {
            var total = PendingUpdateItems.Count;
            var needReview = PendingUpdateItems.Count(i => i.IsProtected);
            var free = total - needReview;
            parts.Add(
                $"{total} property update(s) ({free} free, {needReview} need review). " +
                "Use Accept All, Review Each, or Try Again.");
        }
        else if (UpdatesApplied)
        {
            parts.Add("Updates applied. Ask in chat or choose another workflow.");
        }

        Explanation = parts.Count == 0 ? string.Empty : string.Join("\n", parts);
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

    /// <summary>
    /// Panel header with counts (#129): free vs need-review (Protect).
    /// </summary>
    public string PendingUpdatesHeader
    {
        get
        {
            if (PendingUpdateItems == null || PendingUpdateItems.Count == 0)
                return "Proposed property updates";

            var total = PendingUpdateItems.Count;
            var needReview = PendingUpdateItems.Count(i => i.IsProtected);
            var free = total - needReview;
            return $"Proposed property updates ({total}: {free} free, {needReview} need review)";
        }
    }

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

    /// <summary>
    /// Friendly title for the row under review, e.g. "Structure Title (Problem)".
    /// Falls back to the raw key when display fields are unset (#129).
    /// </summary>
    public string CurrentReviewDisplayName
    {
        get
        {
            if (!IsInReviewMode || PendingUpdateItems == null || PendingUpdateItems.Count <= CurrentReviewIndex)
                return string.Empty;

            var item = PendingUpdateItems[CurrentReviewIndex];
            if (string.IsNullOrEmpty(item.PropertyDisplayName))
                return item.Key;
            return string.IsNullOrEmpty(item.ElementName)
                ? item.PropertyDisplayName
                : $"{item.PropertyDisplayName} ({item.ElementName})";
        }
    }

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

    public Func<Task> OnAcceptAll { get; set; }
    public Func<Task> OnTryAgain { get; set; }
    public Func<string, Task> OnAcceptProperty { get; set; }
    public Func<string, Task> OnSkipProperty { get; set; }

    /// <summary>
    /// Accept remaining free now; stage remaining Protect and confirm when the queue is done (#140).
    /// </summary>
    public Func<Task> OnAcceptRemainingFree { get; set; }

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
        if (!IsChatEnabled)
        {
            ConversationList.Add(ChatMessage.FromCollaborator(
                "Chat unlocks after the workflow produces property proposals."));
            InputText = string.Empty;
            return;
        }

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

    private async Task ExecuteAcceptAllAsync()
    {
        if (!HasPendingUpdates) return;
        if (OnAcceptAll != null)
            await OnAcceptAll();
    }

    private void ExecuteReviewEach()
    {
        if (!HasPendingUpdates) return;

        IsInReviewMode = true;
        CurrentReviewIndex = 0;
        // One forward pass: land on first open row (or exit if none).
        AdvancePastSettledRows();
        NotifyReviewProperties();
    }

    private async Task ExecuteTryAgain()
    {
        if (!HasPendingUpdates) return;

        ClearPendingUpdates();
        if (OnTryAgain != null)
            await OnTryAgain();
    }

    /// <summary>
    /// Accepts a single row from its inline tick (#129); works outside review mode.
    /// Protect rows are staged for end-of-queue confirm (#140).
    /// </summary>
    public async Task AcceptItemAsync(string key)
    {
        if (string.IsNullOrEmpty(key) || !HasPendingUpdates) return;
        if (OnAcceptProperty != null)
            await OnAcceptProperty(key);
    }

    /// <summary>Sync wrapper for XAML/command binding that cannot await.</summary>
    public void AcceptItem(string key) =>
        _ = AcceptItemAsync(key);

    /// <summary>
    /// Skips (discards) a single row from its inline dismiss button (#129).
    /// </summary>
    public async Task SkipItemAsync(string key)
    {
        if (string.IsNullOrEmpty(key) || !HasPendingUpdates) return;
        if (OnSkipProperty != null)
            await OnSkipProperty(key);
    }

    public void SkipItem(string key) =>
        _ = SkipItemAsync(key);

    private async Task ExecuteAcceptCurrentAsync()
    {
        if (!IsInReviewMode || !HasPendingUpdates) return;

        var key = CurrentReviewKey;
        if (OnAcceptProperty != null)
            await OnAcceptProperty(key);
        AdvanceReview();
    }

    private async Task ExecuteSkipCurrentAsync()
    {
        if (!IsInReviewMode || !HasPendingUpdates) return;

        var key = CurrentReviewKey;
        if (OnSkipProperty != null)
            await OnSkipProperty(key);
        AdvanceReview();
    }

    private async Task ExecuteAcceptRemainingAsync()
    {
        if (!IsInReviewMode) return;

        // #140: free apply now; remaining Protect staged → one confirm when queue done.
        if (OnAcceptRemainingFree != null)
            await OnAcceptRemainingFree();
        else
        {
            var keys = PendingUpdateItems
                .Select(i => i.Key)
                .ToList();
            foreach (var key in keys)
            {
                if (OnAcceptProperty != null)
                    await OnAcceptProperty(key);
            }
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
    /// Settled rows (Accepted/Skipped) are done for Review Each. One list, one forward pass:
    /// foreach item → accept or skip → next. No wrap-around.
    /// </summary>
    private static bool IsSettled(PendingUpdateItem? item)
    {
        if (item == null) return true;
        var kind = item.KindLabel ?? string.Empty;
        return kind.Equals("Skipped", StringComparison.OrdinalIgnoreCase)
            || kind.Equals("Accepted", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// After accept/skip (or on enter review): sit on an open row walking forward only.
    /// If the current row was removed, the next open item is already at this index (#115) — stay.
    /// If the current row is settled (#145), move forward until open or past the end.
    /// </summary>
    private void AdvancePastSettledRows()
    {
        if (PendingUpdateItems == null || PendingUpdateItems.Count == 0)
        {
            IsInReviewMode = false;
            return;
        }

        while (CurrentReviewIndex < PendingUpdateItems.Count
               && IsSettled(PendingUpdateItems[CurrentReviewIndex]))
        {
            CurrentReviewIndex++;
        }

        if (CurrentReviewIndex >= PendingUpdateItems.Count)
            IsInReviewMode = false;
    }

    private void AdvanceReview()
    {
        AdvancePastSettledRows();
        NotifyReviewProperties();
    }

    private void NotifyReviewProperties()
    {
        OnPropertyChanged(nameof(CurrentReviewKey));
        OnPropertyChanged(nameof(CurrentReviewDisplayName));
        OnPropertyChanged(nameof(CurrentReviewValue));
        OnPropertyChanged(nameof(CurrentReviewExisting));
        OnPropertyChanged(nameof(CurrentReviewCraft));
        OnPropertyChanged(nameof(CurrentReviewHasCraft));
        OnPropertyChanged(nameof(CurrentReviewCraftVisibility));
        OnPropertyChanged(nameof(ReviewProgress));
        RefreshTopicalExplanation();
    }

    public void ClearPendingUpdates()
    {
        PendingUpdateItems.Clear();
        IsInReviewMode = false;
        UpdatesApplied = false;
        CurrentReviewIndex = 0;
        OnPropertyChanged(nameof(HasUpdates));
        OnPropertyChanged(nameof(HasPendingUpdates));
        OnPropertyChanged(nameof(PendingUpdatesHeader));
        RefreshTopicalExplanation();
    }

    /// <summary>
    /// Receives classified pending updates from Collaborator after workflow execution (#116).
    /// </summary>
    public void SetPendingUpdates(IReadOnlyList<PendingUpdateItem> items)
    {
        PendingUpdateItems.Clear();
        if (items != null)
        {
            // A property name alone ("Structure Title") does not say which element it belongs
            // to. Name the element on every row once the set covers more than one of them.
            var multiElement = items
                .Select(i => i.ElementName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() > 1;

            foreach (var item in items)
            {
                item.ShowElementName = multiElement;
                PendingUpdateItems.Add(item);
            }
        }

        UpdatesApplied = false;

        // Review Each: one forward pass; skip settled rows after list rebuild (#145).
        if (IsInReviewMode)
            AdvancePastSettledRows();

        OnPropertyChanged(nameof(PendingUpdateItems));
        OnPropertyChanged(nameof(HasUpdates));
        OnPropertyChanged(nameof(HasPendingUpdates));
        OnPropertyChanged(nameof(PendingUpdatesHeader));
        NotifyReviewProperties();
        RefreshTopicalExplanation();
    }

    /// <summary>Called by Collaborator after all free updates are applied.</summary>
    public void MarkUpdatesApplied()
    {
        UpdatesApplied = true;
        IsInReviewMode = false;
        PendingUpdateItems.Clear();
        OnPropertyChanged(nameof(HasUpdates));
        OnPropertyChanged(nameof(HasPendingUpdates));
        OnPropertyChanged(nameof(PendingUpdatesHeader));
        RefreshTopicalExplanation();
    }

    #endregion
}
