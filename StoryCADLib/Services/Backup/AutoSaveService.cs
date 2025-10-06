using System.Timers;
using CommunityToolkit.Mvvm.Messaging;
using StoryCAD.Services.Locking;
using StoryCAD.Services.Messages;
using StoryCAD.Services.Outline;
using Timer = System.Timers.Timer;

// <-- use non-UI timer

namespace StoryCAD.Services.Backup;

public class AutoSaveService : IDisposable
{
    private readonly AppState _appState;
    private readonly SemaphoreSlim _autoSaveGate = new(1, 1);

    private readonly Timer _autoSaveTimer; // System.Timers.Timer
    private readonly EditFlushService _editFlushService;
    private readonly ILogService _logger;
    private readonly OutlineService _outlineService;
    private readonly PreferenceService _preferenceService;
    private readonly Windowing _windowing; // <-- Windowing (mockable), not Window

    public AutoSaveService(
        AppState appState,
        PreferenceService preferenceService,
        EditFlushService editFlushService,
        OutlineService outlineService,
        Windowing windowing, // <-- use Windowing here
        ILogService logger)
    {
        _appState = appState;
        _preferenceService = preferenceService;
        _editFlushService = editFlushService;
        _outlineService = outlineService;
        _windowing = windowing; // <-- store Windowing
        _logger = logger;

        // Interval is milliseconds for System.Timers.Timer
        _autoSaveTimer = new Timer(_preferenceService.Model.AutoSaveInterval * 1000.0)
        {
            AutoReset = true,
            Enabled = false
        };
        _autoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
    }

    public bool IsRunning => _autoSaveTimer.Enabled;
    public bool IsStarted => _autoSaveTimer.Enabled;

    public void Dispose()
    {
        _autoSaveTimer.Stop();
        _autoSaveTimer.Elapsed -= AutoSaveTimer_Elapsed;
        _autoSaveTimer.Dispose();
    }

    // original API names
    public void StartAutoSave() => _autoSaveTimer.Start();
    public void StopAutoSave() => _autoSaveTimer.Stop();

    /// <summary>
    ///     Stops auto-save and waits for any in-progress save to complete
    /// </summary>
    public async Task StopAutoSaveAndWaitAsync()
    {
        _autoSaveTimer.Stop();
        // Wait for any in-progress save to complete
        await _autoSaveGate.WaitAsync();
        _autoSaveGate.Release();
    }

    private async void AutoSaveTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (!await _autoSaveGate.WaitAsync(0))
        {
            return; // avoid overlap
        }

        try
        {
            await AutoSaveProjectAsync(); // <-- no GetAwaiter().GetResult()
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, "Error in AutoSave task.");
        }
        finally
        {
            _autoSaveGate.Release();
        }
    }

    private async Task AutoSaveProjectAsync()
    {
        if (!_preferenceService.Model.AutoSave)
        {
            return;
        }

        if (_appState.CurrentDocument?.Model?.StoryElements?.Count == 0)
        {
            return;
        }

        if (!(_appState.CurrentDocument?.Model?.Changed ?? false))
        {
            return;
        }

        _logger.Log(LogLevel.Info, "Initiating AutoSave.");

        using (new SerializationLock(_logger))
        {
            // flush UI edits on the UI thread and await completion
            await _windowing.GlobalDispatcher.EnqueueAsync(() => { _editFlushService.FlushCurrentEdits(); });

            // perform the file write under the same lock
            await _outlineService.WriteModel(
                _appState.CurrentDocument.Model,
                _appState.CurrentDocument.FilePath);
            if (!_appState.Headless)
            {
                _windowing.GlobalDispatcher.TryEnqueue(() =>
                {
                    // Indicate the model is now saved and unchanged
                    WeakReferenceMessenger.Default.Send(new IsChangedMessage(false));
                });
            }
            else
            {
                _appState.CurrentDocument.Model.Changed = false;
            }
        }
    }
}
