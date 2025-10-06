## Problem Statement
We have a backlog of compile warnings, dead code, TODOs and other non-critical code issues.

## Proposed Solution
A systematic cleanup of the codebase. Most of these will be refactoring or unused code deletions, but full system tests should occur periodically and at the conclusion of the cleanup.

## Reference Documentation

The following analysis documents have been created in `/devdocs/`:
- **issue_1134_build_warnings_analysis.md** - Comprehensive analysis of 57+ build warnings
- **issue_1134_dead_code_warnings.md** - Detailed breakdown of actual dead code (CS0169, CS0414, CS0168)
- **issue_1134_TODO_list.md** - Catalog of 50 TODO comments requiring attention
- **issue_1134_legacy_constructors.md** - 21 classes with legacy DI constructors to remove
- **issue_1134_resharper_guide.md** - Complete guide for using ReSharper/Rider for cleanup

**Note**: UNO Platform API compatibility warnings (Uno0001) are tracked separately in **issue #1139**.

## Alternatives Considered

**Option 1: Manual cleanup without tools**
- Pros: No additional software needed
- Cons: Time-consuming (8-12 hours estimated), error-prone

**Option 2: ReSharper/Rider automated cleanup** (Selected)
- Pros: Fast (2-4 hours), accurate, repeatable
- Cons: Requires license (free command-line tools available)

**Option 3: Roslyn analyzers only**
- Pros: Built into .NET SDK, free
- Cons: Less powerful than ReSharper, requires manual fixes

## Acceptance Criteria

- [ ] All CS0169 warnings (unused fields) resolved - 2 instances
- [ ] All CS0414 warnings (assigned but never used) resolved - 2 instances
- [ ] All CS0168 warnings (unused variables) resolved - 1 instance
- [ ] All CS0105 warnings (duplicate usings) resolved - 5 instances in ShellViewModel
- [ ] All CS0618 warnings (obsolete APIs) resolved - 4 instances in PrintReportDialogVM
- [ ] All 21 legacy constructors removed or documented
- [ ] Critical architectural TODOs documented with resolution plan
- [ ] Build produces zero warnings in Release configuration (excluding Uno0001 - see issue #1139)
- [ ] All existing tests pass
- [ ] Full regression test completed on Windows
- [ ] Full regression test completed on macOS (UNO platform)

---

## Lifecycle

### Design / sign-off

- [x] Plan this section
- [x] Human approves plan

#### Phase 1: Baseline Analysis (No Code Changes)

**Objective**: Establish current state and create tracking mechanisms

- [ ] **Task 1.1**: Run baseline build and capture all warnings
  
  ```bash
  msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64 /fl /flp:WarningsOnly > baseline_warnings.log
  ```
  - Expected: 57+ warnings (duplicates across target frameworks)
  - Document in: `/devdocs/issue_1134_baseline.md`
  
- [ ] **Task 1.2**: Run ReSharper inspection (or use free command-line tool)
  
  ```bash
  inspectcode.exe StoryCAD.sln --output=baseline_inspection.xml
  ```
  - Generate HTML report for review
  - Count issues by severity (Error/Warning/Suggestion)
  
- [ ] **Task 1.3**: Identify test coverage gaps
  - Run existing test suite: Record pass/fail count
  - Note: No new tests in this phase, just baseline

**Deliverables**:

- `/devdocs/issue_1134_baseline.md` (baseline metrics)
- Baseline test results

**Tracking**: Use GitHub issue checkboxes and mark items complete in the reference markdown files as work progresses. The existing analysis documents serve as the tracking system.

**Testing**: Run full test suite to establish baseline
```bash
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64
vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"
```

- [ ] Human approves plan
- [ ] Human final approval

---

### Implementation (TDD) / sign-off

- [ ] Plan this section
- [ ] Human approves plan

#### Phase 2: Configuration & Analyzer Setup (Non-Breaking)

**Objective**: Add analyzer rules and configuration without changing code

- [ ] **Task 2.1**: Update `.editorconfig` with dead code analyzers
  ```ini
  [*.cs]
  # Dead code detection
  dotnet_diagnostic.CS0169.severity = warning
  dotnet_diagnostic.CS0414.severity = warning
  dotnet_diagnostic.CS0168.severity = warning
  dotnet_diagnostic.IDE0051.severity = warning
  dotnet_diagnostic.IDE0052.severity = warning
  dotnet_diagnostic.IDE0060.severity = warning
  
  # Duplicate usings
  dotnet_diagnostic.CS0105.severity = warning
  
  # Obsolete API
  dotnet_diagnostic.CS0618.severity = warning
  ```

- [ ] **Task 2.2**: (Optional) Add Microsoft.CodeAnalysis.NetAnalyzers package
  - Add to `Directory.Build.props`
  - Version: 9.0.0 or latest

- [ ] **Task 2.3**: Document analyzer configuration
  - Update `/devdocs/coding.md` with new analyzer rules
  - Add section on ReSharper usage for team

**Testing**: Build solution to verify analyzers are active
```bash
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug
# Should now see analyzer warnings in output
```

**Commit**: `chore: Add Roslyn analyzers for dead code detection`

- [ ] All tests passing after configuration changes

---

#### Phase 3: Automated Formatting Cleanup (Low Risk)

**Objective**: Fix formatting issues that don't affect functionality

- [ ] **Task 3.1**: Run ReSharper Code Cleanup (formatting only)
  - Profile: "Reformat Code" (no semantic changes)
  - Scope: Entire solution
  - Review git diff before committing

- [ ] **Task 3.2**: Fix duplicate using directives
  - **Primary Target**: `ShellViewModel.cs` (5 duplicates at lines 27-32)
  - Method: ReSharper auto-fix or manual removal
  - **Note**: Line numbers will shift - use file/class names for reference

- [ ] **Task 3.3**: Verify no semantic changes
  - Review git diff: Should only show whitespace/formatting
  - Build solution: Should succeed
  - Run tests: Should have same pass/fail count as baseline

**Testing**:
```bash
# Build
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64

# Run all tests
vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"

# Compare test results to baseline
```

**Commit**: `style: Run code cleanup - formatting and duplicate using removal`

**Update Tracking**: Check off completed items in GitHub issue and mark in reference docs

- [ ] All tests passing after formatting changes

---

#### Phase 4: Simple Dead Code Removal (Low-Medium Risk)

**Objective**: Remove obviously unused code with minimal risk

**ORDER OF OPERATIONS** (to minimize line number drift):
1. Fix issues in order from **bottom of file to top** within each file
2. Process files in **alphabetical order** to maintain consistency
3. Check off completed items in GitHub issue after each file

- [ ] **Task 4.1**: Fix unused exception variable (CS0168)
  - **File**: `OutlineViewModel.cs:537`
  - **Current**: `catch (Exception ex)` where `ex` is never used
  - **Fix**: Change to `catch (Exception)` (remove variable name)
  - **Risk**: Very low (just removing unused name)

**Testing after Task 4.1**:

```bash
# Build
msbuild StoryCAD.sln /t:Build

# Run OutlineViewModel tests
vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll" --Tests:*OutlineViewModel*
```

**Commit**: `fix: Remove unused exception variable in OutlineViewModel.cs:537`

- [ ] **Task 4.2**: Remove unused private fields (CS0169)
  - **File**: `CollaboratorService.cs:23-24`
  - **Fields to remove**:
    ```csharp
    private string collaboratorType;  // Line 23
    private ICollaborator collaborator;  // Line 24
    ```
  - **Verification**:
    - Use ReSharper "Find Usages" to confirm zero references
    - Check if fields are placeholders for future work (review TODOs)
  - **Risk**: Low (private fields, confirmed unused by compiler)

**Testing after Task 4.2**:
```bash
# Build
msbuild StoryCAD.sln /t:Build

# Run CollaboratorService tests
vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll" --Tests:*Collaborator*
```

**Commit**: `fix: Remove unused fields from CollaboratorService.cs`

- [ ] **Task 4.3**: Remove/fix assigned-but-never-used fields (CS0414)

  **Sub-task 4.3a**: `PrintReportDialogVM.WinAppSDK.cs:23`
  - **Field**: `private bool _printTaskCreated;`
  - **Investigation needed**: Is this used for debugging? Future feature?
  - **Options**:
    - Option A: Remove if truly unused
    - Option B: Use in actual logic if it should be checked
    - Option C: Add `#pragma warning disable CS0414` with comment if intentional
  - **Decision**: Document in issue comments before proceeding

  **Sub-task 4.3b**: `SerializationLock.cs:26`
  - **Field**: `private bool _isNested;`
  - **Investigation needed**: Nested lock tracking? Debugging?
  - **Same options as 4.3a**

**Testing after Task 4.3** (for each sub-task):
```bash
# Build
msbuild StoryCAD.sln /t:Build

# For 4.3a - Run print dialog tests
vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll" --Tests:*PrintReport*

# For 4.3b - Run SerializationLock tests
vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll" --Tests:*SerializationLock*
```

**Commits**:
- `fix: Remove/document _printTaskCreated field in PrintReportDialogVM`
- `fix: Remove/document _isNested field in SerializationLock`

**Update Tracking**: Mark CS0168, CS0169, CS0414 issues as completed; update line numbers

- [ ] All tests passing after dead code removal

---

#### Phase 5: Obsolete API Migration (Medium Risk)

**Objective**: Update deprecated SkiaSharp API calls

- [ ] **Task 5.1**: Update SkiaSharp API usage in PrintReportDialogVM.cs
  - **Lines affected**: 250, 251, 255, 288
  - **Current obsolete APIs**:
    - `SKPaint.TextSize` → Use `SKFont.Size`
    - `SKPaint.Typeface` → Use `SKFont.Typeface`
    - `SKPaint.FontSpacing` → Use `SKFont.Spacing`
    - `SKCanvas.DrawText(string, float, float, SKPaint)` → Use overload with `SKFont`

  - **Migration approach**:
    ```csharp
    // Before (line 250-251):
    paint.TextSize = 12;
    paint.Typeface = typeface;
    
    // After:
    SKFont font = new SKFont(typeface, 12);
    
    // Before (line 288):
    canvas.DrawText(text, x, y, paint);
    
    // After:
    canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    ```

- [ ] **Task 5.2**: Test print functionality manually
  - **Manual test plan**:
    1. Open StoryCAD on Windows
    2. Create or open a story
    3. Navigate to Tools → Print Reports
    4. Generate a PDF report
    5. Verify text renders correctly (check font, size, spacing)
    6. Compare output to previous version (baseline screenshot)

  - **Test on macOS (UNO platform)**:
    1. Repeat above steps on macOS build
    2. Verify cross-platform consistency

**Testing**:
```bash
# Build
msbuild StoryCAD.sln /t:Build

# Run PrintReportDialogVM tests
vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll" --Tests:*PrintReport*

# Manual testing required - cannot automate PDF rendering validation
```

**Commit**: `fix: Update SkiaSharp API to non-obsolete methods`

**Update Tracking**: Mark CS0618 warnings as resolved; update line numbers

- [ ] Unit tests passing
- [ ] Manual print test completed on Windows
- [ ] Manual print test completed on macOS

---

#### Phase 6: Nullable Reference Type Cleanup (Low Priority)

**Objective**: Resolve CS8632 warnings (nullable annotations without #nullable context)

**Note**: This is lower priority as `<Nullable>disable</Nullable>` is set project-wide

- [ ] **Task 6.1**: Choose nullable strategy
  - **Option A**: Remove nullable annotations (`?`) from affected files (quick fix)
  - **Option B**: Add `#nullable enable` to individual files (medium effort)
  - **Option C**: Enable nullable project-wide (large effort, requires extensive testing)

  - **Recommendation**: Option A for now (remove annotations)
  - **Reason**: Project intentionally has nullable disabled; full migration is separate effort

- [ ] **Task 6.2**: Remove nullable annotations from production code
  - **Files affected** (11 files):
    - AppState.cs (lines 84, 95, 97, 112)
    - AutoSaveService.cs (line 68)
    - ICollaborator.cs (line 18)
    - SerializationLock.cs (line 34)
    - StoryDocument.cs (lines 20, 33)
    - PrintReportDialogVM.WinAppSDK.cs (lines 25, 26)
    - DispatcherQueueExtensions.cs (lines 18, 39)
    - OutlineService.cs (line 755)

  - **Method**: ReSharper quick fix "Remove nullable annotation"
  - **Risk**: Very low (just removing type hints, no logic change)

- [ ] **Task 6.3**: Decide on test project nullable warnings
  - **40+ instances** in test files
  - **Options**:
    - Fix individually (time-consuming)
    - Suppress in test project (add NoWarn to .csproj)
    - Defer to future nullable enablement project

  - **Recommendation**: Defer (add to backlog)

**Testing**:
```bash
# Build - should eliminate CS8632 warnings
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug

# Run all tests
vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"
```

**Commit**: `fix: Remove nullable annotations to match project nullable policy`

**Update Tracking**: Mark CS8632 warnings as resolved

- [ ] All tests passing after nullable cleanup

---

#### Phase 7: Legacy Constructor Removal (High Risk)

**Objective**: Remove 21 legacy constructors marked for removal

**CRITICAL**: This phase has high risk of breaking XAML bindings and tests

**Prerequisites**:
- All previous phases complete and tested
- Create feature branch: `issue-1134-remove-legacy-constructors`
- One constructor at a time, with testing between each

**Process for Each Constructor**:
1. Use ReSharper "Find Usages" (Alt+F7) to locate all references
2. Update each usage to use DI constructor instead
3. Remove legacy constructor
4. Build and test
5. Commit
6. Move to next constructor

- [ ] **Task 7.1**: Remove Tool ViewModel legacy constructors (9 classes)

  **Order** (simplest to most complex):

  1. [ ] TraitsViewModel.cs:62-65
     - Find usages of `TraitsViewModel()`
     - Update to inject `ListData` via DI
     - Remove parameterless constructor
     - Test: Build + run tests
     - Commit: `refactor: Remove legacy constructor from TraitsViewModel`

  2. [ ] FlawViewModel.cs:36-39
  3. [ ] StockScenesViewModel.cs:50-53
  4. [ ] DramaticSituationsViewModel.cs:61-64
  5. [ ] KeyQuestionsViewModel.cs:77-80
  6. [ ] MasterPlotsViewModel.cs:43-46
  7. [ ] TopicsViewModel.cs:112-115
  8. [ ] InitVM.cs:18-21
  9. [ ] FeedbackViewModel.cs:15-18

**Testing after each removal**:
```bash
# Build
msbuild StoryCAD.sln /t:Build

# Run tests for specific ViewModel
vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll" --Tests:*[ViewModelName]*
```

- [ ] **Task 7.2**: Remove Main ViewModel legacy constructors (8 classes)

  **Note**: These may have XAML usage - verify carefully

  1. [ ] NewProjectViewModel.cs:25-28
  2. [ ] FolderViewModel.cs:136-139
  3. [ ] OverviewViewModel.cs:410-413
  4. [ ] SettingViewModel.cs:257-260
  5. [ ] FileOpenVM.cs:235-241
  6. [ ] WebViewModel.cs:328-333
  7. [ ] NarrativeToolVM.cs:65-68
  8. [ ] CharacterViewModel.cs:921-924 (SPECIAL: investigate which constructor is correct)

**For CharacterViewModel specifically**:
- [ ] Investigate: Line 921 has 3-param constructor marked as legacy
- [ ] Find the "real" DI constructor (should have more parameters)
- [ ] Determine which is newer/correct
- [ ] Document decision in PR

**Testing**: Same as Task 7.1 (build + targeted tests after each)

- [ ] **Task 7.3**: Remove Model/DAL legacy constructors (2 classes)

  1. [ ] Windowing.cs:40-44
     - **CRITICAL**: Check for XAML instantiation
     - **CRITICAL**: Related to circular dependency TODOs (lines 159, 188)
     - May need architectural fix before removal

  2. [ ] PreferencesIO.cs:37-40
     - Lower risk (DAL layer, no XAML)

- [ ] **Task 7.4**: Remove Collaborator ViewModel legacy constructor (1 class)

  1. [ ] WorkflowViewModel.cs:117-120

- [ ] **Task 7.5**: Remove commented-out ShellViewModel constructor
  - **File**: ShellViewModel.cs:1276-1279
  - **Status**: Already commented out
  - **Action**: Delete commented code entirely
  - **Risk**: None (already commented)

**Commit**: `refactor: Remove commented legacy constructor from ShellViewModel`

**Testing after all constructors removed**:
```bash
# Full build
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64

# Full test suite
vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"

# Manual smoke test on Windows
# Manual smoke test on macOS
```

**Update Tracking**: Mark all 21 legacy constructors as completed

- [ ] All tests passing after all legacy constructor removal
- [ ] Manual smoke test on Windows completed
- [ ] Manual smoke test on macOS completed

---

#### Phase 8: TODO Resolution (Variable Risk)

**Objective**: Address or document 50 TODO items

**Strategy**: Categorize TODOs by action required

- [ ] **Task 8.1**: Document architectural TODOs (no code change)

  **High-priority architectural issues** (require design decisions):
  - [ ] Windowing.cs:159 - Circular dependency: Windowing ↔ OutlineViewModel
  - [ ] Windowing.cs:188 - Same architectural issue
  - [ ] OutlineViewModel.cs:58 - Circular dependency: OutlineViewModel ↔ AutoSaveService
  - [ ] OutlineService.cs:19 - Circular dependency: OutlineService ↔ AutoSaveService ↔ OutlineViewModel
  - [ ] NarrativeToolVM.cs:25 - ShellViewModel dependency required for CurrentViewType
  - [ ] ToolValidationService.cs:16 - Refactor CurrentViewType, CurrentNode properties

  **Action**:
  - Create `/devdocs/architectural_debt.md`
  - Document each circular dependency
  - Propose refactoring approach
  - Create separate issues for architectural fixes
  - **Do NOT attempt to fix in this issue**

- [ ] **Task 8.2**: Quick-fix TODOs (safe, simple changes)

  - [ ] OutlineViewModel.cs:674 - Revamp UX to be more user-friendly
    - **Action**: Create UX improvement issue, remove TODO

  - [ ] ShellViewModel.cs:566 - Raise event for StoryModel change?
    - **Decision needed**: Should event be raised?
    - If yes: Implement
    - If no: Remove TODO with comment explaining why not

  - [ ] ShellViewModel.cs:665 - Logging???
    - **Action**: Add appropriate logging or remove TODO

- [ ] **Task 8.3**: Feature TODOs (defer to separate issues)

  **Future features** (create issues, remove TODOs):
  - [ ] ScrivenerReports.cs:55 - Load templates from within ReportFormatter
  - [ ] ScrivenerReports.cs:246 - Activate notes collecting
  - [ ] ScrivenerReports.cs:331 - Complete SaveScrivenerNotes code
  - [ ] CollaboratorService.cs:435 - Use Show and hide properly
  - [ ] CollaboratorService.cs:504 - Add FTP upload code
  - [ ] CollaboratorService.cs:513 - On calls, set callback delegate
  - [ ] SemanticKernelAPI.cs:225 - Force set UUID somehow

  **Action for each**:
  - Create GitHub issue with details from TODO
  - Replace TODO with issue reference: `// TODO: See issue #XXXX`

- [ ] **Task 8.4**: Investigate and fix TODOs (medium effort)

  - [ ] StoryIO.cs:231 - Investigate alternatives on other platforms
    - **Platform-specific code** - test on macOS

  - [ ] ScrivenerIO.cs:245 - Handle Unknown and default cases in switch
    - **Add proper error handling**

  - [ ] StoryModel.cs:10 - Move StoryModel to ViewModels namespace
    - **Refactoring** - verify no breaking changes

  - [ ] StoryElementCollection.cs:48, 73, 74 - Assert/loop improvements
    - **Code improvement** - add assertions or refactor loops

- [ ] **Task 8.5**: Research TODOs (defer or document)

  - [ ] StoryModel.cs:14-16 - ICollectionView research links
    - **Action**: Research, then either implement or close with findings

  - [ ] CharacterViewModel.cs:893, 936 - How to bind to Sex option buttons?
    - **Action**: Investigate XAML binding approach, document solution

**Testing**: Build and run tests after each TODO resolution
```bash
msbuild StoryCAD.sln /t:Build
vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"
```

**Commits**: One commit per logical TODO group
- `docs: Document architectural debt TODOs`
- `fix: Resolve quick-fix TODOs in ShellViewModel`
- `chore: Create issues for feature TODOs`
- etc.

**Update Tracking**: Mark TODOs as resolved, deferred, or converted to issues

- [ ] All quick-fix TODOs resolved
- [ ] All feature TODOs converted to separate issues
- [ ] Architectural TODOs documented
- [ ] All tests passing

---

- [ ] Human approves plan
- [ ] Human final approval

---

### Integration & Manual Testing / sign-off

- [ ] Plan this section
- [ ] Human approves plan

#### Phase 9: Full Regression Testing

**Objective**: Verify all changes haven't introduced regressions

- [ ] **Task 9.1**: Automated test suite verification

  **Run full suite on both target frameworks**:
  ```bash
  # Build both frameworks
  msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64

  # Run tests
  vstest.console.exe "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"
  ```

  **Acceptance**:
  - [ ] All tests that passed in baseline still pass
  - [ ] Zero new test failures
  - [ ] Test count unchanged (unless tests were added)

- [ ] **Task 9.2**: Build verification

  **Clean build both configurations**:
  ```bash
  # Clean
  msbuild StoryCAD.sln /t:Clean

  # Debug build
  msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64

  # Release build (zero warnings target)
  msbuild StoryCAD.sln /t:Build /p:Configuration=Release /p:Platform=x64
  ```

  **Acceptance**:
  - [ ] Debug build: 0 errors
  - [ ] Release build: 0 errors, 0 warnings (or documented exceptions)

- [ ] **Task 9.3**: Manual smoke testing - Windows (WinUI)

  **Test Plan**:
  1. [ ] Launch StoryCAD
  2. [ ] Create new story from template
  3. [ ] Add each story element type (Character, Scene, Setting, Problem, Folder, Web)
  4. [ ] Edit story elements (verify all forms work)
  5. [ ] Use each tool:
     - [ ] Dramatic Situations
     - [ ] Key Questions
     - [ ] Master Plots
     - [ ] Stock Scenes
     - [ ] Topics
     - [ ] Narrative Tool
     - [ ] Print Reports (PDF generation)
  6. [ ] Save story
  7. [ ] Close and reopen story
  8. [ ] Open preferences dialog
  9. [ ] Verify no crashes or errors

- [ ] **Task 9.4**: Manual smoke testing - macOS (UNO Platform)

  **Same test plan as 9.3**, executed on macOS build

  **Additional UNO-specific checks**:
  - [ ] Platform-specific code paths work (file pickers, etc.)
  - [ ] UI renders correctly (no WinUI-specific rendering issues)
  - [ ] Print functionality works on macOS

- [ ] **Task 9.5**: Performance verification

  **Check for performance regressions**:
  - [ ] Startup time (compare to baseline)
  - [ ] File open time for large story files
  - [ ] Memory usage (no significant increase)

  **Method**: Use AppState.StartUpTimer and log startup metrics

- [ ] **Task 9.6**: Log file review

  **Check application logs** for new errors/warnings:
  - [ ] Review StoryCAD logs from manual testing
  - [ ] Verify no new exceptions logged
  - [ ] Check log levels are appropriate

**Documentation of Test Results**:
- [ ] Create `/devdocs/issue_1134_test_results.md`
- [ ] Document all test results (automated + manual)
- [ ] Include screenshots of successful operations
- [ ] Note any issues discovered (create follow-up issues if needed)

- [ ] Human approves plan
- [ ] Human final approval

---

### Final sign-off

- [ ] All acceptance criteria met (see top of issue)
- [ ] All tests passing in CI/CD (if available)
- [ ] Code follows project conventions
- [ ] All four reference documents accurate and updated
- [ ] `/devdocs/issue_1134_test_results.md` completed
- [ ] `/devdocs/architectural_debt.md` created (if applicable)
- [ ] PR ready for review
- [ ] Human final approval

---

## Summary of Changes

### Files Modified (Estimated)
- **Dead code removal**: 4 files
- **Duplicate usings**: 1 file (ShellViewModel.cs)
- **Obsolete API**: 1 file (PrintReportDialogVM.cs)
- **Nullable cleanup**: 11 files
- **Legacy constructors**: 21 files
- **TODO resolution**: 30+ files
- **Configuration**: .editorconfig, Directory.Build.props

**Total**: ~70 files modified

### Lines of Code Changed (Estimated)
- **Deletions**: ~200 lines (dead code, legacy constructors, TODOs)
- **Modifications**: ~50 lines (API updates, nullable cleanup)
- **Additions**: ~20 lines (analyzer config)

### Time Estimate
- **With ReSharper/Rider**: 8-12 hours total (analysis + implementation + testing)
- **Without tools**: 15-20 hours

### Risk Assessment
- **Low risk**: Phases 2-4 (configuration, formatting, simple fixes)
- **Medium risk**: Phases 5-6 (API migration, nullable cleanup)
- **High risk**: Phase 7 (legacy constructor removal)
- **Variable risk**: Phase 8 (TODO resolution - depends on specific TODO)

### Rollback Plan
Each phase commits separately, allowing rollback to any previous state:
```bash
# Rollback to before Phase 7 (legacy constructors)
git revert <commit-hash-range>
```

---

## Post-Cleanup Maintenance

### Prevent Future Issues

1. **Enable analyzer rules** (Phase 2) - warnings appear during development
2. **ReSharper/Rider settings** - export to team-shared DotSettings
3. **Pre-commit hooks** - run code cleanup on changed files
4. **CI/CD integration** - fail builds on warnings (optional)

### Monitoring

- **Weekly**: Check for new warnings in builds
- **PR review**: Verify no new dead code introduced
- **Quarterly**: Run ReSharper inspection for code debt

---

## Notes

- **Line number references**: Will become outdated as code is deleted. Use issue descriptions and file names as primary identifiers. Mark completed items in GitHub issue and reference docs after each phase.

- **Testing frequency**: Test after every phase, not just at the end. This isolates issues to specific changes.

- **Branch strategy**: Consider creating sub-branches for high-risk phases (e.g., `issue-1134-phase7-constructors`) that can be reviewed independently.

- **Architectural issues**: Do NOT attempt to fix circular dependencies in this issue. Document them and create separate issues for proper design/refactoring.
