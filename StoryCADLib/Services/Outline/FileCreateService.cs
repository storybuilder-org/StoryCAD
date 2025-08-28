using System.IO;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Locking;
using StoryCAD.Services.Messages;
using StoryCAD.ViewModels;
using Windows.Storage;

using static CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger;

namespace StoryCAD.Services.Outline;

/// <summary>
/// Service for creating new story files without dependencies on ViewModels 
/// </summary>
public class FileCreateService
{
    private readonly ILogService _logger;
    private readonly OutlineService _outlineService;
    private readonly AppState _appState;
    private readonly PreferenceService _preferences;
    private readonly Windowing _windowing;
    private readonly BackupService _backupService;
    private readonly AutoSaveService _autoSaveService;

    public FileCreateService(
        ILogService logger,
        OutlineService outlineService,
        AppState appState,
        PreferenceService preferences,
        Windowing windowing,
        BackupService backupService,
        AutoSaveService autoSaveService)
    {
        _logger = logger;
        _outlineService = outlineService;
        _appState = appState;
        _preferences = preferences;
        _windowing = windowing;
        _backupService = backupService;
        _autoSaveService = autoSaveService;
    }

    /// <summary>
    /// Creates a new story file
    /// </summary>
    /// <param name="outlineFolder">Folder to create the file in</param>
    /// <param name="outlineName">Name of the outline file</param>
    /// <param name="selectedTemplateIndex">Template index to use for creation</param>
    public async Task<string> CreateFile(string outlineFolder, string outlineName, int selectedTemplateIndex)
    {
        _logger.Log(LogLevel.Info, "FileCreateService - Creating new file");

        try
        {
            Default.Send(new StatusChangedMessage(new StatusMessage("New project command executing", LogLevel.Info)), true);

            // Validate and adjust file name
            if (!Path.GetExtension(outlineName)!.Equals(".stbx"))
            {
                outlineName += ".stbx";
            }

            string newFilePath = Path.Combine(outlineFolder, outlineName);

            if (!StoryIO.IsValidPath(newFilePath))
            {
                _logger.Log(LogLevel.Warn, $"Invalid file path {newFilePath}");
                Default.Send(new StatusChangedMessage(new("Invalid file path", LogLevel.Error)), true);
                return null;
            }

            using (var serializationLock = new SerializationLock(_logger))
            {
                // If the current project needs saved, do so
                if (_appState.CurrentDocument?.Model?.Changed == true && _appState.CurrentDocument?.FilePath != null)
                {
                    await _outlineService.WriteModel(_appState.CurrentDocument.Model, _appState.CurrentDocument.FilePath);
                }
            }

            // Note: UI operations should be handled by the caller
            // We'll send a status message to indicate progress
            Default.Send(new StatusChangedMessage(new("Creating new project", LogLevel.Info)));

            using (var serializationLock = new SerializationLock(_logger))
            {
                // Create the new outline's file
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(outlineFolder);
                string storyModelFile =
                    (await folder.CreateFileAsync(outlineName, CreationCollisionOption.GenerateUniqueName))
                    .Path;

                // Create the StoryModel
                string name = Path.GetFileNameWithoutExtension(storyModelFile);
                string author = _preferences.Model.FirstName + " " + _preferences.Model.LastName;

                // Create the new project
                var newModel = await _outlineService.CreateModel(name, author, selectedTemplateIndex);
                _appState.CurrentDocument = new StoryDocument(newModel, storyModelFile);

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
                Default.Send(new StatusChangedMessage(new("New project created", LogLevel.Info)));

                return storyModelFile;
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, "Error in CreateFile command");
            Default.Send(new StatusChangedMessage(new("New project command failed", LogLevel.Error)));
            return null;
        }
    }
}