using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.DAL;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Locking;
using StoryCADLib.Services.Messages;
using static CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger;

namespace StoryCADLib.Services.Outline;

/// <summary>
///     Service for opening story files without dependencies on ViewModels
/// </summary>
public class FileOpenService
{
    /// <summary>
    ///     Status message when an open is cancelled, at "Save changes?" or later.
    /// </summary>
    internal const string OpenCancelledMessage = "Open Story command cancelled";

    private readonly AppState _appState;
    private readonly AutoSaveService _autoSaveService;
    private readonly BackupService _backupService;
    private readonly ILogService _logger;
    private readonly OutlineService _outlineService;
    private readonly PreferenceService _preferences;
    private readonly StoryIO _storyIO;
    private readonly Windowing _windowing;

    public FileOpenService(
        ILogService logger,
        OutlineService outlineService,
        AppState appState,
        EditFlushService editFlushService,
        PreferenceService preferences,
        Windowing windowing,
        BackupService backupService,
        AutoSaveService autoSaveService,
        StoryIO storyIO)
    {
        _logger = logger;
        _outlineService = outlineService;
        _appState = appState;
        _preferences = preferences;
        _windowing = windowing;
        _backupService = backupService;
        _autoSaveService = autoSaveService;
        _storyIO = storyIO;
    }

    /// <summary>
    ///     Opens a file picker to let the user chose a .stbx file and loads said file
    ///     If fromPath is specified then the picker is skipped.
    /// </summary>
    /// <param name="fromPath">Path to open file from (Optional)</param>
    public async Task OpenFile(string fromPath = "")
    {
        try
        {
            // True once the file is the current document. A cancelled or failed open must not
            // run the steps after the lock on the outline that is still open (issue #1636).
            var opened = false;

            await SerializationLock.RunExclusiveAsync(async ct =>
            {
                _logger.Log(LogLevel.Info, "Executing OpenFile command");

                // Open file picker if `fromPath` is not provided or file doesn't exist at the path.
                // The picker comes before "Save changes?", so cancelling it asks and writes nothing.
                if (fromPath == "" || !File.Exists(fromPath))
                {
                    _logger.Log(LogLevel.Info, "Opening file picker as story wasn't able to be found");
                    var projectFile = await _windowing.ShowFilePicker("Open Project File", ".stbx");
                    if (projectFile == null) //Picker was canceled.
                    {
                        _logger.Log(LogLevel.Info, "Open file picker cancelled.");
                        return;
                    }

                    fromPath = projectFile.Path;
                }

                // Ask before the open outline is replaced. The service skips a document with no
                // file path (issue #1153: ResetModel creates one).
                if (!await Ioc.Default.GetRequiredService<SaveChangesService>()
                        .ConfirmLeaveAsync(OpenCancelledMessage))
                {
                    return;
                }

                // Reset the model and show the home page
                // Note: These UI operations should be handled via messaging
                // For now, we'll send status messages and rely on the caller to handle UI updates
                Default.Send(new StatusChangedMessage(new StatusMessage("Resetting model", LogLevel.Info)));

                var filePath = fromPath;
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.Log(LogLevel.Warn, "Open File command failed: StoryModel.ProjectFile is null.");
                    Default.Send(
                        new StatusChangedMessage(new StatusMessage(OpenCancelledMessage, LogLevel.Info)));
                    return;
                }

                if (!File.Exists(filePath))
                {
                    Default.Send(new StatusChangedMessage(
                        new StatusMessage($"Cannot find file {filePath}", LogLevel.Warn, true)));
                    return;
                }

                //Check file is available.
                if (!await _storyIO.CheckFileAvailability(filePath))
                {
                    Default.Send(new StatusChangedMessage(new StatusMessage("File Unavailable.", LogLevel.Warn, true)));
                    return;
                }

                //Read file
                var loadedModel = await _outlineService.OpenFile(filePath);

                //Check the file we loaded actually has StoryCAD Data.
                if (loadedModel == null)
                {
                    Default.Send(new StatusChangedMessage(
                        new StatusMessage("Unable to open file (No Story Elements found)", LogLevel.Warn, true)));
                    return;
                }

                if (loadedModel.StoryElements.Count == 0)
                {
                    Default.Send(new StatusChangedMessage(
                        new StatusMessage("Unable to open file (No Story Elements found)", LogLevel.Warn, true)));
                    return;
                }

                // Successfully loaded - create StoryDocument
                _appState.CurrentDocument = new StoryDocument(loadedModel, filePath);
                opened = true;

            }, CancellationToken.None, _logger);

            if (!opened)
            {
                return;
            }

            // Take a backup of the project if the user has the 'backup on open' preference set.
            if (_preferences.Model.BackupOnOpen)
            {
                await _backupService.BackupProject();
            }

            // Set the current view to the ExplorerView
            if (_appState.CurrentDocument?.Model?.ExplorerView?.Count > 0)
            {
                _outlineService.SetCurrentView(_appState.CurrentDocument.Model, StoryViewType.ExplorerView);
                Default.Send(new StatusChangedMessage(new StatusMessage("Open Story completed", LogLevel.Info)));

            }

            _windowing.UpdateWindowTitle();
            await UpdateRecents(_appState.CurrentDocument?.FilePath);

            if (_preferences.Model.TimedBackup)
            {
                _backupService.StartTimedBackup();
            }

            if (_preferences.Model.AutoSave)
            {
                _autoSaveService.StartAutoSave();
            }

            _logger.Log(LogLevel.Info, $"Opened project {_appState.CurrentDocument?.FilePath}");

            // Track outline open for usage statistics
            if (_appState.CurrentDocument?.Model != null)
            {
                var model = _appState.CurrentDocument.Model;
                var overview = model.StoryElements.OfType<Models.OverviewModel>().FirstOrDefault();
                if (overview != null)
                {
                    Ioc.Default.GetService<Backend.IUsageTrackingService>()?.OutlineOpened(
                        overview.Uuid, overview.StoryGenre, overview.StoryType, model.StoryElements.Count);
                }
            }
        }
        catch (Exception ex)
        {
            // Report the error to the user
            _logger.LogException(LogLevel.Error, ex, "Error in OpenFile command");
            Default.Send(new StatusChangedMessage(new StatusMessage("Open Story command failed", LogLevel.Error)));
        }

        _logger.Log(LogLevel.Info, "Open Story completed.");
    }

    /// <summary>
    ///     Updates the recent files list with the specified file path
    /// </summary>
    public async Task UpdateRecents(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        // This method was originally in FileOpenVM
        // Implementation depends on how recents are stored (likely in PreferenceService)
        if (_preferences.Model.RecentFiles.Contains(filePath))
        {
            _preferences.Model.RecentFiles.Remove(filePath);
        }

        _preferences.Model.RecentFiles.Insert(0, filePath);

        // Keep only the most recent files (e.g., 10)
        while (_preferences.Model.RecentFiles.Count > 10)
        {
            _preferences.Model.RecentFiles.RemoveAt(_preferences.Model.RecentFiles.Count - 1);
        }

        PreferencesIo loader = new();
        await loader.WritePreferences(_preferences.Model);
    }
}
