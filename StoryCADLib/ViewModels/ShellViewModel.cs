using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StoryCAD.DAL;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Collaborator;
using StoryCAD.Services.Dialogs;
using StoryCAD.Services.Dialogs.Tools;
using StoryCAD.Services.Json;
using StoryCAD.Services.Messages;
using StoryCAD.Services.Navigation;
using StoryCAD.Services.Search;
using StoryCAD.ViewModels.Tools;
using System.Collections.ObjectModel;
using System.Diagnostics;
using StoryCAD.Services;
using WinUIEx;
using StoryCAD.Collaborator.ViewModels;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.Services.Locking;

namespace StoryCAD.ViewModels;

public class ShellViewModel : ObservableRecipient
{
    #region SubViewModels
    // ShellViewModel uses 'sub viewmodels' for different aspects in order
    // to break this very large viewmodel into more manageable pieces.
    public OutlineViewModel OutlineManager { get; }   
    #endregion

    private const string HomePage = "HomePage";
    private const string OverviewPage = "OverviewPage";
    private const string ProblemPage = "ProblemPage";
    private const string CharacterPage = "CharacterPage";
    private const string ScenePage = "ScenePage";
    private const string FolderPage = "FolderPage";
    private const string SettingPage = "SettingPage";
    private const string TrashCanPage = "TrashCanPage";
    private const string WebPage = "WebPage";

    // Required services 
    private readonly LogService Logger;
    private readonly SearchService Search;
    public readonly AutoSaveService _autoSaveService;
    public readonly BackupService _BackupService;
    private readonly Windowing Window;
    private readonly AppState State;
    private readonly PreferenceService Preferences;
    private readonly OutlineService outlineService;
    private readonly SceneViewModel _sceneViewModel;
    private readonly NavigationService _navigationService;

    
    // Navigation navigation landmark nodes
    public StoryNodeItem CurrentNode { get; set; }
    public StoryNodeItem RightTappedNode;
    public TreeViewItem LastClickedTreeviewItem;

    //List of new nodes that have a background, these are cleared on navigation
    public List<StoryNodeItem> NewNodeHighlightCache = new();

    public StoryViewType CurrentViewType;
    public ContentDialog _contentDialog;
    private int _sourceIndex;
    public ObservableCollection<StoryNodeItem> _sourceChildren;
    private int _targetIndex;
    private ObservableCollection<StoryNodeItem> _targetCollection;

    private readonly DispatcherTimer _statusTimer;
    public bool IsClosing;

    public CollaboratorArgs CollabArgs;
   
    public readonly ScrivenerIo Scrivener;

    //File opened (if StoryCAD was opened via an STBX file)
    public string FilePathToLaunch;

    // The right-hand (detail) side of ShellView
    public Frame SplitViewFrame;
    
    // Track the current page type for SaveModel (testable without UI)
    public string CurrentPageType { get; set; }


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
    public RelayCommand RemoveStoryElementCommand { get; }
    public RelayCommand RestoreStoryElementCommand { get; }
    public RelayCommand EmptyTrashCommand { get; }

    // Copy to Narrative command
    public RelayCommand AddToNarrativeCommand { get; }
    public RelayCommand RemoveFromNarrativeCommand { get; }

    #endregion

    #region Shell binding properties


    /// <summary>
    /// IsPaneOpen is bound to ShellSplitView's IsPaneOpen property with
    /// two-way binding, so that it can read and update the property.
    /// If set to true, the left pane (TreeView) is expanded to its full width;
    /// otherwise, the left pane is collapsed. Default to true (expanded).
    /// </summary>
    private bool _isPaneOpen = true;
    public bool IsPaneOpen
    {
        get => _isPaneOpen;
        set => SetProperty(ref _isPaneOpen, value);
    }


    private Visibility _explorerVisibility;
    /// <summary>
    /// Controls visibility for elements that should only be shown in the Explorer view.
    /// </summary>
    public Visibility ExplorerVisibility
    {
        get => _explorerVisibility;
        set => SetProperty(ref _explorerVisibility, value);
    }

    private Visibility _narratorVisibility;
    /// <summary>
    /// Controls visibility for elements that should only be shown in the Narrator view.
    /// </summary>
    public Visibility NarratorVisibility
    {
        get => _narratorVisibility;
        set => SetProperty(ref _narratorVisibility, value);
    }

    private Visibility _trashButtonVisibility;
    /// <summary>
    /// Controls visibility for elements that should only be shown in the Trash view.
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

    private string _currentView;
    public string CurrentView
    {
        get => _currentView;
        set => _currentView = value;
    }

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

    private Windows.UI.Color _changeStatusColor;
    public Windows.UI.Color ChangeStatusColor
    {
        get => _changeStatusColor;
        set => SetProperty(ref _changeStatusColor, value);
    }

    private Windows.UI.Color _backupStatusColor;
    public Windows.UI.Color BackupStatusColor
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
    /// If a story element is changed, identify that the StoryModel is changed and needs written 
    /// to the backing store. Also, provide a visual traffic light on the Shell status bar that 
    /// a save is needed.
    /// </summary>
    public static void ShowChange()
    {
        // TODO: Static method requires service locator - consider making non-static in future refactoring
        StoryModel Model = Ioc.Default.GetRequiredService<OutlineViewModel>().StoryModel;
        if (Ioc.Default.GetRequiredService<AppState>().Headless) { return; }
        if (Model.Changed) { return; }
        
        // Use OutlineService to set changed status with proper separation of concerns
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        outlineService.SetChanged(Model, true);
        ShellInstance.ChangeStatusColor = Colors.Red;
    }

	#endregion

	#region Public Methods
	/// <summary>
	/// Creates a backup of the current project
	/// </summary>
	public async Task CreateBackupNow()
	{
        if (OutlineManager.StoryModel?.CurrentView == null || OutlineManager.StoryModel.CurrentView.Count == 0)
        {
            Messenger.Send(new StatusChangedMessage(new("You need to load a story first!", LogLevel.Warn)));
            Logger.Log(LogLevel.Info, "Failed to open backup menu as CurrentView is null or empty. (Is a story loaded?)");
            return;
        }

        //Show dialog
        var result = await Window.ShowContentDialog(new ContentDialog()
		{
			Title = "Create backup now",
			PrimaryButtonText = "Create Backup",
			SecondaryButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Primary,
			Content = new BackupNow()
		});

		//Check result
		if (result == ContentDialogResult.Primary)
		{
			BackupNowVM vm = Ioc.Default.GetRequiredService<BackupNowVM>();
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
                StoryElement element = outlineService.GetStoryElementByGuid(OutlineManager.StoryModel, node.Uuid);
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
                foreach (var item in NewNodeHighlightCache) { item.Background = null; }
                if (LastClickedTreeviewItem != null) { LastClickedTreeviewItem.Background = null; }
            }
        }
        catch (Exception e)
        {
            Logger.LogException(LogLevel.Error, e, "Error navigating in ShellVM.TreeViewNodeClicked");
        }
    }

    /// <summary>
    /// Shows missing env warning.
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
        Ioc.Default.GetRequiredService<LogService>().Log(LogLevel.Error, "Env missing.");
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
    /// When an AppBar command button is pressed, the currently active StoryElement ViewModel
    /// displayed in SplitViewFrame's Content doesn't go through Deactivate() and hence doesn't
    /// call its WritePreferences() method. Hence this method, which determines which Content
    /// frame's page type is active, and calls its SaveModel() method. If there are changes,
    /// the viewmodel is copied back to its corresponding active StoryElement Model.
    /// </summary>
    public void SaveModel()
    {
        // Use CurrentPageType if available, fall back to Frame for backwards compatibility
        string pageType = CurrentPageType;
        
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
                Logger.Log(LogLevel.Error, $"SaveModel: CurrentPageType '{pageType}' doesn't match Frame page type '{framePageType}'. Using Frame type.");
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
                OverviewViewModel ovm = Ioc.Default.GetRequiredService<OverviewViewModel>();
                ovm.SaveModel();
                break;
            case ProblemPage:
                ProblemViewModel pvm = Ioc.Default.GetRequiredService<ProblemViewModel>();
                pvm.SaveModel();
                break;
            case CharacterPage:
                CharacterViewModel cvm = Ioc.Default.GetRequiredService<CharacterViewModel>();
                cvm.SaveModel();
                break;
            case ScenePage:
                _sceneViewModel.SaveModel();
                break;
            case FolderPage:
                FolderViewModel folderVm = Ioc.Default.GetRequiredService<FolderViewModel>();
                folderVm.SaveModel();
                break;
            case SettingPage:
                SettingViewModel settingVm = Ioc.Default.GetRequiredService<SettingViewModel>();
                settingVm.SaveModel();
                break;
            case WebPage:
                WebViewModel webVm = Ioc.Default.GetRequiredService<WebViewModel>();
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
        OutlineManager.StoryModel = new();
        //TODO: Raise event for StoryModel change?
    }

    #endregion

    #region Tool and Report Commands

    private async void OpenPreferences()
    {
        Messenger.Send(new StatusChangedMessage(new("Updating Preferences", LogLevel.Info, true)));


        //Creates and shows dialog
        Ioc.Default.GetRequiredService<PreferencesViewModel>().LoadModel();
        ContentDialog preferencesDialog = new()
        {
            Content = new PreferencesDialog(),
            Title = "Preferences",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel"
        };

        ContentDialogResult result = await Window.ShowContentDialog(preferencesDialog);
        switch (result)
        {
            // Save changes
            case ContentDialogResult.Primary:
                Ioc.Default.GetRequiredService<PreferencesViewModel>().SaveModel();
                await Ioc.Default.GetRequiredService<PreferencesViewModel>().SaveAsync();
                Messenger.Send(new StatusChangedMessage(new("Preferences updated", LogLevel.Info, true)));

                break;
            //don't save changes
            default:
                Messenger.Send(new StatusChangedMessage(new("Preferences closed", LogLevel.Info, true)));
                break;
        }

    }

    /// <summary>
    /// This method is invoked when the user clicks the Collaborator AppBarButton
    /// on the Shell CommandBar. It Activates and displays the WizardShell. 
    /// </summary>
    private void LaunchCollaborator()
    {
        if (SerializationLock.IsLocked())
        {
            //if (CurrentNode == null)
            //{
            //    Messenger.Send(new StatusChangedMessage(new("Select a node to collaborate on", LogLevel.Warn, true)));
            //    return;
            //}

            //TODO: Logging???
            
            CollabArgs.StoryModel = OutlineManager.StoryModel;
            Ioc.Default.GetService<CollaboratorService>()!.LoadWorkflows(CollabArgs);
            Ioc.Default.GetService<CollaboratorService>()!.CollaboratorWindow.Show();
            Ioc.Default.GetService<WorkflowViewModel>()!.EnableNavigation();
        }
    }

    /// <summary>
    /// This function just calls print reports dialog.
    /// </summary>
    private async void OpenPrintMenu() 
    {
        await Ioc.Default.GetRequiredService<PrintReportDialogVM>().OpenPrintReportDialog();
    }

    #endregion  

    #region Move TreeViewItem Commands

    private void MoveTreeViewItemLeft()
    {
        StatusMessage = string.Empty;
        if (!MoveLeftIsValid()) return;

        _sourceChildren = CurrentNode.Parent.Children;
        _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
        _targetCollection = null;
        _targetIndex = -1;
        StoryNodeItem targetParent = CurrentNode.Parent.Parent;
        // The source must become the parent's successor
        _targetCollection = CurrentNode.Parent.Parent.Children;
        _targetIndex = _targetCollection.IndexOf(CurrentNode.Parent) + 1;

        _sourceChildren.RemoveAt(_sourceIndex);
        if (_targetIndex == -1) { _targetCollection.Add(CurrentNode); }
        else { _targetCollection.Insert(_targetIndex, CurrentNode); }
        CurrentNode.Parent = targetParent;
        ShowChange();
        Logger.Log(LogLevel.Info, $"Moving {CurrentNode.Name} left to parent {CurrentNode.Parent.Name}");
    }

    private bool MoveLeftIsValid()
    {
        if (CurrentNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Click or touch a node to move", LogLevel.Warn)));
            return false;
        }

        if (CurrentNode.Parent != null && CurrentNode.Parent.IsRoot)
        {
            Messenger.Send(new StatusChangedMessage(new("Cannot move further left", LogLevel.Warn)));
            return false;
        }

        if (CurrentNode.Parent == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Cannot move root node.", LogLevel.Warn)));
            return false;
        }

        return true;
    }

    private void ShowStatusMessage(string message, LogLevel logLevel)
    {
        Messenger.Send(new StatusChangedMessage(new(message, logLevel)));
    }

    private void MoveTreeViewItemRight()
    {
        StatusMessage = string.Empty;
        if (!MoveRightIsValid()) return;

        if (CurrentNode.Parent != null)
        {
            _sourceChildren = CurrentNode.Parent.Children;
            _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
            _targetCollection = null;
            _targetIndex = -1;
        }
        else
        {
            Messenger.Send(new StatusChangedMessage(new("Cannot move root node.", LogLevel.Warn)));
            return;
        }


        int sourceIndex = _sourceChildren.IndexOf(CurrentNode);
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
            ObservableCollection<StoryNodeItem> grandparentCollection = CurrentNode.Parent.Parent.Children;
            int siblingIndex = grandparentCollection.IndexOf(CurrentNode.Parent) - 1;

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
        if (!MoveUpIsValid()) return;

        _sourceChildren = CurrentNode.Parent.Children;
        _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
        _targetCollection = null;
        _targetIndex = -1;
        StoryNodeItem targetParent = CurrentNode.Parent;

        if (_sourceIndex == 0)
        {
            if (CurrentNode.Parent.Parent == null)
            {
                ShowStatusMessage("Cannot move up further", LogLevel.Warn);
                return;
            }

            ObservableCollection<StoryNodeItem> grandparentCollection = CurrentNode.Parent.Parent.Children;
            int siblingIndex = grandparentCollection.IndexOf(CurrentNode.Parent) - 1;

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
            _targetCollection.Add(CurrentNode);
        else
            _targetCollection.Insert(_targetIndex, CurrentNode);

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
        if (!MoveDownIsValid()) return;

        _sourceChildren = CurrentNode.Parent.Children;
        _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
        _targetCollection = null;
        _targetIndex = 0;
        StoryNodeItem targetParent = CurrentNode.Parent;

        // If last child, must move to end parent's successor (sibling node).
        // If there are no siblings, we're at the bottom of the tree?
        if (_sourceIndex == _sourceChildren.Count - 1)
        {
            // Find the next sibling of the parent.
            StoryNodeItem nextParentSibling = GetNextSibling(CurrentNode.Parent);

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
            return null;

        ObservableCollection<StoryNodeItem> parentChildren = node.Parent.Children;
        int currentIndex = parentChildren.IndexOf(node);

        if (currentIndex < parentChildren.Count - 1)
            return parentChildren[currentIndex + 1];
        else
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

    public void ViewChanged()
    {
        if (OutlineManager.StoryModel?.CurrentView == null || OutlineManager.StoryModel.CurrentView.Count == 0)
        {
            Messenger.Send(new StatusChangedMessage(new("You need to load a story first!", LogLevel.Warn)));
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
                    outlineService.SetCurrentView(OutlineManager.StoryModel, StoryViewType.ExplorerView);
                    break;
                case "Story Narrator View":
                    outlineService.SetCurrentView(OutlineManager.StoryModel, StoryViewType.NarratorView);
                    break;
            }
            CurrentViewType = OutlineManager.StoryModel.CurrentViewType;
            TreeViewNodeClicked(OutlineManager.StoryModel.CurrentView[0]);
        }
    }

    /// <summary>
    /// This method is called when one of NavigationTree's
    /// TreeViewItem nodes is right-tapped.
    /// 
    /// It alters the visibility of the command bar flyout 
    /// AppBarButtons depending on which portion of the tree 
    /// is tapped and which view (ExplorerView or Navigator) is selected.
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
    /// Opens help menu in the users default browser.
    /// </summary>
    public void LaunchGitHubPages()
    {
        Messenger.Send(new StatusChangedMessage(new("Launching GitHub Pages User Manual", LogLevel.Info, true)));

        Process.Start(new ProcessStartInfo
        {
            FileName = "https://Storybuilder-org.github.io/StoryCAD/",
            UseShellExecute = true
        });

        Messenger.Send(new StatusChangedMessage(new("Launch default browser completed", LogLevel.Info, true)));
    }

    public void ShowConnectionStatus()
    {
        StatusMessage _msg;
        if (!Doppler.DopplerConnection | !Logger.ElmahLogging)
            _msg = new StatusMessage("Connection not established", LogLevel.Warn, true);
        else
            _msg = new StatusMessage("Connection established", LogLevel.Info, true);
        Messenger.Send(new StatusChangedMessage(_msg));
    }


    #region MVVM  processing
    private void IsChangedMessageReceived(IsChangedMessage isDirty)
    {
        OutlineManager.StoryModel.Changed = OutlineManager.StoryModel.Changed || isDirty.Value;
        if (OutlineManager.StoryModel.Changed)
        {
            ChangeStatusColor = Colors.Red;
        }
        else { ChangeStatusColor = Colors.Green; }
    }

    /// <summary>
    /// Sends message
    /// </summary>
    /// <param name="level">Is this message an error, warning, info ect</param>
    /// <param name="message">What message should be shown to the user?</param>
    /// <param name="sendToLog">Should this message be sent to the log as well?</param>
    public void ShowMessage(LogLevel level, string message, bool sendToLog)
    {
        Messenger.Send(new StatusChangedMessage(new(message, level, sendToLog)));
    }

    /// <summary>
    /// This displays a status message and starts a timer for it to be cleared (If Warning or Info.)
    /// </summary>
    /// <param name="statusMessage"></param>
    private void StatusMessageReceived(StatusChangedMessage statusMessage)
    {
        // Bypass if in headless mode or if the app is closing
        if (State.Headless || IsClosing) return;

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
                _statusTimer.Stop();

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
    /// This clears the status message when the timer has ended.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void statusTimer_Tick(object sender, object e)
    {
        _statusTimer.Stop();
        StatusMessage = string.Empty;
    }




    /// <summary>
    /// When a Story Element page's name changes the corresponding
    /// StoryNodeItem, which is bound to a TreeViewItem, must
    /// also change. The way this is done is to have the Name field's
    /// setter send a message here. ShellViewModel knows which
    /// StoryNodeItem instance is selected (via OnSelectionChanged) and
    /// alters its Name as well.
    /// <param name="name"></param>
    /// </summary>
    private void NameMessageReceived(NameChangedMessage name)
    {
        NameChangeMessage _msg = name.Value;
        CurrentNode.Name = _msg.NewName;
        switch (CurrentNode.Type)
        {
            case StoryItemType.Character:
                //int charIndex = CharacterModel.CharacterNames.IndexOf(msg.OldName);
                //CharacterModel.CharacterNames[charIndex] = msg.NewName;
                break;
            case StoryItemType.Setting:
                int _settingIndex = SettingModel.SettingNames.IndexOf(_msg.OldName);
                SettingModel.SettingNames[_settingIndex] = _msg.NewName;
                break;
        }
    }
    #endregion

    #region Constructor(s)

    // Constructor for XAML compatibility - will be removed later
    public ShellViewModel() : this(
        Ioc.Default.GetRequiredService<SceneViewModel>(),
        Ioc.Default.GetRequiredService<LogService>(),
        Ioc.Default.GetRequiredService<SearchService>(),
        Ioc.Default.GetRequiredService<Windowing>(),
        Ioc.Default.GetRequiredService<AppState>(),
        Ioc.Default.GetRequiredService<ScrivenerIo>(),
        Ioc.Default.GetRequiredService<PreferenceService>(),
        Ioc.Default.GetRequiredService<OutlineViewModel>(),
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<NavigationService>())
    {
    }

    public ShellViewModel(SceneViewModel sceneViewModel, LogService logger, SearchService search,
        Windowing window, AppState appState, ScrivenerIo scrivener, PreferenceService preferenceService,
        OutlineViewModel outlineViewModel, OutlineService outlineService, NavigationService navigationService)
    {
        // Store injected services
        _sceneViewModel = sceneViewModel;
        Logger = logger;
        Search = search;
        // TODO: Circular dependency - AutoSaveService and BackupService depend on ShellViewModel
        // Temporary workaround: Use service locator until architectural fix
        _autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        _BackupService = Ioc.Default.GetRequiredService<BackupService>();
        Window = window;
        State = appState;
        Scrivener = scrivener;
        Preferences = preferenceService;
        _navigationService = navigationService;
        // Store sub ViewModels
        OutlineManager = outlineViewModel;
        this.outlineService = outlineService;

        // Register inter-MVVM messaging
        Messenger.Register<IsChangedRequestMessage>(this, (_, m) => { m.Reply(OutlineManager.StoryModel!.Changed); });
        Messenger.Register<ShellViewModel, IsChangedMessage>(this, static (r, m) => r.IsChangedMessageReceived(m));
        Messenger.Register<ShellViewModel, StatusChangedMessage>(this, static (r, m) => r.StatusMessageReceived(m));
        Messenger.Register<ShellViewModel, NameChangedMessage>(this, static (r, m) => r.NameMessageReceived(m));

        OutlineManager.StoryModel = new StoryModel();

        //Skip status timer initialization in Tests.
        if (!State.Headless)
        {
            _statusTimer = new DispatcherTimer();
            _statusTimer.Tick += statusTimer_Tick;
            ChangeStatusColor = Colors.Green;
            BackupStatusColor = Colors.Green;
        }

        Messenger.Send(new StatusChangedMessage(new("Ready", LogLevel.Info)));

        TogglePaneCommand = new RelayCommand(TogglePane, SerializationLock.IsLocked);
        OpenFileOpenMenuCommand = new AsyncRelayCommand(OutlineManager.OpenFileOpenMenu, canExecute: SerializationLock.IsLocked);  
        NarrativeToolCommand = new RelayCommand(async () => await Ioc.Default.GetRequiredService<NarrativeToolVM>().OpenNarrativeTool(), SerializationLock.IsLocked);
        PrintNodeCommand = new RelayCommand(async () => await OutlineManager.PrintCurrentNodeAsync(), SerializationLock.IsLocked);
        OpenFileCommand = new RelayCommand(async () => await OutlineManager.OpenFile(), SerializationLock.IsLocked);
        SaveFileCommand = new RelayCommand(async () => await OutlineManager.SaveFile(), SerializationLock.IsLocked);
        SaveAsCommand = new RelayCommand(async () => await OutlineManager.SaveFileAs(), SerializationLock.IsLocked);
        CreateBackupCommand = new RelayCommand(async () => await CreateBackupNow(), SerializationLock.IsLocked);
        CloseCommand = new RelayCommand(async () => await OutlineManager.CloseFile(), SerializationLock.IsLocked);
        ExitCommand = new RelayCommand(async () => await OutlineManager.ExitApp(), SerializationLock.IsLocked);

        // StoryCAD Collaborator
        CollaboratorCommand = new RelayCommand(LaunchCollaborator, SerializationLock.IsLocked);

        // Tools commands
        KeyQuestionsCommand = new RelayCommand(async () => await OutlineManager.KeyQuestionsTool(), SerializationLock.IsLocked);
        TopicsCommand = new RelayCommand(async () => await OutlineManager.TopicsTool(), SerializationLock.IsLocked);
        MasterPlotsCommand = new RelayCommand(async () => await OutlineManager.MasterPlotTool(), SerializationLock.IsLocked);
        DramaticSituationsCommand = new RelayCommand(async () => await OutlineManager.DramaticSituationsTool(), SerializationLock.IsLocked);
        StockScenesCommand = new RelayCommand(async () => await OutlineManager.StockScenesTool(), SerializationLock.IsLocked);

        PreferencesCommand = new RelayCommand(OpenPreferences, SerializationLock.IsLocked);

        PrintReportsCommand = new RelayCommand(OpenPrintMenu, SerializationLock.IsLocked);
        ScrivenerReportsCommand = new RelayCommand(async () => await OutlineManager.GenerateScrivenerReports(), SerializationLock.IsLocked);

        HelpCommand = new RelayCommand(LaunchGitHubPages);

        // Move StoryElement commands
        MoveLeftCommand = new RelayCommand(MoveTreeViewItemLeft, SerializationLock.IsLocked);
        MoveRightCommand = new RelayCommand(MoveTreeViewItemRight, SerializationLock.IsLocked);
        MoveUpCommand = new RelayCommand(MoveTreeViewItemUp, SerializationLock.IsLocked);
        MoveDownCommand = new RelayCommand(MoveTreeViewItemDown, SerializationLock.IsLocked);

        // Add StoryElement commands
        AddFolderCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Folder), SerializationLock.IsLocked);
        AddSectionCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Section), SerializationLock.IsLocked);
        AddProblemCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Problem), SerializationLock.IsLocked);
        AddCharacterCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Character), SerializationLock.IsLocked);
        AddWebCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Web), SerializationLock.IsLocked);
        AddNotesCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Notes), SerializationLock.IsLocked);
        AddSettingCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Setting), SerializationLock.IsLocked);
        AddSceneCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Scene), SerializationLock.IsLocked);
        ConvertToSceneCommand = new RelayCommand(OutlineManager.ConvertProblemToScene, SerializationLock.IsLocked);
        ConvertToProblemCommand = new RelayCommand(OutlineManager.ConvertSceneToProblem, SerializationLock.IsLocked);

        // Remove Story Element command (move to trash)
        RemoveStoryElementCommand = new RelayCommand(OutlineManager.RemoveStoryElement, SerializationLock.IsLocked);
        RestoreStoryElementCommand = new RelayCommand(OutlineManager.RestoreStoryElement, SerializationLock.IsLocked);
        EmptyTrashCommand = new RelayCommand(OutlineManager.EmptyTrash, SerializationLock.IsLocked);
        // Copy to Narrative command
        AddToNarrativeCommand = new RelayCommand(OutlineManager.CopyToNarrative, SerializationLock.IsLocked);
        RemoveFromNarrativeCommand = new RelayCommand(OutlineManager.RemoveFromNarrative, SerializationLock.IsLocked);

        ViewList.Add("Story Explorer View");
        ViewList.Add("Story Narrator View");

        CurrentView = "Story Explorer View";
        SelectedView = "Story Explorer View";

        ShellInstance = this;
    }

    #endregion
}