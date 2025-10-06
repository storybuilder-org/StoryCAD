using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoryCAD.Collaborator.Views;
using StoryCAD.Services.Collaborator;
using StoryCAD.Services.Navigation;

namespace StoryCAD.Collaborator.ViewModels;

public partial class WorkflowViewModel : ObservableRecipient
{
    private readonly CollaboratorService _collaborator;
    private readonly ILogService _logger;
    private readonly NavigationService _navigationService;

    public WorkflowViewModel(ILogService logger, CollaboratorService collaborator, NavigationService navigationService)
    {
        _logger = logger;
        _collaborator = collaborator;
        _navigationService = navigationService;
    }

    #region Public methods

    public void LoadModel(StoryElement model, string workflow)
    {
        Ioc.Default.GetService<CollaboratorService>()!.LoadWorkflowModel(model, workflow);
    }

    #endregion

    /// <summary>
    ///     Process the NavigationView SelectionChanged event. This is
    ///     triggered when the user clicks on a NavigationViewItem
    ///     on the WorkflowShell page menu.
    ///     To maintain similarity to StoryCAD's ViewModel processing,
    ///     this even is marshalled as a WorkflowViewModel.LoadModel() call,
    ///     passing the the NavigationViewItem as CurrentItem.
    ///     the loading, display and processing of a WizardStepModel.
    ///     LoadModel will then pass the call to
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var item = (NavigationViewItem)args.SelectedItem;
        if (item is null)
        {
            return;
        }

        CurrentItem = item;
        LoadModel(Model, (string)item!.Tag);
        Ioc.Default.GetService<CollaboratorService>()!.ProcessWorkflow();
        //CurrentStep = step.Title;
        // Navigate to the appropriate WizardStep page, passing the WizardStep instance
        // as the parameter. This invokes the Page's OnNavigatedTo() method, which
        // Establishes the WizardStepViewModel as the DataContext (for binding)
        // In turn the ViewModel's Activate() method is invoked.
        _navigationService.NavigateTo(ContentFrame, "WorkflowPage", this);
    }

    public void SetCurrentNavigationViewItem(NavigationViewItem item)
    {
        if (item == null)
        {
            return;
        }

        if (item.Tag == null)
        {
            return;
        }

        ContentFrame.Navigate(Type.GetType(item.Tag.ToString()), item.Content);
        NavView.Header = item.Content;
        NavView.SelectedItem = item;
    }

    public void SetCurrentPage(Type type)
    {
        ContentFrame.Navigate(type);
    }

    public void SetCurrentPage(string typeName)
    {
        var type = Type.GetType("StoryCAD.Collaborator.Views." + typeName);
        ContentFrame.Navigate(type);
    }

    #region public properties

    public string Title { get; set; }
    public string Description { get; set; }
    public string Explanation { get; set; }

    public ObservableCollection<WorkflowStepModel> WorkflowSteps { get; set; } = [];

    // Chat history
    public ObservableCollection<string> ConversationList { get; set; }

    // Chat input
    public string InputText { get; set; } //  Chat input

    // WorkflowShell displays a list of 
    public ObservableCollection<NavigationViewItem> MenuItems { get; set; } = [];


    private object _selectedItem;

    public object SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    private StoryElement _model;

    public StoryElement Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    public StoryItemType ItemType { get; set; }
    public Frame ContentFrame { get; set; }
    public NavigationView NavView { get; set; }

    private NavigationViewItem _currentItem;

    public NavigationViewItem CurrentItem
    {
        get => _currentItem;
        set => SetProperty(ref _currentItem, value);
    }

    private string _currentStep;

    public string CurrentStep
    {
        get => _currentStep;
        set => SetProperty(ref _currentStep, value);
    }

    public XamlRoot WorkflowShellRoot { get; set; }

    #endregion

    #region Visibility bindings

    private Visibility _acceptVisibility = Visibility.Visible;

    public Visibility AcceptVisibility
    {
        get => _acceptVisibility;
        set => SetProperty(ref _acceptVisibility, value);
    }

    private Visibility _exitVisibility = Visibility.Visible;

    public Visibility ExitVisibility
    {
        get => _exitVisibility;
        set => SetProperty(ref _exitVisibility, value);
    }

    private Visibility _progressVisibility = Visibility.Collapsed;

    public Visibility ProgressVisibility
    {
        get => _progressVisibility;
        set => SetProperty(ref _progressVisibility, value);
    }

    #endregion

    #region Commands

    public RelayCommand AcceptCommand { get; }
    public RelayCommand ExitCommand { get; }
    public string PromptOutput { get; set; }

    // public RelayCommand HelpCommand { get; }

    #endregion

    #region Constructor(s)

    // Constructor for XAML compatibility - will be removed later
    public WorkflowViewModel() : this(
        Ioc.Default.GetRequiredService<NavigationService>())
    {
    }

    public WorkflowViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;
        PromptOutput = "Prompt output empty";
        Title = string.Empty;
        Description = string.Empty;
        InputText = string.Empty;
        // Configure Collaborator page navigation
        try
        {
            _navigationService.Configure("WorkflowPage", typeof(WorkflowPage));
            _navigationService.Configure("WelcomePage", typeof(WelcomePage));
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Info, ex, "failed to configure workflow VM navigation");
        }

        AcceptCommand = new RelayCommand(SaveOutputs);
        ExitCommand = new RelayCommand(ExitCollaborator);
    }

    #endregion

    #region Command Buttons

    /// <summary>
    ///     Process the AcceptCommand button.
    ///     Save any pending OutputProperty values.
    /// </summary>
    private void SaveOutputs()
    {
        Ioc.Default.GetService<CollaboratorService>().SaveOutputs();
        // This command is passed via
    }

    /// <summary>
    ///     Process the ExitComand button.
    ///     Resets the NavigationView and hides the Collaborator window.
    /// </summary>
    private void ExitCollaborator()
    {
        // Remove the event handler
        SetCurrentPage(typeof(WelcomePage));
        NavView.SelectionChanged -= NavView_SelectionChanged;
        // Reset the NavigationView.
        MenuItems.Clear();
        // Optionally, clear FooterMenuItems if used
        // FooterMenuItems.Clear();
        // Reset the selected item
        SelectedItem = null;

        // Reset the current page (without navigation)
        //ContentFrame.Navigate(typeof(WelcomePage));

        _collaborator.CollaboratorWindow.AppWindow.Hide();
    }

    public void EnableNavigation()
    {
        NavView.SelectionChanged -= NavView_SelectionChanged;
    }

    #endregion
}
