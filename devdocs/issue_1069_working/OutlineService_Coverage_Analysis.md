# OutlineService Test Coverage Analysis
## Current Branch: issue-1069-test-coverage
## Updated: 2025-01-15

## Summary
- **Total Public Methods**: 32 (verified from source)
- **Methods with Tests**: 21
- **Methods without Tests**: 11
- **Estimated Coverage**: ~80-85%

## Public Methods and Test Status

### ✅ Tested Methods (15)

1. **CreateModel** (async Task<StoryModel>)
   - Tests: `CreateModel_BasicTemplate0_ShouldCreateMinimalModel`, `CreateModel_Template1_ShouldAddProblemAndCharacters`
   - Coverage: Different templates, basic structure

2. **WriteModel** (async Task)
   - Tests: `WriteModel_ShouldWriteFileSuccessfully`, `WriteModel_InvalidPath_ThrowsException`
   - Coverage: Success and error paths

3. **OpenFile** (async Task<StoryModel>)
   - Tests: `OpenFile_FileNotFound_ShouldThrowFileNotFoundException`, `OpenFile_ValidFile_ShouldReturnStoryModel`
   - Coverage: File exists and missing scenarios

4. **SetCurrentView** (void)
   - Tests: `SetCurrentView_SwitchToExplorerView_UpdatesModelCorrectly`, `SetCurrentView_SwitchToNarratorView_UpdatesModelCorrectly`, `SetCurrentView_NullModel_ThrowsArgumentNullException`, `SetCurrentView_InvalidViewType_ThrowsArgumentException`, `SetCurrentView_UsesSerializationLock_NoExceptions`
   - Coverage: All view types, error conditions, thread safety

5. **SetChanged** (void)
   - Tests: `SetChanged_ShouldUpdateModelChangedStatus`, `SetChanged_ShouldUseSerializationLock`
   - Coverage: State change, thread safety

6. **GetStoryElementByGuid** (StoryElement)
   - Tests: `GetStoryElementByGuid_ValidGuid_ReturnsElement`, `GetStoryElementByGuid_EmptyGuid_ThrowsException`, `GetStoryElementByGuid_NonExistentGuid_ThrowsException`
   - Coverage: Valid, empty, and non-existent GUIDs

7. **UpdateStoryElement** (void)
   - Tests: `UpdateStoryElement_ValidElement_UpdatesSuccessfully`, `UpdateStoryElement_NullModel_ThrowsException`, `UpdateStoryElement_NullElement_ThrowsException`, `UpdateStoryElement_ValidElement_MarksModelAsChanged`
   - Coverage: Success, null checks, side effects

8. **UpdateStoryElementByGuid** (void)
   - Tests: `UpdateStoryElementByGuid_ValidGuid_UpdatesElement`, `UpdateStoryElementByGuid_NullModel_ThrowsException`, `UpdateStoryElementByGuid_EmptyGuid_ThrowsException`, `UpdateStoryElementByGuid_NullElement_ThrowsException`, `UpdateStoryElementByGuid_NonExistentGuid_ThrowsException`
   - Coverage: Comprehensive error scenarios

9. **GetCharacterList** (List<CharacterModel>)
   - Tests: `GetCharacterList_WithCharacters_ReturnsAllCharacters`, `GetCharacterList_NoCharacters_ReturnsEmptyList`, `GetCharacterList_NullModel_ThrowsException`, `GetCharacterList_UsesSerializationLock_NoExceptions`
   - Coverage: With/without data, thread safety

10. **GetSettingsList** (List<SettingModel>)
    - Tests: `GetSettingsList_WithSettings_ReturnsAllSettings`, `GetSettingsList_NoSettings_ReturnsEmptyList`, `GetSettingsList_NullModel_ThrowsException`
    - Coverage: With/without data, null checks

11. **GetAllStoryElements** (List<StoryElement>)
    - Tests: `GetAllStoryElements_ReturnsAllElements`, `GetAllStoryElements_EmptyModel_ReturnsEmptyList`, `GetAllStoryElements_NullModel_ThrowsException`, `GetAllStoryElements_UsesSerializationLock_NoExceptions`
    - Coverage: All scenarios, thread safety

12. **Beat Management** (5 methods)
    - CreateBeat, DeleteBeat, AssignBeat, UnassignBeat, SetBeatSheet
    - Tests: Full coverage in OutlineServiceBeatTests.cs
    - Coverage: Create, delete, assign, unassign, replace operations

### ✅ Recently Added Tests (Session 2025-01-15)

13. **AddStoryElement** (StoryElement)
    - Tests: 15 tests covering all element types, parent validation, error cases
    - Coverage: COMPLETE - Character, Scene, Problem, Setting, Folder, Web, Notes, Section

14. **MoveToTrash** (void) - Replaces RemoveStoryElement
    - Tests: 6 tests - success, with children, with references, null checks, root/trash validation
    - Coverage: COMPLETE - Refactored to throw exceptions per API pattern

15. **RestoreFromTrash** (void) - Replaces RestoreStoryElement  
    - Tests: 5 tests - restore to explorer, with children, nested items, validation
    - Coverage: COMPLETE - Refactored to throw exceptions per API pattern

16. **EmptyTrash** (void)
    - Tests: 3 tests - with items, empty trash, null model
    - Coverage: COMPLETE - Refactored to throw exceptions per API pattern

### ❌ Missing Tests (11 methods)

#### High Priority - Used by API
1. **SearchForText** (List<StoryElement>)
   - Purpose: Search story elements for text
   - Called by: SemanticKernelAPI.cs:598

2. **SearchForUuidReferences** (List<StoryElement>)
   - Purpose: Find elements referencing a UUID
   - Called by: SemanticKernelAPI.cs:636

3. **RemoveUuidReferences** (int)
   - Purpose: Remove all references to a UUID
   - Called by: SemanticKernelAPI.cs:675

4. **SearchInSubtree** (List<StoryElement>)
   - Purpose: Search within a subtree
   - Called by: SemanticKernelAPI.cs:716

5. **AddRelationship** (bool)
   - Purpose: Add character relationships
   - Called by: SemanticKernelAPI.cs:573

6. **AddCastMember** (bool)
   - Purpose: Add character to scene
   - Called by: SemanticKernelAPI.cs:542

#### Medium Priority - Used by ViewModels
7. **ConvertProblemToScene** (SceneModel)
   - Purpose: Convert problem to scene
   - Called by: OutlineViewModel, ShellViewModel commands

8. **ConvertSceneToProblem** (ProblemModel)
   - Purpose: Convert scene to problem
   - Called by: OutlineViewModel, ShellViewModel commands

9. **FindElementReferences** (List<StoryElement>)
   - Purpose: Find references to an element
   - Called by: OutlineViewModel

#### Low Priority - Internal/May Have Partial Tests
10. **RemoveReferenceToElement** (bool)
    - Purpose: Remove references to element
    - Called internally by MoveToTrash

11. **RemoveElement** (bool)
    - Purpose: Unknown - no callers found
    - Status: Possibly deprecated

#### Note: Methods That Don't Exist
The following were in original analysis but don't exist as public methods:
- MoveStoryElement (movement is handled by ShellViewModel)
- CopyToNarrative (not found)
- RemoveFromNarrative (not found)

## Missing Search Methods (if not in SearchService)

1. SearchForText
2. SearchForUuidReferences
3. RemoveUuidReferences
4. SearchInSubtree

## Test Coverage by Category

### Well Covered
- File operations (Create, Open, Write)
- View management
- Element retrieval and updates
- Beat management
- List operations

### Gaps
- Element lifecycle (Add, Remove, Restore)
- Tree operations (Move, Copy)
- Relationship management
- Reference management
- Search operations

## Recommendations

1. **Critical Priority**:
   - AddStoryElement - Core functionality
   - RemoveStoryElement/RestoreStoryElement - Trash operations
   - MoveStoryElement - Tree manipulation

2. **High Priority**:
   - Narrative operations (Copy/Remove)
   - Relationship management
   - Reference cleanup

3. **Medium Priority**:
   - Search operations (if not covered elsewhere)
   - EmptyTrash

4. **Test Strategy**:
   - Use test data builders for complex models
   - Test parent/child relationships thoroughly
   - Verify SerializationLock usage
   - Test undo/redo implications