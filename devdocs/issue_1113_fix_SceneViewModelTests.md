# Issue #1113: Fix SceneViewModelTests

**Date:** 2025-10-04
**Branch:** UNOTestBranch
**Status:** Initial Fix Complete - 7 tests passing, 50+ tests stubbed for future implementation

---

## Problem

SceneViewModelTests.cs was fundamentally broken and didn't actually test SceneViewModel:

1. **Unnecessary Fake Implementations** - Contained FakeLogService and FakeStoryIO that didn't properly implement interfaces
2. **Testing Private Methods** - Attempted to test `UpdateViewpointCharacterTip()` which is private
3. **Mismatch with Actual API** - Tests didn't match SceneViewModel's actual public interface
4. **Compilation Errors** - Fake classes failed to compile due to missing interface members

The test file appeared to be either copy-pasted from a different test and never adapted, or orphaned after significant changes to SceneViewModel.

---

## Solution Approach

Used Claude Code agents to:
1. Analyze SceneViewModel.cs for complete public API (68 members: 12 methods, 56 properties)
2. Generate comprehensive test infrastructure following existing patterns (ProblemViewModelTests.cs)
3. Validate generated tests compile and pass with actual production code
4. Demonstrate TDD workflow for adding additional tests

### Agents Used

- **test-automator**: Coverage analysis and test code generation
- **tdd-orchestrator**: TDD workflow demonstration

---

## What Was Generated

### Test Infrastructure (Complete)

**File:** `/mnt/d/dev/src/StoryCAD/StoryCADTests/ViewModels/SceneViewModelTests.cs`

**Test Setup:**
- TestInitialize/TestCleanup with proper setup/teardown
- 5 helper methods:
  - `SetupViewModelWithStory()` - Creates ViewModel with AppState configured
  - `CreateTestStoryModel()` - Creates StoryModel with test characters and settings
  - `CreateTestSceneModel()` - Creates SceneModel with realistic data
  - `CreateTestCharacter(name)` - Creates test CharacterModel
  - `CreateTestSetting(name)` - Creates test SettingModel

**Key Pattern:**
- Uses IoC container (`Ioc.Default.GetService<T>()`) - no fake implementations
- Follows ProblemViewModelTests pattern
- Arrange-Act-Assert structure throughout

### Fully Implemented Tests (7)

**Priority 1: Core Lifecycle (3 tests)**
1. `Activate_WithValidSceneModel_LoadsAllProperties` - Verifies 13 properties load from model
2. `SaveModel_WithModifiedProperties_UpdatesSceneModel` - Verifies changes save back
3. `Deactivate_AfterModifications_SavesChanges` - Verifies deactivation triggers save

**Priority 2: Cast Management (3 tests)**
4. `AddCastMember_WithValidCharacter_AddsToList` - Verifies character added
5. `RemoveCastMember_WithExistingCharacter_RemovesFromList` - Verifies removal
6. `AddCastMember_WithEmptyGuid_DoesNotAdd` - Verifies invalid rejection

**Priority 3: Special Property Behaviors (1 test - added via TDD)**
7. `Name_WhenChanged_SendsNameChangedMessage` - Verifies Name property sends NameChangedMessage

### Stubbed Tests (50+ with TODO comments)

**Priority 2:** 7 additional cast management tests
**Priority 3:** 9 special property behavior tests
**Priority 4:** 20+ property change notification tests
**Priority 5:** 4 scene purposes management tests
**Priority 6:** 4 constructor initialization tests

All stubbed tests have:
- Clear test names describing scenario and expected outcome
- TODO comments grouping related tests
- Organized by priority (based on impact)

---

## Test Results

**Build:** ✅ SUCCESS (0 errors, 0 warnings)

**Test Execution:**
```
Total tests: 7
     Passed: 7
 Total time: 0.38 seconds
```

**All Tests:**
- ✅ Activate_WithValidSceneModel_LoadsAllProperties - 58 ms
- ✅ SaveModel_WithModifiedProperties_UpdatesSceneModel - 1 ms
- ✅ Deactivate_AfterModifications_SavesChanges - <1 ms
- ✅ AddCastMember_WithValidCharacter_AddsToList - <1 ms
- ✅ RemoveCastMember_WithExistingCharacter_RemovesFromList - <1 ms
- ✅ AddCastMember_WithEmptyGuid_DoesNotAdd - <1 ms
- ✅ Name_WhenChanged_SendsNameChangedMessage - 1 ms

---

## Key Achievements

1. **Eliminated Broken Tests** - Removed fake implementations and private method tests
2. **Production-Ready Tests** - Generated tests compile and pass without modification
3. **Comprehensive Coverage Plan** - 68 public members catalogued with prioritized implementation plan
4. **Proper Patterns** - Follows IoC container pattern from other ViewModel tests
5. **TDD Demonstration** - Validated red-green-refactor workflow works with build infrastructure

---

## Next Steps

### Immediate (Issue #1113 Resolution)
- [x] Remove broken test file
- [x] Generate new test infrastructure
- [x] Validate tests compile and pass
- [x] Demonstrate TDD workflow

### Follow-up (Incremental Improvement)
- [ ] Implement Priority 3 tests (10 tests - special property behaviors)
- [ ] Implement Priority 4 tests (20+ tests - property change notifications)
- [ ] Implement Priority 5 tests (4 tests - scene purposes management)
- [ ] Implement Priority 6 tests (4 tests - constructor initialization)
- [ ] Add additional Priority 2 tests (7 tests - cast management edge cases)

**Estimated Remaining Work:** 50+ tests, can be implemented incrementally following existing patterns.

**Recommendation:** Implement 2-3 tests per work session to steadily increase coverage without overwhelming development.

---

## References

- **GitHub Issue:** [#1113 - Fix broken SceneViewModelTests](https://github.com/storybuilder-org/StoryCAD/issues/1113)
- **Agent Experiment Log:** `/mnt/c/temp/test-agents/EXPERIMENT_LOG.md` (detailed testing process)
- **Test Coverage Analysis:** `/mnt/c/temp/test-agents/test3-coverage-analysis-results.md`
- **Generated Test Code:** `/mnt/c/temp/test-agents/ViewModels/SceneViewModelTests-GENERATED.cs` (original)
- **Reference Pattern:** `/mnt/d/dev/src/StoryCAD/StoryCADTests/ViewModels/ProblemViewModelTests.cs`

---

## Build Commands

From `/mnt/d/dev/src/StoryCAD`:

**Build:**
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64
```

**Run SceneViewModelTests:**
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621/StoryCADTests.dll" --Tests:SceneViewModelTests
```

See [build_commands.md](./build_commands.md) for additional commands.

---

## Notes

- All agent outputs saved to `/mnt/c/temp/test-agents/` for reference
- Agent configuration updated to reference StoryCAD devdocs (architecture.md, testing.md, coding.md)
- No production code changes required - SceneViewModel implementation was already correct
- Test infrastructure ready for incremental expansion
