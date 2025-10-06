# StoryCAD Build Warnings Analysis - Dead Code Detection

Generated for issue #1134 - Code cleanup
Build date: 2025-10-05

## Summary

**Total Unique Warnings: 10 types**
**Total Warning Instances: 57+ (many duplicated across target frameworks)**

### Warning Categories

1. **Dead Code (CS0169, CS0414, CS0168)**: 4 unique instances
2. **Duplicate Using Directives (CS0105)**: 5 instances in ShellViewModel
3. **Nullable Reference Type Mismatches (CS8632, CS8767, CS8625, etc.)**: 40+ instances
4. **Obsolete API Usage (CS0618)**: 4 instances in SkiaSharp
5. **Test-Specific Warnings**: 1 instance

---

## Dead Code Warnings (Priority: HIGH)

These are actual unused code that should be removed or investigated:

### CS0169: Field Never Used

**CollaboratorService.cs:23**
```csharp
private string collaboratorType;  // Never used
```

**CollaboratorService.cs:24**
```csharp
private ICollaborator collaborator;  // Never used
```

**Recommendation**: Remove these fields or add explanation if they're placeholders for future work.

---

### CS0414: Field Assigned But Never Used

**PrintReportDialogVM.WinAppSDK.cs:23**
```csharp
private bool _printTaskCreated;  // Assigned but never read
```

**Recommendation**: Either use this field in logic or remove it. May be leftover debugging code.

**SerializationLock.cs:26**
```csharp
private bool _isNested;  // Assigned but never read
```

**Recommendation**: Investigate if this was intended for debugging or nested lock tracking. Remove if not needed.

---

### CS0168: Variable Declared But Never Used

**OutlineViewModel.cs:537**
```csharp
catch (Exception ex)  // Variable 'ex' declared but never used
```

**Recommendation**: Either log the exception or change to `catch (Exception)` without variable name.

---

## Code Quality Issues (Priority: MEDIUM)

### CS0105: Duplicate Using Directives

**ShellViewModel.cs** has 5 duplicate using statements:
- Line 27: `using StoryCAD.Services;` (duplicate)
- Line 29: `using StoryCAD.Collaborator.ViewModels;` (duplicate)
- Line 30: `using StoryCAD.Services.Outline;` (duplicate)
- Line 31: `using StoryCAD.ViewModels.SubViewModels;` (duplicate)
- Line 32: `using StoryCAD.Services.Locking;` (duplicate)

**Recommendation**: Remove the duplicate using directives. These appear twice because of multi-targeting (net9.0-windows10.0.22621 and net9.0-desktop).

---

## Deprecated API Usage (Priority: MEDIUM)

### CS0618: Obsolete SkiaSharp API

All in **PrintReportDialogVM.cs**:

**Line 250**:
```csharp
SKPaint.TextSize  // Use SKFont.Size instead
```

**Line 251**:
```csharp
SKPaint.Typeface  // Use SKFont.Typeface instead
```

**Line 255**:
```csharp
SKPaint.FontSpacing  // Use SKFont.Spacing instead
```

**Line 288**:
```csharp
SKCanvas.DrawText(string, float, float, SKPaint)
// Use DrawText(string text, float x, float y, SKTextAlign textAlign, SKFont font, SKPaint paint) instead
```

**Recommendation**: Update to use the newer SKFont API instead of deprecated SKPaint properties.

---

## Nullable Reference Type Issues (Priority: LOW-MEDIUM)

### CS8632: Nullable Annotations Outside #nullable Context

**Files Affected**:
- AppState.cs (lines 84, 95, 97, 112)
- AutoSaveService.cs (line 68)
- ICollaborator.cs (line 18)
- SerializationLock.cs (line 34)
- StoryDocument.cs (lines 20, 33)
- PrintReportDialogVM.WinAppSDK.cs (lines 25, 26)
- DispatcherQueueExtensions.cs (lines 18, 39)
- OutlineService.cs (line 755)

**Root Cause**: These files use nullable annotations (`?`) but `<Nullable>disable</Nullable>` is set in Directory.Build.props.

**Recommendations**:
1. **Option A** (Preferred): Remove nullable annotations from these files to match project setting
2. **Option B**: Add `#nullable enable` at top of these specific files
3. **Option C**: Enable nullable reference types project-wide (major undertaking)

---

## Test Project Warnings (Priority: LOW)

### Nullable Reference Type Warnings in Tests

**Multiple test files** have CS8600, CS8601, CS8602, CS8618, CS8625, CS8767 warnings related to nullable references.

**Count**: 40+ instances across test files

**Recommendation**: Since tests have `<Nullable>disable</Nullable>` but use nullable types, either:
1. Remove nullable annotations from test code
2. Enable nullable context in test files
3. Suppress these warnings in test project if they don't affect functionality

### CS8892: Synchronous Entry Point Warning

**MicrosoftTestingPlatformEntryPoint.cs:12**
```
Method 'MicrosoftTestingPlatformEntryPoint.Main(string[])' will not be used as an entry point
because a synchronous entry point 'Program.Main(string[])' was found.
```

**Recommendation**: This is informational and can be ignored. It's from test framework code generation.

---

## Action Items for Issue #1134

### Immediate (Dead Code Removal)

1. ✅ Remove unused fields in `CollaboratorService.cs` (lines 23-24)
2. ✅ Investigate and remove/fix `_printTaskCreated` in `PrintReportDialogVM.WinAppSDK.cs:23`
3. ✅ Investigate and remove/fix `_isNested` in `SerializationLock.cs:26`
4. ✅ Fix unused exception variable in `OutlineViewModel.cs:537`

### Short-term (Code Quality)

5. ✅ Remove duplicate using directives in `ShellViewModel.cs`
6. ✅ Update SkiaSharp API usage in `PrintReportDialogVM.cs` to use SKFont instead of deprecated SKPaint properties

### Medium-term (Consistency)

7. 📋 Resolve nullable annotation inconsistencies in production code (11 files affected)
   - Decide on project-wide nullable strategy
   - Either remove annotations or enable nullable context

### Low Priority (Test Cleanup)

8. 📋 Review and fix nullable warnings in test projects (40+ instances)
   - Consider enabling nullable reference types in tests
   - Or remove nullable annotations to match project settings

---

## Detection Methods Used

This analysis was generated using:

```bash
# MSBuild with warnings-only logging
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64 /fl /flp:WarningsOnly
```

**Compiler**: MSBuild version 17.14.23 for .NET Framework
**Frameworks Targeted**:
- net9.0-windows10.0.22621 (WinUI)
- net9.0-desktop (macOS)

---

## Roslyn Analyzer Recommendations

Currently, StoryCAD has **no Roslyn analyzers configured** beyond the default compiler warnings.

### Recommended Additions to .editorconfig:

```ini
# Dead code detection
IDE0051.severity = warning  # Remove unused private member
IDE0052.severity = warning  # Remove unread private member
IDE0059.severity = warning  # Unnecessary value assignment
IDE0060.severity = warning  # Remove unused parameter

# Code quality
IDE0005.severity = warning  # Remove unnecessary using directives
IDE0079.severity = suggestion  # Remove unnecessary suppression
```

### Optional: Add Analyzer Package

Add to `Directory.Build.props`:
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

---

## Notes

- Warnings appear **twice** in the log (once per target framework: Windows and Desktop)
- Dead code warnings are **actual actionable items** for cleanup
- Nullable warnings are **policy decisions** about type safety
- Test warnings are **lower priority** as they don't affect production code
- SkiaSharp warnings are **deprecation notices** - still work but should be updated

### Files with Most Issues

1. **Test Files**: 40+ nullable warnings (expected with nullable disabled)
2. **ShellViewModel.cs**: 5 duplicate usings
3. **PrintReportDialogVM.cs**: 4 obsolete API warnings + 1 dead code warning
4. **CollaboratorService.cs**: 2 unused fields
5. **AppState.cs**: 4 nullable annotation warnings
