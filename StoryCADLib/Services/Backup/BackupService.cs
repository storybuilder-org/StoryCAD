using System.IO.Compression;
using System.Timers;
using CommunityToolkit.Mvvm.Messaging;
using StoryCAD.Services.Locking;
using StoryCAD.Services.Messages;
using Timer = System.Timers.Timer;

namespace StoryCAD.Services.Backup;

public class BackupService
{
    private readonly AppState _appState;
    private readonly SemaphoreSlim _backupGate = new(1, 1);

    private readonly ILogService _logService;
    private readonly PreferenceService _preferenceService;
    private readonly Windowing _windowing;
    private Timer backupTimer;

    // Fields to preserve remaining time when paused
    private double defaultIntervalMs;
    private bool isResumed;
    private DateTime lastStartTime;
    private double? remainingIntervalMs;

    #region Constructor

    public BackupService(ILogService logService, PreferenceService preferenceService, AppState appState,
        Windowing windowing)
    {
        _logService = logService;
        _preferenceService = preferenceService;
        _appState = appState;
        _windowing = windowing;

        // Compute default interval once (in milliseconds)
        remainingIntervalMs = null;
        isResumed = false;
    }

    #endregion

    public bool IsRunning => backupTimer.Enabled;

    /// <summary>
    ///     Creates a backup. If Filename or FilePath are null, defaults are used.
    /// </summary>
    /// <param name="Filename">If null, the story model filename plus timestamp is used.</param>
    /// <param name="FilePath">If null, the user’s BackupDirectory preference is used.</param>
    public async Task BackupProject(string Filename = null, string FilePath = null)
    {
        // Determine filenames and paths
        var originalFileName = Path.GetFileNameWithoutExtension(_appState.CurrentDocument?.FilePath ?? "Untitled");

        if (Filename is null)
        {
            // safe timestamp avoids ':' and other invalid chars
            Filename = $"{originalFileName} as of {DateTime.Now:yyyyMMdd_HHmm}";

            // scrub anything Windows forbids in file names
            foreach (var bad in Path.GetInvalidFileNameChars())
            {
                Filename = Filename.Replace(bad, '_');
            }
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

            var rootFolder = await StorageFolder.GetFolderFromPathAsync(
                _appState.RootDirectory);

            // Save file under the serialization lock (awaited work remains inside using scope)
            using (var serializationLock = new SerializationLock(_logService))
            {
                var tempFolder = await rootFolder.CreateFolderAsync(
                    "Temp", CreationCollisionOption.ReplaceExisting);

                var projectFile = await StorageFile.GetFileFromPathAsync(_appState.CurrentDocument!.FilePath);
                await projectFile.CopyAsync(tempFolder, projectFile.Name, NameCollisionOption.ReplaceExisting);

                var zipFilePath = Path.Combine(FilePath, Filename) + ".zip";
                ZipFile.CreateFromDirectory(tempFolder.Path, zipFilePath);

                _logService.Log(LogLevel.Info, $"Created Zip file at {zipFilePath}");
                await tempFolder.DeleteAsync();
            }

            // update indicator
            _windowing.GlobalDispatcher.TryEnqueue(() =>
            {
                // Indicate the model is now backed up
                WeakReferenceMessenger.Default.Send(new IsBackupStatusMessage(true));
            });
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
                        LogLevel.Warn)));
                });
                _windowing.GlobalDispatcher.TryEnqueue(() =>
                {
                    // Indicate the model is now saved and unchanged
                    WeakReferenceMessenger.Default.Send(new IsBackupStatusMessage(false));
                });
            }

            _logService.LogException(LogLevel.Error, ex, $"Error backing up project: {ex.Message}");
        }

        _logService.Log(LogLevel.Info, "BackupProject complete");
    }

    #region Public Methods

    /// <summary>
    ///     Enables or resumes the timed backup timer. If there was a remaining interval from a previous stop,
    ///     it will resume with that remaining time; otherwise it starts a fresh countdown.
    /// </summary>
    public void StartTimedBackup()
    {
        // Don't start if no project is loaded
        if (string.IsNullOrEmpty(_appState.CurrentDocument?.FilePath))
        {
            return;
        }

        defaultIntervalMs = _preferenceService.Model.TimedBackupInterval * 60 * 1000;

        if (backupTimer == null)
        {
            backupTimer = new Timer(defaultIntervalMs)
            {
                AutoReset = true
            };
            backupTimer.Elapsed += BackupTimer_Elapsed;
        }

        if (!_preferenceService.Model.TimedBackup)
        {
            return;
        }

        // If already running, do nothing
        if (backupTimer.Enabled)
        {
            return;
        }

        var intervalToUse = defaultIntervalMs;
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
    ///     Stops (pauses) the timed backup task and preserves the remaining time until the next backup.
    /// </summary>
    public void StopTimedBackup()
    {
        if (backupTimer != null)
        {
            if (!backupTimer.Enabled)
            {
                return;
            }

            // Calculate how much time is left in the current interval
            var elapsedMs = (DateTime.Now - lastStartTime).TotalMilliseconds;
            var rem = backupTimer.Interval - elapsedMs;
            remainingIntervalMs = rem > 0 ? rem : 0;

            backupTimer.Stop();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Performs the backup asynchronously with proper concurrency control.
    /// </summary>
    private async Task RunBackupTaskAsync()
    {
        // Skip if a backup is already running
        if (!await _backupGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            _logService.Log(LogLevel.Info, "Starting timed backup task.");
            await BackupProject();
            _logService.Log(LogLevel.Info, "Timed backup task finished.");

            // Indicate successful backup
            _windowing.GlobalDispatcher.TryEnqueue(() =>
            {
                WeakReferenceMessenger.Default.Send(new IsBackupStatusMessage(true));
            });
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Error in auto backup task.");

            // Indicate failed backup
            _windowing.GlobalDispatcher.TryEnqueue(() =>
            {
                WeakReferenceMessenger.Default.Send(new IsBackupStatusMessage(false));
            });
        }
        finally
        {
            _backupGate.Release();
        }
    }

    /// <summary>
    ///     Fired when the timer elapses. Performs the backup asynchronously.
    ///     After the first tick on a resumed interval, resets the interval to the default.
    /// </summary>
    private async void BackupTimer_Elapsed(object source, ElapsedEventArgs e)
    {
        await RunBackupTaskAsync();

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
}
