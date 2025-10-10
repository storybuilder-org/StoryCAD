# Issue #1134: UpdateUIToTheme Circular Dependency Fix - Implementation Plan

**Created**: 2025-10-10
**Status**: Ready for Implementation
**Related Issue**: #1134 (Code cleanup)
**Related Docs**:
- `/mnt/c/temp/issue_1134_themechange_TODO.md` (analysis)
- `/mnt/d/dev/src/StoryCAD/devdocs/issue_1134_TODO_list.md` (lines 20-22)

---

## Problem Summary

`Windowing.UpdateUIToTheme()` creates circular dependencies by using Service Locator pattern to call ViewModel methods from a Model class.

### Circular Dependencies

1. **Windowing → OutlineViewModel** (via Service Locator at line 169)
2. **OutlineViewModel → Windowing** (constructor injection at OutlineViewModel.cs:58)
3. **Windowing → ShellViewModel** (via Service Locator at line 170)
4. **ShellViewModel → Windowing** (constructor injection at ShellViewModel.cs:88)

### Architecture Violations

- **Layer Violation**: Model class calling ViewModel methods
- **Service Locator Anti-Pattern**: Hides dependencies via `Ioc.Default.GetRequiredService<>()`
- **Poor Separation of Concerns**: Theme update method has unrelated side effects (file save, navigation)

---

## Solution: Messaging Pattern

**Approach**: Use CommunityToolkit.Mvvm.Messaging to decouple Windowing from ViewModels.

**Key Insight**: Have `Windowing.RequestedTheme` property send `ThemeChangedMessage` automatically when changed. This ensures any code changing the theme triggers updates, not just PreferencesViewModel.

**Benefits**:
- Eliminates circular dependencies
- Proper MVVM layer separation
- Follows StoryCAD's established manual registration pattern
- Future-proof (works from any caller)
- No orchestration needed

---

## Implementation Steps

### Step 1: Create ThemeChangedMessage

**File**: `StoryCADLib/Messages/ThemeChangedMessage.cs` (NEW FILE)

```csharp
using Microsoft.UI.Xaml;

namespace StoryCADLib.Messages;

/// <summary>
/// Message sent when the application theme changes.
/// Allows ViewModels to respond to theme changes without creating circular dependencies.
/// </summary>
public record ThemeChangedMessage
{
    /// <summary>
    /// The new theme that was applied.
    /// </summary>
    public required ElementTheme NewTheme { get; init; }
}
```

**Why a record?**
- Immutable by default
- Value-based equality
- Concise syntax
- Perfect for message objects

---

### Step 2: Modify Windowing.RequestedTheme Property

**File**: `StoryCADLib/Models/Windowing.cs:68-72`

**Before**:
```csharp
public ElementTheme RequestedTheme
{
    get => _requestedTheme;
    set => SetProperty(ref _requestedTheme, value);
}
```

**After**:
```csharp
public ElementTheme RequestedTheme
{
    get => _requestedTheme;
    set
    {
        if (SetProperty(ref _requestedTheme, value))
        {
            // Update UI theme immediately
            ((MainWindow.Content as FrameworkElement)!).RequestedTheme = value;

            // Notify subscribers of theme change
            WeakReferenceMessenger.Default.Send(new ThemeChangedMessage { NewTheme = value });
        }
    }
}
```

**Changes**:
- Use conditional `SetProperty` return value (only true if value changed)
- Update UI theme directly in property setter
- Send message to notify interested parties
- No async needed (message handlers are responsible for their own async operations)

**Add using statement at top of file**:
```csharp
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Messages;
```

---

### Step 3: Delete UpdateUIToTheme() Method

**File**: `StoryCADLib/Models/Windowing.cs:162-172`

**Delete entirely**:
```csharp
/// <summary>
///     This will update the elements of the UI to match the theme set in RequestedTheme.
/// </summary>
public async void UpdateUIToTheme()
{
    ((MainWindow.Content as FrameworkElement)!).RequestedTheme = RequestedTheme;

    //Save file, close current node since it won't be the right theme.
    if (!string.IsNullOrEmpty(appState.CurrentDocument?.FilePath))
    {
        await Ioc.Default.GetRequiredService<OutlineViewModel>().SaveFile();
        Ioc.Default.GetRequiredService<ShellViewModel>().ShowHomePage();
    }
}
```

**Rationale**: This functionality is now handled by:
1. Property setter updates UI theme
2. Message sent to interested parties
3. OutlineViewModel message handler saves file and navigates

---

### Step 4: Update PreferencesViewModel.SaveModel()

**File**: `StoryCADLib/ViewModels/Tools/PreferencesViewModel.cs:307-311`

**Before**:
```csharp
if (CurrentModel.ThemePreference != PreferedTheme)
{
    _windowing.RequestedTheme = CurrentModel.ThemePreference;
    _windowing.UpdateUIToTheme();
}
```

**After**:
```csharp
if (CurrentModel.ThemePreference != PreferedTheme)
{
    _windowing.RequestedTheme = CurrentModel.ThemePreference;
    // Theme change message is now sent automatically by the property setter
    // No need to call UpdateUIToTheme() - it's been deleted
}
```

**Changes**:
- Remove call to `UpdateUIToTheme()`
- Add comment explaining the automatic behavior
- Setting property is sufficient to trigger all updates

**Note**: SaveModel() does NOT need to become async. The message handler in OutlineViewModel handles async operations independently.

---

### Step 5: Add Message Handler to OutlineViewModel

**File**: `StoryCADLib/ViewModels/SubViewModels/OutlineViewModel.cs`

#### 5A. Add using statement (top of file)
```csharp
using StoryCADLib.Messages;
```

#### 5B. Update constructor (lines 51-65)

**Add to constructor body** (after existing initializations):
```csharp
public OutlineViewModel(ILogService logService, PreferenceService preferenceService,
    Windowing windowing, OutlineService outlineService, AppState appState,
    SearchService searchService, BackendService backendService, EditFlushService editFlushService,
    AutoSaveService autoSaveService)
{
    logger = logService;
    preferences = preferenceService;
    window = windowing;
    this.outlineService = outlineService;
    this.appState = appState;
    this.searchService = searchService;
    _backendService = backendService;
    _editFlushService = editFlushService;
    _autoSaveService = autoSaveService;

    // Register message handler for theme changes
    // Uses static handler for better performance (matches ShellViewModel pattern)
    Messenger.Register<OutlineViewModel, ThemeChangedMessage>(this,
        static (recipient, message) => recipient.HandleThemeChanged(message));
}
```

#### 5C. Add message handler method

**Add new private method** (location: near other message handlers or at end of class):
```csharp
/// <summary>
/// Handles theme change messages by saving the current file and navigating to home page.
/// This is necessary because the current node won't have the correct theme after the change.
/// </summary>
/// <param name="message">The theme changed message containing the new theme.</param>
private async void HandleThemeChanged(ThemeChangedMessage message)
{
    logger.Log(LogLevel.Info, $"Theme changed to {message.NewTheme}, handling file save and navigation");

    // Save file and navigate to home if a file is open
    // (required because current node won't have correct theme)
    if (!string.IsNullOrEmpty(appState.CurrentDocument?.FilePath))
    {
        logger.Log(LogLevel.Debug, "Saving current file before theme change");
        await SaveFile();

        logger.Log(LogLevel.Debug, "Navigating to home page after theme change");
        var shellVm = Ioc.Default.GetRequiredService<ShellViewModel>();
        shellVm.ShowHomePage();
    }
}
```

**Note**: Using Service Locator for ShellViewModel is acceptable here because:
1. OutlineViewModel already has circular dependency with ShellViewModel (documented in TODO list)
2. This is a temporary solution until broader architectural refactoring addresses all circular dependencies
3. Adding ShellViewModel to constructor would worsen the circular dependency

---

### Step 6: (Optional) Add Message Handler to ShellViewModel

**File**: `StoryCADLib/ViewModels/ShellViewModel.cs`

**Status**: Optional enhancement - only if ShellViewModel needs to respond to theme changes

**If needed, follow same pattern as OutlineViewModel**:
1. Register handler in constructor
2. Implement `HandleThemeChanged(ThemeChangedMessage message)` method
3. Perform any ShellViewModel-specific theme change logic

**Current Assessment**: Not needed for this fix. ShellViewModel.ShowHomePage() is called by OutlineViewModel's handler.

---

## Testing Strategy

**Important**: StoryCAD does NOT use mocking. All tests use real services via `Ioc.Default.GetRequiredService<>()`.

### Test 1: ThemeChangedMessage Tests

**File**: `StoryCADTests/Messages/ThemeChangedMessageTests.cs` (NEW FILE)

```csharp
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Messages;

namespace StoryCADTests.Messages;

[TestClass]
public class ThemeChangedMessageTests
{
    [TestMethod]
    public void ThemeChangedMessage_CanBeCreated_WithRequiredProperty()
    {
        // Arrange & Act
        var message = new ThemeChangedMessage { NewTheme = ElementTheme.Dark };

        // Assert
        Assert.AreEqual(ElementTheme.Dark, message.NewTheme);
    }

    [TestMethod]
    public void ThemeChangedMessage_RecordsAreEqual_WhenThemeMatches()
    {
        // Arrange
        var message1 = new ThemeChangedMessage { NewTheme = ElementTheme.Light };
        var message2 = new ThemeChangedMessage { NewTheme = ElementTheme.Light };

        // Assert
        Assert.AreEqual(message1, message2);
    }

    [TestMethod]
    public void ThemeChangedMessage_RecordsAreNotEqual_WhenThemeDiffers()
    {
        // Arrange
        var message1 = new ThemeChangedMessage { NewTheme = ElementTheme.Light };
        var message2 = new ThemeChangedMessage { NewTheme = ElementTheme.Dark };

        // Assert
        Assert.AreNotEqual(message1, message2);
    }
}
```

---

### Test 2: Windowing Property Tests

**File**: `StoryCADTests/Models/WindowingTests.cs` (NEW FILE or add to existing)

```csharp
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Messages;
using StoryCADLib.Models;

namespace StoryCADTests.Models;

[TestClass]
public class WindowingThemeTests
{
    private Windowing _windowing;
    private bool _messageReceived;
    private ThemeChangedMessage _receivedMessage;

    [TestInitialize]
    public void Setup()
    {
        _windowing = Ioc.Default.GetRequiredService<Windowing>();
        _messageReceived = false;
        _receivedMessage = null;

        // Register test message handler
        WeakReferenceMessenger.Default.Register<WindowingThemeTests, ThemeChangedMessage>(this,
            static (r, m) => r.HandleTestMessage(m));
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Unregister test handler
        WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
    }

    [TestMethod]
    public void RequestedTheme_WhenChanged_SendsThemeChangedMessage()
    {
        // Arrange
        var newTheme = ElementTheme.Dark;

        // Act
        _windowing.RequestedTheme = newTheme;

        // Assert
        Assert.IsTrue(_messageReceived, "Message should have been sent");
        Assert.IsNotNull(_receivedMessage, "Received message should not be null");
        Assert.AreEqual(newTheme, _receivedMessage.NewTheme, "Message should contain new theme");
    }

    [TestMethod]
    public void RequestedTheme_WhenSetToSameValue_DoesNotSendMessage()
    {
        // Arrange
        var theme = ElementTheme.Light;
        _windowing.RequestedTheme = theme;
        _messageReceived = false; // Reset flag

        // Act
        _windowing.RequestedTheme = theme; // Set to same value

        // Assert
        Assert.IsFalse(_messageReceived, "Message should not be sent when value doesn't change");
    }

    [TestMethod]
    public void RequestedTheme_WhenChanged_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _windowing.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Windowing.RequestedTheme))
                propertyChangedRaised = true;
        };

        // Act
        _windowing.RequestedTheme = ElementTheme.Dark;

        // Assert
        Assert.IsTrue(propertyChangedRaised, "PropertyChanged should be raised");
    }

    private void HandleTestMessage(ThemeChangedMessage message)
    {
        _messageReceived = true;
        _receivedMessage = message;
    }
}
```

---

### Test 3: OutlineViewModel Message Handler Tests

**File**: `StoryCADTests/ViewModels/SubViewModels/OutlineViewModelTests.cs` (add to existing file)

```csharp
[UITestMethod]
public async Task HandleThemeChanged_WithOpenFile_SavesFileAndNavigatesToHome()
{
    // Arrange
    var appState = Ioc.Default.GetRequiredService<AppState>();
    var shellViewModel = Ioc.Default.GetRequiredService<ShellViewModel>();

    // Create a test file
    var result = await outlineService.CreateEmptyOutline("TestThemeChange.stbx");
    Assert.IsTrue(result.IsSuccess, "Should create test file");
    appState.CurrentDocument = new StoryCADLib.DAL.StoryDocument { FilePath = result.Value };

    // Act - Send theme change message
    WeakReferenceMessenger.Default.Send(new ThemeChangedMessage { NewTheme = ElementTheme.Dark });

    // Allow async operations to complete
    await Task.Delay(100);

    // Assert
    // File should be saved (check via file system or service state)
    Assert.IsNotNull(appState.CurrentDocument, "Document should still exist after save");

    // Note: Verifying ShowHomePage() was called is difficult without mocking
    // Manual testing required to verify navigation behavior
}

[UITestMethod]
public async Task HandleThemeChanged_WithoutOpenFile_DoesNothing()
{
    // Arrange
    var appState = Ioc.Default.GetRequiredService<AppState>();
    appState.CurrentDocument = null;

    // Act - Send theme change message
    WeakReferenceMessenger.Default.Send(new ThemeChangedMessage { NewTheme = ElementTheme.Light });

    // Allow async operations to complete
    await Task.Delay(100);

    // Assert - No exception should be thrown
    Assert.IsNull(appState.CurrentDocument, "Document should remain null");
}
```

---

### Test 4: PreferencesViewModel Theme Change Tests

**File**: `StoryCADTests/ViewModels/Tools/PreferencesViewModelTests.cs` (add to existing file)

```csharp
[UITestMethod]
public async Task SaveModel_WhenThemeChanges_UpdatesWindowingRequestedTheme()
{
    // Arrange
    var preferencesViewModel = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    var windowing = Ioc.Default.GetRequiredService<Windowing>();

    // Set different theme
    preferencesViewModel.PreferedTheme = ElementTheme.Dark;
    preferencesViewModel.CurrentModel.ThemePreference = ElementTheme.Light;

    // Act
    await preferencesViewModel.SaveModel();

    // Assert
    Assert.AreEqual(ElementTheme.Dark, windowing.RequestedTheme,
        "Windowing.RequestedTheme should be updated");
}

[UITestMethod]
public async Task SaveModel_WhenThemeChanges_TriggersThemeChangedMessage()
{
    // Arrange
    var preferencesViewModel = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    var messageReceived = false;
    ElementTheme receivedTheme = ElementTheme.Default;

    WeakReferenceMessenger.Default.Register<PreferencesViewModelTests, ThemeChangedMessage>(this,
        (r, m) =>
        {
            messageReceived = true;
            receivedTheme = m.NewTheme;
        });

    preferencesViewModel.PreferedTheme = ElementTheme.Dark;
    preferencesViewModel.CurrentModel.ThemePreference = ElementTheme.Light;

    // Act
    await preferencesViewModel.SaveModel();

    // Assert
    Assert.IsTrue(messageReceived, "ThemeChangedMessage should be sent");
    Assert.AreEqual(ElementTheme.Dark, receivedTheme, "Message should contain new theme");

    // Cleanup
    WeakReferenceMessenger.Default.Unregister<ThemeChangedMessage>(this);
}
```

---

### Manual Testing

1. **Open StoryCAD application**
2. **Open a story file** (File → Open or create new)
3. **Navigate to Preferences** (Tools → Preferences)
4. **Change theme** (Dark → Light or Light → Dark)
5. **Click OK to save preferences**
6. **Verify**:
   - ✅ UI theme changes immediately
   - ✅ Current file is saved
   - ✅ Application navigates to home page
   - ✅ No errors in log
7. **Reopen the file** to verify save occurred
8. **Test with no file open**:
   - Close all files (navigate to home page)
   - Change theme in Preferences
   - Verify no errors occur

---

## Documentation Updates

### Update TODO List

**File**: `/mnt/d/dev/src/StoryCAD/devdocs/issue_1134_TODO_list.md`

**Lines 20-22 (Current)**:
```markdown
#### Windowing.cs
- **Line 159**: ARCHITECTURAL ISSUE - Windowing (UI layer) should not depend on OutlineViewModel (business logic)
- **Line 188**: ARCHITECTURAL ISSUE - Same as UpdateWindowTitle above
```

**Replace with**:
```markdown
#### Windowing.cs
- **Line 159**: ~~ARCHITECTURAL ISSUE - Windowing (UI layer) should not depend on OutlineViewModel (business logic)~~ **RESOLVED** - Implemented messaging pattern with `ThemeChangedMessage` to decouple Windowing from ViewModels. See `/mnt/d/dev/src/StoryCAD/devdocs/issue_1134_theme_change_TODO_plan.md`
- **Line 188**: ARCHITECTURAL ISSUE - Same as UpdateWindowTitle above
```

---

### Update Legacy Constructors Document

**File**: `/mnt/d/dev/src/StoryCAD/devdocs/issue_1134_legacy_constructors.md`

**Lines 431-432 (Current)**:
```markdown
- **Line 169**: Uses Service Locator to get OutlineViewModel (circular dependency)
- **Line 170**: Uses Service Locator to get ShellViewModel (circular dependency)
```

**Replace with**:
```markdown
- **Line 169**: ~~Uses Service Locator to get OutlineViewModel (circular dependency)~~ **RESOLVED** - Refactored to use messaging pattern
- **Line 170**: ~~Uses Service Locator to get ShellViewModel (circular dependency)~~ **RESOLVED** - Refactored to use messaging pattern
```

---

## Architecture Notes Update

**File**: `.claude/docs/architecture/StoryCAD_architecture_notes.md`

**Add to "Messaging Patterns" section** (after existing examples):

```markdown
### Example: Theme Change Messaging

When the application theme changes, `Windowing.RequestedTheme` property sends a `ThemeChangedMessage`:

**Sender (Windowing.cs)**:
```csharp
public ElementTheme RequestedTheme
{
    get => _requestedTheme;
    set
    {
        if (SetProperty(ref _requestedTheme, value))
        {
            ((MainWindow.Content as FrameworkElement)!).RequestedTheme = value;
            WeakReferenceMessenger.Default.Send(new ThemeChangedMessage { NewTheme = value });
        }
    }
}
```

**Receiver (OutlineViewModel.cs)**:
```csharp
// In constructor
Messenger.Register<OutlineViewModel, ThemeChangedMessage>(this,
    static (recipient, message) => recipient.HandleThemeChanged(message));

// Handler method
private async void HandleThemeChanged(ThemeChangedMessage message)
{
    if (!string.IsNullOrEmpty(appState.CurrentDocument?.FilePath))
    {
        await SaveFile();
        shellVm.ShowHomePage();
    }
}
```

**Benefits**:
- Eliminates circular dependency between Windowing and OutlineViewModel
- Proper MVVM layer separation (Model notifies, ViewModel reacts)
- Future-proof (any code changing theme triggers updates)
```

---

## Benefits of This Solution

### 1. Eliminates Circular Dependencies
- Windowing no longer calls ViewModels directly
- Clean unidirectional dependency flow
- Proper MVVM architecture

### 2. Proper Layer Separation
- Model (Windowing) handles UI concerns only
- ViewModel (OutlineViewModel) orchestrates business logic
- Messages provide decoupled communication

### 3. Better Testability
- Each component has clear, focused responsibilities
- Dependencies are explicit (no hidden Service Locator calls in business logic)
- Message flow can be tested independently

### 4. Improved Maintainability
- Single Responsibility Principle honored
- Clear separation of concerns
- Easy to understand call chain
- Future changes to theme handling won't require ViewModel modifications

### 5. Follows Project Principles
- Uses StoryCAD's established manual registration pattern (matches ShellViewModel)
- No mocking in tests - uses real services via IoC
- "Simplest solution that could possibly work" (CLAUDE.md)
- Proper DI usage (no Service Locator in business logic)
- Aligns with existing architecture

### 6. Future-Proof
- Any code changing `Windowing.RequestedTheme` automatically triggers all updates
- Easy to add more subscribers (e.g., if SettingsViewModel needs notification)
- Messaging pattern is well-understood and documented

---

## Implementation Checklist

- [ ] **Step 1**: Create `ThemeChangedMessage.cs`
- [ ] **Step 2**: Modify `Windowing.RequestedTheme` property
- [ ] **Step 3**: Delete `Windowing.UpdateUIToTheme()` method
- [ ] **Step 4**: Update `PreferencesViewModel.SaveModel()`
- [ ] **Step 5**: Add message handler to `OutlineViewModel`
- [ ] **Step 6** (Optional): Add message handler to `ShellViewModel` if needed
- [ ] **Test 1**: Create `ThemeChangedMessageTests.cs`
- [ ] **Test 2**: Create or update `WindowingTests.cs`
- [ ] **Test 3**: Add tests to `OutlineViewModelTests.cs`
- [ ] **Test 4**: Add tests to `PreferencesViewModelTests.cs`
- [ ] **Manual Testing**: Verify theme change workflow
- [ ] **Doc Update 1**: Update `issue_1134_TODO_list.md`
- [ ] **Doc Update 2**: Update `issue_1134_legacy_constructors.md`
- [ ] **Doc Update 3**: Update `StoryCAD_architecture_notes.md`
- [ ] **Build**: Ensure solution builds without errors
- [ ] **Test Run**: Run all tests and verify they pass
- [ ] **Code Review**: Review changes for quality and consistency
- [ ] **PR**: Create pull request with detailed description

---

## Risks & Mitigation

### Risk 1: Message Handler Not Called

**Symptoms**: Theme changes but file not saved / navigation doesn't occur

**Mitigation**:
- Verify message handler is registered in OutlineViewModel constructor
- Add logging to handler to confirm it's being called
- Check WeakReferenceMessenger.Default is used consistently
- Manual testing will catch this immediately

### Risk 2: Race Conditions

**Symptoms**: Intermittent failures in SaveFile() or ShowHomePage()

**Mitigation**:
- Message handlers run on UI thread (MessengerExtensions ensures this)
- SaveFile() is async and properly awaited
- Manual testing with rapid theme changes will reveal issues

### Risk 3: Memory Leaks from Message Subscriptions

**Symptoms**: OutlineViewModel not garbage collected

**Mitigation**:
- WeakReferenceMessenger uses weak references (no strong references to recipients)
- Static handler methods prevent closure captures
- Following established ShellViewModel pattern ensures safety

### Risk 4: Breaking Existing Behavior

**Symptoms**: Theme changes don't work as before

**Mitigation**:
- Comprehensive unit tests verify each step
- Manual testing verifies end-to-end workflow
- Changes are minimal and focused
- Easy to revert if issues arise

---

## Post-Implementation Notes

After implementation, add notes here about:
- Any unexpected issues encountered
- Deviations from the plan and why
- Performance observations
- Suggestions for future improvements

---

## References

### Code Locations
- **Windowing.RequestedTheme**: `StoryCADLib/Models/Windowing.cs:68-72`
- **Windowing.UpdateUIToTheme()**: `StoryCADLib/Models/Windowing.cs:162-172` (TO BE DELETED)
- **PreferencesViewModel.SaveModel()**: `StoryCADLib/ViewModels/Tools/PreferencesViewModel.cs:307-311`
- **OutlineViewModel Constructor**: `StoryCADLib/ViewModels/SubViewModels/OutlineViewModel.cs:51-65`
- **ShellViewModel Messaging Pattern**: `StoryCADLib/ViewModels/ShellViewModel.cs:99-105`

### Documentation
- **Analysis Document**: `/mnt/c/temp/issue_1134_themechange_TODO.md`
- **TODO List**: `/mnt/d/dev/src/StoryCAD/devdocs/issue_1134_TODO_list.md` (lines 20-22)
- **Legacy Constructors**: `/mnt/d/dev/src/StoryCAD/devdocs/issue_1134_legacy_constructors.md` (lines 431-432)
- **Architecture Notes**: `.claude/docs/architecture/StoryCAD_architecture_notes.md` (lines 2054-2067 - messaging pattern)
- **Testing Guide**: `.claude/docs/architecture/testing-guide.md` (real services, no mocking)
- **MVVM Toolkit Guide**: `.claude/docs/dependencies/mvvm-toolkit-guide.md`

### Related Issues
- **Issue #1134**: Code cleanup (parent issue)
- **Issue #1136**: Circular dependency OutlineViewModel ↔ StoryIO (related architectural issue)

---

**End of Implementation Plan**
