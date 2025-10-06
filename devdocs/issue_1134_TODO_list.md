# StoryCAD TODO List

Generated for issue #1134 - Code cleanup

## Architecture Issues

### Circular Dependencies

#### StoryIO.cs
- **Line 65**: Circular dependency - StoryIO ↔ OutlineViewModel prevents injecting Windowing
- **Line 267**: Circular dependency - StoryIO ↔ OutlineViewModel prevents injecting Windowing
- **Line 305**: Static method requires Ioc.Default until refactored to instance method or removed logging

#### OutlineViewModel.cs
- **Line 58**: Circular dependency - OutlineViewModel ↔ AutoSaveService/BackupService

#### OutlineService.cs
- **Line 19**: Circular dependency - OutlineService ↔ AutoSaveService/BackupService ↔ OutlineViewModel ↔ OutlineService

#### Windowing.cs
- **Line 159**: ARCHITECTURAL ISSUE - Windowing (UI layer) should not depend on OutlineViewModel (business logic)
- **Line 188**: ARCHITECTURAL ISSUE - Same as UpdateWindowTitle above

#### NarrativeToolVM.cs
- **Line 25**: ShellViewModel dependency is still required for CurrentViewType, CurrentNode, and RightTappedNode

#### ToolValidationService.cs
- **Line 16**: In a future refactoring, the CurrentViewType, CurrentNode, and RightTappedNode properties [should be moved/refactored]

## Data Access Layer (DAL)

### StoryIO.cs
- **Line 231**: Investigate alternatives on other platforms

### ScrivenerIO.cs
- **Line 245**: Handle Unknown and default cases in switch (and hence remove the following Resharper ignore comment)

## Services

### Reports - ScrivenerReports.cs
- **Line 55**: Load templates from within ReportFormatter
- **Line 246**: Activate notes collecting
- **Line 331**: Complete SaveScrivenerNotes code
- **Line 354**: Is this formatting added?

### Collaborator - CollaboratorService.cs
- **Line 215-216**: CollaboratorWindow.MinWidth/MinHeight commented out (decide to use or remove)
- **Line 435**: Use Show and hide properly
- **Line 475**: Use lock here
- **Line 482**: Absolutely make sure Collaborator is not left in memory after this
- **Line 504**: Add FTP upload code here
- **Line 513**: On calls, set callback delegate
- **Line 525**: On calls, set model etc.

### Backend - BackendService.cs
- **Line 67**: This try/catch swallows all exceptions silently. Consider: [improving error handling]

### IoC - ServiceLocator.cs
- **Line 44**: Support replacing the base services

### API - SemanticKernelAPI.cs
- **Line 225**: Force set uuid somehow

## ViewModels

### ShellViewModel.cs
- **Line 306**: Static method requires service locator - consider making non-static in future refactoring
- **Line 566**: Raise event for StoryModel change?
- **Line 665**: Logging???

### OutlineViewModel.cs
- **Line 674**: Revamp this to be more user-friendly
- **Line 1135**: This suppresses API calls on failure and needs to be handled

### CharacterViewModel.cs
- **Line 893**: How do I bind to Sex option buttons?
- **Line 936**: How do I bind to Sex option buttons?

### NarrativeToolVM.cs
- **Line 129**: Possibly Merge with StoryElement.Delete() Method
- **Line 155**: Possibly move to StoryElement and make bi-directional

## Models

### StoryModel.cs
- **Line 10**: Move StoryModel to ViewModels namespace in a future refactoring
- **Line 14**: Note sorting filtering and grouping depend on ICollectionView (for TreeView?)
- **Line 15**: See http://msdn.microsoft.com/en-us/library/ms752347.aspx#binding_to_collections
- **Line 16**: Maybe http://stackoverflow.com/questions/15593166/binding-treeview-with-a-observablecollection

### StoryElementCollection.cs
- **Line 48**: Assert that NewItems count is always 1, or make this a loop
- **Line 73**: Assert that OldItems count is always 1, or make this a loop
- **Line 74**: Maybe replace the index of with just remove

### StoryElement.cs
- **Line 131**: Examine this more closely. Called from OutlineViewModel.DeleteNode() and other ViewModels

## Views

### WorkflowShell.xaml.cs
- **Line 27**: this [incomplete comment]

## Tests

### OutlineViewModelTests.cs
- **Line 62**: Re-enable after architecture fix in issue #1068
- **Line 65**: This test is currently disabled because it requires manipulating the Headless state

### SceneViewModelTests.cs
- **Line 217**: Priority 2 - Additional Cast Management Tests
- **Line 263**: Priority 3 - Name Property Tests
- **Line 267**: Priority 3 - ViewpointCharacter Property Tests
- **Line 273**: Priority 3 - Setting Property Tests
- **Line 277**: Priority 3 - Protagonist/Antagonist Property Tests
- **Line 285**: Priority 4 - Property Change Notification Tests
- **Line 313**: Priority 5 - Scene Purposes Tests
- **Line 323**: Priority 6 - Constructor Tests

## Summary

### By Category:
- **Architecture Issues**: 9 items (circular dependencies, layer violations)
- **Data Access Layer**: 2 items
- **Services**: 13 items
- **ViewModels**: 8 items
- **Models**: 7 items
- **Views**: 1 item
- **Tests**: 10 items (mostly stubbed test implementations)

### Total: 50 TODO items

### Priority Recommendations:
1. **High Priority**: Architecture issues (circular dependencies, layer violations)
2. **Medium Priority**: Exception handling, logging improvements, user-friendliness
3. **Low Priority**: Code organization refactoring, test completeness
4. **Deferred**: Collections refactoring research, binding improvements
