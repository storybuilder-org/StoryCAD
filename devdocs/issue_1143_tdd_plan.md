# Issue #1143 - TDD Implementation Plan

## Test Status Review

### Existing Tests

1. **FileOpenServiceTests.cs** - HAS TESTS ✅
   - Currently includes `ShellViewModel` in Setup (line 44)
   - Tests cover: creation, file opening, UpdateRecents
   - **IMPORTANT**: Tests will BREAK when we remove `ShellViewModel` parameter

2. **FileOpenVMTests.cs** - LIMITED TESTS ⚠️
   - Basic initialization tests only
   - Missing tests for `ConfirmClicked()` navigation behavior
   - Has one ignored test due to UI thread requirement

3. **ShellViewModelTests.cs** - HAS TreeViewNodeClicked TESTS ✅
   - Multiple tests for TreeViewNodeClicked with different node types
   - Tests verify correct page navigation for each element type

4. **PreferencesIOTests.cs** - NO TEST FOR NEW PREFERENCE ❌
   - Tests serialization/deserialization
   - Does NOT test `ShowFilePickerOnStartup` property

### Missing Tests

1. **No test for ShowFilePickerOnStartup preference**
2. **No tests for FileOpenVM.ConfirmClicked navigation behavior**
3. **No tests for OutlineViewModel file open navigation** (if it exists)

## TDD Implementation Plan

### Phase 1: Write Failing Tests First

#### Test 1: PreferencesModel - ShowFilePickerOnStartup Serialization
**File**: `StoryCADTests/DAL/PreferencesIOTests.cs`

```csharp
[TestMethod]
public async Task PreferencesModel_ShowFilePickerOnStartup_SerializesCorrectly()
{
    // Arrange
    var prefsIo = new PreferencesIo();
    var expectedModel = new PreferencesModel
    {
        ShowFilePickerOnStartup = false  // Non-default value
    };

    // Act
    await prefsIo.WritePreferences(expectedModel);
    var actual = await prefsIo.ReadPreferences();

    // Assert
    Assert.AreEqual(expectedModel.ShowFilePickerOnStartup, actual.ShowFilePickerOnStartup);
}

[TestMethod]
public void PreferencesModel_ShowFilePickerOnStartup_DefaultsToTrue()
{
    // Arrange & Act
    var model = new PreferencesModel();

    // Assert
    Assert.IsTrue(model.ShowFilePickerOnStartup);
}
```

**Status**: This feature already exists, so these tests should PASS. Add them for documentation.

#### Test 2: FileOpenService - Should NOT Depend on ShellViewModel
**File**: `StoryCADTests/Services/Outline/FileOpenServiceTests.cs`

```csharp
[TestMethod]
public void FileOpenService_CanBeCreated_WithoutShellViewModel()
{
    // Arrange & Act - Create service without ShellViewModel
    var service = new FileOpenService(
        _logger,
        _outlineService,
        _appState,
        _editFlushService,
        _preferences,
        _windowing,
        _backupService,
        _autoSaveService,
        _storyIO
        // Note: NO ShellViewModel parameter
    );

    // Assert
    Assert.IsNotNull(service);
}
```

**Status**: This test will FAIL until we remove ShellViewModel from FileOpenService constructor.

#### Test 3: FileOpenVM - Should Navigate to Overview After File Open
**File**: `StoryCADTests/ViewModels/FileOpenVMTests.cs`

```csharp
[TestMethod]
[UITestMethod]  // Requires UI thread for TreeViewNodeClicked
public async Task ConfirmClicked_AfterOpeningRecent_NavigatesToOverview()
{
    // Arrange
    var fileOpenVM = Ioc.Default.GetRequiredService<FileOpenVM>();
    var shellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
    var appState = Ioc.Default.GetRequiredService<AppState>();

    // Create a test file
    var testFile = await TestStoryFactory.CreateTestStory("NavigationTest");
    await Ioc.Default.GetRequiredService<FileOpenService>().UpdateRecents(testFile);

    fileOpenVM.SelectedRecentIndex = 0;
    var recentTab = new NavigationViewItem { Tag = "Recent" };
    fileOpenVM.CurrentTab = recentTab;

    // Act
    fileOpenVM.ConfirmClicked();
    await Task.Delay(500); // Allow async operations to complete

    // Assert
    Assert.IsNotNull(appState.CurrentDocument);
    Assert.IsNotNull(shellVM.CurrentNode);
    Assert.AreEqual(StoryItemType.StoryOverview, shellVM.CurrentNode.Type);
}
```

**Status**: This test will FAIL until we add navigation to FileOpenVM.

### Phase 2: Update Existing Tests to Reflect Architecture Fix

#### Update FileOpenServiceTests Setup
**File**: `StoryCADTests/Services/Outline/FileOpenServiceTests.cs`

```csharp
[TestInitialize]
public void Setup()
{
    // Get services from IoC container
    _logger = Ioc.Default.GetRequiredService<ILogService>();
    _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
    _appState = Ioc.Default.GetRequiredService<AppState>();
    _editFlushService = Ioc.Default.GetRequiredService<EditFlushService>();
    _preferences = Ioc.Default.GetRequiredService<PreferenceService>();
    _windowing = Ioc.Default.GetRequiredService<Windowing>();
    _backupService = Ioc.Default.GetRequiredService<BackupService>();
    _autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
    _storyIO = Ioc.Default.GetRequiredService<StoryIO>();
    // REMOVE: _shellVM = Ioc.Default.GetRequiredService<ShellViewModel>();

    _fileOpenService = new FileOpenService(
        _logger,
        _outlineService,
        _appState,
        _editFlushService,
        _preferences,
        _windowing,
        _backupService,
        _autoSaveService,
        _storyIO
        // REMOVE: _shellVM
    );

    // Reset app state
    _appState.CurrentDocument = null;
}
```

### Phase 3: Implementation (Red-Green-Refactor)

#### Step 1: Make Tests Fail (Red)
1. Run existing tests - they should PASS
2. Add new test: `FileOpenService_CanBeCreated_WithoutShellViewModel` - should FAIL
3. Add new test: `ConfirmClicked_AfterOpeningRecent_NavigatesToOverview` - should FAIL

#### Step 2: Implement Fix (Green)

##### 2a. Remove ShellViewModel from FileOpenService
```csharp
// In FileOpenService.cs
// Remove ShellViewModel parameter from constructor
// Remove _shellVM field
// Remove _shellVM.TreeViewNodeClicked() call
```

**Run tests**: `FileOpenService_CanBeCreated_WithoutShellViewModel` should now PASS
**Run tests**: Other FileOpenServiceTests should still PASS (no behavior change)

##### 2b. Add Navigation to FileOpenVM
```csharp
// In FileOpenVM.ConfirmClicked()
// After each successful file operation, add navigation code
```

**Run tests**: `ConfirmClicked_AfterOpeningRecent_NavigatesToOverview` should now PASS

#### Step 3: Refactor and Verify
1. Build solution
2. Run ALL tests
3. Fix any broken tests
4. Verify no regressions

### Phase 4: Additional Tests to Consider

1. **Test for each FileOpenVM path**:
   - Opening recent file → navigates to Overview
   - Opening sample → navigates to Overview
   - Opening backup → navigates to Overview
   - Creating new file → navigates to Overview

2. **Test for ShowFilePickerOnStartup preference**:
   - Shell startup with preference = true → shows picker
   - Shell startup with preference = false → doesn't show picker

3. **Test for edge cases**:
   - Navigation when ExplorerView is empty
   - Navigation when CurrentDocument is null

## Summary

**Existing Test Coverage**: Good for FileOpenService and ShellViewModel
**Missing Test Coverage**: FileOpenVM navigation behavior, new preference
**TDD Approach**:
1. Write failing tests first (new behavior)
2. Update existing tests (breaking changes)
3. Implement fixes to make tests pass
4. Refactor and verify all tests pass
