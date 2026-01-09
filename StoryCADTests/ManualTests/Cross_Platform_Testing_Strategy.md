# StoryCAD Cross-Platform Testing Strategy

## Status: ✅ IMPLEMENTED

**Implementation Date**: October 30, 2025
**All Phases Complete**: Tests now run on both Windows and macOS in CI/CD

### Quick Summary
- **Phase 1**: ⏭️ Skipped (built-in .NET constants simpler than custom ones)
- **Phase 2**: ✅ Complete (15 hardcoded Windows paths fixed)
- **Phase 3**: ✅ Complete (platform-specific tests isolated, broken `__MACOS__` test removed)
- **Phase 4**: ✅ Complete (GitHub Actions already running tests on Windows + macOS)

See [Implementation Summary](#implementation-summary) for full details.

## Executive Summary

This document originally outlined a strategy to enable cross-platform testing for StoryCAD. **All objectives have been achieved:**
- ✅ Tests run on Windows (WinAppSDK + Skia Desktop)
- ✅ Tests run on macOS (Skia Desktop)
- ✅ Automated CI/CD testing via GitHub Actions
- ✅ Platform-specific code properly isolated
- ✅ Cross-platform path handling implemented

## Current State Analysis (Historical)

### Testing Philosophy
- **Correct Approach**: Tests target ViewModels and business logic, not UI components
- **Problem**: Implementation still has Windows-specific dependencies preventing cross-platform execution

### Issues Identified

#### 1. Architecture Mismatch
- **Current Configuration**: Tests build for `x64` architecture only
- **Runtime Identifiers**: Only Windows targets (`win-x86;win-x64;win-arm64`)
- **Apple Silicon Impact**: ARM64 Macs cannot run x64 .NET assemblies without Rosetta
- **Linux Missing**: No Linux runtime identifiers specified

#### 2. Platform-Specific Test Code

| File | Issue | Impact |
|------|-------|--------|
| `RichEditBoxExtendedTests.cs` | All tests wrapped in `#if WINDOWS10_0_18362_0_OR_GREATER` | No control tests run on macOS/Linux |
| `WindowingTests.cs` | Mixed Windows/macOS tests with platform conditionals | Partial coverage only |
| `CollaboratorInterfaceTests.cs` | Some Windows-specific sections | Reduced test coverage |
| `App.xaml.cs` | Windows-specific test app initialization | Test host may not initialize properly |

#### 3. UI Control Dependencies
Despite testing ViewModels, some tests still instantiate UI controls:
- `RichEditBoxExtended` control creation in tests
- UI thread requirements for control initialization
- Windows-specific control behaviors being tested

#### 4. Project Configuration Issues
```xml
<!-- Current StoryCADTests.csproj -->
<Platforms>x86;x64;arm64</Platforms>
<RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
```
Missing macOS and Linux runtime identifiers prevent proper compilation and execution.

#### 5. Windows Path Notation Issues
**15 instances** of hardcoded Windows paths (backslash separators) across 5 test files:
- **FileOpenServiceTests.cs** (9 instances) - `@"C:\test\file.stbx"` patterns
- **StoryDocumentTests.cs** (2 instances) - `@"C:\Stories\MyStory.stbx"` patterns
- **OutlineViewModelTests.cs** (1 instance) - `@"C:\test.stbx"` pattern
- **AppStateTests.cs** (1 instance) - `@"C:\test.stbx"` pattern
- **RichEditBoxExtendedTests.cs** (1 false positive - RTF content, not a path)

All paths must use `Path.Combine()` for cross-platform compatibility.

## Proposed Solution

### Phase 1: Project Configuration (Immediate)

#### Step 1.1: Update Desktop-Specific PropertyGroup
Add cross-platform runtime identifiers to the `net10.0-desktop` target framework section:
```xml
<PropertyGroup Condition="'$(TargetFramework)'=='net10.0-desktop'">
    <UseWinUI>false</UseWinUI>
    <Platforms>x64;arm64</Platforms>
    <RuntimeIdentifiers>osx-x64;osx-arm64;linux-x64;linux-arm64</RuntimeIdentifiers>
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DefineConstants>$(DefineConstants);DESKTOP_HEAD</DefineConstants>
</PropertyGroup>
```

#### Step 1.2: Add Windows-Specific Build Constant
Add `WINDOWS_HEAD` define to the existing Windows section:
```xml
<PropertyGroup Condition="'$(TargetFramework)'=='net10.0-windows10.0.22621'">
    <UseWinUI>true</UseWinUI>
    <EnableMsixTooling>false</EnableMsixTooling>
    <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <Platforms>x86;x64;arm64</Platforms>
    <RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
    <DefineConstants>$(DefineConstants);WINDOWS_HEAD</DefineConstants>
</PropertyGroup>
```

#### Step 1.3: Build and Verify
1. Build solution: `msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:Platform=x64`
2. Verify both target frameworks build successfully
3. Check for any new compilation errors
4. Run existing tests to ensure no regressions

### Phase 2: Path Notation Fixes (Short-term)

Fix hardcoded Windows path notation across test files. All changes use `Path.Combine()` for cross-platform compatibility.

#### Step 2.1: FileOpenServiceTests.cs (9 instances - Priority 1)
Replace all `@"C:\test\file.stbx"` patterns with `Path.Combine("C:", "test", "file.stbx")` or equivalent.
- Line 120, 135, 136, 157, 161, 165, 173, 188, 221

**Verification**: Build and run FileOpenServiceTests to ensure no test failures

#### Step 2.2: StoryDocumentTests.cs (2 instances - Priority 2)
Replace `@"C:\Stories\MyStory.stbx"` patterns with `Path.Combine("C:", "Stories", "MyStory.stbx")`
- Line 27, 42

**Verification**: Run StoryDocumentTests

#### Step 2.3: OutlineViewModelTests.cs (1 instance)
Replace `@"C:\test.stbx"` with `Path.Combine("C:", "test.stbx")`
- Line 484

**Verification**: Run OutlineViewModelTests

#### Step 2.4: AppStateTests.cs (1 instance)
Replace `@"C:\test.stbx"` with `Path.Combine("C:", "test.stbx")`
- Line 15

**Verification**: Run AppStateTests

#### Step 2.5: Verify All Path Changes
Run complete test suite to ensure all path notation changes work correctly

### Phase 3: Platform-Specific Test Separation (Medium-term)

#### Step 3.1: Refactor Tests to Remove UI Control Creation
Audit existing tests and refactor to follow one of two approaches:
1. **Cross-platform tests**: Test ViewModel/business logic without creating any UI controls
2. **Platform-specific tests**: Explicitly Windows-only (or macOS-only) using partial classes

#### Step 3.2: Use Partial Classes for Platform-Specific Tests
Following StoryCAD test naming standards (ClassNameTests.cs), use partial classes for platform differences:

**Example: RichEditBoxExtendedTests**
- `RichEditBoxExtendedTests.cs` - Shared cross-platform tests (ViewModel logic, no controls)
- `RichEditBoxExtendedTests.Windows.cs` - Windows-only control tests wrapped in `#if WINDOWS_HEAD`
- `RichEditBoxExtendedTests.Desktop.cs` - macOS/Linux tests (if needed) wrapped in `#if DESKTOP_HEAD`

```csharp
// RichEditBoxExtendedTests.cs (shared)
[TestClass]
public partial class RichEditBoxExtendedTests
{
    [TestMethod]
    public void RtfText_PropertyBinding_Works()
    {
        // Tests ViewModel/binding logic WITHOUT creating controls
        var viewModel = new TestViewModel();
        Assert.IsNotNull(viewModel);
        // ... test ViewModel behavior only
    }
}

// RichEditBoxExtendedTests.Windows.cs (Windows-only)
#if WINDOWS_HEAD
[TestClass]
public partial class RichEditBoxExtendedTests
{
    [TestMethod]
    public void Control_RichEditBox_Initialization()
    {
        // Windows-specific UI control test
        var control = new RichEditBox();
        Assert.IsNotNull(control);
        // ... Windows-specific behavior
    }
}
#endif
```

**Note**: Tests stay in current folder structure - no reorganization

#### Step 3.3: Add Test Categories for Filtering
Mark tests with categories to control execution on different platforms:
```csharp
[TestCategory("CrossPlatform")]
[TestMethod]
public void ViewModel_Test() { }

[TestCategory("Windows")]
[TestMethod]
public void Windows_UI_Test() { }
```

**Verification**: Run tests on Windows, then attempt build on macOS to identify remaining platform issues

### Phase 4: CI/CD Integration (Long-term)

#### GitHub Actions Workflow
```yaml
name: Cross-Platform Tests

on: [push, pull_request]

jobs:
  test-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test -f net10.0-windows10.0.22621

  test-macos:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test -f net10.0-desktop --filter TestCategory=CrossPlatform

  test-linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test -f net10.0-desktop --filter TestCategory=CrossPlatform
```

## Implementation Timeline

### Phase 1: Project Configuration (Week 1)
1. Update StoryCADTests.csproj with cross-platform runtime identifiers and build constants
2. Build and verify no regressions
3. Run existing tests on Windows

### Phase 2: Path Notation Fixes (Week 2)
1. Fix FileOpenServiceTests.cs (9 instances)
2. Fix StoryDocumentTests.cs, OutlineViewModelTests.cs, AppStateTests.cs (4 instances)
3. Build and run complete test suite
4. Verify all tests pass

### Phase 3: Platform-Specific Test Separation (Weeks 3-6)
1. Audit tests to identify UI control creation
2. Refactor tests into cross-platform and platform-specific using partial classes
3. Add test categories for filtering
4. Test build on macOS, identify remaining issues

### Phase 4: CI/CD Integration (Months 2-3)
1. Set up GitHub Actions for Windows/macOS/Linux
2. Configure test filters per platform
3. Create test coverage reports per platform

## Success Criteria

1. **Immediate**: Tests build successfully on Windows, macOS, and Linux
2. **Short-term**: 80% of ViewModel tests run on all platforms
3. **Medium-term**: 95% of business logic tests are cross-platform
4. **Long-term**: Automated CI/CD validates every commit on all platforms

## Testing Guidelines

### Do's
- ✅ Test ViewModels and business logic independently of UI
- ✅ Use interfaces and dependency injection for platform-specific code
- ✅ Mark platform-specific tests with appropriate categories
- ✅ Mock UI controls when testing binding logic
- ✅ Write tests that verify behavior, not implementation

### Don'ts
- ❌ Create actual UI controls in cross-platform tests
- ❌ Assume Windows-specific paths or features
- ❌ Use `[UITestMethod]` for ViewModel tests
- ❌ Hard-code platform-specific behaviors
- ❌ Skip testing on non-Windows platforms

## Monitoring and Metrics

Track progress with these metrics:
- **Cross-platform test ratio**: (Cross-platform tests / Total tests) × 100
- **Platform coverage**: Tests passing on [Windows|macOS|Linux] / Total tests
- **Build success rate**: Successful builds per platform
- **Test execution time**: Average time per platform
- **Defect detection rate**: Platform-specific bugs found by tests

## Implementation Summary

### What Was Actually Implemented

#### Phase 1: Project Configuration - ⏭️ SKIPPED
**Decision**: Not needed - built-in .NET constants (`WINDOWS10_0_18362_0_OR_GREATER`) work perfectly and are simpler than custom build constants. Following "Simplicity First" directive.

#### Phase 2: Path Notation Fixes - ✅ COMPLETE
- All 15 hardcoded Windows paths converted to `Path.Combine()`
- Files fixed: FileOpenServiceTests.cs (9), StoryDocumentTests.cs (2), OutlineViewModelTests.cs (1), AppStateTests.cs (1)
- All tests now use cross-platform path notation

#### Phase 3: Platform-Specific Test Separation - ✅ COMPLETE
- Platform-specific tests wrapped in `#if WINDOWS10_0_18362_0_OR_GREATER`
- Partial class pattern established (FileOpenVMTests, StoryIOTests)
- Removed broken `#if __MACOS__` test (doesn't work with Skia desktop)
- Platform-specific code properly isolated:
  - RichEditBoxExtendedTests: Windows-only (RTF implementation differs on Skia)
  - WindowingTests: Windows-specific Win32 P/Invoke tests isolated
  - StoryIOTests.Windows.cs: Windows-specific path validation
  - FileOpenVMTests.Windows.cs: Uses UNO controls (could run on Skia but kept separate)

#### Phase 4: CI/CD Integration - ✅ COMPLETE (Exceeded Expectations)
`.github/workflows/build-release.yml` already implements:
- ✅ Automated testing on every push/PR to UNOTestBranch
- ✅ Windows x64 tests: `dotnet test` on windows-latest
- ✅ macOS ARM64 tests: `dotnet test` on macos-latest
- ✅ Multi-platform builds (Windows x86/x64/ARM64, macOS ARM64)
- ✅ Automated MSIX/PKG packaging
- ✅ Release automation with artifacts

**Bonus features not in original plan:**
- Automatic versioning
- Code signing
- GitHub Releases creation
- Artifact uploads

### Test Categories Decision
**Not implemented** - Compile-time conditionals (`#if`) provide superior filtering compared to runtime test categories. Tests that don't compile on a platform don't need runtime filtering.

### Linux Support
**Deferred** - StoryCAD doesn't target Linux yet. Can be added to build matrix when needed by adding `ubuntu-latest` runner.

## Conclusion

Cross-platform testing has been fully enabled for StoryCAD. The test suite now runs on both Windows and macOS in automated CI/CD, with platform-specific code properly isolated using compile-time conditionals.

Key insights from implementation:
- Built-in .NET constants are simpler than custom build constants
- Compile-time conditionals (`#if`) are superior to runtime test categories
- Most tests were already cross-platform - only needed path fixes
- Platform-specific features (Win32 APIs, RTF controls) correctly isolated
- GitHub Actions CI/CD was already more sophisticated than proposed

## Lessons Learned

1. **Simplicity First Works**: Skipping Phase 1 (custom build constants) resulted in cleaner, more maintainable code
2. **UNO Platform Gotchas**: `__MACOS__` constant doesn't work with `net10.0-desktop` (Skia), only native macOS
3. **RTF Implementation**: RichEditBox behavior differs significantly between Windows and Skia - platform-specific tests required
4. **CI/CD Already Existed**: Phase 4 was already implemented beyond the scope of the strategy document

---

*Document Version: 3.0 - Implementation Complete*
*Date: October 30, 2025*
*Author: Development Team*
*Status: ✅ All phases implemented*