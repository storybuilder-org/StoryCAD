using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Messages;
using Windows.Storage;
using ABI.Windows.Devices.AllJoyn;
using StoryCAD.Services;
using StoryCAD.Services.Outline;
using CommunityToolkit.Mvvm.Messaging;

namespace StoryCAD.ViewModels.SubViewModels
{
    public class OutlineViewModel : ObservableRecipient
    {
        private readonly LogService logger;
        private readonly PreferenceService preferences;
        private readonly Windowing window;
        private readonly OutlineService outlineService;
        // The reference to ShellViewModel is temporary
        // until the ShellViewModel is refactored to fully
        // use OutlineViewModel for outline methods.
        private readonly ShellViewModel shellVm;

        // Changed from a string to StorageFile
        public StorageFile StoryModelFile;

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
                StoryModelFile = await folder.CreateFileAsync(dialogVm.ProjectName, CreationCollisionOption.GenerateUniqueName
                );

                // Create the StoryModel
                string name = Path.GetFileNameWithoutExtension(StoryModelFile.Path);
                string author = preferences.Model.FirstName + " " + preferences.Model.LastName;
                shellVm.StoryModel = await outlineService.CreateModel(
                    StoryModelFile, name, author, dialogVm.SelectedTemplateIndex);
                shellVm.StoryModel.ProjectFolder = folder;
                shellVm.StoryModel.ProjectFile = StoryModelFile;
                shellVm.StoryModel.ProjectPath = StoryModelFile.Path;

                shellVm.SetCurrentView(StoryViewType.ExplorerView);

                Ioc.Default.GetRequiredService<UnifiedVM>()
                          .UpdateRecents(StoryModelFile.Path);

                shellVm.StoryModel.Changed = true;
                await shellVm.SaveFile();

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

        /// <summary>
        /// Write the current StoryModel to the backing project file
        /// </summary>
        public async Task WriteModel()
        {
            logger.Log(LogLevel.Info, $"In WriteModel, file={shellVm.StoryModel.ProjectFilename} path={shellVm.StoryModel.ProjectPath}");
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
                await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(
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
                shellVm.StoryModel.ProjectFolder = await StorageFolder.GetFolderFromPathAsync(preferences.Model.ProjectDirectory);
                shellVm.StoryModel.ProjectFile = await shellVm.StoryModel.ProjectFolder.CreateFileAsync(
                    shellVm.StoryModel.ProjectFilename,
                    CreationCollisionOption.GenerateUniqueName
                );
                shellVm.StoryModel.ProjectFilename = StoryModelFile.Name;
                shellVm.StoryModel.ProjectPath = shellVm.StoryModel.ProjectFolder.Path;
                shellVm.StoryModel.ProjectFilename = Path.GetFileName(StoryModelFile.Path);

                // Update OutlineViewModel's file reference so future saves work correctly
                StoryModelFile = shellVm.StoryModel.ProjectFile;

;

                // Last opened file with reference to this version
                preferences.Model.LastFile1 = shellVm.StoryModel.ProjectFile.Path;
            }
            catch (Exception ex)
            {
                logger.LogException(LogLevel.Error, ex, $"Error writing file {ex.Message} {ex.Source}");
                Messenger.Send(new StatusChangedMessage(new("Error writing file - see log", LogLevel.Error)));
                return;
            }
            logger.Log(LogLevel.Info, "WriteModel successful");
        }

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
            outlineService = Ioc.Default.GetRequiredService<OutlineService>();
            shellVm = shellViewModel;
        }
        #endregion
    }
}
