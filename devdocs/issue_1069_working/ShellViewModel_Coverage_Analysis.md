# ShellViewModel Test Coverage Analysis
## Current Branch: issue-1069-test-coverage
## Updated: 2025-01-15

## Summary
- **Total Public Methods**: 17+
- **Methods with Tests**: 7
- **Methods without Tests**: 10+
- **Coverage**: ~40-45%

## Public Methods and Test Status

### ✅ Tested Methods (7)

1. **TreeViewNodeClicked** (void) - COMPLETE
   - Tests: 12 tests covering all node types, null handling
   - Coverage: Character, Scene, Problem, Setting, Folder, Overview, Web, Notes, Section, TrashCan

2. **SaveModel** (void) - COMPLETE
   - Tests: 5 tests - null frame, home page, unrecognized type, overview page
   - Coverage: All page types, error scenarios

3. **ResetModel** (void) - COMPLETE
   - Test: `ResetModel_CreatesNewStoryModel`
   - Coverage: Model reset verification

4. **ViewChanged** (void) - COMPLETE
   - Tests: 5 tests - Explorer to Narrator, Narrator to Explorer, same view, null model, empty view
   - Coverage: All view transitions

5. **ShowFlyoutButtons** (void) - COMPLETE
   - Tests: 4 tests - TrashCan, Explorer, Narrator, null node
   - Coverage: All visibility scenarios

6. **Move Operations** (4 methods) - COMPLETE (Session 2025-01-15)
   - **MoveLeft**: 3 tests - valid move, root level, null node
   - **MoveRight**: 3 tests - valid move, no previous sibling, null node  
   - **MoveUp**: 4 tests - middle sibling, top of siblings, root, null node
   - **MoveDown**: 5 tests - middle sibling, bottom of siblings, trash prevention, root, null node
   - Plus 1 complex hierarchy integration test

### ❌ Missing Tests (16)

1. **CreateBackupNow** (async Task)
   - Purpose: Creates a backup with user dialog
   - Missing: Dialog confirmation, cancellation, backup creation

2. **MakeBackup** (async Task)
   - Purpose: Creates automatic backup
   - Missing: Successful backup, failure scenarios

3. **ShowChange** (static void)
   - Purpose: Marks model as changed and updates UI
   - Missing: Color change, model state update

4. **ShowDotEnvWarningAsync** (async Task)
   - Purpose: Shows warning dialog for missing .env file
   - Missing: Dialog display, user acknowledgment

5. **ShowHomePage** (void)
   - Purpose: Navigates to home page
   - Missing: Navigation verification

6. **SaveModel** (void)
   - Purpose: Saves current ViewModel to StoryElement
   - Missing: Different page types, save verification

7. **ResetModel** (void)
   - Purpose: Resets story model to new
   - Missing: Model reset verification

8. **ViewChanged** (void)
   - Purpose: Switches between Explorer and Narrator views
   - Missing: View switching, current view update

9. **ShowFlyoutButtons** (void)
   - Purpose: Controls command bar button visibility
   - Missing: Different node types, view modes

10. **LaunchGitHubPages** (void)
    - Purpose: Opens help documentation in browser
    - Missing: Process launch verification

11. **ShowConnectionStatus** (void)
    - Purpose: Shows connection status message
    - Missing: Connected/disconnected states

12. **ShowMessage** (void)
    - Purpose: Displays status messages
    - Missing: Different log levels, message display

13. **GetNextSibling** (StoryNodeItem)
    - Purpose: Gets next sibling node in tree
    - Missing: Has sibling, no sibling, null parent

14. **Move Commands** (4 methods)
    - MoveTreeViewItemLeft
    - MoveTreeViewItemRight  
    - MoveTreeViewItemUp
    - MoveTreeViewItemDown
    - Missing: All movement scenarios, validation, boundaries

## Command Test Coverage

### RelayCommands (Not Tested)
- TogglePaneCommand
- OpenFileCommand
- SaveFileCommand
- SaveAsCommand
- CreateBackupCommand
- CloseCommand
- ExitCommand
- CollaboratorCommand
- HelpCommand
- Tool Commands (KeyQuestions, Topics, MasterPlots, etc.)
- Add Element Commands (Folder, Section, Problem, etc.)
- Remove/Restore Commands

## Property Test Coverage

### Properties (Not Tested)
- IsPaneOpen
- ExplorerVisibility
- NarratorVisibility
- TrashButtonVisibility
- StatusMessage
- StatusColor
- CurrentNode
- RightTappedNode

## Critical Gaps

1. **Navigation Logic**: TreeViewNodeClicked only partially tested
2. **Save/Load Operations**: SaveModel not tested
3. **View Management**: ViewChanged not tested
4. **Move Operations**: All 4 move methods untested
5. **Commands**: No command execution tests

## Recommendations

1. **Highest Priority**:
   - SaveModel - Critical for data persistence
   - TreeViewNodeClicked - Core navigation
   - ViewChanged - View switching logic

2. **High Priority**:
   - Move operations (4 methods)
   - ShowFlyoutButtons - UI state management

3. **Medium Priority**:
   - Backup operations
   - Status/messaging methods
   - Command testing

4. **Test Approach**:
   - Use mocking for services (NavigationService, OutlineService)
   - Test UI state changes separately from business logic
   - Verify command execution through CanExecute/Execute