# StoryCAD Manual Testing Documentation

This directory contains comprehensive manual testing documentation for StoryCAD.

## Quick Start

For immediate testing needs:
- **5 minutes available**: Run [Smoke_Test.md](./Smoke_Test.md)
- **30 minutes available**: Run Core tests (File Operations, Story Elements, Navigation)
- **Feature-specific**: Generate custom test plan using [AI_Test_Plan_Generation_Prompt.md](./AI_Test_Plan_Generation_Prompt.md)

## Test Plans

### Graduated Test Suites
- [Test_Suite_Overview.md](./Test_Suite_Overview.md) - Overview and test selection guide
- [Smoke_Test.md](./Smoke_Test.md) - 5-minute basic validation
- [Core_File_Operations.md](./Core_File_Operations.md) - Essential file operations (10 min)
- [Core_Story_Elements.md](./Core_Story_Elements.md) - Element creation/editing (10 min)
- [Core_Navigation.md](./Core_Navigation.md) - Navigation and UI interaction (10 min)
- [Tools_Test_Plan.md](./Tools_Test_Plan.md) - Tools menu functionality (15 min)
- [Full_Manual_Test_Plan.md](./Full_Manual_Test_Plan.md) - Complete regression suite (3-4 hours)

### Feature-Specific Testing
- [Feature_Specific_Testing_Framework.md](./Feature_Specific_Testing_Framework.md) - How to create targeted test plans
- [AI_Test_Plan_Generation_Prompt.md](./AI_Test_Plan_Generation_Prompt.md) - Generate test plans with AI

## Future Planning

### UI Automation (Post-3.3)
- [UI_Automation_Design_Document.md](./UI_Automation_Design_Document.md) - Technical design for automation
- [UI_Automation_Options.md](./UI_Automation_Options.md) - Tool evaluation and recommendations
- [Feature_Request_UI_Automation.md](./Feature_Request_UI_Automation.md) - Business case for automation

### UNO Platform (4.0)
- [UNO_Platform_Testing_Strategy.md](./UNO_Platform_Testing_Strategy.md) - Cross-platform testing approach

## Important Notes for 3.3 Release

### ⚠️ What NOT to Do
- **Don't invest in WinUI 3-specific UI automation** - Won't transfer to UNO Platform
- **Don't use WinAppDriver** - It's deprecated/abandoned
- **Don't create brittle UI tests** - They'll break with 4.0 migration

### ✅ What TO Do
- **Use these manual test plans** - They adapt to any platform
- **Focus on unit tests** - They survive platform changes
- **Document current behavior** - For comparison after migration

## Testing Strategy

1. **For Regular Builds**: Run smoke test (5 min)
2. **For Feature PRs**: Generate feature-specific test plan (15-30 min)
3. **For Releases**: Run appropriate test suite based on risk
4. **For 4.0 Migration**: See UNO Platform strategy document

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