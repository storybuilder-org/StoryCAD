﻿using System.ComponentModel;
using System.IO.Compression;
using Windows.Storage;
using StoryCAD.ViewModels.SubViewModels;


namespace StoryCAD.Services.Backup;

public class BackupService
{
    private BackgroundWorker timedBackupWorker;
    private System.Timers.Timer backupTimer;

    private LogService Log = Ioc.Default.GetService<LogService>();
    private PreferenceService Prefs = Ioc.Default.GetService<PreferenceService>();
    private OutlineViewModel OutlineManager = Ioc.Default.GetService<OutlineViewModel>();
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
            (Prefs.Model.TimedBackupInterval * 60 * 1000);  // interval in minutes
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
        if (!Prefs.Model.TimedBackup)
            return;
            
        // If the timer is already running, stop it
        if (backupTimer.Enabled)
            backupTimer.Stop();

        // Reset the timer and start it 
        backupTimer.Interval = (Prefs.Model.TimedBackupInterval * 60 * 1000); // interval in minutes
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

    /// <summary>
    /// Creates a backup
    /// </summary>
    /// <param name="Filename">If null, will be the storymodel filename</param>
    /// <param name="FilePath">If null, will be the backup directory</param>
    /// <returns></returns>
    public async Task BackupProject(string Filename = null, string FilePath = null)
    {
        _shellVM = Ioc.Default.GetService<ShellViewModel>();

        // Use OutlineManager.StoryModelFile to get file details
        string originalFileName = Path.GetFileName(OutlineManager.StoryModelFile);
        Filename ??= $"{originalFileName} as of {DateTime.Now}"
                        .Replace('/', ' ')
                        .Replace(':', ' ')
                        .Replace(".stbx", "");
        FilePath ??= Prefs.Model.BackupDirectory.Replace(".stbx", "");

        Log.Log(LogLevel.Info, $"Starting Project Backup at {FilePath}");
        try
        {
            // Create backup directory if it doesn't exist
            if (!Directory.Exists(FilePath))
            {
                Log.Log(LogLevel.Info, "Backup dir not found, making it.");
                Directory.CreateDirectory(FilePath);
            }

            Log.Log(LogLevel.Info, $"Backing up to {FilePath} as {Filename}.zip");

            Log.Log(LogLevel.Info, "Writing file");
            StorageFolder rootFolder = await StorageFolder.GetFolderFromPathAsync(
                Ioc.Default.GetRequiredService<AppState>().RootDirectory);
            StorageFolder tempFolder = await rootFolder.CreateFolderAsync("Temp", CreationCollisionOption.ReplaceExisting);

            // Retrieve the project file using the file path
            StorageFile projectFile = await StorageFile.GetFileFromPathAsync(OutlineManager.StoryModelFile);
            await projectFile.CopyAsync(tempFolder, projectFile.Name, NameCollisionOption.ReplaceExisting);

            string zipFilePath = Path.Combine(FilePath, Filename) + ".zip";
            ZipFile.CreateFromDirectory(tempFolder.Path, zipFilePath);

            Log.Log(LogLevel.Info, $"Created Zip file at {zipFilePath}");
            await tempFolder.DeleteAsync();

            Log.Log(LogLevel.Info, "Finished backup.");
        }
        catch (Exception ex)
        {
            Ioc.Default.GetRequiredService<Windowing>().GlobalDispatcher.TryEnqueue(() =>
            {
                Ioc.Default.GetRequiredService<ShellViewModel>().ShowMessage(
                    LogLevel.Warn,
                    "Making a backup failed, check your backup settings.",
                    false);
            });
            Log.LogException(LogLevel.Error, ex, $"Error backing up project {ex.Message}");
        }
        Log.Log(LogLevel.Info, "BackupProject complete");
    }

}
