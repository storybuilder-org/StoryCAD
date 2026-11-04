using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.DAL;
using StoryCADLib.Services.Backend;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Locking;
using StoryCADLib.Services.Messages;
using static CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger;

namespace StoryCADLib.Services.Outline;

/// <summary>
///     Service for creating new story files without dependencies on ViewModels
/// </summary>
public class FileCreateService
{
    private readonly AppState _appState;
    private readonly AutoSaveService _autoSaveService;
    private readonly BackupService _backupService;
    private readonly ILogService _logger;
    private readonly OutlineService _outlineService;
    private readonly PreferenceService _preferences;
    private readonly IUsageTrackingService _usageTracking;
    private readonly Windowing _windowing;

    public FileCreateService(
        ILogService logger,
        OutlineService outlineService,
        AppState appState,
        PreferenceService preferences,
        Windowing windowing,
        BackupService backupService,
        AutoSaveService autoSaveService,
        IUsageTrackingService usageTracking)
    {
        _logger = logger;
        _outlineService = outlineService;
        _appState = appState;
        _preferences = preferences;
        _windowing = windowing;
        _backupService = backupService;
        _autoSaveService = autoSaveService;
        _usageTracking = usageTracking;
    }

    /// <summary>
    ///     Creates a new story file.
    /// </summary>
    /// <param name="outlineFolder">Folder to create the file in</param>
    /// <param name="outlineName">Name of the outline file</param>
    /// <param name="selectedTemplateIndex">Legacy template index. Prefer the <see cref="OutlineTemplate"/> overload.</param>
    public Task<string> CreateFile(string outlineFolder, string outlineName, int selectedTemplateIndex)
        => CreateFile(outlineFolder, outlineName, (OutlineTemplate)selectedTemplateIndex);

    /// <summary>
    ///     Creates a new story file.
    /// </summary>
    /// <param name="outlineFolder">Folder to create the file in</param>
    /// <param name="outlineName">Name of the outline file</param>
    /// <param name="template">The template to build the outline from.</param>
    public async Task<string> CreateFile(string outlineFolder, string outlineName, OutlineTemplate template)
    {
        _logger.Log(LogLevel.Info, "FileCreateService - Creating new file");

        try
        {
            // No messenger token on status sends: ShellViewModel registers without one and drops tokened sends (#1682).
            Default.Send(new StatusChangedMessage(new StatusMessage("New project command executing", LogLevel.Info)));

            // A blank name became a file named ".stbx" (#1682).
            if (IsBlankOutlineName(outlineName))
            {
                _logger.Log(LogLevel.Warn, "New outline name is blank");
                Default.Send(new StatusChangedMessage(new StatusMessage("Enter a name for the new outline",
                    LogLevel.Warn)));
                return null;
            }

            // Validate and adjust file name
            if (!Path.GetExtension(outlineName)!.Equals(".stbx"))
            {
                outlineName += ".stbx";
            }

            var newFilePath = Path.Combine(outlineFolder, outlineName);

            if (!StoryIO.IsValidPath(newFilePath))
            {
                _logger.Log(LogLevel.Warn, $"Invalid file path {newFilePath}");
                Default.Send(new StatusChangedMessage(new StatusMessage("Invalid file path", LogLevel.Warn)));
                return null;
            }

            using (var serializationLock = new SerializationLock(_logger))
            {
                // If the current project needs saved, do so
                if (_appState.CurrentDocument?.Model?.Changed == true && _appState.CurrentDocument?.FilePath != null)
                {
                    await _outlineService.WriteModel(_appState.CurrentDocument.Model,
                        _appState.CurrentDocument.FilePath);
                }
            }

            // Note: UI operations should be handled by the caller
            // We'll send a status message to indicate progress
            Default.Send(new StatusChangedMessage(new StatusMessage("Creating new project", LogLevel.Info)));

            using (var serializationLock = new SerializationLock(_logger))
            {
                // Pick the new outline's path. System.IO, not StorageFolder: the folder box is free
                // text, and StorageFolder rejects paths .NET accepts, such as '/' separators (#1615).
                var storyModelFile = UniqueFilePath(Path.GetFullPath(outlineFolder), outlineName);

                // Create the StoryModel
                var name = Path.GetFileNameWithoutExtension(storyModelFile);
                var author = _preferences.Model.FirstName + " " + _preferences.Model.LastName;

                // Create the new project
                var newModel = await _outlineService.CreateModel(name, author, template);

                // CreateNew claims the name, so a file that appeared after UniqueFilePath is not overwritten.
                await using (new FileStream(storyModelFile, FileMode.CreateNew)) { }

                // Write the outline now: a new model is unchanged, so no save, prompt or auto-save
                // runs until an edit, and an empty file left on disk could not be opened (#1682).
                try
                {
                    await _outlineService.WriteModel(newModel, storyModelFile);
                }
                catch
                {
                    // The claimed file is still empty; do not leave it behind (#1682). A failed delete
                    // is only logged, so the write's exception is the one reported.
                    try
                    {
                        File.Delete(storyModelFile);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.Log(LogLevel.Warn, $"Could not delete empty new outline {storyModelFile}: {deleteEx.Message}");
                    }

                    throw;
                }
                _appState.CurrentDocument = new StoryDocument(newModel, storyModelFile);

                // Track outline open for usage statistics
                if (_appState.CurrentDocument?.Model != null)
                {
                    var overview = _appState.CurrentDocument.Model.StoryElements
                        .OfType<Models.OverviewModel>().FirstOrDefault();
                    if (overview != null)
                    {
                        _usageTracking.OutlineOpened(
                            overview.Uuid,
                            overview.StoryGenre,
                            overview.StoryType,
                            _appState.CurrentDocument.Model.StoryElements.Count);
                    }
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
                }

                // Start auto-save if configured
                if (_preferences.Model.AutoSave)
                {
                    _autoSaveService.StartAutoSave();
                }

                // Update window title
                _windowing.UpdateWindowTitle();

                _logger.Log(LogLevel.Info, $"Created new project {storyModelFile}");
                Default.Send(new StatusChangedMessage(new StatusMessage("New project created", LogLevel.Info)));

                return storyModelFile;
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, "Error in CreateFile command");
            Default.Send(new StatusChangedMessage(new StatusMessage("New project command failed", LogLevel.Error)));
            return null;
        }
    }

    /// <summary>
    ///     True when the outline name is blank once a trailing ".stbx" is removed. Only that
    ///     extension counts: ".hack" and ".45 Magnum" are titles, not blank names (#1682).
    /// </summary>
    public static bool IsBlankOutlineName(string outlineName)
    {
        var name = outlineName ?? "";
        if (name.EndsWith(".stbx", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^".stbx".Length];
        }

        return string.IsNullOrWhiteSpace(name);
    }

    /// <summary>
    ///     Returns a path in folder for fileName that no file uses yet, adding " (2)", " (3)" and so on
    ///     the same way CreationCollisionOption.GenerateUniqueName did.
    /// </summary>
    private static string UniqueFilePath(string folder, string fileName)
    {
        var path = Path.Combine(folder, fileName);
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var n = 2; File.Exists(path); n++)
        {
            path = Path.Combine(folder, $"{stem} ({n}){extension}");
        }

        return path;
    }
}
