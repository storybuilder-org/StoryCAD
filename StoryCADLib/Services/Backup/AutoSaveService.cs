using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using StoryCAD.Services.Locking;
using StoryCAD.Services.Outline;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Timers;  // <-- use non-UI timer

namespace StoryCAD.Services.Backup
{
    public class AutoSaveService : IDisposable
    {
        private readonly AppState _appState;
        private readonly PreferenceService _preferenceService;
        private readonly EditFlushService _editFlushService;
        private readonly OutlineService _outlineService;
        private readonly Windowing _windowing;   // <-- Windowing (mockable), not Window
        private readonly ILogService _logger;

        private readonly Timer _autoSaveTimer;              // System.Timers.Timer
        private readonly BackgroundWorker _autoSaveWorker;

        public AutoSaveService(
            AppState appState,
            PreferenceService preferenceService,
            EditFlushService editFlushService,
            OutlineService outlineService,
            Windowing windowing,               // <-- use Windowing here
            ILogService logger)
        {
            _appState = appState;
            _preferenceService = preferenceService;
            _editFlushService = editFlushService;
            _outlineService = outlineService;
            _windowing = windowing;            // <-- store Windowing
            _logger = logger;

            // Interval is milliseconds for System.Timers.Timer
            _autoSaveTimer = new Timer(_preferenceService.Model.AutoSaveInterval * 1000.0)
            {
                AutoReset = true,
                Enabled = false
            };
            _autoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;

            _autoSaveWorker = new BackgroundWorker();
            _autoSaveWorker.DoWork += RunAutoSaveTask;
        }

        // original API names
        public void StartAutoSave() => _autoSaveTimer.Start();
        public void StopAutoSave()  => _autoSaveTimer.Stop();
        public bool IsRunning => _autoSaveTimer.Enabled;
        public bool IsStarted => _autoSaveTimer.Enabled;

        private void AutoSaveTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (!_autoSaveWorker.IsBusy)
            {
                _autoSaveWorker.RunWorkerAsync();
            }
        }

        private void RunAutoSaveTask(object? sender, DoWorkEventArgs e)
        {
            try
            {
                AutoSaveProjectAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, ex, "Error in AutoSave task.");
            }
        }

        private async Task AutoSaveProjectAsync()
        {
            if (!_preferenceService.Model.AutoSave) return;
            if (_appState.CurrentDocument?.Model?.StoryElements?.Count == 0) return;
            if (!(_appState.CurrentDocument?.Model?.Changed ?? false)) return;

            _logger.Log(LogLevel.Info, "Initiating AutoSave.");

            using (new SerializationLock(_logger))
            {
                // flush UI edits on the UI thread and await completion
                await _windowing.GlobalDispatcher.EnqueueAsync(() =>
                {
                    _editFlushService.FlushCurrentEdits();
                });

                // perform the file write under the same lock
                await _outlineService.WriteModel(
                    _appState.CurrentDocument.Model,
                    _appState.CurrentDocument.FilePath);
            }
        }

        public void Dispose()
        {
            _autoSaveTimer.Stop();
            _autoSaveTimer.Elapsed -= AutoSaveTimer_Elapsed;
            _autoSaveWorker.DoWork -= RunAutoSaveTask;
            _autoSaveTimer.Dispose();
        }
    }
}
