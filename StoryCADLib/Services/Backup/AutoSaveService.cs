using System.ComponentModel;

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
        private Windowing Window = Ioc.Default.GetRequiredService<Windowing>();
        private LogService _logger = Ioc.Default.GetRequiredService<LogService>();
        private AppState State = Ioc.Default.GetRequiredService<AppState>();
        private PreferenceService Preferences = Ioc.Default.GetRequiredService<PreferenceService>();
        private ShellViewModel _shellVM;

        private BackgroundWorker autoSaveWorker;
        private System.Timers.Timer autoSaveTimer;

        #region Constructor

        public AutoSaveService()
        {
            autoSaveWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = false
            };
            autoSaveWorker.DoWork += RunAutoSaveTask;

            //TODO: Move the following line to Preferences, add appropriate Status and logging
            if (Preferences.Model.AutoSaveInterval is > 61 or < 14)
                Preferences.Model.AutoSaveInterval = 30;
            autoSaveTimer = new System.Timers.Timer();
            autoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
            autoSaveTimer.Interval = Preferences.Model.AutoSaveInterval * 1000;
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
            autoSaveTimer.Interval = Preferences.Model.AutoSaveInterval * 1000;
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
            _shellVM = Ioc.Default.GetService<ShellViewModel>();
            try
            {
                if (autoSaveWorker.CancellationPending || !Preferences.Model.AutoSave ||
                    _shellVM!.StoryModel.StoryElements.Count == 0)
                {
                    return Task.CompletedTask;
                }

                if (_shellVM.StoryModel.Changed)
                {
                    _logger.Log(LogLevel.Info, "Initiating AutoSave backup.");
                    // Save and write the model on the UI thread
                    Window.GlobalDispatcher.TryEnqueue(async () => await _shellVM.SaveFile(true));
                }
            }
            catch (Exception _ex)
            {
                //Show failed message.
                Window.GlobalDispatcher.TryEnqueue(() =>
                {
                    Ioc.Default.GetRequiredService<ShellViewModel>().ShowMessage(LogLevel.Warn,
                        "Making an AutoSave failed.", false);
                });
                _logger.LogException(LogLevel.Error, _ex,
                    $"Error saving file in AutoSaveService.AutoSaveProject() {_ex.Message}");
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}

