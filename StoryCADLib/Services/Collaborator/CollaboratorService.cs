using System.Reflection;
using System.Runtime.Loader;
using Windows.ApplicationModel.AppExtensions;
using Microsoft.UI.Windowing;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Outline;

namespace StoryCADLib.Services.Collaborator;

public class CollaboratorService
{
    private const string PluginFileName = "CollaboratorLib.dll";
    private const string EnvPluginDirVar = "STORYCAD_PLUGIN_DIR";
    private readonly AppState _appState;
    private readonly AutoSaveService _autoSaveService;
    private readonly BackupService _backupService;
    private readonly ILogService _logService;
    private readonly PreferenceService _preferenceService;
    private ICollaborator _collaboratorInterface; // Interface-based reference
    private Assembly CollabAssembly;
#pragma warning disable CS0169 // Field is used in platform-specific code
    private object collaborator;
    private Type collaboratorType;
#pragma warning restore CS0169
    public Window CollaboratorWindow; // The secondary window for Collaborator
#pragma warning disable CS0169 // Fields used in platform-specific code (!HAS_UNO only - see Issue #1126 for macOS support)
    private bool dllExists;     // Used in Windows-only FindDll() method
#pragma warning restore CS0169
#pragma warning disable CS0649 // Field will be assigned in future macOS implementation (Issue #1126)
    private string dllPath;     // Used in ConnectCollaborator() - will be needed for macOS (Issue #1126)
#pragma warning restore CS0649

    public CollaboratorService(AppState appState, ILogService logService, PreferenceService preferenceService,
        AutoSaveService autoSaveService, BackupService backupService)
    {
        _appState = appState;
        _logService = logService;
        _preferenceService = preferenceService;
        _autoSaveService = autoSaveService;
        _backupService = backupService;
    }

    #region Collaborator calls

    /// <summary>
    ///     Gets whether a collaborator is available
    /// </summary>
    public bool HasCollaborator => _collaboratorInterface != null;

    /// <summary>
    ///     Sets the collaborator instance (for testing or direct injection)
    /// </summary>
    public void SetCollaborator(ICollaborator collaboratorInstance)
    {
        _collaboratorInterface = collaboratorInstance;
    }

    public void LoadWorkflows(CollaboratorArgs args)
    {
        // Load the workflow navigation menu for user selection
        if (_collaboratorInterface != null)
        {
            _collaboratorInterface.LoadWizardViewModel();
        }
        else
        {
            _logService.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    /// <summary>
    ///     Load the workflow view model for a specific element type
    /// </summary>
    public void LoadWorkflowViewModel(StoryItemType elementType)
    {
        if (_collaboratorInterface != null)
        {
            _collaboratorInterface.LoadWorkflowViewModel(elementType);
        }
        else
        {
            _logService.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    /// <summary>
    ///     Load the WizardViewModel (WizardShell's VM) with the high level
    ///     NavigationView menu.
    ///     This is a proxy for Collaborator's LoadWizardViewModel.
    /// </summary>
    public void LoadWizardViewModel()
    {
        if (_collaboratorInterface != null)
        {
            _collaboratorInterface.LoadWizardViewModel();
        }
        else
        {
            _logService.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    /// <summary>
    ///     Load the WorkflowViewModel with the currently selected
    ///     Workflow Model.
    ///     This is a proxy for Collaborator's LoadWorkflowModel method.
    /// </summary>
    public void LoadWorkflowModel(StoryElement element, string workflow)
    {
        if (_collaboratorInterface != null)
        {
            _collaboratorInterface.LoadWorkflowModel(element, workflow);
        }
        else
        {
            _logService.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    /// <summary>
    ///     Process the Workflow we've loaded.
    ///     This is a proxy for Collaborator's ProcessWorkflow method.
    /// </summary>
    public void ProcessWorkflow()
    {
        if (_collaboratorInterface != null)
        {
            // Call async version synchronously for backward compatibility
            Task.Run(async () => await ProcessWorkflowAsync()).Wait();
        }
        else
        {
            _logService.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    /// <summary>
    ///     Process the Workflow we've loaded asynchronously.
    /// </summary>
    public async Task ProcessWorkflowAsync()
    {
        if (_collaboratorInterface != null)
        {
            await _collaboratorInterface.ProcessWorkflowAsync();
        }
        else
        {
            _logService.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    public void SendButtonClicked()
    {
        if (_collaboratorInterface != null)
        {
            // Call async version synchronously for backward compatibility
            Task.Run(async () => await SendButtonClickedAsync()).Wait();
        }
        else
        {
            _logService.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    /// <summary>
    ///     Handle send button click asynchronously.
    /// </summary>
    public async Task SendButtonClickedAsync()
    {
        if (_collaboratorInterface != null)
        {
            await _collaboratorInterface.SendButtonClickedAsync();
        }
        else
        {
            _logService.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    /// <summary>
    ///     Save any unchanged OutputProperty values to their StoryElement.
    ///     This is a proxy for Collaborator's SaveOutputs() method.
    /// </summary>
    public void SaveOutputs()
    {
        if (_collaboratorInterface != null)
        {
            _collaboratorInterface.SaveOutputs();
        }
        else
        {
            _logService.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    #endregion

    #region Collaboratorlib connection

    /// <summary>
    ///     If the plugin is active, connect to CollaboratorLib and create an instance
    ///     of Collaborator.
    /// </summary>
    public void ConnectCollaborator()
    {
        // Use the custom context to load the assembly
        CollabAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
        _logService.Log(LogLevel.Info, "Loaded CollaboratorLib.dll");

        // Get the type of the Collaborator class
        var collaboratorType = CollabAssembly.GetType("StoryCollaborator.Collaborator");
        if (collaboratorType == null)
        {
            _logService.Log(LogLevel.Error, "Could not find Collaborator type in assembly");
            return;
        }

        // Create an instance of the Collaborator class
        _logService.Log(LogLevel.Info, "Calling Collaborator constructor.");

        // Get the current StoryModel from AppState
        var storyModel = _appState.CurrentDocument?.Model;
        if (storyModel == null)
        {
            _logService.Log(LogLevel.Error, "No StoryModel available - no document is open");
            return;
        }

        // Create and populate CollaboratorArgs
        var collabArgs = Ioc.Default.GetService<ShellViewModel>()!.CollabArgs = new CollaboratorArgs();
        collabArgs.StoryModel = storyModel;
        collabArgs.WorkflowVm = null; // Collaborator creates its own WorkflowViewModel
        collabArgs.CollaboratorWindow = CollaboratorWindow;

        // Create the API instance for Collaborator to use
        var outlineService = Ioc.Default.GetService<OutlineService>();
        if (outlineService != null)
        {
            var api = new SemanticKernelApi(outlineService);
            api.SetCurrentModel(storyModel);
            collabArgs.StoryApi = api;
        }

        object[] methodArgs = { collabArgs };

        // Find the constructor that takes CollaboratorArgs parameter
        var constructor = collaboratorType.GetConstructor(new[] { typeof(CollaboratorArgs) });
        if (constructor == null)
        {
            _logService.Log(LogLevel.Error, "Could not find Collaborator constructor that takes CollaboratorArgs");
            throw new InvalidOperationException("Collaborator must have a constructor that takes CollaboratorArgs");
        }

        var collaborator = constructor.Invoke(methodArgs);
        _logService.Log(LogLevel.Info, "Collaborator Constructor finished.");

        // Cast to interface - this is now required
        _collaboratorInterface = collaborator as ICollaborator;
        if (_collaboratorInterface != null)
        {
            _logService.Log(LogLevel.Info, "Collaborator successfully loaded with ICollaborator interface.");

            // Create the collaborator window using the ICollaborator interface
            // Pass the API as context
            CollaboratorWindow = _collaboratorInterface.CreateWindow(collabArgs.StoryApi);
            if (CollaboratorWindow != null)
            {
                CollaboratorWindow.AppWindow.Closing += HideCollaborator;
                CollaboratorWindow.Closed += (sender, args) => CollaboratorClosed();
                _logService.Log(LogLevel.Info, "Collaborator window created and configured.");
            }
            else
            {
                _logService.Log(LogLevel.Error, "Failed to create Collaborator window.");
            }
        }
        else
        {
            _logService.Log(LogLevel.Error, "Collaborator does not implement ICollaborator interface. Cannot proceed.");
            throw new InvalidOperationException("CollaboratorLib must implement ICollaborator interface");
        }
    }

    public async Task<bool> CollaboratorEnabled()
    {
        if (!OperatingSystem.IsWindows())
        {
            _logService.Log(LogLevel.Warn, "Collaborator interface not supported on this platform");
            return false;
        }
        
        // Check COLLAB_DEBUG environment variable to bypass collaborator loading
        var collabDebug = Environment.GetEnvironmentVariable("COLLAB_DEBUG");
        if (collabDebug == "0")
        {
            _logService.Log(LogLevel.Info, "Collaborator disabled by COLLAB_DEBUG=0");
            return false;
        }

        // Allow loading if:
        // 1. Developer build AND plugin found
        // 2. OR if STORYCAD_PLUGIN_DIR is set (for JIT debugging without F5)
        var hasPluginDir = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvPluginDirVar));

        return (_appState.DeveloperBuild || hasPluginDir)
               && await FindDll();
    }

    /// <summary>
    ///     Checks if CollaboratorLib.dll exists.
    /// </summary>
    /// <returns>True if CollaboratorLib.dll exists, false otherwise.</returns>
    private async Task<bool> FindDll()
    {
#if !HAS_UNO
        _logService.Log(LogLevel.Info, "Locating Collaborator Package...");

        //Find all installed extensions
        var _catalog = AppExtensionCatalog.Open("org.storybuilder");
        var InstalledExtensions = await _catalog.FindAllAsync();
        _logService.Log(LogLevel.Info, $"Found {InstalledExtensions} installed extensions");
        _logService.Log(LogLevel.Info, "Locating CollaboratorLib...");

        // 1) DEV: explicit override — deterministic
        if (TryResolveFromEnv(out var envPath))
        {
            dllPath = envPath;
            dllExists = true;
            _logService.Log(LogLevel.Info, $"Found via ${EnvPluginDirVar}: {dllPath}");
            return true;
        }

        // 2) DEV: sibling repo (safeguarded scan)
        if (_appState.DeveloperBuild)
        {
            if (TryResolveFromSibling(out var devPath))
            {
                dllPath = devPath;
                dllExists = true;
                _logService.Log(LogLevel.Info, $"Found dev DLL: {dllPath}");
                return true;
            }

            _logService.Log(LogLevel.Warn, "Dev paths not found, falling back to package lookup.");
        }

        // 3) PROD: MSIX AppExtension
        var (ok, pkgPath) = await TryResolveFromExtensionAsync();
        if (ok)
        {
            dllPath = pkgPath;
            dllExists = File.Exists(dllPath);
            _logService.Log(LogLevel.Info, $"Found package DLL: {dllPath}, exists={dllExists}");
            return dllExists;
        }

        _logService.Log(LogLevel.Info, "Failed to resolve CollaboratorLib.dll");
        dllPath = null;
        dllExists = false;
        return false;
#else
        // macOS plugin loading tracked in Issue #1126 and #1135
        await Task.CompletedTask;  // Placeholder until macOS implementation
        _logService.Log(LogLevel.Error, "Collaborator is not supported on this platform.");
        return false;
#endif
    }

    private bool TryResolveFromEnv(out string path)
    {
        path = null;
        var dir = Environment.GetEnvironmentVariable(EnvPluginDirVar);
        if (string.IsNullOrWhiteSpace(dir))
        {
            return false;
        }

        var candidate = Path.Combine(dir, PluginFileName);
        if (!File.Exists(candidate))
        {
            _logService.Log(LogLevel.Warn, $"{EnvPluginDirVar} set but file missing: {candidate}");
            return false;
        }

        var pdb = Path.ChangeExtension(candidate, ".pdb");
        if (!File.Exists(pdb))
        {
            _logService.Log(LogLevel.Warn, $"PDB missing next to DLL: {pdb}");
        }

        path = candidate;
        return true;
    }

    // Looks for: <reposRoot>\StoryBuilderCollaborator\CollaboratorLib\bin\x64\Debug\<net8.0-windows*>\CollaboratorLib.dll
    private bool TryResolveFromSibling(out string path)
    {
        path = null;

        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        var cursor = exeDir;
        for (var i = 0; i < 10 && cursor != null; i++)
        {
            cursor = Path.GetDirectoryName(cursor);
        }

        if (cursor == null)
        {
            return false;
        }

        var baseDebug = Path.Combine(cursor,
            "StoryBuilderCollaborator", "CollaboratorLib", "bin", "x64", "Debug");
        if (!Directory.Exists(baseDebug))
        {
            return false;
        }

        var tfmCandidates = Directory.EnumerateDirectories(baseDebug, "net*-windows*")
            .OrderByDescending(d => Directory.GetLastWriteTimeUtc(d))
            .ToList();
        foreach (var tfmDir in tfmCandidates)
        {
            var candidate = Path.Combine(tfmDir, PluginFileName);
            if (File.Exists(candidate))
            {
                var pdb = Path.ChangeExtension(candidate, ".pdb");
                if (!File.Exists(pdb))
                {
                    _logService.Log(LogLevel.Warn, $"PDB missing next to DLL: {pdb}");
                }

                path = candidate;
                return true;
            }
        }

        return false;
    }

    private async Task<(bool ok, string dllPath)> TryResolveFromExtensionAsync()
    {
        try
        {
            #if !WINDOWS10_0_18362_0_OR_GREATER
            _logService.Log(LogLevel.Warn, "Collaborator is only supported on Windows.");
            return (false, null);
            #else
            var catalog = AppExtensionCatalog.Open("org.storybuilder");
            var exts = await catalog.FindAllAsync();

            _logService.Log(LogLevel.Info, $"Found {exts.Count} installed extensions for org.storybuilder");

            var collab = exts.FirstOrDefault(e =>
                string.Equals(e.Package.Id.Name, "StoryCADCollaborator", StringComparison.OrdinalIgnoreCase) ||
                (e.Package.DisplayName?.Contains("StoryCAD Collaborator", StringComparison.OrdinalIgnoreCase) ??
                 false));

            if (collab == null)
            {
                _logService.Log(LogLevel.Info, "Collaborator extension not installed.");
                return (false, null);
            }

            var pkg = collab.Package;
            _logService.Log(LogLevel.Info,
                $"Found Collaborator Package: {pkg.DisplayName} {pkg.Id.Version.Major}.{pkg.Id.Version.Minor}.{pkg.Id.Version.Build}");

            if (!await pkg.VerifyContentIntegrityAsync())
            {
                _logService.Log(LogLevel.Error, "VerifyContentIntegrityAsync failed; refusing to load.");
                return (false, null);
            }

            var installDir = pkg.InstalledLocation.Path; // StorageFolder → string
            var dll = Path.Combine(installDir, PluginFileName);
            return (true, dll);
            #endif
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Error, $"Extension lookup failed: {ex}");
            return (false, null);
        }
    }

    #endregion

    #region Show/Hide window

    /// <summary>
    ///     This will hide the collaborator window.
    /// </summary>
    private void HideCollaborator(AppWindow appWindow, AppWindowClosingEventArgs e)
    {
        _logService.Log(LogLevel.Debug, "Hiding collaborator window.");
        e.Cancel = true; // Cancel stops the window from being disposed.
        appWindow.Hide(); // Hide the window instead.
        _logService.Log(LogLevel.Debug, "Successfully hid collaborator window.");

        //Call collaborator callback since we need to reenable async to prevent a locked state.
        FinishedCallback();
    }

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
        if (CollaboratorWindow != null)
        {
            CollaboratorWindow.Close(); // Destroy window object
            _logService.Log(LogLevel.Info, "Closed collaborator window");

            //Null objects to deallocate them
            CollabAssembly = null;
            _logService.Log(LogLevel.Info, "Nulled collaborator objects");

            //Run garbage collection to clean up any remnants.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            _logService.Log(LogLevel.Info, "Garbage collection finished.");
        }
    }

    public void CollaboratorClosed()
    {
        _logService.Log(LogLevel.Debug, "Closing Collaborator.");
    }

    #endregion
}
