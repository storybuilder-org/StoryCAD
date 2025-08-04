# StoryCAD Development Guide

This file provides project-specific guidance for StoryCAD development. Universal development standards are defined in `/dev/src/CLAUDE.md` and automatically included.

## About StoryCAD

StoryCAD is a free, open-source Windows application for fiction writers that provides structured outlining tools. It's described as "CAD for fiction writers" and helps writers manage the complexity of plotted fiction through systematic story development.

## Quick Reference

### Technology Stack
- **Framework**: .NET 8.0 with WinUI 3, Windows App SDK 1.6
- **Architecture**: MVVM with CommunityToolkit.Mvvm
- **Testing**: MSTest with WinUI support
- **Platform**: Windows 10/11 (minimum 10.0.19041.0)

### Project Structure
1. **StoryCAD** - Main WinUI 3 application (executable)
2. **StoryCADLib** - Core business logic library (NuGet package)
3. **StoryCADTests** - MSTest test project

## Development Guides

### Core Documentation
- [Architecture Guide](./.claude/architecture.md) - MVVM patterns, data binding, services
- [Build & Test Commands](./.claude/build_commands.md) - WSL/Windows build instructions
- [Testing Guide](./.claude/testing.md) - Test patterns and data creation
- [User Manual Index](/mnt/c/temp/user_manual.md) - User documentation structure

### Workflow Documentation
- [AI Workflow](/mnt/c/temp/AI_Workflow.md) - Issue-centric development process

## Quick Start

### Build from WSL
```bash
# Build solution
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64

# Run all tests
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net8.0-windows10.0.19041.0/StoryCADTests.dll"
```

For detailed commands, see [Build & Test Commands](./.claude/build_commands.md).

## Key Services

- **OutlineService**: Story structure management, element creation, file operations
- **NavigationService**: Page navigation and view transitions
- **AutoSaveService**: Automatic saving with configurable intervals
- **BackupService**: Automated backup functionality
- **CollaboratorService**: AI-powered writing assistance
- **SearchService**: Full-text search across story content
- **LogService**: Comprehensive logging with NLog and elmah.io integration

For detailed architecture information, see [Architecture Guide](./.claude/architecture.md).

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