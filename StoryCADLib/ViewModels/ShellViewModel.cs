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

namespace StoryCAD.ViewModels;

public class ShellViewModel : ObservableRecipient
{
    public bool _canExecuteCommands;

    private const string HomePage = "HomePage";
    private const string OverviewPage = "OverviewPage";
    private const string ProblemPage = "ProblemPage";
    private const string CharacterPage = "CharacterPage";
    private const string ScenePage = "ScenePage";
    private const string FolderPage = "FolderPage";
    private const string SettingPage = "SettingPage";
    private const string TrashCanPage = "TrashCanPage";
    private const string WebPage = "WebPage";

    // Navigation navigation landmark nodes
    public StoryNodeItem CurrentNode { get; set; }
    public StoryNodeItem RightTappedNode;
    public TreeViewItem LastClickedTreeviewItem;

    //List of new nodes that have a background, these are cleared on navigation
    public List<StoryNodeItem> NewNodeHighlightCache = new();

    public StoryViewType CurrentViewType;
    private ContentDialog _contentDialog;
    private int _sourceIndex;
    private ObservableCollection<StoryNodeItem> _sourceChildren;
    private int _targetIndex;
    private ObservableCollection<StoryNodeItem> _targetCollection;
    public readonly LogService Logger;
    public readonly SearchService Search;
    private AutoSaveService _autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
    private Windowing Window = Ioc.Default.GetRequiredService<Windowing>();
    private AppState State = Ioc.Default.GetRequiredService<AppState>();
    private PreferenceService Preferences = Ioc.Default.GetRequiredService<PreferenceService>();
    private DispatcherTimer _statusTimer;

    public CollaboratorArgs CollabArgs;
    // The current story outline being processed. 
    public StoryModel StoryModel;

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
    public RelayCommand OpenUnifiedCommand { get; }
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
    /// ///
    /// </summary>
    private ObservableCollection<StoryNodeItem> _dataSource;
    public ObservableCollection<StoryNodeItem> DataSource
    {
        get => _dataSource;
        set
        {
            _canExecuteCommands = false;
            SetProperty(ref _dataSource, value);
            _canExecuteCommands = true;
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

    public static StoryModel GetModel()
    {
        return ShellInstance.StoryModel;
    }

    /// <summary>
    /// If a story element is changed, identify that
    /// the StoryModel is changed and needs written 
    /// to the backing store. Also, provide a visual
    /// traffic light on the Shell status bar that 
    /// a save is needed.
    /// </summary>
    public static void ShowChange()
    {
        if (Ioc.Default.GetRequiredService<AppState>().StoryCADTestsMode) { return; }
        if (ShellInstance.StoryModel.Changed) { return; }
        ShellInstance.StoryModel.Changed = true;
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
		var Result = await Window.ShowContentDialog(new ContentDialog()
		{
			Title = "Create backup now",
			PrimaryButtonText = "Create Backup",
			SecondaryButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Primary,
			Content = new BackupNow()
		});

		//Check result
		if (Result == ContentDialogResult.Primary)
		{
			BackupNowVM VM = Ioc.Default.GetRequiredService<BackupNowVM>();
			await Ioc.Default.GetRequiredService<BackupService>().BackupProject(VM.Name, VM.Location);
		}
	}

	public async Task PrintCurrentNodeAsync()
    {
	    _canExecuteCommands = false;

		if (RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Right tap a node to print", LogLevel.Warn)));
            Logger.Log(LogLevel.Info, "Print node failed as no node is selected");
            _canExecuteCommands = true;
            return;
        }
        await Ioc.Default.GetRequiredService<PrintReportDialogVM>().PrintSingleNode(RightTappedNode);
        _canExecuteCommands = true;
	}

	private void CloseUnifiedMenu() { _contentDialog.Hide(); }

    public async Task OpenUnifiedMenu()
    {
        if (_canExecuteCommands)
        {
            _canExecuteCommands = false;
            // Needs logging
            _contentDialog = new() { Content = new UnifiedMenuPage() };
            if (Window.RequestedTheme == ElementTheme.Light)
            {
                _contentDialog.RequestedTheme = Window.RequestedTheme;
                _contentDialog.Background = new SolidColorBrush(Colors.LightGray);
            }
            await Window.ShowContentDialog(_contentDialog);
            _canExecuteCommands = true;
        }
    }

    public async Task UnifiedNewFile(UnifiedVM dialogVM)
    {
        Logger.Log(LogLevel.Info, "FileOpenVM - New File starting");
        _canExecuteCommands = false;

        try
        {
            Messenger.Send(new StatusChangedMessage(new("New project command executing", LogLevel.Info)));

            // If the current project needs saved, do so
            if (StoryModel.Changed)
            {
                SaveModel();
                await WriteModel();
            }

            // Start with a blank StoryModel and populate it using the new project dialog's 
            ResetModel();
            ShowHomePage();

            if (!Path.GetExtension(dialogVM.ProjectName)!.Equals(".stbx")) { dialogVM.ProjectName += ".stbx"; }
            StoryModel.ProjectFilename = dialogVM.ProjectName;
            StoryModel.ProjectFolder = await StorageFolder.GetFolderFromPathAsync(dialogVM.ProjectPath);
            StoryModel.ProjectPath = StoryModel.ProjectFolder.Path;
            CreateTemplate(dialogVM.ProjectName, dialogVM.SelectedTemplateIndex);
            SetCurrentView(StoryViewType.ExplorerView);

            Ioc.Default.GetRequiredService<UnifiedVM>().UpdateRecents(Path.Combine(dialogVM.ProjectPath, dialogVM.ProjectName!)); //adds item to recent

            // Save the new project
            StoryModel.Changed = true;
            await SaveFile();
            if (Preferences.Model.BackupOnOpen)
            {
                await MakeBackup();
            }

            // Start the timed backup and auto save services
            if (Preferences.Model.TimedBackup)
            {
                Ioc.Default.GetRequiredService<BackupService>().StartTimedBackup();
            }

            if (Preferences.Model.AutoSave)
            {
                Ioc.Default.GetRequiredService<AutoSaveService>().StartAutoSave();
            }

            TreeViewNodeClicked(StoryModel.ExplorerView[0]);
            Window.UpdateWindowTitle();

            Messenger.Send(new StatusChangedMessage(new("New project command completed", LogLevel.Info, true)));
        }
        catch (Exception _ex)
        {
            Logger.LogException(LogLevel.Error, _ex, "Error creating new project");
            Messenger.Send(new StatusChangedMessage(new("File make failure.", LogLevel.Error)));
        }

        _canExecuteCommands = true;
    }

    /// <summary>
    /// This creates a new StoryModel based on a template
    /// </summary>
    /// <param name="ProjectName">The name of the project</param>
    /// <param name="SelectedTemplateIndex">The template to use (see NewProject.xaml)</param>
    public void CreateTemplate(string ProjectName, int SelectedTemplateIndex)
    {
        OverviewModel _overview = new(Path.GetFileNameWithoutExtension(ProjectName), StoryModel)
        { DateCreated = DateTime.Today.ToString("yyyy-MM-dd"), 
            Author = Preferences.Model.FirstName + " " + Preferences.Model.LastName };
            
        StoryNodeItem _overviewNode = new(_overview, null) { IsExpanded = true, IsRoot = true };
        StoryModel.ExplorerView.Add(_overviewNode);
        TrashCanModel _trash = new(StoryModel);
        StoryNodeItem _trashNode = new(_trash, null);
        StoryModel.ExplorerView.Add(_trashNode);     // The trashcan is the second root
        FolderModel _narrative = new("Narrative View", StoryModel, StoryItemType.Folder);
        StoryNodeItem _narrativeNode = new(_narrative, null) { IsRoot = true };
        StoryModel.NarratorView.Add(_narrativeNode);

        // Every new story gets a StoryProblem with a Protagonist and Antagonist
        // Except for Blank Project
        if (SelectedTemplateIndex != 0)
        {
            StoryElement _storyProblem = new ProblemModel("Story Problem", StoryModel);
            StoryNodeItem _storyProblemNode = new(_storyProblem, null);
            StoryElement _storyProtag = new CharacterModel("Protagonist", StoryModel);
            StoryNodeItem _storyProtagNode = new StoryNodeItem(_storyProtag, null);
            StoryElement _storyAntag = new CharacterModel("Antagonist", StoryModel);
            StoryNodeItem _storyAntagNode = new StoryNodeItem(_storyAntag, null);
            _overview.StoryProblem = _storyProblem.Uuid;
            var _problem = _storyProblem as ProblemModel;
            _problem.Protagonist = _storyProtag.Uuid;
            _problem.Antagonist = _storyAntag.Uuid;
            _problem.Premise =
                @"Your[protagonist] in a situation[genre, setting] wants something[goal], which brings him" +
                @"into [conflict] with a second character[antagonist]. After a series of conflicts[additional " +
                @"problems], the final battle[climax scene] erupts, and the[protagonist] finally resolves the " +
                @"conflict[outcome].";

            // Use the NewProjectDialog template to complete the model
            switch (SelectedTemplateIndex)
            {
                case 1:  // Story problem and characters
                    _overviewNode.Children.Add(_storyProblemNode);
                    _storyProblemNode.Children.Add(_storyProtagNode);
                    _storyProblemNode.Children.Add(_storyAntagNode);

                    //Correctly set parents
                    _storyProblemNode.Parent = _overviewNode;
                    _storyProtagNode.Parent = _storyProblemNode;
                    _storyAntagNode.Parent = _storyProblemNode;
                    _storyProblemNode.IsExpanded = true;
                    break;
                case 2:  // Folders for each type- story problem and characters belong in the corresponding folders
                    StoryElement _problems = new FolderModel("Problems", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _problemsNode = new(_problems, _overviewNode);
                    _storyProblemNode.Parent = _problemsNode;
                    StoryElement _characters = new FolderModel("Characters", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _charactersNode = new(_characters, _overviewNode);
                    _storyProtagNode.Parent = _charactersNode;
                    _storyAntagNode.Parent = _charactersNode;
                    StoryElement _settings = new FolderModel("Settings", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _settingsNode = new(_settings, _overviewNode);
                    StoryElement _scenes = new FolderModel("Scenes", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _scenesNode = new(_scenes, _overviewNode);
                    _overview.StoryProblem = _storyProblem.Uuid;
                    _problemsNode.Children.Add(_storyProblemNode);
                    _charactersNode.Children.Add(_storyProtagNode);
                    _charactersNode.Children.Add(_storyAntagNode);
                    _problemsNode.IsExpanded = true;
                    _charactersNode.IsExpanded = true;
                    break;
                case 3:
                    _storyProblemNode.Name = "External Problem";
                    _storyProblemNode.IsExpanded = true;
                    _storyProblemNode.Parent = _overviewNode;
                    _overviewNode.Children.Add(_storyProblemNode);
                    _storyProblemNode.Children.Add(_storyProtagNode);
                    _storyProblemNode.Children.Add(_storyAntagNode);
                    _problem = _storyProblem as ProblemModel;
                    _problem.Name = "External Problem";
                    _overview.StoryProblem = _problem.Uuid;
                    _problem.Protagonist = _storyProtag.Uuid;
                    _problem.Antagonist = _storyAntagNode.Uuid;
                    _storyProtagNode.Parent = _storyProblemNode;
                    _storyAntagNode.Parent = _storyProblemNode;
                    StoryElement _internalProblem = new ProblemModel("Internal Problem", StoryModel);
                    StoryNodeItem _internalProblemNode = new(_internalProblem, _overviewNode);
                    _problem = _internalProblem as ProblemModel;
                    _problem.Protagonist = _storyProtag.Uuid;
                    _problem.Antagonist = _storyProtag.Uuid;
                    _problem.ConflictType = "Person vs. Self";
                    _problem.Premise =
                        @"Your [protagonist] grapples with an [internal conflict] and is their own antagonist, marred by self-doubt and fears " +
                        @"or having a [goal] that masks this conflict rather than a real need. The [climax scene] is often a moment of introspection in which " +
                        @"he or she makes a decision or discovery that resolves the internal conflict [outcome]. Resolving this problem may enable your " +
                        @"[protagonist] to resolve another (external) problem.";
                    break;
                case 4:
                    _overviewNode.Children.Add(_storyProtagNode);
                    _overviewNode.Children.Add(_storyAntagNode);
                    _storyProtagNode.Children.Add(_storyProblemNode);
                    _storyProtagNode.IsExpanded = true;
                    _storyProblemNode.Parent = _storyProtagNode;
                    _storyProtagNode.Parent = _overviewNode;
                    _storyAntagNode.Parent = _overviewNode;
                    break;
                case 5:
                    StoryElement _problemsFolder = new FolderModel("Problems", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _problemsFolderNode = new(_problemsFolder, _overviewNode) { IsExpanded = true };
                    StoryElement _charactersFolder = new FolderModel("Characters", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _charactersFolderNode = new(_charactersFolder, _overviewNode) { IsExpanded = true };
                    StoryElement _settingsFolder = new FolderModel("Settings", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _settingsFolderNode = new(_settingsFolder, _overviewNode);
                    StoryElement _plotpointsFolder = new FolderModel("Scenes", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _plotpointsFolderNode = new(_plotpointsFolder, _overviewNode);
                    _storyProblemNode.Name = "External Problem";
                    _storyProblemNode.IsExpanded = true;
                    _problemsFolderNode.Children.Add(_storyProblemNode);
                    _storyProblemNode.Parent = _problemsFolderNode;
                    _problem = _storyProblem as ProblemModel;
                    _problem.Name = "External Problem";
                    _problem.Protagonist = _storyProtag.Uuid;
                    _problem.Antagonist = _storyAntagNode.Uuid;
                    _overview.StoryProblem = _problem.Uuid;
                    _internalProblem = new ProblemModel("Internal Problem", StoryModel);
                    _internalProblemNode = new(_internalProblem, _problemsFolderNode);
                    _problem = _internalProblem as ProblemModel;
                    _problem.Protagonist = _storyProtag.Uuid;
                    _problem.Antagonist = _storyProtag.Uuid;
                    _problem.ConflictType = "Person vs. Self";
                    _problem.Premise =
                        @"Your [protagonist] grapples with an [internal conflict] and is their own antagonist, marred by self-doubt and fears " +
                        @"or having a [goal] that masks this conflict rather than a real need. The [climax scene] is often a moment of introspection in which " +
                        @"he or she makes a decision or discovery that resolves the internal conflict [outcome]. Resolving this problem may enable your " +
                        @"[protagonist] to resolve another (external) problem.";
                    _charactersFolderNode.Children.Add(_storyProtagNode);
                    _charactersFolderNode.Children.Add(_storyAntagNode);
                    _storyProtagNode.Parent = _charactersFolderNode;
                    _storyAntagNode.Parent = _charactersFolderNode;
                    break;

            }
        }
    }

    public async Task MakeBackup()
    {
        await Ioc.Default.GetRequiredService<BackupService>().BackupProject();
    }

    public void TreeViewNodeClicked(object selectedItem, bool ClearHighlightCache = true)
    {
        if (selectedItem is null)
        {
            Logger.Log(LogLevel.Info, "TreeViewNodeClicked for null node, event ignored");
            return;
        }
        Logger.Log(LogLevel.Info, $"TreeViewNodeClicked for {selectedItem}");

        try
        {
            NavigationService _nav = Ioc.Default.GetRequiredService<NavigationService>();
            if (selectedItem is StoryNodeItem _node)
            {
                CurrentNode = _node;
                StoryElement _element = StoryModel.StoryElements.StoryElementGuids[_node.Uuid];
                switch (_element.Type)
                {
                    case StoryItemType.Character:
                        _nav.NavigateTo(SplitViewFrame, CharacterPage, _element);
                        break;
                    case StoryItemType.Scene:
                        _nav.NavigateTo(SplitViewFrame, ScenePage, _element);
                        break;
                    case StoryItemType.Problem:
                        _nav.NavigateTo(SplitViewFrame, ProblemPage, _element);
                        break;
                    case StoryItemType.Section:
                        _nav.NavigateTo(SplitViewFrame, FolderPage, _element);
                        break;
                    case StoryItemType.Folder:
                        _nav.NavigateTo(SplitViewFrame, FolderPage, _element);
                        break;
                    case StoryItemType.Setting:
                        _nav.NavigateTo(SplitViewFrame, SettingPage, _element);
                        break;
                    case StoryItemType.Web:
                        _nav.NavigateTo(SplitViewFrame, WebPage, _element);
                        break;
                    case StoryItemType.Notes:
                        _nav.NavigateTo(SplitViewFrame, FolderPage, _element);
                        break;
                    case StoryItemType.StoryOverview:
                        _nav.NavigateTo(SplitViewFrame, OverviewPage, _element);
                        break;
                    case StoryItemType.TrashCan:
                        _nav.NavigateTo(SplitViewFrame, TrashCanPage, _element);
                        break;
                }
                CurrentNode.IsExpanded = true;
            }

            //Clears background of new nodes on navigation as well as the last node.
            if (ClearHighlightCache)
            {
                foreach (var item in NewNodeHighlightCache) { item.Background = null; }
                if (LastClickedTreeviewItem != null) { LastClickedTreeviewItem.Background = null; }
            }
        }
        catch (Exception _e)
        {
            Logger.LogException(LogLevel.Error, _e, "Error navigating in ShellVM.TreeViewNodeClicked");
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

        NavigationService _nav = Ioc.Default.GetRequiredService<NavigationService>();
        _nav.NavigateTo(SplitViewFrame, HomePage);
    }
    private void TogglePane()
    {
        Logger.Log(LogLevel.Trace, $"TogglePane from {IsPaneOpen} to {!IsPaneOpen}");
        IsPaneOpen = !IsPaneOpen;
    }

    /// <summary>
    /// Save the currently active page's story element viewmodel's contents back to the StoryModel.
    /// 
    /// When an AppBar command button is pressed, the currently active StoryElement ViewModel
    /// displayed in SplitViewFrame's Content doesn't go through Deactivate() and hence doesn't
    /// call its WritePreferences() method. Hence this method, which determines which viewmodel's active 
    /// and calls its WritePreferences() method.
    /// </summary>
    public void SaveModel()
    {
        if (SplitViewFrame == null || SplitViewFrame.CurrentSourcePageType is null) { return; }

        Logger.Log(LogLevel.Trace, $"SaveModel Page type={SplitViewFrame.CurrentSourcePageType}");

        switch (SplitViewFrame.CurrentSourcePageType.ToString())
        {
            case "StoryCAD.Views.OverviewPage":
                OverviewViewModel _ovm = Ioc.Default.GetRequiredService<OverviewViewModel>();
                _ovm.SaveModel();
                break;
            case "StoryCAD.Views.ProblemPage":
                ProblemViewModel _pvm = Ioc.Default.GetRequiredService<ProblemViewModel>();
                _pvm.SaveModel();
                break;
            case "StoryCAD.Views.CharacterPage":
                CharacterViewModel _cvm = Ioc.Default.GetRequiredService<CharacterViewModel>();
                _cvm.SaveModel();
                break;
            case "StoryCAD.Views.ScenePage":
                SceneViewModel _scvm = Ioc.Default.GetRequiredService<SceneViewModel>();
                _scvm.SaveModel();
                break;
            case "StoryCAD.Views.FolderPage":
                FolderViewModel _folderVM = Ioc.Default.GetRequiredService<FolderViewModel>();
                _folderVM.SaveModel();
                break;
            case "StoryCAD.Views.SettingPage":
                SettingViewModel _settingVM = Ioc.Default.GetRequiredService<SettingViewModel>();
                _settingVM.SaveModel();
                break;
            case "StoryCAD.Views.WebPage":
                WebViewModel _webVM = Ioc.Default.GetRequiredService<WebViewModel>();
                _webVM.SaveModel();
                break;
        }
    }


    /// <summary>
    /// Opens a file picker to let the user chose a .stbx file and loads said file
    /// If fromPath is specified then the picker is skipped.
    /// </summary>
    /// <param name="fromPath">Path to open file from (Optional)</param>
    public async Task OpenFile(string fromPath = "")
    {
        // Check if current StoryModel has been changed, if so, save and write the model.
        if (StoryModel.Changed)
        {
            SaveModel();
            await WriteModel();
        }

        // Stop the auto save service if it was running
        if (Preferences.Model.AutoSave) { _autoSaveService.StopAutoSave(); }
        
        // Stop the timed backup service if it was running
        Ioc.Default.GetRequiredService<BackupService>().StopTimedBackup();

        _canExecuteCommands = false;
        Logger.Log(LogLevel.Info, "Executing OpenFile command");

        try
        {
            // Reset the model and show the home page
            ResetModel();
            ShowHomePage();

            // Open file picker if `fromPath` is not provided or file doesn't exist at the path.
            if (fromPath == "" || !File.Exists(fromPath))
            {
                Logger.Log(LogLevel.Info, "Opening file picker as story wasn't able to be found");

                StoryModel.ProjectFile = await Ioc.Default.GetService<Windowing>().ShowFilePicker("Open Project File",".stbx");
                if (StoryModel.ProjectFile == null) //Picker was canceled.
                {
                    Logger.Log(LogLevel.Info, "Open file picker cancelled.");
                    _canExecuteCommands = true;  // unblock other commands
                    return;
                }
            }
            else
            {
                //If `fromPath` is provided and file exists at the path, open that file.
                StoryModel.ProjectFile = await StorageFile.GetFileFromPathAsync(fromPath);
            }

            if (StoryModel.ProjectFile == null)
            {
                Logger.Log(LogLevel.Warn, "Open File command failed: StoryModel.ProjectFile is null.");
                Messenger.Send(new StatusChangedMessage(new("Open Story command cancelled", LogLevel.Info)));
                _canExecuteCommands = true;  // Unblock other commands
                return;
            }

            if (!File.Exists(StoryModel.ProjectFile.Path))
            {
                Messenger.Send(new StatusChangedMessage(new("Can't find file", LogLevel.Warn)));
                Logger.Log(LogLevel.Warn, $"File {StoryModel.ProjectFile.Path} does not exist.");
                _canExecuteCommands = true;
                return;
            }

            //Check file is available.
            StoryIO _rdr = Ioc.Default.GetRequiredService<StoryIO>();
            if (!await _rdr.CheckFileAvailability(StoryModel.ProjectFile.Path))
            {
                Messenger.Send(new StatusChangedMessage(new("File Unavailable.", LogLevel.Warn)));
                return;
            }

            // Set the ProjectFolder to the folder the project file is in.
            StoryModel.ProjectFolder = await StoryModel.ProjectFile.GetParentAsync();

            // Read the file into the StoryModel.
             StoryModel = await _rdr.ReadStory(StoryModel.ProjectFile);

            //Check the file we loaded actually has StoryCAD Data.
            if (StoryModel == null)
            {
	            Messenger.Send(new StatusChangedMessage(new("Unable to open file (No Story Elements found)", LogLevel.Warn, true)));
	            _canExecuteCommands = true;  // unblock other commands
	            return;
			}

            if (StoryModel.StoryElements.Count == 0)
            {
                Messenger.Send(new StatusChangedMessage(new("Unable to open file (No Story Elements found)", LogLevel.Warn, true)));
                _canExecuteCommands = true;  // unblock other commands
                return;

            }

            // Take a backup of the project if the user has the 'backup on open' preference set.
            if (Preferences.Model.BackupOnOpen)
            {
                await Ioc.Default.GetRequiredService<BackupService>().BackupProject();
            }

            // Set the current view to the ExplorerView 
            if (StoryModel.ExplorerView.Count > 0)
            {
                SetCurrentView(StoryViewType.ExplorerView);
                Messenger.Send(new StatusChangedMessage(new("Open Story completed", LogLevel.Info)));
            }

            Window.UpdateWindowTitle();
            new UnifiedVM().UpdateRecents(Path.Combine(StoryModel.ProjectFolder.Path, StoryModel.ProjectFile.Name));

            if (Preferences.Model.TimedBackup)
            {
                Ioc.Default.GetRequiredService<BackupService>().StartTimedBackup();
            }

            if (Preferences.Model.AutoSave)
            {
                _autoSaveService.StartAutoSave();
            }

            string _msg = $"Opened project {StoryModel.ProjectFilename}";
            Logger.Log(LogLevel.Info, _msg);
        }
        catch (Exception _ex)
        {
            // Report the error to the user
            Logger.LogException(LogLevel.Error, _ex, "Error in OpenFile command");
            Messenger.Send(new StatusChangedMessage(new("Open Story command failed", LogLevel.Error)));
        }

        Logger.Log(LogLevel.Info, "Open Story completed.");
        _canExecuteCommands = true;
    }

    /// <summary>
    /// Save the currently active page from 
    /// </summary>
    /// <param name="autoSave"></param>
    /// <returns></returns>
    public async Task SaveFile(bool autoSave = false)
    {
        _autoSaveService.StopAutoSave();
        bool _saveExecuteCommands = _canExecuteCommands;
        _canExecuteCommands = false;
        string msg = autoSave ? "AutoSave" : "SaveFile command";
        if (autoSave && !StoryModel.Changed)
        {
            Logger.Log(LogLevel.Info, $"{msg} skipped, no changes");
            _canExecuteCommands = true;
            return;
        }

        if (StoryModel.StoryElements.Count == 0)
        {
            Messenger.Send(new StatusChangedMessage(new("You need to open a story first!", LogLevel.Info)));
            Logger.Log(LogLevel.Info, $"{msg} cancelled (StoryModel.ProjectFile was null)");
            _canExecuteCommands = true;
            return;
        }

        try
        {
            Messenger.Send(new StatusChangedMessage(new($"{msg} executing", LogLevel.Info)));
            SaveModel();
            await WriteModel();
            Messenger.Send(new StatusChangedMessage(new($"{msg} completed", LogLevel.Info)));
            StoryModel.Changed = false;
            ChangeStatusColor = Colors.Green;
        }
        catch (Exception _ex)
        {
            Logger.LogException(LogLevel.Error, _ex, $"Exception in {msg}");
            Messenger.Send(new StatusChangedMessage(new($"{msg} failed", LogLevel.Error)));
        }
        _canExecuteCommands = _saveExecuteCommands;
        _autoSaveService.StartAutoSave();
    }

    /// <summary>
    /// Write the current StoryModel to the backing project file
    /// </summary>|
    public async Task WriteModel()
    {
        Logger.Log(LogLevel.Info, $"In WriteModel, file={StoryModel.ProjectFilename} path={StoryModel.ProjectPath}");
        try
        {
	        try //Updating the lost modified time
	        {
		        OverviewModel _overview =
			        StoryModel.StoryElements.StoryElementGuids[StoryModel.ExplorerView[0].Uuid] as OverviewModel;
		        _overview!.DateModified = DateTime.Today.ToString("yyyy-MM-dd");
	        }
	        catch
	        {
		        Logger.Log(LogLevel.Warn, "Failed to update last modified date/time");
	        }

	        await CreateProjectFile();
	        StorageFile _file = StoryModel.ProjectFile;
	        if (_file != null)
	        {
		        StoryIO _wtr = Ioc.Default.GetRequiredService<StoryIO>();
		        await _wtr.WriteStory(StoryModel.ProjectFile, StoryModel);
	        }
        }
        catch (UnauthorizedAccessException _ex)
        {
            Logger.LogException(LogLevel.Warn, _ex.InnerException, "User doesn't have perms to access this path.");
			//Catch write permission exceptions
			await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(new ContentDialog
			{
				Title = "Access error",
				Content = $"""
				           StoryCAD does not have permission to write to this location.
				           You outline will now be saved at: 
				           {Preferences.Model.ProjectDirectory}
				           """,
				PrimaryButtonText = "Okay"
			},true);

			//Reset to default location
			StoryModel.ProjectFolder = await StorageFolder.GetFolderFromPathAsync(Preferences.Model.ProjectDirectory);
			StoryModel.ProjectFile = await StoryModel.ProjectFolder.CreateFileAsync(StoryModel.ProjectFilename, 
				CreationCollisionOption.GenerateUniqueName);
			StoryModel.ProjectFilename = StoryModel.ProjectFile.Name;

			//Last opened file with reference to this version of the file so this doesn't happen again.
			Preferences.Model.LastFile1 = StoryModel.ProjectFile.Path;
		}
		catch (Exception _ex)
        {
            Logger.LogException(LogLevel.Error, _ex, 
	            $"Error writing file {_ex.Message} {_ex.Source}");
            Messenger.Send(new StatusChangedMessage(new("Error writing file - see log", LogLevel.Error)));
            return;
        }
        Logger.Log(LogLevel.Info, "WriteModel successful");
    }

    private async void SaveFileAs()
    {
        if (_canExecuteCommands)
        {
            _canExecuteCommands = false;
            Messenger.Send(new StatusChangedMessage(new("Save File As command executing", LogLevel.Info, true)));
            try
            {
                if (StoryModel.ProjectFile == null || StoryModel.ProjectPath == null)
                {
                    Messenger.Send(new StatusChangedMessage(new("You need to load a story first!", LogLevel.Info)));
                    Logger.Log(LogLevel.Warn, "User tried to use save as without a story loaded.");
                    _canExecuteCommands = true;
                    return;
                }

                //Creates the content dialog
                ContentDialog _saveAsDialog = new()
                {
                    Title = "Save as",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Cancel",
                    Content = new SaveAsDialog()
                };

                //Sets needed data in VM and then shows the dialog
                SaveAsViewModel _saveAsVM = Ioc.Default.GetRequiredService<SaveAsViewModel>();
                // The default project name and project folder path are from the active StoryModel
                _saveAsVM.ProjectName = StoryModel.ProjectFilename;
                _saveAsVM.ParentFolder = StoryModel.ProjectFolder;
                _saveAsVM.ProjectPathName = StoryModel.ProjectPath;

                ContentDialogResult _result = await Window.ShowContentDialog(_saveAsDialog);

                if (_result == ContentDialogResult.Primary) //If save is clicked
                {
                    if (await VerifyReplaceOrCreate())
                    {
                        //Saves model to disk
                        SaveModel();
                        await WriteModel();
                        if (Path.Combine(_saveAsVM.ProjectPathName, _saveAsVM.ProjectName) ==
                            Path.Combine(StoryModel.ProjectFolder.Path, StoryModel.ProjectFile.Name))
                        {
                            //Stop SaveAs from crashing if the user sets the path to a place where the story is already located.
                            Messenger.Send(new StatusChangedMessage(new("Save File As command completed", LogLevel.Info)));
                            Logger.Log(LogLevel.Info, "User tried to as file to same file as parent.");
                            _canExecuteCommands = true;
                            return;
                        }
                        //Saves the current project folders and files to disk
                        await StoryModel.ProjectFile.CopyAsync(_saveAsVM.ParentFolder, _saveAsVM.ProjectName, NameCollisionOption.ReplaceExisting);

                        //Update the StoryModel properties to use the newly saved copy
                        StoryModel.ProjectFilename = _saveAsVM.ProjectName;
                        StoryModel.ProjectFolder = _saveAsVM.ParentFolder;
                        StoryModel.ProjectPath = _saveAsVM.SaveAsProjectFolderPath;
                        // Add to the recent files stack
                        Window.UpdateWindowTitle();
                        new UnifiedVM().UpdateRecents(Path.Combine(StoryModel.ProjectFolder.Path, StoryModel.ProjectFile.Name));
                        // Indicate everything's done
                        Messenger.Send(new IsChangedMessage(true));
                        StoryModel.Changed = false;
                        ChangeStatusColor = Colors.Green;
                        Messenger.Send(new StatusChangedMessage(new("Save File As command completed", LogLevel.Info, true)));
                    }
                }
                else // if cancelled
                {
                    Messenger.Send(new StatusChangedMessage(new("SaveAs dialog cancelled", LogLevel.Info, true)));
                }
            }
            catch (Exception _ex) //If error occurs in file.
            {
                Logger.LogException(LogLevel.Error, _ex, "Exception in SaveFileAs");
                Messenger.Send(new StatusChangedMessage(new("Save File As failed", LogLevel.Info)));
            }
            _canExecuteCommands = true;
        }
    }

    private async Task<bool> VerifyReplaceOrCreate()
    {
        Logger.Log(LogLevel.Trace, "VerifyReplaceOrCreated");

        SaveAsViewModel _saveAsVM = Ioc.Default.GetRequiredService<SaveAsViewModel>();
        _saveAsVM.SaveAsProjectFolderPath = _saveAsVM.ParentFolder.Path;
        if (File.Exists(Path.Combine(_saveAsVM.ProjectPathName, _saveAsVM.ProjectName)))
        {
            ContentDialog _replaceDialog = new()
            {
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                Title = "Replace file?",
                Content = $"File {Path.Combine(_saveAsVM.ProjectPathName, _saveAsVM.ProjectName)} already exists. \n\nDo you want to replace it?",
            };
            return await Window.ShowContentDialog(_replaceDialog) == ContentDialogResult.Primary;
        }
        return true;
    }

    private async void CloseFile()
    {
        _canExecuteCommands = false;
        Messenger.Send(new StatusChangedMessage(new("Closing project", LogLevel.Info, true)));
        _autoSaveService.StopAutoSave();
        if (StoryModel.Changed)
        {
            ContentDialog _warning = new()
            {
                Title = "Save changes?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
            };
            if (await Window.ShowContentDialog(_warning) == ContentDialogResult.Primary)
            {
                SaveModel();
                await WriteModel();
            }
        }

        ResetModel();
        RightTappedNode = null; //Null right tapped node to prevent possible issues.
        SetCurrentView(StoryViewType.ExplorerView);
        Window.UpdateWindowTitle();
        Ioc.Default.GetRequiredService<BackupService>().StopTimedBackup();
        DataSource = StoryModel.ExplorerView;
        ShowHomePage();
        Messenger.Send(new StatusChangedMessage(new("Close story command completed", LogLevel.Info, true)));
        _canExecuteCommands = true;
    }

    private void ResetModel()
    {
        StoryModel = new();
        //TODO: Raise event for StoryModel change?
    }

    private async void ExitApp()
    {
        _canExecuteCommands = false;
        Messenger.Send(new StatusChangedMessage(new("Executing Exit project command", LogLevel.Info, true)));

        if (StoryModel.Changed)
        {
            ContentDialog _warning = new()
            {
                Title = "Save changes?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
            };
            if (await Window.ShowContentDialog(_warning) == ContentDialogResult.Primary)
            {
                SaveModel();
                await WriteModel();
            }
        }
        BackendService _backend = Ioc.Default.GetRequiredService<BackendService>();
        await _backend.DeleteWorkFile();
        Logger.Flush();
        Application.Current.Exit();  // Win32
    }

    private async Task CreateProjectFile()
    {
        StoryModel.ProjectFile = await StoryModel.ProjectFolder.CreateFileAsync(StoryModel.ProjectFilename, CreationCollisionOption.ReplaceExisting);
    }

    #endregion

    #region Tool and Report Commands

    private async void OpenPreferences()
    {
        Messenger.Send(new StatusChangedMessage(new("Updating Preferences", LogLevel.Info, true)));


        //Creates and shows dialog
        Ioc.Default.GetRequiredService<PreferencesViewModel>().LoadModel();
        ContentDialog _preferencesDialog = new()
        {
            Content = new PreferencesDialog(),
            Title = "Preferences",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel"
        };

        ContentDialogResult _result = await Window.ShowContentDialog(_preferencesDialog);
        switch (_result)
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
        if (_canExecuteCommands)
        {
            if (CurrentNode == null)
            {
                Messenger.Send(new StatusChangedMessage(new("Select a node to collaborate on", LogLevel.Warn, true)));
                return;
            }

            //TODO: Logging???
            
            var id = CurrentNode.Uuid; // get the story element;
            CollabArgs.SelectedElement = StoryModel.StoryElements.StoryElementGuids[id];
            CollabArgs.StoryModel = StoryModel;
            Ioc.Default.GetService<CollaboratorService>()!.LoadWorkflows(CollabArgs);
            Ioc.Default.GetService<CollaboratorService>()!.CollaboratorWindow.Show();
            Ioc.Default.GetService<WorkflowViewModel>()!.EnableNavigation();
        }
    }

    private async void KeyQuestionsTool()
    {
        Logger.Log(LogLevel.Info, "Displaying KeyQuestions tool dialog");
        if (_canExecuteCommands)
        {
            _canExecuteCommands = false;
            if (RightTappedNode == null) { RightTappedNode = CurrentNode; }

            //Creates and shows dialog
            ContentDialog _keyQuestionsDialog = new()
            {
                Title = "Key questions",
                CloseButtonText = "Close",
                Content = new KeyQuestionsDialog()
            };
            await Ioc.Default.GetService<Windowing>().ShowContentDialog(_keyQuestionsDialog);

            Ioc.Default.GetRequiredService<KeyQuestionsViewModel>().NextQuestion();
            Logger.Log(LogLevel.Info, "KeyQuestions finished");
            _canExecuteCommands = true;
        }

    }

    private async void TopicsTool()
    {
        Logger.Log(LogLevel.Info, "Displaying Topics tool dialog");
        if (_canExecuteCommands)
        {
            _canExecuteCommands = false;
            if (RightTappedNode == null) { RightTappedNode = CurrentNode; }

            ContentDialog _dialog = new()
            {
                Title = "Topic Information",
                CloseButtonText = "Done",
                Content = new TopicsDialog()
            };
            await Window.ShowContentDialog(_dialog);

            _canExecuteCommands = true;
        }

        Logger.Log(LogLevel.Info, "Topics finished");
    }

    /// <summary>
    /// This shows the master plot dialog
    /// </summary>
    private async void MasterPlotTool()
    {
        if (_canExecuteCommands)
        {
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Displaying MasterPlot tool dialog");
            if (VerifyToolUse(true, true))
            {
                //Creates and shows content dialog
                ContentDialog _dialog = new()
                {
                    Title = "Master plots",
                    PrimaryButtonText = "Copy",
                    SecondaryButtonText = "Cancel",
                    Content = new MasterPlotsDialog()
                };
                ContentDialogResult _result = await Window.ShowContentDialog(_dialog);

                if (_result == ContentDialogResult.Primary) // Copy command
                {
                    MasterPlotsViewModel _masterPlotsVM = Ioc.Default.GetRequiredService<MasterPlotsViewModel>();
                    string _masterPlotName = _masterPlotsVM.PlotPatternName;
                    PlotPatternModel _model = _masterPlotsVM.MasterPlots[_masterPlotName];
                    IList<PlotPatternScene> _scenes = _model.PlotPatternScenes;
                    ProblemModel _problem = new ProblemModel(_masterPlotName, StoryModel);
                    // add the new ProblemModel & node to the end of the target (RightTappedNode) children 
                    StoryNodeItem _problemNode = new(_problem, RightTappedNode);
                    RightTappedNode.IsExpanded = true;
                    _problemNode.IsSelected = true;
                    _problemNode.IsExpanded = true;
                    if (_scenes.Count == 1)
                    {
                        _problem.StoryQuestion = "See Notes.";
                        _problem.Notes = _scenes[0].Notes;
                    }
                    else foreach (PlotPatternScene _scene in _scenes)
                        {
                            SceneModel _child = new(StoryModel) { Name = _scene.SceneTitle, Remarks = "See Notes.", Notes = _scene.Notes };
                            // add the new SceneModel & node to the end of the problem's children 
                            StoryNodeItem _newNode = new(_child, _problemNode);
                            _newNode.IsSelected = true;
                        }

                    Messenger.Send(new StatusChangedMessage(new($"MasterPlot {_masterPlotName} inserted", LogLevel.Info, true)));
                    ShowChange();
                    Logger.Log(LogLevel.Info, "MasterPlot complete");
                }
            }
        }
        _canExecuteCommands = true;
    }

    /// <summary>
    /// This function just calls print reports dialog.
    /// </summary>
    private async void OpenPrintMenu() 
    {
        await Ioc.Default.GetRequiredService<PrintReportDialogVM>().OpenPrintReportDialog();
    }

    private async void DramaticSituationsTool()
    {
        Logger.Log(LogLevel.Info, "Displaying Dramatic Situations tool dialog");
        if (_canExecuteCommands)
        {
            _canExecuteCommands = false;

            if (VerifyToolUse(true, true))
            {
                //Creates and shows dialog
                ContentDialog _dialog = new()
                {
                    Title = "Dramatic situations",
                    PrimaryButtonText = "Copy as problem",
                    SecondaryButtonText = "Copy as scene",
                    CloseButtonText = "Cancel",
                    Content = new DramaticSituationsDialog()
                };
                ContentDialogResult _result = await Window.ShowContentDialog(_dialog);

                DramaticSituationModel _situationModel = Ioc.Default.GetRequiredService<DramaticSituationsViewModel>().Situation;
                string _msg;

                if (_result == ContentDialogResult.Primary)
                {
                    ProblemModel _problem = new(StoryModel) { Name = _situationModel.SituationName, StoryQuestion = "See Notes.", Notes = _situationModel.Notes };

                    // Insert the new Problem as the target's child
                    _ = new StoryNodeItem(_problem, RightTappedNode);
                    _msg = $"Problem {_situationModel.SituationName} inserted";
                    ShowChange();
                }
                else if (_result == ContentDialogResult.Secondary)
                {
                    SceneModel _sceneVar = new(StoryModel) { Name = _situationModel.SituationName, Remarks = "See Notes.", Notes = _situationModel.Notes };
                    // Insert the new Scene as the target's child
                    _ = new StoryNodeItem(_sceneVar, RightTappedNode);
                    _msg = $"Scene {_situationModel.SituationName} inserted";
                    ShowChange();
                }
                else { _msg = "Dramatic Situation tool cancelled"; }

                Logger.Log(LogLevel.Info, _msg);
                Messenger.Send(new StatusChangedMessage(new(_msg, LogLevel.Info, true)));
            }

            _canExecuteCommands = true;
        }
        Logger.Log(LogLevel.Info, "Dramatic Situations finished");
    }

    /// <summary>
    /// This loads the stock scenes dialog in the Plotting Aids submenu
    /// </summary>
    private async void StockScenesTool()
    {
        Logger.Log(LogLevel.Info, "Displaying Stock Scenes tool dialog");
        if (VerifyToolUse(true, true) && _canExecuteCommands)
        {
            _canExecuteCommands = false;
            try
            {
                //Creates and shows dialog
                ContentDialog _dialog = new()
                {
                    Title = "Stock scenes",
                    Content = new StockScenesDialog(),
                    PrimaryButtonText = "Add Scene",
                    CloseButtonText = "Cancel",
                };
                ContentDialogResult _result = await Window.ShowContentDialog(_dialog);

                if (_result == ContentDialogResult.Primary) // Copy command
                {
                    if (string.IsNullOrWhiteSpace(Ioc.Default.GetRequiredService<StockScenesViewModel>().SceneName))
                    {
                        Messenger.Send(new StatusChangedMessage(new("You need to select a stock scene",
                            LogLevel.Warn)));
                        return;
                    }

                    SceneModel _sceneVar = new(StoryModel)
                    { Name = Ioc.Default.GetRequiredService<StockScenesViewModel>().SceneName };
                    StoryNodeItem _newNode = new(_sceneVar, RightTappedNode);
                    _sourceChildren = RightTappedNode.Children;
                    TreeViewNodeClicked(_newNode);
                    RightTappedNode.IsExpanded = true;
                    _newNode.IsSelected = true;
                    Messenger.Send(new StatusChangedMessage(new("Stock Scenes inserted", LogLevel.Info)));
                }
                else
                {
                    Messenger.Send(new StatusChangedMessage(new("Stock Scenes canceled", LogLevel.Warn)));
                }
            }
            catch (Exception _e)
            {
                Logger.LogException(LogLevel.Error, _e, _e.Message);
            }
            _canExecuteCommands = true;
        }
    }


    private async void GenerateScrivenerReports()
    {
        if (DataSource == null || DataSource.Count == 0)
        {
            Messenger.Send(new StatusChangedMessage(new("You need to open a story first!", LogLevel.Info)));
            Logger.Log(LogLevel.Info, $"Scrivener Report cancelled (DataSource was null or empty)");
            return;
        }

        //TODO: revamp this to be more user friendly.
        _canExecuteCommands = false;
        Messenger.Send(new StatusChangedMessage(new("Generate Scrivener Reports executing", LogLevel.Info, true)));
        SaveModel();

        // Select the Scrivener .scrivx file to add the report to
        StorageFile _file = await Ioc.Default.GetService<Windowing>().ShowFilePicker("Open file", ".scrivx");
        if (_file != null)
        {
            Scrivener.ScrivenerFile = _file;
            Scrivener.ProjectPath = Path.GetDirectoryName(_file.Path);
            if (!await Scrivener.IsScrivenerRelease3())
                throw new ApplicationException("Project is not Scrivener Release 3");
            // Load the Scrivener project file's model
            ScrivenerReports _rpt = new(_file, StoryModel);
            await _rpt.GenerateReports();
        }

        Messenger.Send(new StatusChangedMessage(new("Generate Scrivener reports completed", LogLevel.Info, true)));
        _canExecuteCommands = true;
    }

    /// <summary>
    /// Opens help menu
    /// </summary>
    private void LaunchGitHubPages()
    {
        _canExecuteCommands = false;
        Messenger.Send(new StatusChangedMessage(new("Launching GitHub Pages User Manual", LogLevel.Info, true)));

        Process.Start(new ProcessStartInfo()
        {
            FileName = @"https://Storybuilder-org.github.io/StoryCAD/",
            UseShellExecute = true
        });

        Messenger.Send(new StatusChangedMessage(new("Launch default browser completed", LogLevel.Info, true)));

        _canExecuteCommands = true;
    }

    /// <summary>
    /// Verify that the tool being called has its prerequisites met.
    /// </summary>
    /// <param name="explorerViewOnly">This tool can only run in StoryExplorer view</param>
    /// <param name="nodeRequired">A node (right-clicked or clicked) must be present</param>
    /// <param name="checkOutlineIsOpen">A checks an outline is open (defaults to true)</param>
    /// <returns>true if prerequisites are met</returns>
    public bool VerifyToolUse(bool explorerViewOnly, bool nodeRequired, bool checkOutlineIsOpen = true)
    {
        try
        {
            if (explorerViewOnly && CurrentViewType != StoryViewType.ExplorerView)
            {
                Messenger.Send(new StatusChangedMessage(new("This tool can only be run in Story Explorer view", LogLevel.Warn)));
                return false;
            }

            if (checkOutlineIsOpen)
            {
                if (StoryModel == null)
                {
                    Messenger.Send(new StatusChangedMessage(new("Open or create an outline first", LogLevel.Warn)));
                    return false;
                }
                if (CurrentViewType == StoryViewType.ExplorerView && StoryModel.ExplorerView.Count == 0)
                {
                    Messenger.Send(new StatusChangedMessage(new("Open or create an outline first", LogLevel.Warn)));
                    return false;
                }
                if (CurrentViewType == StoryViewType.NarratorView && StoryModel.NarratorView.Count == 0)
                {
                    Messenger.Send(new StatusChangedMessage(new("Open or create an outline first", LogLevel.Warn)));
                    return false;
                }
            }
            if (nodeRequired)
            {
                if (RightTappedNode == null) { RightTappedNode = CurrentNode; }
                if (RightTappedNode == null)
                {
                    Messenger.Send(new StatusChangedMessage(new("You need to select a node first", LogLevel.Warn)));
                    return false;
                }
            }
            return true;
        }
        catch (Exception _ex)
        {
            Logger.LogException(LogLevel.Error, _ex, "Error in ShellVM.VerifyToolUse()");
            return false; // Return false to prevent any issues.
        }
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
        StoryNodeItem _targetParent = CurrentNode.Parent.Parent;
        // The source must become the parent's successor
        _targetCollection = CurrentNode.Parent.Parent.Children;
        _targetIndex = _targetCollection.IndexOf(CurrentNode.Parent) + 1;

        _sourceChildren.RemoveAt(_sourceIndex);
        if (_targetIndex == -1) { _targetCollection.Add(CurrentNode); }
        else { _targetCollection.Insert(_targetIndex, CurrentNode); }
        CurrentNode.Parent = _targetParent;
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
        StoryNodeItem _targetParent = CurrentNode.Parent;

        if (_sourceIndex == 0)
        {
            if (CurrentNode.Parent.Parent == null)
            {
                ShowStatusMessage("Cannot move up further", LogLevel.Warn);
                return;
            }

            ObservableCollection<StoryNodeItem> _grandparentCollection = CurrentNode.Parent.Parent.Children;
            int _siblingIndex = _grandparentCollection.IndexOf(CurrentNode.Parent) - 1;

            if (_siblingIndex >= 0)
            {
                _targetCollection = _grandparentCollection[_siblingIndex].Children;
                _targetParent = _grandparentCollection[_siblingIndex];
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

        CurrentNode.Parent = _targetParent;
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
        StoryNodeItem _targetParent = CurrentNode.Parent;

        // If last child, must move to end parent's successor (sibling node).
        // If there are no siblings, we're at the bottom of the tree?
        if (_sourceIndex == _sourceChildren.Count - 1)
        {
            // Find the next sibling of the parent.
            StoryNodeItem _nextParentSibling = GetNextSibling(CurrentNode.Parent);

            // If there's no next sibling, then we're at the bottom of the first root's children.
            if (_nextParentSibling == null)
            {
                ShowStatusMessage("Cannot move down further", LogLevel.Warn);
                return;
            }

            // If the next sibling is the TrashCan, disallow moving the node to the TrashCan.
            if (_nextParentSibling.Type == StoryItemType.TrashCan)
            {
                ShowStatusMessage("Cannot move to trash", LogLevel.Warn);
                return;
            }

            // If the next sibling is not the TrashCan, move the node to the beginning of its children.
            _targetCollection = _nextParentSibling.Children;
            _targetParent = _nextParentSibling;
        }
        // Otherwise, move down a notch
        else
        {
            _targetCollection = _sourceChildren;
            _targetIndex = _sourceIndex + 1;
        }
        _sourceChildren.RemoveAt(_sourceIndex);
        _targetCollection.Insert(_targetIndex, CurrentNode);
        CurrentNode.Parent = _targetParent;

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

    #region Add and Remove Story Element Commands

    private void AddFolder()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Folder), false);
    }

    private void AddSection()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Section), false);
    }

    private void AddProblem()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Problem), false);
    }

    private void AddCharacter()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Character), false);
    }
    private void AddWeb()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Web), false);
    }
    private void AddNotes()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Notes), false);
    }

    private void AddSetting()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Setting), false);
    }

    private void AddScene()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Scene), false);
    }

    private StoryNodeItem AddStoryElement(StoryItemType typeToAdd)
    {
        Logger.Log(LogLevel.Trace, "AddStoryElement");
        _canExecuteCommands = false;
        string _msg = $"Adding StoryElement {typeToAdd}";
        Logger.Log(LogLevel.Info, _msg);
        if (RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Right tap a node to add to", LogLevel.Warn)));
            Logger.Log(LogLevel.Info, "Add StoryElement failed- node not selected");
            _canExecuteCommands = true;
            return null;
        }

        if (RootNodeType(RightTappedNode) == StoryItemType.TrashCan)
        {
            Messenger.Send(new StatusChangedMessage(new("You can't add to Deleted Items", LogLevel.Warn)));
            Logger.Log(LogLevel.Info, "Add StoryElement failed- can't add to TrashCan");
            _canExecuteCommands = true;
            return null;
        }

        StoryNodeItem _newNode = null;
        switch (typeToAdd)
        {
            case StoryItemType.Folder:
                _newNode = new StoryNodeItem(new FolderModel(StoryModel), RightTappedNode);
                break;
            case StoryItemType.Section:
                _newNode = new StoryNodeItem(new FolderModel("New Section", StoryModel, StoryItemType.Folder), RightTappedNode, StoryItemType.Folder);
                break;
            case StoryItemType.Problem:
                _newNode = new StoryNodeItem(new ProblemModel(StoryModel), RightTappedNode);
                break;
            case StoryItemType.Character:
                _newNode = new StoryNodeItem(new CharacterModel(StoryModel), RightTappedNode);
                break;
            case StoryItemType.Setting:
                _newNode = new StoryNodeItem(new SettingModel(StoryModel), RightTappedNode);
                break;
            case StoryItemType.Scene:
                _newNode = new StoryNodeItem(new SceneModel(StoryModel), RightTappedNode);
                break;
            case StoryItemType.Web:
                _newNode = new StoryNodeItem(new WebModel(StoryModel), RightTappedNode);
                break;
            case StoryItemType.Notes:
                _newNode = new StoryNodeItem(new FolderModel("New Note", StoryModel, StoryItemType.Notes), RightTappedNode, StoryItemType.Notes);
                break;
        }

        if (_newNode != null)
        {
            _newNode.Parent.IsExpanded = true;
            _newNode.IsRoot = false; //Only an overview node can be a root, which cant be created normally
            _newNode.IsSelected = false;
            _newNode.Background = Window.ContrastColor;
            NewNodeHighlightCache.Add(_newNode);
        }
        else { return null; }

        Messenger.Send(new IsChangedMessage(true));
        Messenger.Send(new StatusChangedMessage(new($"Added new {typeToAdd}", LogLevel.Info, true)));
        _canExecuteCommands = true;

        return _newNode;
    }

    private async void RemoveStoryElement()
    {
        Logger.Log(LogLevel.Trace, "RemoveStoryElement");
        if (RightTappedNode == null)
        {
            StatusMessage = "Right tap a node to delete";
            return;
        }
        if (RootNodeType(RightTappedNode) == StoryItemType.TrashCan)
        {
            StatusMessage = "You can't delete from the trash!";
            return;
        }
        if (RightTappedNode.IsRoot)
        {
            StatusMessage = "You can't delete a root node!";
            return;
        }

        List<StoryNodeItem> _foundNodes = new();
        foreach (StoryNodeItem _node in DataSource[0]) //Gets all nodes in the tree #TODO: MAKE RECURSIVE
        {
            if (Ioc.Default.GetRequiredService<DeletionService>().SearchStoryElement(_node, RightTappedNode.Uuid, StoryModel))
            {
                _foundNodes.Add(_node);
            }
        }

        bool _delete = true;
        //Only warns if it finds a node its referenced in
        if (_foundNodes.Count >= 1)
        {
            //Creates UI
            StackPanel _content = new();
            _content.Children.Add(new TextBlock { Text = "The following nodes will be updated to remove references to this node:" });
            _content.Children.Add(new ListView { ItemsSource = _foundNodes, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Height = 300, Width = 480 });

            //Creates dialog and then shows it
            ContentDialog _Dialog = new()
            {
                Content = _content,
                Title = "Are you sure you want to delete this node?",
                Width = 500,
                PrimaryButtonText = "Confirm",
                SecondaryButtonText = "Cancel"
            };
            if (await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(_Dialog) == ContentDialogResult.Secondary) { _delete = false; }
        }


        if (_delete)
        {
            foreach (StoryNodeItem _node in _foundNodes)
            {
                Ioc.Default.GetRequiredService<DeletionService>().SearchStoryElement(_node, RightTappedNode.Uuid, StoryModel, true);
            }

            if (CurrentView.Equals("Story Explorer View")) { RightTappedNode.Delete(StoryViewType.ExplorerView); }
            else { RightTappedNode.Delete(StoryViewType.NarratorView); }
        }
    }

    private void RestoreStoryElement()
    {
        Logger.Log(LogLevel.Trace, "RestoreStoryElement");
        if (RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Right tap a node to restore", LogLevel.Warn)));
            return;
        }
        if (RootNodeType(RightTappedNode) != StoryItemType.TrashCan)
        {
            Messenger.Send(new StatusChangedMessage(new("You can only restore from Deleted StoryElements", LogLevel.Warn)));
            return;
        }

        if (RightTappedNode.IsRoot)
        {
            Messenger.Send(new StatusChangedMessage(new("You can't restore a root node!", LogLevel.Warn)));
            return;
        }

        //TODO: Add dialog to confirm restore
        ObservableCollection<StoryNodeItem> _target = DataSource[0].Children;
        DataSource[1].Children.Remove(RightTappedNode);
        _target.Add(RightTappedNode);
        RightTappedNode.Parent = DataSource[0];
        Messenger.Send(new StatusChangedMessage(new($"Restored node {RightTappedNode.Name}", LogLevel.Info, true)));
    }

    /// <summary>
    /// Add a Scene StoryNodeItem to the end of the Narrative view
    /// by copying from the Scene's StoryNodeItem in the ExplorerView
    /// view.
    /// </summary>
    private void CopyToNarrative()
    {
        Logger.Log(LogLevel.Trace, "CopyToNarrative");
        if (RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Select a node to copy", LogLevel.Info)));
            return;
        }
        if (RightTappedNode.Type != StoryItemType.Scene)
        {
            Messenger.Send(new StatusChangedMessage(new("You can only copy a scene", LogLevel.Warn)));
            return;
        }

        SceneModel _sceneVar = (SceneModel)StoryModel.StoryElements.StoryElementGuids[RightTappedNode.Uuid];
        _ = new StoryNodeItem(_sceneVar, StoryModel.NarratorView[0]);
        Messenger.Send(new StatusChangedMessage(new($"Copied node {RightTappedNode.Name} to Narrative View", LogLevel.Info, true)));
    }

    /// <summary>
    /// Clears trash
    /// </summary>
    private void EmptyTrash()
    {
        if (DataSource == null)
        {
            Messenger.Send(new StatusChangedMessage(new("You need to load a story first!", LogLevel.Warn)));
            Logger.Log(LogLevel.Info, "Failed to empty trash as DataSource is null. (Is a story loaded?)");
            return;
        }

        StatusMessage = "Trash Emptied.";
        Logger.Log(LogLevel.Info, "Emptied Trash.");
        DataSource[1].Children.Clear();
    }

    /// <summary>
    /// Remove a TreeViewItem from the Narrative view for a copied Scene.
    /// </summary>
    private void RemoveFromNarrative()
    {
        Logger.Log(LogLevel.Trace, "RemoveFromNarrative");

        if (RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Select a node to remove", LogLevel.Info)));
            return;
        }
        if (RightTappedNode.Type != StoryItemType.Scene)
        {
            Messenger.Send(new StatusChangedMessage(new("You can only remove a Scene copy", LogLevel.Info)));
            return;
        }

        foreach (StoryNodeItem _item in StoryModel.NarratorView[0].Children.ToList())
        {
            if (_item.Uuid == RightTappedNode.Uuid)
            {
                StoryModel.NarratorView[0].Children.Remove(_item);
                Messenger.Send(new StatusChangedMessage(new($"Removed node {RightTappedNode.Name} from Narrative View", LogLevel.Info, true)));
                return;
            }
        }

        Messenger.Send(new StatusChangedMessage(new($"Node {RightTappedNode.Name} not in Narrative View", LogLevel.Info, true)));

    }

    /// <summary>
    /// Search up the StoryNodeItem tree to its
    /// root from a specified node and return its StoryItemType. 
    /// 
    /// This allows code to determine which TreeView it's in.
    /// </summary>
    /// <param name="startNode">The node to begin searching from</param>
    /// <returns>The StoryItemType of the root node</returns>
    public static StoryItemType RootNodeType(StoryNodeItem startNode)
    {
        try
        {
            StoryNodeItem node = startNode;
            while (!node.IsRoot)
                node = node.Parent;
            return node.Type;
        }
        catch (Exception ex)
        {
            Ioc.Default.GetService<LogService>().LogException(
                LogLevel.Error, ex, $"Root node type exception, this shouldn't happen {ex.Message} {ex.Message}");
            return StoryItemType.Unknown;
        }
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
            if (RootNodeType(RightTappedNode) == StoryItemType.TrashCan)
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

    public void ShowConnectionStatus()
    {
        StatusMessage _msg;
        if (!Doppler.DopplerConnection | !Logger.ElmahLogging)
            _msg = new StatusMessage("Connection not established", LogLevel.Warn, true);
        else
            _msg = new StatusMessage("Connection established", LogLevel.Info, true);
        Messenger.Send(new StatusChangedMessage(_msg));
    }

    private void SetCurrentView(StoryViewType view)
    {
        if (view == StoryViewType.ExplorerView)
        {
            DataSource = StoryModel.ExplorerView;
            SelectedView = ViewList[0];
            CurrentViewType = StoryViewType.ExplorerView;
        }
        else if (view == StoryViewType.NarratorView)
        {
            DataSource = StoryModel.NarratorView;
            SelectedView = ViewList[1];
            CurrentViewType = StoryViewType.NarratorView;
        }
    }

    #region MVVM  processing
    private void IsChangedMessageReceived(IsChangedMessage isDirty)
    {
        StoryModel.Changed = StoryModel.Changed || isDirty.Value;
        if (StoryModel.Changed)
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
        if (Assembly.GetEntryAssembly().Location.Contains("StoryCADTests.dll")
            || Assembly.GetEntryAssembly().Location.Contains("CollaboratorTests.dll")
            || Assembly.GetEntryAssembly().Location.Contains("testhost.dll"))
        {
            return;
        }

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

        if (IsDescendant(StoryModel.ExplorerView[1], target) || IsDescendant(StoryModel.ExplorerView[1], source))
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

    #endregion

    #region Constructor(s)

    public ShellViewModel()
    {
        Messenger.Register<IsChangedRequestMessage>(this, (_, m) => { m.Reply(StoryModel!.Changed); });
        Messenger.Register<ShellViewModel, IsChangedMessage>(this, static (r, m) => r.IsChangedMessageReceived(m));
        Messenger.Register<ShellViewModel, StatusChangedMessage>(this, static (r, m) => r.StatusMessageReceived(m));
        Messenger.Register<ShellViewModel, NameChangedMessage>(this, static (r, m) => r.NameMessageReceived(m));

        Scrivener = Ioc.Default.GetRequiredService<ScrivenerIo>();
        Logger = Ioc.Default.GetRequiredService<LogService>();
        Search = Ioc.Default.GetRequiredService<SearchService>();

        StoryModel = new StoryModel();

        //Skip status timer initialization in Tests.
        if (!State.StoryCADTestsMode)
        {
            _statusTimer = new DispatcherTimer();
            _statusTimer.Tick += statusTimer_Tick;
            ChangeStatusColor = Colors.Green;
        }

        Messenger.Send(new StatusChangedMessage(new("Ready", LogLevel.Info)));

        _canExecuteCommands = true;
        TogglePaneCommand = new RelayCommand(TogglePane, () => _canExecuteCommands);
        OpenUnifiedCommand = new RelayCommand(async () => await OpenUnifiedMenu(), () => _canExecuteCommands);
        CloseUnifiedCommand = new RelayCommand(CloseUnifiedMenu, () => _canExecuteCommands);
        NarrativeToolCommand = new RelayCommand(async () => await Ioc.Default.GetRequiredService<NarrativeToolVM>().OpenNarrativeTool(), () => _canExecuteCommands);
        PrintNodeCommand = new RelayCommand(async () => await PrintCurrentNodeAsync(), () => _canExecuteCommands);
        OpenFileCommand = new RelayCommand(async () => await OpenFile(), () => _canExecuteCommands);
        SaveFileCommand = new RelayCommand(async () => await SaveFile(), () => _canExecuteCommands);
        SaveAsCommand = new RelayCommand(SaveFileAs, () => _canExecuteCommands);
        CreateBackupCommand = new RelayCommand(async () => await CreateBackupNow(), () => _canExecuteCommands);
        CloseCommand = new RelayCommand(CloseFile, () => _canExecuteCommands);
        ExitCommand = new RelayCommand(ExitApp, () => _canExecuteCommands);

        // StoryCAD Collaborator
        CollaboratorCommand = new RelayCommand(LaunchCollaborator, () => _canExecuteCommands);

        // Tools commands
        KeyQuestionsCommand = new RelayCommand(KeyQuestionsTool, () => _canExecuteCommands);
        TopicsCommand = new RelayCommand(TopicsTool, () => _canExecuteCommands);
        MasterPlotsCommand = new RelayCommand(MasterPlotTool, () => _canExecuteCommands);
        DramaticSituationsCommand = new RelayCommand(DramaticSituationsTool, () => _canExecuteCommands);
        StockScenesCommand = new RelayCommand(StockScenesTool, () => _canExecuteCommands);

        PreferencesCommand = new RelayCommand(OpenPreferences, () => _canExecuteCommands);

        PrintReportsCommand = new RelayCommand(OpenPrintMenu, () => _canExecuteCommands);
        ScrivenerReportsCommand = new RelayCommand(GenerateScrivenerReports, () => _canExecuteCommands);

        HelpCommand = new RelayCommand(LaunchGitHubPages, () => _canExecuteCommands);

        // Move StoryElement commands
        MoveLeftCommand = new RelayCommand(MoveTreeViewItemLeft, () => _canExecuteCommands);
        MoveRightCommand = new RelayCommand(MoveTreeViewItemRight, () => _canExecuteCommands);
        MoveUpCommand = new RelayCommand(MoveTreeViewItemUp, () => _canExecuteCommands);
        MoveDownCommand = new RelayCommand(MoveTreeViewItemDown, () => _canExecuteCommands);
        // Add StoryElement commands
        AddFolderCommand = new RelayCommand(AddFolder, () => _canExecuteCommands);
        AddSectionCommand = new RelayCommand(AddSection, () => _canExecuteCommands);
        AddProblemCommand = new RelayCommand(AddProblem, () => _canExecuteCommands);
        AddCharacterCommand = new RelayCommand(AddCharacter, () => _canExecuteCommands);
        AddWebCommand = new RelayCommand(AddWeb, () => _canExecuteCommands);
        AddNotesCommand = new RelayCommand(AddNotes, () => _canExecuteCommands);
        AddSettingCommand = new RelayCommand(AddSetting, () => _canExecuteCommands);
        AddSceneCommand = new RelayCommand(AddScene, () => _canExecuteCommands);
        // Remove Story Element command (move to trash)
        RemoveStoryElementCommand = new RelayCommand(RemoveStoryElement, () => _canExecuteCommands);
        RestoreStoryElementCommand = new RelayCommand(RestoreStoryElement, () => _canExecuteCommands);
        EmptyTrashCommand = new RelayCommand(EmptyTrash, () => _canExecuteCommands);
        // Copy to Narrative command
        AddToNarrativeCommand = new RelayCommand(CopyToNarrative, () => _canExecuteCommands);
        RemoveFromNarrativeCommand = new RelayCommand(RemoveFromNarrative, () => _canExecuteCommands);

        ViewList.Add("Story Explorer View");
        ViewList.Add("Story Narrator View");

        CurrentView = "Story Explorer View";
        SelectedView = "Story Explorer View";

        ShellInstance = this;
    }

    public void SearchNodes()
    {
        _canExecuteCommands = false;    //This prevents other commands from being used till this one is complete.
        Logger.Log(LogLevel.Info, $"Search started, Searching for {FilterText}");
        SaveModel();
        if (DataSource == null || DataSource.Count == 0)
        {
            Logger.Log(LogLevel.Info, "Data source is null or Empty.");
            Messenger.Send(new StatusChangedMessage(new("You need to load a story first!", LogLevel.Warn)));

            _canExecuteCommands = true;
            return;
        }

        int _searchTotal = 0;

        foreach (StoryNodeItem _node in DataSource[0])
        {
            if (Search.SearchStoryElement(_node, FilterText, StoryModel)) //checks if node name contains the thing we are looking for
            {
                _searchTotal++;
                if (Window.RequestedTheme == ElementTheme.Light)
                {
                    _node.Background = new SolidColorBrush(Colors.LightGoldenrodYellow);
                }
                else
                {
                    _node.Background = new SolidColorBrush(Colors.DarkGoldenrod);
                } //Light Goldenrod is hard to read in dark theme
                _node.IsExpanded = true;

                StoryNodeItem _parent = _node.Parent;
                if (_parent != null)
                {
                    while (!_parent.IsRoot)
                    {
                        _parent.IsExpanded = true;
                        _parent = _parent.Parent;
                    }

                    if (_parent.IsRoot) { _parent.IsExpanded = true; }
                }
            }
            else { _node.Background = null; }
        }

        switch (_searchTotal)
        {
            case 0:
                Messenger.Send(new StatusChangedMessage(new("Found no matches", LogLevel.Info, true)));
                break;
            case 1:
                Messenger.Send(new StatusChangedMessage(new("Found 1 match", LogLevel.Info, true)));
                break;
            default:
                Messenger.Send(new StatusChangedMessage(new($"Found {_searchTotal} matches", LogLevel.Info, true)));
                break;
        }
        _canExecuteCommands = true;    //Enables other commands from being used till this one is complete.
    }
    #endregion


}