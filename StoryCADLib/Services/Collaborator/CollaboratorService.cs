using System.Reflection;
using System.Runtime.Loader;
using Windows.ApplicationModel.AppExtensions;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage;
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
    private AssemblyLoadContext _pluginLoadContext; // Custom load context for plugin dependencies
#pragma warning disable CS0169 // Field is used in platform-specific code
    private object collaborator;
    private Type collaboratorType;
#pragma warning restore CS0169
    public Window CollaboratorWindow; // The secondary window for Collaborator
#pragma warning disable CS0169, CS0414 // CS0169: unused on macOS, CS0414: assigned but unused on Windows - tracked for Issue #1126
    private bool dllExists;     // Used in Windows-only FindDll() method
#pragma warning restore CS0169, CS0414
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
    ///     Opens the Collaborator window with the current story context.
    ///     Collaborator handles all workflow operations internally.
    /// </summary>
    public void OpenCollaborator()
    {
        if (_collaboratorInterface == null)
        {
            _logService.Log(LogLevel.Warn, "Collaborator plugin not available - plugin DLL not found or failed to load");
            return;
        }

        // Get the current StoryModel from AppState
        var storyModel = _appState.CurrentDocument?.Model;
        if (storyModel == null)
        {
            _logService.Log(LogLevel.Error, "No StoryModel available - no document is open");
            return;
        }

        // Create the API instance for Collaborator to use
        var outlineService = Ioc.Default.GetService<OutlineService>();
        if (outlineService != null)
        {
            var api = new SemanticKernelApi(outlineService);
            api.SetCurrentModel(storyModel);

            try
            {
                CollaboratorWindow = _collaboratorInterface.Open(api, storyModel);
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Error, $"Failed to open Collaborator: {ex.Message}");
                return;
            }

            if (CollaboratorWindow != null)
            {
                CollaboratorWindow.Closed += (sender, args) => CollaboratorClosed();
                CollaboratorWindow.Activate();
                _logService.Log(LogLevel.Info, "Collaborator window opened");
            }
            else
            {
                _logService.Log(LogLevel.Error, "Failed to create Collaborator window");
            }
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
        // Use custom load context to resolve plugin dependencies
        _pluginLoadContext = new PluginLoadContext(dllPath);
        CollabAssembly = _pluginLoadContext.LoadFromAssemblyPath(dllPath);
        _logService.Log(LogLevel.Info, "Loaded CollaboratorLib.dll with custom load context");

        //debugging
        Type[] collaboratorTypes;
        try
        {
            collaboratorTypes = CollabAssembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Log each loader exception
            foreach (var loaderEx in ex.LoaderExceptions)
            {
                if (loaderEx != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Loader Exception: {loaderEx.Message}");
                }
            }
            throw;
        }

        // Get the type of the Collaborator class
        var collaboratorType = CollabAssembly.GetType("StoryCollaborator.Collaborator");
        if (collaboratorType == null)
        {   
            _logService.Log(LogLevel.Error, "Could not find Collaborator type in assembly");
            return;
        }

        // Create an instance of the Collaborator class using parameterless constructor
        _logService.Log(LogLevel.Info, "Creating Collaborator instance.");

        // Find the parameterless constructor
        var constructor = collaboratorType.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
        {
            _logService.Log(LogLevel.Error, "Could not find parameterless constructor for Collaborator");
            throw new InvalidOperationException("Collaborator must have a parameterless constructor");
        }

        var collaborator = constructor.Invoke(null);
        _logService.Log(LogLevel.Info, "Collaborator instance created.");

        // Cast to interface
        _collaboratorInterface = collaborator as ICollaborator;
        if (_collaboratorInterface == null)
        {
            _logService.Log(LogLevel.Error, "Collaborator does not implement ICollaborator interface");
            throw new InvalidOperationException("CollaboratorLib must implement ICollaborator interface");
        }

        _logService.Log(LogLevel.Info, "Collaborator successfully loaded with ICollaborator interface.");
    }

    public async Task<bool> CollaboratorEnabled()
    {
        if (!OperatingSystem.IsWindows())
        {
            _logService.Log(LogLevel.Warn, "Collaborator interface not supported on this platform");
            return false;
        }
        
        // COLLAB_DEBUG environment variable provides runtime control over Collaborator loading
        // - "0" = disable loading (useful for testing StoryCAD without Collaborator overhead)
        // - "1" or unset = enable loading (normal operation)
        //
        // Why environment variable instead of #if DEBUG?
        // - Runtime flexibility: can disable/enable without rebuilding
        // - Test scenarios: can test Store deployment behavior in debug builds
        // - CI/CD: can run tests with/without Collaborator via environment config
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
    ///     Locates CollaboratorLib.dll using STORYCAD_PLUGIN_DIR environment variable.
    ///
    ///     For development/debugging:
    ///     - Set STORYCAD_PLUGIN_DIR to CollaboratorLib build output directory
    ///     - Example: D:\dev\src\Collaborator\CollaboratorLib\bin\x64\Debug\net9.0-windows10.0.22621
    ///
    ///     For production deployment:
    ///     - Plugin will be bundled in MSIX package at AppContext.BaseDirectory
    ///     - See issue #30 for production deployment strategy
    /// </summary>
    /// <returns>True if CollaboratorLib.dll was found and is accessible, false otherwise.</returns>
    private async Task<bool> FindDll()
    {
#if !HAS_UNO
        await Task.CompletedTask; // Async signature required for cross-platform compatibility
        _logService.Log(LogLevel.Info, "Locating CollaboratorLib...");

        if (TryResolveFromEnv(out var envPath))
        {
            dllPath = envPath;
            dllExists = true;
            _logService.Log(LogLevel.Info, $"Found via {EnvPluginDirVar}: {dllPath}");
            return true;
        }

        _logService.Log(LogLevel.Info, "CollaboratorLib.dll not found. Set STORYCAD_PLUGIN_DIR environment variable.");
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

        // Null objects to deallocate them
        _collaboratorInterface = null;
        CollabAssembly = null;
        _logService.Log(LogLevel.Info, "Nulled collaborator objects");

        // Run garbage collection to clean up any remnants
        GC.Collect();
        GC.WaitForPendingFinalizers();
        _logService.Log(LogLevel.Info, "Garbage collection finished.");
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

        CollaboratorWindow = null;
        FinishedCallback();
    }

    #endregion

    /// <summary>
    /// Custom AssemblyLoadContext that resolves plugin dependencies from the plugin directory.
    /// Uses AssemblyDependencyResolver to find dependencies next to the plugin DLL.
    /// </summary>
    private class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Simple strategy: prefer what's already loaded, otherwise load from plugin
            // This ensures StoryCADLib and other shared assemblies use the same instance
            // while allowing plugin-specific dependencies to load from plugin directory

            var existingAssembly = Default.Assemblies
                .FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(
                    a.GetName(), assemblyName));

            if (existingAssembly != null)
            {
                // Assembly already loaded in default context - use that instance
                return null;
            }

            // Not in default context - try to load from plugin directory
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            // Not found anywhere - let runtime handle it
            return null;
        }
    }
}
