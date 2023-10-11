using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;
 

namespace StoryCAD.Services.Backup
{
    public class BackupService
    {
        private BackgroundWorker timedBackupWorker;
        private System.Timers.Timer backupTimer;

        private LogService Log = Ioc.Default.GetService<LogService>();
        private AppState State = Ioc.Default.GetService<AppState>();
        ShellViewModel _shellVM;

        #region Constructor

        public BackupService()
        {
            timedBackupWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = false
            };
            timedBackupWorker.DoWork += RunBackupTask;

            backupTimer = new System.Timers.Timer
                (State.Preferences.TimedBackupInterval * 60 * 1000);  // interval in minutes
            backupTimer.AutoReset = true;
            backupTimer.Elapsed += BackupTimer_Elapsed;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This method is used to enable the timed backup timer and start counting down. At the expiration
        /// of the timer, BackupTask will be called to backup  the project.  The timer's AutoReset will cause 
        /// ir to keep running timer events until it's stopped via a call to StopBackupTimer().
        /// 
        /// These two methods (start and stop) are called from file open, file  new, and file close to
        /// insure that  timed backups are taken only when a project is open.
        /// 
        /// If the user's Preferences don't want timed backups, then the interval timer won't be started. 
        /// </summary>
        public void StartTimedBackup()
        {
            if (!State.Preferences.TimedBackup)
               return;
            
            // If the timer is already running, stop it
            if (backupTimer.Enabled)
                backupTimer.Stop();

            // Reset the timer and start it 
            backupTimer.Interval = (State.Preferences.TimedBackupInterval * 60 * 1000); // interval in minutes
            backupTimer.Start();
        }

        /// <summary>
        /// Stops the timed backup task
        /// </summary>
        public void StopTimedBackup()
        {
            backupTimer.Stop();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Makes a backup every x minutes, x being the value of
        /// TimedBackupInterval in user preferences.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RunBackupTask(object sender, DoWorkEventArgs e)
        {
            try
            {
                //while (!timedBackupWorker.CancellationPending)
                //{
                    Log.Log(LogLevel.Info, "Starting timed backup task.");
                    await BackupProject();
                //}
                //e.Cancel = true;
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
        private void BackupTimer_Elapsed(object source, System.Timers.ElapsedEventArgs e)
        {
            if (!timedBackupWorker.IsBusy)
                timedBackupWorker.RunWorkerAsync();
        }

        #endregion


        public async Task BackupProject()
        {
            _shellVM = Ioc.Default.GetService<ShellViewModel>();

            Log.Log(LogLevel.Info, $"Starting Project Backup at {State.Preferences.BackupDirectory}");
            try
            {
                //Creates backup directory if it doesn't exist
                if (!Directory.Exists(State.Preferences.BackupDirectory))
                {
                    Log.Log(LogLevel.Info, "Backup dir not found, making it.");
                    Directory.CreateDirectory(State.Preferences.BackupDirectory);
                }

                //Gets correct name for file
                Log.Log(LogLevel.Info, "Getting backup path and file to make");
                string fileName = $"{_shellVM!.StoryModel.ProjectFile.Name} as of {DateTime.Now}".Replace('/', ' ').Replace(':', ' ').Replace(".stbx", "");
                StorageFolder backupRoot = await StorageFolder.GetFolderFromPathAsync(State.Preferences.BackupDirectory.Replace(".stbx", ""));
                StorageFolder backupLocation = await backupRoot.CreateFolderAsync(_shellVM.StoryModel.ProjectFile.Name, CreationCollisionOption.OpenIfExists);
                Log.Log(LogLevel.Info, $"Backing up to {backupLocation.Path} as {fileName}.zip");

                Log.Log(LogLevel.Info, "Writing file");
                StorageFolder Temp = await StorageFolder.GetFolderFromPathAsync(Ioc.Default.GetRequiredService<AppState>().RootDirectory);
                Temp = await Temp.CreateFolderAsync("Temp", CreationCollisionOption.ReplaceExisting);
                await _shellVM.StoryModel.ProjectFile.CopyAsync(Temp, _shellVM.StoryModel.ProjectFile.Name,
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
                Ioc.Default.GetRequiredService<Windowing>().GlobalDispatcher.TryEnqueue(() =>
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
