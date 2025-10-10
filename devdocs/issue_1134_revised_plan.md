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

### 1.1 Dead Code Warnings ✅ COMPLETED
- **CS0168**: Unused exception variables - change to proper logging
- **CS0169**: Unused private fields - remove or suppress if platform-specific
- **CS0414**: Assigned but never used fields - remove or suppress if platform-specific
- **CS0162**: Unreachable code - suppress if intentional (platform-specific)

### 1.2 Code Quality Warnings
- **CS0105**: Duplicate using directives - remove duplicates (ReSharper already cleaned this)
- **CS0618**: Obsolete API usage (SkiaSharp) - research alternatives before migrating

### 1.3 Nullable Warnings (Suppress, Don't Fix)
- **CS8632**: Nullable annotation without #nullable context - **SUPPRESS with pragma**, don't remove `?` markers
  - The `?` markers document that values can be null
  - Removing them would hide important nullability information
  - Proper fix: Add `#pragma warning disable CS8632` to affected files

**Fix**: Address each warning type systematically

**Verify**:
```bash
# Build must succeed with 0 errors
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64

# Build test project then run all tests
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCADTests/StoryCADTests.csproj -t:Build -p:Configuration=Debug -p:Platform=x64 && "/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621/StoryCADTests.dll"
```

**Document & Commit**:
1. **FIRST**: Update `/devdocs/issue_1134_progress_log.md` with what was done
2. **THEN**: Commit code changes AND progress log together
3. Individual commits per warning type (e.g., "fix: Remove unused fields (CS0169)")

---

## Phase 2: Legacy Constructor Removal ✅ COMPLETED

**Objective**: Remove 21 legacy constructors marked for DI migration

**What was done**:
- Removed 20 of 21 legacy parameterless constructors
- 2 constructors must remain (SettingViewModel, OverviewViewModel) - required by XAML tooling
- Key learning: Clean rebuild removes stale XamlTypeInfo.g.cs references

**Results**:
- Commits: 8733486c (18 constructors), 7173fe7a (FolderViewModel)
- Build: ✅ 0 errors, 38 warnings (Uno0001 only)
- Tests: ✅ 418 passed, 3 skipped

**ViewModels Updated** (20):
- TraitsViewModel, FlawViewModel, StockScenesViewModel
- DramaticSituationsViewModel, KeyQuestionsViewModel, MasterPlotsViewModel, TopicsViewModel
- InitVM, FeedbackViewModel, NewProjectViewModel, FolderViewModel
- WebViewModel, PrintReportDialogVM, NarrativeToolVM, FileOpenVM
- CharacterViewModel, WorkflowViewModel, ShellViewModel

**ViewModels Preserved** (2):
- SettingViewModel (required by XAML)
- OverviewViewModel (required by XAML)

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
