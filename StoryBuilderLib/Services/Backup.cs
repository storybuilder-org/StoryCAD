using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Services;

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
    private async void BackupTask(object sender, DoWorkEventArgs e)
    {
        try
        {
            Log.Log(LogLevel.Info, "Timed backup task started.");
            while (!timeBackupWorker.CancellationPending)
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
            DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
            {
                ContentDialog warning = new();
                warning.Title = "Backup Warning";
                warning.Content = "The last backup failed due to the following reason:\n" + ex.Message;
                warning.XamlRoot = GlobalData.XamlRoot;
                warning.CloseButtonText = "Understood.";
                await warning.ShowAsync();
            });

            Log.LogException(LogLevel.Error, ex, $"Error backing up project {ex.Message}");
        }
        Log.Log(LogLevel.Info, "BackupProject complete");
    }
}