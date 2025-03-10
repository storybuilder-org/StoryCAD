using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StoryCAD.DAL;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Backend;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Collaborator;
using StoryCAD.Services.Dialogs;
using StoryCAD.Services.Dialogs.Tools;
using StoryCAD.Services.Json;
using StoryCAD.Services.Messages;
using StoryCAD.Services.Navigation;
using StoryCAD.Services.Reports;
using StoryCAD.Services.Search;
using StoryCAD.ViewModels.Tools;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Windows.Storage;
using StoryCAD.Services;
using WinUIEx;
using Application = Microsoft.UI.Xaml.Application;
using StoryCAD.Collaborator.ViewModels;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels.SubViewModels;

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
    private readonly Windowing Window;
    private readonly AppState State;
    private readonly PreferenceService Preferences;
    private readonly OutlineService outlineService;

    
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

    public CollaboratorArgs CollabArgs;
   
    public readonly ScrivenerIo Scrivener;

    //File opened (if StoryCAD was opened via an STBX file)
    public string FilePathToLaunch;

    // The right-hand (detail) side of ShellView
    public Frame SplitViewFrame;

    // Drag and drop variables
    private StoryNodeItem dragSourceStoryNode;
    private readonly object dragLock = new ();

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
    //Open/Closes unified menu
    public AsyncRelayCommand OpenUnifiedCommand { get; }
    public RelayCommand CloseUnifiedCommand { get; }

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
    /// DataSource is bound to Shell's NavigationTree TreeView control and
    /// contains either the StoryExplorer (ExplorerView) or StoryNarrator (NarratorView)
    /// ObservableCollection of StoryNodeItem instances.
    /// </summary>
    private ObservableCollection<StoryNodeItem> _dataSource;
    public ObservableCollection<StoryNodeItem> DataSource
    {
        get => _dataSource;
        set
        {
            OutlineManager._canExecuteCommands = false;
            SetProperty(ref _dataSource, value);
            OutlineManager._canExecuteCommands = true;
        }
    }

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

    // CommandBar Flyout AppBarButton properties
    private Visibility _addFolderVisibility;
    public Visibility AddFolderVisibility
    {
        get => _addFolderVisibility;
        set => SetProperty(ref _addFolderVisibility, value);
    }

    private Visibility _addSectionVisibility;
    public Visibility AddSectionVisibility
    {
        get => _addSectionVisibility;
        set => SetProperty(ref _addSectionVisibility, value);
    }

    private Visibility _addProblemVisibility;
    public Visibility AddProblemVisibility
    {
        get => _addProblemVisibility;
        set => SetProperty(ref _addProblemVisibility, value);
    }

    private Visibility _addCharacterVisibility;
    public Visibility AddCharacterVisibility
    {
        get => _addCharacterVisibility;
        set => SetProperty(ref _addCharacterVisibility, value);
    }

    private Visibility _addSettingVisibility;
    public Visibility AddSettingVisibility
    {
        get => _addSettingVisibility;
        set => SetProperty(ref _addSettingVisibility, value);
    }

    private Visibility _addSceneVisibility;
    public Visibility AddSceneVisibility
    {
        get => _addSceneVisibility;
        set => SetProperty(ref _addSceneVisibility, value);
    }

    private Visibility _removeStoryElementVisibility;
    public Visibility RemoveStoryElementVisibility
    {
        get => _removeStoryElementVisibility;
        set => SetProperty(ref _removeStoryElementVisibility, value);
    }

    private Visibility _restoreStoryElementVisibility;
    public Visibility RestoreStoryElementVisibility
    {
        get => _restoreStoryElementVisibility;
        set => SetProperty(ref _restoreStoryElementVisibility, value);
    }

    private Visibility _addToNarrativeVisibility;
    public Visibility AddToNarrativeVisibility
    {
        get => _addToNarrativeVisibility;
        set => SetProperty(ref _addToNarrativeVisibility, value);
    }

    private Visibility _removeFromNarrativeVisibility;
    public Visibility RemoveFromNarrativeVisibility
    {
        get => _removeFromNarrativeVisibility;
        set => SetProperty(ref _removeFromNarrativeVisibility, value);
    }

    private Visibility _printNodeVisibility;
    public Visibility PrintNodeVisibility
    {
        get => _printNodeVisibility;
        set => SetProperty(ref _printNodeVisibility, value);
    }

    private Visibility _emptyTrashVisibility;
    public Visibility EmptyTrashVisibility
    {
        get => _emptyTrashVisibility;
        set => SetProperty(ref _emptyTrashVisibility, value);
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
        StoryModel Model = Ioc.Default.GetRequiredService<OutlineViewModel>().StoryModel;
        if (Ioc.Default.GetRequiredService<AppState>().StoryCADTestsMode) { return; }
        if (Model.Changed) { return; }
        Model.Changed = true;
        ShellInstance.ChangeStatusColor = Colors.Red;
    }

	#endregion

	#region Public Methods
	/// <summary>
	/// Creates a backup of the current project
	/// </summary>
	public async Task CreateBackupNow()
	{
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

	private void CloseUnifiedMenu() { _contentDialog.Hide(); }

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
            NavigationService nav = Ioc.Default.GetRequiredService<NavigationService>();
            if (selectedItem is StoryNodeItem node)
            {
                CurrentNode = node;
                StoryElement element = OutlineManager.StoryModel.StoryElements.StoryElementGuids[node.Uuid];
                switch (element.ElementType)
                {
                    case StoryItemType.Character:
                        nav.NavigateTo(SplitViewFrame, CharacterPage, element);
                        break;
                    case StoryItemType.Scene:
                        nav.NavigateTo(SplitViewFrame, ScenePage, element);
                        break;
                    case StoryItemType.Problem:
                        nav.NavigateTo(SplitViewFrame, ProblemPage, element);
                        break;
                    case StoryItemType.Section:
                        nav.NavigateTo(SplitViewFrame, FolderPage, element);
                        break;
                    case StoryItemType.Folder:
                        nav.NavigateTo(SplitViewFrame, FolderPage, element);
                        break;
                    case StoryItemType.Setting:
                        nav.NavigateTo(SplitViewFrame, SettingPage, element);
                        break;
                    case StoryItemType.Web:
                        nav.NavigateTo(SplitViewFrame, WebPage, element);
                        break;
                    case StoryItemType.Notes:
                        nav.NavigateTo(SplitViewFrame, FolderPage, element);
                        break;
                    case StoryItemType.StoryOverview:
                        nav.NavigateTo(SplitViewFrame, OverviewPage, element);
                        break;
                    case StoryItemType.TrashCan:
                        nav.NavigateTo(SplitViewFrame, TrashCanPage, element);
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
                      """,
            PrimaryButtonText = "Okay"
        };
        await Window.ShowContentDialog(Dialog);
        Ioc.Default.GetRequiredService<LogService>().Log(LogLevel.Error, "Env missing.");
    }

    public void ShowHomePage()
    {
        Logger.Log(LogLevel.Info, "ShowHomePage");

        NavigationService nav = Ioc.Default.GetRequiredService<NavigationService>();
        nav.NavigateTo(SplitViewFrame, HomePage);
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
        if (SplitViewFrame == null || SplitViewFrame.CurrentSourcePageType is null) { return; }

        Logger.Log(LogLevel.Trace, $"SaveModel Page type={SplitViewFrame.CurrentSourcePageType}");

        switch (SplitViewFrame.CurrentSourcePageType.ToString())
        {
            case "StoryCAD.Views.OverviewPage":
                OverviewViewModel ovm = Ioc.Default.GetRequiredService<OverviewViewModel>();
                ovm.SaveModel();
                break;
            case "StoryCAD.Views.ProblemPage":
                ProblemViewModel pvm = Ioc.Default.GetRequiredService<ProblemViewModel>();
                pvm.SaveModel();
                break;
            case "StoryCAD.Views.CharacterPage":
                CharacterViewModel cvm = Ioc.Default.GetRequiredService<CharacterViewModel>();
                cvm.SaveModel();
                break;
            case "StoryCAD.Views.ScenePage":
                SceneViewModel scvm = Ioc.Default.GetRequiredService<SceneViewModel>();
                scvm.SaveModel();
                break;
            case "StoryCAD.Views.FolderPage":
                FolderViewModel folderVm = Ioc.Default.GetRequiredService<FolderViewModel>();
                folderVm.SaveModel();
                break;
            case "StoryCAD.Views.SettingPage":
                SettingViewModel settingVm = Ioc.Default.GetRequiredService<SettingViewModel>();
                settingVm.SaveModel();
                break;
            case "StoryCAD.Views.WebPage":
                WebViewModel webVm = Ioc.Default.GetRequiredService<WebViewModel>();
                webVm.SaveModel();
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
        if (OutlineManager._canExecuteCommands)
        {
            if (CurrentNode == null)
            {
                Messenger.Send(new StatusChangedMessage(new("Select a node to collaborate on", LogLevel.Warn, true)));
                return;
            }

            //TODO: Logging???
            
            var id = CurrentNode.Uuid; // get the story element;
            CollabArgs.SelectedElement = OutlineManager.StoryModel.StoryElements.StoryElementGuids[id];
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
        if (DataSource == null || DataSource.Count == 0)
        {
            Messenger.Send(new StatusChangedMessage(new("You need to load a story first!", LogLevel.Warn)));
            Logger.Log(LogLevel.Info, "Failed to switch views as DataSource is null or empty. (Is a story loaded?)");
            return;
        }
        if (!SelectedView.Equals(CurrentView))
        {
            CurrentView = SelectedView;
            switch (CurrentView)
            {
                case "Story Explorer View":
                    SetCurrentView(StoryViewType.ExplorerView);
                    break;
                case "Story Narrator View":
                    SetCurrentView(StoryViewType.NarratorView);
                    break;
            }
            TreeViewNodeClicked(Ioc.Default.GetRequiredService<ShellViewModel>().DataSource[0]);
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
                AddFolderVisibility = Visibility.Collapsed;
                AddSectionVisibility = Visibility.Collapsed;
                AddProblemVisibility = Visibility.Collapsed;
                AddCharacterVisibility = Visibility.Collapsed;
                AddSettingVisibility = Visibility.Collapsed;
                AddSceneVisibility = Visibility.Collapsed;
                RemoveStoryElementVisibility = Visibility.Collapsed;
                AddToNarrativeVisibility = Visibility.Collapsed;
                RemoveFromNarrativeVisibility = Visibility.Collapsed;
                PrintNodeVisibility = Visibility.Collapsed;

                RestoreStoryElementVisibility = Visibility.Visible;
                EmptyTrashVisibility = Visibility.Visible;
            }
            else
            {
                //Explorer tree, show everything but empty trash and add section
                if (SelectedView == ViewList[0])
                {
                    AddFolderVisibility = Visibility.Visible;
                    AddSectionVisibility = Visibility.Collapsed;
                    AddProblemVisibility = Visibility.Visible;
                    AddCharacterVisibility = Visibility.Visible;
                    AddSettingVisibility = Visibility.Visible;
                    AddSceneVisibility = Visibility.Visible;
                    RemoveStoryElementVisibility = Visibility.Visible;
                    //TODO: Use correct values (bug with this)
                    //RestoreStoryElementVisibility = Visibility.Collapsed;
                    RestoreStoryElementVisibility = Visibility.Collapsed;
                    AddToNarrativeVisibility = Visibility.Visible;
                    //RemoveFromNarrativeVisibility = Visibility.Collapsed;
                    PrintNodeVisibility = Visibility.Visible;
                    EmptyTrashVisibility = Visibility.Collapsed;
                }
                else //Narrator Tree, hide most things.
                {
                    RemoveStoryElementVisibility = Visibility.Visible;
                    RemoveFromNarrativeVisibility = Visibility.Visible;
                    AddSectionVisibility = Visibility.Visible;
                    PrintNodeVisibility = Visibility.Visible;

                    AddFolderVisibility = Visibility.Collapsed;
                    AddProblemVisibility = Visibility.Collapsed;
                    AddCharacterVisibility = Visibility.Collapsed;
                    AddSettingVisibility = Visibility.Collapsed;
                    AddSceneVisibility = Visibility.Collapsed;
                    RestoreStoryElementVisibility = Visibility.Collapsed;
                    AddToNarrativeVisibility = Visibility.Collapsed;
                    EmptyTrashVisibility = Visibility.Collapsed;
                }
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

    public void SetCurrentView(StoryViewType view)
    {
        if (view == StoryViewType.ExplorerView)
        {
            DataSource = OutlineManager.StoryModel.ExplorerView;
            SelectedView = ViewList[0];
            CurrentViewType = StoryViewType.ExplorerView;
        }
        else if (view == StoryViewType.NarratorView)
        {
            DataSource = OutlineManager.StoryModel.NarratorView;
            SelectedView = ViewList[1];
            CurrentViewType = StoryViewType.NarratorView;
        }
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
        //Ignore status messages inside tests
        if (State.StoryCADTestsMode) { return; }

        if (_statusTimer.IsEnabled) { _statusTimer.Stop(); } //Stops a timer if one is already running

        StatusMessage = statusMessage.Value.Status; //This shows the message

        switch (statusMessage.Value.Level)
        {
            case LogLevel.Info:
                StatusColor = Window.SecondaryColor;
                _statusTimer.Interval = new TimeSpan(0, 0, 15);  // Timer will tick in 15 seconds
                _statusTimer.Start();
                break;
            case LogLevel.Warn:
                StatusColor = new SolidColorBrush(Colors.Yellow);
                _statusTimer.Interval = new TimeSpan(0, 0, 30); // Timer will tick in 30 seconds
                _statusTimer.Start();
                break;
            case LogLevel.Error: // Timer won't be started
                StatusColor = new SolidColorBrush(Colors.Red);
                break;
            case LogLevel.Fatal: // Timer won't be started
                StatusColor = new SolidColorBrush(Colors.DarkRed);
                break;
        }
        Logger.Log(statusMessage.Value.Level, statusMessage.Value.Status);
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


    #region Drag and Drop logic

    /// <summary>
    /// Edit to validate a drag and drop source node.
    ///
    /// This routing is called from the Shell.xaml.cs DragItemsStarting event.
    /// </summary>
    /// <param name="source">object (should be StoryNodeItem)</param>
    /// <returns>true if edits passed, false if not</returns>
    public bool ValidateDragSource(object source)
    {
        Logger.Log(LogLevel.Trace, $"ValidateDragSource enter");

        // args.Items[0] is the object you're dragging.
        // With SelectionMode="Single" there will be only the one.
        Type type = source.GetType();
        if (!type.Name.Equals("StoryNodeItem"))
        {
            ShowMessage(LogLevel.Warn, "Drag source isn't a tree node", true);
            return false;
        }

        dragSourceStoryNode = source as StoryNodeItem;
        Logger.Log(LogLevel.Trace, $"  Source node:{dragSourceStoryNode?.Name ?? "null"}");

        if (dragSourceStoryNode!.IsRoot)
        {
            ShowMessage(LogLevel.Warn, "Can't drag the tree root", true);
            return false;
        }

        StoryNodeItem parent = dragSourceStoryNode!.Parent;
        if (parent == null)
        {
            ShowMessage(LogLevel.Warn, "Can't drag from root", true);
            return false;
        }

        while (!parent.IsRoot) // find the drag source's root
        {
            parent = parent.Parent;
        }

        Logger.Log(LogLevel.Trace, $"Root Type is {parent.Type}");

        if (parent.Type == StoryItemType.TrashCan)
        {
            ShowMessage(LogLevel.Warn, "Can't drag from Trashcan", true);
            return false;
        }

        // Report status
        Logger.Log(LogLevel.Trace, $"ValidateDragSource exit");
        // Source node is valid for move
        return true;
    }

    /// <summary>
    /// Edit to validate a drag and drop  target node.
    ///
    /// This routing is called from the Shell.xaml.cs DragEnter event.
    /// </summary>
    /// <param name="target">The StoryNodeItem bound to the TreeViewItem being dragged over</param>
    /// <returns>true if edits passed, false if not</returns>
    public bool ValidateDragTarget(object target)
    {
        Logger.Log(LogLevel.Trace, $"ValidateDragTarget enter");

        // target is the node you're dragging over (the prospective target)
        Type type = target.GetType();
        if (!type.Name.Equals("StoryNodeItem"))
        {
            ShowMessage(LogLevel.Warn, "Drag target isn't a story node", true);
            return false;
        }

        var dragTargetStoryNode = target as StoryNodeItem;
        Logger.Log(LogLevel.Trace, $"  Target node:{dragTargetStoryNode?.Name ?? "null"}");

        // Find the node's root
        var node= dragTargetStoryNode;
        while (!node.IsRoot) // find the drag source's root
        {
            node = node.Parent;
        }
        Logger.Log(LogLevel.Trace, $"Root Type is {node.Type}");

        // Although for the drag source either root node is not valid, 
        // as a target the first root (StoryOverview) is.
        if (node!.Type == StoryItemType.TrashCan)
        {
            ShowMessage(LogLevel.Warn, "Drag to Trashcan invalid", true);
            return false;
        }

        // The drag target can't be the drag source
        if (dragTargetStoryNode == dragSourceStoryNode)
        {
            ShowMessage(LogLevel.Warn, "Drag source can't be drag target", true);
            return false;
        }

        // Target node is valid for move
        Logger.Log(LogLevel.Trace, $"ValidateDragTarget exit");
        return true;
    }
    
    /// <summary>
    /// The visual feedback provided for drag-over effects can leave the TreeView
    /// display hosed in some cases (such as attempting to drag the tree's root
    /// node, for example.) This method repaints the TreeView to clean things up.
    ///
    /// Drag and drop can be used for either view (Explorer or Narrator), and so
    /// both cases must be accounted for.
    /// </summary>


    /// <summary>
    /// Determine if a prospective NavigationTree drag-and-drop move is invalid.
    /// This includes both edits on 
    /// </summary>
    /// <param name="source">The source node being dragged.</param>
    /// <param name="target">The target node for the drop.</param>
    /// <returns>True if the move is invalid; otherwise, false.</returns>
    public bool ValidateDragAndDrop(StoryNodeItem source, StoryNodeItem target)
    {
        if (source == null)
        {
            Logger.Log(LogLevel.Trace, $"Source is null.");
            return false;
        }

        if (target == null)
        {
            Logger.Log(LogLevel.Trace, $"Target is null.");
            return false;
        }

        if (IsDescendant(source, target))
        {
            ShowMessage(LogLevel.Warn, "Cannot move a parent to its own child", true);
            return false;
        }

        if (target.Type == StoryItemType.TrashCan || source.Type == StoryItemType.TrashCan)
        {
            ShowMessage(LogLevel.Warn, "Cannot move to/from the trashcan", true);
            return false;
        }

        if (IsDescendant(OutlineManager.StoryModel.ExplorerView[1], target) ||
            IsDescendant(OutlineManager.StoryModel.ExplorerView[1], source))
        {
            ShowMessage(LogLevel.Warn, "Operation involves trashcan", true);
            return false;
        }

        // If none of the conditions are met, the move is considered valid.
        Logger.Log(LogLevel.Trace, $"Drag/Drop Operation is valid.");
        return true;
    }

    /// <summary>
    /// Recursive method to check if StoryNodeItem 'x' is a descendant of StoryNodeItem 'y'.
    /// </summary>
    /// <param name="y">The potential ancestor node.</param>
    /// <param name="x">The node to check if it is a descendant of 'y'.</param>
    /// <returns>True if 'x' is a descendant of 'y'; otherwise, false.</returns>
    public bool IsDescendant(StoryNodeItem y, StoryNodeItem x)
    {
        foreach (var child in y.Children)
        {
            if (child == x || IsDescendant(child, x))
            {
                Logger.Log(LogLevel.Trace, $"{y.Name} is a descendant of {x.Name}");
                return true;
            }
        }
        Logger.Log(LogLevel.Trace, $"{y.Name} is not a descendant of {x.Name}");

        return false;
    }

    /// <summary>
    /// If both the source and target of a drag/drop are valid (that is, the
    /// requested operation is a valid reordering of the nodes of the Navigation
    /// TreeView), complete the move by modifying the TreeView's DataSource
    /// ObservableCollection of StoryNodeItem instances.
    /// </summary>
    public void MoveStoryNode(StoryNodeItem dragSourceStoryNode, StoryNodeItem dragTargetStoryNode, DragAndDropDirection direction)
    {
        lock (dragLock)
        {
            int targetIndex = -1;
            try
            {
                bool sourceIsTargetDescendant = IsDescendant(dragTargetStoryNode, dragSourceStoryNode);

                StoryNodeItem sourceParent = dragSourceStoryNode.Parent;
                if (sourceParent == null || !sourceParent.Children.Contains(dragSourceStoryNode))
                {
                    throw new InvalidOperationException("Source node is not a child of its parent or parent is null.");
                }
                sourceParent.Children.Remove(dragSourceStoryNode);

                if (dragTargetStoryNode.IsRoot || sourceIsTargetDescendant ||
                    dragTargetStoryNode.Type == StoryItemType.Folder || dragTargetStoryNode.Type == StoryItemType.Section)
                {
                    dragTargetStoryNode.Children.Insert(0, dragSourceStoryNode);
                    dragSourceStoryNode.Parent = dragTargetStoryNode;
                }
                else
                {
                    StoryNodeItem targetParent = dragTargetStoryNode.Parent;
                    if (targetParent == null || !targetParent.Children.Contains(dragTargetStoryNode))
                    {
                        throw new InvalidOperationException("Target node's parent is null or target node not found in its parent's children collection.");
                    }

                    targetIndex = targetParent.Children.IndexOf(dragTargetStoryNode);

                    // Check if inserting above or below the target node
                    if (direction == DragAndDropDirection.AboveTargetItem)
                    {
                        // Pushes dragTargetStoryNode and all subsequent nodes forward
                        targetParent.Children.Insert(targetIndex, dragSourceStoryNode);
                    }
                    else
                    {
                        // Adds just after dragTargetStoryNode. If dragTargetStoryNode's
                        // index equals Count, it's added at the end of the list.
                        targetParent.Children.Insert(targetIndex + 1, dragSourceStoryNode);
                    }

                    dragSourceStoryNode.Parent = targetParent;
                }

                ShowChange();  // Report the move
                ShowMessage(LogLevel.Info, "Drag and drop successful", false);
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Error in drag-drop operation");
                ShowMessage(LogLevel.Error, "Error in drag-drop operation", false);

                // Log the target index for debugging
                Logger.Log(LogLevel.Error, $"Target Index: {targetIndex}");
            }
        }

        // Refresh UI and report the move
        ShellViewModel.ShowChange();
    }

    #endregion


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

    internal void LockCommands()
    {
        OutlineManager._canExecuteCommands = false;
    }

    internal void UnlockCommands()
    {
        OutlineManager._canExecuteCommands = false;
    }

    #endregion

    #region Constructor(s)

    public ShellViewModel()
    {
        // Resolve services via Ioc as needed
        Logger = Ioc.Default.GetRequiredService<LogService>();
        Search = Ioc.Default.GetRequiredService<SearchService>();
        _autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        Window = Ioc.Default.GetRequiredService<Windowing>();
        State = Ioc.Default.GetRequiredService<AppState>();
        Scrivener = Ioc.Default.GetRequiredService<ScrivenerIo>();
        Preferences = Ioc.Default.GetRequiredService<PreferenceService>();
        // Resolve sub ViewModels
        OutlineManager = Ioc.Default.GetRequiredService<OutlineViewModel>();
        outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Register inter-MVVM messaging
        Messenger.Register<IsChangedRequestMessage>(this, (_, m) => { m.Reply(OutlineManager.StoryModel!.Changed); });
        Messenger.Register<ShellViewModel, IsChangedMessage>(this, static (r, m) => r.IsChangedMessageReceived(m));
        Messenger.Register<ShellViewModel, StatusChangedMessage>(this, static (r, m) => r.StatusMessageReceived(m));
        Messenger.Register<ShellViewModel, NameChangedMessage>(this, static (r, m) => r.NameMessageReceived(m));

        OutlineManager.StoryModel = new StoryModel();

        //Skip status timer initialization in Tests.
        if (!State.StoryCADTestsMode)
        {
            _statusTimer = new DispatcherTimer();
            _statusTimer.Tick += statusTimer_Tick;
            ChangeStatusColor = Colors.Green;
        }

        Messenger.Send(new StatusChangedMessage(new("Ready", LogLevel.Info)));

        OutlineManager._canExecuteCommands = true;
        TogglePaneCommand = new RelayCommand(TogglePane, () => OutlineManager._canExecuteCommands);
        //OpenUnifiedCommand = new RelayCommand(async () => await OutlineManager.OpenUnifiedMenu(), () => OutlineManager._canExecuteCommands);
        OpenUnifiedCommand = new AsyncRelayCommand(OutlineManager.OpenUnifiedMenu, canExecute: () => OutlineManager._canExecuteCommands);  
        CloseUnifiedCommand = new RelayCommand(CloseUnifiedMenu, () => OutlineManager._canExecuteCommands);
        NarrativeToolCommand = new RelayCommand(async () => await Ioc.Default.GetRequiredService<NarrativeToolVM>().OpenNarrativeTool(), () => OutlineManager._canExecuteCommands);
        PrintNodeCommand = new RelayCommand(async () => await OutlineManager.PrintCurrentNodeAsync(), () => OutlineManager._canExecuteCommands);
        OpenFileCommand = new RelayCommand(async () => await OutlineManager.OpenFile(), () => OutlineManager._canExecuteCommands);
        SaveFileCommand = new RelayCommand(async () => await OutlineManager.SaveFile(), () => OutlineManager._canExecuteCommands);
        SaveAsCommand = new RelayCommand(async () => OutlineManager.SaveFileAs(), () => OutlineManager._canExecuteCommands);
        CreateBackupCommand = new RelayCommand(async () => await CreateBackupNow(), () => OutlineManager._canExecuteCommands);
        CloseCommand = new RelayCommand(async () => await OutlineManager.CloseFile(), () => OutlineManager._canExecuteCommands);
        ExitCommand = new RelayCommand(async () => await OutlineManager.ExitApp(), () => OutlineManager._canExecuteCommands);

        // StoryCAD Collaborator
        CollaboratorCommand = new RelayCommand(LaunchCollaborator, () => OutlineManager._canExecuteCommands);

        // Tools commands
        KeyQuestionsCommand = new RelayCommand(async () => await OutlineManager.KeyQuestionsTool(), () => OutlineManager._canExecuteCommands);
        TopicsCommand = new RelayCommand(async () => await OutlineManager.TopicsTool(), () => OutlineManager._canExecuteCommands);
        MasterPlotsCommand = new RelayCommand(async () => await OutlineManager.MasterPlotTool(), () => OutlineManager._canExecuteCommands);
        DramaticSituationsCommand = new RelayCommand(async () => await OutlineManager.DramaticSituationsTool(), () => OutlineManager._canExecuteCommands);
        StockScenesCommand = new RelayCommand(async () => await OutlineManager.StockScenesTool(), () => OutlineManager._canExecuteCommands);

        PreferencesCommand = new RelayCommand(OpenPreferences, () => OutlineManager._canExecuteCommands);

        PrintReportsCommand = new RelayCommand(OpenPrintMenu, () => OutlineManager._canExecuteCommands);
        ScrivenerReportsCommand = new RelayCommand(async () => await OutlineManager.GenerateScrivenerReports(), () => OutlineManager._canExecuteCommands);

        HelpCommand = new RelayCommand(LaunchGitHubPages);

        // Move StoryElement commands
        MoveLeftCommand = new RelayCommand(MoveTreeViewItemLeft, () => OutlineManager._canExecuteCommands);
        MoveRightCommand = new RelayCommand(MoveTreeViewItemRight, () => OutlineManager._canExecuteCommands);
        MoveUpCommand = new RelayCommand(MoveTreeViewItemUp, () => OutlineManager._canExecuteCommands);
        MoveDownCommand = new RelayCommand(MoveTreeViewItemDown, () => OutlineManager._canExecuteCommands);

        // Add StoryElement commands
        AddFolderCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Folder), () => OutlineManager._canExecuteCommands);
        AddSectionCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Section), () => OutlineManager._canExecuteCommands);
        AddProblemCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Problem), () => OutlineManager._canExecuteCommands);
        AddCharacterCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Character), () => OutlineManager._canExecuteCommands);
        AddWebCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Web), () => OutlineManager._canExecuteCommands);
        AddNotesCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Notes), () => OutlineManager._canExecuteCommands);
        AddSettingCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Setting), () => OutlineManager._canExecuteCommands);
        AddSceneCommand = new RelayCommand(() => OutlineManager.AddStoryElement(StoryItemType.Scene), () => OutlineManager._canExecuteCommands);

        // Remove Story Element command (move to trash)
        RemoveStoryElementCommand = new RelayCommand(OutlineManager.RemoveStoryElement, () => OutlineManager._canExecuteCommands);
        RestoreStoryElementCommand = new RelayCommand(OutlineManager.RestoreStoryElement, () => OutlineManager._canExecuteCommands);
        EmptyTrashCommand = new RelayCommand(OutlineManager.EmptyTrash, () => OutlineManager._canExecuteCommands);
        // Copy to Narrative command
        AddToNarrativeCommand = new RelayCommand(OutlineManager.CopyToNarrative, () => OutlineManager._canExecuteCommands);
        RemoveFromNarrativeCommand = new RelayCommand(OutlineManager.RemoveFromNarrative, () => OutlineManager._canExecuteCommands);

        ViewList.Add("Story Explorer View");
        ViewList.Add("Story Narrator View");

        CurrentView = "Story Explorer View";
        SelectedView = "Story Explorer View";

        ShellInstance = this;
    }

    #endregion
}