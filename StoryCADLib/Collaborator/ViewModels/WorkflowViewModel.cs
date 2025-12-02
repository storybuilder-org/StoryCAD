using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
        ConversationList = new ObservableCollection<string>();
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

    public ObservableCollection<string> ConversationList { get; set; }

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

        ProgressVisibility = Microsoft.UI.Xaml.Visibility.Visible;
        ConversationList.Add($"User: {InputText}");
        ConversationList.Add("Assistant: Chat processing not implemented in host stub.");
        InputText = string.Empty;
        ProgressVisibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private void SaveOutputs()
    {
        // Host stub
    }

    #endregion
}
