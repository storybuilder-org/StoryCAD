using System.ComponentModel;
using System.IO.Compression;
using Windows.Storage;
using Microsoft.UI;
using CommunityToolkit.Mvvm.Messaging;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.Services.Locking;
using StoryCAD.Services.Messages;
using System.Timers;

namespace StoryCAD.Services.Backup;

public class BackupService
{
    private BackgroundWorker timedBackupWorker;
    private Timer backupTimer;

    private readonly LogService _logService;
    private readonly PreferenceService _preferenceService;
    private readonly OutlineViewModel _outlineViewModel;
    private readonly AppState _appState;
    private readonly Windowing _windowing;
    // TODO: ShellViewModel removed due to circular dependency - needs architectural fix

    // Fields to preserve remaining time when paused
    private double defaultIntervalMs;
    private double? remainingIntervalMs;
    private DateTime lastStartTime;
    private bool isResumed;

    public bool IsRunning => backupTimer.Enabled;

    #region Constructor

    public BackupService(LogService logService, PreferenceService preferenceService, OutlineViewModel outlineViewModel, AppState appState, Windowing windowing)
    {
        _logService = logService;
        _preferenceService = preferenceService;
        _outlineViewModel = outlineViewModel;
        _appState = appState;
        _windowing = windowing;
        // TODO: _shellViewModel assignment removed due to circular dependency

        // Compute default interval once (in milliseconds)
        remainingIntervalMs = null;
        isResumed = false;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Enables or resumes the timed backup timer. If there was a remaining interval from a previous stop,
    /// it will resume with that remaining time; otherwise it starts a fresh countdown.
    /// </summary>
    public void StartTimedBackup()
    {
        // Don't start if no project is loaded
        if (String.IsNullOrEmpty(_outlineViewModel.StoryModelFile))
        {
            return;
        }

        defaultIntervalMs = _preferenceService.Model.TimedBackupInterval * 60 * 1000;

        if (backupTimer == null)
        {
            timedBackupWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = false
            };
            timedBackupWorker.DoWork += RunBackupTask;

            backupTimer = new System.Timers.Timer(defaultIntervalMs)
            {
                AutoReset = true
            };
            backupTimer.Elapsed += BackupTimer_Elapsed;
        }

        if (!_preferenceService.Model.TimedBackup)
            return;

        // If already running, do nothing
        if (backupTimer.Enabled)
            return;

        double intervalToUse = defaultIntervalMs;
        // If we previously stopped and have a positive remaining interval, resume from there
        if (remainingIntervalMs.HasValue && remainingIntervalMs.Value > 0)
        {
            intervalToUse = remainingIntervalMs.Value;
            isResumed = true;
        }
        else
        {
            isResumed = false;
        }

        backupTimer.Interval = intervalToUse;
        lastStartTime = DateTime.Now;
        backupTimer.Start();
    }

    /// <summary>
    /// Stops (pauses) the timed backup task and preserves the remaining time until the next backup.
    /// </summary>
    public void StopTimedBackup()
    {
        if (backupTimer != null)
        {
            if (!backupTimer.Enabled)
                return;

            // Calculate how much time is left in the current interval
            double elapsedMs = (DateTime.Now - lastStartTime).TotalMilliseconds;
            double rem = backupTimer.Interval - elapsedMs;
            remainingIntervalMs = rem > 0 ? rem : 0;

            backupTimer.Stop();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// The worker method that performs the backup asynchronously.
    /// </summary>
    private async void RunBackupTask(object sender, DoWorkEventArgs e)
    {
        try
        {
            _logService.Log(LogLevel.Info, "Starting timed backup task.");
            await BackupProject();
            _logService.Log(LogLevel.Info, "Timed backup task finished.");
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Error in auto backup task.");
        }
    }

    /// <summary>
    /// Fired when the timer elapses. Launches the background backup worker.
    /// After the first tick on a resumed interval, resets the interval to the default.
    /// </summary>
    private void BackupTimer_Elapsed(object source, ElapsedEventArgs e)
    {
        // Update UI indicator on the UI thread
        _windowing.GlobalDispatcher.TryEnqueue(() =>
        {
            // TODO: Circular dependency - BackupService and ShellViewModel
            // Temporary workaround: Use service locator until architectural fix
            Ioc.Default.GetRequiredService<ShellViewModel>().BackupStatusColor = Colors.Red;
        });

        if (!timedBackupWorker.IsBusy)
            timedBackupWorker.RunWorkerAsync();

        // If we had resumed from a smaller interval, reset to default for subsequent ticks
        if (isResumed)
        {
            backupTimer.Interval = defaultIntervalMs;
            isResumed = false;
        }

        // Prepare for the next interval
        lastStartTime = DateTime.Now;
        remainingIntervalMs = null;
    }

    #endregion

    /// <summary>
    /// Creates a backup. If Filename or FilePath are null, defaults are used.
    /// </summary>
    /// <param name="Filename">If null, the story model filename plus timestamp is used.</param>
    /// <param name="FilePath">If null, the user’s BackupDirectory preference is used.</param>
    public async Task BackupProject(string Filename = null, string FilePath = null)
    {
        // TODO: Circular dependency - BackupService ↔ AutoSaveService
        // AutoSaveService requires BackupService via SerializationLock, creating a circular dependency
        var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();

        // Determine filenames and paths
        var originalFileName = Path.GetFileNameWithoutExtension(_outlineViewModel.StoryModelFile);

        if (Filename is null)
        {
            // safe timestamp avoids ':' and other invalid chars
            Filename = $"{originalFileName} as of {DateTime.Now:yyyyMMdd_HHmm}";

            // scrub anything Windows forbids in file names
            foreach (var bad in Path.GetInvalidFileNameChars())
                Filename = Filename.Replace(bad, '_');
        }

        FilePath ??= _preferenceService.Model.BackupDirectory;


        _logService.Log(LogLevel.Info, $"Starting Project Backup at {FilePath}");
        try
        {
            // Create backup directory if missing
            if (!Directory.Exists(FilePath))
            {
                _logService.Log(LogLevel.Info, "Backup dir not found, making it.");
                Directory.CreateDirectory(FilePath);
            }

            _logService.Log(LogLevel.Info, $"Backing up to {FilePath} as {Filename}.zip");
            _logService.Log(LogLevel.Info, "Writing file");

            StorageFolder rootFolder = await StorageFolder.GetFolderFromPathAsync(
                _appState.RootDirectory);

            //Save file.
            using (var serializationLock = new SerializationLock(autoSaveService, this, _logService))
            {
                StorageFolder tempFolder = await rootFolder.CreateFolderAsync(
                    "Temp", CreationCollisionOption.ReplaceExisting);

                StorageFile projectFile = await StorageFile.GetFileFromPathAsync(_outlineViewModel.StoryModelFile);
                await projectFile.CopyAsync(tempFolder, projectFile.Name, NameCollisionOption.ReplaceExisting);

                string zipFilePath = Path.Combine(FilePath, Filename) + ".zip";
                ZipFile.CreateFromDirectory(tempFolder.Path, zipFilePath);

                _logService.Log(LogLevel.Info, $"Created Zip file at {zipFilePath}");
                await tempFolder.DeleteAsync();
            }
            
            //update indicator.
            _windowing.GlobalDispatcher.TryEnqueue(() =>
                // TODO: Circular dependency - BackupService and ShellViewModel
            // Temporary workaround: Use service locator until architectural fix
            Ioc.Default.GetRequiredService<ShellViewModel>().BackupStatusColor = Colors.Green
            );
            _logService.Log(LogLevel.Info, "Finished backup.");
        }
        catch (Exception ex)
        {
            if (!_appState.Headless)
            {
                _windowing.GlobalDispatcher.TryEnqueue(() =>
                {
                    WeakReferenceMessenger.Default.Send(new StatusChangedMessage(new StatusMessage(
                        "Making a backup failed, check your backup settings.",
                        LogLevel.Warn,
                        false)));
                });
                // TODO: Circular dependency - BackupService and ShellViewModel
            // Temporary workaround: Use service locator until architectural fix
            Ioc.Default.GetRequiredService<ShellViewModel>().BackupStatusColor = Colors.Red;
            }
            _logService.LogException(LogLevel.Error, ex, $"Error backing up project: {ex.Message}");
        }

        _logService.Log(LogLevel.Info, "BackupProject complete");
    }
}
