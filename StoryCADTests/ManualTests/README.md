# StoryCAD Manual Testing Documentation

This directory contains comprehensive manual testing documentation for StoryCAD.

## Quick Start

For immediate testing needs:
- **5 minutes available**: Run [Smoke_Test.md](./Smoke_Test.md)
- **30 minutes available**: Run Core tests (File Operations, Story Elements, Navigation)
- **Feature-specific**: Generate custom test plan using [AI_Test_Plan_Generation_Prompt.md](./AI_Test_Plan_Generation_Prompt.md)

## Test Plans

### Tier 1 — Must Test (blocks release)
- [Smoke_Test.md](./Smoke_Test.md) - 5-minute basic validation (~5 min)
- [Core_File_Operations.md](./Core_File_Operations.md) - Essential file operations (~10 min)
- [Core_Story_Elements.md](./Core_Story_Elements.md) - Element creation/editing (~10 min)
- [Core_Navigation.md](./Core_Navigation.md) - Navigation and UI interaction (~10 min)
- [StoryWorld_Test_Plan.md](./StoryWorld_Test_Plan.md) - **NEW** Worldbuilding feature, all 9 tabs (~15 min)
- [Cross_Platform_File_Interchange.md](./Cross_Platform_File_Interchange.md) - **NEW** .stbx roundtrip Mac/Windows (~10 min)

### Tier 2 — Should Test
- [Copy_Elements_Test_Plan.md](./Copy_Elements_Test_Plan.md) - **NEW** Copy Elements to another outline (~10 min)
- [Reports_Test_Plan.md](./Reports_Test_Plan.md) - **NEW** Print, PDF export, Scrivener export (~15 min)
- [Tools_Test_Plan.md](./Tools_Test_Plan.md) - Tools menu functionality (~15 min)
- [Services_Test_Plan.md](./Services_Test_Plan.md) - **NEW** AutoSave, Backup, Search, Logging (~15 min)
- [Preferences_Test_Plan.md](./Preferences_Test_Plan.md) - **NEW** Settings, theme, directories (~10 min)

### Tier 3 — Nice to Test
- [Window_Management.md](./Window_Management.md) - **NEW** Resize, full-screen, responsive layout (~5 min)
- [macOS_Specific.md](./macOS_Specific.md) - **NEW** Menu bar, permissions, install (~15 min)
- [Full_Manual_Test_Plan.md](./Full_Manual_Test_Plan.md) - Complete regression suite (~30 min)

### Overview and Guides
- [Test_Suite_Overview.md](./Test_Suite_Overview.md) - Overview, tiers, and test selection guide
- [Feature_Specific_Testing_Framework.md](./Feature_Specific_Testing_Framework.md) - How to create targeted test plans
- [AI_Test_Plan_Generation_Prompt.md](./AI_Test_Plan_Generation_Prompt.md) - Generate test plans with AI

### Cross-Platform Strategy
- [UNO_Platform_Testing_Strategy.md](./UNO_Platform_Testing_Strategy.md) - Cross-platform testing approach
- [Cross_Platform_Testing_Strategy.md](./Cross_Platform_Testing_Strategy.md) - Platform-specific testing details

### UI Automation (Future)
- [UI_Automation_Design_Document.md](./UI_Automation_Design_Document.md) - Technical design for automation
- [UI_Automation_Options.md](./UI_Automation_Options.md) - Tool evaluation and recommendations
- [Feature_Request_UI_Automation.md](./Feature_Request_UI_Automation.md) - Business case for automation

## Testing Strategy for 4.0

1. **For Regular Builds**: Run Smoke Test (~5 min)
2. **For Feature PRs**: Generate feature-specific test plan (~15-30 min)
3. **For Releases**: Run Tier 1 on macOS first (~1 hr), then Tier 2 (~1 hr), then Tier 3
4. **Windows Follow-Up**: After macOS, repeat tiers on Windows with focus on StoryWorld, Copy Elements, and Print reports

## Test Data

Sample outlines are available via File > Open Sample Outline:
- "Danger Calls" - Medium complexity
- "Hamlet" - Large outline
- Create test files in `TestInputs/` folder

## Contributing

When adding new tests:
1. Use the template in each test plan file
2. Keep tests focused and time-boxed
3. Include clear pass/fail criteria
4. Update this README if adding new test categories