# StoryCAD Architecture Documentation

*Compiled from Issue #1069 Test Coverage Implementation*
*Updated with Issue #1100 Architecture Refactoring*
*Revised with UNO Platform Integration - 2025-10-08*

---

## Overview

StoryCAD is a cross-platform desktop application for fiction writers that provides structured outlining tools. Described as "CAD for fiction writers," it helps writers manage the complexity of plotted fiction through systematic story development.

**Platform Support**: Windows 10/11, macOS (desktop), with potential for Linux support
**License**: GNU GPL v3 (open source)
**Repository**: StoryCAD

---

## Technology Stack

### Core Technologies

- **Framework**: .NET 9.0
- **UI Platform**: UNO Platform (SDK 6.2.36)
- **UI Framework**:
  - WinUI 3 on Windows (via Windows App SDK 1.6)
  - Skia rendering on macOS/Linux
- **Architecture Pattern**: MVVM with CommunityToolkit.Mvvm
- **Testing**: MSTest with WinUI support
- **Language**: C#

### Platform-Specific Technologies

**Windows Target** (`net9.0-windows10.0.22621`):
- Windows App SDK 1.6
- Native WinUI 3 controls
- MSIX packaging
- Windows-specific APIs (file associations, Package.Current)

**Desktop Target** (`net9.0-desktop`):
- Skia rendering engine
- Cross-platform file I/O
- Platform-agnostic UI implementation

### Supporting Libraries

- **MVVM**: CommunityToolkit.Mvvm 8.4.0
- **Logging**: NLog, elmah.io (optional telemetry)
- **AI Integration**: Microsoft.SemanticKernel 1.41.0
- **Environment Variables**: dotenv.net
- **Secret Management**: Doppler integration

---

## UNO Platform Architecture

### What is UNO Platform?

UNO Platform enables StoryCAD to run on multiple operating systems from a single codebase. It provides:

1. **Single Project Structure**: One `.csproj` file manages all platform targets
2. **Platform Heads**: Different execution models for different platforms
3. **Shared Code**: Views, ViewModels, and Services work across all platforms
4. **Native Performance**: WinUI on Windows, Skia on other platforms
5. **Conditional Compilation**: Platform-specific code separated via preprocessor directives

### Platform Heads

#### WinAppSDK Head (Windows)

**Target Framework**: `net9.0-windows10.0.22621`

**Characteristics**:
- Native WinUI 3 rendering
- Full Windows App SDK features
- MSIX packaging for Microsoft Store
- File association support (`.stbx` files)
- Windows-specific APIs available
- Package.Current API for packaged apps
- Windows App Lifecycle for file activation

**Configuration** (from `.csproj`):
```xml
<TargetFrameworks Condition="'$(OS)'=='Windows_NT'">
    net9.0-windows10.0.22621;net9.0-desktop
</TargetFrameworks>
<UseWinUI Condition="'$(TPIdentifier)' == 'windows'">true</UseWinUI>
<EnableMsixTooling>true</EnableMsixTooling>
<WindowsPackageType>MSIX</WindowsPackageType>
```

#### Desktop Head (macOS/Linux)

**Target Framework**: `net9.0-desktop`

**Characteristics**:
- Skia-based rendering engine
- Cross-platform UI rendering
- No Windows-specific APIs
- Native macOS application bundle
- GTK backend on Linux (potential)
- Cross-platform file I/O

**Configuration** (from `.csproj`):
```xml
<TargetFrameworks Condition="'$(OS)'!='Windows_NT'">
    net9.0-desktop
</TargetFrameworks>
<UnoFeatures>
    SkiaRenderer;
    Lottie;
    Toolkit;
</UnoFeatures>
```

### UNO Single Project Structure

All three projects use `<UnoSingleProject>true</UnoSingleProject>`, which provides:

- **Unified Project File**: Single `.csproj` handles all platforms
- **Conditional Compilation**: Platform-specific code via `#if` directives
- **Shared XAML**: Views work across all platforms
- **Platform-Specific Files**: MSBuild conditions exclude files per platform
- **Simplified Build**: Single build command for all targets

### Multi-Targeting Strategy

**On Windows Build Machine**:
```xml
<PropertyGroup Condition="'$(OS)'=='Windows_NT'">
    <TargetFrameworks>net9.0-desktop;net9.0-windows10.0.22621</TargetFrameworks>
</PropertyGroup>
```
- Produces **two assemblies**: one for Desktop (cross-platform), one for Windows (WinAppSDK)
- Allows testing both platforms from Windows
- StoryCADLib has both capabilities available

**On macOS/Linux Build Machine**:
```xml
<PropertyGroup Condition="'$(OS)'!='Windows_NT'">
    <TargetFrameworks>net9.0-desktop</TargetFrameworks>
</PropertyGroup>
```
- Produces **one assembly**: Desktop target only
- Cannot build Windows-specific target on non-Windows OS

### Platform-Specific Code Patterns

#### 1. Preprocessor Directives

Used to exclude code at compile time:

```csharp
#if WINDOWS10_0_18362_0_OR_GREATER
// Windows-specific file activation
var activationArgs = Microsoft.Windows.AppLifecycle.AppInstance
    .GetCurrent().GetActivatedEventArgs();
#endif
```

#### 2. Runtime Platform Detection

Used when feature might be optionally available:

```csharp
var isPackaged = false;
try
{
    var package = Package.Current;  // Windows-specific API
    isPackaged = package != null;
}
catch
{
    // Non-Windows platform or unpackaged
}
```

#### 3. Platform-Specific File Exclusion

Via MSBuild conditions in `.csproj`:

```xml
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) != 'windows'">
    <Compile Remove="ViewModels\Tools\PrintReportDialogVM.WinAppSDK.cs" />
</ItemGroup>
```

#### 4. Platform Logging Configuration

Different logging providers per platform:

```csharp
#if __WASM__
builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__ || __MACCATALYST__
builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#else
builder.AddConsole();
#endif
```

### UNO Platform Benefits

1. **Code Reuse**: 95%+ shared code between platforms
2. **Native Performance**: Platform-specific rendering where available
3. **Single Maintenance**: One codebase, multiple platforms
4. **Gradual Migration**: Can maintain Windows-first development while adding cross-platform
5. **Future Expansion**: WebAssembly, iOS, Android possible with minimal changes

---

## Project Structure

### Solution Components

1. **StoryCAD** - Main UNO application (executable)
2. **StoryCADLib** - Core business logic library (cross-platform NuGet package)
3. **StoryCADTests** - MSTest test project (multi-target)

### StoryCAD Project (Main Application)

**Location**: `/mnt/d/dev/src/StoryCAD/StoryCAD/`
**Output Type**: Executable (WinExe)
**SDK**: Uno.Sdk

**Project Structure**:
```
StoryCAD/
├── StoryCAD.csproj (UNO Single Project, Exe)
├── App.xaml / App.xaml.cs (Application entry point)
├── Package.appxmanifest (Windows-only, conditional)
├── /Views/
│   ├── Shell.xaml (Main application shell)
│   ├── HomePage.xaml
│   ├── OverviewPage.xaml
│   ├── ProblemPage.xaml
│   ├── CharacterPage.xaml
│   ├── ScenePage.xaml
│   ├── FolderPage.xaml
│   ├── SettingPage.xaml
│   ├── TrashCanPage.xaml
│   ├── WebPage.xaml
│   └── PreferencesInitialization.xaml
├── /Assets/ (MSIX assets - Windows only)
│   ├── Icons/
│   ├── Splash screens/
│   └── Logos/
└── /Properties/
    └── launchSettings.json
```

**Target Frameworks**:
- Windows: `net9.0-windows10.0.22621` (WinAppSDK head)
- macOS/Linux: `net9.0-desktop` (Desktop head)

**Platform-Specific Features**:
- MSIX packaging (Windows only)
- File associations for `.stbx` (Windows only)
- WindowsAppSDKSelfContained deployment (Windows only)

### StoryCADLib Project (Core Library)

**Location**: `/mnt/d/dev/src/StoryCAD/StoryCADLib/`
**Output Type**: Library
**SDK**: Uno.Sdk

**Project Structure**:
```
StoryCADLib/
├── StoryCADLib.csproj (UNO Single Project Library)
│
├── /Models/ (Data models)
│   ├── StoryModel.cs (Root model)
│   ├── StoryDocument.cs (Model + FilePath wrapper)
│   ├── StoryElement.cs (Base class)
│   ├── CharacterModel.cs
│   ├── SceneModel.cs
│   ├── ProblemModel.cs
│   ├── SettingModel.cs
│   ├── FolderModel.cs
│   ├── WebModel.cs
│   ├── TrashCanModel.cs
│   ├── NoteModel.cs
│   ├── RelationshipModel.cs
│   ├── StoryNodeItem.cs (TreeView node)
│   ├── StoryElementCollection.cs
│   └── /Tools/
│       └── ToolsData.cs
│
├── /ViewModels/ (MVVM ViewModels)
│   ├── ShellViewModel.cs
│   ├── OutlineViewModel.cs
│   ├── OverviewViewModel.cs
│   ├── ProblemViewModel.cs
│   ├── CharacterViewModel.cs
│   ├── SceneViewModel.cs
│   ├── FolderViewModel.cs
│   ├── SettingViewModel.cs
│   ├── TrashCanViewModel.cs
│   ├── WebViewModel.cs
│   ├── NoteViewModel.cs
│   ├── RelationshipViewModel.cs
│   └── /Tools/
│       ├── PrintReportDialogVM.cs
│       └── PrintReportDialogVM.WinAppSDK.cs (Windows-specific)
│
├── /Views/ (XAML views - shared with StoryCAD)
│   ├── Controls/
│   └── Dialogs/
│
├── /Services/
│   ├── OutlineService.cs (Core service, stateless)
│   ├── EditFlushService.cs (ISaveable pattern)
│   ├── NavigationService.cs
│   ├── AutoSaveService.cs
│   ├── BackupService.cs
│   ├── CollaboratorService.cs
│   ├── SearchService.cs
│   ├── PreferenceService.cs
│   ├── AppState.cs (Central state management)
│   ├── ISaveable.cs (Interface for ViewModels)
│   ├── SerializationLock.cs (Thread safety)
│   ├── /Backend/
│   │   └── BackendService.cs
│   ├── /Json/
│   │   ├── ControlIO.cs
│   │   ├── ListsIO.cs
│   │   └── ToolsIO.cs
│   ├── /Logging/
│   │   ├── LogService.cs
│   │   └── NLog.config
│   ├── /Navigation/
│   │   └── NavigationService.cs
│   ├── /IoC/
│   │   ├── BootStrapper.cs
│   │   └── Ioc.cs (CommunityToolkit IoC container)
│   └── /Dialogs/
│       ├── KeyQuestionsDialog.xaml
│       ├── MasterPlotsDialog.xaml
│       ├── DramaticSituationsDialog.xaml
│       ├── TopicsDialog.xaml
│       ├── StockScenesDialog.xaml
│       ├── FeedbackDialog.xaml
│       ├── ElementPicker.xaml
│       ├── FileOpenMenu.xaml
│       ├── SaveAsDialog.xaml
│       ├── BackupNow.xaml
│       ├── NewRelationshipPage.xaml
│       └── HelpPage.xaml
│
├── /Controls/ (Reusable UI controls)
│   ├── Flaw.xaml
│   ├── Traits.xaml
│   ├── Conflict.xaml
│   └── RelationshipView.xaml
│
├── /Collaborator/ (AI features)
│   ├── /Views/
│   │   ├── WelcomePage.xaml
│   │   ├── WorkflowShell.xaml
│   │   └── WorkflowPage.xaml
│   └── /ViewModels/
│       └── CollaboratorViewModel.cs
│
├── /API/ (External integration layer)
│   ├── IStoryCADAPI.cs
│   ├── SemanticKernelAPI.cs
│   └── OperationResult.cs
│
├── /DAL/ (Data Access Layer)
│   ├── StoryIO.cs (File I/O)
│   ├── PreferencesIo.cs
│   └── /Doppler/
│       └── DopplerService.cs (Secret management)
│
└── /Assets/
    └── /Install/
        ├── /samples/ (Story templates)
        │   ├── Blank.stbx
        │   ├── MasterPlots.stbx
        │   └── Three Act.stbx
        ├── /reports/ (Report templates)
        │   └── [Various report templates]
        ├── Controls.json
        ├── Lists.json
        ├── Tools.json
        └── Symbols.txt
```

**Multi-Targeting Configuration**:
- Windows: `net9.0-desktop` + `net9.0-windows10.0.22621`
- macOS/Linux: `net9.0-desktop` only
- Platform-specific files excluded via MSBuild conditions

**Platform-Specific Files**:
- `PrintReportDialogVM.WinAppSDK.cs` - Excluded on non-Windows platforms

### StoryCADTests Project

**Location**: `/mnt/d/dev/src/StoryCAD/StoryCADTests/`
**Output Type**: Executable (Test App)
**SDK**: Uno.Sdk

**Project Structure**:
```
StoryCADTests/
├── StoryCADTests.csproj (UNO Single Project Test Exe)
├── app.manifest (Windows app manifest)
├── Package.appxmanifest (Windows-only)
├── GlobalUsings.cs
│
├── /TestInputs/ (Test data - copied to output)
│   ├── AddElement.stbx
│   ├── CloseFileTest.stbx
│   ├── Full.stbx
│   ├── OpenFromDesktopTest.stbx
│   ├── OpenTest.stbx
│   └── StructureTests.stbx
│
├── /Assets/ (Windows-only test app assets)
│   └── [MSIX icons and logos]
│
├── /UnitTests/
│   ├── OutlineServiceTests.cs (Service layer tests)
│   ├── SemanticKernelAPITests.cs (API layer tests)
│   ├── StoryModelTests.cs
│   ├── CharacterModelTests.cs
│   ├── RelationshipTests.cs
│   ├── SearchServiceTests.cs
│   └── [Other test files]
│
├── /IntegrationTests/
│   └── FileIOTests.cs
│
└── .env (Environment variables for testing)
```

**Target Frameworks**:
- Windows: `net9.0-windows10.0.22621` (for UI tests) + `net9.0-desktop`
- macOS/Linux: `net9.0-desktop` only

**Test Execution**:
- Uses `EnableMSTestRunner` for modern test execution
- Supports `[TestMethod]` (cross-platform)
- Supports `[UITestMethod]` (Windows only, requires WinUI)

### Solution-Level Configuration Files

```
/mnt/d/dev/src/StoryCAD/
├── StoryCAD.sln
├── global.json (UNO SDK version: 6.2.36)
├── Directory.Packages.props (Central package management)
├── Directory.Build.props (Solution-wide build config)
└── /devdocs/
    └── [Architecture documentation]
```

---

## Architectural Layers

### Complete Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                      UNO PLATFORM LAYER                             │
│  ┌───────────────────────┐         ┌──────────────────────┐        │
│  │  WinAppSDK Head       │         │  Desktop Head        │        │
│  │  (Windows)            │         │  (macOS/Linux)       │        │
│  │  - Native WinUI 3     │         │  - Skia Rendering    │        │
│  │  - MSIX Packaging     │         │  - Cross-platform    │        │
│  │  - File Association   │         │  - No Windows APIs   │        │
│  └───────────┬───────────┘         └──────────┬───────────┘        │
│              └──────────────┬────────────────────┘                  │
└────────────────────────────┼──────────────────────────────────────┘
                             │
┌────────────────────────────┼──────────────────────────────────────┐
│                     PRESENTATION LAYER                             │
│                            │                                        │
│  ┌─────────────────────────┴────────────────────┐                 │
│  │           Shell.xaml                         │                 │
│  │  - Main Window                               │                 │
│  │  - Navigation Host                           │                 │
│  │  - TreeView (ExplorerView/NarratorView)     │                 │
│  │  - TrashView (Separate TreeView)            │                 │
│  └─────────────────────┬────────────────────────┘                 │
│                        │                                           │
│  ┌─────────────────────┴────────────────────┐                     │
│  │         Views (XAML Pages)               │                     │
│  │  - HomePage.xaml                         │                     │
│  │  - OverviewPage.xaml                     │                     │
│  │  - ProblemPage.xaml                      │                     │
│  │  - CharacterPage.xaml                    │                     │
│  │  - ScenePage.xaml                        │                     │
│  │  - FolderPage.xaml                       │                     │
│  │  - SettingPage.xaml                      │                     │
│  │  - TrashCanPage.xaml                     │                     │
│  │  - WebPage.xaml                          │                     │
│  └─────────────────────┬────────────────────┘                     │
│                        │                                           │
│  ┌─────────────────────┴────────────────────┐                     │
│  │   ViewModels (MVVM Pattern)              │                     │
│  │                                           │                     │
│  │  ShellViewModel                           │                     │
│  │  ├─ Tree operations                       │                     │
│  │  ├─ Selection tracking                    │                     │
│  │  ├─ Context menu commands                 │                     │
│  │  └─ Calls OutlineService                  │                     │
│  │                                           │                     │
│  │  OutlineViewModel                         │                     │
│  │  ├─ File operations (new, open, save)    │                     │
│  │  ├─ Delegates to OutlineService           │                     │
│  │  └─ Uses AppState.CurrentDocument         │                     │
│  │                                           │                     │
│  │  Element ViewModels                       │                     │
│  │  ├─ Implement ISaveable                   │                     │
│  │  ├─ Data binding to models                │                     │
│  │  └─ Element-specific logic                │                     │
│  │     - OverviewViewModel                   │                     │
│  │     - CharacterViewModel                  │                     │
│  │     - SceneViewModel                      │                     │
│  │     - ProblemViewModel, etc.              │                     │
│  └─────────────────────┬────────────────────┘                     │
└────────────────────────┼──────────────────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────────────────┐
│                      SERVICE LAYER                                 │
│                                                                    │
│  ┌────────────────────────────────────────────────────────┐      │
│  │               AppState (Central State)                 │      │
│  │  - CurrentDocument: StoryDocument                      │      │
│  │  - CurrentSaveable: ISaveable                          │      │
│  │  - CurrentDocumentChanged event                        │      │
│  └─────────────────────────┬──────────────────────────────┘      │
│                            │                                       │
│  ┌─────────────────────────┴──────────────────────────────┐      │
│  │         OutlineService (Stateless)                     │      │
│  │  - Receives StoryModel as parameter                    │      │
│  │  - CRUD operations on story elements                   │      │
│  │  - Tree manipulation                                   │      │
│  │  - Calls StoryIO for persistence                       │      │
│  │  - Thread-safe via SerializationLock                   │      │
│  └─────────────────────────┬──────────────────────────────┘      │
│                            │                                       │
│  ┌────────────────────────┬┴────────────────────────────┐        │
│  │                        │                             │        │
│  ▼                        ▼                             ▼        │
│  EditFlushService    NavigationService        AutoSaveService    │
│  - ISaveable pattern - Page transitions       - Uses AppState    │
│                                                - Calls EditFlush  │
│                                                                    │
│  BackupService      CollaboratorService       SearchService       │
│  - File backups     - AI assistance           - Full-text search  │
│                                                                    │
│  PreferenceService  LogService (NLog/elmah.io)                    │
│  - User settings    - Logging/telemetry                           │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────────────┐
│                      API LAYER (External Integration)              │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  SemanticKernelAPI (IStoryCADAPI)                       │     │
│  │  - Implements IStoryCADAPI interface                    │     │
│  │  - All operations return OperationResult<T>             │     │
│  │  - Wraps OutlineService calls in try-catch              │     │
│  │  - Thread-safe with SerializationLock                   │     │
│  │  - Never throws exceptions to external callers          │     │
│  └─────────────────────────┬───────────────────────────────┘     │
│                            │                                       │
│  ┌─────────────────────────┴───────────────────────────────┐     │
│  │         External Consumers                              │     │
│  │  - Large Language Models (ChatGPT, Claude)              │     │
│  │  - External Tools                                       │     │
│  │  - CLI Applications                                     │     │
│  │  - Third-party Integrations                             │     │
│  └─────────────────────────────────────────────────────────┘     │
└────────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────────────┐
│                      DATA LAYER                                    │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  StoryDocument (Atomic Wrapper)                         │     │
│  │  ├── StoryModel (In-memory representation)              │     │
│  │  └── FilePath (File location)                           │     │
│  └─────────────────────────┬───────────────────────────────┘     │
│                            │                                       │
│  ┌─────────────────────────┴───────────────────────────────┐     │
│  │  StoryModel                                             │     │
│  │  ├── ExplorerView: StoryNodeItem (root)                │     │
│  │  ├── NarratorView: StoryNodeItem (root)                │     │
│  │  ├── TrashCan: StoryNodeItem (separate tree root)      │     │
│  │  ├── StoryElements: ObservableCollection<StoryElement> │     │
│  │  ├── StoryElementGuids: Dictionary<Guid, StoryElement> │     │
│  │  └── Changed: bool (unsaved changes flag)              │     │
│  └─────────────────────────┬───────────────────────────────┘     │
│                            │                                       │
│  ┌─────────────────────────┴───────────────────────────────┐     │
│  │  StoryElement (Base Class)                              │     │
│  │  ├── Uuid: Guid (unique identifier)                     │     │
│  │  ├── Name: string                                       │     │
│  │  ├── Type: StoryItemType                                │     │
│  │  ├── Parent: Guid                                       │     │
│  │  ├── Children: List<Guid>                               │     │
│  │  └── Node: StoryNodeItem (UI representation)            │     │
│  └─────────────────────────┬───────────────────────────────┘     │
│                            │                                       │
│  ┌─────────────────────────┴───────────────────────────────┐     │
│  │  Derived Model Types                                    │     │
│  │  ├── CharacterModel                                     │     │
│  │  │   └── RelationshipCollection                         │     │
│  │  ├── SceneModel                                         │     │
│  │  │   └── CastMembers (character references)            │     │
│  │  ├── ProblemModel                                       │     │
│  │  ├── SettingModel                                       │     │
│  │  ├── FolderModel                                        │     │
│  │  ├── WebModel                                           │     │
│  │  ├── NoteModel                                          │     │
│  │  └── TrashCanModel                                      │     │
│  └─────────────────────────┬───────────────────────────────┘     │
│                            │                                       │
│  ┌─────────────────────────┴───────────────────────────────┐     │
│  │  Data Access Layer (DAL)                                │     │
│  │  ├── StoryIO (File I/O, XML serialization)             │     │
│  │  │   ├── ReadModel(filePath): StoryDocument            │     │
│  │  │   └── WriteModel(document): void                    │     │
│  │  ├── PreferencesIo (User settings persistence)         │     │
│  │  └── Doppler (Secret management for API keys)          │     │
│  └─────────────────────────────────────────────────────────┘     │
└────────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                          │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  IoC Container (CommunityToolkit.Mvvm.Ioc)              │     │
│  │  ├── BootStrapper.Initialise()                          │     │
│  │  ├── Service registration                               │     │
│  │  └── Dependency injection                               │     │
│  └─────────────────────────────────────────────────────────┘     │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  Cross-Cutting Concerns                                 │     │
│  │  ├── Logging (NLog, elmah.io)                           │     │
│  │  ├── SerializationLock (Thread safety)                  │     │
│  │  ├── Error handling                                     │     │
│  │  └── Windowing service                                  │     │
│  └─────────────────────────────────────────────────────────┘     │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  File System                                            │     │
│  │  └── .stbx files (JSON-based story format)              │     │
│  └─────────────────────────────────────────────────────────┘     │
└────────────────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

#### 1. Data Model Layer

**Purpose**: In-memory representation of story data

**Key Classes**:
- **StoryDocument**: Wrapper class containing StoryModel and FilePath (Issue #1100)
  - Provides atomic document operations
  - Single source of truth for document state
  - Prevents mismatched model/path states

- **StoryModel**: Root of the story data structure
  - Contains ExplorerView and NarratorView trees
  - Maintains StoryElements collection
  - StoryElementGuids dictionary for O(1) lookups
  - Changed flag for unsaved modifications

- **StoryElement**: Base class for all story components
  - Derived types: CharacterModel, SceneModel, ProblemModel, SettingModel, FolderModel, WebModel, NoteModel, TrashCanModel
  - GUID-based references (not string names)
  - Parent-child relationships via Guid references

- **StoryNodeItem**: Tree node representation for UI display
  - Synchronized with StoryElement.Name
  - Expansion state tracking
  - UI-specific properties (background colors, icons)

- **StoryElementCollection**: ObservableCollection with automatic indexing
  - Automatic updates to StoryElementGuids dictionary
  - INotifyCollectionChanged for UI binding

#### 2. Service Layer

**Purpose**: Business logic and application services

**Core Services**:

- **OutlineService**: Central service for story structure management
  - **Stateless design** - receives StoryModel as parameter
  - CRUD operations on story elements
  - Tree manipulation (add, remove, move, copy)
  - Element conversion (Problem ↔ Scene)
  - Thread-safe via SerializationLock
  - Calls StoryIO for file I/O

- **AppState**: Central state management (Issue #1100)
  - CurrentDocument: StoryDocument (model + file path)
  - CurrentSaveable: ISaveable (active ViewModel)
  - CurrentDocumentChanged event for UI updates
  - Single source of truth replacing ViewModel-held state

- **EditFlushService**: Handles saving current page edits (Issue #1100)
  - Uses ISaveable interface to flush ViewModel changes
  - Called before save operations
  - Eliminates ViewModel dependencies in services

- **NavigationService**: Page navigation and view transitions
  - Frame-based navigation
  - Parameter passing between pages

- **AutoSaveService**: Automatic saving with configurable intervals
  - Uses AppState.CurrentDocument (not OutlineViewModel)
  - Calls EditFlushService before saving
  - Asynchronous operation

- **BackupService**: Automated backup functionality
  - Uses AppState.CurrentDocument.FilePath
  - Configurable backup location and retention

- **CollaboratorService**: AI-powered writing assistance
  - Integrates with Microsoft.SemanticKernel
  - Workflow-based AI interactions

- **SearchService**: Full-text search across story content
  - Case-insensitive search
  - Returns formatted results with context

- **LogService**: Comprehensive logging
  - NLog for file logging
  - elmah.io for optional telemetry (opt-in)
  - Cross-cutting logging for all layers

**Service Call Patterns**:

```
ViewModels → OutlineService
  - Direct method calls
  - Can throw and handle exceptions
  - No OperationResult wrapper

API → OutlineService
  - Wrapped in try-catch
  - Returns OperationResult<T>
  - Never throws to external callers

Services → Services
  - AutoSaveService → EditFlushService → OutlineService → StoryIO
  - BackupService → AppState
  - All Services → LogService
```

#### 3. API Layer

**Purpose**: External integration interface

**Key Components**:

- **IStoryCADAPI**: Interface for external consumption
  - All methods return OperationResult<T>
  - Versioned for compatibility

- **SemanticKernelAPI**: Implementation of IStoryCADAPI
  - Calls OutlineService directly
  - All operations wrapped in try-catch
  - Thread-safe with SerializationLock
  - Never allows exceptions to escape
  - Designed for LLM and external tool consumption

- **OperationResult<T>**: Safe result wrapper
  ```csharp
  public class OperationResult<T>
  {
      public bool IsSuccess { get; set; }
      public string ErrorMessage { get; set; }
      public T Payload { get; set; }
  }
  ```

**Why OperationResult for API but not ViewModels?**
- **API**: External callers (LLMs, tools) need safe, predictable interface
  - Can't assume error handling capability
  - Need structured error information
  - Must prevent application crashes from external code

- **ViewModels**: Internal application code
  - Can handle exceptions appropriately
  - Has access to UI for error display
  - Part of trusted codebase

#### 4. ViewModel Layer

**Purpose**: MVVM presentation logic

**Key ViewModels**:

- **ShellViewModel**: Main application shell
  - Tree operations (add, remove, move, copy)
  - Selection tracking (last-clicked pattern)
  - Context menu command implementation
  - Calls OutlineService for business logic
  - No longer has SaveModel() (moved to OutlineViewModel)

- **OutlineViewModel**: Story outline management
  - File operations (new, open, save, save as, close)
  - Delegates all business logic to OutlineService
  - Uses AppState.CurrentDocument (not local state)
  - Direct exception handling (no OperationResult)

- **Element ViewModels**: Element-specific UI logic
  - Implement **ISaveable** interface (Issue #1100)
  - Data binding to element models
  - Element-specific validation
  - Types: OverviewViewModel, CharacterViewModel, SceneViewModel, ProblemViewModel, FolderViewModel, SettingViewModel, WebViewModel, TrashCanViewModel

**ISaveable Pattern** (Issue #1100):
```csharp
public interface ISaveable
{
    void SaveModel(); // Flush ViewModel data to Model
}
```
- Pages register saveable ViewModels in OnNavigatedTo
- EditFlushService calls SaveModel() without knowing ViewModel types
- Eliminates type-specific switch statements

#### 5. View Layer

**Purpose**: XAML-based user interface

**Key Views**:

- **Shell.xaml**: Main application window
  - Two TreeViews: Explorer/Narrator (left), Trash (right)
  - Navigation frame for content pages
  - Menu bar and toolbar
  - Status bar

- **Content Pages**: Element editing views
  - HomePage.xaml
  - OverviewPage.xaml
  - ProblemPage.xaml
  - CharacterPage.xaml
  - ScenePage.xaml
  - FolderPage.xaml
  - SettingPage.xaml
  - WebPage.xaml
  - TrashCanPage.xaml

- **Dialogs**: Reusable tool dialogs
  - KeyQuestionsDialog
  - MasterPlotsDialog
  - DramaticSituationsDialog
  - TopicsDialog
  - StockScenesDialog
  - FeedbackDialog
  - ElementPicker
  - SaveAsDialog

- **Controls**: Reusable UI components
  - Flaw.xaml (character flaws)
  - Traits.xaml (character traits)
  - Conflict.xaml (problem conflicts)
  - RelationshipView.xaml (character relationships)

#### 6. Data Access Layer (DAL)

**Purpose**: File I/O and persistence

**Key Components**:

- **StoryIO**: File operations
  - ReadModel(filePath): Load .stbx files
  - WriteModel(document): Save .stbx files
  - JSON serialization (converted from XML)
  - Backward compatibility with legacy XML formats
  - Auto-migration of old XML structures to JSON

- **PreferencesIo**: User settings
  - Platform-specific storage locations
  - JSON-based preferences

- **Doppler**: Secret management
  - API key storage
  - Secure credential handling

#### 7. Infrastructure Layer

**Purpose**: Cross-cutting concerns

**Key Components**:

- **BootStrapper**: IoC container initialization
  - Service registration
  - Dependency injection setup
  - Headless mode support

- **SerializationLock**: Thread safety
  ```csharp
  using (var serializationLock = new SerializationLock(autoSaveService, backupService, _logger))
  {
      // Thread-safe operation
  }
  ```
  - Ensures serial reusability
  - Prevents concurrent modifications
  - Used throughout OutlineService and API

- **LogService**: Cross-cutting logging
  - NLog file logging
  - elmah.io telemetry (opt-in)
  - Structured logging

---

## Cross-Platform Considerations

### Platform-Specific Features

#### Windows-Only Features

1. **MSIX Packaging**
   - Microsoft Store deployment
   - Automatic updates
   - Sandboxed environment

2. **File Association**
   - `.stbx` files open in StoryCAD
   - Handled via Package.appxmanifest

3. **Windows App Lifecycle**
   - File activation from Explorer
   - Protocol activation

4. **Printing**
   - Native Windows printing APIs
   - `PrintReportDialogVM.WinAppSDK.cs` (Windows-specific)

5. **Package.Current API**
   - Installed location detection
   - Package version info

#### Cross-Platform Features

1. **File I/O**
   - Standard .NET file operations
   - Works on all platforms

2. **Story Editing**
   - All core editing features available
   - MVVM works identically across platforms

3. **AI Integration**
   - SemanticKernel works cross-platform
   - Collaborator features available everywhere

4. **Preferences**
   - Platform-specific storage locations
   - JSON-based, portable format

### Platform Detection Strategies

#### Build-Time Detection (MSBuild)

Used in `.csproj` files:

```xml
<!-- Exclude Windows-specific files on non-Windows -->
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) != 'windows'">
    <Compile Remove="ViewModels\Tools\PrintReportDialogVM.WinAppSDK.cs" />
</ItemGroup>
```

#### Compile-Time Detection (Preprocessor)

Used in C# code:

```csharp
#if WINDOWS10_0_18362_0_OR_GREATER
// Windows-specific code here
var activationArgs = Microsoft.Windows.AppLifecycle.AppInstance
    .GetCurrent().GetActivatedEventArgs();
#endif
```

#### Runtime Detection

Used when feature might be optionally available:

```csharp
var isPackaged = false;
try
{
    var package = Package.Current;  // Windows-specific API
    isPackaged = package != null;
}
catch
{
    // Non-Windows platform or unpackaged app
}
```

### User Experience Differences

#### Windows

- MSIX-packaged app
- File associations work
- Printing available
- Windows menu conventions
- Ctrl keyboard shortcuts

#### macOS

- Native .app bundle
- No file associations (yet)
- Printing unavailable (graceful degradation)
- macOS menu bar integration
- Cmd keyboard shortcuts

### Graceful Degradation

When features are unavailable:

1. **UI Elements Hidden**: Windows-only menu items hidden on macOS
2. **Error Messages**: Clear communication when feature unavailable
3. **Alternative Workflows**: Offer cross-platform alternatives where possible

---

## Build and Deployment

### Building for Windows

**Prerequisites**:
- Windows 10/11 build machine
- Visual Studio 2022 or later
- .NET 9 SDK
- Windows App SDK

**Build Command**:
```bash
dotnet build -c Release -f net9.0-windows10.0.22621
```

**Output**:
- MSIX package for Microsoft Store
- Self-contained deployment option
- WinUI 3 native rendering

**Packaging**:
- Configured via Package.appxmanifest
- Code signing required for distribution
- Microsoft Store submission

### Building for macOS

**Prerequisites**:
- macOS build machine (Apple Silicon or Intel)
- .NET 9 SDK
- UNO Platform tooling

**Build Command**:
```bash
dotnet build -c Release -f net9.0-desktop
```

**Output**:
- Native .app bundle
- Skia rendering
- macOS-signed application (for distribution)

**Packaging**:
- Code signing for distribution
- Notarization for Gatekeeper
- DMG or PKG installer

### Building for Linux (Future)

**Prerequisites**:
- Linux build machine
- .NET 9 SDK
- GTK dependencies

**Build Command**:
```bash
dotnet build -c Release -f net9.0-desktop
```

**Output**:
- Linux executable
- Skia rendering with GTK backend
- AppImage or Flatpak packaging

### Multi-Target Build

On Windows, to build both targets:

```bash
dotnet build -c Release
```

This produces both:
- `net9.0-windows10.0.22621` (WinAppSDK head)
- `net9.0-desktop` (Desktop head)

### CI/CD Considerations

**Windows Pipeline**:
1. Build both targets (`net9.0-windows10.0.22621` and `net9.0-desktop`)
2. Run tests on both frameworks
3. Package MSIX for Windows
4. Sign and publish to Microsoft Store

**macOS Pipeline**:
1. Build `net9.0-desktop` only
2. Run tests on Desktop framework
3. Create .app bundle
4. Sign and notarize
5. Create DMG installer

**Testing Strategy**:
- Run unit tests on all target frameworks
- Run UI tests on Windows only (`[UITestMethod]` requires WinUI)
- Cross-platform integration testing

---

## Dependency Management

### Central Package Management

StoryCAD uses **Central Package Management** via `Directory.Packages.props`:

```xml
<Project ToolsVersion="15.0">
    <PropertyGroup>
        <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    </PropertyGroup>
    <ItemGroup>
        <PackageVersion Include="Microsoft.SemanticKernel" Version="1.41.0" />
        <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.0" />
        <PackageVersion Include="NLog" Version="5.3.4" />
        <!-- All package versions centralized -->
    </ItemGroup>
</Project>
```

**Benefits**:
- Single source of truth for package versions
- Consistent versions across all projects
- Easier dependency upgrades
- Prevents version conflicts
- Simplified `.csproj` files

### UNO SDK Implicit Packages

From Directory.Packages.props:
```xml
<!--
  To update the version of Uno, you should instead update the Sdk version
  in the global.json file.

  See https://aka.platform.uno/using-uno-sdk for more information.
  See https://aka.platform.uno/using-uno-sdk#implicit-packages for more
  information regarding the Implicit Packages.
-->
```

**What this means**:
- UNO SDK automatically includes platform-specific packages
- WinUI packages, Skia packages automatically managed
- `global.json` controls UNO version (currently 6.2.36)
- Reduces manual package reference management
- Platform-specific dependencies added automatically

### Version Control

**global.json**:
```json
{
  "sdk": {
    "version": "9.0.0",
    "rollForward": "latestMinor"
  },
  "msbuild-sdks": {
    "Uno.Sdk": "6.2.36"
  }
}
```

To update UNO Platform:
1. Update `Uno.Sdk` version in `global.json`
2. All three projects automatically use new version
3. No changes to individual `.csproj` files needed

### Major Dependencies

**Core Frameworks**:
- UNO Platform SDK: 6.2.36
- .NET: 9.0
- Windows App SDK: 1.6 (Windows only)

**Libraries**:
- CommunityToolkit.Mvvm: 8.4.0 (MVVM framework)
- Microsoft.SemanticKernel: 1.41.0 (AI integration)
- NLog: 5.3.4 (logging)
- dotenv.net: 3.2.1 (environment variables)

**Testing**:
- MSTest: Latest stable
- MSTest.Sdk: Latest stable

---

## Key Architectural Patterns

### 1. MVVM (Model-View-ViewModel)

**Implementation**:
- Strict separation between UI and business logic
- Data binding using CommunityToolkit.Mvvm
- INotifyPropertyChanged for property change notifications
- RelayCommand for command binding

**Example**:
```csharp
[ObservableObject]
public partial class CharacterViewModel : ISaveable
{
    [ObservableProperty]
    private string _name;

    [RelayCommand]
    private async Task SaveCharacter()
    {
        // Save logic
    }

    public void SaveModel()
    {
        // Flush ViewModel to Model
        _model.Name = Name;
    }
}
```

### 2. Dependency Injection

**Implementation**:
- Uses `Ioc.Default` from CommunityToolkit.Mvvm
- Services registered at startup via BootStrapper
- Consumption doesn't use true DI - ViewModels call Ioc.Default.GetService<T>() directly
- The constructors of almost all ServiceCollection entries do use DI

**Registration** (BootStrapper.cs):
```csharp
public static void Initialise(bool forUI = true)
{
    Ioc.Default.ConfigureServices(
        new ServiceCollection()
            .AddSingleton<AppState>()
            .AddSingleton<OutlineService>()
            .AddSingleton<NavigationService>()
            .AddSingleton<AutoSaveService>()
            .AddSingleton<BackupService>()
            .AddSingleton<LogService>()
            // ... other services
            .BuildServiceProvider()
    );
}
```

**Consumption**:
```csharp
public class CharacterViewModel
{
    private readonly OutlineService _outlineService;

    public CharacterViewModel()
    {
        _outlineService = Ioc.Default.GetService<OutlineService>();
    }
}
```

### 3. Thread Safety Pattern (SerializationLock)

**Purpose**: Ensures serial reusability - one operation completes before another starts

**Implementation**:
```csharp
using (var serializationLock = new SerializationLock(autoSaveService, backupService, _logger))
{
    // Thread-safe operation
    // Auto-save and backup are paused during this block
}
```

**Used in**:
- OutlineService (all state-modifying operations)
- SemanticKernelAPI (all API operations)
- StoryIO (file read/write operations)

**Prevents**:
- Concurrent modifications to story data
- File corruption from simultaneous writes
- Race conditions between auto-save and manual operations

### 4. ISaveable Pattern (Issue #1100)

**Purpose**: Flush ViewModel changes to Model without type-specific knowledge

**Interface**:
```csharp
public interface ISaveable
{
    void SaveModel(); // Flush ViewModel data to Model
}
```

**Implementation** (in ViewModels):
```csharp
public class CharacterViewModel : ISaveable
{
    public void SaveModel()
    {
        // Transfer ViewModel properties to Model
        _model.Name = Name;
        _model.Age = Age;
        _model.Role = Role;
        // etc.
    }
}
```

**Usage** (in Pages):
```csharp
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    _appState.CurrentSaveable = ViewModel; // Register this page's ViewModel
}

protected override void OnNavigatedFrom(NavigationEventArgs e)
{
    _appState.CurrentSaveable = null; // Unregister
    base.OnNavigatedFrom(e);
}
```

**Called by EditFlushService**:
```csharp
public void FlushCurrentEdit()
{
    _appState.CurrentSaveable?.SaveModel();
}
```

**Benefits**:
- No ViewModel dependencies in services
- No type-specific switch statements
- ViewModels self-register in pages
- Clean separation of concerns

### 5. OperationResult Pattern

**Purpose**: Safe error handling for API operations

**Implementation**:
```csharp
public class OperationResult<T>
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public T Payload { get; set; }
}
```

**Usage in API**:
```csharp
public OperationResult<Guid> AddElement(string parentGuid, StoryItemType type, string name)
{
    try
    {
        using (var serializationLock = new SerializationLock(...))
        {
            var newGuid = _outlineService.AddStoryElement(model, parentGuid, type, name);
            return new OperationResult<Guid>
            {
                IsSuccess = true,
                Payload = newGuid
            };
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "AddElement failed");
        return new OperationResult<Guid>
        {
            IsSuccess = false,
            ErrorMessage = ex.Message
        };
    }
}
```

**Why use OperationResult?**
- External callers (LLMs, tools) need predictable interface
- No exceptions leak to external code
- Structured error information
- Clear success/failure indication

### 6. StoryDocument Pattern (Issue #1100)

**Purpose**: Encapsulate StoryModel and FilePath together atomically

**Implementation**:
```csharp
public class StoryDocument
{
    public StoryModel StoryModel { get; set; }
    public string FilePath { get; set; }
}
```

**Usage**:
```csharp
// Before (Issue #1100):
// OutlineViewModel held separate StoryModel and StoryModelFile properties
// Services had to access these separately, risking mismatched state

// After (Issue #1100):
var document = AppState.CurrentDocument;
var model = document.StoryModel;
var path = document.FilePath;
// Always in sync, single source of truth
```

**Benefits**:
- Prevents mismatched model/path states
- Atomic document operations
- Simplifies service interfaces
- Single source of truth

### 7. Stateless Services Pattern

**Purpose**: Services don't maintain state between calls

**Implementation** (OutlineService):
```csharp
public class OutlineService
{
    // NO stored StoryModel field

    public Guid AddStoryElement(StoryModel model, Guid parentGuid, StoryItemType type, string name)
    {
        // Receives model as parameter
        var parent = model.StoryElements.StoryElementGuids[parentGuid];
        var newElement = new StoryElement { ... };
        model.StoryElements.Add(newElement);
        return newElement.Uuid;
    }
}
```

**Benefits**:
- Easier to test (no hidden state)
- Thread-safe by design (no shared state)
- Clearer dependencies (explicit parameters)
- Prevents stale state bugs

---

## Data Management

### Story Structure

**Tree-based hierarchy**:
- Stories organized as trees of elements
- Parent-child relationships via Guid references
- Two views: ExplorerView, NarratorView
- Separate TrashView for deleted items

**Element Types**:
- **Overview**: Story-level metadata
- **Problem**: Story problems/conflicts
- **Character**: Story characters
- **Scene**: Individual scenes
- **Folder**: Organizational containers
- **Setting**: Locations and time periods
- **Web**: External links and research
- **Note**: Free-form notes
- **TrashCan**: Deleted items container

**Two Views**:
- **ExplorerView**: Main story structure (hierarchy of all element types)
- **NarratorView**: Alternative narrative organization (scenes in reading order)
- Both views reference the same underlying StoryElement objects

### Trash Architecture

**Current Architecture** (Post-refactoring):
- **Separate TrashView**: Trash is now a separate TreeView (not dual-root)
- **TrashCan node**: Root node in TrashView
- **Deletion**: Elements moved to TrashCan, preserving hierarchy
- **Restoration**: Only top-level items in trash can be restored
- **Subtree preservation**: Deleted subtrees maintain structure in trash
- **Empty trash**: Permanently removes all trash items

**Previous Architecture** (Legacy):
- TrashCan was second root node in main TreeView
- Some old files still have this structure
- Migration handled automatically during file load

### GUID-Based References

**Why GUIDs instead of names?**
- Prevents issues with duplicate names
- Enables reliable element tracking
- Supports element renaming without breaking references
- Fast lookups via StoryElementGuids dictionary

**Implementation**:
```csharp
public class StoryElement
{
    public Guid Uuid { get; set; }  // Unique identifier
    public Guid Parent { get; set; } // Reference to parent
    public List<Guid> Children { get; set; } // References to children
}

public class StoryModel
{
    public Dictionary<Guid, StoryElement> StoryElementGuids { get; set; }
    // O(1) lookup by GUID
}
```

### Relationship Management

**Character Relationships**:
- **RelationshipModel**: Defines relationships between characters
- **Bidirectional**: Can be set from either character
- **Duplicate prevention**: Checks Partner, RelationType, Trait, Attitude (Notes ignored)
- **Validation**: Ensures valid character references

**Cast Members**:
- **Scene-Character associations**: Characters cast in scenes
- **Role tracking**: Character roles in specific scenes
- **Validation**: Ensures valid character and scene references

### Reference Management

**Finding References**:
- **FindElementReferences**: Locate all references to an element
- Searches cast members, relationships, etc.

**Removing References**:
- **RemoveReferences**: Clean up references when deleting elements
- Automatic cleanup during element deletion
- Prevents dangling references

### Element Conversion

**Problem ↔ Scene Conversion**:
You can convert a problem to a scene or vice versa. This conversion process works by:
- Creating a new element of the target type (Scene or Problem)
- Copying all common properties (Name, Notes, etc.) from the source element
- Preserving all child elements by transferring them to the new element
- Maintaining the position in the tree structure - the converted element remains in the same location
- Replacing the old element with the new element in the StoryElements collection
- Updating all parent-child relationships to reference the new element's GUID
- Synchronizing the name between StoryElement and StoryNodeItem so the UI reflects the change immediately
- The original element type is completely replaced, but the tree hierarchy remains intact

---

## State Management

### Central State (AppState) - Issue #1100

**Purpose**: Single source of truth for application state

**Properties**:
- **CurrentDocument**: StoryDocument (StoryModel + FilePath)
- **CurrentSaveable**: ISaveable (active ViewModel for edit flushing)
- **CurrentDocumentChanged**: Event for UI notifications

**Why AppState?**
- Eliminates circular dependencies (Services ↔ ViewModels)
- Services no longer depend on OutlineViewModel
- Clear ownership of document state
- Thread-safe access patterns

**Before (Issue #1100)**:
```
AutoSaveService → OutlineViewModel.StoryModel
BackupService → OutlineViewModel.StoryModelFile
(Circular dependency: ViewModel → Service → ViewModel)
```

**After (Issue #1100)**:
```
AutoSaveService → AppState.CurrentDocument.StoryModel
BackupService → AppState.CurrentDocument.FilePath
(No circular dependency: ViewModel → AppState ← Service)
```

### Model State

**StoryModel properties**:
- **Changed**: Tracks unsaved changes
- **CurrentView**: Active view (Explorer/Narrator)
- **StoryElementGuids**: Dictionary for fast lookups

**Changed Flag**:
- Set to true on any modification
- Reset to false after save
- Used to prompt for unsaved changes

### UI State

**Selection State**:
- Managed through ShellViewModel
- Last-clicked tracking for drag-and-drop
- Context menu based on selected node type

**Expansion State**:
- TreeView expansion persisted per node
- Maintained across saves/loads

**Background Colors**:
- Dynamic styling based on selection
- Validation feedback (red for errors)

**Context Menus**:
- Element-specific commands
- Based on node type (e.g., characters can't contain scenes)

**Page Registration**:
- Pages register ISaveable ViewModels in OnNavigatedTo
- Unregister in OnNavigatedFrom

### Stateless Services

**Design Principle**:
- OutlineService designed to be stateless
- Receives StoryModel as parameter for all operations
- No global state dependencies
- Improves testability and maintainability

**Example**:
```csharp
// STATELESS (correct)
public Guid AddStoryElement(StoryModel model, Guid parentGuid, ...)
{
    // Receives model as parameter
}

// STATEFUL (incorrect - old pattern)
private StoryModel _model; // NO! Don't store state
public Guid AddStoryElement(Guid parentGuid, ...)
{
    // Uses _model field - bad for testing and concurrency
}
```

---

## File Management

### File Format

**Extension**: `.stbx` (StoryCAD Binary)

**Serialization**:
- JSON-based format (converted from XML)
- Human-readable for version control
- Backward compatible with legacy formats
- IO operations handled in the DAL StoryIO.cs file

**File Structure**:
```json
{
  "ExplorerView": {...},
  "NarratorView": {...},
  "TrashCan": {...},
  "StoryElements": [
    {
      "Type": "Character",
      ...
    },
    {
      "Type": "Scene",
      ...
    }
  ]
}
```

**Auto-Migration**:
- Handles legacy file structures (dual-root TrashCan, XML format)
- Automatically converts old XML files to JSON on load
- Updates old formats on load
- Preserves data integrity during migration

### File Operations

**New Story**:
- Six built-in templates:
  - Blank: Empty story structure
  - Master Plots: Pre-populated with classic plot structure
  - Three Act: Three-act structure template
  - (Three additional templates)

**Open Story**:
- File picker dialog
- Recent files list
- File activation from Explorer (Windows only)

**Save Story**:
- Save to current file (if already saved)
- Save As for new location
- Auto-save in background (configurable)

**Close Story**:
- Prompt for unsaved changes
- Cleanup of resources

### Auto-Save

**Configuration**:
- Enabled/disabled in preferences
- Default interval is 15 seconds
- Runs on background thread

**Operation**:
1. Check if model has changed
2. Flush current edit via EditFlushService
3. Call OutlineService.WriteModel
4. Reset Changed flag

### Backup

**Configuration**:
- Enabled/disabled in preferences
- Configurable backup location
- Retention policy

**Three Types of Backups**:

1. **Auto Backup at Start of Session**:
   - Automatically created when a file is first opened
   - Provides a safety net before any edits are made
   - Stored with timestamp indicating session start

2. **Timed Backup**:
   - Created at regular intervals during editing
   - Configurable interval (separate from auto-save)
   - Provides periodic snapshots throughout the session
   - Continues as long as the file remains open

3. **Manual Backup (On Request)**:
   - User-initiated backup via menu or command
   - Created immediately when requested
   - Useful before major changes or experiments
   - User has full control over when these are created

**Operation**:
- Copy .stbx file to backup location
- Timestamped backups for all types
- Automatic cleanup of old backups based on retention policy

### Recent Files

**Tracking**:
- Last N opened files (configurable)
- Stored in preferences
- Displayed in file menu

**Validation**:
- Check file still exists
- Remove missing files from list

---

## Testing Architecture

### Test Types

**Unit Tests** (`[TestMethod]`):
- Test individual methods in isolation
- No UI dependencies
- Fast execution
- Run on all target frameworks

**UI Tests** (`[UITestMethod]`):
- Test WinUI-specific functionality
- Require WinUI runtime
- Run on Windows target only (`net9.0-windows10.0.22621`)
- Slower execution

**Integration Tests**:
- Test service interactions
- Test file I/O operations
- Test end-to-end scenarios

### Test Conventions

**File Naming**:
- `[SourceFileName]Tests.cs`
- Example: `OutlineService.cs` → `OutlineServiceTests.cs`

**Method Naming**:
- `MethodName_Scenario_ExpectedResult`
- Example: `AddStoryElement_ValidParent_ReturnsNewGuid`

**Folder Structure**:
- The StoryCADTests project folder structure mirrors the StoryCADLib structure
- This design makes locating test files easier
- Makes test coverage more visible - gaps in folder structure highlight untested areas
- Example: If StoryCADLib has `/Services/OutlineService.cs`, tests are in `/Services/OutlineServiceTests.cs`

**Test Data**:
- Located in `/TestInputs/` directory
- Copied to output directory for test execution
- Files:
  - AddElement.stbx
  - CloseFileTest.stbx
  - Full.stbx
  - OpenFromDesktopTest.stbx
  - OpenTest.stbx
  - StructureTests.stbx

**Mocking**:
- Uses DI container for test doubles
- Minimal mocking (prefer real dependencies where possible)

### Coverage Requirements

**Critical Components**:
- SemanticKernelAPI: 100% coverage achieved
- OutlineService: 100% coverage achieved
- Critical paths have integration tests
- Thread safety verified in tests

**Coverage Tools**:
- MSTest code coverage
- Reported in CI/CD pipeline

### Headless Mode Testing

**What is Headless Mode?**
- Testing without UI instantiation
- Used for service layer tests
- Some features behave differently (Changed flag, Issue #1068)
- API tests run in headless mode

**Initialization**:
```csharp
[TestInitialize]
public void Initialize()
{
    BootStrapper.Initialise(forUI: false); // Headless mode
}
```

**Limitations**:
- Changed flag doesn't work correctly in headless mode
- No UI-based events
- No navigation
- Issue #1068 will address limitations

### Multi-Target Test Execution

**Windows**:
- Tests run on both `net9.0-windows10.0.22621` and `net9.0-desktop`
- `[UITestMethod]` only runs on Windows target
- `[TestMethod]` runs on both

**macOS/Linux**:
- Tests run on `net9.0-desktop` only
- `[UITestMethod]` skipped (no WinUI)
- `[TestMethod]` runs normally

**Strategy**:
- Write most tests as `[TestMethod]` (cross-platform)
- Use `[UITestMethod]` only for WinUI-specific features
- Ensure service layer is thoroughly tested in headless mode

---

## Security and Privacy

### Data Storage

**Local Storage**:
- All story data stored locally on user's device
- No automatic cloud synchronization
- User has complete control over their data
- No server-side storage

**File Permissions**:
- Standard file system permissions
- No special privileges required

### Environment Variables

**Development**:
- `.env` file for local secrets
- Not committed to version control
- Located in application directory

**Production**:
- Doppler for secret management (API keys)
- Secure credential storage
- Opt-in for features requiring API keys

**Platform-Specific Paths**:
```csharp
var isPackaged = /* detect if MSIX package */;
var path = isPackaged
    ? Path.Combine(Package.Current.InstalledLocation.Path, ".env")
    : Path.Combine(AppContext.BaseDirectory, ".env");
```

### Error Reporting

**Optional Telemetry (elmah.io)**:
- **What it is**: elmah.io is a cloud-based error logging and monitoring service
- **What it does**: Captures unhandled exceptions and logs them to a centralized dashboard for developers
- **How it works**: When an error occurs, exception details (stack trace, message, context) are sent to elmah.io servers
- **Opt-in nature**: Completely optional - requires explicit user consent in preferences
- **Benefits**: Helps developers identify and fix bugs in production
- **Data collected**: Exception type, message, stack trace, StoryCAD version, platform information
- **No personal data**: Does not collect story content, user names, or file paths
- **Privacy**: Users must explicitly enable this feature; it is disabled by default

**Logging**:
- Local NLog file logging (no privacy concerns)
- Structured logging for debugging
- No sensitive data logged
- Log files stored locally on user's machine

**Privacy Principles**:
- User data stays on user's device
- Explicit consent required for telemetry
- Transparent about data collection
- No tracking without permission
- elmah.io telemetry is opt-in only

### Platform-Specific Security

**Windows MSIX**:
- Package signature required
- Capability declarations in Package.appxmanifest
- Sandboxed environment

**macOS**:
- Code signing for distribution
- Notarization for Gatekeeper
- Entitlements for sandboxed apps

---

## Key Design Decisions

### 1. Stateless Services

**Decision**: Services don't maintain state between calls

**Rationale**:
- Improves testability (no hidden state)
- Reduces bugs from stale state
- Enables better concurrency control
- Clearer dependencies

**Trade-offs**:
- Must pass StoryModel as parameter
- Slightly more verbose API

### 2. GUID-Based References

**Decision**: Elements referenced by GUID instead of names

**Rationale**:
- Prevents issues with duplicate names
- Enables reliable element tracking
- Supports renaming without breaking references
- Fast O(1) lookups

**Trade-offs**:
- More complex data structure
- Requires GUID management

### 3. Separate Trash View

**Decision**: Trash moved from dual-root to separate TreeView

**Rationale**:
- Cleaner architecture
- Better separation of concerns
- Improved UI clarity
- Easier to implement restore logic

**Trade-offs**:
- Migration required from old format
- Two TreeViews instead of one

### 4. SerializationLock Pattern

**Decision**: Ensure operations complete atomically

**Rationale**:
- Prevents data corruption from concurrent access
- Integrates with auto-save and backup
- Thread-safe by design

**Trade-offs**:
- Operations are serialized (not parallel)
- Must remember to use lock in all state-modifying operations

### 5. OperationResult for API

**Decision**: API returns OperationResult<T>, ViewModels don't

**Rationale**:
- External callers need safe, predictable interface
- No unhandled exceptions leak to API consumers
- Clear success/failure semantics
- ViewModels can handle exceptions appropriately (internal code)

**Trade-offs**:
- Two different patterns (API vs ViewModels)
- More code for API operations

### 6. StoryDocument Pattern (Issue #1100)

**Decision**: Encapsulate StoryModel and FilePath together

**Rationale**:
- Prevents mismatched model/path states
- Atomic document operations
- Simplifies service interfaces
- Single source of truth

**Trade-offs**:
- Additional wrapper class
- Must update both properties together

### 7. ISaveable Interface Pattern (Issue #1100)

**Decision**: ViewModels implement ISaveable for save operations

**Rationale**:
- Eliminates ViewModel dependencies in services
- No type-specific switch statements
- ViewModels self-register in pages
- Clean separation of concerns

**Trade-offs**:
- Additional interface to implement
- Must remember to register/unregister in pages

### 8. UNO Platform Adoption

**Decision**: Migrate from pure WinAppSDK to UNO Platform

**Rationale**:
- Enable cross-platform support (macOS, potentially Linux)
- Single codebase for all platforms
- Native performance on each platform
- Future expansion to mobile/web

**Trade-offs**:
- Additional complexity in project structure
- Platform-specific code handling required
- Multi-targeting build configuration

---

## Known Architectural Considerations

### 1. Name Synchronization

**Issue**: StoryElement.Name and StoryNodeItem.Name must stay synchronized

**Solution**: Two-way synchronization:
- Setting StoryElement.Name automatically updates Node.Name
- Changing Node.Name in the TreeView automatically updates StoryElement.Name
- This bidirectional sync ensures consistency regardless of where the name is changed

**Importance**: Critical for tree display accuracy and data integrity

### 2. Headless Mode

**Issue**: Some features work differently in headless mode

**Affected Features**:
- Changed flag doesn't work correctly
- No UI-based events

**Mitigation**: Issue #1068 will address limitations

### 3. Drag-and-Drop

**Current State**: Implemented at UI layer only

**Limitations**:
- No business logic validation
- TreeView control handles internally

**Future**: Consider adding validation layer for business rules

### 4. Circular Dependencies Resolved (Issue #1100)

**Previous**:
- Services depended on OutlineViewModel for document state
- OutlineViewModel ↔ AutoSaveService, BackupService, StoryIO

**Now**:
- Services use AppState.CurrentDocument
- Clean dependency hierarchy
- No circular dependencies

### 5. ViewModel Responsibilities

**Changes (Issue #1100)**:
- ShellViewModel simplified (SaveModel removed)
- OutlineViewModel no longer holds document state
- Element ViewModels handle their own save operations via ISaveable
- Better separation of concerns achieved

### 6. Platform-Specific Features

**Current State**: Some features are Windows-only

**Examples**:
- Printing (Windows-specific APIs)
- File associations (MSIX-specific)

**Future**:
- Consider platform abstraction layer
- Graceful degradation strategies
- Clear user communication for unavailable features

---

## Performance Considerations

### Indexing

**StoryElementGuids Dictionary**:
- O(1) lookup by GUID
- Critical for large story outlines
- Automatically maintained by StoryElementCollection

**Trade-offs**:
- Memory overhead (dictionary storage)
- Must keep in sync with collection

### Serialization

**SerializationLock**:
- Prevents performance issues from concurrent operations
- Auto-save runs on separate thread
- Backup operations are asynchronous

**Optimization**:
- JSON serialization is fast and efficient
- Large stories may require optimization
- Consider lazy loading for very large outlines

### Memory Management

**Current**:
- Story models kept in memory during editing
- Full tree loaded on open

**Future Optimizations**:
- Lazy loading for very large outlines
- Virtualization for large TreeViews
- Paging for very large collections

### UI Performance

**TreeView**:
- Two separate TreeViews (Explorer/Narrator, Trash)
- Virtualization enabled
- Expansion state persisted

**Data Binding**:
- INotifyPropertyChanged for efficient updates
- ObservableCollection for collection changes
- Avoid unnecessary property notifications

---

## CI/CD Process

### Overview

StoryCAD uses automated Continuous Integration and Continuous Deployment (CI/CD) pipelines to ensure code quality, run tests, and deploy releases across multiple platforms.

### CI/CD Architecture

**Pipeline Platforms**:
- **GitHub Actions**: Primary CI/CD platform
- **Azure DevOps**: Alternative/additional pipeline (if applicable)
- Automated builds triggered on push, pull request, and release tags

**Multi-Platform Build Strategy**:
- Separate build jobs for different target platforms
- Windows builds run on `windows-latest` runners
- macOS builds run on `macos-latest` runners
- Linux builds (future) will run on `ubuntu-latest` runners

### Build Pipeline

**Trigger Events**:
- Push to main/development branches
- Pull requests (for validation)
- Release tags (for deployment)
- Manual workflow dispatch

**Build Steps**:
1. **Checkout Code**: Clone repository and submodules
2. **Setup .NET**: Install .NET 9 SDK
3. **Restore Dependencies**: `dotnet restore` with NuGet package cache
4. **Build Solution**:
   - Windows: `dotnet build -c Release -f net9.0-windows10.0.22621`
   - macOS: `dotnet build -c Release -f net9.0-desktop`
5. **Run Tests**: Execute test suite on each platform
6. **Package Artifacts**: Create platform-specific installers

**Build Artifacts**:
- **Windows**: MSIX package, installer
- **macOS**: .app bundle, DMG installer
- **Platform-agnostic**: NuGet package for StoryCADLib

### Test Pipeline

**Test Execution Strategy**:
- Run on every commit and pull request
- Multi-target test execution (Windows builds both frameworks)
- Code coverage reporting

**Test Stages**:
1. **Unit Tests**: Fast, isolated tests (all platforms)
2. **UI Tests**: WinUI-specific tests (Windows only)
3. **Integration Tests**: End-to-end scenarios (all platforms)

**Test Reporting**:
- MSTest results published to pipeline
- Code coverage metrics tracked
- Failed tests block PR merges
- Coverage thresholds enforced for critical components

### Deployment Pipeline

**Release Process**:
1. **Version Tagging**: Create release tag (e.g., `v1.2.3`)
2. **Automated Build**: CI/CD detects tag and triggers release build
3. **Platform-Specific Packaging**:
   - Windows: MSIX package signed with certificate
   - macOS: .app signed and notarized for Gatekeeper
4. **Artifact Upload**: Packages uploaded to GitHub Releases
5. **Store Submission**: Automated submission to Microsoft Store (Windows)

**Deployment Targets**:
- **GitHub Releases**: All platform installers
- **Microsoft Store**: Windows MSIX (automated or manual submission)
- **Direct Download**: Links from project website
- **Future**: Mac App Store (requires Apple Developer account)

### Code Signing

**Windows Code Signing**:
- MSIX packages must be signed for distribution
- Certificate stored as GitHub Secret
- Signing integrated into build pipeline
- Required for Microsoft Store submission

**macOS Code Signing**:
- Developer ID certificate for distribution outside Mac App Store
- Notarization required for Gatekeeper
- Certificates stored as GitHub Secrets
- Automated signing in pipeline

### Environment Management

**Build Secrets**:
- Code signing certificates (Windows, macOS)
- Store API keys (Microsoft Store, future Mac App Store)
- elmah.io API keys (if used in builds)
- Stored securely in GitHub Secrets or Azure Key Vault

**Environment Variables**:
- Platform detection (OS type)
- Build configuration (Debug/Release)
- Version numbers (from tags or manual input)

### Quality Gates

**PR Requirements**:
- All tests must pass
- Code coverage must meet minimum threshold
- Build must succeed on all target platforms
- No merge until checks pass

**Release Requirements**:
- All tests pass on release branch
- Manual approval for production releases
- Version number incremented
- Release notes prepared

### Continuous Monitoring

**Post-Deployment**:
- elmah.io monitors production errors (opt-in users)
- GitHub Issues track user-reported bugs
- Download metrics from releases
- Store ratings and reviews

**Feedback Loop**:
- Production errors inform development priorities
- User feedback drives feature development
- Metrics guide performance optimization

### Pipeline Configuration Files

**Typical Locations**:
- `.github/workflows/build.yml` - Main build workflow
- `.github/workflows/test.yml` - Test execution workflow
- `.github/workflows/release.yml` - Release deployment workflow
- `azure-pipelines.yml` - Azure DevOps pipeline (if used)

### Future CI/CD Improvements

**Planned Enhancements**:
- Automated Mac App Store submission
- Linux package generation (AppImage, Flatpak)
- Performance benchmarking in pipeline
- Automated changelog generation
- Semantic versioning automation
- Staged rollout capabilities
- A/B testing infrastructure for new features

---

## Future Architecture Considerations

### Potential Improvements

#### 1. Add Validation Layer for Drag-and-Drop
- Business rules for valid moves
- Prevent invalid tree operations
- User feedback for invalid drops

#### 2. Improve Changed Flag Handling
- Work consistently in headless mode (Issue #1068)
- Better integration with auto-save
- Granular change tracking

#### 3. Consider Command Pattern for Operations
- Enable undo/redo functionality
- Better operation tracking
- Operation history

#### 4. Add Caching Layer
- Improve performance for large stories
- Cache frequently accessed elements
- Invalidation strategy

#### 5. Platform Abstraction Layer
- Interface-based platform services
- Cleaner separation of platform-specific code
- Easier testing

**Example**:
```csharp
public interface IPlatformService
{
    bool IsPackaged { get; }
    string GetDataPath();
    Task<bool> LaunchFileAsync(string path);
    bool SupportsPrinting { get; }
}
```

### Additional Platform Support

**UNO Platform enables**:
- **WebAssembly**: Browser-based version
- **iOS**: iPhone/iPad apps
- **Android**: Android tablets/phones
- **Linux Desktop**: Native Linux support

**Considerations**:
- Mobile UI adaptations required
- Touch interface for mobile
- Storage differences on mobile
- App store distribution

### API Evolution

**Current**: SemanticKernelAPI for LLM integration

**Future Possibilities**:
- RESTful API for web integration
- Plugin architecture for extending functionality
- Third-party integration patterns
- API versioning strategy

### Collaboration Features

**Current**: Single-user file-based architecture

**Future Considerations**:
- Backend service for collaborative editing
- Real-time sync between users
- Conflict resolution strategies
- Version control integration

---

## Integration Points

### AI/LLM Integration

**Current**:
- Via SemanticKernelAPI
- CollaboratorService for AI assistance
- Microsoft.SemanticKernel 1.41.0

**Features**:
- AI-powered writing suggestions
- Workflow-based interactions
- Extensible for future AI features

**Supported Models**:
- OpenAI (ChatGPT)
- Azure OpenAI
- Other SemanticKernel-compatible models

### External Tools

**API Enablement**:
- SemanticKernelAPI enables external tool integration
- OperationResult<T> for safe consumption
- Thread-safe operations

**Use Cases**:
- Custom writing tools
- Analysis tools
- Report generators

### Version Control

**Git Integration**:
- Text-based JSON format enables Git integration
- Diff-friendly structure
- Collaborative editing possible with VCS
- Human-readable format for code review

**Best Practices**:
- Commit .stbx files
- Use meaningful commit messages
- Branch for experimental changes

### UNO Platform Extensions

**Development Tools**:
1. **Hot Reload**: Development-time feature for rapid iteration
2. **DevServer**: Remote control for debugging
3. **Studio Integration**: Debugging tools in development builds

**Studio Integration** (App.xaml.cs):
```csharp
#if DEBUG
MainWindow.UseStudio();
#endif
```

**Platform-Specific Logging**:
```csharp
#if __WASM__
builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__ || __MACCATALYST__
builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#else
builder.AddConsole();
#endif
```

---

## Error Handling Strategy

### Service Layer

**Pattern**:
- Exceptions caught and logged
- Return appropriate error codes/messages
- Never expose internal exceptions to UI

**Example**:
```csharp
public Guid AddStoryElement(...)
{
    try
    {
        // Operation
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "AddStoryElement failed");
        throw; // Re-throw for ViewModels to handle
    }
}
```

### API Layer

**Pattern**:
- All operations wrapped in try-catch
- OperationResult pattern for safe consumption
- Detailed error messages for debugging
- Never throw exceptions

**Example**:
```csharp
public OperationResult<Guid> AddElement(...)
{
    try
    {
        var guid = _outlineService.AddStoryElement(...);
        return new OperationResult<Guid> { IsSuccess = true, Payload = guid };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "API AddElement failed");
        return new OperationResult<Guid> { IsSuccess = false, ErrorMessage = ex.Message };
    }
}
```

### UI Layer

**Pattern**:
- User-friendly error messages
- Graceful degradation
- Recovery options where possible

**Example**:
```csharp
try
{
    await _outlineViewModel.OpenFile(filePath);
}
catch (FileNotFoundException)
{
    await ShowMessageDialog("File not found. It may have been moved or deleted.");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to open file");
    await ShowMessageDialog("An error occurred while opening the file. Please try again.");
}
```

---

## Glossary

**Terms**:
- **UNO Platform**: Cross-platform UI framework enabling single codebase for multiple platforms
- **WinAppSDK**: Windows App SDK, Microsoft's native Windows development framework
- **WinUI 3**: Microsoft's modern UI framework for Windows applications
- **Skia**: Cross-platform 2D graphics library used for rendering on non-Windows platforms
- **MSIX**: Modern Windows app package format for deployment
- **Multi-Targeting**: Building a single project for multiple target frameworks
- **Platform Head**: UNO term for different execution models (WinAppSDK head, Desktop head, etc.)
- **UNO Single Project**: Project structure where one .csproj manages multiple platforms
- **Conditional Compilation**: Code included/excluded based on target platform
- **SerializationLock**: Thread safety pattern ensuring serial reusability
- **ISaveable**: Interface for ViewModels to flush changes to Models
- **OperationResult<T>**: Safe result wrapper for API operations
- **StoryDocument**: Atomic wrapper containing StoryModel and FilePath
- **AppState**: Central state management service
- **Headless Mode**: Testing mode without UI instantiation

---

## Architectural Decision Records

### ADR-001: UNO Platform Adoption

**Date**: 2024-2025 (estimated)
**Status**: Implemented
**Context**: Need for cross-platform support beyond Windows
**Decision**: Adopt UNO Platform for cross-platform development
**Consequences**:
- Positive: Cross-platform support with single codebase
- Positive: Native performance on each platform
- Negative: Additional build complexity
- Negative: Platform-specific code handling required

### ADR-002: Issue #1100 Architecture Refactoring

**Date**: 2025-01-24
**Status**: Implemented
**Context**: Circular dependencies between ViewModels and Services
**Decision**: Introduce AppState as central state management, ISaveable pattern for edit flushing
**Consequences**:
- Positive: Eliminated circular dependencies
- Positive: Cleaner separation of concerns
- Positive: Better testability
- Negative: Additional interfaces and patterns to learn

### ADR-003: OperationResult for API Layer Only

**Date**: 2025 (from Issue #1069)
**Status**: Implemented
**Context**: Need safe error handling for external API consumers
**Decision**: Use OperationResult<T> for API layer, direct exceptions for ViewModels
**Consequences**:
- Positive: Safe API for external callers
- Positive: Simpler ViewModel code
- Negative: Two different patterns in codebase

### ADR-004: Separate Trash View

**Date**: 2024-2025 (estimated)
**Status**: Implemented
**Context**: Dual-root TreeView was complex and confusing
**Decision**: Move Trash to separate TreeView
**Consequences**:
- Positive: Cleaner UI
- Positive: Better separation of concerns
- Negative: Migration required from old format

---

## Document Revision History

| Date | Version | Changes |
|------|---------|---------|
| 2025-01-24 | 1.0 | Initial compilation from Issue #1069 |
| 2025-01-24 | 1.1 | Updated with Issue #1100 refactoring |
| 2025-10-08 | 2.0 | **Major revision**: Added UNO Platform architecture, complete folder structures, multi-targeting strategy, platform-specific code patterns, cross-platform considerations, build/deployment, dependency management, enhanced diagrams |
| 2025-10-08 | 2.1 | **Corrections and additions**: Updated Dependency Injection description, expanded Element Conversion explanation, corrected file format from XML to JSON, updated file operations (6 templates, 15-second auto-save default, 3 backup types), added test folder structure mirroring convention, expanded elmah.io description, corrected name synchronization to two-way, added comprehensive CI/CD Process section |

---

*This document represents the current understanding of StoryCAD architecture as of 2025-10-08, incorporating the UNO Platform migration and all architectural refactorings through Issue #1100. For questions or corrections, please consult the development team.*
