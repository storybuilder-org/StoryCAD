using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
using StoryCADLib.DAL;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services;
using StoryCADLib.Services.Backend;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.Dialogs.Tools;
using StoryCADLib.Services.Locking;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Reports;
using StoryCADLib.Services.Search;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.ViewModels.SubViewModels;

public class OutlineViewModel : ObservableRecipient
{
    // TODO: Circular dependency - OutlineViewModel ↔ AutoSaveService/BackupService
    // These services depend on OutlineViewModel in their constructors,
    // so we cannot inject them here without creating a circular dependency.
    // The lazy-loading properties below will fail if accessed before the services are constructed.
    // Long-term fix: Break the dependency by having services use messaging or move shared data to AppState.
    private readonly AutoSaveService _autoSaveService;
    private readonly BackendService _backendService;
    private readonly EditFlushService _editFlushService;
    private readonly AppState appState;
    private readonly ILogService logger;
    private readonly OutlineService outlineService;
    private readonly PreferenceService preferences;
    private readonly SearchService searchService;
    private readonly Windowing window;

    private BackupService _backupService;
    // The reference to ShellViewModel is temporary
    // until the ShellViewModel is refactored to fully
    // use OutlineViewModel for outline methods.

    /// <summary>
    ///     Private backing store for ShellVM since we can't
    ///     access it immediately on the constructor of this class
    ///     so we have shellVm as a property to get it when needed but
    /// </summary>
    private ShellViewModel _shellVM;

    #region Constructor(s)

    public OutlineViewModel(ILogService logService, PreferenceService preferenceService,
        Windowing windowing, OutlineService outlineService, AppState appState,
        SearchService searchService, BackendService backendService, EditFlushService editFlushService,
        AutoSaveService autoSaveService)
    {
        logger = logService;
        preferences = preferenceService;
        window = windowing;
        this.outlineService = outlineService;
        this.appState = appState;
        this.searchService = searchService;
        _backendService = backendService;
        _editFlushService = editFlushService;
        _autoSaveService = autoSaveService;
    }

    #endregion

    private ShellViewModel shellVm
    {
        get
        {
            if (_shellVM == null)
            {
                _shellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
            }

            return _shellVM;
        }
    }

    private BackupService backupService
    {
        get
        {
            if (_backupService == null)
            {
                _backupService = Ioc.Default.GetRequiredService<BackupService>();
            }

            return _backupService;
        }
    }

    // StoryModel and StoryModelFile moved to AppState.CurrentDocument
    // Access via appState.CurrentDocument?.Model and appState.CurrentDocument?.FilePath

    /// <summary>
    ///     Opens a file picker to let the user chose a .stbx file and loads said file
    ///     If fromPath is specified then the picker is skipped.
    /// </summary>
    /// <param name="fromPath">Path to open file from (Optional)</param>
    public async Task OpenFile(string fromPath = "")
    {
        using (var serializationLock = new SerializationLock(logger))
        {
            // Check if current StoryModel has been changed, if so, save and write the model.
            if (appState.CurrentDocument?.Model?.Changed ?? false)
            {
                _editFlushService.FlushCurrentEdits();
                await outlineService.WriteModel(appState.CurrentDocument.Model, appState.CurrentDocument.FilePath);
            }

            logger.Log(LogLevel.Info, "Executing OpenFile command");
        }

        try
        {
            using (var serializationLock = new SerializationLock(logger))
            {
                // Reset the model and show the home page
                shellVm.ResetModel();
                shellVm.ShowHomePage();

                // Open file picker if `fromPath` is not provided or file doesn't exist at the path.
                if (fromPath == "" || !File.Exists(fromPath))
                {
                    logger.Log(LogLevel.Info, "Opening file picker as story wasn't able to be found");
                    var projectFile = await window.ShowFilePicker("Open Project File", ".stbx");
                    if (projectFile == null) //Picker was canceled.
                    {
                        logger.Log(LogLevel.Info, "Open file picker cancelled.");
                        return;
                    }

                    fromPath = projectFile.Path;
                }

                var filePath = fromPath;
                if (string.IsNullOrEmpty(filePath))
                {
                    logger.Log(LogLevel.Warn, "Open File command failed: StoryModel.ProjectFile is null.");
                    Messenger.Send(
                        new StatusChangedMessage(new StatusMessage("Open Story command cancelled", LogLevel.Info)));
                    return;
                }

                if (!File.Exists(filePath))
                {
                    Messenger.Send(new StatusChangedMessage(
                        new StatusMessage($"Cannot find file {filePath}", LogLevel.Warn, true)));
                    return;
                }

                //Check file is available.
                var rdr = Ioc.Default.GetRequiredService<StoryIO>();
                if (!await rdr.CheckFileAvailability(filePath))
                {
                    Messenger.Send(
                        new StatusChangedMessage(new StatusMessage("File Unavailable.", LogLevel.Warn, true)));
                    return;
                }

                //Read file
                var loadedModel = await outlineService.OpenFile(filePath);

                //Check the file we loaded actually has StoryCAD Data.
                if (loadedModel == null)
                {
                    Messenger.Send(new StatusChangedMessage(
                        new StatusMessage("Unable to open file (No Story Elements found)", LogLevel.Warn, true)));
                    return;
                }

                if (loadedModel.StoryElements.Count == 0)
                {
                    Messenger.Send(new StatusChangedMessage(
                        new StatusMessage("Unable to open file (No Story Elements found)", LogLevel.Warn, true)));
                    return;
                }

                // Successfully loaded - create StoryDocument
                appState.CurrentDocument = new StoryDocument(loadedModel, filePath);
            }

            // Take a backup of the project if the user has the 'backup on open' preference set.
            if (preferences.Model.BackupOnOpen)
            {
                await Ioc.Default.GetRequiredService<BackupService>().BackupProject();
            }

            // Set the current view to the ExplorerView
            if (appState.CurrentDocument?.Model?.ExplorerView?.Count > 0)
            {
                outlineService.SetCurrentView(appState.CurrentDocument.Model, StoryViewType.ExplorerView);
                Messenger.Send(new StatusChangedMessage(new StatusMessage("Open Story completed", LogLevel.Info)));
            }

            window.UpdateWindowTitle();
            await Ioc.Default.GetRequiredService<FileOpenService>().UpdateRecents(appState.CurrentDocument?.FilePath);

            if (preferences.Model.TimedBackup)
            {
                Ioc.Default.GetRequiredService<BackupService>().StartTimedBackup();
            }

            if (preferences.Model.AutoSave)
            {
                _autoSaveService.StartAutoSave();
            }

            // Navigate to Overview node after successful open
            if (appState.CurrentDocument?.Model?.ExplorerView?.Count > 0)
            {
                shellVm.TreeViewNodeClicked(appState.CurrentDocument.Model.ExplorerView[0]);
            }

            logger.Log(LogLevel.Info, $"Opened project {appState.CurrentDocument?.FilePath}");
        }
        catch (Exception ex)
        {
            // Report the error to the user
            logger.LogException(LogLevel.Error, ex, "Error in OpenFile command");
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Open Story command failed", LogLevel.Error)));
        }

        logger.Log(LogLevel.Info, "Open Story completed.");
    }

    /// <summary>
    ///     Create a new file.
    /// </summary>
    /// <param name="dialogVm"></param>
    /// <returns></returns>
    public async Task CreateFile(FileOpenVM dialogVm)
    {
        logger.Log(LogLevel.Info, "FileOpenVM - New File starting");

        try
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("New project command executing", LogLevel.Info)),
                true);
            // Validate the requested file path before making any changes
            if (!Path.GetExtension(dialogVm.OutlineName)!.Equals(".stbx"))
            {
                dialogVm.OutlineName += ".stbx";
            }

            var newFilePath = Path.Combine(dialogVm.OutlineFolder, dialogVm.OutlineName);

            if (!StoryIO.IsValidPath(newFilePath))
            {
                logger.Log(LogLevel.Warn, $"Invalid file path {newFilePath}");
                Messenger.Send(new StatusChangedMessage(new StatusMessage("Invalid file path", LogLevel.Error)), true);
                return;
            }

            using (var serializationLock = new SerializationLock(logger))
            {
                // If the current project needs saved, do so
                if (appState.CurrentDocument?.Model?.Changed == true && appState.CurrentDocument?.FilePath != null)
                {
                    await outlineService.WriteModel(appState.CurrentDocument.Model, appState.CurrentDocument.FilePath);
                }
            }

            // Start with a blank StoryModel
            shellVm.ResetModel();
            shellVm.ShowHomePage();

            using (var serializationLock = new SerializationLock(logger))
            {
                // Create the new outline's file
                var folder = await StorageFolder.GetFolderFromPathAsync(dialogVm.OutlineFolder);
                var storyModelFile =
                    (await folder.CreateFileAsync(dialogVm.OutlineName, CreationCollisionOption.GenerateUniqueName))
                    .Path;

                // Create the StoryModel
                var name = Path.GetFileNameWithoutExtension(storyModelFile);
                var author = preferences.Model.FirstName + " " + preferences.Model.LastName;

                // Create the new project StorageFile; throw an exception if it already exists.
                var newModel = await outlineService.CreateModel(name, author, dialogVm.SelectedTemplateIndex);
                appState.CurrentDocument = new StoryDocument(newModel, storyModelFile);
            }

            outlineService.SetCurrentView(appState.CurrentDocument.Model, StoryViewType.ExplorerView);

            await Ioc.Default.GetRequiredService<FileOpenService>().UpdateRecents(appState.CurrentDocument.FilePath);
            outlineService.SetChanged(appState.CurrentDocument.Model, true);
            await SaveFile();

            using (var serializationLock = new SerializationLock(logger))
            {
                if (preferences.Model.BackupOnOpen)
                {
                    await shellVm.MakeBackup();
                    shellVm.BackupStatusColor = Colors.Green;
                }
                else
                {
                    shellVm.BackupStatusColor = Colors.Green;
                }

                shellVm.TreeViewNodeClicked(appState.CurrentDocument.Model.ExplorerView[0]);
                window.UpdateWindowTitle();
            }

            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("New project command completed", LogLevel.Info, true)),
                true);
        }
        catch (Exception ex)
        {
            logger.LogException(LogLevel.Error, ex, "Error in CreateFile command");
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Error creating new project", LogLevel.Error)),
                true);
        }
    }

    /// <summary>
    ///     Opens the file open menu
    /// </summary>
    public async Task OpenFileOpenMenu()
    {
        logger.Log(LogLevel.Info, "Opening File Menu");

        shellVm._contentDialog = new ContentDialog { Content = new FileOpenMenuPage() };
        if (window.RequestedTheme == ElementTheme.Light)
        {
            shellVm._contentDialog.RequestedTheme = window.RequestedTheme;
            shellVm._contentDialog.Background = new SolidColorBrush(Colors.LightGray);
        }

        logger.Log(LogLevel.Info, "Showing File Menu");
        await window.ShowContentDialog(shellVm._contentDialog);
        logger.Log(LogLevel.Info, "Closed File Menu");
    }

    /// <summary>
    ///     Save the currently active page from
    /// </summary>
    /// <param name="autoSave"></param>
    /// <returns></returns>
    public async Task SaveFile(bool autoSave = false)
    {
        using (var serializationLock = new SerializationLock(logger))
        {
            var msg = autoSave ? "AutoSave" : "SaveFile command";
            if (autoSave && !(appState.CurrentDocument?.Model?.Changed ?? false))
            {
                logger.Log(LogLevel.Info, $"{msg} skipped, no changes");
                return;
            }

            if (appState.CurrentDocument?.Model == null || appState.CurrentDocument.Model.StoryElements.Count == 0)
            {
                Messenger.Send(
                    new StatusChangedMessage(new StatusMessage("You need to open a story first!", LogLevel.Info)));
                logger.Log(LogLevel.Info, $"{msg} cancelled (CurrentDocument or Model was null)");
                return;
            }

            try
            {
                Messenger.Send(new StatusChangedMessage(new StatusMessage($"{msg} executing", LogLevel.Info)));
                _editFlushService.FlushCurrentEdits();
                await outlineService.WriteModel(appState.CurrentDocument.Model, appState.CurrentDocument.FilePath);
                Messenger.Send(new StatusChangedMessage(new StatusMessage($"{msg} completed", LogLevel.Info)));
                outlineService.SetChanged(appState.CurrentDocument.Model, false);
                shellVm.ChangeStatusColor = Colors.Green;
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex, $"Exception in {msg}");
                Messenger.Send(new StatusChangedMessage(new StatusMessage($"{msg} failed", LogLevel.Error)));
            }

            _autoSaveService.StartAutoSave();
        }
    }

    public async Task SaveFileAs()
    {
        using (var serializationLock = new SerializationLock(logger))
        {
            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("Save File As command executing", LogLevel.Info, true)));
            try
            {
                if (string.IsNullOrEmpty(appState.CurrentDocument?.FilePath))
                {
                    Messenger.Send(
                        new StatusChangedMessage(new StatusMessage("You need to load a story first!", LogLevel.Info)));
                    logger.Log(LogLevel.Warn, "User tried to use save as without a story loaded.");
                    return;
                }

                var saveAsVm = Ioc.Default.GetRequiredService<SaveAsViewModel>();

                // Create the content dialog
                ContentDialog saveAsDialog = null;
                if (!appState.Headless)
                {
                    // Set default values in the view model using the current story file info
                    saveAsVm.ProjectName = Path.GetFileName(appState.CurrentDocument.FilePath);
                    saveAsVm.ParentFolder = Path.GetDirectoryName(appState.CurrentDocument.FilePath);

                    saveAsDialog = new ContentDialog
                    {
                        Title = "Save as",
                        PrimaryButtonText = "Save",
                        SecondaryButtonText = "Cancel",
                        Content = new SaveAsDialog()
                    };
                }


                var result = await window.ShowContentDialog(saveAsDialog);

                if (result == ContentDialogResult.Primary)
                {
                    if (await VerifyReplaceOrCreate())
                    {
                        var newFilePath = Path.Combine(saveAsVm.ParentFolder, saveAsVm.ProjectName);

                        if (!StoryIO.IsValidPath(newFilePath))
                        {
                            logger.Log(LogLevel.Warn, $"File path {newFilePath} is not valid");
                            shellVm.ShowMessage(LogLevel.Warn, "File path contains invalid characters", false);
                            return;
                        }

                        // Save the model to disk at the current file location
                        _editFlushService.FlushCurrentEdits();
                        await outlineService.WriteModel(appState.CurrentDocument.Model,
                            appState.CurrentDocument.FilePath);

                        // If the new path is the same as the current one, exit early
                        if (newFilePath.Equals(appState.CurrentDocument.FilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            Messenger.Send(new StatusChangedMessage(new StatusMessage("Save File As command completed",
                                LogLevel.Info)));
                            logger.Log(LogLevel.Info, "User tried to save file to same location as current file.");
                            return;
                        }

                        logger.Log(LogLevel.Info,
                            $"Testing filename validity for {saveAsVm.ParentFolder}\\{saveAsVm.ProjectName}");
                        // Copy the current file to the new location/name
                        var currentFile = await StorageFile.GetFileFromPathAsync(appState.CurrentDocument.FilePath);
                        var folder = await StorageFolder.GetFolderFromPathAsync(saveAsVm.ParentFolder);
                        await currentFile.CopyAsync(folder, saveAsVm.ProjectName, NameCollisionOption.ReplaceExisting);

                        // Update the story file path to the new location
                        appState.CurrentDocument.FilePath = newFilePath;

                        // Update window title and recent files
                        window.UpdateWindowTitle();
                        await Ioc.Default.GetRequiredService<FileOpenService>()
                            .UpdateRecents(appState.CurrentDocument.FilePath);

                        // Indicate the model is now saved and unchanged
                        Messenger.Send(new IsChangedMessage(true));
                        outlineService.SetChanged(appState.CurrentDocument.Model, false);
                        shellVm.ChangeStatusColor = Colors.Green;
                        Messenger.Send(new StatusChangedMessage(new StatusMessage("Save File As command completed",
                            LogLevel.Info, true)));
                    }
                }
                else
                {
                    Messenger.Send(
                        new StatusChangedMessage(new StatusMessage("SaveAs dialog cancelled", LogLevel.Info, true)));
                }
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex, "Exception in SaveFileAs");
                Messenger.Send(new StatusChangedMessage(new StatusMessage("Save File As failed", LogLevel.Info)));
            }
        }
    }

    private async Task<bool> VerifyReplaceOrCreate()
    {
        logger.Log(LogLevel.Trace, "VerifyReplaceOrCreated");

        var saveAsVm = Ioc.Default.GetRequiredService<SaveAsViewModel>();
        if (File.Exists(Path.Combine(saveAsVm.ParentFolder, saveAsVm.ProjectName))
            && !appState.Headless)
        {
            ContentDialog replaceDialog = new()
            {
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                Title = "Replace file?",
                Content = $"File {Path.Combine(saveAsVm.ParentFolder,
                    saveAsVm.ProjectName)} already exists. \n\nDo you want to replace it?"
            };
            return await window.ShowContentDialog(replaceDialog) == ContentDialogResult.Primary;
        }

        return true;
    }

    public async Task CloseFile()
    {
        Messenger.Send(new StatusChangedMessage(new StatusMessage("Closing project", LogLevel.Info, true)));
        using (var serializationLock = new SerializationLock(logger))
        {
            // Stop auto-save and wait for any in-progress save to complete
            await _autoSaveService.StopAutoSaveAndWaitAsync();

            if (appState.CurrentDocument?.Model?.Changed == true && !appState.Headless)
            {
                ContentDialog warning = new()
                {
                    Title = "Save changes?",
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No"
                };
                if (await window.ShowContentDialog(warning) == ContentDialogResult.Primary)
                {
                    _editFlushService.FlushCurrentEdits();
                    await outlineService.WriteModel(appState.CurrentDocument.Model, appState.CurrentDocument.FilePath);

                    // Mark the model as saved and update UI
                    outlineService.SetChanged(appState.CurrentDocument.Model, false);
                    Messenger.Send(new IsChangedMessage(false));
                }
            }

            shellVm.ResetModel();
            appState.CurrentDocument = null;
            shellVm.RightTappedNode = null; //Null right tapped node to prevent possible issues.
            window.UpdateWindowTitle();
            Ioc.Default.GetRequiredService<BackupService>().StopTimedBackup();
        }

        shellVm.ShowHomePage();
        Messenger.Send(
            new StatusChangedMessage(new StatusMessage("Close story command completed", LogLevel.Info, true)));
    }

    /// <summary>
    ///     Quits the app.
    /// </summary>
    public async Task ExitApp()
    {
        using (var serializationLock = new SerializationLock(logger))
        {
            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("Executing Exit project command", LogLevel.Info, true)));

            if (appState.CurrentDocument?.Model?.Changed == true)
            {
                ContentDialog warning = new()
                {
                    Title = "Save changes?",
                    PrimaryButtonText = "Yes",
                    SecondaryButtonText = "No"
                };
                if (await window.ShowContentDialog(warning) == ContentDialogResult.Primary)
                {
                    _editFlushService.FlushCurrentEdits();
                    await outlineService.WriteModel(appState.CurrentDocument.Model, appState.CurrentDocument.FilePath);
                }
            }

            await _backendService.DeleteWorkFile();
            try
            {
                logger.Flush();
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex, "Error flushing log during shutdown");
                Messenger.Send(new StatusChangedMessage(new StatusMessage("Error flushing log during shutdown", LogLevel.Warn)));
            }
        }

        Application.Current.Exit(); // Win32
    }

    /// <summary>
    ///     Write the current StoryModel to the backing project file
    /// </summary>
    public async Task WriteModel()
    {
        logger.Log(LogLevel.Info, $"In WriteModel, path={appState.CurrentDocument?.FilePath}");
        try
        {
            // Updating the last modified time
            try
            {
                var overview =
                    appState.CurrentDocument.Model.StoryElements.StoryElementGuids[
                        appState.CurrentDocument.Model.ExplorerView[0].Uuid] as OverviewModel;
                overview!.DateModified = DateTime.Today.ToString("yyyy-MM-dd");
            }
            catch
            {
                logger.Log(LogLevel.Warn, "Failed to update last modified date/time");
            }

            // Use the file path if available, otherwise fallback to the old path
            await outlineService.WriteModel(appState.CurrentDocument.Model, appState.CurrentDocument.FilePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogException(LogLevel.Warn, ex.InnerException, "User doesn't have perms to access this path.");
            //Catch write permission exceptions
            await window.ShowContentDialog(
                new ContentDialog
                {
                    Title = "Access error",
                    Content = $"""
                               StoryCAD does not have permission to write to this location.
                               You outline will now be saved at:
                               {preferences.Model.ProjectDirectory}
                               """,
                    PrimaryButtonText = "Okay"
                },
                true
            );

            // Reset to default location
            appState.CurrentDocument.FilePath = Path.Combine(preferences.Model.ProjectDirectory,
                Path.GetFileName(appState.CurrentDocument.FilePath)!);

            // Last opened file with reference to this version
            preferences.Model.RecentFiles.Insert(0, appState.CurrentDocument.FilePath);
        }
        catch (Exception ex)
        {
            logger.LogException(LogLevel.Error, ex, $"Error writing file {ex.Message} {ex.Source}");
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Error writing file - see log", LogLevel.Error)));
            return;
        }

        logger.Log(LogLevel.Info, "WriteModel successful");
    }

    #region Reports and Tools

    public void SearchNodes()
    {
        using (var serializationLock = new SerializationLock(logger))
        {
            logger.Log(LogLevel.Info, $"Search started, Searching for {shellVm.FilterText}");
            _editFlushService.FlushCurrentEdits();
            if (appState.CurrentDocument?.Model?.CurrentView == null ||
                appState.CurrentDocument.Model.CurrentView.Count == 0)
            {
                logger.Log(LogLevel.Info, "Data source is null or Empty.");
                Messenger.Send(
                    new StatusChangedMessage(new StatusMessage("You need to load a story first!", LogLevel.Warn)));
                return;
            }

            var searchTotal = 0;

            foreach (var node in appState.CurrentDocument.Model.CurrentView[0])
            {
                //checks if node name contains the thing we are looking for
                if (searchService.SearchString(node, shellVm.FilterText, appState.CurrentDocument.Model))
                {
                    searchTotal++;
                    if (window.RequestedTheme == ElementTheme.Light)
                    {
                        node.Background = new SolidColorBrush(Colors.LightGoldenrodYellow);
                    }
                    else
                    {
                        node.Background = new SolidColorBrush(Colors.DarkGoldenrod);
                    } //Light Goldenrod is hard to read in dark theme

                    node.IsExpanded = true;

                    var parent = node.Parent;
                    if (parent != null)
                    {
                        while (!parent.IsRoot)
                        {
                            parent.IsExpanded = true;
                            parent = parent.Parent;
                        }

                        if (parent.IsRoot)
                        {
                            parent.IsExpanded = true;
                        }
                    }
                }
                else
                {
                    node.Background = null;
                }
            }

            switch (searchTotal)
            {
                case 0:
                    Messenger.Send(
                        new StatusChangedMessage(new StatusMessage("Found no matches", LogLevel.Info, true)));
                    break;
                case 1:
                    Messenger.Send(new StatusChangedMessage(new StatusMessage("Found 1 match", LogLevel.Info, true)));
                    break;
                default:
                    Messenger.Send(new StatusChangedMessage(new StatusMessage($"Found {searchTotal} matches",
                        LogLevel.Info, true)));
                    break;
            }
        }
    }

    public async Task GenerateScrivenerReports()
    {
        if (appState.CurrentDocument?.Model?.CurrentView == null ||
            appState.CurrentDocument.Model.CurrentView.Count == 0)
        {
            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("You need to open a story first!", LogLevel.Info)));
            logger.Log(LogLevel.Info, "Scrivener Report cancelled (CurrentView was null or empty)");
            return;
        }

        //TODO: revamp this to be more user-friendly.
        using (var serializationLock = new SerializationLock(logger))
        {
            _editFlushService.FlushCurrentEdits();

            // Select the Scrivener .scrivx file to add the report to
            var file = await window.ShowFilePicker("Open file", ".scrivx");
            if (file != null)
            {
                shellVm.Scrivener.ScrivenerFile = file;
                shellVm.Scrivener.ProjectPath = Path.GetDirectoryName(file.Path);
                if (!await shellVm.Scrivener.IsScrivenerRelease3())
                {
                    throw new ApplicationException("Project is not Scrivener Release 3");
                }

                // Load the Scrivener project file's model
                ScrivenerReports rpt = new(file, appState);
                await rpt.GenerateReports();
            }

            Messenger.Send(new StatusChangedMessage(new StatusMessage("Generate Scrivener reports completed",
                LogLevel.Info, true)));
        }
    }

    public Task PrintCurrentNodeAsync()
    {
        using (var serializationLock = new SerializationLock(logger))
        {
            if (shellVm.RightTappedNode == null)
            {
                Messenger.Send(new StatusChangedMessage(new StatusMessage("Right tap a node to print", LogLevel.Warn)));
                logger.Log(LogLevel.Info, "Print node failed as no node is selected");
                return Task.CompletedTask;
            }

            Ioc.Default.GetRequiredService<PrintReportDialogVM>().PrintSingleNode(shellVm.RightTappedNode);
        }

        return Task.CompletedTask;
    }

    public async Task KeyQuestionsTool()
    {
        logger.Log(LogLevel.Info, "Displaying KeyQuestions tool dialog");
        using (var serializationLock = new SerializationLock(logger))
        {
            if (shellVm.RightTappedNode == null)
            {
                shellVm.RightTappedNode = shellVm.CurrentNode;
            }

            //Creates and shows dialog
            ContentDialog keyQuestionsDialog = new()
            {
                Title = "Key questions",
                CloseButtonText = "Close",
                Content = new KeyQuestionsDialog()
            };
            await window.ShowContentDialog(keyQuestionsDialog);

            Ioc.Default.GetRequiredService<KeyQuestionsViewModel>().NextQuestion();

            logger.Log(LogLevel.Info, "KeyQuestions finished");
        }
    }

    public async Task TopicsTool()
    {
        logger.Log(LogLevel.Info, "Displaying Topics tool dialog");
        using (var serializationLock = new SerializationLock(logger))
        {
            if (shellVm.RightTappedNode == null)
            {
                shellVm.RightTappedNode = shellVm.CurrentNode;
            }

            ContentDialog dialog = new()
            {
                Title = "Topic Information",
                CloseButtonText = "Done",
                Content = new TopicsDialog()
            };
            await window.ShowContentDialog(dialog);
        }

        logger.Log(LogLevel.Info, "Topics finished");
    }

    /// <summary>
    ///     This shows the master plot dialog
    /// </summary>
    public async Task MasterPlotTool()
    {
        using (var serializationLock = new SerializationLock(logger))
        {
            logger.Log(LogLevel.Info, "Displaying MasterPlot tool dialog");
            if (VerifyToolUse(true, true))
            {
                ContentDialog dialog = null;
                if (!appState.Headless)
                {
                    //Creates and shows content dialog
                    dialog = new ContentDialog
                    {
                        Title = "Master plots",
                        PrimaryButtonText = "Copy",
                        SecondaryButtonText = "Cancel",
                        Content = new MasterPlotsDialog()
                    };
                }

                var result = await window.ShowContentDialog(dialog);

                if (result == ContentDialogResult.Primary) // Copy command
                {
                    var masterPlotsVm = Ioc.Default.GetRequiredService<MasterPlotsViewModel>();
                    var masterPlotName = masterPlotsVm.PlotPatternName;
                    var model = masterPlotsVm.MasterPlots[masterPlotName];
                    IList<PlotPatternScene> scenes = model.PlotPatternScenes;
                    var problem = new ProblemModel(masterPlotName, appState.CurrentDocument.Model,
                        shellVm.RightTappedNode);
                    // add the new ProblemModel & node to the end of the target (shellVm.RightTappedNode) children
                    shellVm.RightTappedNode.IsExpanded = true;
                    problem.Node.IsSelected = true;
                    problem.Node.IsExpanded = true;
                    if (scenes.Count == 1)
                    {
                        problem.Description = "See Notes.";
                        problem.Notes = scenes[0].Notes;
                    }
                    else
                    {
                        foreach (var scene in scenes)
                        {
                            SceneModel child = new(appState.CurrentDocument.Model, shellVm.RightTappedNode)
                                { Name = scene.SceneTitle, Description = "See Notes.", Notes = scene.Notes };

                            child.Node.IsSelected = true;
                        }
                    }

                    Messenger.Send(new StatusChangedMessage(new StatusMessage(
                        $"MasterPlot {masterPlotName} inserted", LogLevel.Info, true)));
                    Messenger.Send(new IsChangedMessage(true));
                    logger.Log(LogLevel.Info, "MasterPlot complete");
                }
            }
        }
    }

    public async Task DramaticSituationsTool()
    {
        logger.Log(LogLevel.Info, "Displaying Dramatic Situations tool dialog");
        using (var serializationLock = new SerializationLock(logger))
        {
            if (shellVm.RightTappedNode == null)
            {
                shellVm.ShowMessage(LogLevel.Warn, "Right tap a node to insert a dramatic situation", false);
            }

            if (VerifyToolUse(true, true))
            {
                ContentDialog dialog = null;
                if (!appState.Headless)
                {
                    //Creates and shows dialog
                    dialog = new ContentDialog
                    {
                        Title = "Dramatic situations",
                        PrimaryButtonText = "Copy as problem",
                        SecondaryButtonText = "Copy as scene",
                        CloseButtonText = "Cancel",
                        Content = new DramaticSituationsDialog()
                    };
                }

                var result = await window.ShowContentDialog(dialog);

                var situationModel =
                    Ioc.Default.GetRequiredService<DramaticSituationsViewModel>().Situation;

                // If no situation was selected (null or user cancelled), return early
                if (situationModel == null || result == ContentDialogResult.None)
                {
                    logger.Log(LogLevel.Info, "Dramatic situations tool cancelled or no situation selected");
                    return;
                }

                string msg;

                if (result == ContentDialogResult.Primary)
                {
                    ProblemModel problem = new(situationModel.SituationName, appState.CurrentDocument.Model,
                        shellVm.RightTappedNode)
                    {
                        Description = "See Notes.",
                        Notes = situationModel.Notes
                    };

                    // Insert the new Problem as the target's child
                    msg = $"Problem {situationModel.SituationName} inserted";
                    Messenger.Send(new IsChangedMessage(true));
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    SceneModel sceneVar = new(situationModel.SituationName, appState.CurrentDocument.Model,
                        shellVm.RightTappedNode)
                    {
                        Description = "See Notes.",
                        Notes = situationModel.Notes
                    };
                    // Insert the new Scene as the target's child
                    msg = $"Scene {situationModel.SituationName} inserted";
                    Messenger.Send(new IsChangedMessage(true));
                }
                else
                {
                    msg = "Dramatic Situation tool cancelled";
                }

                logger.Log(LogLevel.Info, msg);
                Messenger.Send(new StatusChangedMessage(new StatusMessage(msg, LogLevel.Info, true)));
            }
        }

        logger.Log(LogLevel.Info, "Dramatic Situations finished");
    }

    /// <summary>
    ///     This loads the stock scenes dialog in the Plotting Aids submenu
    /// </summary>
    public async Task StockScenesTool()
    {
        logger.Log(LogLevel.Info, "Displaying Stock Scenes tool dialog");
        if (VerifyToolUse(true, true))
        {
            using (var serializationLock = new SerializationLock(logger))
            {
                try
                {
                    //Creates and shows dialog
                    ContentDialog dialog = null;

                    if (!appState.Headless)
                    {
                        dialog = new ContentDialog
                        {
                            Title = "Stock scenes",
                            Content = new StockScenesDialog(),
                            PrimaryButtonText = "Add Scene",
                            CloseButtonText = "Cancel"
                        };
                    }

                    var result = await window.ShowContentDialog(dialog);

                    if (result == ContentDialogResult.Primary) // Copy command
                    {
                        if (string.IsNullOrWhiteSpace(Ioc.Default.GetRequiredService<StockScenesViewModel>().SceneName))
                        {
                            Messenger.Send(new StatusChangedMessage(new StatusMessage(
                                "You need to select a stock scene",
                                LogLevel.Warn)));
                            return;
                        }

                        SceneModel sceneVar = new(Ioc.Default.GetRequiredService<StockScenesViewModel>().SceneName,
                            appState.CurrentDocument.Model, shellVm.RightTappedNode);

                        shellVm._sourceChildren = shellVm.RightTappedNode.Children;
                        shellVm.TreeViewNodeClicked(sceneVar.Node);
                        shellVm.RightTappedNode.IsExpanded = true;
                        sceneVar.Node.IsSelected = true;
                        Messenger.Send(
                            new StatusChangedMessage(new StatusMessage("Stock Scenes inserted", LogLevel.Info)));
                    }
                    else
                    {
                        Messenger.Send(
                            new StatusChangedMessage(new StatusMessage("Stock Scenes canceled", LogLevel.Warn)));
                    }
                }
                catch (Exception e)
                {
                    logger.LogException(LogLevel.Error, e, e.Message);
                }
            }
        }
    }


    /// <summary>
    ///     Verify that the tool being called has its prerequisites met.
    /// </summary>
    /// <param name="explorerViewOnly">This tool can only run in StoryExplorer view</param>
    /// <param name="nodeRequired">A node (right-clicked or clicked) must be present</param>
    /// <param name="checkOutlineIsOpen">A checks an outline is open (defaults to true)</param>
    /// <returns>true if prerequisites are met</returns>
    public bool VerifyToolUse(bool explorerViewOnly, bool nodeRequired, bool checkOutlineIsOpen = true)
    {
        try
        {
            if (explorerViewOnly && shellVm.CurrentViewType != StoryViewType.ExplorerView)
            {
                Messenger.Send(new StatusChangedMessage(new StatusMessage(
                    "This tool can only be run in Story Explorer view", LogLevel.Warn)));
                return false;
            }

            if (checkOutlineIsOpen)
            {
                if (appState.CurrentDocument?.Model == null)
                {
                    Messenger.Send(
                        new StatusChangedMessage(new StatusMessage("Open or create an outline first", LogLevel.Warn)));
                    return false;
                }

                if (shellVm.CurrentViewType == StoryViewType.ExplorerView &&
                    appState.CurrentDocument.Model.ExplorerView.Count == 0)
                {
                    Messenger.Send(
                        new StatusChangedMessage(new StatusMessage("Open or create an outline first", LogLevel.Warn)));
                    return false;
                }

                if (shellVm.CurrentViewType == StoryViewType.NarratorView &&
                    appState.CurrentDocument.Model.NarratorView.Count == 0)
                {
                    Messenger.Send(
                        new StatusChangedMessage(new StatusMessage("Open or create an outline first", LogLevel.Warn)));
                    return false;
                }
            }

            if (nodeRequired)
            {
                if (shellVm.RightTappedNode == null)
                {
                    shellVm.RightTappedNode = shellVm.CurrentNode;
                }

                if (shellVm.RightTappedNode == null)
                {
                    Messenger.Send(
                        new StatusChangedMessage(new StatusMessage("You need to select a node first", LogLevel.Warn)));
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogException(LogLevel.Error, ex, "Error in ShellVM.VerifyToolUse()");
            return false; // Return false to prevent any issues.
        }
    }

    #endregion

    #region Add and Remove Story Element Commands

    /// <summary>
    ///     Adds a new StoryElement to the open StoryModel
    /// </summary>
    /// <param name="typeToAdd">Element type that you want to add</param>
    public void AddStoryElement(StoryItemType typeToAdd)
    {
        using (var serializationLock = new SerializationLock(logger))
        {
            logger.Log(LogLevel.Info, $"Adding StoryElement {typeToAdd}");
            if (shellVm.RightTappedNode == null)
            {
                Messenger.Send(
                    new StatusChangedMessage(new StatusMessage("Right tap a node to add to", LogLevel.Warn)));
                logger.Log(LogLevel.Info, "Add StoryElement failed- node not selected");
                return;
            }

            if (StoryNodeItem.RootNodeType(shellVm.RightTappedNode) == StoryItemType.TrashCan)
            {
                Messenger.Send(new StatusChangedMessage(new StatusMessage("Cannot add add to Deleted Items",
                    LogLevel.Warn, true)));
                return;
            }

            //Create new element via outline service
            var newNode =
                outlineService.AddStoryElement(appState.CurrentDocument.Model, typeToAdd, shellVm.RightTappedNode);

            newNode.Node.Parent.IsExpanded = true;
            newNode.IsSelected = false;
            newNode.Node.Background = window.ContrastColor;
            shellVm.NewNodeHighlightCache.Add(newNode.Node);
            logger.Log(LogLevel.Info, $"Added Story Element {newNode.Uuid}");

            Messenger.Send(new IsChangedMessage(true));
            Messenger.Send(new StatusChangedMessage(new StatusMessage($"Added new {typeToAdd}", LogLevel.Info, true)));

            shellVm.TreeViewNodeClicked(newNode, false);
        }
    }

    public async Task RemoveStoryElement()
    {
        try
        {
            if (shellVm.RightTappedNode == null)
            {
                Messenger.Send(
                    new StatusChangedMessage(new StatusMessage("Right tap a node to delete", LogLevel.Warn)));
                return;
            }

            var _delete = true;
            var elementToDelete = shellVm.RightTappedNode.Uuid;

            // Collect all GUIDs (element + all children)
            var allGuids = outlineService.CollectAllDescendantGuids(shellVm.RightTappedNode, appState.CurrentDocument.Model);

            // Find references to ANY of these GUIDs
            var _foundElements = new List<StoryElement>();
            foreach (var guid in allGuids)
            {
                var refs = outlineService.FindElementReferences(appState.CurrentDocument.Model, guid);
                foreach (var refElement in refs)
                {
                    if (!_foundElements.Contains(refElement))
                    {
                        _foundElements.Add(refElement);
                    }
                }
            }

            var state = appState;
            //Only warns if it finds a node its referenced in
            if (_foundElements.Count > 0 && !state.Headless)
            {
                //Creates UI
                StackPanel _content = new();
                _content.Children.Add(new TextBlock
                    { Text = "The following nodes will be updated to remove references to this node:" });
                _content.Children.Add(new ListView
                {
                    ItemsSource = _foundElements, DisplayMemberPath = "Name",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center, Height = 300, Width = 480
                });

                //Creates dialog and then shows it
                ContentDialog _Dialog = new()
                {
                    Content = _content,
                    Title = "Are you sure you want to delete this node?",
                    Width = 500,
                    PrimaryButtonText = "Confirm",
                    SecondaryButtonText = "Cancel"
                };

                //Handle content dialog result
                if (await window.ShowContentDialog(_Dialog) !=
                    ContentDialogResult.Primary)
                {
                    _delete = false;
                }
            }
            else
            {
                _delete = true;
            }

            // Go through with delete
            if (_delete)
            {
                using (var serializationLock = new SerializationLock(logger))
                {
                    try
                    {
                        // Get the element to move to trash
                        var element =
                            outlineService.GetStoryElementByGuid(appState.CurrentDocument.Model, elementToDelete);
                        if (element != null)
                        {
                            // Use the new OutlineService method to move to trash
                            outlineService.MoveToTrash(element, appState.CurrentDocument.Model);

                            // Clear the selected nodes to prevent issues (fix for #1056)
                            if (shellVm.CurrentNode?.Uuid == elementToDelete)
                            {
                                shellVm.CurrentNode = null;
                            }

                            if (shellVm.RightTappedNode?.Uuid == elementToDelete)
                            {
                                shellVm.RightTappedNode = null;
                            }

                            // Mark the model as changed
                            Messenger.Send(new IsChangedMessage(true));
                            Messenger.Send(
                                new StatusChangedMessage(new StatusMessage("Element moved to trash", LogLevel.Info)));
                        }
                        else
                        {
                            Messenger.Send(
                                new StatusChangedMessage(new StatusMessage("Element not found", LogLevel.Error)));
                        }
                    }
                    catch (Exception ex)
                    {
                        Messenger.Send(new StatusChangedMessage(
                            new StatusMessage($"Failed to move element to trash: {ex.Message}", LogLevel.Error)));
                    }
                }
            }
        }
        catch (InvalidOperationException)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("You cannot delete this node", LogLevel.Warn)));
        }
        catch (Exception e)
        {
            logger.LogException(LogLevel.Error, e, "Error deleting node");
        }
    }

    public void RestoreStoryElement()
    {
        logger.Log(LogLevel.Trace, "RestoreStoryElement");
        if (shellVm.RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Right tap a node to restore", LogLevel.Warn)));
            return;
        }

        try
        {
            using (var serializationLock = new SerializationLock(logger))
            {
                // Use the new OutlineService method to restore from trash
                outlineService.RestoreFromTrash(shellVm.RightTappedNode, appState.CurrentDocument.Model);

                // Mark the model as changed
                Messenger.Send(new IsChangedMessage(true));

                Messenger.Send(new StatusChangedMessage(new StatusMessage(
                    $"Restored node {shellVm.RightTappedNode.Name} and all its contents", LogLevel.Info, true)));
            }
        }
        catch (InvalidOperationException ex)
        {
            // Handle validation errors from the service
            Messenger.Send(new StatusChangedMessage(new StatusMessage(ex.Message, LogLevel.Warn)));
        }
        catch (Exception e)
        {
            logger.LogException(LogLevel.Error, e, "Error restoring node");
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Error restoring element", LogLevel.Error)));
        }
    }

    /// <summary>
    ///     Add a Scene StoryNodeItem to the end of the Narrative view
    ///     by copying from the Scene's StoryNodeItem in the ExplorerView
    ///     view.
    /// </summary>
    public void CopyToNarrative()
    {
        logger.Log(LogLevel.Trace, "CopyToNarrative");
        if (shellVm.RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Select a node to copy", LogLevel.Info)));
            return;
        }

        if (shellVm.RightTappedNode.Type != StoryItemType.Scene)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("You can only copy a scene", LogLevel.Warn)));
            return;
        }

        var _sceneVar =
            (SceneModel)outlineService.GetStoryElementByGuid(appState.CurrentDocument.Model,
                shellVm.RightTappedNode.Uuid);
        _ = new StoryNodeItem(_sceneVar, appState.CurrentDocument.Model.NarratorView[0]);
        Messenger.Send(new IsChangedMessage(true));
        Messenger.Send(new StatusChangedMessage(new StatusMessage(
            $"Copied node {shellVm.RightTappedNode.Name} to Narrative View", LogLevel.Info, true)));
    }

    /// <summary>
    ///     Clears trash
    /// </summary>
    public void EmptyTrash()
    {
        if (appState.CurrentDocument?.Model == null)
        {
            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("You need to load a story first!", LogLevel.Warn)));
            logger.Log(LogLevel.Info, "Failed to empty trash - no story loaded.");
            return;
        }

        try
        {
            using (var serializationLock = new SerializationLock(logger))
            {
                // Check if trash has items before attempting to empty
                var trashCanNode =
                    appState.CurrentDocument.Model.TrashView?.FirstOrDefault(n => n.Type == StoryItemType.TrashCan);
                if (trashCanNode == null || trashCanNode.Children.Count == 0)
                {
                    logger.Log(LogLevel.Info, "Trash is already empty.");
                    Messenger.Send(new StatusChangedMessage(new StatusMessage("No Deleted StoryElements to empty",
                        LogLevel.Info)));

                    // Fix error #1056 - clear selected nodes even when trash is empty
                    shellVm.RightTappedNode = null;
                    shellVm.CurrentNode = null;
                    return;
                }

                try
                {
                    // Use the new OutlineService method to empty trash
                    outlineService.EmptyTrash(appState.CurrentDocument.Model);

                    shellVm.StatusMessage = "Trash Emptied.";
                    logger.Log(LogLevel.Info, "Emptied Trash.");

                    // Fix error #1056 - clear selected nodes
                    shellVm.RightTappedNode = null;
                    shellVm.CurrentNode = null;

                    // Mark the model as changed
                    Messenger.Send(new IsChangedMessage(true));
                }
                catch (Exception ex)
                {
                    Messenger.Send(new StatusChangedMessage(new StatusMessage($"Failed to empty trash: {ex.Message}",
                        LogLevel.Error)));
                }
            }
        }
        catch (Exception e)
        {
            logger.LogException(LogLevel.Error, e, "Error emptying trash");
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Error emptying trash", LogLevel.Error)));
        }
    }

    /// <summary>
    ///     Remove a TreeViewItem from the Narrative view for a copied Scene.
    /// </summary>
    public void RemoveFromNarrative()
    {
        logger.Log(LogLevel.Trace, "RemoveFromNarrative");

        if (shellVm.RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Select a node to remove", LogLevel.Info)));
            return;
        }

        if (shellVm.RightTappedNode.Type != StoryItemType.Scene)
        {
            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("You can only remove a Scene copy", LogLevel.Info)));
            return;
        }

        foreach (var _item in appState.CurrentDocument.Model.NarratorView[0].Children.ToList())
        {
            if (_item.Uuid == shellVm.RightTappedNode.Uuid)
            {
                appState.CurrentDocument.Model.NarratorView[0].Children.Remove(_item);
                Messenger.Send(new IsChangedMessage(true));
                Messenger.Send(new StatusChangedMessage(new StatusMessage(
                    $"Removed node {shellVm.RightTappedNode.Name} from Narrative View", LogLevel.Info, true)));
                return;
            }
        }

        Messenger.Send(new StatusChangedMessage(
            new StatusMessage($"Node {shellVm.RightTappedNode.Name} not in Narrative View", LogLevel.Info, true)));
    }

    /// <summary>
    ///     Convert the currently selected Problem to a Scene.
    /// </summary>
    public void ConvertProblemToScene()
    {
        if (shellVm.RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Select a node to convert", LogLevel.Info)));
            return;
        }

        if (shellVm.RightTappedNode.Type != StoryItemType.Problem)
        {
            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("You can only convert a Problem", LogLevel.Warn)));
            return;
        }

        var problem =
            (ProblemModel)outlineService.GetStoryElementByGuid(appState.CurrentDocument.Model,
                shellVm.RightTappedNode.Uuid);
        var scene = outlineService.ConvertProblemToScene(appState.CurrentDocument.Model, problem);
        shellVm.TreeViewNodeClicked(scene.Node, false);
        Messenger.Send(new StatusChangedMessage(new StatusMessage("Converted Problem to Scene", LogLevel.Info, true)));
    }

    /// <summary>
    ///     Convert the currently selected Scene to a Problem.
    /// </summary>
    public void ConvertSceneToProblem()
    {
        if (shellVm.RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Select a node to convert", LogLevel.Info)));
            return;
        }

        if (shellVm.RightTappedNode.Type != StoryItemType.Scene)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("You can only convert a Scene", LogLevel.Warn)));
            return;
        }

        var scene = (SceneModel)outlineService.GetStoryElementByGuid(appState.CurrentDocument.Model,
            shellVm.RightTappedNode.Uuid);
        var problem = outlineService.ConvertSceneToProblem(appState.CurrentDocument.Model, scene);
        shellVm.TreeViewNodeClicked(problem.Node, false);
        Messenger.Send(new StatusChangedMessage(new StatusMessage("Converted Scene to Problem", LogLevel.Info, true)));
    }

    #endregion
}
