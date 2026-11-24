using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Services.Messages;

namespace StoryCADLib.Services.Outline;

/// <summary>
///     The writer's answer to "Save changes?".
/// </summary>
internal enum SaveChangesAnswer
{
    Yes,
    No,
    Cancel
}

/// <summary>
///     Asks "Save changes?" before the open outline is replaced or closed (issue #1636).
///     Every way of leaving an outline calls <see cref="ConfirmLeaveAsync" />, so the question
///     and its three answers are the same everywhere.
/// </summary>
internal class SaveChangesService
{
    internal const string SaveErrorMessage = "Save error. The outline stays open.";

    private readonly AppState _appState;
    private readonly EditFlushService _editFlushService;
    private readonly ILogService _logger;
    private readonly OutlineService _outlineService;
    private readonly Windowing _windowing;

    public SaveChangesService(AppState appState, Windowing windowing, EditFlushService editFlushService,
        OutlineService outlineService, ILogService logger)
    {
        _appState = appState;
        _windowing = windowing;
        _editFlushService = editFlushService;
        _outlineService = outlineService;
        _logger = logger;
    }

    /// <summary>
    ///     The answer used when no dialog can show (unit tests).
    /// </summary>
    internal SaveChangesAnswer HeadlessAnswer { get; set; } = SaveChangesAnswer.No;

    /// <summary>
    ///     If the open outline has unsaved changes, asks "Save changes?" and saves on Yes.
    ///     The caller holds the SerializationLock.
    /// </summary>
    /// <param name="cancelMessage">Status message to show when the writer answers Cancel.</param>
    /// <returns>
    ///     True when the caller can go on: nothing to save, Yes and saved, or No. False when it
    ///     must stop: Cancel, or Yes and the save failed. The status message for a false result
    ///     is sent here; the caller sends none.
    /// </returns>
    internal async Task<bool> ConfirmLeaveAsync(string cancelMessage)
    {
        var document = _appState.CurrentDocument;
        if (document?.Model == null || !document.Model.Changed || string.IsNullOrEmpty(document.FilePath))
        {
            return true;
        }

        var answer = await AskAsync();
        switch (answer)
        {
            case SaveChangesAnswer.No:
                return true;

            case SaveChangesAnswer.Yes:
                try
                {
                    _editFlushService.FlushCurrentEdits();
                    await _outlineService.WriteModel(document.Model, document.FilePath);
                    _outlineService.SetChanged(document.Model, false);
                    WeakReferenceMessenger.Default.Send(new IsChangedMessage(false));
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogException(LogLevel.Error, ex, "Save failed at Save changes?");
                    WeakReferenceMessenger.Default.Send(
                        new StatusChangedMessage(new StatusMessage(SaveErrorMessage, LogLevel.Error)));
                    return false;
                }

            default:
                WeakReferenceMessenger.Default.Send(
                    new StatusChangedMessage(new StatusMessage(cancelMessage, LogLevel.Info)));
                return false;
        }
    }

    private async Task<SaveChangesAnswer> AskAsync()
    {
        if (_appState.Headless)
        {
            return HeadlessAnswer;
        }

        try
        {
            // force: another dialog (the Open/Create dialog) can be open when the question is asked.
            var result = await _windowing.ShowContentDialog(new ContentDialog
            {
                Title = "Save changes?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                CloseButtonText = "Cancel"
            }, true);

            return result switch
            {
                ContentDialogResult.Primary => SaveChangesAnswer.Yes,
                ContentDialogResult.Secondary => SaveChangesAnswer.No,
                _ => SaveChangesAnswer.Cancel // the Cancel button, Escape, or no dialog shown
            };
        }
        catch (Exception ex)
        {
            // The question could not be shown. Staying in the outline loses nothing.
            _logger.LogException(LogLevel.Error, ex, "Save changes? could not be shown");
            return SaveChangesAnswer.Cancel;
        }
    }
}
