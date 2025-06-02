using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.UI;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.Services.Locking;

namespace StoryCAD.Services.Backup
{
    public class BackupService
    {
        private BackgroundWorker timedBackupWorker;
        private System.Timers.Timer backupTimer;

        private readonly LogService Log = Ioc.Default.GetService<LogService>();
        private readonly PreferenceService Prefs = Ioc.Default.GetService<PreferenceService>();
        private readonly OutlineViewModel OutlineManager = Ioc.Default.GetService<OutlineViewModel>();
        private ShellViewModel _shellVM;

        // Fields to preserve remaining time when paused
        private double defaultIntervalMs;
        private double? remainingIntervalMs;
        private DateTime lastStartTime;
        private bool isResumed;

        public bool IsRunning => backupTimer.Enabled;

        #region Constructor

        public BackupService()
        {
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
            defaultIntervalMs = Prefs.Model.TimedBackupInterval * 60 * 1000;

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

            if (!Prefs.Model.TimedBackup)
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
                Log.Log(LogLevel.Info, "Starting timed backup task.");
                await BackupProject();
                Log.Log(LogLevel.Info, "Timed backup task finished.");
            }
            catch (Exception ex)
            {
                Log.LogException(LogLevel.Error, ex, "Error in auto backup task.");
            }
        }

        /// <summary>
        /// Fired when the timer elapses. Launches the background backup worker.
        /// After the first tick on a resumed interval, resets the interval to the default.
        /// </summary>
        private void BackupTimer_Elapsed(object source, System.Timers.ElapsedEventArgs e)
        {
            // Update UI indicator on the UI thread
            Ioc.Default.GetRequiredService<Windowing>().GlobalDispatcher.TryEnqueue(() =>
            {
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
            var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
            _shellVM = Ioc.Default.GetService<ShellViewModel>();

            // Determine filenames and paths
            string originalFileName = Path.GetFileName(OutlineManager.StoryModelFile);
            Filename ??= $"{originalFileName} as of {DateTime.Now}"
                            .Replace('/', ' ')
                            .Replace(':', ' ')
                            .Replace(".stbx", "");
            FilePath ??= Prefs.Model.BackupDirectory.Replace(".stbx", "");

            Log.Log(LogLevel.Info, $"Starting Project Backup at {FilePath}");
            try
            {
                // Create backup directory if missing
                if (!Directory.Exists(FilePath))
                {
                    Log.Log(LogLevel.Info, "Backup dir not found, making it.");
                    Directory.CreateDirectory(FilePath);
                }

                Log.Log(LogLevel.Info, $"Backing up to {FilePath} as {Filename}.zip");
                Log.Log(LogLevel.Info, "Writing file");

                StorageFolder rootFolder = await StorageFolder.GetFolderFromPathAsync(
                    Ioc.Default.GetRequiredService<AppState>().RootDirectory);

                using (var serializationLock = new SerializationLock(autoSaveService, this, Log))
                {
                    StorageFolder tempFolder = await rootFolder.CreateFolderAsync(
                        "Temp", CreationCollisionOption.ReplaceExisting);

                    StorageFile projectFile = await StorageFile.GetFileFromPathAsync(OutlineManager.StoryModelFile);
                    await projectFile.CopyAsync(tempFolder, projectFile.Name, NameCollisionOption.ReplaceExisting);

                    string zipFilePath = Path.Combine(FilePath, Filename) + ".zip";
                    ZipFile.CreateFromDirectory(tempFolder.Path, zipFilePath);

                    Log.Log(LogLevel.Info, $"Created Zip file at {zipFilePath}");
                    await tempFolder.DeleteAsync();
                }
                Ioc.Default.GetRequiredService<Windowing>().GlobalDispatcher.TryEnqueue(() =>
                    Ioc.Default.GetRequiredService<ShellViewModel>().BackupStatusColor = Colors.Green
                );
                Log.Log(LogLevel.Info, "Finished backup.");
            }
            catch (Exception ex)
            {
                if (!Ioc.Default.GetRequiredService<AppState>().Headless)
                {
                    Ioc.Default.GetRequiredService<Windowing>().GlobalDispatcher.TryEnqueue(() =>
                    {
                        Ioc.Default.GetRequiredService<ShellViewModel>().ShowMessage(
                            LogLevel.Warn,
                            "Making a backup failed, check your backup settings.",
                            false);
                    });
                }
                Log.LogException(LogLevel.Error, ex, $"Error backing up project: {ex.Message}");
                Ioc.Default.GetRequiredService<ShellViewModel>().BackupStatusColor = Colors.Red;
            }

            Log.Log(LogLevel.Info, "BackupProject complete");
        }
    }
}
