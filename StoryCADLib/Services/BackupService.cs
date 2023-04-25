using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;

namespace StoryCAD.Services
{
    public class BackupService
    {
        private BackgroundWorker timedBackupWorker;
        private  System.Timers.Timer backupTimer;

        private LogService Log = Ioc.Default.GetService<LogService>();
        ShellViewModel Shell = Ioc.Default.GetService<ShellViewModel>();

        //TODO: Merge BackupProject into this
        /// <summary>
        /// Makes a backup every x minutes, x being the value of TimedBackupInterval in user preferences.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BackupTask(object sender, DoWorkEventArgs e)
        {
            try
            {
                Log.Log(LogLevel.Info, "Timed backup task started.");
                while (!timedBackupWorker.CancellationPending)
                {
                    Thread.Sleep((GlobalData.Preferences.TimedBackupInterval * 60) * 1000);
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
        public void RunTimedBackup(object source, System.Timers.ElapsedEventArgs e)
        {
            if (!timedBackupWorker.IsBusy)
                timedBackupWorker.RunWorkerAsync();
        }

        /// <summary>
        /// This method is used to enable the timed backup
        /// timer and start counting down. At the expiration
        /// of the timer, BackupTask will be called to backup
        /// the project.  The timer's AutoReset will cause it
        /// to keep running timer events until it's stopped
        /// via a call to StopBackupTimer().
        /// These two methods are called from file open, file
        /// new, and file close to insure that  timed backups
        /// are taken only when a project is open.
        /// 
        /// If the user's Preferences don't want timed backups,
        /// they won't be started. 
        /// </summary>
        public void StartTimedBackup()
        {
            //TODO: Don't start this if the user doesn't want it.
            if (GlobalData.Preferences.TimedBackup)
            {
                backupTimer.Start();
            }
        }

        /// <summary>
        /// Stops the timed backup task
        /// </summary>
        public void StopTimedBackup()
        {
            if (timedBackupWorker.IsBusy)
            {
                timedBackupWorker.CancelAsync();
            }
            timedBackupWorker.DoWork -= BackupTask;
        }

        public async Task BackupProject()
        {
            Log.Log(LogLevel.Info, $"Starting Project Backup at {GlobalData.Preferences.BackupDirectory}");
            try
            {
                //Creates backup directory if it doesn't exist
                if (!Directory.Exists(GlobalData.Preferences.BackupDirectory))
                {
                    Log.Log(LogLevel.Info, "Backup dir not found, making it.");
                    Directory.CreateDirectory(GlobalData.Preferences.BackupDirectory);
                }

                //Gets correct name for file
                Log.Log(LogLevel.Info, "Getting backup path and file to made");
                string fileName = $"{Shell.StoryModel.ProjectFile.Name} as of {DateTime.Now}".Replace('/', ' ').Replace(':', ' ').Replace(".stbx", "");
                StorageFolder backupRoot = await StorageFolder.GetFolderFromPathAsync(GlobalData.Preferences.BackupDirectory.Replace(".stbx", ""));
                StorageFolder backupLocation = await backupRoot.CreateFolderAsync(Shell.StoryModel.ProjectFile.Name, CreationCollisionOption.OpenIfExists);
                Log.Log(LogLevel.Info, $"Backing up to {backupLocation.Path} as {fileName}.zip");

                Log.Log(LogLevel.Info, "Writing file");
                StorageFolder Temp = await StorageFolder.GetFolderFromPathAsync(GlobalData.RootDirectory);
                Temp = await Temp.CreateFolderAsync("Temp", CreationCollisionOption.ReplaceExisting);
                await Shell.StoryModel.ProjectFile.CopyAsync(Temp, Shell.StoryModel.ProjectFile.Name,
                    NameCollisionOption.ReplaceExisting);
                ZipFile.CreateFromDirectory(Temp.Path, Path.Combine(backupLocation.Path, fileName) + ".zip");

                //Creates zip archive then cleans up
                Log.Log(LogLevel.Info, $"Created Zip file at {Path.Combine(backupLocation.Path, fileName)}.zip");
                await Temp.DeleteAsync();

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

        public BackupService()
        {
            timedBackupWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = false
            };
            timedBackupWorker.DoWork += BackupTask;

            backupTimer = new System.Timers.Timer
                (GlobalData.Preferences.TimedBackupInterval * 60 * 1000);
            backupTimer.AutoReset = true;
            backupTimer.Stop();
            backupTimer.Elapsed += RunTimedBackup;
        }
    }
}
