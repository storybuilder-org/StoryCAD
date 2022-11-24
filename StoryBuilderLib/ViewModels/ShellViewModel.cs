using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Dialogs;
using StoryBuilder.Services.Dialogs.Tools;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.Services.Navigation;
using StoryBuilder.Services.Reports;
using StoryBuilder.Services.Search;
using StoryBuilder.ViewModels.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using StoryBuilder.Services;
using WinRT;
using GuidAttribute = System.Runtime.InteropServices.GuidAttribute;
using Octokit;
using ProductHeaderValue = Octokit.ProductHeaderValue;
using StoryBuilder.Services.Backend;

namespace StoryBuilder.ViewModels;

public class ShellViewModel : ObservableRecipient
{
    private bool _canExecuteCommands;

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
    public TreeViewItem RightClickedTreeviewItem;

    public StoryViewType CurrentViewType;

    private ContentDialog _contentDialog;

    private int _sourceIndex;
    private ObservableCollection<StoryNodeItem> _sourceChildren;
    private int _targetIndex;
    private ObservableCollection<StoryNodeItem> _targetCollection;
    public readonly LogService Logger;
    public readonly SearchService Search;

    private DispatcherTimer _statusTimer;
    private DispatcherTimer _autoSaveTimer = new();

    // The current story outline being processed. 
    public StoryModel StoryModel;

    public readonly ScrivenerIo Scrivener;

    // The right-hand (detail) side of ShellView
    public Frame SplitViewFrame;

    #region CommandBar Relay Commands

    // Open/Close Navigation pane (Hamburger menu)
    public RelayCommand TogglePaneCommand { get; }
    // Open file
    public RelayCommand OpenFileCommand { get; }
    // Save command
    public RelayCommand SaveFileCommand { get; }
    // SaveAs command
    public RelayCommand SaveAsCommand { get; }
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
    /// Used for theming
    /// </summary>
    public PreferencesModel UserPreferences = GlobalData.Preferences;

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

        if (ShellInstance.StoryModel.Changed) { return; }
        ShellInstance.StoryModel.Changed = true;
        ShellInstance.ChangeStatusColor = Colors.Red;
    }

    #endregion

    #region Public Methods

    public async Task PrintCurrentNodeAsync()
    {
        if (RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Right tap a node to print", LogLevel.Warn)));
            Logger.Log(LogLevel.Info, "Print node failed as no node is selected");
            _canExecuteCommands = true;
            return;
        }
        await Ioc.Default.GetRequiredService<PrintReportDialogVM>().PrintSingleNode(RightTappedNode);
    }

    private void CloseUnifiedMenu() { _contentDialog.Hide(); }

    public async Task OpenUnifiedMenu()
    {
        _canExecuteCommands = false;
        // Needs logging
        _contentDialog = new() { XamlRoot = GlobalData.XamlRoot, Content = new UnifiedMenuPage() };
        if (Microsoft.UI.Xaml.Application.Current.RequestedTheme == ApplicationTheme.Light) { _contentDialog.Background = new SolidColorBrush(Colors.LightGray); }
        await _contentDialog.ShowAsync();
        _canExecuteCommands = true;
    }

    public async Task UnifiedNewFile(UnifiedVM dialogVM)
    {
        _canExecuteCommands = false;
        Logger.Log(LogLevel.Info, "FileOpenVM - New File starting");
        try
        {
            Messenger.Send(new StatusChangedMessage(new("New project command executing", LogLevel.Info)));

            // If the current project needs saved, do so
            if (StoryModel.Changed)
            {
                SaveModel();
                await WriteModel();
            }

            // Start with a blank StoryModel and populate it
            // using the new project dialog's settings

            ResetModel();

            if (!Path.GetExtension(dialogVM.ProjectName)!.Equals(".stbx")) { dialogVM.ProjectName += ".stbx"; }
            StoryModel.ProjectFilename = dialogVM.ProjectName;
            StoryModel.ProjectFolder = await StorageFolder.GetFolderFromPathAsync(dialogVM.ProjectPath);
            StoryModel.ProjectPath = StoryModel.ProjectFolder.Path;

            OverviewModel _overview = new(Path.GetFileNameWithoutExtension(dialogVM.ProjectName), StoryModel) 
                { DateCreated = DateTime.Today.ToString("d"), Author = GlobalData.Preferences.Name };
            StoryNodeItem _overviewNode = new(_overview, null) { IsExpanded = true, IsRoot = true };
            StoryModel.ExplorerView.Add(_overviewNode);
            TrashCanModel _trash = new(StoryModel);
            StoryNodeItem _trashNode = new(_trash, null);
            StoryModel.ExplorerView.Add(_trashNode);     // The trashcan is the second root
            FolderModel _narrative = new("Narrative View", StoryModel, StoryItemType.Folder);
            StoryNodeItem _narrativeNode = new(_narrative, null) { IsRoot = true };
            StoryModel.NarratorView.Add(_narrativeNode);
            // Use the NewProjectDialog template to complete the model
            switch (dialogVM.SelectedTemplateIndex)
            {
                case 0:
                    break;
                case 1:
                    StoryElement _problems = new FolderModel("Problems", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _problemsNode = new(_problems, _overviewNode);
                    StoryElement _characters = new FolderModel("Characters", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _charactersNode = new(_characters, _overviewNode);
                    StoryElement _settings = new FolderModel("Settings", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _settingsNode = new(_settings, _overviewNode);
                    StoryElement _scene = new FolderModel("Scenes", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _plotpointsNode = new(_scene, _overviewNode);
                    break;
                case 2:
                    StoryElement _externalProblem = new ProblemModel("External Problem", StoryModel);
                    StoryNodeItem _externalProblemNode = new(_externalProblem, _overviewNode);
                    StoryElement _internalProblem = new ProblemModel("Internal Problem", StoryModel);
                    StoryNodeItem _internalProblemNode = new(_internalProblem, _overviewNode);
                    break;
                case 3:
                    StoryElement _protagonist = new CharacterModel("Protagonist", StoryModel);
                    StoryNodeItem _protagonistNode = new(_protagonist, _overviewNode);
                    StoryElement _antagonist = new CharacterModel("Antagonist", StoryModel);
                    StoryNodeItem _antagonistNode = new(_antagonist, _overviewNode);
                    break;
                case 4:
                    StoryElement _problemsFolder = new FolderModel("Problems", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _problemsFolderNode = new(_problemsFolder, _overviewNode) { IsExpanded = true };
                    StoryElement _charactersFolder = new FolderModel("Characters", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _charactersFolderNode = new(_charactersFolder, _overviewNode) { IsExpanded = true };
                    StoryElement _settingsFolder = new FolderModel("Settings", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _settingsFolderNode = new(_settingsFolder, _overviewNode);
                    StoryElement _plotpointsFolder = new FolderModel("Plot Points", StoryModel, StoryItemType.Folder);
                    StoryNodeItem _plotpointsFolderNode = new(_plotpointsFolder, _overviewNode);
                    StoryElement _externalProb = new ProblemModel("External Problem", StoryModel);
                    StoryNodeItem _externalProbNode = new(_externalProb, _problemsFolderNode);
                    StoryElement _internalProb = new ProblemModel("Internal Problem", StoryModel);
                    StoryNodeItem _internalProbNode = new(_internalProb, _problemsFolderNode);
                    StoryElement _protag = new CharacterModel("Protagonist", StoryModel);
                    StoryNodeItem _protagNode = new(_protag, _charactersFolderNode);
                    StoryElement _antag = new CharacterModel("Antagonist", StoryModel);
                    StoryNodeItem _antagNode = new(_antag, _charactersFolderNode);
                    break;
            }


            GlobalData.MainWindow.Title = $"StoryBuilder - Editing {dialogVM.ProjectName!.Replace(".stbx", "")}";
            SetCurrentView(StoryViewType.ExplorerView);
            //TODO: Set expand and is selected?

            Ioc.Default.GetRequiredService<UnifiedVM>().UpdateRecents(Path.Combine(dialogVM.ProjectPath, dialogVM.ProjectName)); //adds item to recent

            // Save the new project
            await SaveFile();
            if (GlobalData.Preferences.BackupOnOpen) { await MakeBackup(); }
            Ioc.Default.GetRequiredService<BackupService>().StartTimedBackup();
            Messenger.Send(new StatusChangedMessage(new("New project command executing", LogLevel.Info, true)));

        }
        catch (Exception _ex)
        {
            Logger.LogException(LogLevel.Error, _ex, "Error creating new project");
            Messenger.Send(new StatusChangedMessage(new("File make failure.", LogLevel.Error)));
        }
        _canExecuteCommands = true;
    }

    public async Task MakeBackup()
    {
        await Ioc.Default.GetRequiredService<BackupService>().BackupProject();
    }

    public void TreeViewNodeClicked(object selectedItem)
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
        await new ContentDialog
        {
            Title = "File missing.",
            Content = "This copy is missing a key file, if you are working on a branch or fork this is expected and you do not need to do anything about this." +
                      "\nHowever if you are not a developer then report this as it should not happen.\nThe following may have issues or possible errors\n" +
                      "Syncfusion related items and error logging.",
            XamlRoot = GlobalData.XamlRoot,
            PrimaryButtonText = "Okay"
        }.ShowAsync();
        Ioc.Default.GetRequiredService<LogService>().Log(LogLevel.Error, "Env missing.");
    }

    public async Task ShowChangelog()
    {
        try
        {
            GitHubClient _client = new(new ProductHeaderValue("Stb2ChangelogGrabber"));

            ContentDialog _changelogUi = new()
            {
                Width = 800,
                Content = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new TextBlock
                    {
                        TextWrapping = TextWrapping.Wrap,
                        Text = (await _client.Repository.Release.Get("storybuilder-org", "StoryBuilder-2", GlobalData.Version.Replace("Version: ", ""))).Body
                    }
                },
                Title = "What's new in StoryBuilder " + GlobalData.Version,
                PrimaryButtonText = "Okay",
                XamlRoot = GlobalData.XamlRoot
            };
            await _changelogUi.ShowAsync();
        }
        catch (Exception _e)
        {
            //TODO: Update with proper exception catching
            if (_e.Source!.Contains("Net")) { Logger.Log(LogLevel.Info, "Error with network, user probably isn't connected to wifi or is using an auto build"); }
            if (_e.Source!.Contains("Octokit.NotFoundException")) { Logger.Log(LogLevel.Info, "Error finding changelog for this version"); }
            else { Logger.Log(LogLevel.Info, "Error in ShowChangeLog()"); }
        }
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
    /// call its SaveModel() method. Hence this method, which determines which viewmodel's active 
    /// and calls its SaveModel() method.
    /// </summary>
    /// <returns></returns>
    private void SaveModel()
    {
        if (SplitViewFrame.CurrentSourcePageType is null){ return;}
        
        Logger.Log(LogLevel.Trace, $"SaveModel- Page type={SplitViewFrame.CurrentSourcePageType}");

        switch (SplitViewFrame.CurrentSourcePageType.ToString())
        {
            case "StoryBuilder.Views.OverviewPage":
                OverviewViewModel _ovm = Ioc.Default.GetRequiredService<OverviewViewModel>();
                _ovm.SaveModel();
                break;
            case "StoryBuilder.Views.ProblemPage":
                ProblemViewModel _pvm = Ioc.Default.GetRequiredService<ProblemViewModel>();
                _pvm.SaveModel();
                break;
            case "StoryBuilder.Views.CharacterPage":
                CharacterViewModel _cvm = Ioc.Default.GetRequiredService<CharacterViewModel>();
                _cvm.SaveModel();
                break;
            case "StoryBuilder.Views.ScenePage":
                SceneViewModel _scvm = Ioc.Default.GetRequiredService<SceneViewModel>();
                _scvm.SaveModel();
                break;
            case "StoryBuilder.Views.FolderPage":
                FolderViewModel _folderVM = Ioc.Default.GetRequiredService<FolderViewModel>();
                _folderVM.SaveModel();
                break;
            case "StoryBuilder.Views.SettingPage":
                SettingViewModel _settingVM = Ioc.Default.GetRequiredService<SettingViewModel>();
                _settingVM.SaveModel();
                break;
            case "StoryBuilder.Views.WebPage":
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
        if (GlobalData.Preferences.AutoSave)
        {
            if (GlobalData.Preferences.AutoSaveInterval is > 31 or < 4) { GlobalData.Preferences.AutoSaveInterval = 20; }
            else { GlobalData.Preferences.AutoSaveInterval = GlobalData.Preferences.AutoSaveInterval; }
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;
            _autoSaveTimer.Interval = new(0, 0, 0, GlobalData.Preferences.AutoSaveInterval, 0);
        }
        _autoSaveTimer.Stop();

        if (StoryModel.Changed)
        {
            SaveModel();
            await WriteModel();
        }

        _canExecuteCommands = false;
        Logger.Log(LogLevel.Info, "Executing OpenFile command");

        try
        {
            ResetModel();
            ShowHomePage();
            if (fromPath == "" || !File.Exists(fromPath))
            {
                Logger.Log(LogLevel.Info, "Opening file picker as story wasn't able to be found");
                FileOpenPicker _filePicker = new();
                //Make folder Picker work in Win32
                WinRT.Interop.InitializeWithWindow.Initialize(_filePicker, GlobalData.WindowHandle);
                _filePicker.CommitButtonText = "Project Folder";
                //TODO: Use preferences project folder instead of DocumentsLibrary
                //except you can't. Thanks, UWP.
                _filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                _filePicker.FileTypeFilter.Add(".stbx");
                StoryModel.ProjectFile = await _filePicker.PickSingleFileAsync();
                if (StoryModel.ProjectFile == null) //Picker was canceled.
                {
                    Logger.Log(LogLevel.Info, "File picked to locate file was canceled.");
                    _canExecuteCommands = true;  // unblock other commands
                    return;
                }
            }
            else
            {
                StoryModel.ProjectFile = await StorageFile.GetFileFromPathAsync(fromPath);
            }

            StoryModel.ProjectFolder = await StoryModel.ProjectFile.GetParentAsync();
            if (StoryModel.ProjectFile == null)
            {
                Logger.Log(LogLevel.Info, "Open File command cancelled (StoryModel.ProjectFile was null)");
                Messenger.Send(new StatusChangedMessage(new("Open Story command cancelled", LogLevel.Info)));
                _canExecuteCommands = true;  // unblock other commands
                return;
            }

            Ioc.Default.GetRequiredService<BackupService>().StopTimedBackup();
            //NOTE: BasicProperties.DateModified can be the date last changed

            StoryReader _rdr = Ioc.Default.GetRequiredService<StoryReader>();
            StoryModel = await _rdr.ReadFile(StoryModel.ProjectFile);

            if (GlobalData.Preferences.BackupOnOpen) { await Ioc.Default.GetRequiredService<BackupService>().BackupProject(); }

            if (StoryModel.ExplorerView.Count > 0)
            {
                SetCurrentView(StoryViewType.ExplorerView);
                Messenger.Send(new StatusChangedMessage(new("Open Story completed", LogLevel.Info)));
            }

            GlobalData.MainWindow.Title = $"StoryBuilder - Editing {StoryModel.ProjectFilename.Replace(".stbx", "")}";
            new UnifiedVM().UpdateRecents(Path.Combine(StoryModel.ProjectFolder.Path, StoryModel.ProjectFile.Name));
            if (GlobalData.Preferences.TimedBackup) { Ioc.Default.GetRequiredService<BackupService>().StartTimedBackup(); }

            ShowHomePage();
            if (GlobalData.Preferences.AutoSave) { _autoSaveTimer.Start(); }
            string _msg = $"Opened project {StoryModel.ProjectFilename}";
            Logger.Log(LogLevel.Info, _msg);
        }
        catch (Exception _ex)
        {
            Logger.LogException(LogLevel.Error, _ex, "Error in OpenFile command");
            Messenger.Send(new StatusChangedMessage(new("Open Story command failed", LogLevel.Error)));
        }

        Logger.Log(LogLevel.Info, "Open Story completed.");
        _canExecuteCommands = true;
    }
    public async Task SaveFile()
    {
        if (DataSource == null || DataSource.Count == 0)
        {
            Messenger.Send(new StatusChangedMessage(new("You need to open a story first!", LogLevel.Info)));
            Logger.Log(LogLevel.Info, "SaveFile command cancelled (DataSource was null or empty)");
            return;
        }
        Logger.Log(LogLevel.Trace, "Saving file");
        try //Updating the lost modified timer
        {
            ((OverviewModel)StoryModel.StoryElements.StoryElementGuids[StoryModel.ExplorerView[0].Uuid]).DateModified = DateTime.Now.ToString("d");
        }
        catch (NullReferenceException) { Messenger.Send(new StatusChangedMessage(new("Failed to update Last Modified date", LogLevel.Warn))); } //This appears to happen when in narrative view but im not sure how to fix it.

        _canExecuteCommands = false;
        Logger.Log(LogLevel.Info, "Executing SaveFile command");
        try
        {
            //TODO: SaveFile is both an AppButton command and called from NewFile and OpenFile. Split these.
            Messenger.Send(new StatusChangedMessage(new("Save File command executing", LogLevel.Info)));
            SaveModel();
            await WriteModel();
            Messenger.Send(new StatusChangedMessage(new("Save File command completed", LogLevel.Info)));
            StoryModel.Changed = false;
            ChangeStatusColor = Colors.Green;
        }
        catch (Exception _ex)
        {
            Logger.LogException(LogLevel.Error, _ex, "Exception in SaveFile");
            Messenger.Send(new StatusChangedMessage(new("Save File failed", LogLevel.Error)));
        }

        Logger.Log(LogLevel.Info, "SaveFile completed");
        _canExecuteCommands = true;
    }

    private async void AutoSaveTimer_Tick(object sender, object e)
    {
        if (GlobalData.Preferences.AutoSave) { await SaveFile(); }
    }

    /// <summary>
    /// Write the current StoryModel to the backing project file
    /// </summary>
    private async Task WriteModel()
    {
        Logger.Log(LogLevel.Info, $"In WriteModel, file={StoryModel.ProjectFilename}");
        try
        {
            await CreateProjectFile();
            StorageFile _file = StoryModel.ProjectFile;
            if (_file != null)
            {
                StoryWriter _wtr = Ioc.Default.GetRequiredService<StoryWriter>();
                //TODO: WriteFile isn't working; file is empty
                await _wtr.WriteFile(StoryModel.ProjectFile, StoryModel);
                // Prevent updates to the remote version of the file until
                // we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(_file);
                await CachedFileManager.CompleteUpdatesAsync(_file);
            }
        }
        catch (Exception _ex)
        {
            Logger.LogException(LogLevel.Error, _ex, "Error writing file");
            Messenger.Send(new StatusChangedMessage(new("Error writing file - see log", LogLevel.Error)));
            return;
        }
        Logger.Log(LogLevel.Info, "WriteModel successful");
    }

    private async void SaveFileAs()
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
                XamlRoot = GlobalData.XamlRoot,
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Cancel",
                Content = new SaveAsDialog()
            };

            //Sets needed data in VM and then shows the dialog
            SaveAsViewModel _saveAsVM = Ioc.Default.GetRequiredService<SaveAsViewModel>();
            // The default project name and project folder path are from the active StoryModel
            _saveAsVM.ProjectName = StoryModel.ProjectFilename;
            _saveAsVM.ProjectPathName = StoryModel.ProjectPath;

            ContentDialogResult _result = await _saveAsDialog.ShowAsync();

            if (_result == ContentDialogResult.Primary) //If save is clicked
            {
                if (await VerifyReplaceOrCreate())
                {
                    //Saves model to disk
                    SaveModel();
                    await WriteModel();

                    //Saves the current project folders and files to disk
                    await StoryModel.ProjectFile.CopyAsync(_saveAsVM.ParentFolder, _saveAsVM.ProjectName);

                    //Update the StoryModel properties to use the newly saved copy
                    StoryModel.ProjectFilename = _saveAsVM.ProjectName;
                    StoryModel.ProjectFolder = _saveAsVM.ParentFolder;
                    StoryModel.ProjectPath = _saveAsVM.SaveAsProjectFolderPath;
                    // Add to the recent files stack
                    GlobalData.MainWindow.Title = $"StoryBuilder - Editing {StoryModel.ProjectFilename.Replace(".stbx", "")}";
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
                Title = "Replace file",
                Content = $"File {_saveAsVM.SaveAsProjectFolderPath} already exists. \n\nDo you want to replace it?",
                XamlRoot = GlobalData.XamlRoot
            };
            return await _replaceDialog.ShowAsync() == ContentDialogResult.Primary;
        }
        return true;
    }

    private async void CloseFile()
    {
        _canExecuteCommands = false;
        Messenger.Send(new StatusChangedMessage(new("Closing project", LogLevel.Info, true)));
        _autoSaveTimer.Stop();
        if (StoryModel.Changed)
        {
            ContentDialog _warning = new()
            {
                Title = "Save changes?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                XamlRoot = GlobalData.XamlRoot
            };
            if (await _warning.ShowAsync() == ContentDialogResult.Primary)
            {
                SaveModel();
                await WriteModel();
            }
        }

        ResetModel();
        RightTappedNode = null; //Null right tapped node to prevent possible issues.
        SetCurrentView(StoryViewType.ExplorerView);
        GlobalData.MainWindow.Title = "StoryBuilder";
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
                XamlRoot = GlobalData.XamlRoot
            };
            if (await _warning.ShowAsync() == ContentDialogResult.Primary)
            {
                SaveModel();
                await WriteModel();
            }
        }
        BackendService _backend = Ioc.Default.GetRequiredService<BackendService>();
        await _backend.DeleteWorkFile();
        Logger.Flush();
        Microsoft.UI.Xaml.Application.Current.Exit();  // Win32
    }

    private async Task CreateProjectFile()
    {
        StoryModel.ProjectFile = await StoryModel.ProjectFolder.CreateFileAsync(StoryModel.ProjectFilename, CreationCollisionOption.ReplaceExisting);
    }

    #endregion

    #region Tool and Report Commands

    private async void Preferences()
    {
        Messenger.Send(new StatusChangedMessage(new("Updating Preferences", LogLevel.Info, true)));

        //Creates and shows dialog
        ContentDialog _preferencesDialog = new()
        {
            XamlRoot = GlobalData.XamlRoot,
            Content = new PreferencesDialog(),
            Title = "Preferences",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel"
        };

        ContentDialogResult _result = await _preferencesDialog.ShowAsync();
        switch (_result)
        {
            // Save changes
            case ContentDialogResult.Primary:
                await Ioc.Default.GetRequiredService<PreferencesViewModel>().SaveAsync();
                Messenger.Send(new StatusChangedMessage(new("Preferences updated", LogLevel.Info, true)));

                break;
            //don't save changes
            default:
                Messenger.Send(new StatusChangedMessage(new("Preferences closed", LogLevel.Info, true)));
                break;
        }

    }

    private async void KeyQuestionsTool()
    {
        Logger.Log(LogLevel.Info, "Displaying KeyQuestions tool dialog");
        if (RightTappedNode == null) { RightTappedNode = CurrentNode; }

        //Creates and shows dialog
        ContentDialog _keyQuestionsDialog = new()
        {
            Title = "Key questions",
            CloseButtonText = "Close",
            XamlRoot = GlobalData.XamlRoot,
            Content = new KeyQuestionsDialog()
        };
        await _keyQuestionsDialog.ShowAsync();

        Ioc.Default.GetRequiredService<KeyQuestionsViewModel>().NextQuestion();
        Logger.Log(LogLevel.Info, "KeyQuestions finished");
    }

    private async void TopicsTool()
    {
        Logger.Log(LogLevel.Info, "Displaying Topics tool dialog");
        if (RightTappedNode == null) { RightTappedNode = CurrentNode; }

        ContentDialog _dialog = new()
        {
            XamlRoot = GlobalData.XamlRoot,
            Title = "Topic Information",
            CloseButtonText = "Done",
            Content = new TopicsDialog()
        };
        await _dialog.ShowAsync();
        Logger.Log(LogLevel.Info, "Topics finished");
    }

    /// <summary>
    /// This shows the master plot dialog
    /// </summary>
    private async void MasterPlotTool()
    {
        Logger.Log(LogLevel.Info, "Displaying MasterPlot tool dialog");
        if (VerifyToolUse(true, true))
        {
            //Creates and shows content dialog
            ContentDialog _dialog = new()
            {
                XamlRoot = GlobalData.XamlRoot,
                Title = "Master plots",
                PrimaryButtonText = "Copy",
                SecondaryButtonText = "Cancel",
                Content = new MasterPlotsDialog()
            };
            ContentDialogResult _result = await _dialog.ShowAsync();

            if (_result == ContentDialogResult.Primary) // Copy command
            {
                MasterPlotsViewModel _masterPlotsVM = Ioc.Default.GetRequiredService<MasterPlotsViewModel>();
                string _masterPlotName = _masterPlotsVM.MasterPlotName;
                MasterPlotModel _model = _masterPlotsVM.MasterPlots[_masterPlotName];
                IList<MasterPlotScene> _scenes = _model.MasterPlotScenes;
                foreach (MasterPlotScene _scene in _scenes)
                {
                    SceneModel _child = new(StoryModel) { Name = _scene.SceneTitle, Remarks = "See Notes.", Notes = _scene.Notes };

                    if (RightTappedNode == null)
                    {
                        Messenger.Send(new StatusChangedMessage(new("You need to right click a node to", LogLevel.Info)));
                        return;
                    }

                    // add the new SceneModel & node to the end of the target's children 
                    StoryNodeItem _newNode = new(_child, RightTappedNode);
                    RightTappedNode.IsExpanded = true;
                    _newNode.IsSelected = true;
                }

                Messenger.Send(new StatusChangedMessage(new($"MasterPlot {_masterPlotName} inserted", LogLevel.Info, true)));
                ShowChange();
                Logger.Log(LogLevel.Info, "MasterPlot complete");
            }
        }
    }

    private async void DramaticSituationsTool()
    {
        Logger.Log(LogLevel.Info, "Displaying Dramatic Situations tool dialog");
        if (VerifyToolUse(true, true))
        {

            //Creates and shows dialog
            ContentDialog _dialog = new()
            {
                XamlRoot = GlobalData.XamlRoot,
                Title = "Dramatic situations",
                PrimaryButtonText = "Copy as problem",
                SecondaryButtonText = "Copy as scene",
                CloseButtonText = "Cancel",
                Content = new DramaticSituationsDialog()
            };
            ContentDialogResult _result = await _dialog.ShowAsync();

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
        Logger.Log(LogLevel.Info, "Dramatic Situations finished");
    }

    /// <summary>
    /// This loads the stock scenes dialog in the Plotting Aids submenu
    /// </summary>
    private async void StockScenesTool()
    {
        Logger.Log(LogLevel.Info, "Displaying Stock Scenes tool dialog");
        if (VerifyToolUse(true, true))
            try
            {
                //Creates and shows dialog
                ContentDialog _dialog = new()
                {
                    Title = "Stock scenes",
                    Content = new StockScenesDialog(),
                    PrimaryButtonText = "Add Scene",
                    CloseButtonText = "Cancel",
                    XamlRoot = GlobalData.XamlRoot
                };
                ContentDialogResult _result = await _dialog.ShowAsync();

                if (_result == ContentDialogResult.Primary)   // Copy command
                {
                    if (string.IsNullOrWhiteSpace(Ioc.Default.GetRequiredService<StockScenesViewModel>().SceneName))
                    {
                        Messenger.Send(new StatusChangedMessage(new("You need to select a stock scene", LogLevel.Warn)));
                        return;
                    }

                    SceneModel _sceneVar = new(StoryModel) { Name = Ioc.Default.GetRequiredService<StockScenesViewModel>().SceneName };
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
            catch (Exception _e) { Logger.LogException(LogLevel.Error, _e, _e.Message); }
    }

    private async void GeneratePrintReports()
    {
        PrintReportDialogVM _reportVM = Ioc.Default.GetRequiredService<PrintReportDialogVM>();
        if (Ioc.Default.GetRequiredService<ShellViewModel>().DataSource == null)
        {
            Messenger.Send(new StatusChangedMessage(new("You need to load a Story first!", LogLevel.Warn)));
            return;
        }
        _canExecuteCommands = false;
        Messenger.Send(new StatusChangedMessage(new("Generate Print Reports executing", LogLevel.Info, true)));

        SaveModel();

        // Run reports dialog
        _reportVM.Dialog = new()
        {
            Title = "Generate Reports",
            XamlRoot = GlobalData.XamlRoot,
            Content = new PrintReportsDialog()
        };
        await _reportVM.Dialog.ShowAsync();
        _canExecuteCommands = true;
    }

    private async void GenerateScrivenerReports()
    {
        //TODO: revamp this to be more user friendly.
        _canExecuteCommands = false;
        Messenger.Send(new StatusChangedMessage(new("Generate Scrivener Reports executing", LogLevel.Info, true)));
        SaveModel();

        // Select the Scrivener .scrivx file to add the report to
        FileOpenPicker _openPicker = new();
        if (Window.Current == null)
        {
            IntPtr _hwnd = GetActiveWindow();
            IInitializeWithWindow _initializeWithWindow = _openPicker.As<IInitializeWithWindow>();
            _initializeWithWindow.Initialize(_hwnd);
        }
        _openPicker.ViewMode = PickerViewMode.List;
        _openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        _openPicker.FileTypeFilter.Add(".scrivx");
        StorageFile _file = await _openPicker.PickSingleFileAsync();
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

        ProcessStartInfo _psi = new()
        {
            FileName = @"https://storybuilder-org.github.io/StoryBuilder-2/",
            UseShellExecute = true
        };
        Process.Start(_psi);

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
    private bool VerifyToolUse(bool explorerViewOnly, bool nodeRequired, bool checkOutlineIsOpen = true)
    {
        try
        {

            if (explorerViewOnly && !IsExplorerView())
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
        if (CurrentNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Click or touch a node to move", LogLevel.Warn)));
            return;
        }

        if (CurrentNode.Parent != null && CurrentNode.Parent.IsRoot)
        {
            Messenger.Send(new StatusChangedMessage(new("Cannot move further left", LogLevel.Warn)));
            return;
        }


        if (!MoveIsValid()) // Verify message
            return;

        if (CurrentNode.Parent != null)
        {
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
        else
            Messenger.Send(new StatusChangedMessage(new("Cannot move root node.", LogLevel.Warn)));
    }

    private void MoveTreeViewItemRight()
    {
        //TODO: Logging
        StoryNodeItem _targetParent;

        if (CurrentNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Click or touch a node to move", LogLevel.Warn)));
            return;
        }

        // Probably true only if first child of root
        //if (_currentNode.Parent.IsRoot)
        //{
        //    StatusMessage = "Cannot move further right";
        //    return;
        //}
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


        if (_sourceIndex > 0) // not first child, new parent will be previous sibling
        {
            _targetParent = CurrentNode.Parent.Children[_sourceIndex - 1];
            _targetCollection = _targetParent.Children;
            _targetIndex = _targetCollection.Count;
        }
        else
        {
            // find parent's predecessor
            if (CurrentNode.Parent.Parent == null)
            {
                Messenger.Send(new StatusChangedMessage(new("Cannot move further right", LogLevel.Warn)));
                return;
            }

            ObservableCollection<StoryNodeItem> _grandparentCollection = CurrentNode.Parent.Parent.Children;
            int _siblingIndex = _grandparentCollection.IndexOf(CurrentNode.Parent) - 1;
            if (_siblingIndex >= 0)
            {
                _targetParent = _grandparentCollection[_siblingIndex];
                _targetCollection = _targetParent.Children;
                if (_targetCollection.Count > 0)
                {
                    _targetParent = _targetCollection[^1];
                    _targetCollection = _targetParent.Children;
                    _targetIndex = _targetCollection.Count;
                }
                else
                {
                    Messenger.Send(new StatusChangedMessage(new("Cannot move further right", LogLevel.Warn)));
                    return;
                }
            }
            else
            {
                Messenger.Send(new StatusChangedMessage(new("Cannot move further right", LogLevel.Warn)));
                return;
            }
        }

        if (MoveIsValid()) // Verify move
        {
            _sourceChildren.RemoveAt(_sourceIndex);
            if (_targetIndex == -1) { _targetCollection.Add(CurrentNode); }
            else
                _targetCollection.Insert(_targetIndex, CurrentNode);
            CurrentNode.Parent = _targetParent;
            ShowChange();

            Logger.Log(LogLevel.Info, $"Moving {CurrentNode.Name} right to parent {CurrentNode.Parent.Name}");
        }
    }

    private void MoveTreeViewItemUp()
    {
        if (CurrentNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Click or touch a node to move", LogLevel.Warn)));
            return;
        }

        if (CurrentNode.IsRoot)
        {
            Messenger.Send(new StatusChangedMessage(new("Cannot move up further", LogLevel.Warn)));
            return;
        }
        _sourceChildren = CurrentNode.Parent.Children;
        _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
        _targetCollection = null;
        _targetIndex = -1;
        StoryNodeItem _targetParent = CurrentNode.Parent;

        // If first child, must move to end parent's predecessor
        if (_sourceIndex == 0)
        {
            if (CurrentNode.Parent.Parent == null)
            {
                Messenger.Send(new StatusChangedMessage(new("Cannot move up further", LogLevel.Warn)));
                return;
            }
            // find parent's predecessor
            ObservableCollection<StoryNodeItem> _grandparentCollection = CurrentNode.Parent.Parent.Children;
            int _siblingIndex = _grandparentCollection.IndexOf(CurrentNode.Parent) - 1;
            if (_siblingIndex >= 0)
            {
                _targetCollection = _grandparentCollection[_siblingIndex].Children;
                _targetParent = _grandparentCollection[_siblingIndex];
            }
            else
            {
                Messenger.Send(new StatusChangedMessage(new("Cannot move up further", LogLevel.Warn)));
                return;
            }
        }
        // Otherwise, move up a notch
        else
        {
            _targetCollection = _sourceChildren;
            _targetIndex = _sourceIndex - 1;
        }

        if (MoveIsValid()) // Verify move
        {
            _sourceChildren.RemoveAt(_sourceIndex);
            if (_targetIndex == -1)
                _targetCollection.Add(CurrentNode);
            else
                _targetCollection.Insert(_targetIndex, CurrentNode);
            CurrentNode.Parent = _targetParent;
            ShowChange();

            Logger.Log(LogLevel.Info, $"Moving {CurrentNode.Name} up to parent {CurrentNode.Parent.Name}");
        }
    }

    private void MoveTreeViewItemDown()
    {
        if (CurrentNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Click or touch a node to move", LogLevel.Warn)));
            return;
        }
        if (CurrentNode.IsRoot)
        {
            Messenger.Send(new StatusChangedMessage(new("Cannot move a root node", LogLevel.Warn)));
            return;
        }

        _sourceChildren = CurrentNode.Parent.Children;
        _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
        _targetCollection = null;
        _targetIndex = 0;
        StoryNodeItem _targetParent = CurrentNode.Parent;

        // If last child, must move to end parent's successor
        if (_sourceIndex == _sourceChildren.Count - 1)
        {
            if (CurrentNode.Parent.Parent == null)
            {
                Messenger.Send(new StatusChangedMessage(new("Cannot move down further", LogLevel.Warn)));
                return;
            }
            // find parent's successor
            ObservableCollection<StoryNodeItem> _grandparentCollection = CurrentNode.Parent.Parent.Children;
            int _siblingIndex = _grandparentCollection.IndexOf(CurrentNode.Parent) + 1;
            if (_siblingIndex == _grandparentCollection.Count)
            {
                CurrentNode.Parent = DataSource[1];
                _sourceChildren.RemoveAt(_sourceIndex);
                DataSource[1].Children.Insert(_targetIndex, CurrentNode);
                Messenger.Send(new StatusChangedMessage(new("Moved to trash", LogLevel.Info)));

                return;
            }
            if (_grandparentCollection[_siblingIndex].IsRoot)
            {
                Messenger.Send(new StatusChangedMessage(new("Cannot move down further", LogLevel.Warn)));
                return;
            }
            _targetCollection = _grandparentCollection[_siblingIndex].Children;
            _targetParent = _grandparentCollection[_siblingIndex];
        }
        // Otherwise, move down a notch
        else
        {
            _targetCollection = _sourceChildren;
            _targetIndex = _sourceIndex + 1;
        }

        if (MoveIsValid()) // Verify move
        {
            _sourceChildren.RemoveAt(_sourceIndex);
            _targetCollection.Insert(_targetIndex, CurrentNode);
            CurrentNode.Parent = _targetParent;
            ShowChange();

            Logger.Log(LogLevel.Info, $"Moving {CurrentNode.Name} down up to parent {CurrentNode.Parent.Name}");
        }
    }


    #endregion

    #region Add and Remove Story Element Commands

    private void AddFolder()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Folder));
    }

    private void AddSection()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Section));
    }

    private void AddProblem()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Problem));
    }

    private void AddCharacter()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Character));
    }
    private void AddWeb()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Web));
    }
    private void AddNotes()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Notes));
    }

    private void AddSetting()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Setting));
    }

    private void AddScene()
    {
        TreeViewNodeClicked(AddStoryElement(StoryItemType.Scene));
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
        }

        Messenger.Send(new IsChangedMessage(true));
        Messenger.Send(new StatusChangedMessage(new($"Added new {typeToAdd}", LogLevel.Info, true)));
        _canExecuteCommands = true;

        return null;
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
            _contentDialog = new()
            {
                XamlRoot = GlobalData.XamlRoot,
                Content = _content,
                Title = "Are you sure you want to delete this node?",
                Width = 500,
                PrimaryButtonText = "Confirm",
                SecondaryButtonText = "Cancel"
            }; 
            if (await _contentDialog.ShowAsync() == ContentDialogResult.Secondary) { _delete = false; }
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
    private static StoryItemType RootNodeType(StoryNodeItem startNode)
    {
        StoryNodeItem _node = startNode;
        while (!_node.IsRoot)
            _node = _node.Parent;
        return _node.Type;
    }

    #endregion

    /// <summary>
    /// TODO: This method is not implemented yet.
    /// Remove the Re sharper comment once implemented.
    /// </summary>
    /// <returns></returns>
    // ReSharper disable once MemberCanBeMadeStatic.Local
    private bool MoveIsValid() { return true; }

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
        switch (RootNodeType(RightTappedNode))
        {
            case StoryItemType.StoryOverview:   // ExplorerView tree
                AddFolderVisibility = Visibility.Visible;
                AddSectionVisibility = Visibility.Collapsed;
                AddProblemVisibility = Visibility.Visible;
                AddCharacterVisibility = Visibility.Visible;
                AddSettingVisibility = Visibility.Visible;
                AddSceneVisibility = Visibility.Visible;
                RemoveStoryElementVisibility = Visibility.Visible;
                //TODO: Use correct values (bug with this)
                //RestoreStoryElementVisibility = Visibility.Collapsed;
                RestoreStoryElementVisibility = Visibility.Visible;
                AddToNarrativeVisibility = Visibility.Visible;
                //RemoveFromNarrativeVisibility = Visibility.Collapsed;
                RestoreStoryElementVisibility = Visibility.Visible;
                PrintNodeVisibility = Visibility.Visible;
                EmptyTrashVisibility = Visibility.Collapsed;
                break;
            case StoryItemType.Section:         // NarratorView tree
                AddFolderVisibility = Visibility.Collapsed;
                AddSectionVisibility = Visibility.Visible;
                AddProblemVisibility = Visibility.Collapsed;
                AddCharacterVisibility = Visibility.Collapsed;
                AddSettingVisibility = Visibility.Collapsed;
                AddSceneVisibility = Visibility.Collapsed;
                RemoveStoryElementVisibility = Visibility.Visible;
                RestoreStoryElementVisibility = Visibility.Collapsed;
                AddToNarrativeVisibility = Visibility.Collapsed;
                RemoveFromNarrativeVisibility = Visibility.Visible;
                PrintNodeVisibility = Visibility.Visible;
                EmptyTrashVisibility = Visibility.Collapsed;
                break;
            case StoryItemType.TrashCan:        // Trashcan tree (either view)
                AddFolderVisibility = Visibility.Collapsed;
                AddSectionVisibility = Visibility.Collapsed;
                AddProblemVisibility = Visibility.Collapsed;
                AddCharacterVisibility = Visibility.Collapsed;
                AddSettingVisibility = Visibility.Collapsed;
                AddSceneVisibility = Visibility.Collapsed;
                RemoveStoryElementVisibility = Visibility.Collapsed;
                RestoreStoryElementVisibility = Visibility.Visible;
                AddToNarrativeVisibility = Visibility.Collapsed;
                RemoveFromNarrativeVisibility = Visibility.Collapsed;
                PrintNodeVisibility = Visibility.Collapsed;
                EmptyTrashVisibility = Visibility.Visible;
                break;
        }
    }

    public void ShowConnectionStatus()
    {
        StatusMessage _msg;
        if (!GlobalData.DopplerConnection | !GlobalData.ElmahLogging)
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
            CurrentViewType = StoryViewType.ExplorerView;
        }
        else if (view == StoryViewType.NarratorView)
        {
            DataSource = StoryModel.NarratorView;
            CurrentViewType = StoryViewType.NarratorView;
        }
    }

    private bool IsExplorerView() => CurrentViewType == StoryViewType.ExplorerView;
    // ReSharper disable once UnusedMember.Local
    private bool IsNarratorView() => CurrentViewType == StoryViewType.NarratorView;

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
        if (_statusTimer.IsEnabled) { _statusTimer.Stop(); } //Stops a timer if one is already running

        StatusMessage = statusMessage.Value.Status; //This shows the message

        switch (statusMessage.Value.Level)
        {
            case LogLevel.Info:
                StatusColor = GlobalData.Preferences.SecondaryColor;
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

        //_itemSelector = Ioc.Default.GetRequiredService<TreeViewSelection>();

        Messenger.Register<IsChangedRequestMessage>(this, (_, m) => { m.Reply(StoryModel!.Changed); });
        Messenger.Register<ShellViewModel, IsChangedMessage>(this, static (r, m) => r.IsChangedMessageReceived(m));
        Messenger.Register<ShellViewModel, StatusChangedMessage>(this, static (r, m) => r.StatusMessageReceived(m));
        Messenger.Register<ShellViewModel, NameChangedMessage>(this, static (r, m) => r.NameMessageReceived(m));

        //Preferences = Ioc.Default.GetRequiredService<Preferences>();
        Scrivener = Ioc.Default.GetRequiredService<ScrivenerIo>();
        Logger = Ioc.Default.GetRequiredService<LogService>();
        Search = Ioc.Default.GetRequiredService<SearchService>();

        StoryModel = new StoryModel();

        _statusTimer = new DispatcherTimer();
        _statusTimer.Tick += statusTimer_Tick;

        Messenger.Send(new StatusChangedMessage(new("Ready", LogLevel.Info)));

        _canExecuteCommands = true;
        TogglePaneCommand = new RelayCommand(TogglePane, () => _canExecuteCommands);
        OpenUnifiedCommand = new RelayCommand(async () => await OpenUnifiedMenu(), () => _canExecuteCommands);
        CloseUnifiedCommand = new RelayCommand(CloseUnifiedMenu, () => _canExecuteCommands);
        NarrativeToolCommand = new RelayCommand(async () => await OpenNarrativeTool(), () => _canExecuteCommands);
        PrintNodeCommand = new RelayCommand(async () => await PrintCurrentNodeAsync(), () => _canExecuteCommands);
        OpenFileCommand = new RelayCommand(async () => await OpenFile(), () => _canExecuteCommands);
        SaveFileCommand = new RelayCommand(async () => await SaveFile(), () => _canExecuteCommands);
        SaveAsCommand = new RelayCommand(SaveFileAs, () => _canExecuteCommands);
        CloseCommand = new RelayCommand(CloseFile, () => _canExecuteCommands);
        ExitCommand = new RelayCommand(ExitApp, () => _canExecuteCommands);

        // Tools commands
        KeyQuestionsCommand = new RelayCommand(KeyQuestionsTool, () => _canExecuteCommands);
        TopicsCommand = new RelayCommand(TopicsTool, () => _canExecuteCommands);
        MasterPlotsCommand = new RelayCommand(MasterPlotTool, () => _canExecuteCommands);
        DramaticSituationsCommand = new RelayCommand(DramaticSituationsTool, () => _canExecuteCommands);
        StockScenesCommand = new RelayCommand(StockScenesTool, () => _canExecuteCommands);

        PreferencesCommand = new RelayCommand(Preferences, () => _canExecuteCommands);

        PrintReportsCommand = new RelayCommand(GeneratePrintReports, () => _canExecuteCommands);
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

        ChangeStatusColor = Colors.Green;

        ShellInstance = this;
    }

    private async Task OpenNarrativeTool()
    {
        if (VerifyToolUse(false,false))
        {
            ContentDialog _dialog = new()
            {
                XamlRoot = GlobalData.XamlRoot,
                Title = "Narrative Editor",
                PrimaryButtonText = "Done",
                Content = new NarrativeTool()
            };
            await _dialog.ShowAsync();
        }
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
                if (Microsoft.UI.Xaml.Application.Current.RequestedTheme == ApplicationTheme.Light) { _node.Background = new SolidColorBrush(Colors.LightGoldenrodYellow); }
                else { _node.Background = new SolidColorBrush(Colors.DarkGoldenrod); } //Light Goldenrod is hard to read in dark theme
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

    [ComImport]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = true, SetLastError = false)]
    private static extern IntPtr GetActiveWindow();
}