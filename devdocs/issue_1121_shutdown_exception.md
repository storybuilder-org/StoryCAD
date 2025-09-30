# Win32 Exception on Application Shutdown #1121

## Code Tasks Planning (TDD Approach)

### Test-Driven Development Strategy
Following TDD, we'll write tests first that demonstrate the bug, then implement the fix, ensuring no regression.

### Code Tasks

#### 1. Create Unit Tests for Shutdown Behavior
- Write test class `ShellViewModelShutdownTests.cs`
- Test: `OnShutdown_WithOpenDocument_CallsCloseFile`
- Test: `OnShutdown_WithoutDocument_DoesNotCallCloseFile`
- Test: `OnShutdown_UpdatesCumulativeTimeUsed`
- Test: `OnShutdown_SavesPreferences`
- Test: `OnShutdown_DestroysCollaborator`
- Test: `OnShutdown_SetsIsClosingFlag`
- Verify tests fail (demonstrating the bug exists)

#### 2. Refactor Shutdown Logic for Testability
- Extract shutdown logic from Shell.xaml.cs event handler
- Create `ShellViewModel.OnApplicationClosing()` method
- Make it async and testable
- Ensure it can be mocked/verified in tests

#### 3. Implement Shutdown Cleanup
- Add CloseFile call when document is open
- Add cumulative time tracking (need StartTime field)
- Add preference saving for time tracking
- Add Collaborator cleanup
- Add status timer stop
- Set IsClosing flag last

#### 4. Update Shell.xaml.cs
- Replace inline lambda with call to ShellViewModel.OnApplicationClosing()
- Handle async properly in event handler
- Ensure errors don't prevent shutdown

#### 5. Run and Fix Tests
- Build test project
- Run all shutdown tests
- Fix any failing tests
- Ensure all tests pass

#### 6. Add Integration Test
- Test actual shutdown sequence with document open
- Test shutdown with unsaved changes
- Verify no exceptions in debug mode

#### 7. Manual Testing
- Test in Visual Studio debug mode
- Close with X button - verify no exception
- Close with File > Exit - verify no exception
- Test with and without open document

### Test Details

#### Test Setup Requirements
```csharp
// Mocks needed:
- Mock<OutlineViewModel>
- Mock<PreferencesService>
- Mock<PreferencesIo>
- Mock<CollaboratorService>
- Mock<AppState>
- Mock<ILogService>
```

#### Example Test Structure
```csharp
[TestMethod]
public async Task OnShutdown_WithOpenDocument_CallsCloseFile()
{
    // Arrange
    var mockOutline = new Mock<OutlineViewModel>();
    var mockAppState = new Mock<AppState>();
    mockAppState.Setup(x => x.CurrentDocument).Returns(new StoryDocument());
    var shellVm = new ShellViewModel(...);

    // Act
    await shellVm.OnApplicationClosing();

    // Assert
    mockOutline.Verify(x => x.CloseFile(), Times.Once);
}
```

### Implementation Notes

1. **Thread Safety**: Use SerializationLock where needed
2. **Error Handling**: Log but don't throw - shutdown must complete
3. **Async Handling**: Ensure async operations complete before shutdown
4. **Platform Compatibility**: Test on both WinAppSDK and Desktop heads

### Files to Modify
- `StoryCAD/Views/Shell.xaml.cs` - Update shutdown handler
- `StoryCADLib/ViewModels/ShellViewModel.cs` - Add OnApplicationClosing method
- `StoryCADTests/ViewModels/ShellViewModelShutdownTests.cs` - New test file

### Success Criteria
- All unit tests pass
- No Win32 exception when closing in debug mode
- Document properly closed if open
- Preferences saved with updated time
- No regression in normal operation