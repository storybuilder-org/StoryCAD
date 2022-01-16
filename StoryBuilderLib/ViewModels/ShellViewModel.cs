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
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using StoryBuilder.Services;
using WinRT;
using GuidAttribute = System.Runtime.InteropServices.GuidAttribute;

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
        public RelayCommand AddSceneCommand { get; }

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

        private bool _isSearching;
        public bool IsSearching
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
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
        private void CloseUnifiedMenu()
        {
            _contentDialog.Hide();
        }

        private async void OpenUnifiedMenu()                      
        {
            _canExecuteCommands = false;
            // Needs logging
            _contentDialog = new ContentDialog();
            _contentDialog.XamlRoot = GlobalData.XamlRoot;
            _contentDialog.Content = new UnifiedMenuPage();
            await _contentDialog.ShowAsync();
            _canExecuteCommands = true;
        }

        public async Task UnifiedNewFile(UnifiedVM dialogVM)
        {
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "UnifyVM - New File starting");
            try
            {
                StatusMessage = "New project command executing";
                // If the current project needs saved, do so
                if (StoryModel.Changed)
                {
                    SaveModel();
                    await WriteModel();
                }
                
                UnifiedVM vm = dialogVM;  // Access the dialog settings

                // Start with a blank StoryModel and populate it
                // using the new project dialog's settings

                ResetModel();

                if (!Path.GetExtension(vm.ProjectName).Equals(".stbx")) { vm.ProjectName += ".stbx"; }
                StoryModel.ProjectFilename = vm.ProjectName;
                StoryModel.ProjectFolder = await StorageFolder.GetFolderFromPathAsync(vm.ProjectPath);
                StoryModel.ProjectPath = StoryModel.ProjectFolder.Path;
          
                OverviewModel overview = new("Working Title", StoryModel);
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
                        StoryElement scene = new FolderModel("Scene", StoryModel);
                        StoryNodeItem plotpointsNode = new(scene, overviewNode);
                        break;
                    case "External and Internal Problems":
                        StoryElement externalProblem = new ProblemModel("External Problem", StoryModel);
                        StoryNodeItem externalProblemNode = new(externalProblem, overviewNode);
                        StoryElement internalProblem = new ProblemModel("Internal Problem", StoryModel);
                        StoryNodeItem internalProblemNode = new(internalProblem, overviewNode);
                        break;
                    case "Protagonist and Antagonist":
                        StoryElement protagonist = new CharacterModel("Protagonist", StoryModel);
                        StoryNodeItem protagonistNode = new(protagonist, overviewNode);
                        StoryElement antagonist = new CharacterModel("Antagonist", StoryModel);
                        StoryNodeItem antagonistNode = new(antagonist, overviewNode);
                        break;
                    case "Problems and Characters":
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

                Ioc.Default.GetService<MainWindowVM>().Title = $"StoryBuilder - Editing {vm.ProjectName.Replace(".stbx","")}";
                SetCurrentView(StoryViewType.ExplorerView);
                //TODO: Set expand and is selected?

                // Save the new project
                await SaveFile();
                await Ioc.Default.GetService<BackupService>().BackupProject();
                Ioc.Default.GetService<BackupService>().StartTimedBackup();
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
        /// <returns></returns>
        public async Task OpenFile(string fromPath = "")
        {
            if (StoryModel.Changed)
            {
                SaveModel();
                await WriteModel();
            }
            //Logger.Log(LogLevel.Trace, "OpenFile");
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Executing OpenFile command");

            try
            {
                ResetModel();

                if (fromPath == "" || !File.Exists(fromPath))
                {
                    //var window = new Window();
                    //var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
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
                }
                else
                {
                    StoryModel.ProjectFile = await StorageFile.GetFileFromPathAsync(fromPath);
                }

                StoryModel.ProjectFolder = await StoryModel.ProjectFile.GetParentAsync();
                if (StoryModel.ProjectFile == null) 
                {
                    Logger.Log(LogLevel.Info,"Open File command cancelled (StoryModel.ProjectFile was null)");
                    StatusMessage = "Open Story command cancelled";
                    _canExecuteCommands = true;  // unblock other commands
                    return;
                }
                Ioc.Default.GetService<BackupService>().StopTimedBackup();
                //NOTE: BasicProperties.DateModified can be the date last changed

                StoryReader rdr = Ioc.Default.GetService<StoryReader>();
                StoryModel = await rdr.ReadFile(StoryModel.ProjectFile);
                await Ioc.Default.GetService<BackupService>().BackupProject();
                if (StoryModel.ExplorerView.Count > 0)
                {
                    SetCurrentView(StoryViewType.ExplorerView);
                    StatusMessage = "Open Story completed";
                }
                Ioc.Default.GetService<MainWindowVM>().Title = $"StoryBuilder - Editing {StoryModel.ProjectFilename.Replace(".stbx", "")}";
                new UnifiedVM().UpdateRecents(Path.Combine(StoryModel.ProjectFolder.Path,StoryModel.ProjectFile.Name));
                Ioc.Default.GetService<BackupService>().StartTimedBackup();
                string msg = $"Opened project {StoryModel.ProjectFilename}";
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

        private async Task SaveFile()
        {
            Logger.Log(LogLevel.Trace, "Saving file");
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Executing SaveFile command");
            try
            {
                //TODO: SaveFile is both an AppButton command and called from NewFile and OpenFile. Split these.
                StatusMessage = "Save File command executing";
                SaveModel();
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
                StatusMessage = "Error writing file - see log";
                return;
            }
            Logger.Log(LogLevel.Info, "WriteModel successful");
        }


        private async void SaveFileAs()
        {
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Running save as");
            StatusMessage = "Save File As command executing";
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
                        SaveAsVM.SaveAsProjectFolder = await SaveAsVM.ParentFolder.CreateFolderAsync(SaveAsVM.ProjectName, CreationCollisionOption.OpenIfExists);
                        await StoryModel.ProjectFolder.CopyContentsRecursive(SaveAsVM.SaveAsProjectFolder);

                        //Update the StoryModel properties to use the newly saved copy
                        StoryModel.ProjectFilename = SaveAsVM.ProjectName;
                        StoryModel.ProjectFolder = SaveAsVM.SaveAsProjectFolder;
                        StoryModel.ProjectPath = SaveAsVM.SaveAsProjectFolderPath;
                        // Add to the recent files stack
                        Ioc.Default.GetService<MainWindowVM>().Title = $"StoryBuilder - Editing {StoryModel.ProjectFilename.Replace(".stbx", "")}";
                        new UnifiedVM().UpdateRecents(Path.Combine(StoryModel.ProjectFolder.Path, StoryModel.ProjectFile.Name));
                        // Indicate everything's done
                        Messenger.Send(new IsChangedMessage(true));
                        StoryModel.Changed = false;
                        ChangeStatusColor = Colors.Green;
                        StatusMessage = "Save File As command completed";
                        Logger.Log(LogLevel.Info, "Save as command completed");
                    }
                }
                else // if cancelled
                {
                    StatusMessage = "SaveAs dialog cancelled";
                    Logger.Log(LogLevel.Info, "'SaveAs' project command cancelled");
                }
            }
            catch (Exception ex) //If error occurs in file.
            {
                Logger.LogException(LogLevel.Error, ex, "Exception in SaveFileAs");
                StatusMessage = "Save File As failed";
            }
            _canExecuteCommands = true;
        }

        private async Task<bool> VerifyReplaceOrCreate()
        {
            Logger.Log(LogLevel.Trace, "VerifyReplaceOrCreated");
            ContentDialog replaceDialog = new()
            {
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No"
            };
            SaveAsViewModel SaveAsVM = Ioc.Default.GetService<SaveAsViewModel>();
            SaveAsVM.SaveAsProjectFolderPath = Path.Combine(SaveAsVM.ParentFolder.Path, SaveAsVM.ProjectName);
            SaveAsVM.ProjectFolderExists = await SaveAsVM.ParentFolder.TryGetItemAsync(SaveAsVM.ProjectName) != null;
            if (SaveAsVM.ProjectFolderExists)
            {
                replaceDialog.Title = "Replace SaveAs Folder";
                replaceDialog.Content = $"Folder {SaveAsVM.SaveAsProjectFolderPath} already exists. Replace?";
            }
            else
            {
                replaceDialog.Title = "Create SaveAs Folder";
                replaceDialog.Content = $"Create folder {SaveAsVM.SaveAsProjectFolderPath}?";
            }
            replaceDialog.XamlRoot = GlobalData.XamlRoot;
            ContentDialogResult result = await replaceDialog.ShowAsync();
            return result == ContentDialogResult.Primary;

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
                SaveModel();
                await WriteModel();
            }
            ResetModel();
            SetCurrentView(StoryViewType.ExplorerView);
            Ioc.Default.GetService<MainWindowVM>().Title = "StoryBuilder";

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
                SaveModel();
                await WriteModel();
            }
            StatusMessage = "Goodbye";
            Application.Current.Exit();  // Win32
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
            //Logging stuff
            Logger.Log(LogLevel.Info, "Launching Preferences");
            StatusMessage = "Updating Preferences";

            //Creates and shows dialog
            ContentDialog PreferencesDialog = new();
            PreferencesDialog.XamlRoot = GlobalData.XamlRoot;
            PreferencesDialog.Content = new PreferencesDialog();
            PreferencesDialog.Title = "Preferences";
            PreferencesDialog.PrimaryButtonText = "Save";
            PreferencesDialog.SecondaryButtonText = "About StoryBuilder";
            PreferencesDialog.CloseButtonText = "Cancel";

            ContentDialogResult result = await PreferencesDialog.ShowAsync();
            switch (result)
            {
                // Save changes
                case ContentDialogResult.Primary:
                    await Ioc.Default.GetService<PreferencesViewModel>().SaveAsync();
                    Logger.Log(LogLevel.Info, "Preferences update completed");
                    StatusMessage = "Preferences updated";
                    break;
                case ContentDialogResult.Secondary:
                {
                    ContentDialog AboutDialog = new();
                    AboutDialog.XamlRoot = GlobalData.XamlRoot;
                    AboutDialog.Content = new About();
                    AboutDialog.Width = 900;
                    AboutDialog.Title = "About StoryBuilder";
                    AboutDialog.SecondaryButtonText = "Join Discord";
                    AboutDialog.CloseButtonText = "Close";
                    var a = await AboutDialog.ShowAsync();

                    if (a == ContentDialogResult.Secondary)
                    {
                        Process Browser = new();
                        Browser.StartInfo.FileName = @"https://discord.gg/wfZxU4bx6n";
                        Browser.StartInfo.UseShellExecute = true;
                        Browser.Start();
                        }
                    break;
                }
                //don't save changes
                default:
                    Logger.Log(LogLevel.Info, "Preferences update canceled");
                    StatusMessage = "Preferences closed";
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
                    // add the new SceneModel & node to the end of the target's children 
                    StoryNodeItem newNode = new(child, RightTappedNode);
                    RightTappedNode.IsExpanded = true;
                    newNode.IsSelected = true;
                }
                string msg = $"MasterPlot {masterPlotName} inserted";
                StatusMessage = msg;
                Logger.Log(LogLevel.Info, msg);
                ShowChange();
                Logger.Log(LogLevel.Info, "MasterPlot complete");
            }
            else  // canceled
            {
                StatusMessage = "MasterPlot cancelled";
                Logger.Log(LogLevel.Info, "MasterPlot canceled");
            } 
        }

        private async void DramaticSituationsTool()
        {
            Logger.Log(LogLevel.Info, "Dislaying Dramatic Situations tool dialog");
            if (RightTappedNode == null)  { RightTappedNode = CurrentNode; }

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
            switch (result)
            {
                case ContentDialogResult.Primary:
                {
                    ProblemModel problem = new ProblemModel(StoryModel);
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
                    msg = "MasterPlot cancelled";
                    break;
            }

            StatusMessage = msg;
            Logger.Log(LogLevel.Info, msg);
            RightTappedNode.IsExpanded = true;
            newNode.IsSelected = true;
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
                dialog.PrimaryButtonText = "Stock Scenes";
                dialog.CloseButtonText = "Cancel";
                dialog.XamlRoot = GlobalData.XamlRoot;
                ContentDialogResult result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)   // Copy command
                {
                    SceneModel sceneVar = new SceneModel(StoryModel);
                    sceneVar.Name = Ioc.Default.GetService<StockScenesViewModel>().SceneName;
                    StoryNodeItem newNode = new(sceneVar, RightTappedNode);
                    _sourceChildren = RightTappedNode.Children;
                    _sourceChildren.Add(newNode);
                    RightTappedNode.IsExpanded = true;
                    newNode.IsSelected = true;
                    Logger.Log(LogLevel.Info, "Stock Scenes finished");
                }
                else {Logger.Log(LogLevel.Info, "Stock Scenes canceled");}
            }
            catch (Exception e) {  Logger.LogException(LogLevel.Error, e, e.Message); }
        }

        private async void OpenReportsDialog()
        {
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

                //switch (ReportVM.ReportType)
                //{
                //    case "Scrivener":
                //        //Put code to make scrivener report here
                //        break;
                //    case "Preview":
                //        //Put code to show preview here (or remove it)
                //        break;
                //    case "Printer":
                //        //Put code to print here (or remove it)
                //    break;
                //}

                StatusMessage = "Report generator complete";
                Logger.Log(LogLevel.Info, "Report Generator complete");
            }
            else
            {
                StatusMessage = "Report generator canceled";
                Logger.Log(LogLevel.Info, "Report Generator canceled");
            }
        }

        private async void GeneratePrintReports()
        {
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Executing Generate Print Reports command");
            StatusMessage = "Generate Print Reports executing";
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

                PrintReports rpt = new PrintReports(ReportVM, StoryModel);
                await rpt.Generate();

                StatusMessage = "Generate Print Reports complete";
                Logger.Log(LogLevel.Info, "Generate Print Reports complete");
            }
            else
            {
                StatusMessage = "Generate Print Reports canceled";
                Logger.Log(LogLevel.Info, "Generate Print Reports canceled");
            }
            _canExecuteCommands = true;
        }

        private async void GenerateScrivenerReports()
        {
            _canExecuteCommands = false;
            Logger.Log(LogLevel.Info, "Executing Generate Scrivener Reports command");
            StatusMessage = "Generate Scrivener Reports executing";
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
            StoryNodeItem targetParent = CurrentNode.Parent.Parent;

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
            StoryNodeItem _targetParent = CurrentNode.Parent;

            // If first child, must move to end parent's predecessor
            if (_sourceIndex == 0)
            {
                if (CurrentNode.Parent.Parent == null)
                {
                    StatusMessage = "Cannot move up further";
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
            StoryNodeItem _targetParent = CurrentNode.Parent;

            // If last child, must move to end parent's successor
            if (_sourceIndex == _sourceChildren.Count - 1)
            {
                if (CurrentNode.Parent.Parent == null)
                {
                    StatusMessage = "Cannot move down further";
                    return;
                }
                // find parent's successor
                ObservableCollection<StoryNodeItem> grandparentCollection = CurrentNode.Parent.Parent.Children;
                int siblingIndex = grandparentCollection.IndexOf(CurrentNode.Parent) + 1;
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

        private void AddScene()
        {
            AddStoryElement(StoryItemType.Scene);
        }

        private void AddStoryElement(StoryItemType typeToAdd)
        {
            Logger.Log(LogLevel.Trace, "AddStoryElement");
            _canExecuteCommands = false;
            string msg = $"Adding StoryElement {typeToAdd.ToString()}";
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
                    FolderModel folder = new(StoryModel);
                    _ = new StoryNodeItem(folder, RightTappedNode);
                    break;
                case StoryItemType.Section:
                    SectionModel section = new(StoryModel);
                    _ = new StoryNodeItem(section, RightTappedNode);
                    break;
                case StoryItemType.Problem:
                    ProblemModel problem = new(StoryModel);
                    _ = new StoryNodeItem(problem, RightTappedNode);
                    break;
                case StoryItemType.Character:
                    CharacterModel character = new(StoryModel);
                    _ = new StoryNodeItem(character, RightTappedNode);
                    break;
                case StoryItemType.Setting:
                    SettingModel setting = new(StoryModel);
                    _ = new StoryNodeItem(setting, RightTappedNode);
                    break;
                case StoryItemType.Scene:
                    SceneModel sceneVar = new(StoryModel);
                    _ = new StoryNodeItem(sceneVar, RightTappedNode);
                    break;
            }

            Messenger.Send(new IsChangedMessage(true));
            msg = $"Added new {typeToAdd.ToString()}";
            Logger.Log(LogLevel.Info, msg);
            StatusMessage smsg = new(msg, 100);
            Messenger.Send(new StatusChangedMessage(smsg));
            _canExecuteCommands = true;
        }

        private void RemoveStoryElement()
        {
            Logger.Log(LogLevel.Trace, "RemoveStoryElement");
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
            DataSource[1].Children.Add(RightTappedNode);
            RightTappedNode.Parent = DataSource[1];
            string msg = $"Deleted node {RightTappedNode.Name}";
            Logger.Log(LogLevel.Info, msg);
            StatusMessage smsg = new(msg, 100);
            Messenger.Send(new StatusChangedMessage(smsg));
        }

        private void RestoreStoryElement()
        {
             Logger.Log(LogLevel.Trace, "RestoreStoryElement");
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
            DataSource[1].Children.Remove(RightTappedNode);
            target.Add(RightTappedNode);
            RightTappedNode.Parent = DataSource[0];
            string msg = $"Restored node {RightTappedNode.Name}";
            Logger.Log(LogLevel.Info, msg);
            StatusMessage smsg = new(msg, 100);
            Messenger.Send(new StatusChangedMessage(smsg));
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
                StatusMessage = "Select a node to copy";
                return;
            }
            if (RightTappedNode.Type != StoryItemType.Scene)
            {
                StatusMessage = "You can only copy a scene";
                return;
            }

            SceneModel sceneVar = (SceneModel) StoryModel.StoryElements.StoryElementGuids[RightTappedNode.Uuid];
            // ReSharper disable once ObjectCreationAsStatement
            _ = new StoryNodeItem(sceneVar, StoryModel.NarratorView[0]);
            string msg = $"Copied node {RightTappedNode.Name} to Narrative View";
            Logger.Log(LogLevel.Info, msg);
            StatusMessage smsg = new(msg, 100);
            Messenger.Send(new StatusChangedMessage(smsg));
        }

        /// <summary>
        /// Remove a TreeViewItem from the Narrative view for a copied Scene.
        /// </summary>
        private void RemoveFromNarrative()
        {
            Logger.Log(LogLevel.Trace, "RemoveFromNarrative");

            string msg;
            StatusMessage smsg;

            if (RightTappedNode == null)
            {
                StatusMessage = "Select a node to remove";
                return;
            }
            if (RightTappedNode.Type != StoryItemType.Scene)
            {
                StatusMessage = "You can only remove a Scene copy";
                return;
            }

            foreach (StoryNodeItem item in StoryModel.NarratorView[0].Children.ToList())
            {
                if (item.Uuid == RightTappedNode.Uuid)
                {
                    StoryModel.NarratorView[0].Children.Remove(item);
                    msg = $"Removed node {RightTappedNode.Name} from Narrative View";
                    Logger.Log(LogLevel.Info, msg);
                    smsg = new StatusMessage(msg, 100);
                    Messenger.Send(new StatusChangedMessage(smsg));
                    return;
                }
            }
            msg = $"Node {RightTappedNode.Name} not in Narrative View";
            Logger.Log(LogLevel.Info, msg);
            smsg = new StatusMessage(msg, 100);
            Messenger.Send(new StatusChangedMessage(smsg));
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
        }

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
            StatusMessage = "Ready";

            _canExecuteCommands = true;
            TogglePaneCommand = new RelayCommand(TogglePane, () => _canExecuteCommands);
            OpenUnifiedCommand = new RelayCommand(OpenUnifiedMenu, () => _canExecuteCommands);
            CloseUnifiedCommand = new RelayCommand(CloseUnifiedMenu, () => _canExecuteCommands);
            OpenFileCommand = new RelayCommand(async () => await OpenFile(), () => _canExecuteCommands);
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

            PrintReportsCommand = new RelayCommand(GeneratePrintReports, () => _canExecuteCommands);
            ScrivenerReportsCommand = new RelayCommand(GenerateScrivenerReports, () => _canExecuteCommands);

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

        private void SearchNodes()
        {
            _canExecuteCommands = false;    //This prevents other commands from being used till this one is complete.
            Logger.Log(LogLevel.Info, "Better search started.");

            StoryNodeItem root = DataSource[0]; //Gets all nodes in the tree
            int SearchTotal = 0;

            if (FilterText == "" || !IsSearching) //Nulls the backgrounds to make them transparent (default) //Check if toggled and null backgrounds if not.
            {
                Logger.Log(LogLevel.Info, "Search text is blank, making all backgrounds null.");
                foreach (StoryNodeItem node in root) { node.Background = null; }
                FilterText = "";
            }
            else
            {
                foreach (StoryNodeItem node in root)
                {
                    if (Search.SearchStoryElement(node, FilterText, StoryModel)) //checks if node name contains the thing we are looking for
                    {
                        SearchTotal++;
                        if (Application.Current.RequestedTheme == ApplicationTheme.Light) { node.Background = new SolidColorBrush(Colors.LightGoldenrodYellow); }
                        else { node.Background = new SolidColorBrush(Colors.DarkGoldenrod); } //Light Goldenrod is hard to read in dark theme
                        node.IsExpanded = true; 
                        
                        StoryNodeItem parent = node.Parent;
                        while (!parent.IsRoot)
                        {
                            parent.IsExpanded = true;
                            parent = parent.Parent;
                        }

                        if (parent.IsRoot) { parent.IsExpanded = true; }
                    }
                    else { node.Background = null; }
                }
            }

            _canExecuteCommands = true;    //Enables other commands from being used till this one is complete.
            Logger.Log(LogLevel.Info, "Better search completed, found " + SearchTotal + " matches");
            StatusMessage = $"Found {SearchTotal} matches";
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
