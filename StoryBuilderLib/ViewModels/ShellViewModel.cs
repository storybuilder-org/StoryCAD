using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Controllers;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Dialogs;
using StoryBuilder.Services.Dialogs.Tools;
using StoryBuilder.Services.Help;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.Services.Navigation;
using StoryBuilder.Services.Scrivener;
using StoryBuilder.Services.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT;
using GuidAttribute = System.Runtime.InteropServices.GuidAttribute;

namespace StoryBuilder.ViewModels
{
    public class ShellViewModel : ObservableRecipient
    {
        private bool _canExecuteCommands;

        public static string HomePage = "HomePage";
        public static string OverviewPage = "OverviewPage";
        public static string ProblemPage = "ProblemPage";
        public static string CharacterPage = "CharacterPage";
        public static string PlotPointPage = "PlotPointPage";
        public static string FolderPage = "FolderPage";
        public static string SectionPage = "SectionPage";
        public static string SettingPage = "SettingPage";
        public static string TrashCanPage = "TrashCanPage";

        // Navigation navigation landmark nodes
        public StoryNodeItem CurrentNode { get; set; }
        public StoryNodeItem RightTappedNode;
        public StoryNodeItem TrashCanNode;

        public StoryViewType ViewType;

        private int _sourceIndex;
        private ObservableCollection<StoryNodeItem> _sourceChildren;
        private int _targetIndex;
        private ObservableCollection<StoryNodeItem> _targetCollection;
        public readonly StoryController _story;
        public readonly LogService Logger;
        public readonly HelpService Help;
        public readonly SearchService Search;

        // The current story outline being processed. 
        public StoryModel StoryModel;

        public readonly ScrivenerIo Scrivener;
        private bool _saveAsProjectFolderExists;
        private StorageFolder _saveAsParentFolder;

        private string _saveAsProjectFolderPath;
        private StorageFolder _saveAsProjectFolder;

        // The right-hand (detail) side of ShellView
        public Frame SplitViewFrame;

        #region CommandBar Relay Commands

        // Open/Close Navigation pane (Hamburger menu)
        public RelayCommand TogglePaneCommand { get; }

        // File operation MenuFlyout commands
        public RelayCommand NewFileCommand { get; }
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
        //Open unified menu
        public RelayCommand OpenUnifiedCommand { get; }

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
        public RelayCommand ReportsCommand { get; }
        public RelayCommand PreferencesCommand { get; }

        // Filter command
        public RelayCommand FilterCommand { get; set; }

        #endregion

        #region Add Story Element CommandBarFlyOut Relay Commands

        // Add commands
        public RelayCommand AddFolderCommand { get; }
        public RelayCommand AddSectionCommand { get; }
        public RelayCommand AddProblemCommand { get; }
        public RelayCommand AddCharacterCommand { get; }
        public RelayCommand AddSettingCommand { get; }
        public RelayCommand AddPlotPointCommand { get; }

        // Remove command (move to trash)
        public RelayCommand RemoveStoryElementCommand { get; }
        public RelayCommand RestoreStoryElementCommand { get; }
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

        /// <summary>
        /// FilterIsChecked binds to the Filter AppBarToggleButton IsChecked property
        /// </summary>
        private bool _filterIsChecked;
        public bool FilterIsChecked
        {
            get => _filterIsChecked;
            set => SetProperty(ref _filterIsChecked, value);
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

        private Visibility _addPlotPointVisibility;
        public Visibility AddPlotPointVisibility
        {
            get => _addPlotPointVisibility;
            set => SetProperty(ref _addPlotPointVisibility, value);
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

        private TimeSpan _messageDuration;
        public TimeSpan MessageDuration 
        {
            get => _messageDuration;
            set => SetProperty(ref _messageDuration, value);
        }

        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set => SetProperty(ref _filterText, value);
        }

        private string _filterStatus;
        public string FilterStatus
        {
            get => _filterStatus;
            set => SetProperty(ref _filterStatus, value);
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

        private async void OpenUnifiedMenu()
        {
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Executing unified menu command");
            UnifiedMenu dialog = new();
            dialog.XamlRoot = GlobalData.XamlRoot;
            var result = await dialog.ShowAsync();
        }

        /// <summary>
        /// Used to open a file such an sample story or recent file
        /// </summary>
        public async Task OpenFileFromPath(string Path)
        {
            _story.ProjectFolder = await StorageFolder.GetFolderFromPathAsync(Path);
            _story.ProjectPath = _story.ProjectFolder.Path;
            _story.ProjectFilename = _story.ProjectFolder.DisplayName;

            IReadOnlyList<StorageFile> files = await _story.ProjectFolder.GetFilesAsync();
            StorageFile file = files[0];
            //NOTE: BasicProperties.DateModified can be the date last changed

            _story.ProjectFilename = file.Name;
            _story.ProjectFile = file;
            // Make sure files folder exists...
            _story.FilesFolder = await _story.ProjectFolder.GetFolderAsync("files");
            //TODO: Back up at the right place (after open?)
            await BackupProject();
            StoryReader rdr = Ioc.Default.GetService<StoryReader>();
            StoryModel = await rdr.ReadFile(file);
            if (StoryModel.ExplorerView.Count > 0)
            {
                SetCurrentView(StoryViewType.ExplorerView);
                _story.LoadStatus = LoadStatus.LoadFromRtfFiles;
                StatusMessage = "Open Story completed";
            }
            Ioc.Default.GetService<UnifiedVM>().UpdateRecents(_story.ProjectPath); //Updates path
            _canExecuteCommands = true;
        }

        public async Task UnifiedNewFile()
        {
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "UnifyVM - New File starting");
            try
            {
                //TODO: Make sure both path and filename are present
                UnifiedVM vm = Ioc.Default.GetService<UnifiedVM>();
                if (!Path.GetExtension(vm.ProjectName).Equals(".stbx"))
                    vm.ProjectName = vm.ProjectName + ".stbx";
                _story.ProjectFilename = vm.ProjectName;
                StorageFolder parent = await StorageFolder.GetFolderFromPathAsync(vm.ProjectPath);
                _story.ProjectFolder = await parent.CreateFolderAsync(vm.ProjectName);
                _story.ProjectPath = _story.ProjectFolder.Path;
                StatusMessage = "New project command executing";
                if (StoryModel.Changed)
                {
                    await SaveModel();
                    await WriteModel();
                }

                ResetModel();
                var overview = new OverviewModel("Working Title", StoryModel);
                overview.Author = GlobalData.Preferences.LicenseOwner;
                var overviewNode = new StoryNodeItem(overview, null)
                {
                    IsExpanded = true,
                    IsRoot = true
                };
                StoryModel.ExplorerView.Add(overviewNode);
                TrashCanModel trash = new TrashCanModel(StoryModel);
                StoryNodeItem trashNode = new StoryNodeItem(trash, null);
                StoryModel.ExplorerView.Add(trashNode);     // The trashcan is the second root
                var narrative = new SectionModel("Narrative View", StoryModel);
                var narrativeNode = new StoryNodeItem(narrative, null);
                narrativeNode.IsRoot = true;
                StoryModel.NarratorView.Add(narrativeNode);
                trash = new TrashCanModel(StoryModel);
                trashNode = new StoryNodeItem(trash, null);
                StoryModel.NarratorView.Add(trashNode);     // The trashcan is the second root
                                                            // Use the NewProjectDialog template to complete the model
                switch (vm.SelectedTemplate)
                {
                    case "Blank Project":
                        break;
                    case "Empty Folders":
                        StoryElement problems = new FolderModel("Problems", StoryModel);
                        StoryNodeItem problemsNode = new(problems, overviewNode);
                        StoryElement characters = new FolderModel("Characters", StoryModel);
                        StoryNodeItem charactersNode = new(characters, overviewNode);
                        StoryElement settings = new FolderModel("Settings", StoryModel);
                        StoryNodeItem settingsNode = new(settings, overviewNode);
                        StoryElement plotpoints = new FolderModel("Plot Points", StoryModel);
                        StoryNodeItem plotpointsNode = new(plotpoints, overviewNode);
                        break;
                    case "External/Internal Problems":
                        StoryElement externalProblem = new ProblemModel("External Problem", StoryModel);
                        StoryNodeItem externalProblemNode = new(externalProblem, overviewNode);
                        StoryElement internalProblem = new ProblemModel("Internal Problem", StoryModel);
                        StoryNodeItem internalProblemNode = new(internalProblem, overviewNode);
                        break;
                    case "Protagonist/Antagonist":
                        StoryElement protagonist = new CharacterModel("Protagonist", StoryModel);
                        StoryNodeItem protagonistNode = new(protagonist, overviewNode);
                        StoryElement antagonist = new CharacterModel("Antagonist", StoryModel);
                        StoryNodeItem antagonistNode = new(antagonist, overviewNode);
                        break;
                    case "Problems and Characters":
                        StoryElement problemsFolder = new FolderModel("Problems", StoryModel);
                        StoryNodeItem problemsFolderNode = new StoryNodeItem(problemsFolder, overviewNode)
                        {
                            IsExpanded = true
                        };
                        StoryElement charactersFolder = new FolderModel("Characters", StoryModel);
                        StoryNodeItem charactersFolderNode = new StoryNodeItem(charactersFolder, overviewNode);
                        charactersFolderNode.IsExpanded = true;
                        StoryElement settingsFolder = new FolderModel("Settings", StoryModel);
                        StoryNodeItem settingsFolderNode = new StoryNodeItem(settingsFolder, overviewNode);
                        StoryElement plotpointsFolder = new FolderModel("Plot Points", StoryModel);
                        StoryNodeItem plotpointsFolderNode = new StoryNodeItem(plotpointsFolder, overviewNode);
                        StoryElement externalProb = new ProblemModel("External Problem", StoryModel);
                        StoryNodeItem externalProbNode = new StoryNodeItem(externalProb, problemsFolderNode);
                        StoryElement internalProb = new ProblemModel("Internal Problem", StoryModel);
                        StoryNodeItem internalProbNode = new StoryNodeItem(internalProb, problemsFolderNode);
                        StoryElement protag = new CharacterModel("Protagonist", StoryModel);
                        StoryNodeItem protagNode = new StoryNodeItem(protag, charactersFolderNode);
                        StoryElement antag = new CharacterModel("Antagonist", StoryModel);
                        StoryNodeItem antagNode = new StoryNodeItem(antag, charactersFolderNode);
                        break;
                }
                SetCurrentView(StoryViewType.ExplorerView);
                //TODO: Set expand and isselected?

                // Save the new project
                await SaveFile();

                StatusMessage = "New project ready.";
                Logger.Log(LogLevel.Info, "Unity - NewFile command completed");
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Error creating new project");
                StatusMessage = "File make failure.";
            }
            _canExecuteCommands = true;
        }

        public void TreeViewNodeClicked(object selectedItem)
        {
            if (selectedItem is null)
            {
                Logger.Log(LogLevel.Info, "TreeViewNodeClicked for null node, event ignored");
                return;
            }
            Logger.Log(LogLevel.Info, string.Format("TreeViewNodeClicked for {0}",selectedItem.ToString()));

            try {
                var nav = Ioc.Default.GetService<NavigationService>();
                if (selectedItem is StoryNodeItem node)
                {
                    CurrentNode = node;
                    StoryElement element = StoryModel.StoryElements.StoryElementGuids[node.Uuid];
                    switch (node.Type)
                    {
                        case StoryItemType.Character:
                            nav.NavigateTo(SplitViewFrame, CharacterPage, element);
                            break;
                        case StoryItemType.PlotPoint:
                            nav.NavigateTo(SplitViewFrame, PlotPointPage, element);
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

        public void ShowHomePage()
        {
            Logger.Log(LogLevel.Info, "ShowHomePage");
    
            var nav = Ioc.Default.GetService<NavigationService>();
            nav.NavigateTo(SplitViewFrame, HomePage);
        }

        /// <summary>
        /// Process the MainWindow's Closed event.
        /// 
        /// The user has clicked the window's close button.
        /// Insure the file is saved before allowding the
        /// app to terminate.
        /// </summary>
        public void ProcessCloseButton()
        {
            //BUG: Process the close button
            //throw new NotImplementedException();
        }
        private void TogglePane()
        {
            Logger.Log(LogLevel.Trace, string.Format("TogglePane from {0} to {1}", IsPaneOpen, !IsPaneOpen));
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
        private async Task SaveModel()
        {
            if (SplitViewFrame.CurrentSourcePageType is null)
                return;
            Logger.Log(LogLevel.Trace, string.Format("SaveModel- Page type={0}",
                SplitViewFrame.CurrentSourcePageType.ToString()));
            if (SplitViewFrame.CurrentSourcePageType == null)
                return;
            switch (SplitViewFrame.CurrentSourcePageType.ToString())
            {
                case "StoryBuilder.Views.OverviewPage":
                    OverviewViewModel ovm = Ioc.Default.GetService<OverviewViewModel>();
                    await ovm.SaveModel();
                    break;
                case "StoryBuilder.Views.ProblemPage":
                    ProblemViewModel pvm = Ioc.Default.GetService<ProblemViewModel>();
                    await pvm.SaveModel();
                    break;
                case "StoryBuilder.Views.CharacterPage":
                    CharacterViewModel cvm = Ioc.Default.GetService<CharacterViewModel>();
                    await cvm.SaveModel();
                    break;
                case "StoryBuilder.Views.PlotPointPage":
                    PlotPointViewModel ppvm = Ioc.Default.GetService<PlotPointViewModel>();
                    await ppvm.SaveModel();
                    break;
                case "StoryBuilder.Views.FolderPage":
                    FolderViewModel sepvm = Ioc.Default.GetService<FolderViewModel>();
                    await sepvm.SaveModel();
                    break;
                case "StoryBuilder.Views.SectionPage":
                    SectionViewModel scvm = Ioc.Default.GetService<SectionViewModel>();
                    await scvm.SaveModel();
                    break;
                case "StoryBuilder.Views.SettingPage":
                    SettingViewModel svm = Ioc.Default.GetService<SettingViewModel>();
                    await svm.SaveModel();
                    break;
            }
        }

        
        private async void NewFile()
        {
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Executing NewFile command");
            NewProjectDialog dialog = new();
            dialog.XamlRoot = GlobalData.XamlRoot;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    //TODO: Make sure both path and filename are present
                    NewProjectViewModel vm = Ioc.Default.GetService<NewProjectViewModel>();
                    if (!Path.GetExtension(vm.ProjectName).Equals(".stbx"))
                        vm.ProjectName = vm.ProjectName + ".stbx";
                    _story.ProjectFilename = vm.ProjectName;
                    StorageFolder parent = await StorageFolder.GetFolderFromPathAsync(vm.ParentPathName);
                    _story.ProjectFolder = await parent.CreateFolderAsync(vm.ProjectName);
                    _story.ProjectPath = _story.ProjectFolder.Path;
                    StatusMessage = "New project command executing";
                    if (StoryModel.Changed)
                    {
                        await SaveModel();
                        await WriteModel();
                    }

                    ResetModel();
                    var overview = new OverviewModel("Working Title", StoryModel);
                    overview.Author = GlobalData.Preferences.LicenseOwner;
                    var overviewNode = new StoryNodeItem(overview, null)
                    {
                        IsExpanded = true,
                        IsRoot = true
                    };
                    StoryModel.ExplorerView.Add(overviewNode);
                    TrashCanModel trash = new TrashCanModel(StoryModel);
                    StoryNodeItem trashNode = new StoryNodeItem(trash, null);
                    StoryModel.ExplorerView.Add(trashNode);     // The trashcan is the second root
                    var narrative = new SectionModel("Narrative View", StoryModel);
                    var narrativeNode = new StoryNodeItem(narrative, null);
                    narrativeNode.IsRoot = true;
                    StoryModel.NarratorView.Add(narrativeNode);
                    trash = new TrashCanModel(StoryModel);
                    trashNode = new StoryNodeItem(trash, null);
                    StoryModel.NarratorView.Add(trashNode);     // The trashcan is the second root
                    // Use the NewProjectDialog template to complete the model
                    switch (vm.SelectedTemplate)
                    {
                        case "Blank Project":
                            break;
                        case "Empty Folders":
                            StoryElement problems = new FolderModel("Problems", StoryModel);
                            StoryNodeItem problemsNode = new(problems, overviewNode);
                            StoryElement characters = new FolderModel("Characters", StoryModel);
                            StoryNodeItem charactersNode = new(characters, overviewNode);
                            StoryElement settings = new FolderModel("Settings", StoryModel);
                            StoryNodeItem settingsNode = new(settings, overviewNode);
                            StoryElement plotpoints = new FolderModel("Plot Points", StoryModel);
                            StoryNodeItem plotpointsNode = new(plotpoints, overviewNode);
                            break;
                        case "External/Internal Problems":
                            StoryElement externalProblem = new ProblemModel("External Problem", StoryModel);
                            StoryNodeItem externalProblemNode = new(externalProblem, overviewNode);
                            StoryElement internalProblem = new ProblemModel("Internal Problem", StoryModel);
                            StoryNodeItem internalProblemNode = new(internalProblem, overviewNode);
                            break;
                        case "Protagonist/Antagonist":
                            StoryElement protagonist = new CharacterModel("Protagonist", StoryModel);
                            StoryNodeItem protagonistNode = new(protagonist, overviewNode);
                            StoryElement antagonist = new CharacterModel("Antagonist", StoryModel);
                            StoryNodeItem antagonistNode = new(antagonist, overviewNode);
                            break;
                        case "Problems and Characters":
                            StoryElement problemsFolder = new FolderModel("Problems", StoryModel);
                            StoryNodeItem problemsFolderNode = new StoryNodeItem(problemsFolder, overviewNode)
                            {
                                IsExpanded = true
                            };
                            StoryElement charactersFolder = new FolderModel("Characters", StoryModel);
                            StoryNodeItem charactersFolderNode = new StoryNodeItem(charactersFolder, overviewNode);
                            charactersFolderNode.IsExpanded = true;
                            StoryElement settingsFolder = new FolderModel("Settings", StoryModel);
                            StoryNodeItem settingsFolderNode = new StoryNodeItem(settingsFolder, overviewNode);
                            StoryElement plotpointsFolder = new FolderModel("Plot Points", StoryModel);
                            StoryNodeItem plotpointsFolderNode = new StoryNodeItem(plotpointsFolder, overviewNode);
                            StoryElement externalProb = new ProblemModel("External Problem", StoryModel);
                            StoryNodeItem externalProbNode = new StoryNodeItem(externalProb, problemsFolderNode);
                            StoryElement internalProb = new ProblemModel("Internal Problem", StoryModel);
                            StoryNodeItem internalProbNode = new StoryNodeItem(internalProb, problemsFolderNode);
                            StoryElement protag = new CharacterModel("Protagonist", StoryModel);
                            StoryNodeItem protagNode = new StoryNodeItem(protag, charactersFolderNode);
                            StoryElement antag = new CharacterModel("Antagonist", StoryModel);
                            StoryNodeItem antagNode = new StoryNodeItem(antag, charactersFolderNode);
                            break;
                    }
                    SetCurrentView(StoryViewType.ExplorerView);
                    //TODO: Set expand and isselected?

                    // Save the new project
                    await SaveFile();

                    StatusMessage = "New project ready.";
                    Logger.Log(LogLevel.Info, "NewFile command completed");
                }
                catch (Exception ex)
                {
                    Logger.LogException(LogLevel.Error, ex, "Error creating new project");
                    StatusMessage = "New Story command failed";
                }
            }
            else
            {
                StatusMessage = "New project command cancelled.";
                Logger.Log(LogLevel.Info, "NewFile command cancelled");
            }
            _canExecuteCommands = true;
        }

        public async void OpenFile()
        {
            if (StoryModel.Changed)
            {
                await SaveModel();
                await WriteModel();
            }
            //Logger.Log(LogLevel.Trace, "OpenFile");
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Executing OpenFile command");

            try
            {
                ResetModel();
                var folderPicker = new FolderPicker();
                //Make folder Picker works in Win32
                if (Window.Current == null)
                {
                    IntPtr hwnd = GetActiveWindow();
                    var initializeWithWindow = folderPicker.As<IInitializeWithWindow>();
                    initializeWithWindow.Initialize(hwnd);
                }
                folderPicker.CommitButtonText = "Project Folder";
                PreferencesModel prefs = GlobalData.Preferences;
                //TODO: Use preferences project folder instead of DocumentsLibrary
                //except you can't. Thanks, UWP.
                folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                folderPicker.FileTypeFilter.Add(".stbx");
                folderPicker.FileTypeFilter.Add(".stb");
                _story.ProjectFolder = await folderPicker.PickSingleFolderAsync();
                //TODO: Test for cancelled FolderPicker
                if (_story.ProjectFolder == null) 
                {
                    Logger.Log(LogLevel.Info,"Open File command cancelled");
                    StatusMessage = "Open Story command cancelled";
                    _canExecuteCommands = true;  // unblock other commands
                    return;
                }
                //if (!_story.ProjectFolder.DisplayName.EndsWith(".stbx"))
                //{
                //    StatusMessage = "Project Folder must end in .stbx";
                //    return;
                //}
                _story.ProjectPath = _story.ProjectFolder.Path;
                _story.ProjectFilename = _story.ProjectFolder.DisplayName;

                IReadOnlyList<StorageFile> files = await _story.ProjectFolder.GetFilesAsync();
                StorageFile file = files[0];
                //NOTE: BasicProperties.DateModified can be the date last changed

                if (file.FileType.ToLower().Equals(".stbx"))
                {
                    _story.ProjectFilename = file.Name;
                    _story.ProjectFile = file;
                    // Make sure files folder exists...
                    _story.FilesFolder = await _story.ProjectFolder.GetFolderAsync("files");
                    //TODO: Back up at the right place (after open?)
                    await BackupProject();
                    StoryReader rdr = Ioc.Default.GetService<StoryReader>();
                    StoryModel = await rdr.ReadFile(file);
                    if (StoryModel.ExplorerView.Count > 0)
                    {
                        SetCurrentView(StoryViewType.ExplorerView);
                        _story.LoadStatus = LoadStatus.LoadFromRtfFiles;
                        StatusMessage = "Open Story completed";
                    }
                }
                else 
                {
                    
                    string message = $"Open project {_story.ProjectFilename} command failed. Unsupported file extension";
                    Logger.Log(LogLevel.Info, message);
                }
                string msg = $"Open project {_story.ProjectFilename} command completed";
                Logger.Log(LogLevel.Info, msg);
                
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Error in OpenFile command");
                StatusMessage = "Open Story command failed";
            }

            Logger.Log(LogLevel.Info, "Open Story completed.");
            _canExecuteCommands = true;
        }

        private async Task BackupProject()
        {
            //Logger.Log(LogLevel.Trace, "BackupProject");
            Logger.Log(LogLevel.Info, "BackupProject executing");
            try
            {
                StorageFolder backup;
                //TODO: Get backup folder from Preferences
                string path = @"C:\Users\Terry Cox\Documents\Writing\Backup";
                StorageFolder backupRoot = await StorageFolder.GetFolderFromPathAsync(path);
                string projectName = _story.ProjectFolder.DisplayName;
                if (await backupRoot.TryGetItemAsync(projectName) == null)
                    backup = await backupRoot.CreateFolderAsync(projectName);
                else
                    backup = await backupRoot.GetFolderAsync(projectName);
                await _story.ProjectFolder.CopyContentsRecursive(backup);
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Error backing up project");
                //TODO: Percolate exception
            }

            Logger.Log(LogLevel.Info, "BackupProject complete");
        }

        private async Task SaveFile()
        {
            //Logger.Log(LogLevel.Trace, "SaveFile");
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Executing SaveFile command");
            try
            {
                //TODO: SaveFile is both an AppButton command and called from NewFile and OpenFile. Split these.
                StatusMessage = "Save File command executing";
                await SaveModel();
                await WriteModel();
                StatusMessage = "Save File command completed";
                StoryModel.Changed = false;
                ChangeStatusColor = Colors.Green;
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Exception in SaveFile");
                StatusMessage = "Save File failed";
            }

            Logger.Log(LogLevel.Info, "SaveFile completed");
            _canExecuteCommands = true;
        }

        /// <summary>
        /// Write the current StoryModel to the backing project folder/files
        /// </summary>
        /// <returns>Task (async function)</returns>
        private async Task WriteModel()
        {
            Logger.Log(LogLevel.Info, string.Format("In WriteModel, file={0}", _story.ProjectFilename));
            try
            {
                await CreateProjectFile();
                var file = _story.ProjectFile;
                if (file != null)
                {
                    StoryWriter wtr = Ioc.Default.GetService<StoryWriter>();
                    //TODO: WriteFile isn't working; file is empty
                    await wtr.WriteFile(_story.ProjectFile, StoryModel);
                    // Prevent updates to the remote version of the file until
                    // we finish making changes and call CompleteUpdatesAsync.
                    CachedFileManager.DeferUpdates(file);
                    await CachedFileManager.CompleteUpdatesAsync(file);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Error writing file");
                StatusMessage = "Error writing file - see log";
                return;
            }
            Logger.Log(LogLevel.Info, "WriteModel successful");
        }

        private async void SaveFileAs()
        {
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Executing 'SaveAs' command");
            try
            {
                StatusMessage = "Save File As command executing";
                SaveAsDialog dialog = new();
                dialog.XamlRoot = GlobalData.XamlRoot;
                var vm = Ioc.Default.GetService<SaveAsViewModel>();
                vm.ProjectName = _story.ProjectFilename;
                vm.ProjectPathName = _story.ProjectPath;
                var result = await dialog.ShowAsync();
                _saveAsParentFolder = dialog.ParentFolder;
                string saveAsProjectName = vm.ProjectName;
                _saveAsProjectFolderExists = dialog.ProjectFolderExists;
                _saveAsProjectFolderPath = dialog.ProjectFolderPath;
                if (result == ContentDialogResult.Primary)
                {
                    if (await VerifyReplaceOrCreate())
                    {
                        await SaveModel();  // Save the model at its present location so it can be copied
                        await WriteModel();
                        string projectName = _story.ProjectFolder.DisplayName;
                        if (!_saveAsProjectFolderExists)
                            _saveAsProjectFolder = await _saveAsParentFolder.CreateFolderAsync(saveAsProjectName);
                        else
                            _saveAsProjectFolder = await _saveAsParentFolder.GetFolderAsync(saveAsProjectName);
                        await _story.ProjectFolder.CopyContentsRecursive(_saveAsProjectFolder);
                        _story.ProjectFilename = saveAsProjectName;
                        _story.ProjectPath = _saveAsProjectFolderPath;
                        // The folder and file are copied, but the 
                        Messenger.Send(new IsChangedMessage(true));
                        StoryModel.Changed = false;
                        ChangeStatusColor = Colors.Green;
                    }
                }
                else
                {
                    // display cancelled message on Shell
                    StatusMessage = "SaveAs dialog cancelled";
                    Logger.Log(LogLevel.Info, "'SaveAs' project command cancelled");
                    _canExecuteCommands = true;
                    return;
                }
                _story.LoadStatus = LoadStatus.LoadFromRtfFiles;
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Exception in SaveFileAs");
                StatusMessage = "Save File As failed";
                return;
            }
            StatusMessage = "Save File As command completed";
            Logger.Log(LogLevel.Info, "Save as command completed");
            _canExecuteCommands = true;
        }

        private async void CloseFile()
        {
            //BUG: Close file logic doesn't work (see comments)
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Executing Close project command");
            StatusMessage = "Closing project";
            // Save the existing file if changed
            if (StoryModel.Changed)
            {
                await SaveModel();
                await WriteModel();
            }
            ResetModel();
            SetCurrentView(StoryViewType.ExplorerView);
            DataSource = StoryModel.ExplorerView;
            ShowHomePage();
            //TODO: Navigate to background Page (is there one?)
            StatusMessage = "Close story command completed";
            Logger.Log(LogLevel.Info, "Close story command completed");
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
            Logger.Log(LogLevel.Info, "Executing Exit project command");
            //TODO: Only close if changed
            if (StoryModel.Changed)
            {
                await SaveModel();
                await WriteModel();
            }
            StatusMessage = "Goodbye";
            Application.Current.Exit();  // Win32
        }

        private async Task<bool> VerifyReplaceOrCreate()
        {
            Logger.Log(LogLevel.Trace, "VerifyReplaceOrCreated");
            ContentDialog replaceDialog = new ContentDialog()
            {
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No"
            };
            if (_saveAsProjectFolderExists)
            {
                replaceDialog.Title = "Replace SaveAs Folder";
                replaceDialog.Content = $"Folder {_saveAsProjectFolderPath} already exists. Replace?";
            }
            else
            {
                replaceDialog.Title = "Create SaveAs Folder";
                replaceDialog.Content = $"Create folder {_saveAsProjectFolderPath}?";
            }
            replaceDialog.XamlRoot = GlobalData.XamlRoot;
            ContentDialogResult result = await replaceDialog.ShowAsync();
            return (result == ContentDialogResult.Primary);
            //if (result == ContentDialogResult.Primary)
            //    _saveAsVerified = true;
            //else
            //    _saveAsVerified = false;
        }

        private async Task CreateProjectFolder()
        {
            StorageFolder folder = await _story.ProjectFolder.CreateFolderAsync(_story.ProjectFilename);
            _story.ProjectFolder = folder;
            _story.ProjectPath = folder.Path;
        }

        private async Task CreateProjectFile()
        {
            _story.ProjectFile = await _story.ProjectFolder.CreateFileAsync(_story.ProjectFilename, CreationCollisionOption.ReplaceExisting);
            //Story.ProjectFolder = await Story.ProjectFile.GetParentAsync();
            // Also, create the data subfolder
            _story.FilesFolder = await _story.ProjectFolder.CreateFolderAsync("files", CreationCollisionOption.OpenIfExists);
        }

        #endregion

        #region Tool and Report Commands

        /// <summary>
        /// Search the 
        /// </summary>
        private void SearchNodes()
        {
            bool searchResult = false;  // true if any node returns true

            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "In ToggleFilter");
            ///TODO: 
            if (FilterIsChecked)
            {
                Logger.Log(LogLevel.Info, "FilterIsChecked= true");
                if (FilterText.Equals(string.Empty))
                {
                    Logger.Log(LogLevel.Info, "No search text provided");
                    StatusMessage = "No search text provided";
                    return;
                }

                // Search both roots (active and trash) in the currently displayed view.
                //TODO: When the view changes, reset the search nodes
                StoryNodeItem root = DataSource[0];
                foreach (StoryNodeItem node in root)
                {
                    bool result =  Search.SearchStoryElement(node, FilterText, StoryModel);
                    if (result == true)
                    {
                        // Note: for display, think about a setter.  
                        //    <Setter Target="SelectionGrid.Background" Value="{ThemeResource TreeViewItemBackgroundSelected}" />
                        //    (or better yet, bind the background property like what's done with the Edit statusbar button foreground
                        // Note: May want to set a breadcrumb property in StoryElement and save previous backGround brush. 
                        // Note: I have a recursive search function; see StoryNodeItem::GetEnumerator
                        // Note: set parent nodes IsExpanded up to the root
                    }
                }
                root = DataSource[1];  // trashcan 
                foreach (var node in root)
                {
                    // Do what we did above
                }
            }
            else
            {
                // foreach view
                //    foreach root node
                //       foreach (TreeNode node in root)
                //         if breadcrumb set
                //            reset background color
                //            reset treeviewnode parent's IsExpanded
                //         end if
                //       end for
                //    end for
                // end for 
            }
            Logger.Log(LogLevel.Info, "ToggleFilter");
            _canExecuteCommands = true;
        }


        /// <summary>
        /// The Help (?) icon on the shell will display an HTML (CHM) help file in a 
        /// separate window by invoking the HelpService.
        /// </summary>
        private void LaunchHelp()
        {
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Launching StoryBuilder help");
            StatusMessage = "Showing StoryBuilder Help";

            Help.LaunchHelp();
            Logger.Log(LogLevel.Info, "Show help completed");
            _canExecuteCommands = true;
        }

        private async void Preferences()
        {
            Logger.Log(LogLevel.Info, "Launching Preferences");
            StatusMessage = "Updating Preferences";
            PreferencesDialog dialog = new PreferencesDialog();
            dialog.XamlRoot = GlobalData.XamlRoot;
            dialog.PreferencesVm.LoadModel();
            var result = await dialog.ShowAsync();
            dialog.PreferencesVm.SaveModel();
            if (result == ContentDialogResult.Primary) // Save changes
            {
                string path = GlobalData.Preferences.InstallationDirectory;
                PreferencesIO loader = new PreferencesIO(GlobalData.Preferences, path);
                await loader.UpdateFile();
            }
            Logger.Log(LogLevel.Info, "Preferences update completed");
            StatusMessage = "Preferences updated";
            // else Cancel- exit
            //TODO: Log cancellation, move 'updated' log and status into Save changes block
        }

        private async void KeyQuestionsTool()
        {
            Logger.Log(LogLevel.Info, "Displaying KeyQuestions tool dialog");
            if (RightTappedNode == null)
                RightTappedNode = CurrentNode;
            KeyQuestionsDialog dialog = new KeyQuestionsDialog();
            dialog.XamlRoot = GlobalData.XamlRoot;
            dialog.KeyQuestionsVm.NextQuestion();
            await dialog.ShowAsync();
            Logger.Log(LogLevel.Info, "KeyQuestions finished");
        }

        private async void TopicsTool()
        {
            Logger.Log(LogLevel.Info, "Displaying Topics tool dialog");
            if (RightTappedNode == null)
                RightTappedNode = CurrentNode;
            TopicsDialog dialog = new TopicsDialog();
            dialog.XamlRoot = GlobalData.XamlRoot;
            await dialog.ShowAsync();
            Logger.Log(LogLevel.Info, "Topics finished");
        }
        private async void MasterPlotTool()
        {
            Logger.Log(LogLevel.Info, "Displaying MasterPlot tool dialog");
            if (RightTappedNode == null)
                RightTappedNode = CurrentNode;
            MasterPlotsDialog dialog = new();
            dialog.XamlRoot = GlobalData.XamlRoot;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)   // Copy command
            {
                string masterPlotName = dialog.MasterPlotsVm.MasterPlotName;
                MasterPlotModel model = dialog.MasterPlotsVm.MasterPlots[masterPlotName];
                IList<MasterPlotScene> scenes = model.MasterPlotScenes;
                foreach (MasterPlotScene scene in scenes)
                {
                    PlotPointModel plotPoint = new PlotPointModel(StoryModel);
                    plotPoint.Name = scene.SceneTitle;
                    plotPoint.Notes = scene.Notes;
                    StoryNodeItem newNode = new StoryNodeItem(plotPoint, RightTappedNode);
                    _sourceChildren = RightTappedNode.Children;
                    _sourceChildren.Add(newNode);
                    RightTappedNode.IsExpanded = true;
                    newNode.IsSelected = true;
                }
            }
            // Else canceled
            Logger.Log(LogLevel.Info, "MasterPlot finished");
        }

        private async void DramaticSituationsTool()
        {
            Logger.Log(LogLevel.Info, "Dislaying Dramatic Situations tool dialog");
            if (RightTappedNode == null)
                RightTappedNode = CurrentNode;
            DramaticSituationsDialog dialog = new DramaticSituationsDialog();
            dialog.XamlRoot = GlobalData.XamlRoot;
            var result = await dialog.ShowAsync();
            DramaticSituationModel situationModel = dialog.DramaticSituationsVm.Situation;
            StoryNodeItem newNode = null;
            switch (result)
            {
                case ContentDialogResult.Primary:       // problem
                    ProblemModel problem = new ProblemModel(StoryModel);
                    problem.Name = situationModel.SituationName;
                    problem.Notes = situationModel.Notes;
                    newNode = new StoryNodeItem(problem, RightTappedNode);
                    break;
                case ContentDialogResult.Secondary:     // scene
                    PlotPointModel plotPoint = new PlotPointModel(StoryModel);
                    plotPoint.Name = situationModel.SituationName;
                    plotPoint.Notes = situationModel.Notes;
                    newNode = new StoryNodeItem(plotPoint, RightTappedNode);
                    break;
                case ContentDialogResult.None:
                    return;
            }
            _sourceChildren = RightTappedNode.Children;
            _sourceChildren.Add(newNode);
            RightTappedNode.IsExpanded = true;
            newNode.IsSelected = true;
            // Else canceled
            Logger.Log(LogLevel.Info, "Dramatic Situations finished");
        }

        private async void StockScenesTool()
        {
            Logger.Log(LogLevel.Info, "Displaying Stock Scenes tool dialog");
            if (RightTappedNode == null)
                RightTappedNode = CurrentNode;
            try
            {
                StockScenesDialog dialog = new StockScenesDialog();
                dialog.XamlRoot = GlobalData.XamlRoot;
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)   // Copy command
                {
                    PlotPointModel plotPoint = new PlotPointModel(StoryModel);
                    plotPoint.Name = dialog.StockScenesVm.SceneName;
                    StoryNodeItem newNode = new StoryNodeItem(plotPoint, RightTappedNode);
                    _sourceChildren = RightTappedNode.Children;
                    _sourceChildren.Add(newNode);
                    RightTappedNode.IsExpanded = true;
                    newNode.IsSelected = true;
                }
                // Else canceled
            }
            catch (Exception e)
            {
                string msg = e.Message;
            }
            Logger.Log(LogLevel.Info, "Stock Scenes finished");
        }
        private async void GenerateScrivenerReports()
        {
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Executing generate Scrivener reports command");
            StatusMessage = "Generate Scrivener Reports executing";
            await SaveModel();

            // Select the Scrivener .scrivx file to add the report to
            FileOpenPicker openPicker = new FileOpenPicker();
            if (Window.Current == null)
            {
                IntPtr hwnd = GetActiveWindow();
                var initializeWithWindow = openPicker.As<IInitializeWithWindow>();
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

            StatusMessage = "Generate Scrivener Reports completed";
            Logger.Log(LogLevel.Info, "Generate Scrivener reports completed");
            _canExecuteCommands = true;
        }

        #endregion  

        #region Move TreeViewItem Commands

        private void MoveTreeViewItemLeft()
        {
            //TODO: Logging
            if (CurrentNode == null)
            {
                StatusMessage = "Click or touch a node to move";
                return;
            }

            if (CurrentNode.Parent.IsRoot)
            {
                StatusMessage = "Cannot move further left";
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

            if (!MoveIsValid()) // Verify message
            {
                StatusMessage = MoveErrorMesage;
                return;
            }

            _sourceChildren.RemoveAt(_sourceIndex);
            if (_targetIndex == -1)
                _targetCollection.Add(CurrentNode);
            else
                _targetCollection.Insert(_targetIndex, CurrentNode);
            CurrentNode.Parent = targetParent;
        }

        private void MoveTreeViewItemRight()
        {
            //TODO: Logging
            StoryNodeItem targetParent;

            if (CurrentNode == null)
            {
                StatusMessage = "Click or touch a node to move";
                return;
            }

            // Probably true only if first child of root
            //if (_currentNode.Parent.IsRoot)
            //{
            //    StatusMessage = "Cannot move further right";
            //    return;
            //}

            _sourceChildren = CurrentNode.Parent.Children;
            _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
            _targetCollection = null;
            _targetIndex = -1;

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
                    StatusMessage = "Cannot move further right";
                    return;
                }

                var grandparentCollection = CurrentNode.Parent.Parent.Children;
                int siblingIndex = grandparentCollection.IndexOf(CurrentNode.Parent) - 1;
                if (siblingIndex >= 0)
                {
                    targetParent = grandparentCollection[siblingIndex];
                    _targetCollection = targetParent.Children;
                    if (_targetCollection.Count > 0)
                    {
                        targetParent = _targetCollection[_targetCollection.Count - 1];
                        _targetCollection = targetParent.Children;
                        _targetIndex = _targetCollection.Count;
                    }
                    else
                    {
                        StatusMessage = "Cannot move further right";
                        return;
                    }
                }
                else
                {
                    StatusMessage = "Cannot move further right";
                    return;
                }
            }

            if (MoveIsValid()) // Verify move
            {
                _sourceChildren.RemoveAt(_sourceIndex);
                if (_targetIndex == -1)
                    _targetCollection.Add(CurrentNode);
                else
                    _targetCollection.Insert(_targetIndex, CurrentNode);
                CurrentNode.Parent = targetParent;
            }
        }

        private void MoveTreeViewItemUp()
        {
            //TODO: Logging
            StoryNodeItem _targetParent;

            if (CurrentNode == null)
            {
                StatusMessage = "Click or touch a node to move";
                return;
            }

            if (CurrentNode.IsRoot)
            {
                StatusMessage = "Cannot move up further";
                return;
            }
            _sourceChildren = CurrentNode.Parent.Children;
            _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
            _targetCollection = null;
            _targetIndex = -1;
            _targetParent = CurrentNode.Parent;

            // If first child, must move to end parent's predecessor
            if (_sourceIndex == 0)
            {
                if (CurrentNode.Parent.Parent == null)
                {
                    StatusMessage = "Cannot move up further";
                    return;
                }
                // find parent's predecessor
                var grandparentCollection = CurrentNode.Parent.Parent.Children;
                var siblingIndex = grandparentCollection.IndexOf(CurrentNode.Parent) - 1;
                if (siblingIndex >= 0)
                {
                    _targetCollection = grandparentCollection[siblingIndex].Children;
                    _targetParent = grandparentCollection[siblingIndex];
                }
                else
                {
                    StatusMessage = "Cannot move up further";
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
            }
        }

        private void MoveTreeViewItemDown()
        {
            //TODO: Logging
            StoryNodeItem _targetParent;

            if (CurrentNode == null)
            {
                StatusMessage = "Click or touch a node to move";
                return;
            }
            if (CurrentNode.IsRoot)
            {
                StatusMessage = "Cannot move a root node";
                return;
            }

            _sourceChildren = CurrentNode.Parent.Children;
            _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
            _targetCollection = null;
            _targetIndex = 0;
            _targetParent = CurrentNode.Parent;

            // If last child, must move to end parent's successor
            if (_sourceIndex == _sourceChildren.Count - 1)
            {
                if (CurrentNode.Parent.Parent == null)
                {
                    StatusMessage = "Cannot move down further";
                    return;
                }
                // find parent's successor
                var grandparentCollection = CurrentNode.Parent.Parent.Children;
                var siblingIndex = grandparentCollection.IndexOf(CurrentNode.Parent) + 1;
                if (siblingIndex == grandparentCollection.Count)
                {
                    StatusMessage = "Cannot move down further";
                    return;
                }
                if (grandparentCollection[siblingIndex].IsRoot)
                {
                    StatusMessage = "Cannot move down further";
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
            }
        }

        #endregion

        #region Add and Remove Story Element Commands

        private void AddFolder()
        {
            AddStoryElement(StoryItemType.Folder);
        }

        private void AddSection()
        {
            AddStoryElement(StoryItemType.Section);
        }

        private void AddProblem()
        {
            AddStoryElement(StoryItemType.Problem);
        }

        private void AddCharacter()
        {
            AddStoryElement(StoryItemType.Character);
        }

        private void AddSetting()
        {
            AddStoryElement(StoryItemType.Setting);
        }

        private void AddPlotPoint()
        {
            AddStoryElement(StoryItemType.PlotPoint);
        }

        private void AddStoryElement(StoryItemType typeToAdd)
        {
            //Logger.Log(LogLevel.Trace, "AddStoryElement");
            _canExecuteCommands = false;
            string msg = string.Format("Adding StoryElement {0}", typeToAdd.ToString());
            Logger.Log(LogLevel.Info, msg);
            if (RightTappedNode == null)
            {
                Logger.Log(LogLevel.Info, "Add StoryElement failed- node not selected");
                StatusMessage = "Right tap a node to add to";
                _canExecuteCommands = true;
                return;
            }

            if (RootNodeType(RightTappedNode) == StoryItemType.TrashCan)
            {
                Logger.Log(LogLevel.Info, "Add StoryElement failed- can't add to TrashCan");
                StatusMessage = "You can't add to Deleted Items";
                _canExecuteCommands = true;
                return;
            }

            switch (typeToAdd)
            {
                case StoryItemType.Folder:
                    FolderModel folder = new FolderModel(StoryModel);
                    _ = new StoryNodeItem(folder, RightTappedNode);
                    break;
                case StoryItemType.Section:
                    SectionModel section = new SectionModel(StoryModel);
                    _ = new StoryNodeItem(section, RightTappedNode);
                    break;
                case StoryItemType.Problem:
                    ProblemModel problem = new ProblemModel(StoryModel);
                    _ = new StoryNodeItem(problem, RightTappedNode);
                    break;
                case StoryItemType.Character:
                    CharacterModel character = new CharacterModel(StoryModel);
                    _ = new StoryNodeItem(character, RightTappedNode);
                    break;
                case StoryItemType.Setting:
                    SettingModel setting = new SettingModel(StoryModel);
                    _ = new StoryNodeItem(setting, RightTappedNode);
                    break;
                case StoryItemType.PlotPoint:
                    PlotPointModel plotPoint = new PlotPointModel(StoryModel);
                    _ = new StoryNodeItem(plotPoint, RightTappedNode);
                    break;
            }

            Messenger.Send(new IsChangedMessage(true));
            msg = string.Format("Added new {0}", typeToAdd.ToString());
            Logger.Log(LogLevel.Info, msg);
            var smsg = new StatusMessage(msg, 100);
            Messenger.Send(new StatusChangedMessage(smsg));
            _canExecuteCommands = true;
        }

        private void RemoveStoryElement()
        {
            //TODO: Logging
            if (RightTappedNode == null)
            {
                StatusMessage = "Right tap a node to delete";
                return;
            }
            if (RootNodeType(RightTappedNode) == StoryItemType.TrashCan)
            {
                StatusMessage = "You can't deleted from Deleted StoryElements";
                return;
            }
            if (RightTappedNode.Parent == null)
            {
                StatusMessage = "You can't delete a root node";
                return;
            }

            ObservableCollection<StoryNodeItem> source =
                RightTappedNode.Parent.Children;
            source.Remove(RightTappedNode);
            TrashCanNode.Children.Add(RightTappedNode);
            RightTappedNode.Parent = TrashCanNode;
            RightTappedNode = null;
            string msg = string.Format("Deleted node {0}", RightTappedNode.Name);
            Logger.Log(LogLevel.Info, msg);
            var smsg = new StatusMessage(msg, 100);
            Messenger.Send(new StatusChangedMessage(smsg));
        }

        private void RestoreStoryElement()
        {
            //TODO: Logging
            if (RightTappedNode == null)
            {
                StatusMessage = "Right tap a node to restore";
                return;
            }
            if (RootNodeType(RightTappedNode) != StoryItemType.TrashCan)
            {
                StatusMessage = "You can only restore from Deleted StoryElements";
                return;
            }
            //TODO: Add dialog to confirm restore
            ObservableCollection<StoryNodeItem> target = DataSource[0].Children;
            TrashCanNode.Children.Remove(RightTappedNode);
            target.Add(RightTappedNode);
            RightTappedNode.Parent = DataSource[0];
            RightTappedNode = null;
        }

        /// <summary>
        /// Add a PlotPoint StoryNodeItem to the end of the Narrative view
        /// by copying from the PlotPoint's StoryNodeItem in the Explorer
        /// view.
        /// </summary>
        private void CopyToNarrative()
        {
            if (RightTappedNode == null)
            {
                StatusMessage = "Select a node to copy";
                return;
            }
            if (RightTappedNode.Type != StoryItemType.PlotPoint)
            {
                StatusMessage = "You can only copy a PlotPoint";
                return;
            }

            PlotPointModel plotPoint = (PlotPointModel)
                StoryModel.StoryElements.StoryElementGuids[RightTappedNode.Uuid];
            // ReSharper disable once ObjectCreationAsStatement
            new StoryNodeItem(plotPoint, StoryModel.NarratorView[0]);

            StatusMessage = "PlotPoint copied to Narrator view";
        }

        /// <summary>
        /// Remove a TreeViewItem for a copied PlotPoint.
        ///
        /// Because you can't remove an ObservableCollection member
        /// directly, this method removes the PlotPoint from
        /// the Narrative view StoryNodeItem and then reloads it.
        /// </summary>
        private void RemoveFromNarrative()
        {
            if (RightTappedNode == null)
            {
                StatusMessage = "Select a node to remove";
                return;
            }
            if (RightTappedNode.Type != StoryItemType.PlotPoint)
            {
                StatusMessage = "You can only remove a PlotPoint copy";
                return;
            }

            foreach (var item in StoryModel.NarratorView[0].Children.ToList())
            {
                if (item.Uuid == RightTappedNode.Uuid)
                {
                    StoryModel.NarratorView[0].Children.Remove(item);
                    break;
                }
            }

            DataSource = LoadNarratorView();
        }

        /// <summary>
        /// Search up the StoryNodeItem tree to its
        /// root from a specified node and return its StoryItemType. 
        /// </summary>
        /// <param name="startNode">The node to begin searching from</param>
        /// <returns>The StoryItemType of the root node</returns>
        private StoryItemType RootNodeType(StoryNodeItem startNode)
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

        public void ViewChanged(object sender, SelectionChangedEventArgs args)
        {
            if (!SelectedView.Equals(CurrentView))
            {
                CurrentView = SelectedView;
                LoadViewFromModel();
                switch (CurrentView)
                {
                    case "Story Explorer View":
                        SetCurrentView(StoryViewType.ExplorerView);
                        break;
                    case "Story Narrator View":
                        SetCurrentView(StoryViewType.NarratorView);
                        break;
                }
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
                    AddPlotPointVisibility = Visibility.Visible;
                    RemoveStoryElementVisibility = Visibility.Visible;
                    //TODO: Use correct values (bug with this)
                    //RestoreStoryElementVisibility = Visibility.Collapsed;
                    RestoreStoryElementVisibility = Visibility.Visible;
                    AddToNarrativeVisibility = Visibility.Visible;
                    //RemoveFromNarrativeVisibility = Visibility.Collapsed;
                    RestoreStoryElementVisibility = Visibility.Visible;
                    break;
                case StoryItemType.Section:         // Narrator tree
                    AddFolderVisibility = Visibility.Collapsed;
                    AddSectionVisibility = Visibility.Visible;
                    AddProblemVisibility = Visibility.Collapsed;
                    AddCharacterVisibility = Visibility.Collapsed;
                    AddSettingVisibility = Visibility.Collapsed;
                    AddPlotPointVisibility = Visibility.Collapsed;
                    RemoveStoryElementVisibility = Visibility.Visible;
                    RestoreStoryElementVisibility = Visibility.Collapsed;
                    AddToNarrativeVisibility = Visibility.Collapsed;
                    RemoveFromNarrativeVisibility = Visibility.Visible;
                    break;
                case StoryItemType.TrashCan:        // Trashcan tree (either view)
                    AddFolderVisibility = Visibility.Collapsed;
                    AddSectionVisibility = Visibility.Collapsed;
                    AddProblemVisibility = Visibility.Collapsed;
                    AddCharacterVisibility = Visibility.Collapsed;
                    AddSettingVisibility = Visibility.Collapsed;
                    AddPlotPointVisibility = Visibility.Collapsed;
                    RemoveStoryElementVisibility = Visibility.Collapsed;
                    RestoreStoryElementVisibility = Visibility.Visible;
                    AddToNarrativeVisibility = Visibility.Collapsed;
                    RemoveFromNarrativeVisibility = Visibility.Collapsed;
                    break;
            }
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
            if (DataSource.Count > 0)
                CurrentNode = DataSource[0];
            StoryModel.Changed = false;
        }

        private void LoadViewFromModel()
        {
            //DataSource = CurrentView.Equals("Story Explorer View") ? LoadExplorerView() : LoadNarratorView();
        }

        /// <summary>
        /// Load the Story Explorer ViewModel collection and return it for binding to the
        /// Shell's Navigator TreeView.
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<StoryNodeItem> LoadExplorerView()
        {
            ObservableCollection<StoryNodeItem> vm = new ObservableCollection<StoryNodeItem>();
            //foreach (StoryNodeItem root in Model.ExplorerView)
            //{
            //    StoryNodeItem vmRoot = new StoryNodeItem(root, null);
            //    RecurseStoryNodeItem(root, vmRoot);
            //    vm.Add(vmRoot);
            //}

            //CurrentRootNode = vm[0];
            //TrashCanNode = vm[1];
            //ViewType = StoryViewType.ExplorerView;
            return vm;
        }

        /// <summary>
        /// Load the Story Narrator StoryViewModel Story and return it for binding to the
        /// Shell's Navigator TreeView.
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<StoryNodeItem> LoadNarratorView()
        {
            ObservableCollection<StoryNodeItem> vm = new ObservableCollection<StoryNodeItem>();
            //foreach (StoryNodeItem root in Model.NarratorView)           {
            //    StoryNodeItem vmRoot = new StoryNodeItem(root, null);
            //    RecurseStoryNodeItem(root, vmRoot);
            //    vm.Add(vmRoot);
            //}

            //CurrentRootNode = vm[0];
            //TrashCanNode = vm[1];
            //ViewType = StoryViewType.NarratorView;
            return vm;
        }

        /// <summary>
        /// Populate the TreeView's StoryNodeItem via traversing one of 
        /// StoryModel's two StoryNodeItem via recursive descent.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parent"></param>
        //private static void RecurseStoryNodeItem(StoryNodeItem node, StoryNodeItem parent)
        //{
        //    // Find the node's Children node
        //    foreach (StoryNodeItem child in node.Children)
        //    {
        //        var vmNode = new StoryNodeItem(child, parent);
        //        parent.Children.Add(vmNode);
        //        RecurseStoryNodeItem(child, vmNode);
        //    }
        //}

        //private void SaveModelFromView()
        //{
        //    ObservableCollection<StoryNodeItem> model = new ObservableCollection<StoryNodeItem>();
        //    foreach (StoryNodeItem vmRoot in DataSource)
        //    {
        //        StoryNodeItem modelRoot = new StoryNodeItem(vmRoot, null);
        //        RecurseStoryNodeViewModel(vmRoot, modelRoot);
        //        model.Add(modelRoot);
        //    }
        //    if (CurrentView.Equals("Story Explorer View"))
        //        Model.ExplorerView = model;
        //    else
        //        Model.NarratorView = model;
        //}

        /// <summary>
        /// Populate the appropriate StoryModel's  StoryNodeItem via traversing
        /// the active TreeView's DataSource StoryNodeItem via recursive descent.
        /// </summary>
        /// <param name="node"></param> 
        /// <param name="parent"></param>
        //private static void RecurseStoryNodeViewModel(StoryNodeItem node, StoryNodeItem parent)
        //{
        //    // Find the node's Children 
        //    foreach (StoryNodeItem child in node.Children)
        //    {
        //        var vmNode = new StoryNodeItem(child, parent);
        //        parent.Children.Add(vmNode);
        //        RecurseStoryNodeViewModel(child, vmNode);
        //    }
        //}

        

        #region MVVM Message processing
        private void IsChangedMessageReceived(IsChangedMessage isDirty)
        {
            StoryModel.Changed = StoryModel.Changed || isDirty.Value;
            if (StoryModel.Changed)
                ChangeStatusColor = Colors.Red;
            else
                ChangeStatusColor = Colors.Green;
        }

        private void StatusMessageReceived(StatusChangedMessage statusMessage)
        {
            StatusMessage = statusMessage.Value.Status;
        }

        /// <summary>
        /// When a Story Element page's name changes the corresponding
        /// StoryNodeItem, which is bound to a TreeViewItem, must
        /// also change. The way this is done is to have the Name field's
        /// setter send a message here. ShellViewModel knows which
        /// StoryNodeItem instance is selected (via OnSelectionChanged) and
        /// alters its Name as well.
        /// </summary>
        /// <param name="msg"></param>
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
                default:
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


            _story = Ioc.Default.GetService<StoryController>();
            //Preferences = Ioc.Default.GetService<Preferences>();
            Scrivener = Ioc.Default.GetService<ScrivenerIo>();
            Logger = Ioc.Default.GetService<LogService>();
            Help = Ioc.Default.GetService<HelpService>();
            Search = Ioc.Default.GetService<SearchService>();

            Title = "Hello Terry";
            StoryModel = new StoryModel();
            StatusMessage = "Ready";

            _canExecuteCommands = true;
            TogglePaneCommand = new RelayCommand(TogglePane, () => _canExecuteCommands);
            NewFileCommand = new RelayCommand(NewFile, () => _canExecuteCommands);
            OpenFileCommand = new RelayCommand(OpenFile, () => _canExecuteCommands);
            SaveFileCommand = new RelayCommand(async () => await SaveFile(), () => _canExecuteCommands);
            SaveAsCommand = new RelayCommand(SaveFileAs, () => _canExecuteCommands);
            CloseCommand = new RelayCommand(CloseFile, () => _canExecuteCommands);
            ExitCommand = new RelayCommand(ExitApp, () => _canExecuteCommands);

            FilterCommand = new RelayCommand(SearchNodes, () => _canExecuteCommands);

            KeyQuestionsCommand = new RelayCommand(KeyQuestionsTool, () => _canExecuteCommands);
            TopicsCommand = new RelayCommand(TopicsTool, () => _canExecuteCommands);
            MasterPlotsCommand = new RelayCommand(MasterPlotTool, () => _canExecuteCommands);
            DramaticSituationsCommand = new RelayCommand(DramaticSituationsTool, () => _canExecuteCommands);
            StockScenesCommand = new RelayCommand(StockScenesTool, () => _canExecuteCommands);
            PreferencesCommand = new RelayCommand(Preferences, () => _canExecuteCommands);

            ReportsCommand = new RelayCommand(GenerateScrivenerReports, () => _canExecuteCommands);

            HelpCommand = new RelayCommand(LaunchHelp, () => _canExecuteCommands);

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
            AddPlotPointCommand = new RelayCommand(AddPlotPoint, () => _canExecuteCommands);
            // Remove Story Element command (move to trash)
            RemoveStoryElementCommand = new RelayCommand(RemoveStoryElement, () => _canExecuteCommands);
            RestoreStoryElementCommand = new RelayCommand(RestoreStoryElement, () => _canExecuteCommands);
            // Copy to Narrative command
            AddToNarrativeCommand = new RelayCommand(CopyToNarrative, () => _canExecuteCommands);
            RemoveFromNarrativeCommand = new RelayCommand(RemoveFromNarrative, () => _canExecuteCommands);

            ViewList.Add("Story Explorer View");
            ViewList.Add("Story Narrator View");

            CurrentView = "Story Explorer View";
            SelectedView = "Story Explorer View";

            FilterStatus = "Filter:Off";

            ChangeStatusColor = Colors.Green;

            ShellInstance = this;
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
        public static extern IntPtr GetActiveWindow();
    }
}
