using Windows.ApplicationModel.AppExtensions;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Messages;
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

        // Issue #90 ruling of 2026-07-15: the COLLAB_DEV_ENABLED purchase-check bypass retired.
        // Entitlement is holding a valid activation, however obtained -- including via the
        // dev/tester allowlist, which COLLAB_DEV_ENABLED now only routes StoreActivationService
        // toward (item 3), rather than skipping this check.
        if (!IsPurchaseVerified())
        {
            _logService.Log(LogLevel.Warn, "Collaborator blocked: no active store activation.");
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
    // Worker call (the per-call enforcement in the activation contract, "JWT"; see
    // StoryCADWiki: wiki/repos/Collaborator/sources/iap-billing-docs.md) is the
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
        // No platform store (NullStoreService, e.g. any desktop build outside a store bundle) means
        // no plans to list and a Subscribe button that can never succeed. Don't show the dialog;
        // tell the user Collaborator needs the store edition. Resolved on demand to match above.
        var store = Ioc.Default.GetService<IStoreService>();
        if (store is null || !store.IsSupported)
        {
            // Called on the UI thread from OpenCollaboratorAsync, so send the status directly.
            _logService.Log(LogLevel.Warn, "Subscribe dialog suppressed; no platform store is available in this build.");
            WeakReferenceMessenger.Default.Send(new StatusChangedMessage(new StatusMessage(
                "Collaborator requires the Microsoft Store or Mac App Store edition of StoryCAD.",
                LogLevel.Warn, true)));
            return false;
        }

        var windowing = Ioc.Default.GetService<Windowing>();
        var vm = Ioc.Default.GetService<SubscribeDialogViewModel>();
        if (windowing is null || vm is null)
        {
            _logService.Log(LogLevel.Warn, "Subscribe dialog unavailable; no Windowing or view model registered.");
            return false;
        }

        return await vm.ShowAsync(windowing);
    }

    /// <summary>
    ///     Offers a credit-pack purchase via <see cref="BuyCreditsDialogViewModel.ShowAsync" />
    ///     (issue #90 design section 10 "Credit packs", step 10). Returns true when a pack was
    ///     purchased and credited. Public (unlike <see cref="ShowSubscribeDialogAsync" />, which
    ///     has no caller outside <see cref="OpenCollaborator" />'s own gate): the out-of-credits
    ///     message the workflow and chat callers show (StoryCADLib.Services.Store.
    ///     OutOfCreditsException) names this screen, so a menu item or in-panel button can call it
    ///     directly once one is wired up -- no further plumbing needed on this side.
    /// </summary>
    public async Task<bool> ShowBuyCreditsDialogAsync()
    {
        var store = Ioc.Default.GetService<IStoreService>();
        if (store is null || !store.IsSupported)
        {
            _logService.Log(LogLevel.Warn, "Buy Credits dialog suppressed; no platform store is available in this build.");
            WeakReferenceMessenger.Default.Send(new StatusChangedMessage(new StatusMessage(
                "Buying credits requires the Microsoft Store or Mac App Store edition of StoryCAD.",
                LogLevel.Warn, true)));
            return false;
        }

        var windowing = Ioc.Default.GetService<Windowing>();
        var vm = Ioc.Default.GetService<BuyCreditsDialogViewModel>();
        if (windowing is null || vm is null)
        {
            _logService.Log(LogLevel.Warn, "Buy Credits dialog unavailable; no Windowing or view model registered.");
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
