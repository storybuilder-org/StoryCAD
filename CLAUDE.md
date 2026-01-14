# StoryCAD Development Guide

This file provides project-specific guidance for StoryCAD development.

**BEFORE ANY IMPLEMENTATION WORK:**

- You must present a plan.
- You must ask for explicit approval.
- You must not proceed until approval is given.



## **Simplicity First**

**When solving StoryCAD problems, always choose the simplest approach:**
- Direct method calls over reflection
- Concrete types over abstractions (until needed)
- Built-in .NET features over external libraries
- Straightforward debugging (`Debugger.Launch()`) over complex conditions
- Clear, obvious code over clever solutions

## Platform Development (UNO)

### Multi-Targeting Strategy

**Windows builds both targets**:
- `net10.0-windows10.0.22621` (WinAppSDK head) - Windows 10/11, Windows Store
- `net10.0-desktop` (Desktop head) - Windows 7/8/10/11, macOS, Linux

**macOS builds desktop only**:
- `net10.0-desktop` (Desktop head) - macOS, also runs on Windows/Linux

**Critical Concept - Platform-Specific Code**:

When using **cross-platform APIs only** (standard .NET, UNO controls):
- ✅ Build on either platform → Universal binary runs everywhere

When using **platform-specific APIs** (AppKit, Win32, etc.):
- ⚠️ Build on Windows → Excludes Mac-specific code (can't compile AppKit)
- ⚠️ Build on Mac → Excludes Windows-specific code (can't compile Win32)
- 📦 Need platform-specific builds for production distribution

**Example**:
```csharp
#if __MACOS__
using AppKit;  // This code ONLY compiles on Mac
public void SetupMacMenu() { }
#endif
```

See `/devdocs/platform_targeting_guidelines.md` for complete workflows and decision trees.

### Cross-Machine Development Workflow

**Primary Strategy**: Develop on Windows (VS2026), test on Mac (Rider)

1. **Code on Windows**:
   - Write code in VS2026 (familiar environment)
   - Test on Windows (WinAppSDK head)
   - Commit and push

2. **Test on Mac**:
   ```bash
   git pull
   dotnet run -f net10.0-desktop  # Quick verification
   ```

3. **Debug on Mac** (if needed):
   - Open in Rider on Mac Mini
   - Debug Mac-specific issues
   - Commit fixes and pull back to Windows

**Alternative Workflows**:
- Network share (rapid iteration, must copy to local for build)
- Separate build folders (simultaneous debugging)

See `devdocs/platform_targeting_guidelines.md` for detailed workflow comparison.

### When to Build on Which Platform

Use this decision tree to determine where to develop:

| Issue Type | Build Platform | Test Platform | Rationale |
|------------|----------------|---------------|-----------|
| UNO Platform bugs | Windows | Both | Cross-platform testing required |
| Pure XAML/UI | Windows | Both | Faster in familiar IDE |
| Business logic | Windows | Both | No platform-specific code |
| Mac-specific features | Mac | Mac only | Requires AppKit/Mac APIs |
| Windows-specific features | Windows | Windows only | Requires Win32/WinAppSDK APIs |
| Performance optimization | Both | Both | Platform differences matter |

**Key Points**:
- ~90% of StoryCAD code is shared/cross-platform → Develop on Windows
- Mac-specific features (AppKit menus, sandboxing) → Must build on Mac
- Always test on both platforms before merging to UNOTestBranch

### Platform-Specific Testing

**Quick Manual Test (Mac)**:
```bash
# No IDE needed:
dotnet run -f net10.0-desktop
```

**Debugging (Mac)**:
```bash
# Must build on Mac, use Rider:
rider ~/Documents/dev/src/StoryCAD/StoryCAD.sln
# Set breakpoints and debug normally
```

**Important**: Cannot remote debug from Windows to Mac. Use logging for remote troubleshooting or debug locally on Mac.

### Working with UNO Platform
When developing on the UNOTestBranch:
- **Shared Code First**: Write platform-agnostic code whenever possible
- **Platform-Specific Code**:
  - Use partial classes: `Windowing.WinAppSDK.cs` for Windows, `Windowing.desktop.cs` for macOS
  - Use conditional compilation: `#if HAS_UNO_WINUI` for Windows, `#if __MACOS__` for macOS
- **Testing**: Test on both Windows and macOS before committing
- **File Paths**: Use platform-agnostic paths (avoid hardcoded Windows paths)
- **Keyboard Shortcuts**: Support both Ctrl (Windows) and Cmd (macOS) patterns

### UNO Platform Resources
- [UNO Platform Documentation](https://platform.uno/docs/) - Main documentation
- [Platform-Specific C#](https://platform.uno/docs/articles/platform-specific-csharp.html) - Partial classes and conditional compilation
- [Getting Started with UNO](https://platform.uno/docs/articles/get-started.html) - Setup and basics
- [WinUI to UNO Migration](https://platform.uno/docs/articles/howto-migrate-existing-code.html) - Migration guidance
- [Supported Features](https://platform.uno/docs/articles/supported-features.html) - API compatibility matrix
- [UNO Platform GitHub](https://github.com/unoplatform/uno) - Source code and issues
- [Migration Guide](https://github.com/orgs/storybuilder-org/projects/6) - StoryCAD specific migration project
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
- **Framework**: .NET 10.0 with UNO Platform 5.x
- **UI Framework**: WinUI 3 (cross-platform via UNO)
- **Platform Targets**:
  - **Windows**: WinAppSDK head (Windows 10/11, minimum 10.0.19041.0)
  - **macOS**: Desktop head (macOS 10.15+)
  - **Future**: Linux, WebAssembly, iOS, Android
- **Architecture**: MVVM with CommunityToolkit.Mvvm
- **Testing**: MSTest with UNO Platform support
- **Shared Code**: ~90% across all platforms

### Production Stack (Main Branch - Version 3.x)
- **Framework**: .NET 10.0 with WinUI 3, Windows App SDK 1.6
- **Platform**: Windows 10/11 only

### Project Structure
1. **StoryCAD** - Main WinUI 3 application (executable)
2. **StoryCADLib** - Core business logic library (NuGet package)
3. **StoryCADTests** - MSTest test project

## Documentation Resources

### Core Memory Files (`/home/tcox/.claude/memory/`)

Essential architecture and development documentation accessible to all sessions:

- **[patterns.md](/home/tcox/.claude/memory/patterns.md)** - 9 architectural patterns (MVVM, ISaveable, SerializationLock, OperationResult, Stateless Services, IMessenger, Platform-Specific Code, StoryDocument, DI)
- **[architecture.md](/home/tcox/.claude/memory/architecture.md)** - Critical dated decisions (2025-10-12), circular dependencies, navigation lifecycle, StoryDocument never-null rule, theme management
- **[gotchas.md](/home/tcox/.claude/memory/gotchas.md)** - Common pitfalls, headless mode behavior, platform-specific gotchas, ISaveable registration, navigation timing
- **[cross-platform.md](/home/tcox/.claude/memory/cross-platform.md)** - Cross-platform development workflows, multi-targeting, when to build where
- **[testing.md](/home/tcox/.claude/memory/testing.md)** - Test patterns, naming conventions (MethodName_Scenario_ExpectedResult), TDD workflow, test data creation
- **[build-commands.md](/home/tcox/.claude/memory/build-commands.md)** - WSL/Windows/Mac build and test commands, TDD red-green-refactor cycle, cross-machine workflow
- **[dependencies.md](/home/tcox/.claude/memory/dependencies.md)** - UNO Platform 6.2.36, MVVM Toolkit 8.4.0, Semantic Kernel 1.41.0 usage patterns

**Usage**: All memory files cross-reference each other. Start with patterns.md for quick lookup, then drill into specific topics.

### Comprehensive Documentation (`.claude/docs/`)

Detailed technical documentation (memory files provide condensed versions):

- **Architecture**: `.claude/docs/architecture/` - StoryCAD_architecture_notes.md (comprehensive), coding-standards.md, debugging-guide.md, ai-workflow.md
- **Dependencies**: `.claude/docs/dependencies/` - Full dependency guides with complete API examples
- **Code Examples**: `.claude/docs/examples/` - Copy-paste templates for ViewModels, Services, platform-specific code
- **Troubleshooting**: `.claude/docs/troubleshooting/` - Common errors with solutions

### User Manual (ManualTest Repository)

- **Repository**: [github.com/storybuilder-org/ManualTest](https://github.com/storybuilder-org/ManualTest)
- **AI Agent Guide**: [ManualTest/CLAUDE.md](/mnt/d/dev/src/ManualTest/CLAUDE.md) - Comprehensive guidance for working with user documentation
- **Location**: `/mnt/d/dev/src/ManualTest/docs/`
- **Published (Staging)**: https://storybuilder-org.github.io/StoryBuilder-Manual/
- **Format**: Jekyll with Just the Docs theme (115 markdown files)
- **Index**: `/mnt/d/dev/src/ManualTest/docs/index.md` - Complete index with file summaries (always current)
- **Audience**: Fiction writers (non-technical users)

**Search Strategy**:
- **DO NOT** read all 115 files sequentially
- **DO** use index file for quick lookup
- **DO** use Grep to search specific sections
- **DO** read ManualTest/CLAUDE.md for detailed search guidance

**Key Manual Sections**:
- `Front Matter/` - Introduction, help resources, legal information (4 files)
- `Quick Start/` - UI basics, navigation, file operations, keyboard shortcuts (23 files)
- `Story Elements/` - Forms and tabs for each story element type (30 files)
- `Tools/` - Master Plots, Dramatic Situations, Stock Scenes, Conflict Builder (10 files)
- `Writing with StoryCAD/` - Outlining philosophy, workflow, plotting techniques (17 files)
- `Tutorial Creating a Story/` - Step-by-step story creation guide (10 files)
- `Reports/` - Print reports and Scrivener integration (4 files)
- `Preferences/` - Application settings and configuration (1 file)
- `For Developers/` - API documentation, changelog, developer notes (6 files)

**When to Reference User Documentation**:
- Before implementing UI changes (understand user workflows)
- When adding new story elements (follow established patterns)
- When changing features (update corresponding user docs)
- See architecture notes for feature-to-documentation mapping

### Context7 MCP Server

StoryCAD uses Context7 MCP server for up-to-date documentation of public dependencies:
- **Status**: Configured in `/home/tcox/.claude.json`
- **Provides**: Latest docs for UNO Platform, Semantic Kernel, MVVM Toolkit, and other NuGet packages
- **Usage**: Query Context7 for dependency-specific questions

## Quick Start

### Build from WSL
```bash
# Build solution
"/mnt/c/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64

# Run all tests (use vstest.console.exe - has access to Windows environment variables)
"/mnt/c/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll"
```

For detailed commands, see [Build & Test Commands](/home/tcox/.claude/memory/build-commands.md).

## Key Services

- **OutlineService**: Story structure management, element creation, file operations
- **NavigationService**: Page navigation and view transitions
- **AutoSaveService**: Automatic saving with configurable intervals
- **BackupService**: Automated backup functionality
- **CollaboratorService**: AI-powered writing assistance
- **SearchService**: Full-text search across story content
- **LogService**: Comprehensive logging with NLog and elmah.io integration

For detailed architecture information, see [Architecture Memory](/home/tcox/.claude/memory/architecture.md) and [Patterns](/home/tcox/.claude/memory/patterns.md).

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

**PIE Workflow Implementation**: The enterprise PIE policy (Plan-Implement-Evaluate) from `/etc/claude-code/CLAUDE.md` is implemented for StoryCAD via **`.claude/docs/architecture/ai-workflow.md`**. This issue-centric workflow defines how to plan, implement, and evaluate work within GitHub issues.

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
When following AI_workflow.md (PIE implementation):

**Plan Phase**:
1. Read issue to understand current state
2. Recommend specialized agents for complex tasks in the plan
3. Update issue body with planned tasks between checkboxes (include agent recommendations)
4. Get plan approval before proceeding

**Implement Phase**:
5. Use specialized agents proactively for approved tasks (no additional approval needed)
6. Check off tasks as completed
7. Add comments as specified in the workflow

**Evaluate Phase**:
8. Include design documents in PR comments (not in repository files)
9. Document agent effectiveness and lessons learned

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
# Visual Studio 2026 MSBuild (WSL path)
"/mnt/c/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64
"/mnt/c/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Release -p:Platform=x64

# Standard MSBuild
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64
msbuild StoryCAD.sln /t:Build /p:Configuration=Release /p:Platform=x64
```

### Test Commands
```bash
# Use vstest.console.exe - has access to Windows environment variables needed for integration tests

# From WSL (recommended)
"/mnt/c/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll"

# Run specific test class
"/mnt/c/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll" /Tests:StoryModelTests
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
- Example: Windowing ↔ OutlineViewModel (documented in /home/tcox/.claude/memory/architecture.md)
- Do not memorise or document any information about Storybuilder-miscellaneous in the StoryCAD Repo.