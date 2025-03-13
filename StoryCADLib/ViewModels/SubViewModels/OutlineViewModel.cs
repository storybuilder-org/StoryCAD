using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Messages;
using Windows.Storage;
using Microsoft.UI;
using StoryCAD.Services;
using StoryCAD.Services.Outline;
using CommunityToolkit.Mvvm.Messaging;
using StoryCAD.DAL;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StoryCAD.Services.Backend;
using StoryCAD.Services.Dialogs;
using StoryCAD.Services.Dialogs.Tools;
using StoryCAD.ViewModels.Tools;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Reports;
using StoryCAD.Services.Search;

namespace StoryCAD.ViewModels.SubViewModels;

public class OutlineViewModel : ObservableRecipient
{
    private readonly LogService logger;
    private readonly PreferenceService preferences;
    private readonly Windowing window;
    private readonly OutlineService outlineService;
    private readonly SearchService searchService;
    // The reference to ShellViewModel is temporary
    // until the ShellViewModel is refactored to fully
    // use OutlineViewModel for outline methods.

    /// <summary>
    /// Private backing store for ShellVM since we can't
    /// access it immediately on the constructor of this class
    /// so we have shellVm as a property to get it when needed but
    /// </summary>
    private ShellViewModel? _shellVM;

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

    /// <summary>
    /// Path to outline file.
    /// </summary>
    public string StoryModelFile;

    /// <summary>
    /// Current Outline being edited
    /// </summary>
    public StoryModel StoryModel = new();

    /// <summary>
    /// Lock that controls the UI, Autosave, Backup.
    /// </summary>
    public bool _canExecuteCommands;

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
            shellVm.SaveModel();
            await outlineService.WriteModel(StoryModel, StoryModelFile);
        }

        // Stop the auto save service if it was running
        if (preferences.Model.AutoSave) { shellVm._autoSaveService.StopAutoSave(); }

        // Stop the timed backup service if it was running
        Ioc.Default.GetRequiredService<BackupService>().StopTimedBackup();

        _canExecuteCommands = false;
        logger.Log(LogLevel.Info, "Executing OpenFile command");

        try
        {
            // Reset the model and show the home page
            shellVm.ResetModel();
            shellVm.ShowHomePage();

            // Open file picker if `fromPath` is not provided or file doesn't exist at the path.
            if (fromPath == "" || !File.Exists(fromPath))
            {
                logger.Log(LogLevel.Info, "Opening file picker as story wasn't able to be found");

                StorageFile? projectFile = await window.ShowFilePicker("Open Project File", ".stbx");
                if (projectFile == null) //Picker was canceled.
                {
                    logger.Log(LogLevel.Info, "Open file picker cancelled.");
                    _canExecuteCommands = true; // unblock other commands
                    StoryModelFile = string.Empty;
                    return;
                }
            }

            StoryModelFile = fromPath;
            if (StoryModelFile == null)
            {
                logger.Log(LogLevel.Warn, "Open File command failed: StoryModel.ProjectFile is null.");
                Messenger.Send(new StatusChangedMessage(new("Open Story command cancelled", LogLevel.Info)));
                _canExecuteCommands = true;  // Unblock other commands
                return;
            }

            if (!File.Exists(StoryModelFile))
            {
                Messenger.Send(new StatusChangedMessage(
                    new($"Cannot find file {StoryModelFile}", LogLevel.Warn, true)));
                _canExecuteCommands = true;
                return;
            }

            //Check file is available.
            StoryIO rdr = Ioc.Default.GetRequiredService<StoryIO>();
            if (!await rdr.CheckFileAvailability(StoryModelFile))
            {
                Messenger.Send(new StatusChangedMessage(new("File Unavailable.", LogLevel.Warn,true)));
                return;
            }

            //Read file
            StoryModel = await outlineService.OpenFile(StoryModelFile);

            //Check the file we loaded actually has StoryCAD Data.
            if (StoryModel == null)
            {
                Messenger.Send(new StatusChangedMessage(
                    new("Unable to open file (No Story Elements found)", LogLevel.Warn, true)));
                _canExecuteCommands = true;  // unblock other commands
                return;
            }

            if (StoryModel.StoryElements.Count == 0)
            {
                Messenger.Send(new StatusChangedMessage(
                    new("Unable to open file (No Story Elements found)", LogLevel.Warn, true)));
                _canExecuteCommands = true;  // unblock other commands
                return;

            }

            // Take a backup of the project if the user has the 'backup on open' preference set.
            if (preferences.Model.BackupOnOpen)
            {
                await Ioc.Default.GetRequiredService<BackupService>().BackupProject();
            }

            // Set the current view to the ExplorerView 
            if (StoryModel.ExplorerView.Count > 0)
            {
                shellVm.SetCurrentView(StoryViewType.ExplorerView);
                Messenger.Send(new StatusChangedMessage(new("Open Story completed", LogLevel.Info)));
            }

            window.UpdateWindowTitle();
            new UnifiedVM().UpdateRecents(StoryModelFile);

            if (preferences.Model.TimedBackup)
            {
                Ioc.Default.GetRequiredService<BackupService>().StartTimedBackup();
            }

            if (preferences.Model.AutoSave)
            {
                shellVm._autoSaveService.StartAutoSave();
            }

            string msg = $"Opened project {StoryModelFile}";
            logger.Log(LogLevel.Info, msg);
        }
        catch (Exception ex)
        {
            // Report the error to the user
            logger.LogException(LogLevel.Error, ex, "Error in OpenFile command");
            Messenger.Send(new StatusChangedMessage(new("Open Story command failed", LogLevel.Error)));
        }

        logger.Log(LogLevel.Info, "Open Story completed.");
        _canExecuteCommands = true;
    }

    public async Task CreateFile(UnifiedVM dialogVm)
    {
        logger.Log(LogLevel.Info, "FileOpenVM - New File starting");
        _canExecuteCommands = false;

        try
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("New project command executing", LogLevel.Info)), true);

            // If the current project needs saved, do so
            if (StoryModel.Changed && StoryModelFile != null)
            {
                await outlineService.WriteModel(StoryModel, StoryModelFile);
            }

            // Start with a blank StoryModel
            shellVm.ResetModel();
            shellVm.ShowHomePage();

            // Ensure the filename has .stbx extension
            if (!Path.GetExtension(dialogVm.ProjectName)!.Equals(".stbx"))
            {
                dialogVm.ProjectName += ".stbx";
            }

            // Create the new outline's file
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(dialogVm.ProjectPath);
            StoryModelFile = (await folder.CreateFileAsync(dialogVm.ProjectName, CreationCollisionOption.GenerateUniqueName)).Path;

            // Create the StoryModel
            string name = Path.GetFileNameWithoutExtension(StoryModelFile);
            string author = preferences.Model.FirstName + " " + preferences.Model.LastName;

            // Create the new project StorageFile; throw an exception if it already exists.
            StoryModel = await outlineService.CreateModel(name, author, dialogVm.SelectedTemplateIndex);

            shellVm.SetCurrentView(StoryViewType.ExplorerView);

            Ioc.Default.GetRequiredService<UnifiedVM>().UpdateRecents(StoryModelFile);

            StoryModel.Changed = true;
            await SaveFile();

            if (preferences.Model.BackupOnOpen)
            {
                await shellVm.MakeBackup();
            }

            // Start the timed backup and auto save services
            if (preferences.Model.TimedBackup)
            {
                Ioc.Default.GetRequiredService<BackupService>().StartTimedBackup();
            }
            if (preferences.Model.AutoSave)
            {
                Ioc.Default.GetRequiredService<AutoSaveService>().StartAutoSave();
            }

            shellVm.TreeViewNodeClicked(StoryModel.ExplorerView[0]);
            window.UpdateWindowTitle();

            Messenger.Send(new StatusChangedMessage(new("New project command completed", LogLevel.Info, true)), true);
        }
        catch (Exception ex)
        {
            Messenger.Send(new StatusChangedMessage(new("Error creating new project", LogLevel.Error)), true);
        }

        _canExecuteCommands = true;
    }

    public async Task OpenUnifiedMenu()
    {
        if (_canExecuteCommands)
        {
            _canExecuteCommands = false;
            // Needs logging
            shellVm._contentDialog = new() { Content = new UnifiedMenuPage() };
            if (window.RequestedTheme == ElementTheme.Light)
            {
                shellVm._contentDialog.RequestedTheme = window.RequestedTheme;
                shellVm._contentDialog.Background = new SolidColorBrush(Colors.LightGray);
            }
            await window.ShowContentDialog(shellVm._contentDialog);
            _canExecuteCommands = true;
        }
    }

    /// <summary>
    /// Save the currently active page from 
    /// </summary>
    /// <param name="autoSave"></param>
    /// <returns></returns>
    public async Task SaveFile(bool autoSave = false)
    {
        shellVm._autoSaveService.StopAutoSave();
        bool saveExecuteCommands = _canExecuteCommands;
        _canExecuteCommands = false;
        string msg = autoSave ? "AutoSave" : "SaveFile command";
        if (autoSave && !StoryModel.Changed)
        {
            logger.Log(LogLevel.Info, $"{msg} skipped, no changes");
            _canExecuteCommands = true;
            return;
        }

        if (StoryModel.StoryElements.Count == 0)
        {
            Messenger.Send(new StatusChangedMessage(new("You need to open a story first!", LogLevel.Info)));
            logger.Log(LogLevel.Info, $"{msg} cancelled (StoryModel.ProjectFile was null)");
            _canExecuteCommands = true;
            return;
        }

        try
        {
            Messenger.Send(new StatusChangedMessage(new($"{msg} executing", LogLevel.Info)));
            shellVm.SaveModel();
            await outlineService.WriteModel(StoryModel, StoryModelFile);
            Messenger.Send(new StatusChangedMessage(new($"{msg} completed", LogLevel.Info)));
            StoryModel.Changed = false;
            shellVm.ChangeStatusColor = Colors.Green;
        }
        catch (Exception ex)
        {
            logger.LogException(LogLevel.Error, ex, $"Exception in {msg}");
            Messenger.Send(new StatusChangedMessage(new($"{msg} failed", LogLevel.Error)));
        }
        _canExecuteCommands = saveExecuteCommands;
        shellVm._autoSaveService.StartAutoSave();
    }

    public async void SaveFileAs()
    {
        if (_canExecuteCommands)
        {
            _canExecuteCommands = false;
            Messenger.Send(new StatusChangedMessage(new("Save File As command executing", LogLevel.Info, true)));
            try
            {
                if (string.IsNullOrEmpty(StoryModelFile))
                {
                    Messenger.Send(new StatusChangedMessage(new("You need to load a story first!", LogLevel.Info)));
                    logger.Log(LogLevel.Warn, "User tried to use save as without a story loaded.");
                    _canExecuteCommands = true;
                    return;
                }

                // Create the content dialog
                ContentDialog saveAsDialog = new()
                {
                    Title = "Save as",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Cancel",
                    Content = new SaveAsDialog()
                };

                // Set default values in the view model using the current story file info
                SaveAsViewModel saveAsVm = Ioc.Default.GetRequiredService<SaveAsViewModel>();
                saveAsVm.ProjectName = Path.GetFileName(StoryModelFile);
                saveAsVm.ParentFolder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(StoryModelFile));

                ContentDialogResult result = await window.ShowContentDialog(saveAsDialog);

                if (result == ContentDialogResult.Primary)
                {
                    if (await VerifyReplaceOrCreate())
                    {
                        // Save the model to disk at the current file location
                        shellVm.SaveModel();
                        await outlineService.WriteModel(StoryModel, StoryModelFile);

                        // If the new path is the same as the current one, exit early
                        string newFilePath = Path.Combine(saveAsVm.ParentFolder.Path, saveAsVm.ProjectName);
                        if (newFilePath.Equals(StoryModelFile, StringComparison.OrdinalIgnoreCase))
                        {
                            Messenger.Send(new StatusChangedMessage(new("Save File As command completed", LogLevel.Info)));
                            logger.Log(LogLevel.Info, "User tried to save file to same location as current file.");
                            _canExecuteCommands = true;
                            return;
                        }

                        // Copy the current file to the new location/name
                        StorageFile currentFile = await StorageFile.GetFileFromPathAsync(StoryModelFile);
                        await currentFile.CopyAsync(saveAsVm.ParentFolder, saveAsVm.ProjectName, NameCollisionOption.ReplaceExisting);

                        // Update the story file path to the new location
                        StoryModelFile = newFilePath;

                        // Update window title and recents
                        window.UpdateWindowTitle();
                        new UnifiedVM().UpdateRecents(StoryModelFile);

                        // Indicate the model is now saved and unchanged
                        Messenger.Send(new IsChangedMessage(true));
                        StoryModel.Changed = false;
                        shellVm.ChangeStatusColor = Colors.Green;
                        Messenger.Send(new StatusChangedMessage(new("Save File As command completed", LogLevel.Info, true)));
                    }
                }
                else
                {
                    Messenger.Send(new StatusChangedMessage(new("SaveAs dialog cancelled", LogLevel.Info, true)));
                }
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex, "Exception in SaveFileAs");
                Messenger.Send(new StatusChangedMessage(new("Save File As failed", LogLevel.Info)));
            }
            _canExecuteCommands = true;
        }
    }

    private async Task<bool> VerifyReplaceOrCreate()
    {
        logger.Log(LogLevel.Trace, "VerifyReplaceOrCreated");

        SaveAsViewModel saveAsVm = Ioc.Default.GetRequiredService<SaveAsViewModel>();
        saveAsVm.SaveAsProjectFolderPath = saveAsVm.ParentFolder.Path;
        if (File.Exists(Path.Combine(saveAsVm.ProjectPathName, saveAsVm.ProjectName)))
        {
            ContentDialog replaceDialog = new()
            {
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                Title = "Replace file?",
                Content = $"File {Path.Combine(saveAsVm.ProjectPathName, saveAsVm.ProjectName)} already exists. \n\nDo you want to replace it?",
            };
            return await window.ShowContentDialog(replaceDialog) == ContentDialogResult.Primary;
        }
        return true;
    }

    public async Task CloseFile()
    {
        _canExecuteCommands = false;
        Messenger.Send(new StatusChangedMessage(new("Closing project", LogLevel.Info, true)));
        shellVm._autoSaveService.StopAutoSave();
        if (StoryModel.Changed)
        {
            ContentDialog warning = new()
            {
                Title = "Save changes?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
            };
            if (await window.ShowContentDialog(warning) == ContentDialogResult.Primary)
            {
                shellVm.SaveModel();
                await outlineService.WriteModel(StoryModel, StoryModelFile);
            }
        }
        
        shellVm.ResetModel();
        StoryModelFile = string.Empty;
        shellVm.RightTappedNode = null; //Null right tapped node to prevent possible issues.
        shellVm.SetCurrentView(StoryViewType.ExplorerView);
        window.UpdateWindowTitle();
        Ioc.Default.GetRequiredService<BackupService>().StopTimedBackup();
        shellVm.DataSource = StoryModel.ExplorerView;
        shellVm.ShowHomePage();
        Messenger.Send(new StatusChangedMessage(new("Close story command completed", LogLevel.Info, true)));
        _canExecuteCommands = true;
    }

    public async Task ExitApp()
    {
        _canExecuteCommands = false;
        Messenger.Send(new StatusChangedMessage(new("Executing Exit project command", LogLevel.Info, true)));

        if (StoryModel.Changed)
        {
            ContentDialog warning = new()
            {
                Title = "Save changes?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
            };
            if (await window.ShowContentDialog(warning) == ContentDialogResult.Primary)
            {
                shellVm.SaveModel();
                await outlineService.WriteModel(StoryModel, StoryModelFile);
            }
        }
        BackendService backend = Ioc.Default.GetRequiredService<BackendService>();
        await backend.DeleteWorkFile();
        logger.Flush();
        Application.Current.Exit();  // Win32
    }

    /// <summary>
    /// Write the current StoryModel to the backing project file
    /// </summary>
    public async Task WriteModel()
    {
        logger.Log(LogLevel.Info, $"In WriteModel, path={StoryModelFile}");
        try
        {
            // Updating the last modified time
            try
            {
                OverviewModel overview =
                    StoryModel.StoryElements.StoryElementGuids[StoryModel.ExplorerView[0].Uuid] as OverviewModel;
                overview!.DateModified = DateTime.Today.ToString("yyyy-MM-dd");
            }
            catch
            {
                logger.Log(LogLevel.Warn, "Failed to update last modified date/time");
            }

            // Use the file path if available, otherwise fallback to the old path
            await outlineService.WriteModel(StoryModel, StoryModelFile);
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
            StoryModelFile = Path.Combine(preferences.Model.ProjectDirectory, Path.GetFileName(StoryModelFile)!);

            // Last opened file with reference to this version
            preferences.Model.LastFile1 = StoryModelFile;
        }
        catch (Exception ex)
        {
            logger.LogException(LogLevel.Error, ex, $"Error writing file {ex.Message} {ex.Source}");
            Messenger.Send(new StatusChangedMessage(new("Error writing file - see log", LogLevel.Error)));
            return;
        }
        logger.Log(LogLevel.Info, "WriteModel successful");
    }

    #region Reports and Tools

    public void SearchNodes()
    {
        _canExecuteCommands = false;    //This prevents other commands from being used till this one is complete.
        logger.Log(LogLevel.Info, $"Search started, Searching for {shellVm.FilterText}");
        shellVm.SaveModel();
        if (shellVm.DataSource == null || shellVm.DataSource.Count == 0)
        {
            logger.Log(LogLevel.Info, "Data source is null or Empty.");
            Messenger.Send(new StatusChangedMessage(new("You need to load a story first!", LogLevel.Warn)));

            _canExecuteCommands = true;
            return;
        }

        int searchTotal = 0;

        foreach (StoryNodeItem node in shellVm.DataSource[0])
        {
            if (searchService.SearchStoryElement(node, shellVm.FilterText, StoryModel)) //checks if node name contains the thing we are looking for
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

        switch (searchTotal)
        {
            case 0:
                Messenger.Send(new StatusChangedMessage(new("Found no matches", LogLevel.Info, true)));
                break;
            case 1:
                Messenger.Send(new StatusChangedMessage(new("Found 1 match", LogLevel.Info, true)));
                break;
            default:
                Messenger.Send(new StatusChangedMessage(new($"Found {searchTotal} matches", LogLevel.Info, true)));
                break;
        }
        _canExecuteCommands = true;    //Enables other commands from being used till this one is complete.
    }

    public async Task GenerateScrivenerReports()
    {
        if (shellVm.DataSource == null || shellVm.DataSource.Count == 0)
        {
            Messenger.Send(new StatusChangedMessage(new("You need to open a story first!", LogLevel.Info)));
            logger.Log(LogLevel.Info, $"Scrivener Report cancelled (DataSource was null or empty)");
            return;
        }

        //TODO: revamp this to be more user friendly.
        _canExecuteCommands = false;
        shellVm.SaveModel();

        // Select the Scrivener .scrivx file to add the report to
        StorageFile file = await window.ShowFilePicker("Open file", ".scrivx");
        if (file != null)
        {
            shellVm.Scrivener.ScrivenerFile = file;
            shellVm.Scrivener.ProjectPath = Path.GetDirectoryName(file.Path);
            if (!await shellVm.Scrivener.IsScrivenerRelease3())
                throw new ApplicationException("Project is not Scrivener Release 3");
            // Load the Scrivener project file's model
            ScrivenerReports rpt = new(file, StoryModel);
            await rpt.GenerateReports();
        }

        Messenger.Send(new StatusChangedMessage(new("Generate Scrivener reports completed", LogLevel.Info, true)));
        _canExecuteCommands = true;
    }
    public async Task PrintCurrentNodeAsync()
    {
        _canExecuteCommands = false;

        if (shellVm.RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Right tap a node to print", LogLevel.Warn)));
            logger.Log(LogLevel.Info, "Print node failed as no node is selected");
            _canExecuteCommands = true;
            return;
        }
        await Ioc.Default.GetRequiredService<PrintReportDialogVM>().PrintSingleNode(shellVm.RightTappedNode);
        _canExecuteCommands = true;
    }

    public async Task KeyQuestionsTool()
    {
        logger.Log(LogLevel.Info, "Displaying KeyQuestions tool dialog");
        if (_canExecuteCommands)
        {
            _canExecuteCommands = false;
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
            _canExecuteCommands = true;
        }
    }

    public async Task TopicsTool()
    {
        logger.Log(LogLevel.Info, "Displaying Topics tool dialog");
        if (_canExecuteCommands)
        {
            _canExecuteCommands = false;
            if (shellVm.RightTappedNode == null) { shellVm.RightTappedNode = shellVm.CurrentNode; }

            ContentDialog dialog = new()
            {
                Title = "Topic Information",
                CloseButtonText = "Done",
                Content = new TopicsDialog()
            };
            await window.ShowContentDialog(dialog);

            _canExecuteCommands = true;
        }

        logger.Log(LogLevel.Info, "Topics finished");
    }

    /// <summary>
    /// This shows the master plot dialog
    /// </summary>
    public async Task MasterPlotTool()
    {
        if (_canExecuteCommands)
        {
            _canExecuteCommands = false;
            logger.Log(LogLevel.Info, "Displaying MasterPlot tool dialog");
            if (VerifyToolUse(true, true))
            {
                //Creates and shows content dialog
                ContentDialog dialog = new()
                {
                    Title = "Master plots",
                    PrimaryButtonText = "Copy",
                    SecondaryButtonText = "Cancel",
                    Content = new MasterPlotsDialog()
                };
                ContentDialogResult result = await window.ShowContentDialog(dialog);

                if (result == ContentDialogResult.Primary) // Copy command
                {
                    MasterPlotsViewModel masterPlotsVm = Ioc.Default.GetRequiredService<MasterPlotsViewModel>();
                    string masterPlotName = masterPlotsVm.PlotPatternName;
                    PlotPatternModel model = masterPlotsVm.MasterPlots[masterPlotName];
                    IList<PlotPatternScene> scenes = model.PlotPatternScenes;
                    ProblemModel problem = new ProblemModel(masterPlotName, StoryModel, shellVm.RightTappedNode);
                    // add the new ProblemModel & node to the end of the target (shellVm.RightTappedNode) children 
                    StoryNodeItem problemNode = new(problem, shellVm.RightTappedNode);
                    shellVm.RightTappedNode.IsExpanded = true;
                    problemNode.IsSelected = true;
                    problemNode.IsExpanded = true;
                    if (scenes.Count == 1)
                    {
                        problem.StoryQuestion = "See Notes.";
                        problem.Notes = scenes[0].Notes;
                    }
                    else foreach (PlotPatternScene scene in scenes)
                    {
                        SceneModel child = new(StoryModel, shellVm.RightTappedNode)
                            { Name = scene.SceneTitle, Remarks = "See Notes.", Notes = scene.Notes };
                        // add the new SceneModel & node to the end of the problem's children 
                        StoryNodeItem newNode = new(child, problemNode);
                        newNode.IsSelected = true;
                    }

                    Messenger.Send(new StatusChangedMessage(new($"MasterPlot {masterPlotName} inserted", LogLevel.Info, true)));
                    ShellViewModel.ShowChange();
                    logger.Log(LogLevel.Info, "MasterPlot complete");
                }
            }
        }
        _canExecuteCommands = true;
    }

    public async Task DramaticSituationsTool()
    {
        logger.Log(LogLevel.Info, "Displaying Dramatic Situations tool dialog");
        if (_canExecuteCommands)
        {
            _canExecuteCommands = false;

            if (VerifyToolUse(true, true))
            {
                //Creates and shows dialog
                ContentDialog dialog = new()
                {
                    Title = "Dramatic situations",
                    PrimaryButtonText = "Copy as problem",
                    SecondaryButtonText = "Copy as scene",
                    CloseButtonText = "Cancel",
                    Content = new DramaticSituationsDialog()
                };
                ContentDialogResult result = await window.ShowContentDialog(dialog);

                DramaticSituationModel situationModel = Ioc.Default.GetRequiredService<DramaticSituationsViewModel>().Situation;
                string msg;

                if (result == ContentDialogResult.Primary)
                {
                    ProblemModel problem = new() { Name = situationModel.SituationName, StoryQuestion = "See Notes.", Notes = situationModel.Notes };

                    // Insert the new Problem as the target's child
                    _ = new StoryNodeItem(problem, shellVm.RightTappedNode);
                    msg = $"Problem {situationModel.SituationName} inserted";
                    ShellViewModel.ShowChange();
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    SceneModel sceneVar = new() { Name = situationModel.SituationName, Remarks = "See Notes.", Notes = situationModel.Notes };
                    // Insert the new Scene as the target's child
                    _ = new StoryNodeItem(sceneVar, shellVm.RightTappedNode);
                    msg = $"Scene {situationModel.SituationName} inserted";
                    ShellViewModel.ShowChange();
                }
                else { msg = "Dramatic Situation tool cancelled"; }

                logger.Log(LogLevel.Info, msg);
                Messenger.Send(new StatusChangedMessage(new(msg, LogLevel.Info, true)));
            }

            _canExecuteCommands = true;
        }
        logger.Log(LogLevel.Info, "Dramatic Situations finished");
    }

    /// <summary>
    /// This loads the stock scenes dialog in the Plotting Aids submenu
    /// </summary>
    public async Task StockScenesTool()
    {
        logger.Log(LogLevel.Info, "Displaying Stock Scenes tool dialog");
        if (VerifyToolUse(true, true) && _canExecuteCommands)
        {
            _canExecuteCommands = false;
            try
            {
                //Creates and shows dialog
                ContentDialog dialog = new()
                {
                    Title = "Stock scenes",
                    Content = new StockScenesDialog(),
                    PrimaryButtonText = "Add Scene",
                    CloseButtonText = "Cancel",
                };
                ContentDialogResult result = await window.ShowContentDialog(dialog);

                if (result == ContentDialogResult.Primary) // Copy command
                {
                    if (string.IsNullOrWhiteSpace(Ioc.Default.GetRequiredService<StockScenesViewModel>().SceneName))
                    {
                        Messenger.Send(new StatusChangedMessage(new("You need to select a stock scene",
                            LogLevel.Warn)));
                        return;
                    }

                    SceneModel sceneVar = new()
                        { Name = Ioc.Default.GetRequiredService<StockScenesViewModel>().SceneName };
                    StoryNodeItem newNode = new(sceneVar, shellVm.RightTappedNode);
                    shellVm._sourceChildren = shellVm.RightTappedNode.Children;
                    shellVm.TreeViewNodeClicked(newNode);
                    shellVm.RightTappedNode.IsExpanded = true;
                    newNode.IsSelected = true;
                    Messenger.Send(new StatusChangedMessage(new("Stock Scenes inserted", LogLevel.Info)));
                }
                else
                {
                    Messenger.Send(new StatusChangedMessage(new("Stock Scenes canceled", LogLevel.Warn)));
                }
            }
            catch (Exception e)
            {
                logger.LogException(LogLevel.Error, e, e.Message);
            }
            _canExecuteCommands = true;
        }
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
            if (explorerViewOnly && shellVm.CurrentViewType != StoryViewType.ExplorerView)
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
                if (shellVm.CurrentViewType == StoryViewType.ExplorerView && StoryModel.ExplorerView.Count == 0)
                {
                    Messenger.Send(new StatusChangedMessage(new("Open or create an outline first", LogLevel.Warn)));
                    return false;
                }
                if (shellVm.CurrentViewType == StoryViewType.NarratorView && StoryModel.NarratorView.Count == 0)
                {
                    Messenger.Send(new StatusChangedMessage(new("Open or create an outline first", LogLevel.Warn)));
                    return false;
                }
            }
            if (nodeRequired)
            {
                if (shellVm.RightTappedNode == null) { shellVm.RightTappedNode = shellVm.CurrentNode; }
                if (shellVm.RightTappedNode == null)
                {
                    Messenger.Send(new StatusChangedMessage(new("You need to select a node first", LogLevel.Warn)));
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
    /// Adds a new StoryElement to the open StoryModel
    /// </summary>
    /// <param name="typeToAdd">Element type that you want to add</param>
    public void AddStoryElement(StoryItemType typeToAdd)
    {
        _canExecuteCommands = false;
        logger.Log(LogLevel.Info, $"Adding StoryElement {typeToAdd}");
        if (shellVm.RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Right tap a node to add to", LogLevel.Warn)));
            logger.Log(LogLevel.Info, "Add StoryElement failed- node not selected");
            _canExecuteCommands = true;
            return;
        }

        if (StoryNodeItem.RootNodeType(shellVm.RightTappedNode) == StoryItemType.TrashCan)
        {
            Messenger.Send(new StatusChangedMessage(new("Cannot add add to Deleted Items", LogLevel.Warn, true)));
            _canExecuteCommands = true;
            return;
        }

        //Create new element via outline service
        StoryElement newNode = outlineService.AddStoryElement(StoryModel, typeToAdd, shellVm.RightTappedNode);

        newNode.Node.Parent.IsExpanded = true;
        newNode.IsSelected = false;
        newNode.Node.Background = window.ContrastColor;
        shellVm.NewNodeHighlightCache.Add(newNode.Node);
        logger.Log(LogLevel.Info, $"Added Story Element {newNode.Uuid}");

        Messenger.Send(new IsChangedMessage(true));
        Messenger.Send(new StatusChangedMessage(new($"Added new {typeToAdd}", LogLevel.Info, true)));
        _canExecuteCommands = true;

        shellVm.TreeViewNodeClicked(newNode, false);
    }


    public async void RemoveStoryElement()
    {
        bool _delete = true;
        Guid elementToDelete = shellVm.RightTappedNode.Uuid;
        List<StoryElement> _foundElements = outlineService.FindElementReferences(StoryModel, elementToDelete);

        //Only warns if it finds a node its referenced in
        if (_foundElements.Count >= 1)
        {
            //Creates UI
            StackPanel _content = new();
            _content.Children.Add(new TextBlock { Text = "The following nodes will be updated to remove references to this node:" });
            _content.Children.Add(new ListView { ItemsSource = _foundElements, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center, Height = 300, Width = 480 });

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
            if (await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(_Dialog) !=
                ContentDialogResult.Primary)
            {
                _delete = false;
            }
        }

        // Go through with delete
        if (_delete)
        {
            outlineService.RemoveReferenceToElement(elementToDelete, StoryModel);

            if (shellVm.CurrentView.Equals("Story Explorer View"))
            {
                shellVm.RightTappedNode.Delete(StoryViewType.ExplorerView,StoryModel.ExplorerView[0]);
            }
            else { shellVm.RightTappedNode.Delete(StoryViewType.NarratorView, StoryModel.NarratorView[0]); }
        }
    }

    public void RestoreStoryElement()
    {
        logger.Log(LogLevel.Trace, "RestoreStoryElement");
        if (shellVm.RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Right tap a node to restore", LogLevel.Warn)));
            return;
        }
        if (StoryNodeItem.RootNodeType(shellVm.RightTappedNode) != StoryItemType.TrashCan)
        {
            Messenger.Send(new StatusChangedMessage(new("You can only restore from Deleted StoryElements", LogLevel.Warn)));
            return;
        }

        if (shellVm.RightTappedNode.IsRoot)
        {
            Messenger.Send(new StatusChangedMessage(new("You can't restore a root node!", LogLevel.Warn)));
            return;
        }

        //TODO: Add dialog to confirm restore
        ObservableCollection<StoryNodeItem> _target = shellVm.DataSource[0].Children;
        shellVm.DataSource[1].Children.Remove(shellVm.RightTappedNode);
        _target.Add(shellVm.RightTappedNode);
        shellVm.RightTappedNode.Parent = shellVm.DataSource[0];
        Messenger.Send(new StatusChangedMessage(new($"Restored node {shellVm.RightTappedNode.Name}", LogLevel.Info, true)));
    }

    /// <summary>
    /// Add a Scene StoryNodeItem to the end of the Narrative view
    /// by copying from the Scene's StoryNodeItem in the ExplorerView
    /// view.
    /// </summary>
    public void CopyToNarrative()
    {
        logger.Log(LogLevel.Trace, "CopyToNarrative");
        if (shellVm.RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Select a node to copy", LogLevel.Info)));
            return;
        }
        if (shellVm.RightTappedNode.Type != StoryItemType.Scene)
        {
            Messenger.Send(new StatusChangedMessage(new("You can only copy a scene", LogLevel.Warn)));
            return;
        }

        SceneModel _sceneVar = (SceneModel)StoryModel.StoryElements.StoryElementGuids[shellVm.RightTappedNode.Uuid];
        _ = new StoryNodeItem(_sceneVar, StoryModel.NarratorView[0]);
        Messenger.Send(new StatusChangedMessage(new($"Copied node {shellVm.RightTappedNode.Name} to Narrative View", LogLevel.Info, true)));
    }

    /// <summary>
    /// Clears trash
    /// </summary>
    public void EmptyTrash()
    {
        if (shellVm.DataSource == null)
        {
            Messenger.Send(new StatusChangedMessage(new("You need to load a story first!", LogLevel.Warn)));
            logger.Log(LogLevel.Info, "Failed to empty trash as DataSource is null. (Is a story loaded?)");
            return;
        }

        shellVm.StatusMessage = "Trash Emptied.";
        logger.Log(LogLevel.Info, "Emptied Trash.");
        shellVm.DataSource[1].Children.Clear();
    }

    /// <summary>
    /// Remove a TreeViewItem from the Narrative view for a copied Scene.
    /// </summary>
    public void RemoveFromNarrative()
    {
        logger.Log(LogLevel.Trace, "RemoveFromNarrative");

        if (shellVm.RightTappedNode == null)
        {
            Messenger.Send(new StatusChangedMessage(new("Select a node to remove", LogLevel.Info)));
            return;
        }
        if (shellVm.RightTappedNode.Type != StoryItemType.Scene)
        {
            Messenger.Send(new StatusChangedMessage(new("You can only remove a Scene copy", LogLevel.Info)));
            return;
        }

        foreach (StoryNodeItem _item in StoryModel.NarratorView[0].Children.ToList())
        {
            if (_item.Uuid == shellVm.RightTappedNode.Uuid)
            {
                StoryModel.NarratorView[0].Children.Remove(_item);
                Messenger.Send(new StatusChangedMessage(new($"Removed node {shellVm.RightTappedNode.Name} from Narrative View", LogLevel.Info, true)));
                return;
            }
        }

        Messenger.Send(new StatusChangedMessage(new($"Node {shellVm.RightTappedNode.Name} not in Narrative View", LogLevel.Info, true)));

    }
    #endregion

    #region Constructor(s)

    public OutlineViewModel()
    {
        logger = Ioc.Default.GetRequiredService<LogService>();
        preferences = Ioc.Default.GetRequiredService<PreferenceService>();
        window = Ioc.Default.GetRequiredService<Windowing>();
        outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        searchService = Ioc.Default.GetRequiredService<SearchService>();
    }

    #endregion
}