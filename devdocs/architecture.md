# StoryCAD Architecture Guide

## Platform Architecture (UNO Platform)

### Current Branch Status
- **Production (main)**: WinUI 3 (Windows App SDK) - Version 3.x
- **Development (UNOTestBranch)**: UNO Platform conversion - Version 4.0
- **Target Platforms**:
  - Windows (WinAppSDK head)
  - macOS (Desktop head)
  - Future: Linux, WebAssembly, iOS, Android

### UNO Platform Architecture
StoryCAD 4.0 uses [UNO Platform](https://platform.uno/) to achieve cross-platform support while maintaining near-identical codebase with the WinUI 3 version. The architecture supports:
- **Single Codebase**: 90% shared code across platforms
- **Platform Heads**: Platform-specific implementations for 10% of features
- **Native Performance**: Each platform uses native UI rendering

## Core Architecture

### MVVM Pattern
- **Views**: Located in `/StoryCAD/Views/` - XAML UI definitions (shared across platforms)
- **ViewModels**: Located in `/StoryCADLib/ViewModels/` - UI logic and data binding (platform-agnostic)
- **Models**: Located in `/StoryCADLib/Models/` - Data structures and business entities (platform-agnostic)
- **Services**: Located in `/StoryCADLib/Services/` - Business logic and data access (platform-agnostic with interfaces for platform-specific implementations)

### Dependency Injection
- Uses CommunityToolkit.Mvvm IoC container
- Services registered in `ServiceLocator.cs`
- Access pattern: `Ioc.Default.GetRequiredService<T>()`
- All services are registered as singletons

### Data Model
- **Primary Model**: `StoryModel` - central data structure containing all story elements
- **File Format**: `.stbx` files (JSON-based serialization)
- **Storage**: Local filesystem with automatic backup capabilities
- **Serialization**: System.Text.Json with custom converters

## Data Binding Patterns

### Property Change Notifications
Always use properties (not fields) for data binding:

```csharp
// WRONG - Field won't trigger binding updates
private StoryModel _storyModel;

// CORRECT - Property with change notification
private StoryModel _storyModel;
public StoryModel StoryModel 
{ 
    get => _storyModel;
    set => SetProperty(ref _storyModel, value);
}
```

### Collection Change Monitoring
To track changes in ObservableCollections (similar to PropertyChanged for individual properties):

```csharp
private ObservableCollection<StoryNodeItem> _explorerView;
public ObservableCollection<StoryNodeItem> ExplorerView
{
    get => _explorerView;
    set
    {
        if (_explorerView != null)
            _explorerView.CollectionChanged -= ExplorerView_CollectionChanged;
        
        SetProperty(ref _explorerView, value);
        
        if (_explorerView != null)
            _explorerView.CollectionChanged += ExplorerView_CollectionChanged;
    }
}

private void ExplorerView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
{
    // Mark model as changed when collection is modified
    Changed = true;
}
```

This pattern is used throughout StoryCAD to track when collections are modified, just as `OnPropertyChanged` tracks individual property changes in ViewModels.

## Story Elements Hierarchy

The application manages these core story elements:
- **StoryOverview**: Main story information and metadata
- **Problems**: Story conflicts and dramatic issues
- **Characters**: Character development with multiple trait tabs
- **Scenes**: Individual story scenes with structure and sequel tabs
- **Settings**: Story locations with sensory details
- **Folders/Sections**: Organizational containers for grouping elements
- **WebPages**: Research and reference materials
- **TrashCan**: Container for deleted elements (special handling)

## Navigation Architecture

### Current Structure
- **Tree Structure**: `ObservableCollection<StoryNodeItem>` represents the story hierarchy
- **Three Collections**: 
  - ExplorerView (complete hierarchy)
  - NarratorView (simplified view)
  - TrashView (deleted items)
- **CurrentView**: Points to either ExplorerView or NarratorView based on user selection
- **Drag & Drop**: Full support with validation, restricted between main tree and trash
- **Selection**: Single-selection tree with context menus

### Alternative Root Node Display
In some experimental branches, the navigation tree displays root nodes separately:
- ItemsRepeater iterates over root nodes
- Each root has its own TreeView for children
- Prevents users from creating new roots accidentally

## Key Services

### Core Services
- **NavigationService**: Manages page navigation and view transitions
- **LogService**: Comprehensive logging with NLog and elmah.io integration
- **BackupService**: Automated and manual backup functionality
- **AutoSaveService**: Automatic saving with configurable intervals
- **OutlineService**: Story structure management and validation

### Advanced Services
- **CollaboratorService**: AI-powered writing assistance using Semantic Kernel
- **SearchService**: Unified service for both string-based content search and UUID-based reference search with optional deletion
- **ReportService**: Generates various output formats including Scrivener integration
- **PreferencesService**: User settings persistence and management

## Platform-Specific Implementations

### Platform Abstraction Strategy
UNO Platform allows most code to be shared, but certain features require platform-specific implementations:

#### Shared Code (90%)
- All ViewModels and business logic
- Data models and serialization
- Most UI (XAML) definitions
- Service interfaces and core implementations

#### Platform-Specific Code (10%)
Platform-specific implementations are handled through:
- **Conditional Compilation**: `#if HAS_UNO_WINUI` or `#if __MACOS__`
- **Dependency Injection**: Platform-specific service implementations
- **Partial Classes**: Platform-specific UI code-behind

### Windows (WinAppSDK Head)
- **File Dialogs**: Native Windows file pickers
- **Keyboard Shortcuts**: Ctrl-based shortcuts
- **Context Menus**: Windows-style right-click menus
- **Window Management**: Multiple window support
- **Storage**: Windows ApplicationData folders

### macOS (Desktop Head)
- **File Dialogs**: Native macOS file dialogs
- **Keyboard Shortcuts**: Cmd-based shortcuts
- **Menu Bar**: Native macOS menu bar at top of screen
- **Storage**: macOS application support directories
- **Gestures**: Trackpad gesture support

### Platform Service Example
```csharp
// Interface in shared code
public interface IFileDialogService
{
    Task<string> OpenFileAsync(string[] extensions);
    Task<string> SaveFileAsync(string defaultName);
}

// Windows implementation
#if HAS_UNO_WINUI
public class WindowsFileDialogService : IFileDialogService
{
    public async Task<string> OpenFileAsync(string[] extensions)
    {
        var picker = new FileOpenPicker();
        // Windows-specific implementation
    }
}
#endif

// macOS implementation
#if __MACOS__
public class MacFileDialogService : IFileDialogService
{
    public async Task<string> OpenFileAsync(string[] extensions)
    {
        // macOS-specific implementation using NSOpenPanel
    }
}
#endif
```

## File I/O Architecture

The `StoryModel` is the in-memory representation of a `.stbx` file outline. All file I/O operations are handled by `/StoryCADLib/DAL/StoryIO.cs`.

### StoryModel Structure
A StoryModel contains:
- **StoryElements**: Collection of all story items (overview, characters, scenes, etc.)
- **ExplorerView**: Complete hierarchical tree of all elements
- **NarratorView**: Simplified view for linear storytelling
- **TrashView**: Deleted items awaiting permanent removal
- **CurrentView**: Points to either Explorer or Narrator view
- **Changed**: Tracks if the model has unsaved modifications
- **Version**: File format version for compatibility

### .stbx File Format
**.stbx files are JSON-based** story outline files. The JSON structure includes:
- Flattened list of all story elements with parent-child relationships via UUIDs
- View configurations (which nodes appear in Explorer vs Narrator views)
- Node states (expanded/collapsed, selected)
- Version information for format migrations

### Key I/O Methods

**WriteStory(string output_path, StoryModel model)**:
- Serializes StoryModel to JSON using `model.Serialize()`
- Saves current version information
- Writes JSON to disk using `FileIO.WriteTextAsync()`

**ReadStory(StorageFile StoryFile)**:
- Reads .stbx file as text
- Detects and migrates legacy XML format if needed
- Deserializes JSON using custom converters:
  - `EmptyGuidConverter` - Handles empty GUID strings
  - `StoryElementConverter` - Polymorphic deserialization of element types
  - `JsonStringEnumConverter` - Enum serialization
- Calls `RebuildTree()` to reconstruct hierarchical views from flat data

### Tree Structure Rebuilding
The `RebuildTree()` method reconstructs the hierarchical views:
1. Removes duplicate nodes (by UUID)
2. Identifies root nodes (nodes without parents)
3. Establishes parent-child relationships
4. Populates ExplorerView and NarratorView based on view flags
5. Handles legacy dual-root migration if needed
6. Initializes TrashView with TrashCan root
7. Sets CurrentView to appropriate view

### Legacy Format Support
- Automatically detects and migrates old XML .stbx files to JSON
- Creates backup of original XML file before migration
- Handles dual-root structure where TrashCan was a second root in ExplorerView

## UI State Management

### Selection State
- Managed through ShellViewModel with last-clicked tracking
- RightTappedNode property for context menu operations
- LastClickedNode for navigation state

### Expansion State
- TreeView expansion state persisted per node
- IsExpanded property on each StoryNodeItem

### Background Colors
- Dynamic styling based on selection and validation state
- Different colors for different node types

### Context Menus
- Element-specific commands based on node type
- Position-aware (e.g., can't add to TrashCan)
- Validation before showing options

## Common Patterns

### Model Creation and Initialization
When creating new models from templates:
```csharp
// Create model with template
var model = await outlineService.CreateModel(name, author, templateIndex);

// Reset Changed flag after template creation
// Template adds nodes which trigger change events
model.Changed = false;
```

### View Switching
```csharp
public void SetCurrentView(StoryModel model, StoryViewType viewType)
{
    switch (viewType)
    {
        case StoryViewType.ExplorerView:
            model.CurrentView = model.ExplorerView;
            model.CurrentViewType = StoryViewType.ExplorerView;
            break;
        case StoryViewType.NarratorView:
            model.CurrentView = model.NarratorView;
            model.CurrentViewType = StoryViewType.NarratorView;
            break;
    }
}
```

### Element Restoration from Trash
Business rules for restore:
1. Only top-level items can be restored (not children)
2. Parent restoration brings all children automatically
3. Items restore to end of root node's children
4. Validation messages guide user behavior

## Threading and Async Patterns

- UI operations must happen on UI thread
- File I/O operations are async and marshaled to I/O thread
- Collection modifications need synchronization
- Use `ConfigureAwait(false)` for non-UI async operations
- Marshal I/O calls using dispatcher pattern throughout codebase

Common pattern for UI thread marshaling:
```csharp
await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
{
    // UI updates here
});
```

Common pattern for I/O operations:
```csharp
await Task.Run(async () =>
{
    // File I/O operations here
}).ConfigureAwait(false);
```

## Security and Privacy

### Secret Management
- **Doppler Integration**: Production secrets (elmah.io API key, MySQL connection) fetched from Doppler service
- **Local Development**: Uses `.env` files with tokens (SYNCFUSION_TOKEN, DOPPLER_TOKEN)
- **Never in Code**: API keys and secrets never hardcoded in source

### Cloud Storage Challenges
- **File Availability**: `CheckFileAvailability()` handles cloud-synced files (OneDrive, Google Drive, etc.)
- **Sync Issues**: Files may be unavailable if offline or not synced locally
- **User Guidance**: Clear error messages when cloud files can't be accessed
- **Force Local Pull**: Attempts to read file to trigger cloud provider download

### Privacy Concerns
- **Logging Issue**: Currently logs full `preferences.json` including:
  - User name and email
  - Directory paths
  - All preference settings
- **TODO**: Need to mask sensitive data before logging
- **elmah.io**: Recent logs sent to cloud error tracking (requires user consent)

### Data Storage
- **Local by Default**: Story files stored on user's device
- **User Control**: All exports/sharing initiated by user
- **No Auto-Sync**: No automatic cloud backup or sync built into StoryCAD

## Error Tracking (elmah.io)

### Configuration
- API key retrieved from Doppler in production
- Requires explicit user consent (`ErrorCollectionConsent` preference)
- Logs include system info, preferences, and error details

### Logged Information
- System architecture and Windows version
- Application performance metrics
- User preferences (INCLUDING sensitive data - needs fixing)
- Stack traces and error context

### Privacy Controls
- User can opt out via preferences
- Local logging always available as alternative
- Clear consent dialog on first run