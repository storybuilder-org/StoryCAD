using StoryCADLib.Services.Locking;

namespace StoryCADLib.Services;

/// <summary>
///     Service responsible for flushing UI edits from ViewModels to the Model.
///     Eliminates circular dependencies by using AppState instead of direct ViewModel references.
/// </summary>
public class EditFlushService
{
    private readonly AppState _appState;
    private readonly ILogService _logger;

    public EditFlushService(AppState appState, ILogService logger)
    {
        _appState = appState;
        _logger = logger;
    }

    /// <summary>
    ///     Flushes edits from the current saveable ViewModel to the model.
    ///     Safe to call even when CurrentSaveable is null.
    /// </summary>
    public void FlushCurrentEdits()
    {
        try
        {
            _appState.CurrentSaveable?.SaveModel();
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, "Error flushing edits to model");
        }
    }

    /// <summary>
    ///     Issue #1645: when the main window goes inactive, copy the open page's edits into the model.
    ///     The Collaborator window is not modal; a click in it deactivates the main window, so each
    ///     workflow run and chat message reads what the writer sees. In memory only.
    ///     The state is an enum because its type differs by head: Microsoft.UI.Xaml.WindowActivationState
    ///     on Windows, Windows.UI.Core.CoreWindowActivationState on the Uno desktop head. Both name the
    ///     value Deactivated.
    ///     Skipped while SerializationLock is held: AutoSave and Backup copy the edits and write the
    ///     file under it, and a copy now could change the model mid-write (as RunExclusiveAsync skips
    ///     UI work while busy).
    /// </summary>
    public void OnMainWindowActivationChanged(Enum state)
    {
        if (state?.ToString() == "Deactivated" && SerializationLock.IsIdle)
        {
            FlushCurrentEdits();
        }
    }
}
