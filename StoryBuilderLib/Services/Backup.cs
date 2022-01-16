using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Services
{
    public class BackupService
    {
        private BackgroundWorker timeBackupWorker = new(){WorkerSupportsCancellation = true,WorkerReportsProgress = false};
        private LogService Log = Ioc.Default.GetService<LogService>();
        ShellViewModel Shell = Ioc.Default.GetService<ShellViewModel>();

        /// <summary>
        /// Makes a backup every x minutes, x being the value of TimedBackupInterval in user preferences.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackupTask(object sender, DoWorkEventArgs e)
        {
            try
            {
                Log.Log(LogLevel.Info, "Timed backup task started.");
                while (!timeBackupWorker.CancellationPending)
                {
                    System.Threading.Thread.Sleep((GlobalData.Preferences.TimedBackupInterval * 60) * 1000);
                    Log.Log(LogLevel.Trace, "Starting auto backup");
                    BackupProject();
                }

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
        }

        public async Task BackupProject()
        {
            Log.Log(LogLevel.Info, "Starting Project Backup");
            //Creates backup directory if it doesnt exist
            if (!Directory.Exists(GlobalData.Preferences.BackupDirectory)) { Directory.CreateDirectory(GlobalData.Preferences.BackupDirectory); }

            try
            {
                //Gets correct name for file
                string fileName = $"{Shell.StoryModel.ProjectFile.Name} as of {DateTime.Now}".Replace('/', ' ').Replace(':', ' ').Replace(".stbx", "");
                StorageFolder backupRoot = await StorageFolder.GetFolderFromPathAsync(GlobalData.Preferences.BackupDirectory.Replace(".stbx", ""));
                StorageFolder backupLocation = await backupRoot.CreateFolderAsync(Shell.StoryModel.ProjectFile.Name, CreationCollisionOption.OpenIfExists);
                Log.Log(LogLevel.Info, $"Backing up to {backupLocation.Path} as {fileName}.zip");

                StorageFolder Temp = await StorageFolder.GetFolderFromPathAsync(GlobalData.RootDirectory);
                Temp = await Temp.CreateFolderAsync("Temp", CreationCollisionOption.ReplaceExisting);
                await Shell.StoryModel.ProjectFile.CopyAsync(Temp, Shell.StoryModel.ProjectFile.Name,NameCollisionOption.ReplaceExisting);
                ZipFile.CreateFromDirectory(Temp.Path,Path.Combine(backupLocation.Path,fileName ) + ".zip");
                
                //Creates ziparchive then cleans up
                Log.Log(LogLevel.Info, $"Created Zip file at {Path.Combine(backupLocation.Path, fileName)}.zip");
                await Temp.DeleteAsync();

                //Creates entry and flushes to disk.
                Log.Log(LogLevel.Info, $"Finished backup.");
            }
            catch (Exception ex)
            {
                Log.LogException(LogLevel.Error, ex, "Error backing up project");
            }
            Log.Log(LogLevel.Info, "BackupProject complete");
        }
    }
}
