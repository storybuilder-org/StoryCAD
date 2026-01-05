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
        AcceptCommand = new RelayCommand(SaveOutputs);
        SendCommand = new RelayCommand(async () => await SendButtonClicked());
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
}
