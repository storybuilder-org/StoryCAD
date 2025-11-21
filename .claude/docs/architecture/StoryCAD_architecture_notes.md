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

## StoryBuilder Organization & Repository Ecosystem

### GitHub Organization

**Organization**: [github.com/storybuilder-org](https://github.com/storybuilder-org)

StoryCAD is part of a larger ecosystem of repositories under the StoryBuilder organization. This section documents all repositories, their purposes, and relationships.

---

### Core Application Repositories

#### 1. StoryCAD (Main Repository)
- **GitHub**: [github.com/storybuilder-org/StoryCAD](https://github.com/storybuilder-org/StoryCAD)
- **Local Path**: `/mnt/d/dev/src/StoryCAD/`
- **Visibility**: Public
- **Purpose**: Main StoryCAD application - cross-platform fiction writing tool
- **Technology**: .NET 9.0, UNO Platform 6.2.36, WinUI 3
- **Targets**: Windows (WinAppSDK), macOS (Desktop head)
- **Key Components**:
  - `StoryCAD/` - Main WinUI 3 application
  - `StoryCADLib/` - Core business logic library (NuGet package)
  - `StoryCADTests/` - MSTest test project
  - `.claude/docs/` - LLM/AI agent documentation
  - `devdocs/` - Active issue documentation

**Branches**:
- `main` - Production Windows-only version (3.x)
- `UNOTestBranch` - Cross-platform development (4.0)

---

#### 2. StoryBuilderCollaborator
- **GitHub**: [github.com/storybuilder-org/StoryBuilderCollaborator](https://github.com/storybuilder-org/StoryBuilderCollaborator)
- **Local Path**: `/mnt/d/dev/src/StoryBuilderCollaborator/`
- **Visibility**: Private
- **Purpose**: AI collaboration features for StoryCAD using Microsoft Semantic Kernel
- **Technology**: .NET 9.0, Semantic Kernel 1.41.0
- **Integration**: Loaded as plugin DLL by StoryCAD's CollaboratorService
- **Key Components**:
  - `CollaboratorLib/` - Plugin library implementing ICollaborator interface
  - Semantic Kernel workflows for character development, plot suggestions, etc.
  - AI-assisted writing wizards

**Relationship to StoryCAD**:
- Separate assembly loaded dynamically at runtime
- StoryCAD → CollaboratorService → CollaboratorLib.dll
- Optional feature (enabled for developer builds)
- See: `StoryCADLib/Services/Collaborator/CollaboratorService.cs`

---

### Documentation Repositories

#### 3. ManualTest
- **GitHub**: [github.com/storybuilder-org/ManualTest](https://github.com/storybuilder-org/ManualTest)
- **Local Path**: `/mnt/d/dev/src/ManualTest/`
- **Visibility**: Public
- **Purpose**: End-user documentation for StoryCAD
- **Technology**: Jekyll with Just the Docs theme
- **URL**: [User Manual](https://storybuilder-org.github.io/ManualTest/) (if published)

**Content Structure**:
```
ManualTest/docs/
├── Front Matter/          # Getting started, license, help
├── Quick Start/           # UI basics, navigation
├── Story Elements/        # Element-by-element documentation
├── Tools/                 # Plot tools, conflict builder
├── Writing with StoryCAD/ # Workflows and techniques
├── Tutorial Creating a Story/
├── Preferences/           # Settings and configuration
├── Reports/               # Report generation
├── Researching your story/
├── For Developers/        # API documentation
├── Back Matter/           # Appendices
└── Miscellaneous/
```

**Usage**:
- Reference via CLAUDE.md "User Manual Navigation"
- Search with Glob/Grep rather than reading all files
- See: `CLAUDE.md` for navigation guidance

---

### Support & Utility Repositories

#### 4. StoryCAD-Legacy-STBX-Conversion-Tool
- **GitHub**: [github.com/storybuilder-org/StoryCAD-Legacy-STBX-Conversion-Tool](https://github.com/storybuilder-org/StoryCAD-Legacy-STBX-Conversion-Tool)
- **Local Path**: `/mnt/d/dev/src/StoryCAD-Legacy-STBX-Conversion-Tool/`
- **Visibility**: Public
- **Purpose**: Convert legacy XML-based .stbx files to JSON-based format
- **Technology**: .NET
- **Status**: Maintenance mode (conversion tool for legacy users)

**Background**:
- StoryCAD evolved from StoryBuilder
- File format changed from XML to JSON
- This tool enables migration of old user files

---

#### 5. NRTFTree-Async
- **GitHub**: [github.com/storybuilder-org/NRTFTree-Async](https://github.com/storybuilder-org/NRTFTree-Async)
- **Local Path**: (NuGet package dependency)
- **Visibility**: Public
- **Purpose**: Forked RTF processing library with async support
- **Technology**: .NET
- **Integration**: Used by StoryCAD for RTF text editing features
- **Fork Note**: Modified from original NRTFTree to add async/await support

**Usage in StoryCAD**:
- Referenced as NuGet package: NRTFTree-Async 1.0.1
- See: `Directory.Packages.props`
- RTF text boxes in story element forms (Problem, Scene, etc.)

---

#### 6. API-Samples
- **GitHub**: [github.com/storybuilder-org/API-Samples](https://github.com/storybuilder-org/API-Samples)
- **Local Path**: `/mnt/d/dev/src/API-Samples/`
- **Visibility**: Private
- **Purpose**: Sample code and examples for StoryCAD API
- **Technology**: .NET, C#
- **Audience**: External developers using StoryCAD API

**Content**:
- Example API usage patterns
- Integration examples
- Test scenarios for API consumers

**Note**: StoryCAD has a public API (SemanticKernelAPI) that external tools can use


### Repository Relationships

#### Dependency Graph

```
StoryCAD (Main App)
├── StoryCADLib (Core Logic - same repo)
│   └── NRTFTree-Async (NuGet package)
├── StoryBuilderCollaborator (Plugin - separate repo)
│   └── Semantic Kernel 1.41.0
└── ManualTest (Documentation - separate repo)

StoryCAD-Legacy-STBX-Conversion-Tool
└── (standalone utility for users)

API-Samples
└── StoryCAD API (references main app)
```

#### Runtime Architecture

```
┌─────────────────────────────────────────┐
│         StoryCAD Main App               │
│  (StoryCAD.exe / StoryCAD.app)          │
└────────────┬────────────────────────────┘
             │
             ├─► StoryCADLib (in-process)
             │   └─► NRTFTree-Async
             │
             └─► CollaboratorLib.dll (plugin)
                 └─► Semantic Kernel → OpenAI/Azure
```

---

### Development Workflows

#### Working on StoryCAD Core
1. Clone: `github.com/storybuilder-org/StoryCAD`
2. Branch: `main` (Windows) or `UNOTestBranch` (cross-platform)
3. Documentation: `.claude/docs/` for LLMs, `devdocs/` for active issues
4. Tests: `StoryCADTests/`

#### Working on AI Features
1. Clone: `github.com/storybuilder-org/StoryBuilderCollaborator` (private)
2. Build CollaboratorLib.dll
3. Set `STORYCAD_PLUGIN_DIR` environment variable
4. StoryCAD loads plugin at runtime
[StoryCAD.2025-11-18.log](../../../../../../Downloads/StoryCAD.2025-11-18.log)
#### Writing Documentation
**For End Users**:
- Repository: `ManualTest`
- Format: Markdown with Jekyll/Just the Docs
- Build: `bundle exec jekyll serve`

**For Developers/LLMs**:
- Repository: `StoryCAD/.claude/docs/`
- Format: Plain Markdown
- No build required

**For Active Issues**:
- Location: `StoryCAD/devdocs/issue_NNNN_*.md`
- Archive: Move to `storybuilder-miscellaneous` when issue closes

---

### Repository Management

#### Issue Tracking
- **Main Issues**: [github.com/storybuilder-org/StoryCAD/issues](https://github.com/storybuilder-org/StoryCAD/issues)
- **Documentation**: `devdocs/issue_NNNN_*.md` for active issues
- **Archive**: `storybuilder-miscellaneous/devdocs/` for closed issues
- **Tool**: `ListStoryCADIssues` for issue management

#### Release Process
1. Development in feature branches (e.g., `issue-1143-remove-cyclic-dependency`)
2. Pull request to `main` or `UNOTestBranch`
3. Review and merge
4. Package with MSIX (Windows) or .app bundle (macOS)
5. Publish releases on GitHub

#### Documentation Updates
- **User Manual**: Update `ManualTest` when user-facing features change
- **Developer Docs**: Update `.claude/docs/` when architecture/patterns change
- **Issue Docs**: Create `devdocs/issue_NNNN_*.md` for implementation notes

---

### Quick Reference: Repository URLs

| Repository | URL | Visibility |
|------------|-----|------------|
| StoryCAD | https://github.com/storybuilder-org/StoryCAD | Public |
| StoryBuilderCollaborator | https://github.com/storybuilder-org/StoryBuilderCollaborator | Private |
| ManualTest | https://github.com/storybuilder-org/ManualTest | Public |
| StoryCAD-Legacy-STBX-Conversion-Tool | https://github.com/storybuilder-org/StoryCAD-Legacy-STBX-Conversion-Tool | Public |
| NRTFTree-Async | https://github.com/storybuilder-org/NRTFTree-Async | Public |
| API-Samples | https://github.com/storybuilder-org/API-Samples | Private |

---

### External Dependencies vs Internal Repositories

**External Dependencies** (NuGet packages):
- UNO Platform (6.2.36)
- Microsoft.SemanticKernel (1.41.0)
- CommunityToolkit.Mvvm (8.4.0)
- NLog, MySQL.Data, Octokit, etc.
- See: `Directory.Packages.props` for complete list

**Internal Repositories** (StoryBuilder org):
- CollaboratorLib (plugin)
- NRTFTree-Async (forked/customized library)

**Distinction**: Internal repos are maintained by StoryBuilder org, external are third-party packages.

---

## User Documentation

### Location and Purpose

StoryCAD's user-facing documentation is maintained in the **ManualTest repository**:

- **Repository**: [github.com/storybuilder-org/ManualTest](https://github.com/storybuilder-org/ManualTest)
- **Local Path**: `/mnt/d/dev/src/ManualTest/`
- **Published (Staging)**: https://storybuilder-org.github.io/StoryBuilder-Manual/
- **Format**: Jekyll static site with Just the Docs theme
- **Content**: 115 markdown files organized in 11 thematic sections
- **Audience**: Fiction writers (non-technical users)

**For AI Agent Guidance**: See `/mnt/d/dev/src/ManualTest/CLAUDE.md`

### Documentation Structure

1. **Front Matter** (4 files) - Introduction, legal, help resources
2. **Quick Start** (23 files) - UI basics, navigation, file operations
3. **Story Elements** (30 files) - Forms and tabs for each element type
4. **Tools** (10 files) - Plot aids, conflict builder, dramatic situations
5. **Writing with StoryCAD** (17 files) - Outlining workflows and techniques
6. **Tutorial Creating a Story** (10 files) - Step-by-step guide
7. **Reports** (4 files) - Print and Scrivener integration
8. **Researching your story** (3 files) - Notes and web research
9. **Preferences** (1 file) - Application settings
10. **For Developers** (6 files) - API docs, changelog, developer notes
11. **Back Matter** (2 files) - Glossary, appendices

### Finding Relevant User Documentation

**Index File**: `/mnt/c/temp/user_manual.md` contains summaries of all 115 documentation files.

**Search Strategy**:
```bash
# Quick lookup - find which file documents a feature
grep -i "scene" /mnt/c/temp/user_manual.md

# Search specific section
grep -r "conflict" /mnt/d/dev/src/ManualTest/docs/"Story Elements"/
```

### Cross-Referencing: Technical ↔ User Documentation

#### When Implementing Features

Before implementing a feature, **read the relevant user documentation** to understand:
- User expectations and mental models
- Current UI workflows
- Terminology users are familiar with
- Related features that may be affected

**Example**: Implementing changes to Scene elements?
- Read: `/mnt/d/dev/src/ManualTest/docs/Story Elements/Scene_Form.md`
- Read: `/mnt/d/dev/src/ManualTest/docs/Story Elements/Scene_Tab.md`
- Read: `/mnt/d/dev/src/ManualTest/docs/Story Elements/Conflict_Tab.md`
- Read: `/mnt/d/dev/src/ManualTest/docs/Story Elements/Sequel_Tab.md`

#### When Features Change

**Documentation updates are required** when code changes affect:
- UI layout or navigation (Quick Start)
- Story element forms or tabs (Story Elements)
- Tools or wizards (Tools)
- Workflows or processes (Writing with StoryCAD)
- API surface (For Developers)
- Settings or preferences (Preferences)

**Documentation Workflow**:
1. Make code changes in StoryCAD repository
2. Update corresponding documentation in ManualTest repository (staging)
3. Test documentation locally: `cd ManualTest && bundle exec jekyll serve`
4. Push to ManualTest to publish to staging site
5. Notify maintainer to copy `/docs/` to StoryCAD repo for production

#### Bidirectional Linking

**From User Docs → Technical Docs**:
User manual "For Developers" section links to:
- StoryCAD CLAUDE.md
- Architecture documentation (`.claude/docs/architecture/`)
- API implementation details

**From Technical Docs → User Docs**:
Architecture documentation links to:
- User workflows and feature documentation
- API usage examples in "For Developers"
- UI element descriptions

### Feature-to-Documentation Mapping

| Feature Area | Technical Code Location | User Documentation Section |
|--------------|------------------------|---------------------------|
| Story Elements (Character, Problem, etc.) | `StoryCADLib/Models/`, `StoryCAD/ViewModels/` | `Story Elements/` (30 files) |
| Plot Tools (Master Plots, Dramatic Situations) | `StoryCAD/Views/Tools/` | `Tools/` (10 files) |
| File Operations | `StoryCADLib/Services/OutlineService.cs` | `Quick Start/Reading_and_Writing_Outlines.md` |
| Navigation | `StoryCAD/Views/Shell.xaml` | `Quick Start/Navigating_in_StoryCAD.md` |
| Reports | `StoryCADLib/Services/Reports/` | `Reports/` (4 files) |
| Preferences | `StoryCAD/Views/Preferences.xaml` | `Preferences/Preferences.md` |
| API | `StoryCADLib/SemanticKernelAPI.cs` | `For Developers/Using_the_API.md` |
| Collaborator (AI features) | `CollaboratorLib/` (separate repo) | User docs TBD (feature in development) |

### Documentation Standards for Developers

When updating user documentation:

1. **Write for non-technical audience**: Fiction writers, not programmers
2. **Use screenshots**: Visual guides for UI elements (store in `/docs/media/`)
3. **Task-oriented**: Explain "how to accomplish X" not "how X works internally"
4. **Plain language**: Avoid technical jargon (use "story outline" not "document object model")
5. **Include examples**: Show concrete examples of feature usage
6. **Test locally**: Verify Jekyll site builds and navigation works
7. **Front matter required**: All pages need front matter or they won't publish

**Jekyll Front Matter Template**:
```yaml
---
title: Page Title
layout: default
nav_enabled: true
nav_order: 33
parent: Section Name  # Optional
has_toc: false
---
```

### Documentation Quality Checklist

Before marking documentation updates complete:

- [ ] Updated relevant user documentation pages in ManualTest
- [ ] Screenshots updated (if UI changed)
- [ ] Front matter correct on all modified pages
- [ ] Links tested (internal and external)
- [ ] Tested locally with Jekyll
- [ ] Navigation logical and discoverable
- [ ] Terminology consistent with glossary
- [ ] Examples accurate and current
- [ ] Cross-references to related pages added
- [ ] Staging site verified

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

### 8. MVVM Messenger Pattern

**Purpose**: Loosely-coupled communication between ViewModels and components

StoryCAD uses the `IMessenger` interface from CommunityToolkit.Mvvm to enable ViewModels, services, and UI components to communicate without direct dependencies. This pattern is particularly important in a complex application where components need to notify each other of state changes without creating tight coupling.

**Core Implementation**:

The messenger pattern uses a publish-subscribe model where components can:
- **Register** to receive specific message types
- **Send** messages that registered recipients will receive
- **Request** information using request/response messages

**Key Components**:

**ShellViewModel** (Primary Message Hub):
```csharp
public class ShellViewModel : ObservableRecipient
{
    public ShellViewModel(...)
    {
        // Register message handlers in constructor
        Messenger.Register<IsChangedRequestMessage>(this,
            (_, m) => { m.Reply(State.CurrentDocument?.Model?.Changed ?? false); });

        Messenger.Register<ShellViewModel, IsChangedMessage>(this,
            static (r, m) => r.IsChangedMessageReceived(m));

        Messenger.Register<ShellViewModel, IsBackupStatusMessage>(this,
            static (r, m) => r.BackupStatusMessageReceived(m));

        Messenger.Register<ShellViewModel, StatusChangedMessage>(this,
            static (r, m) => r.StatusMessageReceived(m));

        Messenger.Register<ShellViewModel, NameChangedMessage>(this,
            static (r, m) => r.NameMessageReceived(m));
    }

    private void StatusMessageReceived(StatusChangedMessage statusMessage)
    {
        // Update UI with status message
        StatusMessage = statusMessage.Value.Status;
        switch (statusMessage.Value.Level)
        {
            case LogLevel.Info:
                StatusColor = Window.SecondaryColor;
                _statusTimer.Interval = TimeSpan.FromSeconds(15);
                _statusTimer.Start();
                break;
            case LogLevel.Warn:
                StatusColor = new SolidColorBrush(Colors.Goldenrod);
                _statusTimer.Interval = TimeSpan.FromSeconds(30);
                _statusTimer.Start();
                break;
            case LogLevel.Error:
                StatusColor = new SolidColorBrush(Colors.Red);
                break;
        }
    }

    private void IsChangedMessageReceived(IsChangedMessage isDirty)
    {
        if (State.CurrentDocument?.Model != null)
        {
            State.CurrentDocument.Model.Changed = isDirty.Value;
        }

        // Update visual indicator
        ChangeStatusColor = isDirty.Value ? Colors.Red : Colors.Green;
    }

    private void NameMessageReceived(NameChangedMessage name)
    {
        // Synchronize TreeView node name with element name
        CurrentNode.Name = name.Value.NewName;
        switch (CurrentNode.Type)
        {
            case StoryItemType.Setting:
                var settingIndex = SettingModel.SettingNames.IndexOf(name.Value.OldName);
                SettingModel.SettingNames[settingIndex] = name.Value.NewName;
                break;
        }
    }
}
```

**Message Types**:

StoryCAD uses several message types for different communication needs:

1. **StatusChangedMessage**: Updates status bar with user feedback
   ```csharp
   public class StatusChangedMessage : ValueChangedMessage<StatusMessage>
   {
       public StatusChangedMessage(StatusMessage value) : base(value) { }
   }

   // Usage - sending from any ViewModel or service:
   Messenger.Send(new StatusChangedMessage(
       new StatusMessage("File saved successfully", LogLevel.Info, true)));
   ```

2. **IsChangedMessage**: Notifies when document has unsaved changes
   ```csharp
   public class IsChangedMessage : ValueChangedMessage<bool>
   {
       public IsChangedMessage(bool value) : base(value) { }
   }

   // Usage - notifying document changed:
   Messenger.Send(new IsChangedMessage(true));
   ```

3. **IsChangedRequestMessage**: Request current changed state (request/response pattern)
   ```csharp
   public class IsChangedRequestMessage : RequestMessage<bool> { }

   // Usage - requesting information:
   var isChanged = Messenger.Send(new IsChangedRequestMessage());
   ```

4. **NameChangedMessage**: Synchronizes element name changes across UI
   ```csharp
   public class NameChangedMessage : ValueChangedMessage<NameChangeMessage>
   {
       public NameChangedMessage(NameChangeMessage value) : base(value) { }
   }

   // Usage - from OverviewViewModel when name changes:
   NameChangeMessage msg = new(oldName, newName);
   Messenger.Send(new NameChangedMessage(msg));
   ```

5. **IsBackupStatusMessage**: Updates backup status indicator
   ```csharp
   public class IsBackupStatusMessage : ValueChangedMessage<bool>
   {
       public IsBackupStatusMessage(bool value) : base(value) { }
   }
   ```

**Sending Messages**:

Any component can send messages using the static `Messenger.Send()` method:

```csharp
// From ViewModel - notify status change
Messenger.Send(new StatusChangedMessage(
    new StatusMessage("Loading file...", LogLevel.Info)));

// From service - notify error
Messenger.Send(new StatusChangedMessage(
    new StatusMessage("Failed to save file", LogLevel.Error)));

// From business logic - notify document changed
Messenger.Send(new IsChangedMessage(true));
```

**Registering for Messages**:

Components register for messages in their constructor or initialization:

```csharp
// Register with instance method handler
Messenger.Register<IsChangedRequestMessage>(this,
    (recipient, message) => {
        message.Reply(State.CurrentDocument?.Model?.Changed ?? false);
    });

// Register with static method for better performance
Messenger.Register<ShellViewModel, StatusChangedMessage>(this,
    static (recipient, message) => recipient.StatusMessageReceived(message));
```

**Message Flow Examples**:

**Example 1: User Edits Story Element Name**
```
User types in OverviewViewModel.Name property
    ↓
OverviewViewModel.Name setter sends NameChangedMessage
    ↓
ShellViewModel receives message (registered handler)
    ↓
ShellViewModel.NameMessageReceived() updates TreeView node name
    ↓
UI automatically updates via data binding
```

**Example 2: Document Save Operation**
```
User clicks Save button
    ↓
OutlineViewModel.SaveFile() sends StatusChangedMessage("Saving...", Info)
    ↓
ShellViewModel.StatusMessageReceived() updates status bar
    ↓
Save operation completes
    ↓
OutlineViewModel sends IsChangedMessage(false)
    ↓
ShellViewModel.IsChangedMessageReceived() updates change indicator (red → green)
    ↓
OutlineViewModel sends StatusChangedMessage("File saved", Info)
    ↓
ShellViewModel displays success message
```

**Example 3: Request/Response Pattern**
```
Service needs to check if document has changes
    ↓
Service sends IsChangedRequestMessage
    ↓
ShellViewModel receives request
    ↓
ShellViewModel replies with current changed state
    ↓
Service receives response and proceeds accordingly
```

**Benefits of the Messenger Pattern**:

1. **Loose Coupling**: Components don't need direct references to each other
   - ViewModels can communicate without circular dependencies
   - Services can notify UI without knowing about ViewModels
   - Easy to add new message recipients without modifying senders

2. **Centralized Communication**: All inter-component messages flow through one system
   - Easy to trace message flow for debugging
   - Single point of control for communication
   - Simplified testing with message interception

3. **Separation of Concerns**: Each component focuses on its responsibility
   - ShellViewModel handles UI updates
   - Element ViewModels handle business logic
   - Services handle data operations
   - All communicate via well-defined messages

4. **Testability**: Easy to test message handling in isolation
   - Mock message sending/receiving in unit tests
   - Verify correct messages are sent for specific actions
   - Test message handlers independently

5. **Type Safety**: Compile-time checking of message types
   - Messages are strongly typed classes
   - No string-based events or magic values
   - IDE autocomplete and refactoring support

**Common Usage Patterns**:

**Status Updates**: Keep users informed of operations
```csharp
Messenger.Send(new StatusChangedMessage(
    new StatusMessage("Opening file...", LogLevel.Info)));
// ... perform operation ...
Messenger.Send(new StatusChangedMessage(
    new StatusMessage("File opened successfully", LogLevel.Info)));
```

**Change Tracking**: Update UI indicators when data changes
```csharp
// After modifying data
State.CurrentDocument.Model.Changed = true;
Messenger.Send(new IsChangedMessage(true));
```

**Name Synchronization**: Keep TreeView and element names in sync
```csharp
// In element ViewModel when name changes
if (_name != value)
{
    NameChangeMessage msg = new(_name, value);
    Messenger.Send(new NameChangedMessage(msg));
    _name = value;
}
```

**Important Considerations**:

1. **Message Registration Lifecycle**: Always register in constructor, unregister when disposed
2. **Thread Safety**: Messages can be sent from any thread; handlers may need UI thread marshaling
3. **Performance**: Use static handlers when possible for better performance
4. **Message Design**: Keep messages simple and focused on single purpose
5. **Documentation**: Document which components send/receive which messages

**Comparison with Direct Method Calls**:

**Without Messenger (Tight Coupling)**:
```csharp
public class ElementViewModel
{
    private ShellViewModel _shell; // Direct dependency

    public void UpdateName(string newName)
    {
        _shell.UpdateTreeNodeName(newName); // Direct call
    }
}
```

**With Messenger (Loose Coupling)**:
```csharp
public class ElementViewModel
{
    // No dependency on ShellViewModel

    public void UpdateName(string newName)
    {
        Messenger.Send(new NameChangedMessage(new(oldName, newName)));
    }
}
```

The Messenger pattern is fundamental to StoryCAD's architecture, enabling clean separation between UI, business logic, and services while maintaining responsive user feedback and proper state synchronization.

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
| 2025-10-09 | 2.2 | **MVVM Messenger Pattern documentation**: Added comprehensive section 8 on MVVM Messenger Pattern (after Stateless Services Pattern). Documents IMessenger interface from CommunityToolkit.Mvvm, message types (StatusChangedMessage, IsChangedMessage, IsChangedRequestMessage, NameChangedMessage, IsBackupStatusMessage), registration and sending patterns, message flow examples, benefits, and usage patterns. Based on ShellViewModel.cs implementation analysis. |

---

*This document represents the current understanding of StoryCAD architecture as of 2025-10-09, incorporating the UNO Platform migration and all architectural refactorings through Issue #1100. For questions or corrections, please consult the development team.*
