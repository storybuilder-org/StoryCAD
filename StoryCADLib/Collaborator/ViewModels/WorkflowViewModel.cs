using System.Collections.ObjectModel;
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
public partial class WorkflowViewModel : ObservableRecipient
{
    public WorkflowViewModel()
    {
        ConversationList = new ObservableCollection<ChatMessage>();
        PendingUpdates = new Dictionary<string, object>();

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
    /// Property updates extracted from workflow but not yet applied.
    /// Key format: "ElementLabel.PropertyName", Value: new value
    /// </summary>
    public Dictionary<string, object> PendingUpdates { get; set; }

    /// <summary>
    /// True if there are updates to display (pending or applied).
    /// </summary>
    public bool HasUpdates => PendingUpdates?.Count > 0;

    /// <summary>
    /// True if updates exist and haven't been applied yet.
    /// </summary>
    public bool HasPendingUpdates => HasUpdates && !UpdatesApplied;

    public Microsoft.UI.Xaml.Visibility ReviewModeVisibility =>
        IsInReviewMode ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    private bool _isInReviewMode;
    /// <summary>
    /// True when user is reviewing updates one at a time.
    /// </summary>
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
    /// <summary>
    /// Index of the property currently being reviewed (0-based).
    /// </summary>
    public int CurrentReviewIndex
    {
        get => _currentReviewIndex;
        set => SetProperty(ref _currentReviewIndex, value);
    }

    /// <summary>
    /// Display key of the property currently being reviewed.
    /// </summary>
    public string CurrentReviewKey => IsInReviewMode && PendingUpdates?.Count > CurrentReviewIndex
        ? PendingUpdates.Keys.ElementAt(CurrentReviewIndex)
        : string.Empty;

    /// <summary>
    /// Value of the property currently being reviewed.
    /// </summary>
    public string CurrentReviewValue => IsInReviewMode && PendingUpdates?.Count > CurrentReviewIndex
        ? PendingUpdates.Values.ElementAt(CurrentReviewIndex)?.ToString() ?? "(empty)"
        : string.Empty;

    /// <summary>
    /// Progress text for review mode (e.g., "2 of 5").
    /// </summary>
    public string ReviewProgress => IsInReviewMode
        ? $"{CurrentReviewIndex + 1} of {PendingUpdates?.Count ?? 0}"
        : string.Empty;

    /// <summary>
    /// Callback invoked when user clicks Accept All.
    /// Collaborator sets this to apply all pending updates.
    /// </summary>
    public Action OnAcceptAll { get; set; }

    /// <summary>
    /// Callback invoked when user clicks Try Again.
    /// Collaborator sets this to re-execute the workflow.
    /// </summary>
    public Func<Task> OnTryAgain { get; set; }

    /// <summary>
    /// Callback invoked when user accepts a single property in review mode.
    /// Parameter is the property key (e.g., "Overview.Premise").
    /// </summary>
    public Action<string> OnAcceptProperty { get; set; }

    /// <summary>
    /// Callback invoked when user skips a single property in review mode.
    /// Parameter is the property key (e.g., "Overview.Premise").
    /// </summary>
    public Action<string> OnSkipProperty { get; set; }

    private bool _updatesApplied;
    /// <summary>
    /// True when updates have been applied (disables action buttons via CanExecute).
    /// </summary>
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

        OnAcceptAll?.Invoke();
        ClearPendingUpdates();
    }

    private void ExecuteReviewEach()
    {
        if (!HasPendingUpdates) return;

        CurrentReviewIndex = 0;
        IsInReviewMode = true;
        OnPropertyChanged(nameof(CurrentReviewKey));
        OnPropertyChanged(nameof(CurrentReviewValue));
        OnPropertyChanged(nameof(ReviewProgress));
    }

    private async Task ExecuteTryAgain()
    {
        if (!HasPendingUpdates) return;

        ClearPendingUpdates();
        if (OnTryAgain != null)
        {
            await OnTryAgain();
        }
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
        // Notify Collaborator to remove from its pending updates
        OnSkipProperty?.Invoke(key);
        // Note: Collaborator calls SetPendingUpdates which updates our PendingUpdates
        AdvanceReview();
    }

    private void ExecuteAcceptRemaining()
    {
        if (!IsInReviewMode) return;

        // Accept all remaining updates
        var remainingKeys = PendingUpdates.Keys.Skip(CurrentReviewIndex).ToList();
        foreach (var key in remainingKeys)
        {
            OnAcceptProperty?.Invoke(key);
        }

        ClearPendingUpdates();
    }

    private void AdvanceReview()
    {
        if (CurrentReviewIndex >= PendingUpdates.Count - 1 || PendingUpdates.Count == 0)
        {
            // Done reviewing
            IsInReviewMode = false;
            ClearPendingUpdates();
        }
        else
        {
            // Move to next
            CurrentReviewIndex++;
            OnPropertyChanged(nameof(CurrentReviewKey));
            OnPropertyChanged(nameof(CurrentReviewValue));
            OnPropertyChanged(nameof(ReviewProgress));
        }
    }

    public void ClearPendingUpdates()
    {
        PendingUpdates.Clear();
        IsInReviewMode = false;
        UpdatesApplied = false;
        CurrentReviewIndex = 0;
    }

    /// <summary>
    /// Receives pending updates from Collaborator after workflow execution.
    /// </summary>
    public void SetPendingUpdates(Dictionary<string, object> updates)
    {
        PendingUpdates = updates;
        UpdatesApplied = false;
        OnPropertyChanged(nameof(PendingUpdates));
        OnPropertyChanged(nameof(HasUpdates));
        OnPropertyChanged(nameof(HasPendingUpdates));
    }

    /// <summary>
    /// Called by Collaborator after updates are applied (disables action buttons).
    /// </summary>
    public void MarkUpdatesApplied()
    {
        UpdatesApplied = true;
        IsInReviewMode = false;
    }

    #endregion
}
