# Collaborator Interface Simplification Design

**Status**: Draft - Updated with Ownership Insights
**Related Issues**: #1088, #1135, #1136
**Author**: AI Architect-Reviewer Agent + Human Design Review
**Date**: 2025-10-14
**Last Updated**: 2025-10-14 (Added "Key Design Insights" section)

## Executive Summary

This document proposes a simplified plugin interface architecture for StoryCAD's Collaborator plugin that reduces complexity from three interfaces (ICollaborator, IStoryCADAPI, IWorkflowRunner) to a cleaner contract with better separation of concerns, explicit concurrency control, and cross-platform compatibility.

**Key Improvements**:
- Single primary interface (ICollaboratorPlugin) with minimal methods
- Plugin is fully self-contained (owns window, workflow selection, element picking)
- Explicit data locking via standard StoryCAD pattern (using SerializationLock)
- Plugin doesn't receive workflow/element parameters - discovers them via its own UI
- 16% reduction in contract surface (19 vs 23 touchpoints)
- Cross-platform ready (Windows & macOS)

## Key Design Insights from Issue #1134 TODO Cleanup

During cleanup of TODOs in CollaboratorService.cs (Issue #1134), several critical architectural insights emerged about plugin ownership and coupling:

### Insight 1: Plugin Window Ownership is Complete

**Discovery**: If the plugin owns its window, StoryCAD has no responsibility for show/hide/cleanup.

**Impact on Interface**:
- Removed: `ICollaborator.CreateWindow()` - unused confusion
- StoryCAD doesn't manage window lifecycle
- Plugin creates window in `ShowAsync()`, destroys in `DisposeAsync()`

**Code Removed**:
```csharp
//TODO: Use Show and hide properly (Line 464 - DELETED)
//CollaboratorWindow.Show();
//CollaboratorWindow.Activate();

//TODO: Absolutely make sure Collaborator is not left in memory (Line 505 - DELETED)
```

**Rationale**: If plugin owns the window, StoryCAD can't and shouldn't manage it. Plugin is responsible for its own cleanup.

### Insight 2: Plugin Should Be Self-Contained

**Discovery**: Plugin shouldn't need StoryCAD to tell it what workflow or element to work on.

**Current (Coupling)**:
```csharp
await plugin.ShowAsync(elementId, workflow); // StoryCAD decides workflow/element
```

**Proposed (Self-Contained)**:
```csharp
await plugin.ShowAsync(); // Plugin shows its own UI to pick workflow/element
```

**Impact on Interface**:
- Plugin displays its own NavigationView for workflow selection
- Plugin uses its own element picker UI
- Plugin calls `IStoryDataService.GetAllElements()` to browse available elements
- No workflow/element parameters needed in `ShowAsync()`

**Benefits**:
- True plugin independence
- Plugin can add new workflows without StoryCAD code changes
- Plugin owns its entire UX flow
- Easier to maintain separate repositories

### Insight 3: SerializationLock Uses Standard Pattern

**Discovery**: SerializationLock should work the same way for Collaborator as for all other StoryCAD operations.

**Current Confusion** (from TODO line 498):
```csharp
//TODO: Use lock here.
//Ioc.Default.GetRequiredService<OutlineViewModel>()._canExecuteCommands = true;
```

**Proposed Pattern**:
```csharp
public async Task LaunchCollaboratorAsync()
{
    using (var serializationLock = new SerializationLock(_log))
    {
        _autoSaveService.StopAutoSave();
        _backupService.StopTimedBackup();

        await _plugin.ShowAsync();  // Blocks until window closes

        // Lock released when ShowAsync() returns (user closed Collaborator)
        _autoSaveService.StartAutoSave();
        _backupService.StartTimedBackup();
    }
}
```

**Key Points**:
- `ShowAsync()` is **awaitable** - returns only when plugin window closes
- StoryCAD holds lock for entire Collaborator session
- Same pattern as `PreferencesIO.ReadPreferences()` and all other operations
- No special lock handling needed in interface - standard C# `using` pattern

**Impact on Interface**:
- **Removed** from `ICollaboratorContext`: `ISerializationLock DataLock` property
- **Why**: Plugin doesn't manage lock, StoryCAD does (via using block)
- Plugin just calls `IStoryDataService` methods which are already thread-safe internally

### Insight 4: Callbacks May Not Be Needed

**Discovery**: If `ShowAsync()` is awaitable and blocks until completion, callbacks might be unnecessary.

**Current Thought** (from TODOs line 537, 549):
```csharp
//TODO: On calls, set callback delegate
//args.onDoneCallback = FinishedCallback;
```

**Alternative Pattern**:
```csharp
// No callbacks needed - ShowAsync() is synchronous from StoryCAD's perspective
await _plugin.ShowAsync();
// When this returns, plugin is done
```

**Trade-offs**:
- **Pro**: Simpler interface, no callback complexity
- **Con**: Can't report progress/errors while running
- **Decision**: Keep `ICollaboratorCallbacks` for error reporting, but success is implicit in `ShowAsync()` return

### Updated Design Implications

Based on these insights, the interface should be even simpler than originally proposed:

```csharp
public interface ICollaboratorPlugin : IDisposable
{
    void Initialize(ICollaboratorContext context);
    Task ShowAsync();  // No parameters! Blocks until window closes.
    Task DisposeAsync();
}

public interface ICollaboratorContext
{
    IStoryDataService DataService { get; }
    ICollaboratorCallbacks Callbacks { get; }  // For error reporting only
    // NO SerializationLock - StoryCAD manages that
}

public interface ICollaboratorCallbacks
{
    Task OnErrorAsync(string errorMessage, Exception exception = null);
    // OnCompletedAsync and OnCancelledAsync removed - implicit in ShowAsync() return
}
```

**Result**: Even simpler than first proposal - plugin is truly independent.

## Current Architecture Analysis

### Interface Inventory

**ICollaborator** (Primary Plugin Interface):
```csharp
public interface ICollaborator
{
    void LoadWizardViewModel();
    void LoadWorkflowViewModel(StoryItemType elementType);
    void LoadWorkflowModel(StoryElement element, string workflow);
    Task ProcessWorkflowAsync();
    Task SendButtonClickedAsync();
    void SaveOutputs();
    Window CreateWindow(); // NOT USED - window created by StoryCAD
}
```
- **7 methods**
- **Problem**: Window creation confusion - StoryCAD creates window, but interface has CreateWindow()

**IStoryCADAPI** (Data Access Interface):
```csharp
public interface IStoryCADAPI
{
    StoryElement GetElement(Guid elementId);
    void UpdateElement(StoryElement element);
    List<StoryElement> GetAllElements();
    // Plus 6+ more methods not shown
}
```
- **9+ methods** (SemanticKernelAPI has 30+ total)
- **Problem**: Facade exposes too much, tight coupling to implementation

**IWorkflowRunner** (Workflow Execution Interface):
```csharp
public interface IWorkflowRunner
{
    Task<WorkflowResult> RunWorkflowAsync(WorkflowDefinition workflow);
}
```
- **1 method**
- **Problem**: Defined but NOT IMPLEMENTED anywhere, dead code

### Total Contract Surface
- **3 interfaces**
- **17+ methods** across interfaces
- **Plus**: CollaboratorArgs (6 properties), WorkflowViewModel sharing

### Current Data Flow

```
┌──────────────────────────────────────────────────────────┐
│                        StoryCAD                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │           CollaboratorService                     │   │
│  │  • Creates Window                                 │   │
│  │  • Creates CollaboratorArgs (6 properties)        │   │
│  │  • Loads assembly via reflection                  │   │
│  │  • Calls constructor with args                    │   │
│  │  • Stores ICollaborator reference                 │   │
│  │  • Has onDoneCallback delegate                    │   │
│  └───────────┬──────────────────────────────────────┘   │
│              │                                            │
│              │ Window + CollaboratorArgs                 │
│              ▼                                            │
│  ┌─────────────────────────────────────────────────┐    │
│  │        CollaboratorArgs Bundle                   │    │
│  │  • WorkflowViewModel (shared!)                   │    │
│  │  • CollaboratorWindow (created by StoryCAD)      │    │
│  │  • onDoneCallback (single delegate)              │    │
│  │  • StoryModel, SelectedElement, etc.             │    │
│  └───────────┬──────────────────────────────────────┘   │
└──────────────┼──────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────┐
│                   CollaboratorLib Plugin                  │
│  ┌────────────────────────────────────────────────────┐  │
│  │          Collaborator : ICollaborator              │  │
│  │  • Receives window from StoryCAD                   │  │
│  │  • Uses shared WorkflowViewModel                   │  │
│  │  • Calls IStoryCADAPI methods                      │  │
│  │  • Invokes callback when done                      │  │
│  └───────────┬────────────────────────────────────────┘  │
│              │                                             │
│              │ IStoryCADAPI calls                          │
│              ▼                                             │
│  ┌──────────────────────────────────────────────────┐    │
│  │       SemanticKernelAPI : IStoryCADAPI           │    │
│  │  • 30+ methods for data access                    │    │
│  │  • Accesses StoryModel directly                   │    │
│  │  • NO LOCKING (risk of corruption!)               │    │
│  └──────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────┘
```

### Current Problems

1. **Window Ownership Confusion**
   - StoryCAD creates Window
   - ICollaborator.CreateWindow() exists but unused
   - Unclear who owns lifecycle

2. **Tight ViewModel Coupling**
   - WorkflowViewModel shared between StoryCAD and plugin
   - Creates UI dependency
   - Hard to test

3. **Weak Callback Pattern**
   - Single onDoneCallback delegate
   - No error handling
   - No progress reporting
   - No cancellation support

4. **Missing SerializationLock**
   - No concurrency control in interfaces
   - Risk of data corruption if accessed during save
   - StoryCAD has SerializationLock but not used here

5. **Over-Abstraction**
   - IWorkflowRunner defined but never implemented
   - Dead code in contract

6. **API Surface Bloat**
   - IStoryCADAPI: 9 interface methods
   - SemanticKernelAPI: 30+ total methods
   - Much broader than plugin needs

## Proposed Simplified Architecture

### Design Principles

1. **Minimal Coupling**: Plugin only knows what it absolutely needs
2. **Clear Ownership**: Each component owns its resources
3. **Explicit Locking**: SerializationLock prevents corruption
4. **Rich Communication**: Callbacks for success/error/progress
5. **Platform Agnostic**: Works on Windows and macOS
6. **Testable**: Can test without Window creation

### Proposed Interfaces

#### 1. ICollaboratorPlugin (Primary Interface)

```csharp
/// <summary>
/// Main interface for Collaborator plugin.
/// Plugin creates and owns its window.
/// </summary>
public interface ICollaboratorPlugin : IDisposable
{
    /// <summary>
    /// Initialize plugin with StoryCAD services.
    /// Called once after plugin loaded.
    /// </summary>
    /// <param name="context">Services and callbacks from StoryCAD</param>
    void Initialize(ICollaboratorContext context);

    /// <summary>
    /// Show plugin window and start workflow.
    /// Plugin creates its own window if needed.
    /// </summary>
    /// <param name="elementId">Story element to work on</param>
    /// <param name="workflowName">Workflow to run (optional)</param>
    /// <returns>Task completing when window shown</returns>
    Task ShowAsync(Guid elementId, string workflowName = null);

    /// <summary>
    /// Hide plugin window (don't destroy).
    /// </summary>
    void Hide();

    /// <summary>
    /// Clean up resources asynchronously.
    /// </summary>
    Task DisposeAsync();
}
```

**4 methods** - down from 7
- Plugin creates/owns window
- Async initialization
- Element ID passed, not whole object

#### 2. ICollaboratorContext (Initialization Bundle)

```csharp
/// <summary>
/// Context passed to plugin during initialization.
/// Immutable bundle of services.
/// </summary>
public interface ICollaboratorContext
{
    /// <summary>
    /// Data access service (minimal facade).
    /// </summary>
    IStoryDataService DataService { get; }

    /// <summary>
    /// Callbacks for plugin to notify StoryCAD.
    /// </summary>
    ICollaboratorCallbacks Callbacks { get; }

    /// <summary>
    /// Lock for serializing data access.
    /// Plugin must acquire before any data operations.
    /// </summary>
    ISerializationLock DataLock { get; }

    /// <summary>
    /// Optional: Parent window handle for modal dialogs (platform-specific).
    /// IntPtr on Windows, NSWindow* on macOS.
    /// </summary>
    IntPtr ParentWindowHandle { get; }
}
```

**4 properties** - replaces 6-property CollaboratorArgs
- Explicit DataLock
- No ViewModel sharing
- Platform-agnostic handle

#### 3. IStoryDataService (Minimal Data Facade)

```csharp
/// <summary>
/// Minimal data access for plugin.
/// All operations require DataLock to be held.
/// </summary>
public interface IStoryDataService
{
    /// <summary>
    /// Get story element by ID.
    /// Thread-safe: Acquires DataLock internally.
    /// </summary>
    Task<StoryElement> GetElementAsync(Guid elementId);

    /// <summary>
    /// Update story element.
    /// Thread-safe: Acquires DataLock internally.
    /// </summary>
    Task UpdateElementAsync(StoryElement element);

    /// <summary>
    /// Get all story elements (for context).
    /// Thread-safe: Acquires DataLock internally.
    /// </summary>
    Task<IReadOnlyList<StoryElement>> GetAllElementsAsync();
}
```

**3 methods** - down from 9+
- Minimal surface
- Explicit thread safety
- Returns immutable collections

#### 4. ICollaboratorCallbacks (Rich Communication)

```csharp
/// <summary>
/// Callbacks for plugin to communicate with StoryCAD.
/// </summary>
public interface ICollaboratorCallbacks
{
    /// <summary>
    /// Workflow completed successfully.
    /// </summary>
    /// <param name="modifiedElements">Elements changed by workflow</param>
    Task OnWorkflowCompletedAsync(IEnumerable<Guid> modifiedElements);

    /// <summary>
    /// Error occurred during workflow.
    /// </summary>
    Task OnErrorAsync(string errorMessage, Exception exception = null);

    /// <summary>
    /// User cancelled workflow.
    /// </summary>
    Task OnCancelledAsync();
}
```

**3 methods** - replaces single delegate
- Success/error/cancel paths
- Communicates what changed
- Async for flexibility

#### 5. ISerializationLock (Existing)

```csharp
// Already exists in StoryCAD - reuse it
public interface ISerializationLock
{
    Task<IDisposable> LockAsync();
}
```

**1 method** - already exists
- Pattern: `using (await DataLock.LockAsync()) { ... }`

### New Contract Surface

**Total**: 5 interfaces, 15 methods, 4 properties = **19 touchpoints**
- **Down from**: 3 interfaces, 17+ methods, 6 properties = **23+ touchpoints**
- **Reduction**: 16%+ (cleaner, not just smaller)

### Proposed Data Flow

```
┌──────────────────────────────────────────────────────────┐
│                        StoryCAD                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │           CollaboratorService                     │   │
│  │  • Loads plugin assembly                          │   │
│  │  • Creates ICollaboratorContext                   │   │
│  │  • Passes to plugin.Initialize()                  │   │
│  │  • Calls plugin.ShowAsync(elementId)              │   │
│  │  • Plugin creates its own window                  │   │
│  └───────────┬──────────────────────────────────────┘   │
│              │                                            │
│              │ ICollaboratorContext                       │
│              ▼                                            │
│  ┌─────────────────────────────────────────────────┐    │
│  │     ICollaboratorContext Bundle                  │    │
│  │  • IStoryDataService (facade)                    │    │
│  │  • ICollaboratorCallbacks (rich)                 │    │
│  │  • ISerializationLock (explicit)                 │    │
│  │  • ParentWindowHandle (optional)                 │    │
│  └───────────┬──────────────────────────────────────┘   │
└──────────────┼──────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────┐
│                   CollaboratorLib Plugin                  │
│  ┌────────────────────────────────────────────────────┐  │
│  │    Collaborator : ICollaboratorPlugin              │  │
│  │  • Receives context in Initialize()                │  │
│  │  • Creates OWN window in ShowAsync()               │  │
│  │  • Acquires DataLock before data access            │  │
│  │  • Calls callbacks (success/error/cancel)          │  │
│  └───────────┬────────────────────────────────────────┘  │
│              │                                             │
│              │ IStoryDataService calls                     │
│              ▼                                             │
│  ┌──────────────────────────────────────────────────┐    │
│  │    StoryDataServiceImpl (in StoryCAD)            │    │
│  │  • 3 methods: Get, Update, GetAll                │    │
│  │  • Acquires DataLock internally                   │    │
│  │  • Calls OutlineService                           │    │
│  └──────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────┘
```

### Key Improvements

1. **Plugin Owns Window**
   - Clear ownership
   - Plugin controls lifecycle
   - Cross-platform flexibility

2. **No ViewModel Sharing**
   - Plugin builds its own UI
   - Loose coupling
   - Easy to test

3. **Explicit DataLock**
   - Prevents corruption
   - Clear contract
   - Thread-safe by design

4. **Rich Callbacks**
   - Success/error/cancel paths
   - Communicates changes
   - Async-friendly

5. **Minimal Data Service**
   - 3 methods only
   - What plugin actually needs
   - Easy to mock

## Implementation Example

### StoryCAD Side

```csharp
public class CollaboratorService
{
    private ICollaboratorPlugin _plugin;
    private readonly OutlineService _outlineService;
    private readonly ISerializationLock _serializationLock;

    public async Task LoadPluginAsync(string assemblyPath)
    {
        // Load assembly
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        var pluginType = assembly.GetType("CollaboratorLib.Collaborator");

        // Create instance
        _plugin = Activator.CreateInstance(pluginType) as ICollaboratorPlugin;

        // Create context
        var context = new CollaboratorContext
        {
            DataService = new StoryDataServiceImpl(_outlineService, _serializationLock),
            Callbacks = new CollaboratorCallbacksImpl(this),
            DataLock = _serializationLock,
            ParentWindowHandle = IntPtr.Zero // Optional
        };

        // Initialize plugin
        _plugin.Initialize(context);
    }

    public async Task ShowCollaboratorAsync(Guid elementId, string workflow = null)
    {
        if (_plugin == null)
            throw new InvalidOperationException("Plugin not loaded");

        await _plugin.ShowAsync(elementId, workflow);
    }
}

// Implementation of IStoryDataService
internal class StoryDataServiceImpl : IStoryDataService
{
    private readonly OutlineService _outlineService;
    private readonly ISerializationLock _lock;

    public async Task<StoryElement> GetElementAsync(Guid elementId)
    {
        using (await _lock.LockAsync())
        {
            return _outlineService.GetElement(elementId);
        }
    }

    public async Task UpdateElementAsync(StoryElement element)
    {
        using (await _lock.LockAsync())
        {
            _outlineService.UpdateElement(element);
        }
    }

    public async Task<IReadOnlyList<StoryElement>> GetAllElementsAsync()
    {
        using (await _lock.LockAsync())
        {
            return _outlineService.GetAllElements().AsReadOnly();
        }
    }
}

// Implementation of ICollaboratorCallbacks
internal class CollaboratorCallbacksImpl : ICollaboratorCallbacks
{
    private readonly CollaboratorService _service;

    public async Task OnWorkflowCompletedAsync(IEnumerable<Guid> modifiedElements)
    {
        _logger.Log(LogLevel.Info, $"Workflow completed, {modifiedElements.Count()} elements modified");

        // Re-enable auto-save
        _autoSaveService.StartAutoSave();
        _backupService.StartTimedBackup();
    }

    public async Task OnErrorAsync(string errorMessage, Exception exception)
    {
        _logger.Log(LogLevel.Error, $"Collaborator error: {errorMessage}", exception);
        // Show error dialog
    }

    public async Task OnCancelledAsync()
    {
        _logger.Log(LogLevel.Info, "Workflow cancelled by user");
    }
}
```

### Plugin Side

```csharp
namespace CollaboratorLib
{
    public class Collaborator : ICollaboratorPlugin
    {
        private ICollaboratorContext _context;
        private Window _window;
        private WorkflowViewModel _viewModel;

        public void Initialize(ICollaboratorContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            // No window creation here - happens in ShowAsync
        }

        public async Task ShowAsync(Guid elementId, string workflowName)
        {
            // Get element data
            var element = await _context.DataService.GetElementAsync(elementId);

            // Create window if needed
            if (_window == null)
            {
                _window = new Window { Title = "StoryCAD Collaborator" };
                _window.Closed += (s, e) => OnWindowClosed();
            }

            // Create ViewModel
            _viewModel = new WorkflowViewModel(element, workflowName);

            // Set content
            _window.Content = new WorkflowView { DataContext = _viewModel };

            // Show
            _window.Activate();
        }

        public void Hide()
        {
            _window?.Hide();
        }

        private async void OnWorkflowCompleted(List<Guid> modifiedElements)
        {
            // Notify StoryCAD
            await _context.Callbacks.OnWorkflowCompletedAsync(modifiedElements);
            Hide();
        }

        public void Dispose()
        {
            _window?.Close();
            _window = null;
        }

        public async Task DisposeAsync()
        {
            Dispose();
            await Task.CompletedTask;
        }
    }
}
```

## Cross-Platform Considerations

### Platform-Agnostic Types

All interface types work on both Windows and macOS:
- `Guid` - standard .NET type
- `Task` - standard async pattern
- `StoryElement` - shared data model
- `IntPtr` - platform window handle (optional)

### Platform-Specific Window Creation

```csharp
// Windows (WinUI 3)
#if HAS_UNO_WINUI
_window = new Microsoft.UI.Xaml.Window();
#endif

// macOS (UNO Desktop)
#if __MACOS__
_window = new Microsoft.UI.Xaml.Window(); // UNO provides abstraction
#endif
```

UNO Platform provides unified Window API - same code works on both.

### Plugin Loading

Platform differences handled in CollaboratorService:
- **Windows**: MSIX package or bundled DLL
- **macOS**: Bundled in app bundle or Application Support

But once loaded, ICollaboratorPlugin interface works identically.

## Migration Strategy

### Phase 1: Define New Interfaces (Non-Breaking)

1. Add new interfaces alongside existing ones
2. No changes to existing code
3. Duration: 1 day

### Phase 2: Implement in CollaboratorService (Dual Support)

1. Update CollaboratorService to support both old and new interfaces
2. Try new interface first, fall back to ICollaborator
3. Duration: 2 days

```csharp
public void ConnectCollaborator()
{
    var instance = constructor.Invoke(methodArgs);

    // Try new interface first
    if (instance is ICollaboratorPlugin newPlugin)
    {
        _newPlugin = newPlugin;
        var context = CreateContext();
        _newPlugin.Initialize(context);
        return;
    }

    // Fall back to old interface
    if (instance is ICollaborator oldPlugin)
    {
        _collaboratorInterface = oldPlugin;
        // Existing code
    }
}
```

### Phase 3: Update CollaboratorLib

1. Implement ICollaboratorPlugin in Collaborator class
2. Remove ICollaborator implementation
3. Test on Windows
4. Duration: 3 days

### Phase 4: Test Cross-Platform

1. Test new interface on macOS
2. Verify window creation works
3. Verify data access works
4. Duration: 2 days

### Phase 5: Remove Legacy Interfaces

1. Remove ICollaborator, IStoryCADAPI, IWorkflowRunner
2. Remove fallback code from CollaboratorService
3. Update documentation
4. Duration: 1 day

**Total Estimated Effort**: 2 weeks (9 working days + buffer)

## Benefits and Trade-offs

### Benefits

1. **Clarity**: Clear ownership, explicit locking
2. **Testability**: Can test without Window
3. **Cross-Platform**: Works on Windows & macOS
4. **Maintainability**: Smaller contract, less to document
5. **Safety**: Explicit locking prevents corruption
6. **Flexibility**: Rich callbacks for all scenarios

### Trade-offs

1. **Breaking Change**: Requires CollaboratorLib update
2. **Migration Effort**: 2 weeks of work
3. **Learning Curve**: New patterns for developers

### Risk Mitigation

1. **Dual Support**: Phase 2 supports both interfaces
2. **Testing**: Comprehensive test suite
3. **Documentation**: Clear migration guide
4. **Gradual Rollout**: Deploy to beta users first

## Conclusion

The proposed single-interface architecture with explicit locking and rich callbacks provides a cleaner, safer, and more maintainable contract between StoryCAD and CollaboratorLib. The 16% reduction in contract surface is less important than the improved clarity and cross-platform compatibility.

**Recommendation**: Proceed with migration in conjunction with Issues #1135 (macOS plugin) and #1136 (Cloudflare proxy) to maximize architectural improvements while minimizing disruption.

---

**Next Steps**:
1. Review this design with stakeholders
2. Create detailed implementation plan for each phase
3. Set up test strategy (unit + integration)
4. Begin Phase 1 (define interfaces)
