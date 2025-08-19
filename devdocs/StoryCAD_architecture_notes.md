# StoryCAD Architecture Notes
*Compiled from Issue #1069 Test Coverage Implementation*
*Date: 2025-01-18*

## Overview
StoryCAD is a Windows desktop application for fiction writers that provides structured outlining tools. It's described as "CAD for fiction writers" and helps writers manage the complexity of plotted fiction through systematic story development.

## Technology Stack
- **Framework**: .NET 9.0 with WinUI 3
- **UI Framework**: Windows App SDK 1.6
- **Architecture Pattern**: MVVM with CommunityToolkit.Mvvm
- **Testing**: MSTest with WinUI support
- **Platform**: Windows 10/11 (minimum 10.0.19041.0)
- **Language**: C#
- **License**: GNU GPL v3 (open source)

## Project Structure

### Solution Components
1. **StoryCAD** - Main WinUI 3 application (executable)
2. **StoryCADLib** - Core business logic library (NuGet package)
3. **StoryCADTests** - MSTest test project

### Key Architectural Layers

#### 1. Data Model Layer
- **StoryModel**: In-memory representation of a story outline
- **StoryElement**: Base class for all story components
  - Derived types: CharacterModel, SceneModel, ProblemModel, SettingModel, FolderModel, WebModel, etc.
- **StoryNodeItem**: Tree node representation for UI display
- **StoryElementCollection**: ObservableCollection with automatic indexing via StoryElementGuids dictionary

#### 2. Service Layer
- **OutlineService**: Core service for story structure management
  - Designed to be **stateless** - receives StoryModel as parameter
  - Handles all story operations (CRUD, tree manipulation, file I/O)
  - Thread-safe operations using SerializationLock
  
- **NavigationService**: Page navigation and view transitions
- **AutoSaveService**: Automatic saving with configurable intervals
- **BackupService**: Automated backup functionality
- **CollaboratorService**: AI-powered writing assistance
- **SearchService**: Full-text search across story content
- **LogService**: Comprehensive logging with NLog and elmah.io integration

#### 3. API Layer
- **SemanticKernelAPI**: External API for LLM and external tool integration
  - Implements IStoryCADAPI interface
  - Uses OperationResult<T> pattern for safe consumption
  - All operations wrapped in try-catch for safety
  - Thread-safe with SerializationLock
  - **Calls OutlineService directly** for all story operations
  - Returns OperationResult to external callers (never raw exceptions)

#### 4. ViewModel Layer
- **ShellViewModel**: Main application shell and navigation
  - Handles UI interactions and commands
  - Manages view state and tree operations
  - **Calls OutlineService** for business logic operations
  - Does NOT use OperationResult (internal, can handle exceptions)
  
- **OutlineViewModel**: Story outline management
  - File operations (new, open, save, close)
  - **Delegates to OutlineService** for all business logic
  - Direct exception handling (no OperationResult needed)

- Various element ViewModels (CharacterViewModel, SceneViewModel, etc.)
  - Handle element-specific UI logic
  - Data binding to element models

#### 5. View Layer
- **Shell.xaml**: Main application window with TreeView
- Element-specific views for editing story components
- Tool windows (KeyQuestions, Topics, MasterPlots, etc.)

## Architectural Flow and Relationships

### Call Hierarchy
```
External Tools/LLMs
        ↓
SemanticKernelAPI (uses OperationResult<T>)
        ↓
    OutlineService ← Also called by → ViewModels (ShellViewModel, OutlineViewModel)
        ↓                                    ↑
    StoryModel                           UI Events
```

### Key Relationship: ViewModels vs API to OutlineService
1. **ViewModels → OutlineService**:
   - Direct method calls
   - Can throw and handle exceptions normally
   - Synchronous or async as needed
   - No OperationResult wrapper needed (internal code)
   - Example: `ShellViewModel.SaveModel()` → `OutlineService.WriteModel()`

2. **API → OutlineService**:
   - All calls wrapped in try-catch
   - Returns OperationResult<T> for safe external consumption
   - Never allows exceptions to escape to external callers
   - Consistent error handling pattern
   - Example: `API.AddElement()` → returns `OperationResult<Guid>`

### Why OperationResult for API but not ViewModels?
- **API**: External callers (LLMs, tools) need safe, predictable interface
  - Can't assume error handling capability
  - Need structured error information
  - Must prevent application crashes from external code
  
- **ViewModels**: Internal application code
  - Can handle exceptions appropriately
  - Has access to UI for error display
  - Part of the trusted codebase
  - Can use application-wide error handling

### OutlineService as Central Hub
- **Stateless design**: Receives StoryModel as parameter
- **Called by both** ViewModels and API
- **Single source of truth** for business logic
- **Thread-safe** via SerializationLock
- **No UI dependencies**: Pure business logic

## Key Architectural Patterns

### 1. MVVM (Model-View-ViewModel)
- Strict separation between UI and business logic
- Data binding using CommunityToolkit.Mvvm
- INotifyPropertyChanged for property change notifications
- RelayCommand for command binding

### 2. Dependency Injection
- Uses Ioc.Default from CommunityToolkit
- Services registered at startup
- ViewModels resolve dependencies through DI container

### 3. Thread Safety Pattern
```csharp
using (var serializationLock = new SerializationLock(autoSaveService, backupService, _logger))
{
    // Thread-safe operation
}
```
- Ensures serial reusability (one operation completes before another starts)
- Prevents concurrent modifications to story data
- Used throughout OutlineService and API

### 4. OperationResult Pattern
```csharp
public class OperationResult<T>
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public T Payload { get; set; }
}
```
- Safe error handling for API operations
- Prevents exceptions from propagating to external callers
- Clear success/failure indication with error messages

## Data Management

### Story Structure
- **Tree-based hierarchy**: Stories organized as trees of elements
- **Two views**: 
  - ExplorerView: Main story structure
  - NarratorView: Alternative narrative organization
- **Parent-Child relationships**: Maintained through Parent property and Children collections
- **GUID-based references**: Elements referenced by Guid, not string names

### Trash Architecture (Current)
- **Separate TrashView**: Trash is now a separate TreeView (not dual-root)
- **TrashCan node**: Root node in TrashView
- **Deletion**: Elements moved to TrashCan, preserving hierarchy
- **Restoration**: Only top-level items in trash can be restored
- **Subtree preservation**: Deleted subtrees maintain structure in trash

### Previous Architecture (Legacy)
- TrashCan was second root node in main TreeView
- Some tests still assume this old architecture
- Migration handled during file load

## State Management

### Model State
- **Changed flag**: Tracks if model has unsaved changes
- **CurrentView**: Tracks active view (Explorer/Narrator)
- **StoryElementGuids**: Dictionary for fast element lookup by GUID

### Stateless Services
- OutlineService designed to be stateless
- Receives StoryModel as parameter for all operations
- No global state dependencies
- Improves testability and maintainability

### UI State
- **Selection state**: Managed through ShellViewModel with last-clicked tracking
- **Expansion state**: TreeView expansion persisted per node
- **Background colors**: Dynamic styling based on selection/validation
- **Context menus**: Element-specific commands based on node type

## File Management

### File Format
- **Extension**: .stbx (StoryCAD Binary XML)
- **Serialization**: XML-based format
- **Backward compatibility**: Handles legacy file structures
- **Auto-migration**: Updates old formats on load

### File Operations
- **Templates**: 3 built-in templates for new stories
- **Auto-save**: Configurable interval (default enabled)
- **Backup**: Automatic backup creation
- **Recent files**: Track recently opened files

## Testing Architecture

### Test Types
- **Unit tests**: [TestMethod] attribute
- **UI tests**: [UITestMethod] for WinUI-specific testing
- **Integration tests**: Test service interactions

### Test Conventions
- **File naming**: `[SourceFileName]Tests.cs`
- **Method naming**: `MethodName_Scenario_ExpectedResult`
- **Test data**: Located in /TestInputs/ directory
- **Mocking**: Uses DI container for test doubles

### Coverage Requirements
- SemanticKernelAPI: 100% coverage achieved
- OutlineService: 100% coverage achieved
- Critical paths have integration tests
- Thread safety verified in tests

## Security and Privacy

### Data Storage
- All data stored locally on user's device
- No automatic cloud synchronization
- User has complete control over their data

### Error Reporting
- Optional telemetry requires user consent
- elmah.io integration for error logging (opt-in)
- No personal data collected without permission

## Key Design Decisions

### 1. Stateless Services
- Services don't maintain state between calls
- Improves testability and reduces bugs
- Enables better concurrency control

### 2. GUID-based References
- Elements referenced by GUID instead of names
- Prevents issues with duplicate names
- Enables reliable element tracking

### 3. Separate Trash View
- Trash moved from dual-root to separate TreeView
- Cleaner architecture and UI
- Better separation of concerns

### 4. SerializationLock Pattern
- Ensures operations complete atomically
- Prevents data corruption from concurrent access
- Integrates with auto-save and backup

### 5. OperationResult for API
- Safe error handling for external callers
- No unhandled exceptions leak to API consumers
- Clear success/failure semantics

## Relationship Management

### Character Relationships
- **RelationshipModel**: Defines relationships between characters
- **Duplicate prevention**: Checks Partner, RelationType, Trait, Attitude (Notes ignored)
- **Bidirectional**: Can be set from either character

### Cast Members
- **Scene-Character associations**: Characters can be cast in scenes
- **Role tracking**: Track character roles in specific scenes
- **Validation**: Ensures valid character and scene references

## Search and Reference System

### Text Search
- Case-insensitive search across all story content
- Returns formatted results with element context
- Subtree search capability for scoped searches

### Reference Management
- **FindElementReferences**: Locate all references to an element
- **RemoveReferences**: Clean up references when deleting elements
- **Automatic cleanup**: References removed during element deletion

## Element Conversion

### Problem ↔ Scene Conversion
- Preserves child elements during conversion
- Maintains tree structure
- Updates element type and properties
- Name synchronization between StoryElement and StoryNodeItem

## Known Architectural Considerations

### 1. Name Synchronization
- StoryElement.Name and StoryNodeItem.Name must stay synchronized
- Setting StoryElement.Name now automatically updates Node.Name
- Critical for tree display accuracy

### 2. Headless Mode
- Used for testing and API operations
- Some features (like Changed flag) work differently in headless mode
- Issue #1068 will address this limitation

### 3. Drag-and-Drop
- Currently implemented at UI layer only
- No business logic validation
- TreeView control handles internally
- Future: Consider adding validation layer

### 4. View Model Responsibilities
- ShellViewModel has high responsibility (potential refactoring target)
- Some operations could be moved to services
- Balance between MVVM purity and practical implementation

## Performance Considerations

### Indexing
- StoryElementGuids dictionary provides O(1) lookup
- Critical for large story outlines
- Automatically maintained by StoryElementCollection

### Serialization
- SerializationLock prevents performance issues from concurrent operations
- Auto-save runs on separate thread
- Backup operations are asynchronous

### Memory Management
- Story models kept in memory during editing
- Large stories may require optimization
- Consider lazy loading for very large outlines

## Future Architecture Considerations

### Potential Improvements
1. **Extract more logic from ViewModels to Services**
   - Reduce ViewModel complexity
   - Improve testability

2. **Add validation layer for drag-and-drop**
   - Business rules for valid moves
   - Prevent invalid tree operations

3. **Improve Changed flag handling**
   - Work consistently in headless mode
   - Better integration with auto-save

4. **Consider command pattern for operations**
   - Enable undo/redo functionality
   - Better operation tracking

5. **Add caching layer**
   - Improve performance for large stories
   - Cache frequently accessed elements

## Integration Points

### AI/LLM Integration
- Via SemanticKernelAPI
- CollaboratorService for AI assistance
- Extensible for future AI features

### External Tools
- API enables external tool integration
- File format is parseable XML
- Command-line operations possible

### Version Control
- Text-based format enables Git integration
- Diff-friendly XML structure
- Collaborative editing possible with VCS

## Error Handling Strategy

### Service Layer
- Exceptions caught and logged
- Return appropriate error codes/messages
- Never expose internal exceptions to UI

### API Layer
- All operations wrapped in try-catch
- OperationResult pattern for safe consumption
- Detailed error messages for debugging

### UI Layer
- User-friendly error messages
- Graceful degradation
- Recovery options where possible

---
*Note: This document represents the current understanding of StoryCAD architecture as observed during the test coverage implementation project. Some details may require verification or expansion.*