# Code Cleanup - Issue #1134 - Revised Plan

## Problem Statement
We have a backlog of compile warnings, dead code, TODOs and other non-critical code issues.

## Proposed Solution
Systematic cleanup of the codebase, prioritizing clean compile first. Each phase: identify → fix → verify (build + tests pass) → commit.

**Note**: UNO Platform compatibility warnings (Uno0001) are tracked separately in issue #1139.

## Tools for Issue Identification

Use these techniques to find issues just-in-time (not upfront analysis):

### Compiler Warnings
```bash
# Get all warnings excluding Uno0001
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64 /flp:WarningsOnly /fl
cat msbuild.log | grep -E "warning (CS|UNO)" | grep -v "Uno0001" | sort | uniq
```

### ReSharper Analysis
- **Find Usages**: Alt+F7 (check if code is referenced)
- **Solution-wide Inspection**: Analyze → Inspect Code
- **Code Issues**: ReSharper → Windows → Code Issues

### Search for Patterns
```bash
# Find TODOs
grep -r "TODO" --include="*.cs" StoryCAD StoryCADLib

# Find legacy constructors
grep -r "// Legacy" --include="*.cs" StoryCAD StoryCADLib

# Find unused fields/variables
grep -rn "CS0169\|CS0414\|CS0168"
```

## Acceptance Criteria

- [ ] Build produces zero warnings in Debug configuration (excluding Uno0001)
- [ ] Build produces zero warnings in Release configuration (excluding Uno0001)
- [ ] All CS0169 warnings (unused fields) resolved
- [ ] All CS0414 warnings (assigned but never used) resolved
- [ ] All CS0168 warnings (unused variables) resolved
- [ ] All CS0105 warnings (duplicate usings) resolved
- [ ] All CS0618 warnings (obsolete APIs) resolved
- [ ] All 21 legacy constructors removed or documented
- [ ] Critical architectural TODOs documented with resolution plan
- [ ] All existing tests pass after each phase
- [ ] Full regression test completed on Windows
- [ ] Full regression test completed on macOS (UNO platform)

---

## Phase 0: ReSharper Code Cleanup ✅ COMPLETED

**What was done**: Applied ReSharper "Code Cleanup" to entire solution via Visual Studio 2022

**Result**:
- Formatting standardized
- Code style unified
- Commit: "Apply ReSharper Code Cleanup across entire codebase - Issue #1134"

---

## Phase 1: Compiler Warnings Cleanup

**Objective**: Get as close to zero warnings as possible (excluding Uno0001)

**Identify**:
```bash
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64 /flp:WarningsOnly /fl
cat msbuild.log | grep -E "warning CS" | grep -v "Uno0001" | sort | uniq
```

**Categories**:

### 1.1 Dead Code Warnings
- **CS0168**: Unused exception variables - change `catch (Exception ex)` to `catch (Exception)`
- **CS0169**: Unused private fields - remove them
- **CS0414**: Assigned but never used fields - investigate then remove or suppress with comment

### 1.2 Code Quality Warnings
- **CS0105**: Duplicate using directives - remove duplicates
- **CS0618**: Obsolete API usage - migrate to replacement APIs

### 1.3 Nullable Warnings (Low Priority)
- **CS8632**: Nullable annotation without #nullable context - remove annotations or enable nullable

**Fix**: Address each warning type systematically

**Verify**:
```bash
# Build must succeed with 0 errors
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64

# All tests must pass
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"
```

**Commit**: Individual commits per warning type (e.g., "fix: Remove unused fields (CS0169)")

---

## Phase 2: Legacy Constructor Removal

**Objective**: Remove 21 legacy constructors marked for DI migration

**Identify**:
```bash
grep -rn "// Legacy" --include="*.cs" StoryCAD StoryCADLib
```

**Fix**:
- Use ReSharper "Find Usages" (Alt+F7) for each constructor
- Update XAML/code to use DI constructor
- Remove legacy constructor
- **One at a time** - test between each removal

**Verify**: Build + tests after EACH constructor removal

**Commit**: One commit per constructor (e.g., "refactor: Remove legacy constructor from TraitsViewModel")

**Files affected** (reference - line numbers will drift):
- TraitsViewModel.cs, FlawViewModel.cs, StockScenesViewModel.cs
- DramaticSituationsViewModel.cs, KeyQuestionsViewModel.cs, MasterPlotsViewModel.cs
- TopicsViewModel.cs, InitVM.cs, FeedbackViewModel.cs
- NewProjectViewModel.cs, FolderViewModel.cs, OverviewViewModel.cs
- SettingViewModel.cs, FileOpenVM.cs, WebViewModel.cs
- NarrativeToolVM.cs, CharacterViewModel.cs
- Windowing.cs, PreferencesIO.cs, WorkflowViewModel.cs, ShellViewModel.cs

---

## Phase 3: TODO Resolution

**Objective**: Address or document TODO comments

**Identify**:
```bash
grep -rn "TODO" --include="*.cs" StoryCAD StoryCADLib | tee /devdocs/issue_1134_todo_current.txt
```

**Categories**:

### 3.1 Architectural TODOs
- **Action**: Document in `/devdocs/architectural_debt.md`
- **Do NOT fix** - create separate issues
- Examples: Circular dependencies (Windowing ↔ OutlineViewModel, etc.)

### 3.2 Quick-Fix TODOs
- **Action**: Fix immediately if trivial (< 5 min)
- **Examples**: Add logging, remove obsolete comment

### 3.3 Feature TODOs
- **Action**: Create GitHub issues, replace TODO with issue reference
- **Examples**: Scrivener features, FTP upload, etc.

### 3.4 Research TODOs
- **Action**: Defer to backlog issue

**Fix**: Process one TODO at a time, categorize and act accordingly

**Verify**: Build + tests after each logical group of TODOs

**Commit**: Group by category (e.g., "docs: Document architectural debt TODOs")

---

## Phase 4: Final Verification

**Objective**: Ensure all changes work correctly on both platforms

### 4.1 Build Verification
```bash
# Clean build both configurations
msbuild StoryCAD.sln /t:Clean
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64
msbuild StoryCAD.sln /t:Build /p:Configuration=Release /p:Platform=x64
```

**Acceptance**: 0 errors, 0 warnings (excluding Uno0001)

### 4.2 Test Suite
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"
```

**Acceptance**: All tests pass (same count as baseline)

### 4.3 Manual Regression Testing

**Windows (WinUI)**:
- Launch StoryCAD
- Create/open story
- Add each element type (Character, Scene, Setting, Problem, Folder, Web)
- Use each tool (Dramatic Situations, Master Plots, Print Reports, etc.)
- Save and reload story
- Verify no crashes/errors

**macOS (UNO Platform)**:
- Same test plan as Windows
- Verify platform-specific code works (file pickers, etc.)

### 4.4 Documentation
- Update `/devdocs/issue_1134_progress_log.md` with final summary
- Create `/devdocs/architectural_debt.md` if architectural TODOs found
- Document any deferred work in backlog issues

---

## Progress Tracking

- All work logged in: `/devdocs/issue_1134_progress_log.md`
- User will `/clear` between phases
- Each phase ends with: clean build + all tests passing

---

## Rollback Plan

Each phase commits separately:
```bash
# Rollback to before Phase N
git log --oneline | grep "Issue #1134"
git revert <commit-hash-range>
```

---

## Notes

- **Line numbers are reference only** - use file names and ReSharper search
- **Test after every change** - isolates issues
- **No architectural fixes** - document and create separate issues
- **Uno0001 warnings** - tracked in issue #1139, ignore for this cleanup
