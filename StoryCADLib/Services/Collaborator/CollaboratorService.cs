using Windows.ApplicationModel.AppExtensions;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Store;
using StoryCADLib.ViewModels.Store;

namespace StoryCADLib.Services.Collaborator;

public class CollaboratorService
{
    private readonly AppState _appState;
    private readonly AutoSaveService _autoSaveService;
    private readonly BackupService _backupService;
    private readonly ILogService _logService;
    private readonly PreferenceService _preferenceService;
    private readonly StoryCADApi _storyCADApi;

    // Factory supplied by the composition root (StoryCAD head) when CollaboratorLib
    // is compiled in. Null when Collaborator is absent (public/free build), which is
    // how StoryCAD runs Collaborator-free. A factory (not a single instance) lets us
    // create a fresh Collaborator per session and dispose it on close.
    private readonly Func<ICollaborator> _collaboratorFactory;
    private ICollaborator _collaboratorInterface; // Current session instance (null between sessions)
    public Window CollaboratorWindow; // The secondary window for Collaborator

    public CollaboratorService(AppState appState, ILogService logService, PreferenceService preferenceService,
        AutoSaveService autoSaveService, BackupService backupService, StoryCADApi storyCADApi,
        Func<ICollaborator> collaboratorFactory = null)
    {
        _appState = appState;
        _logService = logService;
        _preferenceService = preferenceService;
        _autoSaveService = autoSaveService;
        _backupService = backupService;
        _storyCADApi = storyCADApi;
        _collaboratorFactory = collaboratorFactory;
    }

    #region Collaborator calls

    /// <summary>
    ///     Gets whether a collaborator is available (i.e. CollaboratorLib was compiled in
    ///     and a factory was registered at the composition root).
    /// </summary>
    public bool HasCollaborator => _collaboratorFactory != null;


    /// <summary>
    ///     Opens the Collaborator window with the current story context.
    ///     Collaborator handles all workflow operations internally.
    /// </summary>
    public async void OpenCollaborator()
    {
        if (_collaboratorFactory == null)
        {
            _logService.Log(LogLevel.Warn, "Collaborator not available - no Collaborator implementation registered");
            return;
        }

        // Get the current StoryModel from AppState
        var storyModel = _appState.CurrentDocument?.Model;
        if (storyModel == null)
        {
            _logService.Log(LogLevel.Error, "No StoryModel available - no document is open");
            return;
        }

        bool devEnabled = Environment.GetEnvironmentVariable("COLLAB_DEV_ENABLED") == "1";
        if (!devEnabled && !IsPurchaseVerified())
        {
            _logService.Log(LogLevel.Warn, "Collaborator blocked: purchase not verified and COLLAB_DEV_ENABLED != 1");
            // Offer the subscription. If the user completes it (now Active), fall through and open
            // Collaborator; otherwise stop here.
            if (!await ShowSubscribeDialogAsync())
            {
                return;
            }
        }

        _storyCADApi.SetCurrentModel(storyModel);

        // Pause background services while Collaborator holds the model
        _backupService.StopTimedBackup();
        _autoSaveService.StopAutoSave();

        try
        {
            // Host supplies the root frame for navigation
            var hostRoot = new StoryCADLib.Collaborator.Views.CollaboratorHostRoot();
            var hostFrame = hostRoot.RootFrameControl;
            if (hostFrame == null)
            {
                _logService.Log(LogLevel.Error, "Collaborator host frame not found");
                return;
            }

            // Create the window on the host side
            CollaboratorWindow = new Window { Content = hostRoot, Title = "Story Collaborator" };
            CollaboratorWindow.Activate();

            // Create a fresh Collaborator instance for this session (disposed on close).
            _collaboratorInterface = _collaboratorFactory();

            var filePath = _appState.CurrentDocument?.FilePath ?? string.Empty;
            await _collaboratorInterface.OpenAsync(_storyCADApi, storyModel, CollaboratorWindow, hostFrame, filePath, _logService);
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Error, $"Failed to open Collaborator: {ex.Message}");
            return;
        }

        if (CollaboratorWindow != null)
        {
            CollaboratorWindow.Closed += (sender, args) => CollaboratorClosed();
            // Window is already activated in WindowManager.CreateWindowAsync
            _logService.Log(LogLevel.Info, "Collaborator window opened");
        }
        else
        {
            _logService.Log(LogLevel.Error, "Failed to create Collaborator window");
        }
    }

    // Gate (issue #30): Collaborator opens only when the store-activation service holds a valid
    // Worker JWT. This is a client-side, open-time check; attaching CurrentJwt to each Collaborator
    // Worker call (the per-call enforcement in devdocs/iap/activation-contract.md, "JWT") is the
    // remaining #30 wiring in the CollaboratorLib/Worker track. Resolved on demand to avoid
    // widening the constructor.
    private static bool IsPurchaseVerified() =>
        Ioc.Default.GetService<IStoreActivationService>()?.State == ActivationState.Active;

    /// <summary>
    ///     Offers the subscription via <see cref="SubscribeDialogViewModel.ShowAsync" /> (the view
    ///     model owns the dialog). Returns true when the user is Active afterward. Resolved on
    ///     demand to avoid widening the constructor.
    /// </summary>
    private async Task<bool> ShowSubscribeDialogAsync()
    {
        var windowing = Ioc.Default.GetService<Windowing>();
        var vm = Ioc.Default.GetService<SubscribeDialogViewModel>();
        if (windowing is null || vm is null)
        {
            _logService.Log(LogLevel.Warn, "Subscribe dialog unavailable; no Windowing or view model registered.");
            return false;
        }

        return await vm.ShowAsync(windowing);
    }

    #endregion

    #region Show/Hide window

    /// <summary>
    ///     This is called when collaborator has finished doing stuff.
    ///     Note: This is invoked from the Collaborator side via a Delegate named onDoneCallback
    /// </summary>
    private void FinishedCallback()
    {
        _logService.Log(LogLevel.Info, "Collaborator Callback, re-enabling async");
        //Reenable Timed Backup if needed.
        if (_preferenceService.Model.TimedBackup)
        {
            _backupService.StartTimedBackup();
        }

        //Reenable auto save if needed.
        if (_preferenceService.Model.AutoSave)
        {
            _autoSaveService.StartAutoSave();
        }

        //Reenable StoryCAD buttons
        //Ioc.Default.GetRequiredService<OutlineViewModel>()._canExecuteCommands = true;
        _logService.Log(LogLevel.Info, "Async re-enabled.");
    }

    public void DestroyCollaborator()
    {
        _logService.Log(LogLevel.Warn, "Destroying collaborator object.");

        try
        {
            _collaboratorInterface?.Close();
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Error closing collaborator during destroy: {ex.Message}");
        }

        try
        {
            _collaboratorInterface?.Dispose();
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Error disposing collaborator during destroy: {ex.Message}");
        }

        if (CollaboratorWindow != null)
        {
            CollaboratorWindow.Close(); // Destroy window object
            CollaboratorWindow = null;
            _logService.Log(LogLevel.Info, "Closed collaborator window");
        }

        _collaboratorInterface = null;
        _logService.Log(LogLevel.Info, "Nulled collaborator objects");
    }

    public void CollaboratorClosed()
    {
        _logService.Log(LogLevel.Debug, "Closing Collaborator.");
        try
        {
            var result = _collaboratorInterface?.Close();
            if (result != null)
            {
                _logService.Log(LogLevel.Info, $"Collaborator summary: {result.Summary}");
                if (result.Messages?.Count > 0)
                {
                    foreach (var message in result.Messages)
                    {
                        _logService.Log(LogLevel.Info, message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Error, $"Collaborator Close failed: {ex.Message}");
        }

        _collaboratorInterface = null;
        CollaboratorWindow = null;

        // Reload current ViewModel from Model to pick up Collaborator's changes
        ReloadCurrentViewModel();

        FinishedCallback();
    }

    /// <summary>
    ///     Reloads the current ViewModel from the Model after Collaborator updates.
    ///     This ensures the UI reflects changes made by Collaborator.
    /// </summary>
    private void ReloadCurrentViewModel()
    {
        try
        {
            if (_appState.CurrentSaveable is IReloadable reloadable)
            {
                _logService.Log(LogLevel.Info, "Reloading current ViewModel from Model after Collaborator close");
                reloadable.ReloadFromModel();
            }
            else
            {
                _logService.Log(LogLevel.Debug, "No reloadable ViewModel active - skipping reload");
            }
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Error, $"Error reloading ViewModel after Collaborator close: {ex.Message}");
        }
    }

    #endregion
}
