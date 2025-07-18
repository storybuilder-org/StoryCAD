# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## About StoryCAD

StoryCAD is a free, open-source Windows application for fiction writers that provides structured outlining tools. It's described as "CAD for fiction writers" and helps writers manage the complexity of plotted fiction through systematic story development.

## Technology Stack

- **Framework**: .NET 8.0 with WinUI 3
- **Platform**: Windows 10/11 (minimum 10.0.19041.0)
- **Architecture**: MVVM with CommunityToolkit.Mvvm
- **UI**: Windows App SDK 1.6 with WinUI 3
- **Dependency Injection**: CommunityToolkit.Mvvm IoC container
- **Logging**: NLog with elmah.io cloud logging
- **Database**: MySQL for certain features
- **AI Integration**: Microsoft Semantic Kernel

## Project Structure

The solution contains three main projects:

1. **StoryCAD** - Main WinUI 3 application (executable)
2. **StoryCADLib** - Core business logic library (generates NuGet package)
3. **StoryCADTests** - MSTest test project with WinUI support

## Build Commands

### Standard Build
```bash
# Restore dependencies
msbuild StoryCAD.sln /t:Restore

# Build solution (Debug)
msbuild StoryCAD.sln /p:Configuration=Debug /p:Platform=x64

# Build solution (Release)
msbuild StoryCAD.sln /p:Configuration=Release /p:Platform=x64

# Build for other platforms
msbuild StoryCAD.sln /p:Configuration=Release /p:Platform=x86
msbuild StoryCAD.sln /p:Configuration=Release /p:Platform=arm64
```

### Using dotnet CLI
```bash
# Restore and build
dotnet restore
dotnet build --configuration Release

# Build specific project
dotnet build StoryCAD/StoryCAD.csproj --configuration Release
```

## Test Commands

### Using MSTest
```bash
# Build tests
msbuild StoryCADTests/StoryCADTests.csproj /t:Build /p:Configuration=Debug /p:Platform=x64

# Run tests with VSTest
vstest.console.exe StoryCADTests/bin/x64/Debug/net8.0-windows10.0.19041.0/StoryCADTests.dll /Logger:Console /Platform:x64
```

### Using dotnet CLI
```bash
# Run all tests
dotnet test StoryCADTests/StoryCADTests.csproj --configuration Debug

# Run tests with settings file
dotnet test StoryCADTests/StoryCADTests.csproj --settings StoryCADTests/mstest.runsettings
```

## Core Architecture

### MVVM Pattern
- **Views**: Located in `/StoryCAD/Views/` - XAML UI definitions
- **ViewModels**: Located in `/StoryCADLib/ViewModels/` - UI logic and data binding
- **Models**: Located in `/StoryCADLib/Models/` - Data structures and business entities
- **Services**: Located in `/StoryCADLib/Services/` - Business logic and data access

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

### Story Elements Hierarchy
The application manages these core story elements:
- **StoryOverview**: Main story information and metadata
- **Problems**: Story conflicts and dramatic issues
- **Characters**: Character development with multiple trait tabs
- **Scenes**: Individual story scenes with structure and sequel tabs
- **Settings**: Story locations with sensory details
- **Folders/Sections**: Organizational containers for grouping elements
- **WebPages**: Research and reference materials

### Navigation Architecture
- **Tree Structure**: `ObservableCollection<StoryNodeItem>` represents the story hierarchy
- **Two Views**: Explorer View (complete hierarchy) and Narrator View (simplified)
- **Drag & Drop**: Full support for reordering elements with validation
- **Selection**: Single-selection tree with context menus for adding elements

#### Root Node Display Architecture (JakeTestBranch)
The navigation tree has been restructured to display root nodes separately from the tree hierarchy:

**Original Structure (main branch):**
- Single `TreeView` containing all nodes in a hierarchical structure
- Root nodes were just top-level items within the TreeView

**New Structure (JakeTestBranch):**
- **ItemsRepeater** iterates over `ShellVm.DataSource` (root nodes)
- **Separate Root Display**: Each root node gets its own standalone `TreeViewItem` (Grid Row 0)
  - Not part of any TreeView hierarchy
  - Appears as flat, non-hierarchical items (like category headers)
  - Uses `RootClick` event handler instead of standard TreeView selection
- **Individual Child TreeViews**: Each root node has its own separate `TreeView` below it (Grid Row 1)
  - Contains only that root's children (`ItemsSource="{x:Bind Children}"`)
  - Children appear in indented tree structures underneath each root

**Benefits:**
- Visual separation between root categories and their hierarchical children
- Prevents users from creating new roots through normal tree operations
- Comment in code: "Shows the root node out of tree, prevents creating new roots"
- Each root acts as a category header with its own tree of children below

## Key Services

### Core Services
- **NavigationService**: Manages page navigation and view transitions
- **LogService**: Comprehensive logging with NLog and elmah.io integration
- **BackupService**: Automated and manual backup functionality
- **AutoSaveService**: Automatic saving with configurable intervals
- **OutlineService**: Story structure management and validation

### Advanced Services
- **CollaboratorService**: AI-powered writing assistance using Semantic Kernel
- **SearchService**: Full-text search across story content
- **ReportService**: Generates various output formats including Scrivener integration
- **PreferencesService**: User settings persistence and management

## Development Notes

### Running the Application
- When debugging in Visual Studio, a developer menu appears with diagnostic tools
- Single instancing works but may need testing outside Visual Studio
- Missing license files won't affect functionality but will disable error reporting

### Testing Strategy
- Unit tests use MSTest framework with `[TestMethod]` attributes
- UI tests use `[UITestMethod]` for WinUI-specific testing
- Test data available in `/TestInputs/` directory
- Test settings configured in `mstest.runsettings`

### File I/O Patterns
- **StoryIO**: Primary file operations for `.stbx` files
- **PreferencesIO**: User settings persistence
- **BackupIO**: Backup file management
- **ScrivenerIO**: Export to Scrivener format

#### .stbx File Format and I/O
**Location**: `/StoryCADLib/DAL/StoryIO.cs`

**.stbx files are JSON-based** story outline files that contain:
- Complete story outline data serialized as JSON
- All story elements and their relationships
- View configurations (Explorer and Narrator views)
- Version tracking information

**Key Methods:**
- **`WriteStory(string output_path, StoryModel model)`**: 
  - Serializes StoryModel to JSON using `model.Serialize()`
  - Saves version information (`model.LastVersion`)
  - Writes JSON to disk using `FileIO.WriteTextAsync()`

- **`ReadStory(StorageFile StoryFile)`**:
  - Reads .stbx file as text
  - Detects legacy XML format and migrates if needed
  - Deserializes JSON using custom converters:
    - `EmptyGuidConverter`
    - `StoryElementConverter` 
    - `JsonStringEnumConverter`
  - Rebuilds tree structure from flattened data using `RebuildTree()`

**Legacy Support:**
- Automatically migrates old XML .stbx files to JSON format
- Backs up original XML files before migration
- `MigrateModel()` method handles XML to JSON conversion

**Cloud Storage Integration:**
- `CheckFileAvailability()` handles cloud storage providers (OneDrive, etc.)
- Robust error handling for offline/unavailable files
- User guidance for cloud storage issues

**Tree Structure Rebuilding:**
- `RebuildTree()` method reconstructs Explorer and Narrator views
- Converts flattened `PersistableNode` data back to hierarchical structure
- Establishes parent-child relationships and identifies root nodes

### UI Patterns
- **Shell.xaml**: Main application shell with SplitView layout
- **Navigation Tree**: ItemsRepeater with nested TreeViews for hierarchical display
- **Content Panes**: Dynamic content based on selected story element
- **Flyout Menus**: Context-sensitive commands for tree operations

### Logging and Diagnostics
- **NLog Configuration**: Multiple targets including file and cloud logging
- **elmah.io Integration**: Cloud-based error tracking (requires API key)
- **Debug Logging**: Extensive trace-level logging for troubleshooting
- **Error Handling**: Centralized exception management with user-friendly messages

## Important Implementation Details

### Story Node Management
- **StoryNodeItem**: Base class for all story elements with common properties
- **Parent/Child Relationships**: Maintained through Parent property and Children collections
- **Validation**: Drag-and-drop operations validated through business rules
- **Persistence**: Auto-save functionality with configurable intervals

### UI State Management
- **Selection State**: Managed through ShellViewModel with last-clicked tracking
- **Expansion State**: TreeView expansion state persisted per node
- **Background Colors**: Dynamic styling based on selection and validation state
- **Context Menus**: Element-specific commands based on node type and position

### Data Binding Patterns
- **TwoWay Binding**: Used for editable content with immediate updates
- **OneWay Binding**: Used for read-only display properties
- **Command Binding**: MVVM commands for user actions
- **Collection Binding**: ObservableCollection for dynamic list updates

## Security and Privacy

- **Local Storage**: All data stored locally under user control
- **No Cloud Sync**: Data remains on user's device unless explicitly exported
- **Open Source**: GNU GPL v3 license ensures transparency
- **Optional Telemetry**: Error reporting requires user consent and API configuration