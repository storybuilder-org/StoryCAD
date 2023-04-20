using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using NLog;
using StoryCAD.Models;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;
using LogLevel = StoryCAD.Services.Logging.LogLevel;

namespace StoryCAD.Services
{
    public class BackupService
    {
        /// <summary>
        /// This will force the backup service to run every 10 seconds
        /// and ignore the value set by the user for debugging purposes.
        ///
        /// Useful for debugging and stress testing.
        /// </summary>
        public bool EnableFastBackupService = false;

        private BackgroundWorker timeBackupWorker = new()
            {WorkerSupportsCancellation = true,WorkerReportsProgress = false};
        private LogService Log = Ioc.Default.GetService<LogService>();
        ShellViewModel Shell = Ioc.Default.GetService<ShellViewModel>();

        /// <summary>
        /// Makes a backup every x minutes, x being the value of TimedBackupInterval in user preferences.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BackupTask(object sender, DoWorkEventArgs e)
        {
            try
            {
                Log.Log(LogLevel.Info, $"Timed backup task started. (Waiting {GlobalData.Preferences.TimedBackupInterval} Minutes per backup.");
                while (!timeBackupWorker.CancellationPending)
                {
                    if (EnableFastBackupService)
                    {
                        Thread.Sleep(10000);
                    }
                    else if (!EnableFastBackupService)
                    {
                        Thread.Sleep((GlobalData.Preferences.TimedBackupInterval * 60) * 1000);
                    }
                    Log.Log(LogLevel.Info, "Starting auto backup");
                    await BackupProject();
                }
                e.Cancel = true;
                Log.Log(LogLevel.Info, "Timed backup task finished.");
            }
            catch (Exception ex)
            {
                Log.LogException(LogLevel.Error, ex,"Error in auto backup task.");
            }
        }

        /// <summary>
        /// Launches the timed backup task
        /// </summary>
        public void StartTimedBackup()
        {
            if (!GlobalData.Preferences.TimedBackup) { return; }
            timeBackupWorker.DoWork += BackupTask;
            if (!timeBackupWorker.IsBusy)
                timeBackupWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Stops the timed backup task
        /// </summary>
        public void StopTimedBackup()
        {
            if (timeBackupWorker.IsBusy)
            {
                timeBackupWorker.CancelAsync();
            }
            timeBackupWorker.DoWork -= BackupTask;
        }

        public async Task BackupProject()
        {
            Log.Log(LogLevel.Info, $"Starting Project Backup at {GlobalData.Preferences.BackupDirectory}");
            try
            {
                try
                {
                    //Creates backup directory if it doesn't exist
                    if (!Directory.Exists(GlobalData.Preferences.BackupDirectory))
                    {
                        Log.Log(LogLevel.Info, "Backup dir not found, making it.");
                        Directory.CreateDirectory(GlobalData.Preferences.BackupDirectory);
                    }
                }
                catch (Exception ex)
                {
                    Log.Log(LogLevel.Error, "Error creating backup directory.");
                    Ioc.Default.GetRequiredService<ShellViewModel>().ShowMessage(LogLevel.Warn, 
                        "Error creating backup directory, check your backup settings.", true);
                }

                //Gets correct name for file
                Log.Log(LogLevel.Info, "Getting backup path and file to make");
                string _FormattedTime = $"{ DateTime.Now }".Replace('/', '-').Replace(':', '-');
                string fileName = $"{Shell.StoryModel.ProjectFile.Name} as of {_FormattedTime}".Replace(".stbx", "");

                StorageFolder backupRoot = await StorageFolder.GetFolderFromPathAsync(GlobalData.Preferences.BackupDirectory.Replace(".stbx", ""));
                StorageFolder backupLocation = await backupRoot.CreateFolderAsync(Shell.StoryModel.ProjectFile.Name, CreationCollisionOption.OpenIfExists);
                Log.Log(LogLevel.Info, $"Backing up to {backupLocation.Path} as {fileName}.zip");


                //Create temp dir
                Log.Log(LogLevel.Info, "Writing file");
                StorageFolder TempDir = await StorageFolder.GetFolderFromPathAsync(GlobalData.RootDirectory);
                TempDir = await TempDir.CreateFolderAsync("Temp", CreationCollisionOption.ReplaceExisting);

                //Copies file to temp dir and zips it
                await Shell.StoryModel.ProjectFile.CopyAsync(TempDir, Shell.StoryModel.ProjectFile.Name, NameCollisionOption.ReplaceExisting);

                ZipFile.CreateFromDirectory(TempDir.Path, Path.Combine(backupLocation.Path, fileName) + ".zip");

                try
                {
                    ZipFile.CreateFromDirectory(TempDir.Path, Path.Combine(backupLocation.Path, fileName) + ".zip");
                }
                catch (IOException) //File already exists error
                {
                    if (File.Exists(Path.Combine(backupLocation.Path, fileName)))
                    {
                        File.Delete(Path.Combine(backupLocation.Path, fileName));
                        Log.Log(LogLevel.Warn, "Duplicate file found and deleted.");
                    }

                    try
                    {
                        ZipFile.CreateFromDirectory(TempDir.Path, Path.Combine(backupLocation.Path, fileName) + ".zip");
                    }
                    catch { }
                }

                //Creates zip archive then cleans up
                Log.Log(LogLevel.Info, $"Created Zip file at {Path.Combine(backupLocation.Path, fileName)}.zip");
                await TempDir.DeleteAsync();

                //Creates entry and flushes to disk.
                Log.Log(LogLevel.Info, "Finished backup.");
            }
            catch (Exception ex)
            {
                //Show failed message.
                GlobalData.GlobalDispatcher.TryEnqueue(() =>
                {
                    Ioc.Default.GetRequiredService<ShellViewModel>().ShowMessage(LogLevel.Warn,
                        "Making a backup failed, check your backup settings.", false);
                });
                Log.LogException(LogLevel.Error, ex, $"Error backing up project {ex.Message}");
            }
            Log.Log(LogLevel.Info, "BackupProject complete");
        }
    }
}
