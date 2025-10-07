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
    private readonly AppState _appState;
    private readonly AutoSaveService _autoSaveService;
    private readonly BackupService _backupService;
    private readonly EditFlushService _editFlushService;
    private readonly ILogService _logger;
    private readonly OutlineService _outlineService;
    private readonly PreferenceService _preferences;
    private readonly StoryIO _storyIO;
    private readonly Windowing _windowing;
    private readonly ShellViewModel _shellVM;

    public FileOpenService(
        ILogService logger,
        OutlineService outlineService,
        AppState appState,
        EditFlushService editFlushService,
        PreferenceService preferences,
        Windowing windowing,
        BackupService backupService,
        AutoSaveService autoSaveService,
        StoryIO storyIO, ShellViewModel shellVM)
    {
        _logger = logger;
        _outlineService = outlineService;
        _appState = appState;
        _editFlushService = editFlushService;
        _preferences = preferences;
        _windowing = windowing;
        _backupService = backupService;
        _autoSaveService = autoSaveService;
        _storyIO = storyIO;
        _shellVM = shellVM;
    }

    /// <summary>
    ///     Opens a file picker to let the user chose a .stbx file and loads said file
    ///     If fromPath is specified then the picker is skipped.
    /// </summary>
    /// <param name="fromPath">Path to open file from (Optional)</param>
    public async Task OpenFile(string fromPath = "")
    {
        using (var serializationLock = new SerializationLock(_logger))
        {
            // Check if current StoryModel has been changed, if so, save and write the model.
            if (_appState.CurrentDocument?.Model?.Changed ?? false)
            {
                _editFlushService.FlushCurrentEdits();
                await _outlineService.WriteModel(_appState.CurrentDocument.Model, _appState.CurrentDocument.FilePath);
            }

            _logger.Log(LogLevel.Info, "Executing OpenFile command");
        }

        try
        {
            using (var serializationLock = new SerializationLock(_logger))
            {
                // Reset the model and show the home page
                // Note: These UI operations should be handled via messaging
                // Note: These UI operations should be handled via messaging
                // For now, we'll send status messages and rely on the caller to handle UI updates
                Default.Send(new StatusChangedMessage(new StatusMessage("Resetting model", LogLevel.Info)));

                // Open file picker if `fromPath` is not provided or file doesn't exist at the path.
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

                var filePath = fromPath;
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.Log(LogLevel.Warn, "Open File command failed: StoryModel.ProjectFile is null.");
                    Default.Send(
                        new StatusChangedMessage(new StatusMessage("Open Story command cancelled", LogLevel.Info)));
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
            _shellVM.TreeViewNodeClicked(_appState.CurrentDocument.Model.CurrentView[0]);
            _logger.Log(LogLevel.Info, $"Opened project {_appState.CurrentDocument?.FilePath}");
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
