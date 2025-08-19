using System.ComponentModel;
using StoryCAD.Services.Locking;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.Services.Backup
{
    /// <summary>
    /// Automatically save the active project at regular intervals.
    ///
    /// This class uses a BackgroundWorker to run the AutoSave task.
    /// The worker runs on a separate thread from the UI thread and
    /// is event-driven by a timer.  The timer is set to the user's
    /// specified PreferencesAutoSaveInterval (in seconds).
    /// </summary>
    public class AutoSaveService
    {
        private readonly Windowing _window;
        private readonly LogService _logger;
        private readonly AppState _appState;
        private readonly PreferenceService _preferenceService;
        private readonly OutlineViewModel _outlineVM;
        // TODO: ShellViewModel removed due to circular dependency - needs architectural fix

        private BackgroundWorker autoSaveWorker;
        private System.Timers.Timer autoSaveTimer;
        /// <summary>
        /// Returns the running status of the AutoSave service.
        /// </summary>
        public bool IsRunning => autoSaveTimer.Enabled;

        #region Constructor

        // Constructor for backward compatibility - will be removed later
        public AutoSaveService() : this(
            Ioc.Default.GetRequiredService<Windowing>(),
            Ioc.Default.GetRequiredService<LogService>(),
            Ioc.Default.GetRequiredService<AppState>(),
            Ioc.Default.GetRequiredService<PreferenceService>(),
            Ioc.Default.GetRequiredService<OutlineViewModel>(),
            null) // TODO: Circular dependency with ShellViewModel - needs architectural fix
        {
        }

        public AutoSaveService(Windowing window, LogService logger, AppState appState, PreferenceService preferenceService, OutlineViewModel outlineViewModel, ShellViewModel shellViewModel = null)
        {
            _window = window;
            _logger = logger;
            _appState = appState;
            _preferenceService = preferenceService;
            _outlineVM = outlineViewModel;
            // TODO: _shellViewModel assignment removed due to circular dependency

            autoSaveWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = false
            };
            autoSaveWorker.DoWork += RunAutoSaveTask;

            //TODO: Move the following line to Preferences, add appropriate Status and logging
            if (_preferenceService.Model.AutoSaveInterval is > 61 or < 14)
                _preferenceService.Model.AutoSaveInterval = 30;
            autoSaveTimer = new System.Timers.Timer();
            autoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
            autoSaveTimer.Interval = _preferenceService.Model.AutoSaveInterval * 1000;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts the AutoSave Service
        /// </summary>
        public void StartAutoSave()
        {
            // If the timer is already running, stop it
            if (autoSaveTimer.Enabled)
                autoSaveTimer.Stop();

            // Reset the timer and start it 
            autoSaveTimer.Interval = _preferenceService.Model.AutoSaveInterval * 1000;
            autoSaveTimer.Start();
        }

        /// <summary>
        /// Stops AutoSave service
        /// </summary>
        public void StopAutoSave()
        {
            autoSaveTimer.Stop();
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Launches the AutoSave task when the timer elapses
        /// </summary>
        private void AutoSaveTimer_Elapsed(object source, System.Timers.ElapsedEventArgs e)
        {
            if (!autoSaveWorker.IsBusy)
                autoSaveWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Performs an AutoSave when triggered by AutoSaveTimer_Elapsed (the autoSaveTimer event)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RunAutoSaveTask(object sender, DoWorkEventArgs e)
        {
            try
            {
                _logger.Log(LogLevel.Info, "Invoking AutoSave.");
                await AutoSaveProject();
                //_logger.Log(LogLevel.Info, "AutoSave task finished.");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, ex, "Error in AutoSave task.");
            }
        }

        private Task AutoSaveProject()
        {
            var backupService = Ioc.Default.GetRequiredService<BackupService>();
            var logService = _logger;

            using (var serializationLock = new SerializationLock(this, backupService, _logger))
            {
                try
                {
                    if (autoSaveWorker.CancellationPending || !_preferenceService.Model.AutoSave ||
                        _outlineVM.StoryModel.StoryElements.Count == 0)
                    {
                        return Task.CompletedTask;
                    }

                    if (_outlineVM.StoryModel.Changed)
                    {
                        _logger.Log(LogLevel.Info, "Initiating AutoSave backup.");
                        // Save and write the model on the UI thread
                        _window.GlobalDispatcher.TryEnqueue(async () => await _outlineVM.SaveFile(true));
                    }
                }
                catch (Exception _ex)
                {
                    //Show failed message.
                    _window.GlobalDispatcher.TryEnqueue(() =>
                    {
                        // TODO: Circular dependency - ShellViewModel depends on AutoSaveService and vice versa
                        // Temporary workaround: Use service locator until architectural fix
                        Ioc.Default.GetRequiredService<ShellViewModel>().ShowMessage(LogLevel.Warn,
                            "Making an AutoSave failed.", false);
                    });
                    _logger.LogException(LogLevel.Error, _ex,
                        $"Error saving file in AutoSaveService.AutoSaveProject() {_ex.Message}");
                }

                return Task.CompletedTask;
            }
        }

        #endregion
    }
}

