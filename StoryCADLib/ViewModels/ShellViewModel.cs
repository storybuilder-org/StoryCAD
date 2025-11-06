using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using StoryCADLib.Collaborator.ViewModels;
using StoryCADLib.DAL;
using StoryCADLib.Services;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Collaborator;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.Dialogs.Tools;
using StoryCADLib.Services.Json;
using StoryCADLib.Services.Locking;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Navigation;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Search;
using StoryCADLib.ViewModels.SubViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.ViewModels;

public class ShellViewModel : ObservableRecipient
{
    private const string HomePage = "HomePage";
    private const string OverviewPage = "OverviewPage";
    private const string ProblemPage = "ProblemPage";
    private const string CharacterPage = "CharacterPage";
    private const string ScenePage = "ScenePage";
    private const string FolderPage = "FolderPage";
    private const string SettingPage = "SettingPage";
    private const string TrashCanPage = "TrashCanPage";
    private const string WebPage = "WebPage";
    public readonly AutoSaveService _autoSaveService;
    public readonly BackupService _BackupService;
    private readonly NavigationService _navigationService;
    private readonly SceneViewModel _sceneViewModel;

    private readonly DispatcherTimer _statusTimer;

    // Required services 
    private readonly ILogService Logger;
    private readonly OutlineService outlineService;
    private readonly PreferenceService Preferences;

    public readonly ScrivenerIo Scrivener;
    private readonly SearchService Search;
    private readonly AppState State;
    private readonly Windowing Window;
    public ContentDialog _contentDialog;
    public ObservableCollection<StoryNodeItem> _sourceChildren;
    private int _sourceIndex;
    private ObservableCollection<StoryNodeItem> _targetCollection;
    private int _targetIndex;

    public CollaboratorArgs CollabArgs;

    public StoryViewType CurrentViewType;

    //File opened (if StoryCAD was opened via an STBX file)
    public string FilePathToLaunch;
    public bool IsClosing;
    public TreeViewItem LastClickedTreeviewItem;

    //List of new nodes that have a background, these are cleared on navigation
    public List<StoryNodeItem> NewNodeHighlightCache = new();
    public StoryNodeItem RightTappedNode;

    // The right-hand (detail) side of ShellView
    public Frame SplitViewFrame;

    #region Constructor(s)
    public ShellViewModel(SceneViewModel sceneViewModel, ILogService logger, SearchService search,
        AutoSaveService autoSaveService, BackupService backupService, Windowing window,
        AppState appState, ScrivenerIo scrivener, PreferenceService preferenceService,
        OutlineViewModel outlineViewModel, OutlineService outlineService, NavigationService navigationService,
        PrintReportDialogVM printReportDialogVM)
    {
        // Store injected services
        _sceneViewModel = sceneViewModel;
        Logger = logger;
        Search = search;
        _autoSaveService = autoSaveService;
        _BackupService = backupService;
        Window = window;
        State = appState;
        Scrivener = scrivener;
        Preferences = preferenceService;
        _navigationService = navigationService;
        PrintReportDialog = printReportDialogVM;
        // Store sub ViewModels
        OutlineManager = outlineViewModel;
        this.outlineService = outlineService;

        // Register inter-MVVM messaging
        Messenger.Register<IsChangedRequestMessage>(this,
            (_, m) => { m.Reply(State.CurrentDocument?.Model?.Changed ?? false); });
        Messenger.Register<ShellViewModel, IsChangedMessage>(this, static (r, m) => r.IsChangedMessageReceived(m));
        Messenger.Register<ShellViewModel, IsBackupStatusMessage>(this,
            static (r, m) => r.BackupStatusMessageReceived(m));
        Messenger.Register<ShellViewModel, StatusChangedMessage>(this, static (r, m) => r.StatusMessageReceived(m));
        Messenger.Register<ShellViewModel, NameChangedMessage>(this, static (r, m) => r.NameMessageReceived(m));

        State.CurrentDocument = new StoryDocument(new StoryModel());

        //Skip status timer initialization in Tests.
        if (!State.Headless)
        {
            _statusTimer = new DispatcherTimer();
            _statusTimer.Tick += statusTimer_Tick;
            ChangeStatusColor = Colors.Green;
            BackupStatusColor = Colors.Green;
        }

        Messenger.Send(new StatusChangedMessage(new StatusMessage("Ready", LogLevel.Info)));

        TogglePaneCommand = new RelayCommand(TogglePane, SerializationLock.CanExecuteCommands);
        OpenFileOpenMenuCommand =
            new AsyncRelayCommand(OutlineManager.OpenFileOpenMenu, SerializationLock.CanExecuteCommands);
        NarrativeToolCommand =
            new RelayCommand(async () => await Ioc.Default.GetRequiredService<NarrativeToolVM>().OpenNarrativeTool(),
                SerializationLock.CanExecuteCommands);
        PrintNodeCommand = new RelayCommand(async () => await OutlineManager.PrintCurrentNodeAsync(),
            SerializationLock.CanExecuteCommands);
        OpenFileCommand = new RelayCommand(async () => await OutlineManager.OpenFile(),
            SerializationLock.CanExecuteCommands);
        SaveFileCommand = new RelayCommand(async () => await OutlineManager.SaveFile(),
            SerializationLock.CanExecuteCommands);
        SaveAsCommand = new RelayCommand(async () => await OutlineManager.SaveFileAs(),
            SerializationLock.CanExecuteCommands);
        CreateBackupCommand =
            new RelayCommand(async () => await CreateBackupNow(), SerializationLock.CanExecuteCommands);
        CloseCommand = new RelayCommand(async () => await OutlineManager.CloseFile(),
            SerializationLock.CanExecuteCommands);
        ExitCommand =
            new RelayCommand(async () => await OutlineManager.ExitApp(), SerializationLock.CanExecuteCommands);

        // StoryCAD Collaborator
        CollaboratorCommand = new RelayCommand(LaunchCollaborator, SerializationLock.CanExecuteCommands);

        // Tools commands
        KeyQuestionsCommand = new RelayCommand(async () => await OutlineManager.KeyQuestionsTool(),
            SerializationLock.CanExecuteCommands);
        TopicsCommand = new RelayCommand(async () => await OutlineManager.TopicsTool(),
            SerializationLock.CanExecuteCommands);
        MasterPlotsCommand = new RelayCommand(async () => await OutlineManager.MasterPlotTool(),
            SerializationLock.CanExecuteCommands);
        DramaticSituationsCommand = new RelayCommand(async () => await OutlineManager.DramaticSituationsTool(),
            SerializationLock.CanExecuteCommands);
        StockScenesCommand = new RelayCommand(async () => await OutlineManager.StockScenesTool(),
            SerializationLock.CanExecuteCommands);

        PreferencesCommand = new RelayCommand(OpenPreferences, SerializationLock.CanExecuteCommands);

        PrintReportsCommand = new RelayCommand(OpenPrintMenu, SerializationLock.CanExecuteCommands);
        ExportReportsToPdfCommand = new RelayCommand(OpenExportPdfMenu, SerializationLock.CanExecuteCommands);
        ScrivenerReportsCommand = new RelayCommand(async () => await OutlineManager.GenerateScrivenerReports(),
            SerializationLock.CanExecuteCommands);

        HelpCommand = new RelayCommand(LaunchGitHubPages);

        // Move StoryElement commands
        MoveLeftCommand = new RelayCommand(MoveTreeViewItemLeft, SerializationLock.CanExecuteCommands);
        MoveRightCommand = new RelayCommand(MoveTreeViewItemRight, SerializationLock.CanExecuteCommands);
        MoveUpCommand = new RelayCommand(MoveTreeViewItemUp, SerializationLock.CanExecuteCommands);
        MoveDownCommand = new RelayCommand(MoveTreeViewItemDown, SerializationLock.CanExecuteCommands);

        // Add StoryElement commands
        AddFolderCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Folder),
            SerializationLock.CanExecuteCommands);
        AddSectionCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Section),
            SerializationLock.CanExecuteCommands);
        AddProblemCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Problem),
            SerializationLock.CanExecuteCommands);
        AddCharacterCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Character),
            SerializationLock.CanExecuteCommands);
        AddWebCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Web),
            SerializationLock.CanExecuteCommands);
        AddNotesCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Notes),
            SerializationLock.CanExecuteCommands);
        AddSettingCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Setting),
            SerializationLock.CanExecuteCommands);
        AddSceneCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Scene),
            SerializationLock.CanExecuteCommands);
        ConvertToSceneCommand =
            new RelayCommand(OutlineManager.ConvertProblemToScene, SerializationLock.CanExecuteCommands);
        ConvertToProblemCommand =
            new RelayCommand(OutlineManager.ConvertSceneToProblem, SerializationLock.CanExecuteCommands);

        // Remove Story Element command (move to trash)
        RemoveStoryElementCommand =
            new AsyncRelayCommand(OutlineManager.RemoveStoryElement, SerializationLock.CanExecuteCommands);
        RestoreStoryElementCommand =
            new RelayCommand(OutlineManager.RestoreStoryElement, SerializationLock.CanExecuteCommands);
        EmptyTrashCommand = new RelayCommand(OutlineManager.EmptyTrash, SerializationLock.CanExecuteCommands);
        // Copy to Narrative command
        AddToNarrativeCommand = new RelayCommand(OutlineManager.CopyToNarrative, SerializationLock.CanExecuteCommands);
        RemoveFromNarrativeCommand =
            new RelayCommand(OutlineManager.RemoveFromNarrative, SerializationLock.CanExecuteCommands);

        ViewList.Add("Story Explorer View");
        ViewList.Add("Story Narrator View");

        CurrentView = "Story Explorer View";
        SelectedView = "Story Explorer View";

        ShellInstance = this;
    }

    #endregion

    #region SubViewModels

    // ShellViewModel uses 'sub viewmodels' for different aspects in order
    // to break this very large viewmodel into more manageable pieces.
    public OutlineViewModel OutlineManager { get; }

    #endregion

    // Public property for debugging PrintManager state
    public PrintReportDialogVM PrintReportDialog { get; }

    // Navigation navigation landmark nodes
    public StoryNodeItem CurrentNode { get; set; }
    public DateTime AppStartTime { get; set; } = DateTime.Now;

    // Track the current page type for SaveModel (testable without UI)
    public string CurrentPageType { get; set; }

    public void ViewChanged()
    {
        if (State.CurrentDocument?.Model?.CurrentView == null || State.CurrentDocument.Model.CurrentView.Count == 0)
        {
            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("You need to load a story first!", LogLevel.Warn)));
            Logger.Log(LogLevel.Info, "Failed to switch views as CurrentView is null or empty. (Is a story loaded?)");
            return;
        }

        if (!SelectedView.Equals(CurrentView))
        {
            CurrentView = SelectedView;
            var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
            switch (CurrentView)
            {
                case "Story Explorer View":
                    outlineService.SetCurrentView(State.CurrentDocument.Model, StoryViewType.ExplorerView);
                    break;
                case "Story Narrator View":
                    outlineService.SetCurrentView(State.CurrentDocument.Model, StoryViewType.NarratorView);
                    break;
            }

            CurrentViewType = State.CurrentDocument.Model.CurrentViewType;
            TreeViewNodeClicked(State.CurrentDocument.Model.CurrentView[0]);
        }
    }

    /// <summary>
    ///     This method is called when one of NavigationTree's
    ///     TreeViewItem nodes is right-tapped.
    ///     It alters the visibility of the command bar flyout
    ///     AppBarButtons depending on which portion of the tree
    ///     is tapped and which view (ExplorerView or Navigator) is selected.
    /// </summary>
    public void ShowFlyoutButtons()
    {
        try
        {
            //Trash Can - View Hide all buttons except Empty Trash.
            if (StoryNodeItem.RootNodeType(RightTappedNode) == StoryItemType.TrashCan)
            {
                ExplorerVisibility = Visibility.Collapsed;
                NarratorVisibility = Visibility.Collapsed;
                AddButtonVisibility = Visibility.Collapsed;
                PrintNodeVisibility = Visibility.Collapsed;
                AddButtonVisibility = Visibility.Collapsed;
                TrashButtonVisibility = Visibility.Visible;
            }
            else
            {
                //Explorer tree, show everything but empty trash and add section
                if (SelectedView == ViewList[0])
                {
                    ExplorerVisibility = Visibility.Visible;
                    NarratorVisibility = Visibility.Collapsed;
                    AddButtonVisibility = Visibility.Visible;
                    PrintNodeVisibility = Visibility.Visible;
                    TrashButtonVisibility = Visibility.Collapsed;
                }
                else //Narrator Tree, hide most things.
                {
                    ExplorerVisibility = Visibility.Collapsed;
                    NarratorVisibility = Visibility.Visible;
                    AddButtonVisibility = Visibility.Collapsed;
                    PrintNodeVisibility = Visibility.Visible;
                    TrashButtonVisibility = Visibility.Collapsed;
                }

                AddButtonVisibility = Visibility.Visible;
            }
        }
        catch (Exception e) //errors (is RightTappedNode null?
        {
            Logger.Log(LogLevel.Error, $"An error occurred in ShowFlyoutButtons() \n{e.Message}\n" +
                                       $"- For reference RightTappedNode is " + RightTappedNode);
        }
    }

    /// <summary>
    ///     Opens help menu in the users default browser.
    /// </summary>
    public void LaunchGitHubPages()
    {
        Messenger.Send(
            new StatusChangedMessage(new StatusMessage("Launching GitHub Pages User Manual", LogLevel.Info, true)));

        Process.Start(new ProcessStartInfo
        {
            FileName = "https://Storybuilder-org.github.io/StoryCAD/",
            UseShellExecute = true
        });

        Messenger.Send(
            new StatusChangedMessage(new StatusMessage("Launch default browser completed", LogLevel.Info, true)));
    }

    public void ShowConnectionStatus()
    {
        StatusMessage _msg;
        if (!Doppler.DopplerConnection | !Logger.ElmahLogging)
        {
            _msg = new StatusMessage("Connection not established", LogLevel.Warn, true);
        }
        else
        {
            _msg = new StatusMessage("Connection established", LogLevel.Info, true);
        }

        Messenger.Send(new StatusChangedMessage(_msg));
    }


    #region CommandBar Relay Commands

    // Open/Close Navigation pane (Hamburger menu)
    public RelayCommand TogglePaneCommand { get; }

    // Open file
    public RelayCommand OpenFileCommand { get; }

    // Save command
    public RelayCommand SaveFileCommand { get; }

    // SaveAs command
    public RelayCommand SaveAsCommand { get; }

    public RelayCommand CreateBackupCommand { get; }

    // CloseCommand
    public RelayCommand CloseCommand { get; }

    // ExitCommand
    public RelayCommand ExitCommand { get; }

    //Open/Closes FileOpen menu
    public AsyncRelayCommand OpenFileOpenMenuCommand { get; }

    // Move current TreeViewItem flyout
    public RelayCommand MoveLeftCommand { get; }
    public RelayCommand MoveRightCommand { get; }
    public RelayCommand MoveUpCommand { get; }
    public RelayCommand MoveDownCommand { get; }

    // Launch Collaborator
    public RelayCommand CollaboratorCommand { get; }
    public RelayCommand HelpCommand { get; }

    // Tools MenuFlyOut Commands
    public RelayCommand KeyQuestionsCommand { get; }
    public RelayCommand TopicsCommand { get; }
    public RelayCommand MasterPlotsCommand { get; }
    public RelayCommand DramaticSituationsCommand { get; }
    public RelayCommand StockScenesCommand { get; }
    public RelayCommand PrintReportsCommand { get; }
    public RelayCommand ExportReportsToPdfCommand { get; }
    public RelayCommand ScrivenerReportsCommand { get; }
    public RelayCommand PreferencesCommand { get; }

    private Visibility _collaboratorVisibility;

    public Visibility CollaboratorVisibility
    {
        get => _collaboratorVisibility;
        set => SetProperty(ref _collaboratorVisibility, value);
    }

    #endregion

    #region Add Story Element CommandBarFlyOut Relay Commands

    // Add commands
    public RelayCommand AddFolderCommand { get; }
    public RelayCommand AddSectionCommand { get; }
    public RelayCommand AddProblemCommand { get; }
    public RelayCommand AddCharacterCommand { get; }
    public RelayCommand AddWebCommand { get; }
    public RelayCommand AddNotesCommand { get; }
    public RelayCommand AddSettingCommand { get; }
    public RelayCommand AddSceneCommand { get; }
    public RelayCommand ConvertToSceneCommand { get; }
    public RelayCommand ConvertToProblemCommand { get; }
    public RelayCommand PrintNodeCommand { get; }
    public RelayCommand NarrativeToolCommand { get; }

    // Remove command (move to trash)
    public AsyncRelayCommand RemoveStoryElementCommand { get; }
    public RelayCommand RestoreStoryElementCommand { get; }
    public RelayCommand EmptyTrashCommand { get; }

    // Copy to Narrative command
    public RelayCommand AddToNarrativeCommand { get; }
    public RelayCommand RemoveFromNarrativeCommand { get; }

    #endregion

    #region Shell binding properties

    /// <summary>
    ///     IsPaneOpen is bound to ShellSplitView's IsPaneOpen property with
    ///     two-way binding, so that it can read and update the property.
    ///     If set to true, the left pane (TreeView) is expanded to its full width;
    ///     otherwise, the left pane is collapsed. Default to true (expanded).
    /// </summary>
    private bool _isPaneOpen = true;

    public bool IsPaneOpen
    {
        get => _isPaneOpen;
        set => SetProperty(ref _isPaneOpen, value);
    }


    private Visibility _explorerVisibility;

    /// <summary>
    ///     Controls visibility for elements that should only be shown in the Explorer view.
    /// </summary>
    public Visibility ExplorerVisibility
    {
        get => _explorerVisibility;
        set => SetProperty(ref _explorerVisibility, value);
    }

    private Visibility _narratorVisibility;

    /// <summary>
    ///     Controls visibility for elements that should only be shown in the Narrator view.
    /// </summary>
    public Visibility NarratorVisibility
    {
        get => _narratorVisibility;
        set => SetProperty(ref _narratorVisibility, value);
    }

    private Visibility _trashButtonVisibility;

    /// <summary>
    ///     Controls visibility for elements that should only be shown in the Trash view.
    /// </summary
    public Visibility TrashButtonVisibility
    {
        get => _trashButtonVisibility;
        set => SetProperty(ref _trashButtonVisibility, value);
    }

    private Visibility _addButtonVisibility;

    public Visibility AddButtonVisibility
    {
        get => _addButtonVisibility;
        set => SetProperty(ref _addButtonVisibility, value);
    }

    private Visibility _printNodeVisibility;

    public Visibility PrintNodeVisibility
    {
        get => _printNodeVisibility;
        set => SetProperty(ref _printNodeVisibility, value);
    }

    // Status Bar properties

    public readonly ObservableCollection<string> ViewList = new();

    private string _selectedView;

    public string SelectedView
    {
        get => _selectedView;
        set => SetProperty(ref _selectedView, value);
    }

    public string CurrentView { get; set; }

    private string _statusMessage;

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private SolidColorBrush _statusColor;

    public SolidColorBrush StatusColor
    {
        get => _statusColor;
        set => SetProperty(ref _statusColor, value);
    }


    private string _filterText;

    public string FilterText
    {
        get => _filterText;
        set => SetProperty(ref _filterText, value);
    }

    private Color _changeStatusColor;

    public Color ChangeStatusColor
    {
        get => _changeStatusColor;
        set => SetProperty(ref _changeStatusColor, value);
    }

    private Color _backupStatusColor;

    public Color BackupStatusColor
    {
        get => _backupStatusColor;
        set => SetProperty(ref _backupStatusColor, value);
    }

    #endregion

    #region Static members

    // Static access to the ShellViewModel singleton for
    // change tracking at the application level
    public static ShellViewModel ShellInstance;

    /// <summary>
    ///     If a story element is changed, identify that the StoryModel is changed and needs written
    ///     to the backing store. Also, provide a visual traffic light on the Shell status bar that
    ///     a save is needed.
    /// </summary>
    public static void ShowChange()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = appState.CurrentDocument?.Model;
        if (appState.Headless)
        {
            return;
        }

        if (model?.Changed == true)
        {
            return;
        }

        // Use OutlineService to set changed status with proper separation of concerns
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        outlineService.SetChanged(model, true);
        ShellInstance.ChangeStatusColor = Colors.Red;
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Creates a backup of the current project
    /// </summary>
    public async Task CreateBackupNow()
    {
        if (State.CurrentDocument?.Model?.CurrentView == null || State.CurrentDocument.Model.CurrentView.Count == 0)
        {
            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("You need to load a story first!", LogLevel.Warn)));
            Logger.Log(LogLevel.Info,
                "Failed to open backup menu as CurrentView is null or empty. (Is a story loaded?)");
            return;
        }

        //Show dialog
        var result = await Window.ShowContentDialog(new ContentDialog
        {
            Title = "Create backup now",
            PrimaryButtonText = "Create Backup",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = new BackupNow(Path.GetFileNameWithoutExtension(State.CurrentDocument?.FilePath))
        });

        //Check result
        if (result == ContentDialogResult.Primary)
        {
            var vm = Ioc.Default.GetRequiredService<BackupNowVM>();
            await Ioc.Default.GetRequiredService<BackupService>().BackupProject(vm.Name, vm.Location);
        }
    }

    public async Task MakeBackup()
    {
        await Ioc.Default.GetRequiredService<BackupService>().BackupProject();
    }

    public void TreeViewNodeClicked(object selectedItem, bool clearHighlightCache = true)
    {
        if (selectedItem is null)
        {
            Logger.Log(LogLevel.Info, "TreeViewNodeClicked for null node, event ignored");
            return;
        }

        Logger.Log(LogLevel.Info, $"TreeViewNodeClicked for {selectedItem}");

        try
        {
            if (selectedItem is StoryNodeItem node)
            {
                CurrentNode = node;
                var element = outlineService.GetStoryElementByGuid(State.CurrentDocument!.Model, node.Uuid);
                switch (element.ElementType)
                {
                    case StoryItemType.Character:
                        CurrentPageType = CharacterPage;
                        _navigationService.NavigateTo(SplitViewFrame, CharacterPage, element);
                        break;
                    case StoryItemType.Scene:
                        CurrentPageType = ScenePage;
                        _navigationService.NavigateTo(SplitViewFrame, ScenePage, element);
                        break;
                    case StoryItemType.Problem:
                        CurrentPageType = ProblemPage;
                        _navigationService.NavigateTo(SplitViewFrame, ProblemPage, element);
                        break;
                    case StoryItemType.Section:
                        CurrentPageType = FolderPage;
                        _navigationService.NavigateTo(SplitViewFrame, FolderPage, element);
                        break;
                    case StoryItemType.Folder:
                        CurrentPageType = FolderPage;
                        _navigationService.NavigateTo(SplitViewFrame, FolderPage, element);
                        break;
                    case StoryItemType.Setting:
                        CurrentPageType = SettingPage;
                        _navigationService.NavigateTo(SplitViewFrame, SettingPage, element);
                        break;
                    case StoryItemType.Web:
                        CurrentPageType = WebPage;
                        _navigationService.NavigateTo(SplitViewFrame, WebPage, element);
                        break;
                    case StoryItemType.Notes:
                        CurrentPageType = FolderPage;
                        _navigationService.NavigateTo(SplitViewFrame, FolderPage, element);
                        break;
                    case StoryItemType.StoryOverview:
                        CurrentPageType = OverviewPage;
                        _navigationService.NavigateTo(SplitViewFrame, OverviewPage, element);
                        break;
                    case StoryItemType.TrashCan:
                        CurrentPageType = TrashCanPage;
                        _navigationService.NavigateTo(SplitViewFrame, TrashCanPage, element);
                        break;
                }

                CurrentNode.IsExpanded = true;
            }

            //Clears background of new nodes on navigation as well as the last node.
            if (clearHighlightCache)
            {
                foreach (var item in NewNodeHighlightCache)
                {
                    item.Background = null;
                }

                if (LastClickedTreeviewItem != null)
                {
                    LastClickedTreeviewItem.Background = null;
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogException(LogLevel.Error, e, "Error navigating in ShellVM.TreeViewNodeClicked");
        }
    }

    /// <summary>
    ///     Shows missing env warning.
    /// </summary>
    public async Task ShowDotEnvWarningAsync()
    {
        ContentDialog Dialog = new()
        {
            Title = "Key file missing.",
            Content = """
                      Your version of StoryCAD is missing a key file.
                      If you are a developer you can safely ignore this message.
                      If you are a user you should report this as you are not supposed to see this message.

                      Things such as logging and elmah.io will not work without the key file.
                      You can disable this warning in the dev menu.
                      """,
            PrimaryButtonText = "Okay"
        };
        await Window.ShowContentDialog(Dialog);
        Logger.Log(LogLevel.Error, "Env missing.");
    }

    public void ShowHomePage()
    {
        if (!State.Headless)
        {
            Logger.Log(LogLevel.Info, "ShowHomePage");

            CurrentPageType = HomePage;
            _navigationService.NavigateTo(SplitViewFrame, HomePage);
        }
    }

    private void TogglePane()
    {
        Logger.Log(LogLevel.Trace, $"TogglePane from {IsPaneOpen} to {!IsPaneOpen}");
        IsPaneOpen = !IsPaneOpen;
    }

    /// <summary>
    ///     When an AppBar command button is pressed, the currently active StoryElement ViewModel
    ///     displayed in SplitViewFrame's Content doesn't go through Deactivate() and hence doesn't
    ///     call its WritePreferences() method. Hence this method, which determines which Content
    ///     frame's page type is active, and calls its SaveModel() method. If there are changes,
    ///     the viewmodel is copied back to its corresponding active StoryElement Model.
    /// </summary>
    public void SaveModel()
    {
        // Use CurrentPageType if available, fall back to Frame for backwards compatibility
        var pageType = CurrentPageType;

        // Validate that CurrentPageType matches Frame if both are available
        if (!string.IsNullOrEmpty(pageType) && SplitViewFrame?.CurrentSourcePageType != null)
        {
            var framePageType = SplitViewFrame.CurrentSourcePageType.ToString();
            if (framePageType.StartsWith("StoryCAD.Views."))
            {
                framePageType = framePageType.Replace("StoryCAD.Views.", "");
            }

            if (pageType != framePageType)
            {
                Logger.Log(LogLevel.Error,
                    $"SaveModel: CurrentPageType '{pageType}' doesn't match Frame page type '{framePageType}'. Using Frame type.");
                pageType = framePageType;
            }
        }

        if (string.IsNullOrEmpty(pageType))
        {
            // Fall back to Frame-based detection if CurrentPageType not set
            if (SplitViewFrame == null)
            {
                Logger.Log(LogLevel.Warn, "SaveModel called but SplitViewFrame is null and CurrentPageType not set");
                return;
            }

            if (SplitViewFrame.CurrentSourcePageType is null)
            {
                Logger.Log(LogLevel.Warn, "SaveModel called but no active page type");
                return;
            }

            pageType = SplitViewFrame.CurrentSourcePageType.ToString();
            // Extract just the page name from the full type name
            if (pageType.StartsWith("StoryCAD.Views."))
            {
                pageType = pageType.Replace("StoryCAD.Views.", "");
            }
        }

        Logger.Log(LogLevel.Trace, $"SaveModel Page type={pageType}");

        switch (pageType)
        {
            case OverviewPage:
                var ovm = Ioc.Default.GetRequiredService<OverviewViewModel>();
                ovm.SaveModel();
                break;
            case ProblemPage:
                var pvm = Ioc.Default.GetRequiredService<ProblemViewModel>();
                pvm.SaveModel();
                break;
            case CharacterPage:
                var cvm = Ioc.Default.GetRequiredService<CharacterViewModel>();
                cvm.SaveModel();
                break;
            case ScenePage:
                _sceneViewModel.SaveModel();
                break;
            case FolderPage:
                var folderVm = Ioc.Default.GetRequiredService<FolderViewModel>();
                folderVm.SaveModel();
                break;
            case SettingPage:
                var settingVm = Ioc.Default.GetRequiredService<SettingViewModel>();
                settingVm.SaveModel();
                break;
            case WebPage:
                var webVm = Ioc.Default.GetRequiredService<WebViewModel>();
                webVm.SaveModel();
                break;
            case HomePage:
                // HomePage doesn't have data to save
                Logger.Log(LogLevel.Trace, "HomePage has no data to save");
                break;
            case TrashCanPage:
                // TrashCanPage doesn't have data to save
                Logger.Log(LogLevel.Trace, "TrashCanPage has no data to save");
                break;
            default:
                Logger.Log(LogLevel.Error, $"SaveModel: Unrecognized page type {pageType}");
                break;
        }
    }

    public void ResetModel()
    {
        State.CurrentDocument = new StoryDocument(new StoryModel());
    }

    #endregion

    #region Tool and Report Commands

    public void RefreshAllCommands()
    {
        // Don't update command state during application shutdown
        // COM infrastructure may be shutting down, causing E_UNEXPECTED exceptions
        if (IsClosing)
        {
            return;
        }

        TogglePaneCommand.NotifyCanExecuteChanged();
        OpenFileOpenMenuCommand.NotifyCanExecuteChanged();
        NarrativeToolCommand.NotifyCanExecuteChanged();
        PrintNodeCommand.NotifyCanExecuteChanged();
        OpenFileCommand.NotifyCanExecuteChanged();
        SaveFileCommand.NotifyCanExecuteChanged();
        SaveAsCommand.NotifyCanExecuteChanged();
        CreateBackupCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
        ExitCommand.NotifyCanExecuteChanged();
        CollaboratorCommand.NotifyCanExecuteChanged();
        KeyQuestionsCommand.NotifyCanExecuteChanged();
        TopicsCommand.NotifyCanExecuteChanged();
        MasterPlotsCommand.NotifyCanExecuteChanged();
        DramaticSituationsCommand.NotifyCanExecuteChanged();
        StockScenesCommand.NotifyCanExecuteChanged();
        PreferencesCommand.NotifyCanExecuteChanged();
        PrintReportsCommand.NotifyCanExecuteChanged();
        ExportReportsToPdfCommand.NotifyCanExecuteChanged();
        ScrivenerReportsCommand.NotifyCanExecuteChanged();
        MoveLeftCommand.NotifyCanExecuteChanged();
        MoveRightCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
        AddFolderCommand.NotifyCanExecuteChanged();
        AddSectionCommand.NotifyCanExecuteChanged();
        AddProblemCommand.NotifyCanExecuteChanged();
        AddCharacterCommand.NotifyCanExecuteChanged();
        AddWebCommand.NotifyCanExecuteChanged();
        AddNotesCommand.NotifyCanExecuteChanged();
        AddSettingCommand.NotifyCanExecuteChanged();
        AddSceneCommand.NotifyCanExecuteChanged();
        ConvertToSceneCommand.NotifyCanExecuteChanged();
        ConvertToProblemCommand.NotifyCanExecuteChanged();
        RemoveStoryElementCommand.NotifyCanExecuteChanged();
        RestoreStoryElementCommand.NotifyCanExecuteChanged();
        EmptyTrashCommand.NotifyCanExecuteChanged();
        AddToNarrativeCommand.NotifyCanExecuteChanged();
        RemoveFromNarrativeCommand.NotifyCanExecuteChanged();
    }


    private async void OpenPreferences()
    {
        Messenger.Send(new StatusChangedMessage(new StatusMessage("Updating Preferences", LogLevel.Info, true)));


        //Creates and shows dialog
        Ioc.Default.GetRequiredService<PreferencesViewModel>().LoadModel();
        ContentDialog preferencesDialog = new()
        {
            Content = new PreferencesDialog(),
            Title = "Preferences",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel"
        };

        var result = await Window.ShowContentDialog(preferencesDialog);
        switch (result)
        {
            // Save changes
            case ContentDialogResult.Primary:
                var prefsVm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
                prefsVm.SaveModel();
                await prefsVm.SaveAsync();

                // Notify user if theme changed
                if (prefsVm.ThemeChanged)
                {
                    Messenger.Send(new StatusChangedMessage(
                        new StatusMessage("Theme will be applied when you restart.",
                        LogLevel.Warn, true)));
                }
                else
                {
                    Messenger.Send(new StatusChangedMessage(new StatusMessage("Preferences updated", LogLevel.Info, true)));
                }

                break;
            //don't save changes
            default:
                Messenger.Send(new StatusChangedMessage(new StatusMessage("Preferences closed", LogLevel.Info, true)));
                break;
        }
    }

    /// <summary>
    ///     This method is invoked when the user clicks the Collaborator AppBarButton
    ///     on the Shell CommandBar. It Activates and displays the WizardShell.
    /// </summary>
    private void LaunchCollaborator()
    {
        if (SerializationLock.CanExecuteCommands())
        {
            //if (CurrentNode == null)
            //{
            //    Messenger.Send(new StatusChangedMessage(new("Select a node to collaborate on", LogLevel.Warn, true)));
            //    return;
            //}


            Logger.Log(LogLevel.Info, "Launching collaborator");
            CollabArgs.StoryModel = State.CurrentDocument?.Model;
            Ioc.Default.GetService<CollaboratorService>()!.LoadWorkflows(CollabArgs);
            Ioc.Default.GetService<CollaboratorService>()!.CollaboratorWindow.Activate();
            Ioc.Default.GetService<WorkflowViewModel>()!.EnableNavigation();
            Logger.Log(LogLevel.Info, "Collaborator opened");

        }
    }

    /// <summary>
    ///     This function just calls print reports dialog.
    /// </summary>
    private async void OpenPrintMenu()
    {
        await PrintReportDialog.OpenPrintReportDialog();
    }

    /// <summary>
    ///     Opens PDF UI Flow.
    /// </summary>
    private async void OpenExportPdfMenu()
    {
        await PrintReportDialog.OpenPrintReportDialog(PrintReportDialogVM.ReportOutputMode.Pdf);
    }

    #endregion

    #region Move TreeViewItem Commands

    private void MoveTreeViewItemLeft()
    {
        StatusMessage = string.Empty;
        if (!MoveLeftIsValid())
        {
            return;
        }

        _sourceChildren = CurrentNode.Parent.Children;
        _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
        _targetCollection = null;
        _targetIndex = -1;
        var targetParent = CurrentNode.Parent.Parent;
        // The source must become the parent's successor
        _targetCollection = CurrentNode.Parent.Parent.Children;
        _targetIndex = _targetCollection.IndexOf(CurrentNode.Parent) + 1;

        _sourceChildren.RemoveAt(_sourceIndex);
        if (_targetIndex == -1)
        {
            _targetCollection.Add(CurrentNode);
        }
        else
        {
            _targetCollection.Insert(_targetIndex, CurrentNode);
        }

        CurrentNode.Parent = targetParent;
        ShowChange();
        Logger.Log(LogLevel.Info, $"Moving {CurrentNode.Name} left to parent {CurrentNode.Parent.Name}");
    }

    private bool MoveLeftIsValid()
    {
        if (CurrentNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Click or touch a node to move", LogLevel.Warn)));
            return false;
        }

        if (CurrentNode.Parent != null && CurrentNode.Parent.IsRoot)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Cannot move further left", LogLevel.Warn)));
            return false;
        }

        if (CurrentNode.Parent == null)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Cannot move root node.", LogLevel.Warn)));
            return false;
        }

        return true;
    }

    private void ShowStatusMessage(string message, LogLevel logLevel)
    {
        Messenger.Send(new StatusChangedMessage(new StatusMessage(message, logLevel)));
    }

    private void MoveTreeViewItemRight()
    {
        StatusMessage = string.Empty;
        if (!MoveRightIsValid())
        {
            return;
        }

        if (CurrentNode.Parent != null)
        {
            _sourceChildren = CurrentNode.Parent.Children;
            _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
            _targetCollection = null;
            _targetIndex = -1;
        }
        else
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Cannot move root node.", LogLevel.Warn)));
            return;
        }


        var sourceIndex = _sourceChildren.IndexOf(CurrentNode);
        StoryNodeItem targetParent;
        ObservableCollection<StoryNodeItem> targetCollection;
        int targetIndex;

        if (sourceIndex > 0)
        {
            targetParent = _sourceChildren[sourceIndex - 1];
            targetCollection = targetParent.Children;
            targetIndex = targetCollection.Count;
        }
        else
        {
            var grandparentCollection = CurrentNode.Parent.Parent.Children;
            var siblingIndex = grandparentCollection.IndexOf(CurrentNode.Parent) - 1;

            if (siblingIndex >= 0)
            {
                targetParent = grandparentCollection[siblingIndex];
                targetCollection = targetParent.Children;

                if (targetCollection.Count > 0)
                {
                    targetParent = targetCollection[^1];
                    targetCollection = targetParent.Children;
                    targetIndex = targetCollection.Count;
                }
                else
                {
                    ShowStatusMessage("Cannot move further right", LogLevel.Warn);
                    return;
                }
            }
            else
            {
                ShowStatusMessage("Cannot move further right", LogLevel.Warn);
                return;
            }
        }

        _sourceChildren.RemoveAt(sourceIndex);
        targetCollection.Insert(targetIndex, CurrentNode);
        CurrentNode.Parent = targetParent;
        ShowChange();
        Logger.Log(LogLevel.Info, $"Moving {CurrentNode.Name} right to parent {CurrentNode.Parent.Name}");
    }

    private bool MoveRightIsValid()
    {
        if (CurrentNode == null)
        {
            ShowStatusMessage("Click or touch a node to move", LogLevel.Warn);
            return false;
        }

        if (CurrentNode.Parent == null)
        {
            ShowStatusMessage("Cannot move root node.", LogLevel.Warn);
            return false;
        }

        if (CurrentNode.Parent.Parent == null
            && CurrentNode.Parent.Children.IndexOf(CurrentNode) == 0)
        {
            ShowStatusMessage("Cannot move further right", LogLevel.Warn);
            return false;
        }

        return true;
    }

    private void MoveTreeViewItemUp()
    {
        StatusMessage = string.Empty;
        if (!MoveUpIsValid())
        {
            return;
        }

        _sourceChildren = CurrentNode.Parent.Children;
        _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
        _targetCollection = null;
        _targetIndex = -1;
        var targetParent = CurrentNode.Parent;

        if (_sourceIndex == 0)
        {
            if (CurrentNode.Parent.Parent == null)
            {
                ShowStatusMessage("Cannot move up further", LogLevel.Warn);
                return;
            }

            var grandparentCollection = CurrentNode.Parent.Parent.Children;
            var siblingIndex = grandparentCollection.IndexOf(CurrentNode.Parent) - 1;

            if (siblingIndex >= 0)
            {
                _targetCollection = grandparentCollection[siblingIndex].Children;
                targetParent = grandparentCollection[siblingIndex];
                _targetIndex = _targetCollection.Count;
            }
            else
            {
                ShowStatusMessage("Cannot move up further", LogLevel.Warn);
                return;
            }
        }
        else
        {
            _targetCollection = _sourceChildren;
            _targetIndex = _sourceIndex - 1;
        }

        // Assuming MoveIsValid() returns true for now, as it was a stub in the original code
        _sourceChildren.RemoveAt(_sourceIndex);

        if (_targetIndex == -1)
        {
            _targetCollection.Add(CurrentNode);
        }
        else
        {
            _targetCollection.Insert(_targetIndex, CurrentNode);
        }

        CurrentNode.Parent = targetParent;
        ShowChange();
        Logger.Log(LogLevel.Info, $"Moving {CurrentNode.Name} up to parent {CurrentNode.Parent.Name}");
    }

    private bool MoveUpIsValid()
    {
        if (CurrentNode == null)
        {
            ShowStatusMessage("Click or touch a node to move", LogLevel.Warn);
            return false;
        }

        if (CurrentNode.IsRoot)
        {
            ShowStatusMessage("Cannot move up further", LogLevel.Warn);
            return false;
        }

        return true;
    }

    private void MoveTreeViewItemDown()
    {
        StatusMessage = string.Empty;
        if (!MoveDownIsValid())
        {
            return;
        }

        _sourceChildren = CurrentNode.Parent.Children;
        _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
        _targetCollection = null;
        _targetIndex = 0;
        var targetParent = CurrentNode.Parent;

        // If last child, must move to end parent's successor (sibling node).
        // If there are no siblings, we're at the bottom of the tree?
        if (_sourceIndex == _sourceChildren.Count - 1)
        {
            // Find the next sibling of the parent.
            var nextParentSibling = GetNextSibling(CurrentNode.Parent);

            // If there's no next sibling, then we're at the bottom of the first root's children.
            if (nextParentSibling == null)
            {
                ShowStatusMessage("Cannot move down further", LogLevel.Warn);
                return;
            }

            // If the next sibling is the TrashCan, disallow moving the node to the TrashCan.
            if (nextParentSibling.Type == StoryItemType.TrashCan)
            {
                ShowStatusMessage("Cannot move to trash", LogLevel.Warn);
                return;
            }

            // If the next sibling is not the TrashCan, move the node to the beginning of its children.
            _targetCollection = nextParentSibling.Children;
            targetParent = nextParentSibling;
        }
        // Otherwise, move down a notch
        else
        {
            _targetCollection = _sourceChildren;
            _targetIndex = _sourceIndex + 1;
        }

        _sourceChildren.RemoveAt(_sourceIndex);
        _targetCollection.Insert(_targetIndex, CurrentNode);
        CurrentNode.Parent = targetParent;

        ShowChange();
        Logger.Log(LogLevel.Info, $"Moving {CurrentNode.Name} down up to parent {CurrentNode.Parent.Name}");
    }

    public StoryNodeItem GetNextSibling(StoryNodeItem node)
    {
        if (node.Parent == null)
        {
            return null;
        }

        var parentChildren = node.Parent.Children;
        var currentIndex = parentChildren.IndexOf(node);

        if (currentIndex < parentChildren.Count - 1)
        {
            return parentChildren[currentIndex + 1];
        }

        return null;
    }

    private bool MoveDownIsValid()
    {
        if (CurrentNode == null)
        {
            ShowStatusMessage("Click or touch a node to move", LogLevel.Warn);
            return false;
        }

        if (CurrentNode.IsRoot)
        {
            ShowStatusMessage("Cannot move a root node", LogLevel.Warn);
            return false;
        }

        return true;
    }

    #endregion


    #region MVVM  processing

    private void IsChangedMessageReceived(IsChangedMessage isDirty)
    {
        if (State.CurrentDocument?.Model != null)
        {
            State.CurrentDocument.Model.Changed = isDirty.Value;
        }

        if (State.CurrentDocument?.Model?.Changed == true)
        {
            ChangeStatusColor = Colors.Red;
        }
        else
        {
            ChangeStatusColor = Colors.Green;
        }
    }

    private void BackupStatusMessageReceived(IsBackupStatusMessage isGood)
    {
        if (isGood.Value)
        {
            BackupStatusColor = Colors.Green;
        }
        else
        {
            BackupStatusColor = Colors.Red;
        }
    }

    /// <summary>
    ///     Sends message
    /// </summary>
    /// <param name="level">Is this message an error, warning, info ect</param>
    /// <param name="message">What message should be shown to the user?</param>
    /// <param name="sendToLog">Should this message be sent to the log as well?</param>
    public void ShowMessage(LogLevel level, string message, bool sendToLog)
    {
        Messenger.Send(new StatusChangedMessage(new StatusMessage(message, level, sendToLog)));
    }

    /// <summary>
    ///     This displays a status message and starts a timer for it to be cleared (If Warning or Info.)
    /// </summary>
    /// <param name="statusMessage"></param>
    private void StatusMessageReceived(StatusChangedMessage statusMessage)
    {
        // Bypass if in headless mode or if the app is closing
        if (State.Headless || IsClosing)
        {
            return;
        }

        try
        {
            // Ensure we are on the UI thread
            var dq = Window.GlobalDispatcher;
            if (dq is { HasThreadAccess: false })
            {
                dq.TryEnqueue(() => StatusMessageReceived(statusMessage));
                return;
            }

            if (_statusTimer.IsEnabled)
            {
                _statusTimer.Stop();
            }

            StatusMessage = statusMessage.Value.Status;
            switch (statusMessage.Value.Level)
            {
                case LogLevel.Info:
                    StatusColor = Window.SecondaryColor;
                    _statusTimer.Interval = TimeSpan.FromSeconds(15);
                    _statusTimer.Start();
                    break;
                case LogLevel.Warn:
                    StatusColor = new SolidColorBrush(Colors.Goldenrod);
                    _statusTimer.Interval = TimeSpan.FromSeconds(30);
                    _statusTimer.Start();
                    break;
                case LogLevel.Error:
                    StatusColor = new SolidColorBrush(Colors.Red);
                    break;
                case LogLevel.Fatal:
                    StatusColor = new SolidColorBrush(Colors.DarkRed);
                    break;
            }

            Logger.Log(statusMessage.Value.Level, StatusMessage);
        }
        catch (Exception ex)
        {
            // Log or handle the exception safely
            Logger.LogException(LogLevel.Warn, ex, "StatusMessageReceived failed during shutdown.");
        }
    }


    /// <summary>
    ///     This clears the status message when the timer has ended.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void statusTimer_Tick(object sender, object e)
    {
        _statusTimer.Stop();
        StatusMessage = string.Empty;
    }

    /// <summary>
    ///     Handles application shutdown cleanup. This method is called when the application
    ///     is closing to ensure all resources are properly released and state is saved.
    /// </summary>
    public async Task OnApplicationClosing()
    {
        try
        {
            Logger.Log(LogLevel.Info, "Application closing - starting cleanup");

            // Set closing flag immediately to prevent UI updates during shutdown
            IsClosing = true;

            // 1. Update cumulative time used and save preferences
            var prefs = Ioc.Default.GetRequiredService<PreferenceService>();
            var sessionTime = (DateTime.Now - AppStartTime).TotalSeconds;
            prefs.Model.CumulativeTimeUsed += Convert.ToInt64(sessionTime);
            Logger.Log(LogLevel.Info,
                $"Session time: {sessionTime} seconds, Total time: {prefs.Model.CumulativeTimeUsed} seconds");
            PreferencesIo prfIo = new();
            await prfIo.WritePreferences(prefs.Model);
            Logger.Log(LogLevel.Info, "Preferences saved");

            // 2. Close the current document if one is open
            // This handles AutoSave stop, BackupService stop, and document cleanup
            if (State.CurrentDocument != null)
            {
                Logger.Log(LogLevel.Info, "Closing open document");
                await OutlineManager.CloseFile();
            }

            // 3. Stop the status timer
            if (_statusTimer != null && _statusTimer.IsEnabled)
            {
                _statusTimer.Stop();
                Logger.Log(LogLevel.Debug, "Status timer stopped");
            }

            // 4. Destroy Collaborator service
            try
            {
                var collaboratorService = Ioc.Default.GetRequiredService<CollaboratorService>();
                collaboratorService.DestroyCollaborator();
                Logger.Log(LogLevel.Info, "Collaborator service destroyed");
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Warn, ex, "Error destroying collaborator service during shutdown");
            }

            Logger.Log(LogLevel.Info, "Application cleanup completed successfully");
        }
        catch (Exception ex)
        {
            // Log but don't throw - we want shutdown to complete even if cleanup fails
            Logger.LogException(LogLevel.Error, ex, "Error during application shutdown cleanup");
        }
    }

    /// <summary>
    ///     When a Story Element page's name changes the corresponding
    ///     StoryNodeItem, which is bound to a TreeViewItem, must
    ///     also change. The way this is done is to have the Name field's
    ///     setter send a message here. ShellViewModel knows which
    ///     StoryNodeItem instance is selected (via OnSelectionChanged) and
    ///     alters its Name as well.
    ///     <param name="name"></param>
    /// </summary>
    private void NameMessageReceived(NameChangedMessage name)
    {
        var _msg = name.Value;
        CurrentNode.Name = _msg.NewName;
        switch (CurrentNode.Type)
        {
            case StoryItemType.Character:
                //int charIndex = CharacterModel.CharacterNames.IndexOf(msg.OldName);
                //CharacterModel.CharacterNames[charIndex] = msg.NewName;
                break;
            case StoryItemType.Setting:
                var _settingIndex = SettingModel.SettingNames.IndexOf(_msg.OldName);
                SettingModel.SettingNames[_settingIndex] = _msg.NewName;
                break;
        }
    }

    #endregion
}
