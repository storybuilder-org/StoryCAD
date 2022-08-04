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

namespace StoryBuilder.ViewModels
{
    public class ShellViewModel : ObservableRecipient
    {
        private bool _canExecuteCommands;

        private const string HomePage = "HomePage";
        private const string OverviewPage = "OverviewPage";
        private const string ProblemPage = "ProblemPage";
        private const string CharacterPage = "CharacterPage";
        private const string ScenePage = "ScenePage";
        private const string FolderPage = "FolderPage";
        private const string SectionPage = "SectionPage";
        private const string SettingPage = "SettingPage";
        private const string TrashCanPage = "TrashCanPage";

        // Navigation navigation landmark nodes
        public StoryNodeItem CurrentNode { get; set; }
        public StoryNodeItem RightTappedNode;

        public StoryViewType ViewType;

        private ContentDialog _contentDialog;

        private int _sourceIndex;
        private ObservableCollection<StoryNodeItem> _sourceChildren;
        private int _targetIndex;
        private ObservableCollection<StoryNodeItem> _targetCollection;
        public readonly LogService Logger;
        public readonly SearchService Search;

        private DispatcherTimer statusTimer;
        private DispatcherTimer autoSaveTimer = new();

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

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
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

        private Visibility _emptyTrashVisibilty;
        public Visibility EmptyTrashVisibility
        {
            get => _emptyTrashVisibilty;
            set => SetProperty(ref _emptyTrashVisibilty, value);
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

        private string _newNodeName;
        public string NewNodeName
        {
            get => _newNodeName;
            set => SetProperty(ref _newNodeName, value);
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

            if (ShellInstance.StoryModel.Changed)
                return;
            ShellInstance.StoryModel.Changed = true;
            ShellInstance.ChangeStatusColor = Colors.Red;
        }

        #endregion

        #region Public Methods

        public async Task PrintCurrentNodeAsync()
        {
            if (RightTappedNode == null)
            {
                Messenger.Send(new StatusChangedMessage(new($"Right tap a node to print", LogLevel.Warn)));
                Logger.Log(LogLevel.Info, "Print node failed as no node is selected");
                _canExecuteCommands = true;
                return;
            }

            PrintReportDialogVM PrintVM = Ioc.Default.GetRequiredService<PrintReportDialogVM>();
            PrintVM.SelectedNodes.Clear(); //Makes sure only one node is selected

            if (RightTappedNode.Type == StoryItemType.StoryOverview) { PrintVM.CreateOverview = true; }
            else { PrintVM.SelectedNodes.Add(RightTappedNode); }

            await new PrintReports(PrintVM, StoryModel).Generate();
        }

        private void CloseUnifiedMenu() {  _contentDialog.Hide();  }

        public async Task OpenUnifiedMenu()                      
        {
            _canExecuteCommands = false;
            // Needs logging
            _contentDialog = new();
            _contentDialog.XamlRoot = GlobalData.XamlRoot;
            _contentDialog.Content = new UnifiedMenuPage();
            if (Microsoft.UI.Xaml.Application.Current.RequestedTheme == ApplicationTheme.Light) { _contentDialog.Background = new SolidColorBrush(Colors.LightGray); }
            await _contentDialog.ShowAsync();
            _canExecuteCommands = true;
        }

        public async Task UnifiedNewFile(UnifiedVM dialogVM)
        {
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "UnifyVM - New File starting");
            try
            {
                Messenger.Send(new StatusChangedMessage(new($"New project command executing", LogLevel.Info)));

                // If the current project needs saved, do so
                if (StoryModel.Changed)
                {
                    SaveModel();
                    await WriteModel();
                }
                
                UnifiedVM vm = dialogVM; //Access the dialog settings

                // Start with a blank StoryModel and populate it
                // using the new project dialog's settings

                ResetModel();

                if (!Path.GetExtension(vm.ProjectName).Equals(".stbx")) { vm.ProjectName += ".stbx"; }
                StoryModel.ProjectFilename = vm.ProjectName;
                StoryModel.ProjectFolder = await StorageFolder.GetFolderFromPathAsync(vm.ProjectPath);
                StoryModel.ProjectPath = StoryModel.ProjectFolder.Path;

                OverviewModel overview = new("Working Title", StoryModel){DateCreated = DateTime.Today.ToString("d")};
                overview.Author = GlobalData.Preferences.Name;
                StoryNodeItem overviewNode = new(overview, null) { IsExpanded = true, IsRoot = true };
                StoryModel.ExplorerView.Add(overviewNode);
                TrashCanModel trash = new(StoryModel);
                StoryNodeItem trashNode = new(trash, null);
                StoryModel.ExplorerView.Add(trashNode);     // The trashcan is the second root
                SectionModel narrative = new("Narrative View", StoryModel);
                StoryNodeItem narrativeNode = new(narrative, null);
                narrativeNode.IsRoot = true;
                StoryModel.NarratorView.Add(narrativeNode);
                StoryModel.NarratorView.Add(trashNode);     // Both views share the trashcan
                // Use the NewProjectDialog template to complete the model
                switch (vm.SelectedTemplateIndex)
                {
                    case 0:
                        break;
                    case 1:
                        StoryElement problems = new FolderModel("Problems", StoryModel);
                        StoryNodeItem problemsNode = new(problems, overviewNode);
                        StoryElement characters = new FolderModel("Characters", StoryModel);
                        StoryNodeItem charactersNode = new(characters, overviewNode);
                        StoryElement settings = new FolderModel("Settings", StoryModel);
                        StoryNodeItem settingsNode = new(settings, overviewNode);
                        StoryElement scene = new FolderModel("Scene", StoryModel);
                        StoryNodeItem plotpointsNode = new(scene, overviewNode);
                        break;
                    case 2:
                        StoryElement externalProblem = new ProblemModel("External Problem", StoryModel);
                        StoryNodeItem externalProblemNode = new(externalProblem, overviewNode);
                        StoryElement internalProblem = new ProblemModel("Internal Problem", StoryModel);
                        StoryNodeItem internalProblemNode = new(internalProblem, overviewNode);
                        break;
                    case 3:
                        StoryElement protagonist = new CharacterModel("Protagonist", StoryModel);
                        StoryNodeItem protagonistNode = new(protagonist, overviewNode);
                        StoryElement antagonist = new CharacterModel("Antagonist", StoryModel);
                        StoryNodeItem antagonistNode = new(antagonist, overviewNode);
                        break;
                    case 4:
                        StoryElement problemsFolder = new FolderModel("Problems", StoryModel);
                        StoryNodeItem problemsFolderNode = new(problemsFolder, overviewNode)
                        {
                            IsExpanded = true
                        };
                        StoryElement charactersFolder = new FolderModel("Characters", StoryModel);
                        StoryNodeItem charactersFolderNode = new(charactersFolder, overviewNode);
                        charactersFolderNode.IsExpanded = true;
                        StoryElement settingsFolder = new FolderModel("Settings", StoryModel);
                        StoryNodeItem settingsFolderNode = new(settingsFolder, overviewNode);
                        StoryElement plotpointsFolder = new FolderModel("Plot Points", StoryModel);
                        StoryNodeItem plotpointsFolderNode = new(plotpointsFolder, overviewNode);
                        StoryElement externalProb = new ProblemModel("External Problem", StoryModel);
                        StoryNodeItem externalProbNode = new(externalProb, problemsFolderNode);
                        StoryElement internalProb = new ProblemModel("Internal Problem", StoryModel);
                        StoryNodeItem internalProbNode = new(internalProb, problemsFolderNode);
                        StoryElement protag = new CharacterModel("Protagonist", StoryModel);
                        StoryNodeItem protagNode = new(protag, charactersFolderNode);
                        StoryElement antag = new CharacterModel("Antagonist", StoryModel);
                        StoryNodeItem antagNode = new(antag, charactersFolderNode);
                        break;
                }


                GlobalData.MainWindow.Title = $"StoryBuilder - Editing {vm.ProjectName.Replace(".stbx","")}";
                SetCurrentView(StoryViewType.ExplorerView);
                //TODO: Set expand and is selected?
                
                Ioc.Default.GetService<UnifiedVM>().UpdateRecents(Path.Combine(vm.ProjectPath,vm.ProjectName)); //adds item to recent

                // Save the new project
                await SaveFile();
                if (GlobalData.Preferences.BackupOnOpen) { await MakeBackup(); }
                Ioc.Default.GetService<BackupService>().StartTimedBackup();
                Messenger.Send(new StatusChangedMessage(new($"New project command executing", LogLevel.Info, true)));

            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Error creating new project");
                Messenger.Send(new StatusChangedMessage(new($"File make failure.", LogLevel.Error, false)));
            }
            _canExecuteCommands = true;
        }

        public async Task MakeBackup()
        {
            await Ioc.Default.GetService<BackupService>().BackupProject();
        }

        public void TreeViewNodeClicked(object selectedItem)
        {
            if (selectedItem is null)
            {
                Logger.Log(LogLevel.Info, "TreeViewNodeClicked for null node, event ignored");
                return;
            }
            Logger.Log(LogLevel.Info, $"TreeViewNodeClicked for {selectedItem}");

            try {
                NavigationService nav = Ioc.Default.GetService<NavigationService>();
                if (selectedItem is StoryNodeItem node)
                {
                    CurrentNode = node;
                    StoryElement element = StoryModel.StoryElements.StoryElementGuids[node.Uuid];
                    switch (node.Type)
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
                            nav.NavigateTo(SplitViewFrame, SectionPage, element);
                            break;
                        case StoryItemType.Folder:
                            nav.NavigateTo(SplitViewFrame, FolderPage, element);
                            break;
                        case StoryItemType.Setting:
                            nav.NavigateTo(SplitViewFrame, SettingPage, element);
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
            }
            catch (Exception e)
            {
                Logger.LogException(LogLevel.Error, e, "Error navigating in TreeViewNodeClicked");
            }
        }

        /// <summary>
        /// Shows dotenv warning.
        /// </summary>
        public async Task ShowWarningAsync()
        {
            ContentDialog dialog = new();
            dialog.Title = "File missing.";
            dialog.Content = "This copy is missing a key file, if you are working on a branch or fork this is expected and you do not need to do anything about this." +
                          "\nHowever if you are not a developer then report this as it should not happen.\nThe following may have issues or possible errors\n" +
                          "Syncfusion related items and error logging.";
            dialog.XamlRoot = GlobalData.XamlRoot;
            dialog.PrimaryButtonText = "Okay";
            await dialog.ShowAsync();
            Ioc.Default.GetService<LogService>().Log(LogLevel.Error,"Env missing.");
        }

        public async Task ShowChangelog()
        {
            try
            {
                GitHubClient client = new(new ProductHeaderValue("Stb2ChangelogGrabber"));

                ContentDialog ChangelogUI = new()
                {
                    Width = 800,
                    Content = new ScrollViewer()
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = new TextBlock()
                        {
                            TextWrapping = TextWrapping.Wrap,
                            Text = (await client.Repository.Release.Get("storybuilder-org", "StoryBuilder-2", GlobalData.Version.Replace("Version: ", ""))).Body
                        }
                    },
                    Title = "What's new in StoryBuilder " + GlobalData.Version,
                    PrimaryButtonText = "Okay",
                    XamlRoot = GlobalData.XamlRoot
                };
                await ChangelogUI.ShowAsync();
            }
            catch (Exception e)
            {
                if (e.Source.Contains("Net")) { Logger.Log( LogLevel.Info , "Error with network, user probably isn't connected to wifi or is using an autobuild"); }
                if (e.Source.Contains("Octokit.NotFoundException")) { Logger.Log( LogLevel.Info , "Error finding changelog for this version"); }
                else { Logger.Log(LogLevel.Info, "Error in ShowChangeLog()"); }
            }
        }

        public void ShowHomePage()
        {
            Logger.Log(LogLevel.Info, "ShowHomePage");
    
            NavigationService nav = Ioc.Default.GetService<NavigationService>();
            nav.NavigateTo(SplitViewFrame, HomePage);
        }

        /// <summary>
        /// Process the MainWindow's Closed event.
        /// 
        /// The user has clicked the window's close button.
        /// Insure the file is saved before allowding the
        /// app to terminate.
        /// </summary>
        public static void ProcessCloseButton()
        {
            //BUG: Process the close button
            //throw new NotImplementedException();
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
            if (SplitViewFrame.CurrentSourcePageType is null)
                return;
            Logger.Log(LogLevel.Trace, $"SaveModel- Page type={SplitViewFrame.CurrentSourcePageType}");
            if (SplitViewFrame.CurrentSourcePageType == null)
                return;
            switch (SplitViewFrame.CurrentSourcePageType.ToString())
            {
                case "StoryBuilder.Views.OverviewPage":
                    OverviewViewModel ovm = Ioc.Default.GetService<OverviewViewModel>();
                    ovm.SaveModel();
                    break;
                case "StoryBuilder.Views.ProblemPage":
                    ProblemViewModel pvm = Ioc.Default.GetService<ProblemViewModel>();
                    pvm.SaveModel();
                    break;
                case "StoryBuilder.Views.CharacterPage":
                    CharacterViewModel cvm = Ioc.Default.GetService<CharacterViewModel>();
                    cvm.SaveModel();
                    break;
                case "StoryBuilder.Views.ScenePage":
                    SceneViewModel scvm = Ioc.Default.GetService<SceneViewModel>();
                    scvm.SaveModel();
                    break;
                case "StoryBuilder.Views.FolderPage":
                    FolderViewModel fpvm = Ioc.Default.GetService<FolderViewModel>();
                    fpvm.SaveModel();
                    break;
                case "StoryBuilder.Views.SectionPage":
                    SectionViewModel secpvm = Ioc.Default.GetService<SectionViewModel>();
                    secpvm.SaveModel();
                    break;
                case "StoryBuilder.Views.SettingPage":
                    SettingViewModel setvm = Ioc.Default.GetService<SettingViewModel>();
                    setvm.SaveModel();
                    break;
            }
        }

        
        /// <summary>
        /// Opens a file picker to let the user chose a stbx file and loads said file
        /// If fromPath is specified then the picker is skipped.
        /// </summary>
        /// <param name="fromPath"></param>
        public async Task OpenFile(string fromPath = "")
        {
            if (GlobalData.Preferences.AutoSave)
            {
                if (GlobalData.Preferences.AutoSaveInterval > 31 || GlobalData.Preferences.AutoSaveInterval < 4) { GlobalData.Preferences.AutoSaveInterval = 20; }
                else { GlobalData.Preferences.AutoSaveInterval = GlobalData.Preferences.AutoSaveInterval; }
                autoSaveTimer.Tick += AutoSaveTimer_Tick;
                autoSaveTimer.Interval = new(0, 0, 0, GlobalData.Preferences.AutoSaveInterval, 0);
            }
            autoSaveTimer.Stop();

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
                    FileOpenPicker filePicker = new();
                    //Make folder Picker work in Win32
                    WinRT.Interop.InitializeWithWindow.Initialize(filePicker, GlobalData.WindowHandle);
                    filePicker.CommitButtonText = "Project Folder";
                    PreferencesModel prefs = GlobalData.Preferences;
                    //TODO: Use preferences project folder instead of DocumentsLibrary
                    //except you can't. Thanks, UWP.
                    filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    filePicker.FileTypeFilter.Add(".stbx");
                    StoryModel.ProjectFile = await filePicker.PickSingleFileAsync();
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
                    Logger.Log(LogLevel.Info,"Open File command cancelled (StoryModel.ProjectFile was null)");
                    Messenger.Send(new StatusChangedMessage(new($"Open Story command cancelled", LogLevel.Info)));
                    _canExecuteCommands = true;  // unblock other commands
                    return;
                }

                Ioc.Default.GetService<BackupService>().StopTimedBackup();
                //NOTE: BasicProperties.DateModified can be the date last changed

                StoryReader rdr = Ioc.Default.GetService<StoryReader>();
                StoryModel = await rdr.ReadFile(StoryModel.ProjectFile);

                if (GlobalData.Preferences.BackupOnOpen) { await Ioc.Default.GetService<BackupService>().BackupProject(); }
               
                if (StoryModel.ExplorerView.Count > 0)
                {
                    SetCurrentView(StoryViewType.ExplorerView);
                    Messenger.Send(new StatusChangedMessage(new($"Open Story completed", LogLevel.Info)));
                }
                GlobalData.MainWindow.Title = $"StoryBuilder - Editing {StoryModel.ProjectFilename.Replace(".stbx", "")}";
                new UnifiedVM().UpdateRecents(Path.Combine(StoryModel.ProjectFolder.Path,StoryModel.ProjectFile.Name)); 
                if (GlobalData.Preferences.TimedBackup) { Ioc.Default.GetService<BackupService>().StartTimedBackup(); }

                ShowHomePage();
                if (GlobalData.Preferences.AutoSave) { autoSaveTimer.Start(); }
                string msg = $"Opened project {StoryModel.ProjectFilename}";
                Logger.Log(LogLevel.Info, msg);
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Error in OpenFile command");
                Messenger.Send(new StatusChangedMessage(new($"Open Story command failed", LogLevel.Error)));
            }

            Logger.Log(LogLevel.Info, "Open Story completed.");
            _canExecuteCommands = true;
        }
        public async Task SaveFile()
        {
            if (DataSource.Count == 0 || DataSource == null)
            {
                Messenger.Send(new StatusChangedMessage(new($"You need to open a story first!", LogLevel.Info,false)));
                Logger.Log(LogLevel.Info, "SaveFile command cancelled (DataSource was null or empty)");
                return;
            }
            Logger.Log(LogLevel.Trace, "Saving file");
            try //Updating the lost modified timer
            {
                (StoryModel.StoryElements.StoryElementGuids[DataSource[0].Uuid] as OverviewModel).DateModified = DateTime.Now.ToString("d");
            }
            catch (NullReferenceException) { Messenger.Send(new StatusChangedMessage(new($"Failed to update Last Modified date", LogLevel.Warn))); } //This appears to happen when in narrative view but im not sure how to fix it.

            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Executing SaveFile command");
            try
            {
                //TODO: SaveFile is both an AppButton command and called from NewFile and OpenFile. Split these.
                Messenger.Send(new StatusChangedMessage(new($"Save File command executing", LogLevel.Info)));
                SaveModel();
                await WriteModel();
                Messenger.Send(new StatusChangedMessage(new($"Save File command completed", LogLevel.Info)));
                StoryModel.Changed = false;
                ChangeStatusColor = Colors.Green;
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Exception in SaveFile");
                Messenger.Send(new StatusChangedMessage(new($"Save File failed", LogLevel.Error)));
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
        /// <returns>Task (async function)</returns>
        private async Task WriteModel()
        {
            Logger.Log(LogLevel.Info, $"In WriteModel, file={StoryModel.ProjectFilename}");
            try
            {
                await CreateProjectFile();
                StorageFile file = StoryModel.ProjectFile;
                if (file != null)
                {
                    StoryWriter wtr = Ioc.Default.GetService<StoryWriter>();
                    //TODO: WriteFile isn't working; file is empty
                    await wtr.WriteFile(StoryModel.ProjectFile, StoryModel);
                    // Prevent updates to the remote version of the file until
                    // we finish making changes and call CompleteUpdatesAsync.
                    CachedFileManager.DeferUpdates(file);
                    await CachedFileManager.CompleteUpdatesAsync(file);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Error writing file");
                Messenger.Send(new StatusChangedMessage(new($"Error writing file - see log", LogLevel.Error)));
                return;
            }
            Logger.Log(LogLevel.Info, "WriteModel successful");
        }


        private async void SaveFileAs()
        {
            _canExecuteCommands = false;
            Messenger.Send(new StatusChangedMessage(new($"Save File As command executing", LogLevel.Info,true)));
            try
            {
                //Creates the content diolouge
                ContentDialog SaveAsDialog = new();
                SaveAsDialog.Title = "Save as";
                SaveAsDialog.XamlRoot = GlobalData.XamlRoot;
                SaveAsDialog.PrimaryButtonText = "Save";
                SaveAsDialog.SecondaryButtonText = "Cancel";
                SaveAsDialog.Content = new SaveAsDialog();
  
                //Sets needed data in VM and then shows the dialog
                SaveAsViewModel SaveAsVM = Ioc.Default.GetService<SaveAsViewModel>();
                // The default project name and project folder path are from the active StoryModel
                SaveAsVM.ProjectName = StoryModel.ProjectFilename;
                SaveAsVM.ProjectPathName = StoryModel.ProjectPath;

                ContentDialogResult result = await SaveAsDialog.ShowAsync();

                if (result == ContentDialogResult.Primary) //If save is clicked
                {
                    if (await VerifyReplaceOrCreate())
                    {
                        //Saves model to disk
                        SaveModel(); 
                        await WriteModel();

                        //Saves the current project folders and files to disk
                        await StoryModel.ProjectFile.CopyAsync(SaveAsVM.ParentFolder, SaveAsVM.ProjectName);

                        //Update the StoryModel properties to use the newly saved copy
                        StoryModel.ProjectFilename = SaveAsVM.ProjectName;
                        StoryModel.ProjectFolder = SaveAsVM.ParentFolder;
                        StoryModel.ProjectPath = SaveAsVM.SaveAsProjectFolderPath;
                        // Add to the recent files stack
                        GlobalData.MainWindow.Title = $"StoryBuilder - Editing {StoryModel.ProjectFilename.Replace(".stbx", "")}";
                        new UnifiedVM().UpdateRecents(Path.Combine(StoryModel.ProjectFolder.Path, StoryModel.ProjectFile.Name));
                        // Indicate everything's done
                        Messenger.Send(new IsChangedMessage(true));
                        StoryModel.Changed = false;
                        ChangeStatusColor = Colors.Green;
                        Messenger.Send(new StatusChangedMessage(new($"Save File As command completed", LogLevel.Info, true)));
                    }
                }
                else // if cancelled
                {
                    Messenger.Send(new StatusChangedMessage(new($"SaveAs dialog cancelled", LogLevel.Info, true)));
                }
            }
            catch (Exception ex) //If error occurs in file.
            {
                Logger.LogException(LogLevel.Error, ex, "Exception in SaveFileAs");
                Messenger.Send(new StatusChangedMessage(new($"Save File As failed", LogLevel.Info)));
            }
            _canExecuteCommands = true;
        }

        private async Task<bool> VerifyReplaceOrCreate()
        {
            Logger.Log(LogLevel.Trace, "VerifyReplaceOrCreated");

            SaveAsViewModel SaveAsVM = Ioc.Default.GetService<SaveAsViewModel>();
            SaveAsVM.SaveAsProjectFolderPath = SaveAsVM.ParentFolder.Path;
            if (File.Exists(Path.Combine(SaveAsVM.ProjectPathName,SaveAsVM.ProjectName)))
            {
                ContentDialog replaceDialog = new()
                {
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    Title = "Replace file",
                    Content = $"File {SaveAsVM.SaveAsProjectFolderPath} already exists. \n\nDo you want to replace it?",
                    XamlRoot = GlobalData.XamlRoot,
                };
                return await replaceDialog.ShowAsync() == ContentDialogResult.Primary;
            }
            else { return true; }
        }

        private async void CloseFile()
        {
            _canExecuteCommands = false;
            Messenger.Send(new StatusChangedMessage(new($"Closing project", LogLevel.Info, true)));
            autoSaveTimer.Stop();
            if (StoryModel.Changed)
            {
                ContentDialog Warning = new()
                {
                    Title = "Save changes?",
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    XamlRoot = GlobalData.XamlRoot
                };
                if (await Warning.ShowAsync() == ContentDialogResult.Primary)
                {
                    SaveModel();
                    await WriteModel();
                }
            }

            ResetModel();
            RightTappedNode = null; //Null right tapped node to prevent possible issues.
            SetCurrentView(StoryViewType.ExplorerView);
            GlobalData.MainWindow.Title = "StoryBuilder";
            Ioc.Default.GetService<BackupService>().StopTimedBackup();
            DataSource = StoryModel.ExplorerView;
            ShowHomePage();
            Messenger.Send(new StatusChangedMessage(new($"Close story command completed", LogLevel.Info, true)));
            _canExecuteCommands = true;
        }

        private void ResetModel()
        {
            StoryModel = new StoryModel();
            //TODO: Raise event for StoryModel change?
        }

        private async void ExitApp()
        {
            _canExecuteCommands = false;
            Messenger.Send(new StatusChangedMessage(new($"Executing Exit project command", LogLevel.Info, true)));

            if (StoryModel.Changed)
            {
                ContentDialog Warning = new()
                {
                    Title = "Save changes?",
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No",
                    XamlRoot = GlobalData.XamlRoot
                };
                if (await Warning.ShowAsync() == ContentDialogResult.Primary)
                {
                    SaveModel();
                    await WriteModel();
                }
            }
            Logger.Flush();
            Microsoft.UI.Xaml.Application.Current.Exit();  // Win32
        }

        private async Task CreateProjectFolder()
        {
            StorageFolder folder = await StoryModel.ProjectFolder.CreateFolderAsync(StoryModel.ProjectFilename);
            StoryModel.ProjectFolder = folder;
            StoryModel.ProjectPath = folder.Path;
        }

        private async Task CreateProjectFile()
        {
            StoryModel.ProjectFile = await StoryModel.ProjectFolder.CreateFileAsync(StoryModel.ProjectFilename, CreationCollisionOption.ReplaceExisting);
            //Story.ProjectFolder = await Story.ProjectFile.GetParentAsync();
        }

        #endregion

        #region Tool and Report Commands

        private async void Preferences()
        {
            Messenger.Send(new StatusChangedMessage(new($"Updating Preferences", LogLevel.Info, true)));

            //Creates and shows dialog
            ContentDialog PreferencesDialog = new();
            PreferencesDialog.XamlRoot = GlobalData.XamlRoot;
            PreferencesDialog.Content = new PreferencesDialog();
            PreferencesDialog.Title = "Preferences";
            PreferencesDialog.PrimaryButtonText = "Save";
            PreferencesDialog.CloseButtonText = "Cancel";

            ContentDialogResult result = await PreferencesDialog.ShowAsync();
            switch (result)
            {
                // Save changes
                case ContentDialogResult.Primary:
                    await Ioc.Default.GetService<PreferencesViewModel>().SaveAsync();
                    Messenger.Send(new StatusChangedMessage(new($"Preferences updated", LogLevel.Info, true)));
                    
                    break;
                //don't save changes
                default:
                    Messenger.Send(new StatusChangedMessage(new($"Preferences closed", LogLevel.Info, true)));
                    break;
            }

        }

        private async void KeyQuestionsTool()
        {
            Logger.Log(LogLevel.Info, "Displaying KeyQuestions tool dialog");
            if (RightTappedNode == null) { RightTappedNode = CurrentNode;}
            
            //Creates and shows dialog
            ContentDialog KeyQuestionsDialog = new();
            KeyQuestionsDialog.Title = "Key questions";
            KeyQuestionsDialog.CloseButtonText = "Close";
            KeyQuestionsDialog.XamlRoot = GlobalData.XamlRoot;
            KeyQuestionsDialog.Content = new KeyQuestionsDialog();
            await KeyQuestionsDialog.ShowAsync();

            Ioc.Default.GetService<KeyQuestionsViewModel>().NextQuestion();

            Logger.Log(LogLevel.Info, "KeyQuestions finished");
        }

        private async void TopicsTool()
        {
            Logger.Log(LogLevel.Info, "Displaying Topics tool dialog");
            if (RightTappedNode == null) { RightTappedNode = CurrentNode;}
            
            ContentDialog dialog = new();
            dialog.XamlRoot = GlobalData.XamlRoot;
            dialog.Title = "Topic Information";
            dialog.CloseButtonText = "Done";
            dialog.Content = new TopicsDialog();
            await dialog.ShowAsync();
            Logger.Log(LogLevel.Info, "Topics finished");
        }

        /// <summary>
        /// This shows the master plot dialog
        /// </summary>
        private async void MasterPlotTool()
        {
            Logger.Log(LogLevel.Info, "Displaying MasterPlot tool dialog");
            if (RightTappedNode == null)  { RightTappedNode = CurrentNode; }
            
            //Creates and shows content dialog
            ContentDialog dialog = new();
            dialog.XamlRoot = GlobalData.XamlRoot;
            dialog.Title = "Master plots";
            dialog.PrimaryButtonText = "Copy";
            dialog.SecondaryButtonText = "Cancel";
            dialog.Content = new MasterPlotsDialog();
            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)   // Copy command
            {
                string masterPlotName = Ioc.Default.GetService<MasterPlotsViewModel>().MasterPlotName;
                MasterPlotModel model = Ioc.Default.GetService<MasterPlotsViewModel >().MasterPlots[masterPlotName];
                IList<MasterPlotScene> scenes = model.MasterPlotScenes;
                foreach (MasterPlotScene scene in scenes)
                {
                    SceneModel child = new SceneModel(StoryModel);
                    child.Name = scene.SceneTitle;
                    child.Remarks = "See Notes.";
                    child.Notes = scene.Notes;

                    if (RightTappedNode == null)
                    {
                        Messenger.Send(new StatusChangedMessage(new($"You need to right click a node to", LogLevel.Info)));
                        return;
                    }

                    // add the new SceneModel & node to the end of the target's children 
                    StoryNodeItem newNode = new(child, RightTappedNode);
                    RightTappedNode.IsExpanded = true;
                    newNode.IsSelected = true;
                }
                Messenger.Send(new StatusChangedMessage(new($"MasterPlot {masterPlotName} inserted", LogLevel.Info, true)));
                ShowChange();
                Logger.Log(LogLevel.Info, "MasterPlot complete");
            }
            else  // canceled
            {
                Messenger.Send(new StatusChangedMessage(new($"MasterPlot cancelled", LogLevel.Info, true)));
            } 
        }

        private async void DramaticSituationsTool()
        {
            Logger.Log(LogLevel.Info, "Dislaying Dramatic Situations tool dialog");
            if (RightTappedNode == null) { RightTappedNode = CurrentNode; }

            //Creates and shows dialog
            ContentDialog dialog = new();
            dialog.XamlRoot = GlobalData.XamlRoot;
            dialog.Title = "Dramatic situations";
            dialog.PrimaryButtonText = "Copy as problem";
            dialog.SecondaryButtonText = "Copy as scene";
            dialog.CloseButtonText = "Cancel";
            dialog.Content = new DramaticSituationsDialog();
            ContentDialogResult result = await dialog.ShowAsync();

            DramaticSituationModel situationModel = Ioc.Default.GetService<DramaticSituationsViewModel>().Situation;
            StoryNodeItem newNode = null;
            string msg;
            if (RightTappedNode == null)
            {
                msg = $"Right tap a node to add this node to.";
                Messenger.Send(new StatusChangedMessage(new StatusMessage(msg, LogLevel.Warn, false)));
                return;
            }
            switch (result)
            {
                case ContentDialogResult.Primary:
                {
                    ProblemModel problem = new(StoryModel);
                    problem.Name = situationModel.SituationName;
                    problem.StoryQuestion = "See Notes.";
                    problem.Notes = situationModel.Notes;

                    // Insert the new Problem as the target's child
                    newNode = new StoryNodeItem(problem, RightTappedNode);
                    msg = $"Problem {situationModel.SituationName} inserted";
                    ShowChange();
                    break;
                }
                case ContentDialogResult.Secondary:
                {
                    SceneModel sceneVar = new SceneModel(StoryModel);
                    sceneVar.Name = situationModel.SituationName;
                    sceneVar.Remarks = "See Notes.";
                    sceneVar.Notes = situationModel.Notes;
                    // Insert the new Scene as the target's child
                    newNode = new StoryNodeItem(sceneVar, RightTappedNode);
                    msg = $"Scene {situationModel.SituationName} inserted";
                    ShowChange();
                    break;
                }
                default:
                    msg = "Dratatic Situation tool cancelled";
                    break;
            }
            Logger.Log(LogLevel.Info, msg);
            Messenger.Send(new StatusChangedMessage(new(msg, LogLevel.Info, true)));

            Logger.Log(LogLevel.Info, "Dramatic Situations finished");
        }

        /// <summary>
        /// This loads the stock scenes dialog in the Plotting Aids submenu
        /// </summary>
        private async void StockScenesTool()
        {
            Logger.Log(LogLevel.Info, "Displaying Stock Scenes tool dialog");
            if (RightTappedNode == null) {RightTappedNode = CurrentNode;}
            try
            {
                //Creates and shows dialog
                ContentDialog dialog = new();
                dialog.Title = "Stock scenes";
                dialog.Content = new StockScenesDialog();
                dialog.PrimaryButtonText = "Add Scene";
                dialog.CloseButtonText = "Cancel";
                dialog.XamlRoot = GlobalData.XamlRoot;
                ContentDialogResult result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)   // Copy command
                {
                    if (string.IsNullOrWhiteSpace(Ioc.Default.GetService<StockScenesViewModel>().SceneName))
                    {
                        Messenger.Send(new StatusChangedMessage(new($"You need to select a stock scene", LogLevel.Warn)));
                        return;
                    }

                    SceneModel sceneVar = new SceneModel(StoryModel); 
                    sceneVar.Name = Ioc.Default.GetService<StockScenesViewModel>().SceneName;
                    StoryNodeItem newNode = new(sceneVar, RightTappedNode);
                    _sourceChildren = RightTappedNode.Children;
                    TreeViewNodeClicked(newNode);
                    RightTappedNode.IsExpanded = true;
                    newNode.IsSelected = true;
                    Messenger.Send(new StatusChangedMessage(new("Stock Scenes inserted", LogLevel.Info)));
                }
                else 
                {
                    Messenger.Send(new StatusChangedMessage(new("Stock Scenes canceled", LogLevel.Warn)));
                }
            }
            catch (Exception e) {  Logger.LogException(LogLevel.Error, e, e.Message); }
        }

        private async void GeneratePrintReports()
        {
            if (Ioc.Default.GetRequiredService<ShellViewModel>().DataSource == null) 
            {
                Messenger.Send(new StatusChangedMessage(new($"You need to load a Story first!", LogLevel.Warn)));
                return; 
            }
            _canExecuteCommands = false;
            Messenger.Send(new StatusChangedMessage(new($"Generate Print Reports executing", LogLevel.Info, true)));

            SaveModel();

            // Run reports dialog
            ContentDialog ReportDialog = new();
            ReportDialog.Title = "Generate Reports";
            ReportDialog.PrimaryButtonText = "Generate";
            ReportDialog.CloseButtonText = "Cancel";
            ReportDialog.XamlRoot = GlobalData.XamlRoot;
            ReportDialog.Content = new PrintReportsDialog();
            var result = await ReportDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                PrintReportDialogVM ReportVM = Ioc.Default.GetRequiredService<PrintReportDialogVM>();

                PrintReports rpt = new(ReportVM, StoryModel);
                await rpt.Generate();
            }
            else
            {
                Messenger.Send(new StatusChangedMessage(new($"Generate Print Reports canceled", LogLevel.Info, true)));
            }
            _canExecuteCommands = true;
        }

        private async void GenerateScrivenerReports()
        {
            _canExecuteCommands = false;
            Messenger.Send(new StatusChangedMessage(new($"Generate Scrivener Reports executing", LogLevel.Info, true)));
            SaveModel();

            // Select the Scrivener .scrivx file to add the report to
            FileOpenPicker openPicker = new();
            if (Window.Current == null)
            {
                IntPtr hwnd = GetActiveWindow();
                IInitializeWithWindow initializeWithWindow = openPicker.As<IInitializeWithWindow>();
                initializeWithWindow.Initialize(hwnd);
            }
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".scrivx");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                Scrivener.ScrivenerFile = file;
                Scrivener.ProjectPath = Path.GetDirectoryName(file.Path);
                if (!await Scrivener.IsScrivenerRelease3())
                    throw new ApplicationException("Project is not Scrivener Release 3");
                // Load the Scrivener project file's model
                ScrivenerReports rpt = new ScrivenerReports(file, StoryModel);
                await rpt.GenerateReports();
            }

            Messenger.Send(new StatusChangedMessage(new($"Generate Scrivener reports completed", LogLevel.Info, true)));
            _canExecuteCommands = true;
        }

        private void LaunchGitHubPages()
        {
            _canExecuteCommands = false;
            Messenger.Send(new StatusChangedMessage(new($"Launching GitHub Pages User Manual", LogLevel.Info, true)));

            string url = @"https://storybuilder-org.github.io/StoryBuilder-2/";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);

            Messenger.Send(new StatusChangedMessage(new($"Launch default browser completed", LogLevel.Info, true)));

            _canExecuteCommands = true;
        }

        #endregion  

        #region Move TreeViewItem Commands

        private void MoveTreeViewItemLeft()
        {
            if (CurrentNode == null)
            {
                Messenger.Send(new StatusChangedMessage(new($"Click or touch a node to move", LogLevel.Info)));
                return;
            }

            if (CurrentNode.Parent != null && CurrentNode.Parent.IsRoot)
            {
                Messenger.Send(new StatusChangedMessage(new($"Cannot move further left", LogLevel.Info)));
                return;
            }


            if (!MoveIsValid()) // Verify message
            {
                Messenger.Send(new StatusChangedMessage(new(MoveErrorMesage, LogLevel.Info)));
                return;
            }

            if (CurrentNode.Parent != null)
            {
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

                Logger.Log(LogLevel.Info, $"Moving {CurrentNode.Name} left to parent {CurrentNode.Parent.Name}");
            }
            else
            {
                Messenger.Send(new StatusChangedMessage(new($"Cannot move root node.", LogLevel.Info)));
                return;
            }
        }

        private void MoveTreeViewItemRight()
        {
            //TODO: Logging
            StoryNodeItem targetParent;

            if (CurrentNode == null)
            {
                Messenger.Send(new StatusChangedMessage(new($"Click or touch a node to move", LogLevel.Info)));
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
                Messenger.Send(new StatusChangedMessage(new($"Cannot move root node.", LogLevel.Info)));
                return;
            }


            if (_sourceIndex > 0) // not first child, new parent will be previous sibling
            {
                targetParent = CurrentNode.Parent.Children[_sourceIndex - 1];
                _targetCollection = targetParent.Children;
                _targetIndex = _targetCollection.Count;
            }
            else
            {
                // find parent's predecessor
                if (CurrentNode.Parent.Parent == null)
                {
                    Messenger.Send(new StatusChangedMessage(new($"Cannot move further right", LogLevel.Info)));
                    return;
                }

                ObservableCollection<StoryNodeItem> grandparentCollection = CurrentNode.Parent.Parent.Children;
                int siblingIndex = grandparentCollection.IndexOf(CurrentNode.Parent) - 1;
                if (siblingIndex >= 0)
                {
                    targetParent = grandparentCollection[siblingIndex];
                    _targetCollection = targetParent.Children;
                    if (_targetCollection.Count > 0)
                    {
                        targetParent = _targetCollection[^1];
                        _targetCollection = targetParent.Children;
                        _targetIndex = _targetCollection.Count;
                    }
                    else
                    {
                        Messenger.Send(new StatusChangedMessage(new($"Cannot move further right", LogLevel.Warn)));
                        return;
                    }
                }
                else
                {
                    Messenger.Send(new StatusChangedMessage(new($"Cannot move further right", LogLevel.Warn)));
                    return;
                }
            }

            if (MoveIsValid()) // Verify move
            {
                _sourceChildren.RemoveAt(_sourceIndex);
                if (_targetIndex == -1) { _targetCollection.Add(CurrentNode); }
                else
                    _targetCollection.Insert(_targetIndex, CurrentNode);
                CurrentNode.Parent = targetParent;
                Logger.Log(LogLevel.Info, $"Moving {CurrentNode.Name} right to parent {CurrentNode.Parent.Name}");
            }
        }

        private void MoveTreeViewItemUp()
        {
            if (CurrentNode == null)
            {
                Messenger.Send(new StatusChangedMessage(new($"Click or touch a node to move", LogLevel.Info)));
                return;
            }

            if (CurrentNode.IsRoot)
            {
                Messenger.Send(new StatusChangedMessage(new($"Cannot move up further", LogLevel.Warn)));
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
                    Messenger.Send(new StatusChangedMessage(new($"Cannot move up further", LogLevel.Warn)));
                    return;
                }
                // find parent's predecessor
                ObservableCollection<StoryNodeItem> grandparentCollection = CurrentNode.Parent.Parent.Children;
                int siblingIndex = grandparentCollection.IndexOf(CurrentNode.Parent) - 1;
                if (siblingIndex >= 0)
                {
                    _targetCollection = grandparentCollection[siblingIndex].Children;
                    _targetParent = grandparentCollection[siblingIndex];
                }
                else
                {
                    Messenger.Send(new StatusChangedMessage(new($"Cannot move up further", LogLevel.Warn)));
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
                Logger.Log(LogLevel.Info, $"Moving {CurrentNode.Name} up to parent {CurrentNode.Parent.Name}");
            }
        }

        private void MoveTreeViewItemDown()
        {
            if (CurrentNode == null)
            {
                Messenger.Send(new StatusChangedMessage(new($"Click or touch a node to move", LogLevel.Info)));
                return;
            }
            if (CurrentNode.IsRoot)
            {
                Messenger.Send(new StatusChangedMessage(new($"Cannot move a root node", LogLevel.Info)));
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
                    Messenger.Send(new StatusChangedMessage(new($"Cannot move down further", LogLevel.Warn)));
                    return;
                }
                // find parent's successor
                ObservableCollection<StoryNodeItem> grandparentCollection = CurrentNode.Parent.Parent.Children;
                int siblingIndex = grandparentCollection.IndexOf(CurrentNode.Parent) + 1;
                if (siblingIndex == grandparentCollection.Count)
                {
                    CurrentNode.Parent = DataSource[1];
                    _sourceChildren.RemoveAt(_sourceIndex);
                    DataSource[1].Children.Insert(_targetIndex, CurrentNode);
                    Messenger.Send(new StatusChangedMessage(new($"Moved to trash", LogLevel.Info)));

                    return;
                }
                if (grandparentCollection[siblingIndex].IsRoot)
                {
                    Messenger.Send(new StatusChangedMessage(new($"Cannot move down further", LogLevel.Warn)));
                    return;
                }
                _targetCollection = grandparentCollection[siblingIndex].Children;
                _targetParent = grandparentCollection[siblingIndex];
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
            string msg = $"Adding StoryElement {typeToAdd}";
            Logger.Log(LogLevel.Info, msg);
            if (RightTappedNode == null)
            {
                Messenger.Send(new StatusChangedMessage(new($"Right tap a node to add to", LogLevel.Warn)));
                Logger.Log(LogLevel.Info, "Add StoryElement failed- node not selected");
                _canExecuteCommands = true;
                return null;
            }

            if (RootNodeType(RightTappedNode) == StoryItemType.TrashCan)
            {
                Messenger.Send(new StatusChangedMessage(new($"You can't add to Deleted Items", LogLevel.Warn)));
                Logger.Log(LogLevel.Info, "Add StoryElement failed- can't add to TrashCan");
                _canExecuteCommands = true;
                return null;
            }

            StoryNodeItem NewNode = null;
            switch (typeToAdd)
            {
                case StoryItemType.Folder:
                    NewNode = new StoryNodeItem(new FolderModel(StoryModel), RightTappedNode);
                    break;
                case StoryItemType.Section:
                    NewNode = new StoryNodeItem(new SectionModel(StoryModel), RightTappedNode);
                    break;
                case StoryItemType.Problem:
                    NewNode = new StoryNodeItem(new ProblemModel(StoryModel), RightTappedNode);
                    break;
                case StoryItemType.Character:
                    NewNode = new StoryNodeItem(new CharacterModel(StoryModel), RightTappedNode);
                    break;
                case StoryItemType.Setting:
                    NewNode = new StoryNodeItem(new SettingModel(StoryModel), RightTappedNode);
                    break;
                case StoryItemType.Scene:
                    NewNode = new StoryNodeItem(new SceneModel(StoryModel), RightTappedNode);
                    break;
            }
            
            if (NewNode != null)
            {
                NewNode.Parent.IsExpanded = true;
                NewNode.IsRoot = false; //Only an overview node can be a root, which cant be created normally
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

            List<StoryNodeItem> FoundNodes = new();
            foreach (StoryNodeItem node in DataSource[0]) //Gets all nodes in the tree
            {
                if (Ioc.Default.GetRequiredService<DeletionService>().SearchStoryElement(node, RightTappedNode.Uuid, StoryModel, false))
                {
                    FoundNodes.Add(node);
                }
            }

            bool delete = true;
            //Only warns if it finds a node its referenced in
            if (FoundNodes.Count >= 1)
            {
                //Creates UI
                StackPanel Content = new();
                Content.Children.Add(new TextBlock() { Text = "The following nodes will be updated to remove references to this node:" });
                Content.Children.Add(new ListView() { ItemsSource = FoundNodes, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Height = 300, Width = 480 });

                //Creates dialog and then shows it
                ContentDialog _contentDialog = new();
                _contentDialog.XamlRoot = GlobalData.XamlRoot;
                _contentDialog.Content = Content;
                _contentDialog.Title = "Are you sure you want to delete this node?";
                _contentDialog.Width = 500;
                _contentDialog.PrimaryButtonText = "Confirm";
                _contentDialog.SecondaryButtonText = "Cancel";
                var result = await _contentDialog.ShowAsync();

                if (result == ContentDialogResult.Secondary) { delete = false; }
            }


            if (delete)
            {
                foreach (StoryNodeItem node in FoundNodes)
                {
                    Ioc.Default.GetRequiredService<DeletionService>().SearchStoryElement(node, RightTappedNode.Uuid, StoryModel, true);
                }
                ObservableCollection<StoryNodeItem> source = RightTappedNode.Parent.Children;
                source.Remove(RightTappedNode);
                DataSource[1].Children.Add(RightTappedNode);
                RightTappedNode.Parent = DataSource[1];
                Messenger.Send(new StatusChangedMessage(new($"Deleted node {RightTappedNode.Name}", LogLevel.Info, true)));
            }
        }

        private void RestoreStoryElement()
        {
             Logger.Log(LogLevel.Trace, "RestoreStoryElement");
            if (RightTappedNode == null)
            {
                Messenger.Send(new StatusChangedMessage(new($"Right tap a node to restore", LogLevel.Warn)));
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
            ObservableCollection<StoryNodeItem> target = DataSource[0].Children;
            DataSource[1].Children.Remove(RightTappedNode);
            target.Add(RightTappedNode);
            RightTappedNode.Parent = DataSource[0];
            Messenger.Send(new StatusChangedMessage(new($"Restored node {RightTappedNode.Name}", LogLevel.Info, true)));
        }

        /// <summary>
        /// Add a Scene StoryNodeItem to the end of the Narrative view
        /// by copying from the Scene's StoryNodeItem in the Explorer
        /// view.
        /// </summary>
        private void CopyToNarrative()
        {
            Logger.Log(LogLevel.Trace, "CopyToNarrative");
            if (RightTappedNode == null)
            {
                Messenger.Send(new StatusChangedMessage(new($"Select a node to copy", LogLevel.Info)));
                return;
            }
            if (RightTappedNode.Type != StoryItemType.Scene)
            {
                Messenger.Send(new StatusChangedMessage(new($"You can only copy a scene", LogLevel.Warn)));
                return;
            }

            SceneModel sceneVar = (SceneModel) StoryModel.StoryElements.StoryElementGuids[RightTappedNode.Uuid];
            _ = new StoryNodeItem(sceneVar, StoryModel.NarratorView[0]);
            Messenger.Send(new StatusChangedMessage(new($"Copied node {RightTappedNode.Name} to Narrative View", LogLevel.Info, true)));
        }

        /// <summary>
        /// Clears trash
        /// </summary>
        private void EmptyTrash()
        {
            if (DataSource == null)
            {
                Messenger.Send(new StatusChangedMessage(new($"You need to load a story first!", LogLevel.Warn, false)));
                Logger.Log(LogLevel.Info, "Failed to empty trash as DataSource is null. (Is a story loaded?)");
                return;
            }
            
            StatusMessage = "Trash Emptied.";
            Logger.Log(LogLevel.Info,"Emptied Trash.");
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
                Messenger.Send(new StatusChangedMessage(new($"Select a node to remove", LogLevel.Info)));
                return;
            }
            if (RightTappedNode.Type != StoryItemType.Scene)
            {
                Messenger.Send(new StatusChangedMessage(new($"You can only remove a Scene copy", LogLevel.Info)));
                return;
            }

            foreach (StoryNodeItem item in StoryModel.NarratorView[0].Children.ToList())
            {
                if (item.Uuid == RightTappedNode.Uuid)
                {
                    StoryModel.NarratorView[0].Children.Remove(item);
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
            StoryNodeItem node = startNode;
            while (!node.IsRoot)
                node = node.Parent;
            return node.Type;
        }

        #endregion

        private string _moveErrorMessage;
        public string MoveErrorMesage
        {
            get => _moveErrorMessage;
            set => SetProperty(ref _moveErrorMessage, value);
        }

        private bool MoveIsValid()
        {
            MoveErrorMesage = string.Empty;
            //TODO: Complete stubbed MoveIsValid method
            return true;
        }

        public void ViewChanged()
        {
            if (DataSource == null || DataSource.Count == 0)
            {
                Messenger.Send(new StatusChangedMessage(new($"You need to load a story first!", LogLevel.Warn, false)));
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
        /// is tapped and which view (Explorer or Navigator) is selected.
        /// </summary>
        public void ShowFlyoutButtons()
        {
            switch (RootNodeType(RightTappedNode))
            {
                case StoryItemType.StoryOverview:   // Explorer tree
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
                case StoryItemType.Section:         // Narrator tree
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
            StatusMessage msg;
            if (!GlobalData.DopplerConnection | !GlobalData.ElmahLogging)
                msg = new StatusMessage("Connection not established", LogLevel.Warn, true);
            else
                msg = new StatusMessage("Connection established", LogLevel.Info, true);
            Messenger.Send(new StatusChangedMessage(msg));
        }

        private void SetCurrentView(StoryViewType view)
        {
            switch (view)
            {
                case StoryViewType.ExplorerView:
                    DataSource = StoryModel.ExplorerView;
                    break;
                case StoryViewType.NarratorView:
                    DataSource = StoryModel.NarratorView;
                    break;
                case StoryViewType.SearchView:
                    break;
            }
            if (DataSource.Count > 0) {CurrentNode = DataSource[0];}
        }

        #region MVVM ` processing
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
        /// <param name="Level"></param>
        /// <param name="Message"></param>
        public void ShowMessage(LogLevel Level, string Message,bool SendToLog)
        {
            Messenger.Send(new StatusChangedMessage(new(Message, Level, SendToLog)));
        }

        /// <summary>
        /// This displays a status message and starts a timer for it to be cleared (If Warning or Info.)
        /// </summary>
        /// <param name="statusMessage"></param>
        private void StatusMessageReceived(StatusChangedMessage statusMessage)
        {
            if (statusTimer.IsEnabled) { statusTimer.Stop(); } //Stops a timer if one is already running

            StatusMessage = statusMessage.Value.Status; //This shows the message

            switch (statusMessage.Value.Level)
            {
                case LogLevel.Info:
                    StatusColor = GlobalData.Preferences.SecondaryColor;
                    statusTimer.Interval = new TimeSpan(0, 0, 15);  // Timer will tick in 15 seconds
                    statusTimer.Start();
                    break;
                case LogLevel.Warn:
                    StatusColor = new SolidColorBrush(Colors.Yellow);
                    statusTimer.Interval = new TimeSpan(0, 0, 30); // Timer will tick in 30 seconds
                    statusTimer.Start();
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
            statusTimer.Stop();
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
            NameChangeMessage msg = name.Value;
            CurrentNode.Name = msg.NewName;
            switch (CurrentNode.Type)
            {
                case StoryItemType.Character:
                    //int charIndex = CharacterModel.CharacterNames.IndexOf(msg.OldName);
                    //CharacterModel.CharacterNames[charIndex] = msg.NewName;
                    break;
                case StoryItemType.Setting:
                    int settingIndex = SettingModel.SettingNames.IndexOf(msg.OldName);
                    SettingModel.SettingNames[settingIndex] = msg.NewName;
                    break;
            }
        }

        #endregion

        #region Constructor(s)

        public ShellViewModel()
        {

            //_itemSelector = Ioc.Default.GetService<TreeViewSelection>();

            Messenger.Register<IsChangedRequestMessage>(this, (r, m) => { m.Reply(StoryModel.Changed); });
            Messenger.Register<ShellViewModel, IsChangedMessage>(this, static (r, m) => r.IsChangedMessageReceived(m));
            Messenger.Register<ShellViewModel, StatusChangedMessage>(this, static (r, m) => r.StatusMessageReceived(m));
            Messenger.Register<ShellViewModel, NameChangedMessage>(this, static (r, m) => r.NameMessageReceived(m));

            //Preferences = Ioc.Default.GetService<Preferences>();
            Scrivener = Ioc.Default.GetService<ScrivenerIo>();
            Logger = Ioc.Default.GetService<LogService>();
            Search = Ioc.Default.GetService<SearchService>();

            Title = "Hello Terry";
            StoryModel = new StoryModel();

            statusTimer = new DispatcherTimer();
            statusTimer.Tick += statusTimer_Tick;

            Messenger.Send(new StatusChangedMessage(new($"Ready", LogLevel.Info)));

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
            ContentDialog dialog = new();
            dialog.XamlRoot = GlobalData.XamlRoot;
            dialog.Title = "Narrative Editor - EXPERIMENTAL";
            dialog.PrimaryButtonText = "Done";
            dialog.Width = 600;
            dialog.Content = new NarrativeTool();
            ContentDialogResult result = await dialog.ShowAsync();
        }

        public void SearchNodes()
        {
            _canExecuteCommands = false;    //This prevents other commands from being used till this one is complete.
            Logger.Log(LogLevel.Info, $"Search started, Searching for { FilterText }");
            SaveModel();
            if (DataSource == null || DataSource.Count == 0)
            {
                Logger.Log(LogLevel.Info, "Data source is null or Empty.");
                Messenger.Send(new StatusChangedMessage(new($"You need to load a story first!", LogLevel.Warn)));

                _canExecuteCommands = true;
                return;
            }

            int SearchTotal = 0;

            foreach (StoryNodeItem node in DataSource[0])
            {
                if (Search.SearchStoryElement(node, FilterText, StoryModel)) //checks if node name contains the thing we are looking for
                {
                    SearchTotal++;
                    if (Microsoft.UI.Xaml.Application.Current.RequestedTheme == ApplicationTheme.Light) { node.Background = new SolidColorBrush(Colors.LightGoldenrodYellow); }
                    else { node.Background = new SolidColorBrush(Colors.DarkGoldenrod); } //Light Goldenrod is hard to read in dark theme
                    node.IsExpanded = true;

                    StoryNodeItem parent = node.Parent;
                    if (parent != null)
                    {
                        while (!parent.IsRoot)
                        {
                            parent.IsExpanded = true;
                            parent = parent.Parent;
                        }

                        if (parent.IsRoot) { parent.IsExpanded = true; }
                    }
                }
                else { node.Background = null; }
            }

            switch (SearchTotal)
            {
                case 0:
                    Messenger.Send(new StatusChangedMessage(new("Found no matches", LogLevel.Info, true)));
                    break;
                case 1:
                    Messenger.Send(new StatusChangedMessage(new("Found 1 match", LogLevel.Info, true)));
                    break;
                default:
                    Messenger.Send(new StatusChangedMessage(new($"Found {SearchTotal} matches", LogLevel.Info, true)));
                    break;
            }
            _canExecuteCommands = true;    //Enables other commands from being used till this one is complete.
        }
        #endregion

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }

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
}