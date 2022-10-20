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
    private BackgroundWorker _timeBackupWorker = new(){WorkerSupportsCancellation = true,WorkerReportsProgress = false};
    private LogService _log = Ioc.Default.GetService<LogService>();
    private ShellViewModel _shell = Ioc.Default.GetService<ShellViewModel>();

    /// <summary>
    /// Makes a backup every x minutes, x being the value of TimedBackupInterval in user preferences.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void BackupTask(object sender, DoWorkEventArgs e)
    {
        try
        {
            _log.Log(LogLevel.Info, "Timed backup task started.");
            while (!_timeBackupWorker.CancellationPending)
            {
                Thread.Sleep(GlobalData.Preferences.TimedBackupInterval * 60 * 1000);
                _log.Log(LogLevel.Info, "Starting auto backup");
                await BackupProject();
            }
            e.Cancel = true;
            _log.Log(LogLevel.Info, "Timed backup task finished.");
        }
        catch (Exception _Ex)
        {
            _log.LogException(LogLevel.Error, _Ex,"Error in auto backup task.");
        }
    }

    /// <summary>
    /// Launches the timed backup task
    /// </summary>
    public void StartTimedBackup()
    {
        if (!GlobalData.Preferences.TimedBackup) { return; }
        _timeBackupWorker.DoWork += BackupTask;
        if (!_timeBackupWorker.IsBusy)
            _timeBackupWorker.RunWorkerAsync();
    }

    /// <summary>
    /// Stops the timed backup task
    /// </summary>
    public void StopTimedBackup()
    {
        if (_timeBackupWorker.IsBusy)
        {
            _timeBackupWorker.CancelAsync();
        }
        _timeBackupWorker.DoWork -= BackupTask;
    }

    public async Task BackupProject()
    {
        _log.Log(LogLevel.Info, $"Starting Project Backup at {GlobalData.Preferences.BackupDirectory}");
        try
        {
            //Creates backup directory if it doesn't exist
            if (!Directory.Exists(GlobalData.Preferences.BackupDirectory))
            {
                _log.Log(LogLevel.Info, "Backup dir not found, making it.");
                Directory.CreateDirectory(GlobalData.Preferences.BackupDirectory);
            }

            //Gets correct name for file
            _log.Log(LogLevel.Info, "Getting backup path and file to made");
            string _FileName = $"{_shell.StoryModel.ProjectFile.Name} as of {DateTime.Now}".Replace('/', ' ').Replace(':', ' ').Replace(".stbx", "");
            StorageFolder _BackupRoot = await StorageFolder.GetFolderFromPathAsync(GlobalData.Preferences.BackupDirectory.Replace(".stbx", ""));
            StorageFolder _BackupLocation = await _BackupRoot.CreateFolderAsync(_shell.StoryModel.ProjectFile.Name, CreationCollisionOption.OpenIfExists);
            _log.Log(LogLevel.Info, $"Backing up to {_BackupLocation.Path} as {_FileName}.zip");

            _log.Log(LogLevel.Info, "Writing file");
            StorageFolder _Temp = await StorageFolder.GetFolderFromPathAsync(GlobalData.RootDirectory);
            _Temp = await _Temp.CreateFolderAsync("Temp", CreationCollisionOption.ReplaceExisting);
            await _shell.StoryModel.ProjectFile.CopyAsync(_Temp, _shell.StoryModel.ProjectFile.Name,
                NameCollisionOption.ReplaceExisting);
            ZipFile.CreateFromDirectory(_Temp.Path, Path.Combine(_BackupLocation.Path, _FileName) + ".zip");

            //Creates zip archive then cleans up
            _log.Log(LogLevel.Info, $"Created Zip file at {Path.Combine(_BackupLocation.Path, _FileName)}.zip");
            await _Temp.DeleteAsync();

            //Creates entry and flushes to disk.
            _log.Log(LogLevel.Info, "Finished backup.");
        }
        catch (Exception _Ex)
        {
            // ReSharper disable once AsyncVoidLambda
            DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
            {
                await new ContentDialog
                {
                    Title = "Backup Warning",
                    Content = "The last backup failed due to the following reason:\n" + _Ex.Message,
                    XamlRoot = GlobalData.XamlRoot,
                    CloseButtonText = "Understood."
                }.ShowAsync();
            });

            _log.LogException(LogLevel.Error, _Ex, $"Error backing up project {_Ex.Message}");
        }
        _log.Log(LogLevel.Info, "BackupProject complete");
    }
}