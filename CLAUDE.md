# StoryCAD Development Guide

This file provides project-specific guidance for StoryCAD development. Universal development standards are defined in `/dev/src/CLAUDE.md` and automatically included.

## Primary Directive: Simplicity First

**When solving StoryCAD problems, always choose the simplest approach:**
- Direct method calls over reflection
- Concrete types over abstractions (until needed)
- Built-in .NET features over external libraries
- Straightforward debugging (`Debugger.Launch()`) over complex conditions
- Clear, obvious code over clever solutions

## Platform Development (UNO)

### Working with UNO Platform
When developing on the UNOTestBranch:
- **Shared Code First**: Write platform-agnostic code whenever possible
- **Platform Checks**: Use `#if HAS_UNO_WINUI` for Windows-specific code, `#if __MACOS__` for macOS
- **Testing**: Test on both Windows and macOS before committing
- **File Paths**: Use platform-agnostic paths (avoid hardcoded Windows paths)
- **Keyboard Shortcuts**: Support both Ctrl (Windows) and Cmd (macOS) patterns

### UNO Platform Resources
- [UNO Platform Documentation](https://platform.uno/docs/)
- [Migration Guide](https://github.com/orgs/storybuilder-org/projects/6)
- Testing Strategy: `/StoryCADTests/ManualTests/UNO_Platform_Testing_Strategy.md`

## Branch Management

**IMPORTANT**: Always work in the correct branch for the issue:
- Check current branch with `git status` at start of session
- Create issue-specific branches: `issue-{number}-{description}`
- Example: `issue-1069-test-coverage` for issue #1069
- Never work directly in `main` or unrelated feature branches
- If in wrong branch, stash changes and switch to correct branch

**When switching to an existing branch:**
1. Check current status: `git status`
2. Stash any uncommitted changes if needed: `git stash`
3. Switch to the branch: `git checkout branch-name`
4. **SYNC THE BRANCH**: Pull latest changes
   - If branch tracks remote: `git pull`
   - If branch is behind main: `git pull origin main` or `git merge main`
5. Apply stashed changes if any: `git stash pop`

## About StoryCAD

StoryCAD is a free, open-source application for fiction writers that provides structured outlining tools. It's described as "CAD for fiction writers" and helps writers manage the complexity of plotted fiction through systematic story development.

**Version 3.x** (main branch): Windows-only application using WinUI 3
**Version 4.0** (UNOTestBranch): Cross-platform application supporting Windows and macOS via UNO Platform

## Quick Reference

### Technology Stack (UNO Platform - Branch: UNOTestBranch)
- **Framework**: .NET 9.0 with UNO Platform 5.x
- **UI Framework**: WinUI 3 (cross-platform via UNO)
- **Platform Targets**:
  - **Windows**: WinAppSDK head (Windows 10/11, minimum 10.0.19041.0)
  - **macOS**: Desktop head (macOS 10.15+)
  - **Future**: Linux, WebAssembly, iOS, Android
- **Architecture**: MVVM with CommunityToolkit.Mvvm
- **Testing**: MSTest with UNO Platform support
- **Shared Code**: ~90% across all platforms

### Production Stack (Main Branch - Version 3.x)
- **Framework**: .NET 9.0 with WinUI 3, Windows App SDK 1.6
- **Platform**: Windows 10/11 only

### Project Structure
1. **StoryCAD** - Main WinUI 3 application (executable)
2. **StoryCADLib** - Core business logic library (NuGet package)
3. **StoryCADTests** - MSTest test project

## Development Guides

### Core Documentation
- [Architecture Guide](./devdocs/architecture.md) - MVVM patterns, data binding, services
- [Build & Test Commands](./devdocs/build_commands.md) - WSL/Windows build instructions
- [Testing Guide](./devdocs/testing.md) - Test patterns and data creation
- [Coding Standards](./devdocs/coding.md) - Naming conventions, layout, SOLID principles

### Workflow Documentation
- [AI Workflow](./devdocs/AI_workflow.md) - Issue-centric development process

### User Manual Navigation
- **Entry Point**: [User Manual Index](/mnt/d/dev/src/ManualTest/index.md) - Start here for overview
- **Documentation Structure**: Organized by topic in subdirectories (Just the Docs format)
- **Efficient Search**: Use Glob/Grep to search specific topics rather than reading all files
- **Key Sections**:
  - `Front Matter/` - Getting started, legal matters, help
  - `Quick Start/` - UI, navigation, basic operations
  - `Story Elements/` - Forms and tabs for each story element type
  - `Tools/` - Plotting aids, conflict builder, dramatic situations
  - `Writing with StoryCAD/` - Workflows, plotting, character development
  - `Tutorial Creating a Story/` - Step-by-step story creation guide

## Quick Start

### Build from WSL
```bash
# Build solution
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64

# Run all tests
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"
```

For detailed commands, see [Build & Test Commands](./devdocs/build_commands.md).

## Key Services

- **OutlineService**: Story structure management, element creation, file operations
- **NavigationService**: Page navigation and view transitions
- **AutoSaveService**: Automatic saving with configurable intervals
- **BackupService**: Automated backup functionality
- **CollaboratorService**: AI-powered writing assistance
- **SearchService**: Full-text search across story content
- **LogService**: Comprehensive logging with NLog and elmah.io integration

For detailed architecture information, see [Architecture Guide](./devdocs/architecture.md).

## Important Notes

### Running the Application
- When debugging in Visual Studio, a developer menu appears with diagnostic tools
- Single instancing works but may need testing outside Visual Studio
- Missing license files won't affect functionality but will disable error reporting

### Security and Privacy
- All data stored locally under user control
- No automatic cloud sync
- GNU GPL v3 license ensures transparency
- Optional telemetry requires user consent

## Working with GitHub Issues

Claude Code can interact with GitHub issues using the `gh` CLI tool. This is essential for following the AI Issue-Centric Workflow.

### Viewing Issues
```bash
# View issue details
gh issue view 1067 --repo storybuilder-org/StoryCAD

# View issue with comments
gh issue view 1067 --comments --repo storybuilder-org/StoryCAD
```

### Updating Issues
```bash
# Edit issue body (opens in editor)
gh issue edit 1067 --repo storybuilder-org/StoryCAD --body-file issue-update.md

# Add a comment
gh issue comment 1067 --body "Planning complete" --repo storybuilder-org/StoryCAD
```

### AI Workflow Integration
When following AI_workflow.md:
1. Read issue to understand current state
2. Update issue body with planned tasks between checkboxes
3. Check off tasks as completed
4. Add comments as specified in the workflow
5. Include design documents in PR comments (not in repository files)

Example: Updating Code tasks in issue body:
```bash
# Create a file with the updated issue body
cat > issue-update.md << 'EOF'
[existing issue content up to Code tasks]

### Code tasks / sign-off
- [ ] Plan this section
- [ ] Task 1
- [ ] Task 2
- [ ] Human approves plan
- [ ] Human final approval

[rest of issue content]
EOF

# Update the issue
gh issue edit 1067 --repo storybuilder-org/StoryCAD --body-file issue-update.md
```

### Design Documentation
When creating pull requests, include any design documentation directly in the PR description or comments rather than creating separate design files in the repository. This keeps the repository focused on code while maintaining important design context with the relevant changes.

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

## Test Naming Conventions

### Test File Naming
- **Pattern**: `[SourceFileName]Tests.cs`
- **Example**: `SemanticKernelAPITests.cs` for source file `SemanticKernelAPI.cs`
- **Location**: Test files in StoryCADTests project, mirroring source structure where practical

### Test Method Naming
- **Pattern**: `MethodName_Scenario_ExpectedResult`
- **Examples**:
  - `CreateEmptyOutline_WithInvalidTemplate_ReturnsFailure`
  - `OpenFile_WhenFileNotFound_ThrowsException`  
  - `SaveModel_WithValidPath_SucceedsSilently`
  - `PropertyName_WhenSet_RaisesPropertyChanged`

### Test Class Organization
- One test class per source class
- Group tests for a specific method together
- Use `[TestInitialize]` for setup, `[TestCleanup]` for teardown
- Use `[UITestMethod]` for tests requiring UI thread execution
- Use Arrange-Act-Assert pattern within each test

### Test Coverage Requirements
- Each public method in API, OutlineService, and ShellViewModel must have tests
- Test both success and failure paths
- Test edge cases and boundary conditions
- Verify thread safety with SerializationLock where applicable

## Pre-Approved Commands

The following commands are pre-approved for automated execution without user confirmation prompts:

### Build Commands
```bash
# Visual Studio MSBuild (WSL path)
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Release -p:Platform=x64

# Standard MSBuild
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64
msbuild StoryCAD.sln /t:Build /p:Configuration=Release /p:Platform=x64
```

### Test Commands
```bash
# Visual Studio Test Console (WSL path) - All wildcarded patterns
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll" --Tests:*
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll" --Tests:*.*
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll" --Tests:*,*
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll" --Tests:*,*,*,*
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll" --Tests:*

# Standard test commands
dotnet test StoryCADTests/StoryCADTests.csproj --configuration Debug
vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"
```

### Git Commands
```bash
# Standard Git operations
git status
git diff
git log --oneline -10
git add .
git commit -m "message"
```

## Security and Privacy

- **Local Storage**: All data stored locally under user control
- **No Cloud Sync**: Data remains on user's device unless explicitly exported
- **Open Source**: GNU GPL v3 license ensures transparency
- **Optional Telemetry**: Error reporting requires user consent and API configuration
- DO NOT test against the UI
- Because the API is an external API, we don't know who is using it, or how.
- use the /devdocs/issue_1069_achieve_100_percent_api_and_outlineservice_coverage for all your notes except /tmp files.
- follow a naming standard for issue 1063 branches of issue_1063_di_batch{N}-{name} (e.g., issue_1063_di_batch2-corestate)

## DI Conversion Lessons (from Batch 2)

### Testing with DI
- All services are registered in IoC container - get them via `Ioc.Default.GetRequiredService<T>()` in tests
- Don't manually create services with `new` - let IoC handle dependency injection
- Always rebuild before running tests after DI changes
- Use `Assert.ThrowsExactly` instead of `Assert.ThrowsException` for precise API testing

### Circular Dependencies
- Watch for circular dependencies when adding constructor parameters
- If encountered, document with TODO and leave specific Ioc.Default calls rather than forcing bad design
- Example: Windowing ↔ OutlineViewModel (architectural issue documented in /devdocs/StoryCAD_architecture_notes.md)