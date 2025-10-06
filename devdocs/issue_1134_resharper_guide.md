# Using ReSharper for StoryCAD Code Cleanup

Generated for issue #1134 - Code cleanup
Date: 2025-10-05

## Overview

This guide explains how to use **JetBrains ReSharper** (or **Rider**) to systematically clean up the StoryCAD codebase, addressing the issues identified in:
- `issue_1134_build_warnings_analysis.md`
- `issue_1134_dead_code_warnings.md`
- `issue_1134_legacy_constructors.md`
- `issue_1134_TODO_list.md`

ReSharper provides two key features for code cleanup:
1. **Code Cleanup**: Automated formatting and style fixes
2. **Code Inspection**: Finding code issues including dead code

---

## Prerequisites

### Option 1: ReSharper (Visual Studio Extension)
- Install: Visual Studio → Extensions → Manage Extensions → Search "ReSharper"
- License: Commercial (trial available) or free for open source projects
- Works with: Visual Studio 2022

### Option 2: Rider (Standalone IDE)
- Full JetBrains IDE with built-in ReSharper features
- License: Same as ReSharper
- Works on: Windows, macOS, Linux

### Option 3: Command-Line Tools (Free)
- Download: [JetBrains ReSharper Command Line Tools](https://www.jetbrains.com/resharper/download/#section=commandline)
- Includes: `cleanupcode.exe`, `inspectcode.exe`
- No license required for command-line tools
- Can be used in CI/CD pipelines

---

## Part 1: Finding Dead Code with Code Inspection

### Using Visual Studio + ReSharper

#### Step 1: Enable Solution-Wide Analysis

1. **ReSharper → Options → Code Inspection → Settings**
2. Check **"Enable solution-wide analysis"**
3. Click **OK**

This enables ReSharper to analyze all files in the solution, not just open files.

#### Step 2: Run Code Inspection

**Option A: Full Solution Inspection**
```
ReSharper → Inspect → Code Issues in Solution
```
- Analyzes entire solution
- Generates report of all issues
- Can export to HTML/XML

**Option B: View Issues in Error List**
```
View → Error List
```
- Filter by "Show Messages from ReSharper"
- Look for warnings like:
  - "Private member is never used"
  - "Field is assigned but its value is never used"
  - "Parameter is never used"

#### Step 3: Navigate to Dead Code Issues

1. In the Inspection Results window, expand categories:
   - **Code Smell → Redundancies in Code**
   - **Code Smell → Redundancies in Symbol Declarations**

2. Look for:
   - `Field 'fieldName' is never used` → **CS0169**
   - `Field 'fieldName' is assigned but its value is never used` → **CS0414**
   - `Variable 'varName' is declared but never used` → **CS0168**
   - `Private member is never used` → **IDE0051**

#### Step 4: Fix Dead Code Issues

For each issue:

1. **Right-click the warning** → **Navigate To** (or press F12)
2. Review the code to confirm it's truly unused
3. **Options**:
   - **Safe Remove**: Right-click → **Safe Delete** (checks for references)
   - **Quick Fix**: Alt+Enter → Select fix
   - **Manual Delete**: Delete the code directly

**For StoryCAD's Known Dead Code** (from `issue_1134_dead_code_warnings.md`):

```csharp
// CollaboratorService.cs:23-24
private string collaboratorType;  // DELETE
private ICollaborator collaborator;  // DELETE

// PrintReportDialogVM.WinAppSDK.cs:23
private bool _printTaskCreated;  // DELETE or use in logic

// SerializationLock.cs:26
private bool _isNested;  // DELETE or use for debugging

// OutlineViewModel.cs:537
catch (Exception ex)  // Change to: catch (Exception)
```

---

## Part 2: Code Cleanup for Formatting Issues

### Using Visual Studio + ReSharper

#### Step 1: Configure Cleanup Profile

1. **ReSharper → Options → Code Cleanup**
2. Select profile: **"Full Cleanup"** (or create custom)
3. Enable these options:
   - ✅ Remove unused using directives
   - ✅ Optimize using directives
   - ✅ Reformat code
   - ✅ Apply file layout
   - ✅ Remove redundant type specifications
   - ✅ Remove redundant empty constructors
   - ✅ Remove redundant qualifiers

#### Step 2: Run Code Cleanup

**On Single File**:
```
1. Open the file
2. ReSharper → Edit → Cleanup Code (Ctrl+E, C)
3. Select cleanup profile
4. Click Run
```

**On Entire Solution**:
```
1. Solution Explorer → Right-click solution
2. ReSharper → Cleanup Code
3. Select "Full Cleanup" profile
4. Click Run
```

**For Specific Files** (e.g., ShellViewModel.cs with duplicate usings):
```
1. Open StoryCADLib/ViewModels/ShellViewModel.cs
2. Press Ctrl+E, C
3. Run cleanup
4. Duplicate usings will be removed automatically
```

#### Step 3: Configure Cleanup on Save (Optional)

1. **ReSharper → Options → Code Editing → Code Cleanup**
2. Check **"Automatically run cleanup when saving a file"**
3. Select profile: **"Full Cleanup"** or custom
4. Click **OK**

Now every time you save a file, ReSharper will auto-cleanup.

---

## Part 3: Fixing Specific Issues

### Issue: Duplicate Using Directives

**File**: `ShellViewModel.cs` (lines 27, 29, 30, 31, 32)

**ReSharper Solution**:
1. Open `ShellViewModel.cs`
2. Press **Ctrl+E, C** (Cleanup Code)
3. Duplicates removed automatically

**Manual Verification**:
```csharp
// Before cleanup:
using StoryCAD.Services;
// ... other usings
using StoryCAD.Services;  // DUPLICATE - will be removed

// After cleanup:
using StoryCAD.Services;  // Only one remains
```

---

### Issue: Obsolete API Usage (SkiaSharp)

**File**: `PrintReportDialogVM.cs` (lines 250, 251, 255, 288)

**ReSharper Detection**:
- Warnings: "Member is obsolete"
- Severity: Warning (yellow underline)

**Fix with Quick Actions**:
1. Place cursor on `SKPaint.TextSize`
2. Press **Alt+Enter**
3. Select **"Use SKFont.Size instead"** (if available)
4. Repeat for other obsolete members

**Manual Fix** (if no quick action):
```csharp
// Before:
paint.TextSize = 12;
paint.Typeface = typeface;

// After:
SKFont font = new SKFont(typeface, 12);
canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
```

---

### Issue: Nullable Reference Type Mismatches

**Files**: Multiple (see `issue_1134_build_warnings_analysis.md`)

**ReSharper Detection**:
- Warning: "Annotation for nullable reference types should only be used in code within a '#nullable' annotations context"
- Code: CS8632

**ReSharper Solutions**:

**Option A: Remove Nullable Annotations** (Quick)
1. Navigate to warning
2. Press **Alt+Enter**
3. Select **"Remove nullable annotation"**

**Option B: Enable Nullable Context** (Better)
1. Navigate to top of file
2. Press **Alt+Enter** on nullable warning
3. Select **"Add '#nullable enable' directive"**

**Option C: Project-Wide** (Best, but requires testing)
```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <Nullable>enable</Nullable>  <!-- Change from disable -->
</PropertyGroup>
```

---

### Issue: Legacy Constructors

**Files**: 21 classes (see `issue_1134_legacy_constructors.md`)

**ReSharper Can't Automatically Remove** (requires manual review)

But ReSharper **can help find usages**:

1. Navigate to legacy constructor
2. Right-click → **Find Usages** (Alt+F7)
3. Review all usages
4. If zero usages, safe to delete
5. If usages exist, update callers first

**Example Workflow**:
```csharp
// TraitsViewModel.cs:63
public TraitsViewModel() : this(Ioc.Default.GetRequiredService<ListData>())

1. Right-click "TraitsViewModel()" → Find Usages
2. Check results:
   - If empty → Safe to delete
   - If used in XAML → Update XAML first
   - If used in tests → Update tests first
3. Delete constructor
4. Build project to verify
```

---

## Part 4: Command-Line Automation

### Install Command-Line Tools

```bash
# Download from: https://www.jetbrains.com/resharper/download/#section=commandline
# Or use Chocolatey:
choco install resharper-clt
```

### Inspect Solution for Issues

```bash
# Run inspection
inspectcode.exe D:\dev\src\StoryCAD\StoryCAD.sln \
  --output=D:\temp\storycad_inspection_results.xml \
  --severity=WARNING

# View results (XML format)
# Can be imported into ReSharper or parsed by scripts
```

**Filter for Dead Code Only**:
```bash
inspectcode.exe StoryCAD.sln \
  --include="UnusedMember.Local;UnusedField.Compiler;UnusedParameter.Local" \
  --output=dead_code_report.xml
```

### Run Code Cleanup

```bash
# Cleanup entire solution
cleanupcode.exe D:\dev\src\StoryCAD\StoryCAD.sln \
  --profile="Full Cleanup" \
  --verbosity=WARN

# Cleanup specific project
cleanupcode.exe D:\dev\src\StoryCAD\StoryCADLib\StoryCADLib.csproj \
  --profile="Built-in: Reformat Code"
```

### Create Custom Cleanup Profile

1. In Visual Studio with ReSharper:
   - **ReSharper → Options → Code Cleanup**
   - Configure custom profile
   - Export settings: **ReSharper → Manage Options → Export to File**

2. Use in command-line:
```bash
cleanupcode.exe StoryCAD.sln \
  --settings=team_cleanup_profile.DotSettings \
  --profile="StoryCAD Cleanup"
```

---

## Part 5: Integration with EditorConfig

ReSharper respects `.editorconfig` settings. Enhance StoryCAD's existing config:

### Add to `.editorconfig`

```ini
[*.cs]

# Dead code detection severity
dotnet_diagnostic.CS0169.severity = warning  # Unused field
dotnet_diagnostic.CS0414.severity = warning  # Field assigned but never used
dotnet_diagnostic.CS0168.severity = warning  # Variable declared but never used
dotnet_diagnostic.IDE0051.severity = warning # Unused private member
dotnet_diagnostic.IDE0052.severity = warning # Unread private member
dotnet_diagnostic.IDE0060.severity = warning # Unused parameter

# Remove duplicate usings
dotnet_diagnostic.CS0105.severity = warning

# Obsolete API usage
dotnet_diagnostic.CS0618.severity = warning

# ReSharper specific settings
resharper_redundant_using_directive_highlighting = warning
resharper_unused_member_local_highlighting = warning
resharper_unused_parameter_local_highlighting = warning
```

Now ReSharper (and Roslyn analyzers) will flag these issues in the IDE.

---

## Part 6: Workflow for Issue #1134

### Phase 1: Assessment (No Code Changes)

**Step 1: Run Full Inspection**
```bash
inspectcode.exe StoryCAD.sln --output=baseline_issues.xml
```

**Step 2: Generate Report**
- Open `baseline_issues.xml` in browser (or convert to HTML)
- Count issues by severity
- Prioritize: Errors → Warnings → Suggestions

**Step 3: Categorize Issues**
- Dead code: CS0169, CS0414, CS0168, IDE0051, IDE0052
- Code style: CS0105, formatting issues
- Obsolete APIs: CS0618
- Nullable warnings: CS8632, CS8625, etc.

### Phase 2: Automated Fixes (Low Risk)

**Step 1: Code Cleanup** (formatting only)
```bash
cleanupcode.exe StoryCAD.sln --profile="Reformat Code"
```

**Step 2: Verify Changes**
```bash
git diff
```

**Step 3: Build and Test**
```bash
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug
vstest.console.exe StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll
```

**Step 4: Commit**
```bash
git add .
git commit -m "chore: Run ReSharper code cleanup - formatting only"
```

### Phase 3: Manual Fixes (High Risk)

**Dead Code Removal** (one at a time):
1. Open file with unused member
2. Use ReSharper's "Safe Delete" (Alt+Delete)
3. Build project
4. Run tests
5. Commit: `fix: Remove unused field 'fieldName' from ClassName`

**Legacy Constructor Removal** (one at a time):
1. Find usages with ReSharper (Alt+F7)
2. Update all callers to use DI
3. Remove legacy constructor
4. Build and test
5. Commit: `refactor: Remove legacy constructor from ViewModelName`

**Obsolete API Updates**:
1. Use ReSharper quick fixes (Alt+Enter)
2. Test affected functionality manually
3. Commit: `fix: Update SkiaSharp API usage to non-obsolete methods`

### Phase 4: Verification

**Run Inspection Again**:
```bash
inspectcode.exe StoryCAD.sln --output=after_cleanup_issues.xml
```

**Compare Results**:
- Issues before: (count from baseline)
- Issues after: (count from after cleanup)
- Issues fixed: (difference)

---

## Part 7: ReSharper Settings Recommendations

### Recommended Inspection Severity Levels

**For StoryCAD** (ReSharper → Options → Code Inspection → Inspection Severity):

| Issue Type | Severity | Rationale |
|------------|----------|-----------|
| Unused private member | **Error** | Should never exist in committed code |
| Unused parameter | **Warning** | May be intentional (interface implementations) |
| Duplicate using directive | **Warning** | Code smell, but not breaking |
| Obsolete API | **Warning** | Works now, but should be fixed |
| Nullable reference mismatch | **Suggestion** | Project has nullable disabled |

### Configure in ReSharper

```
ReSharper → Options → Code Inspection → Inspection Severity → C#

Search for:
- "Unused member" → Set to "Error"
- "Duplicate using" → Set to "Warning"
- "Obsolete" → Set to "Warning"
```

### Export Team Settings

```
ReSharper → Manage Options → Save To
→ Solution Team-Shared Layer
→ StoryCAD.sln.DotSettings (committed to repo)
```

Now all team members get the same inspection settings.

---

## Part 8: CI/CD Integration

### GitHub Actions Workflow

Create `.github/workflows/resharper-inspection.yml`:

```yaml
name: ReSharper Code Inspection

on:
  pull_request:
    branches: [ main, UNOTestBranch ]

jobs:
  inspect:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Download ReSharper CLI
      run: |
        choco install resharper-clt -y

    - name: Run Code Inspection
      run: |
        inspectcode.exe StoryCAD.sln `
          --output=inspection-results.xml `
          --severity=WARNING `
          --verbosity=WARN

    - name: Check for Issues
      run: |
        # Parse XML and fail if critical issues found
        # (Custom script needed)

    - name: Upload Results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: inspection-results
        path: inspection-results.xml
```

---

## Part 9: Quick Reference

### Keyboard Shortcuts (ReSharper)

| Action | Shortcut |
|--------|----------|
| Code Cleanup | `Ctrl+E, C` |
| Quick Fix | `Alt+Enter` |
| Find Usages | `Alt+F7` |
| Safe Delete | `Alt+Delete` |
| Navigate to Declaration | `F12` |
| Inspect This | `Ctrl+Shift+Alt+A` |
| View Error List | `Ctrl+Alt+E` |

### Command-Line Quick Reference

```bash
# Inspection
inspectcode.exe solution.sln --output=results.xml

# Cleanup
cleanupcode.exe solution.sln --profile="Full Cleanup"

# With custom settings
inspectcode.exe solution.sln \
  --settings=team.DotSettings \
  --output=results.xml
```

---

## Part 10: Troubleshooting

### Issue: "Solution-Wide Analysis is Disabled"

**Solution**:
```
ReSharper → Options → Code Inspection → Settings
→ Enable "Enable solution-wide analysis"
```

### Issue: Too Many Warnings

**Solution**:
1. Filter by severity in Error List
2. Start with Errors only
3. Then tackle Warnings
4. Ignore Suggestions initially

### Issue: ReSharper Slows Down Visual Studio

**Solutions**:
- Disable solution-wide analysis for large solutions
- Use command-line tools instead
- Suspend ReSharper when not needed: `Tools → Options → ReSharper → Suspend Now`
- Increase Visual Studio memory: Tools → Options → Performance

### Issue: False Positives

**Suppress Individual Warnings**:
```csharp
// Used via reflection
#pragma warning disable IDE0051
private void OnPluginLoaded() { }
#pragma warning restore IDE0051
```

Or with ReSharper comment:
```csharp
// ReSharper disable once UnusedMember.Local
private void UsedInXaml() { }
```

---

## Summary

### For StoryCAD Issue #1134

**Use ReSharper for**:
- ✅ Finding all dead code (21+ instances identified)
- ✅ Removing duplicate usings (5 in ShellViewModel)
- ✅ Formatting code consistently
- ✅ Finding obsolete API usage (4 SkiaSharp calls)
- ✅ Identifying legacy constructors (21 classes)

**Don't use ReSharper for**:
- ❌ Architectural refactoring (circular dependencies)
- ❌ Complex business logic changes
- ❌ Breaking XAML bindings (manual review needed)

**Best Approach**:
1. **Automated**: Code cleanup for formatting
2. **Semi-automated**: Quick fixes for simple issues
3. **Manual**: Dead code removal (verify with Find Usages first)
4. **Manual**: Legacy constructor removal (requires usage analysis)

### Estimated Time Savings

- **Without ReSharper**: 8-12 hours manual review
- **With ReSharper**: 2-4 hours (automated + verification)

### ROI for StoryCAD

Given the size of the cleanup (50+ TODOs, 21 legacy constructors, 57+ warnings), ReSharper would save significant time even for a one-time cleanup.

**Alternatives if No ReSharper License**:
- Use free command-line tools for inspection
- Use Rider (may have free trial)
- Use Visual Studio's built-in analyzers (less powerful)
- Apply for open-source license (StoryCAD is GPL v3)
