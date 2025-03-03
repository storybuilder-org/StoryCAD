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

namespace StoryCAD.ViewModels.SubViewModels
{
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
        private readonly ShellViewModel shellVm;

        // Changed from a string to StorageFile
        public string StoryModelFile;

        /// <summary>
        /// Opens a file picker to let the user chose a .stbx file and loads said file
        /// If fromPath is specified then the picker is skipped.
        /// </summary>
        /// <param name="fromPath">Path to open file from (Optional)</param>
        public async Task OpenFile(string fromPath = "")
        {
            // Check if current StoryModel has been changed, if so, save and write the model.
            if (shellVm.StoryModel.Changed)
            {
                shellVm.SaveModel();
                await outlineService.WriteModel(shellVm.StoryModel, StoryModelFile);
            }

            // Stop the auto save service if it was running
            if (preferences.Model.AutoSave) { shellVm._autoSaveService.StopAutoSave(); }

            // Stop the timed backup service if it was running
            Ioc.Default.GetRequiredService<BackupService>().StopTimedBackup();

            shellVm._canExecuteCommands = false;
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
                        shellVm._canExecuteCommands = true; // unblock other commands
                        StoryModelFile = string.Empty;
                        return;
                    }
                }

                StoryModelFile = fromPath;
                if (StoryModelFile == null)
                {
                    logger.Log(LogLevel.Warn, "Open File command failed: StoryModel.ProjectFile is null.");
                    Messenger.Send(new StatusChangedMessage(new("Open Story command cancelled", LogLevel.Info)));
                    shellVm._canExecuteCommands = true;  // Unblock other commands
                    return;
                }

                if (!File.Exists(StoryModelFile))
                {
                    Messenger.Send(new StatusChangedMessage(new("Can't find file", LogLevel.Warn)));
                    logger.Log(LogLevel.Warn, $"File {StoryModelFile} does not exist.");
                    shellVm._canExecuteCommands = true;
                    return;
                }

                //Check file is available.
                StoryIO rdr = Ioc.Default.GetRequiredService<StoryIO>();
                if (!await rdr.CheckFileAvailability(StoryModelFile))
                {
                    Messenger.Send(new StatusChangedMessage(new("File Unavailable.", LogLevel.Warn)));
                    return;
                }

                // Read the file into the StoryModel.
                StorageFile file = await StorageFile.GetFileFromPathAsync(StoryModelFile);
                shellVm.StoryModel = await rdr.ReadStory(file);

                //Check the file we loaded actually has StoryCAD Data.
                if (shellVm.StoryModel == null)
                {
                    Messenger.Send(new StatusChangedMessage(new("Unable to open file (No Story Elements found)", LogLevel.Warn, true)));
                    shellVm._canExecuteCommands = true;  // unblock other commands
                    return;
                }

                if (shellVm.StoryModel.StoryElements.Count == 0)
                {
                    Messenger.Send(new StatusChangedMessage(new("Unable to open file (No Story Elements found)", LogLevel.Warn, true)));
                    shellVm._canExecuteCommands = true;  // unblock other commands
                    return;

                }

                // Take a backup of the project if the user has the 'backup on open' preference set.
                if (preferences.Model.BackupOnOpen)
                {
                    await Ioc.Default.GetRequiredService<BackupService>().BackupProject();
                }

                // Set the current view to the ExplorerView 
                if (shellVm.StoryModel.ExplorerView.Count > 0)
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
            shellVm._canExecuteCommands = true;
        }

        public async Task UnifiedNewFile(UnifiedVM dialogVm)
        {
            logger.Log(LogLevel.Info, "FileOpenVM - New File starting");
            shellVm._canExecuteCommands = false;

            try
            {
                Messenger.Send(new StatusChangedMessage(new StatusMessage("New project command executing", LogLevel.Info)), true);

                // If the current project needs saved, do so
                if (shellVm.StoryModel.Changed && StoryModelFile != null)
                {
                    await outlineService.WriteModel(shellVm.StoryModel, StoryModelFile);
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
                var file = await folder.CreateFileAsync(dialogVm.ProjectName, CreationCollisionOption.FailIfExists);
                await outlineService.CreateModel(file, name, author, dialogVm.SelectedTemplateIndex);

                shellVm.SetCurrentView(StoryViewType.ExplorerView);

                Ioc.Default.GetRequiredService<UnifiedVM>().UpdateRecents(StoryModelFile);

                shellVm.StoryModel.Changed = true;
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

                shellVm.TreeViewNodeClicked(shellVm.StoryModel.ExplorerView[0]);
                window.UpdateWindowTitle();

                Messenger.Send(new StatusChangedMessage(new("New project command completed", LogLevel.Info, true)), true);
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex, "Error creating new project");
                Messenger.Send(new StatusChangedMessage(new("File make failure.", LogLevel.Error)), true);
            }

            shellVm._canExecuteCommands = true;
        }

        public async Task OpenUnifiedMenu()
        {
            if (shellVm._canExecuteCommands)
            {
                shellVm._canExecuteCommands = false;
                // Needs logging
                shellVm._contentDialog = new() { Content = new UnifiedMenuPage() };
                if (window.RequestedTheme == ElementTheme.Light)
                {
                    shellVm._contentDialog.RequestedTheme = window.RequestedTheme;
                    shellVm._contentDialog.Background = new SolidColorBrush(Colors.LightGray);
                }
                await window.ShowContentDialog(shellVm._contentDialog);
                shellVm._canExecuteCommands = true;
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
            bool saveExecuteCommands = shellVm._canExecuteCommands;
            shellVm._canExecuteCommands = false;
            string msg = autoSave ? "AutoSave" : "SaveFile command";
            if (autoSave && !shellVm.StoryModel.Changed)
            {
                logger.Log(LogLevel.Info, $"{msg} skipped, no changes");
                shellVm._canExecuteCommands = true;
                return;
            }

            if (shellVm.StoryModel.StoryElements.Count == 0)
            {
                Messenger.Send(new StatusChangedMessage(new("You need to open a story first!", LogLevel.Info)));
                logger.Log(LogLevel.Info, $"{msg} cancelled (StoryModel.ProjectFile was null)");
                shellVm._canExecuteCommands = true;
                return;
            }

            try
            {
                Messenger.Send(new StatusChangedMessage(new($"{msg} executing", LogLevel.Info)));
                shellVm.SaveModel();
                await outlineService.WriteModel(shellVm.StoryModel, StoryModelFile);
                Messenger.Send(new StatusChangedMessage(new($"{msg} completed", LogLevel.Info)));
                shellVm.StoryModel.Changed = false;
                shellVm.ChangeStatusColor = Colors.Green;
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex, $"Exception in {msg}");
                Messenger.Send(new StatusChangedMessage(new($"{msg} failed", LogLevel.Error)));
            }
            shellVm._canExecuteCommands = saveExecuteCommands;
            shellVm._autoSaveService.StartAutoSave();
        }

        public async void SaveFileAs()
        {
            if (shellVm._canExecuteCommands)
            {
                shellVm._canExecuteCommands = false;
                Messenger.Send(new StatusChangedMessage(new("Save File As command executing", LogLevel.Info, true)));
                try
                {
                    if (string.IsNullOrEmpty(StoryModelFile))
                    {
                        Messenger.Send(new StatusChangedMessage(new("You need to load a story first!", LogLevel.Info)));
                        logger.Log(LogLevel.Warn, "User tried to use save as without a story loaded.");
                        shellVm._canExecuteCommands = true;
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
                            await outlineService.WriteModel(shellVm.StoryModel, StoryModelFile);

                            // If the new path is the same as the current one, exit early
                            string newFilePath = Path.Combine(saveAsVm.ParentFolder.Path, saveAsVm.ProjectName);
                            if (newFilePath.Equals(StoryModelFile, StringComparison.OrdinalIgnoreCase))
                            {
                                Messenger.Send(new StatusChangedMessage(new("Save File As command completed", LogLevel.Info)));
                                logger.Log(LogLevel.Info, "User tried to save file to same location as current file.");
                                shellVm._canExecuteCommands = true;
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
                            shellVm.StoryModel.Changed = false;
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
                shellVm._canExecuteCommands = true;
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
            shellVm._canExecuteCommands = false;
            Messenger.Send(new StatusChangedMessage(new("Closing project", LogLevel.Info, true)));
            shellVm._autoSaveService.StopAutoSave();
            if (shellVm.StoryModel.Changed)
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
                    await outlineService.WriteModel(shellVm.StoryModel, StoryModelFile);
                }
            }

            shellVm.ResetModel();
            shellVm.RightTappedNode = null; //Null right tapped node to prevent possible issues.
            shellVm.SetCurrentView(StoryViewType.ExplorerView);
            window.UpdateWindowTitle();
            Ioc.Default.GetRequiredService<BackupService>().StopTimedBackup();
            shellVm.DataSource = shellVm.StoryModel.ExplorerView;
            shellVm.ShowHomePage();
            Messenger.Send(new StatusChangedMessage(new("Close story command completed", LogLevel.Info, true)));
            shellVm._canExecuteCommands = true;
        }

        public async Task ExitApp()
        {
            shellVm._canExecuteCommands = false;
            Messenger.Send(new StatusChangedMessage(new("Executing Exit project command", LogLevel.Info, true)));

            if (shellVm.StoryModel.Changed)
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
                    await outlineService.WriteModel(shellVm.StoryModel, StoryModelFile);
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
                        shellVm.StoryModel.StoryElements.StoryElementGuids[shellVm.StoryModel.ExplorerView[0].Uuid] as OverviewModel;
                    overview!.DateModified = DateTime.Today.ToString("yyyy-MM-dd");
                }
                catch
                {
                    logger.Log(LogLevel.Warn, "Failed to update last modified date/time");
                }

                // Use the file path if available, otherwise fallback to the old path
                await outlineService.WriteModel(shellVm.StoryModel, StoryModelFile);
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

        /// <summary>
        /// Opens help menu
        /// </summary>
        public Task LaunchGitHubPages()
        {
            shellVm._canExecuteCommands = false;
            Messenger.Send(new StatusChangedMessage(new("Launching GitHub Pages User Manual", LogLevel.Info, true)));

            Process.Start(new ProcessStartInfo()
            {
                FileName = @"https://Storybuilder-org.github.io/StoryCAD/",
                UseShellExecute = true
            });

            Messenger.Send(new StatusChangedMessage(new("Launch default browser completed", LogLevel.Info, true)));

            shellVm._canExecuteCommands = true;
            return Task.CompletedTask;
        }

        #region Reports and Tools

                public void SearchNodes()
        {
            shellVm._canExecuteCommands = false;    //This prevents other commands from being used till this one is complete.
            logger.Log(LogLevel.Info, $"Search started, Searching for {shellVm.FilterText}");
            shellVm.SaveModel();
            if (shellVm.DataSource == null || shellVm.DataSource.Count == 0)
            {
                logger.Log(LogLevel.Info, "Data source is null or Empty.");
                Messenger.Send(new StatusChangedMessage(new("You need to load a story first!", LogLevel.Warn)));

                shellVm._canExecuteCommands = true;
                return;
            }

            int searchTotal = 0;

            foreach (StoryNodeItem node in shellVm.DataSource[0])
            {
                if (searchService.SearchStoryElement(node, shellVm.FilterText, shellVm.StoryModel)) //checks if node name contains the thing we are looking for
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
            shellVm._canExecuteCommands = true;    //Enables other commands from being used till this one is complete.
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
            shellVm._canExecuteCommands = false;
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
                ScrivenerReports rpt = new(file, shellVm.StoryModel);
                await rpt.GenerateReports();
            }

            Messenger.Send(new StatusChangedMessage(new("Generate Scrivener reports completed", LogLevel.Info, true)));
            shellVm._canExecuteCommands = true;
        }
        public async Task PrintCurrentNodeAsync()
        {
            shellVm._canExecuteCommands = false;

            if (shellVm.RightTappedNode == null)
            {
                Messenger.Send(new StatusChangedMessage(new("Right tap a node to print", LogLevel.Warn)));
                logger.Log(LogLevel.Info, "Print node failed as no node is selected");
                shellVm._canExecuteCommands = true;
                return;
            }
            await Ioc.Default.GetRequiredService<PrintReportDialogVM>().PrintSingleNode(shellVm.RightTappedNode);
            shellVm._canExecuteCommands = true;
        }

        public async Task KeyQuestionsTool()
        {
            logger.Log(LogLevel.Info, "Displaying KeyQuestions tool dialog");
            if (shellVm._canExecuteCommands)
            {
                shellVm._canExecuteCommands = false;
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
                shellVm._canExecuteCommands = true;
            }
        }

        public async Task TopicsTool()
        {
            logger.Log(LogLevel.Info, "Displaying Topics tool dialog");
            if (shellVm._canExecuteCommands)
            {
                shellVm._canExecuteCommands = false;
                if (shellVm.RightTappedNode == null) { shellVm.RightTappedNode = shellVm.CurrentNode; }

                ContentDialog dialog = new()
                {
                    Title = "Topic Information",
                    CloseButtonText = "Done",
                    Content = new TopicsDialog()
                };
                await window.ShowContentDialog(dialog);

                shellVm._canExecuteCommands = true;
            }

            logger.Log(LogLevel.Info, "Topics finished");
        }

        /// <summary>
        /// This shows the master plot dialog
        /// </summary>
        public async Task MasterPlotTool()
        {
            if (shellVm._canExecuteCommands)
            {
                shellVm._canExecuteCommands = false;
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
                        ProblemModel problem = new ProblemModel(masterPlotName, shellVm.StoryModel);
                        // add the new ProblemModel & node to the end of the target (RightTappedNode) children 
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
                            SceneModel child = new(shellVm.StoryModel)
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
            shellVm._canExecuteCommands = true;
        }

        public async Task DramaticSituationsTool()
        {
            logger.Log(LogLevel.Info, "Displaying Dramatic Situations tool dialog");
            if (shellVm._canExecuteCommands)
            {
                shellVm._canExecuteCommands = false;

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

                shellVm._canExecuteCommands = true;
            }
            logger.Log(LogLevel.Info, "Dramatic Situations finished");
        }

        /// <summary>
        /// This loads the stock scenes dialog in the Plotting Aids submenu
        /// </summary>
        public async Task StockScenesTool()
        {
            logger.Log(LogLevel.Info, "Displaying Stock Scenes tool dialog");
            if (VerifyToolUse(true, true) && shellVm._canExecuteCommands)
            {
                shellVm._canExecuteCommands = false;
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
                shellVm._canExecuteCommands = true;
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
                    if (shellVm.StoryModel == null)
                    {
                        Messenger.Send(new StatusChangedMessage(new("Open or create an outline first", LogLevel.Warn)));
                        return false;
                    }
                    if (shellVm.CurrentViewType == StoryViewType.ExplorerView && shellVm.StoryModel.ExplorerView.Count == 0)
                    {
                        Messenger.Send(new StatusChangedMessage(new("Open or create an outline first", LogLevel.Warn)));
                        return false;
                    }
                    if (shellVm.CurrentViewType == StoryViewType.NarratorView && shellVm.StoryModel.NarratorView.Count == 0)
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

        #region Constructor(s)

        public OutlineViewModel(
            LogService logService,
            PreferenceService preferenceService,
            Windowing window,
            ShellViewModel shellViewModel
        )
        {
            logger = logService;
            preferences = preferenceService;
            this.window = window;
            shellVm = shellViewModel;
            outlineService = Ioc.Default.GetRequiredService<OutlineService>();
            searchService = Ioc.Default.GetRequiredService<SearchService>();
        }

        #endregion
    }
}
