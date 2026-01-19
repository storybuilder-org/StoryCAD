# Issue #782: StoryWorld Implementation Plan

## Completed Work
- [x] StoryWorldModel (with entry classes in Models/StoryWorld/)
- [x] StoryWorldViewModel (INavigable, ISaveable, IReloadable)
- [x] StoryWorldPage.xaml and code-behind
- [x] StoryItemType.StoryWorld enum value
- [x] DI registration in ServiceLocator
- [x] Lists in Lists.json (WorldType, Ontology, etc.)
- [x] Unit tests (Model and ViewModel)

## Remaining Work

### Navigation
- [ ] Add `private const string StoryWorldPage = "StoryWorldPage"` in ShellViewModel.cs
- [ ] Add `nav.Configure(StoryWorldPage, typeof(StoryWorldPage))` in App.xaml.cs
- [ ] Add StoryWorld case in TreeViewNodeClicked switch (ShellViewModel.cs)

### Add StoryWorld Command
- [ ] Add `AddStoryWorldCommand` property in ShellViewModel.cs
- [ ] Initialize command with singleton check (only one StoryWorld per story)
- [ ] Add `CanAddStoryWorld()` method - returns false if StoryWorld already exists
- [ ] Add `NotifyCanExecuteChanged()` call where other commands are notified

### OutlineService
- [ ] Add StoryWorld case in AddStoryElement switch statement
- [ ] Add singleton validation before creating StoryWorld

### Menu and Flyout UI (Shell.xaml)
- [ ] Add StoryWorld button to Menu Bar flyout
- [ ] Add StoryWorld button to right-click flyout
- [ ] Determine icon (Globe is used by Settings - need different icon)
- [ ] Add keyboard shortcut (Alt+W / ⌥W)

### Delete Handling
- [ ] Add confirmation dialog when deleting StoryWorld ("Are you sure?")
- [ ] StoryWorld CAN be deleted (unlike Overview)

### Reports
- [ ] Add StoryWorld to PrintReports.cs
- [ ] Add StoryWorld to ScrivenerReports.cs (confirmed needed)
- [ ] Add StoryWorld formatting in ReportFormatter.cs

### Tests
- [ ] Test adding StoryWorld via command
- [ ] Test singleton constraint (command disabled when StoryWorld exists)
- [ ] Test navigation to StoryWorldPage
- [ ] Test delete with confirmation
- [ ] Test OutlineService.AddStoryElement for StoryWorld

## Decisions
- StoryWorld report contains all tabs (Structure, History, Economy, Magic/Tech, list tabs)
- Scrivener export: Yes
- Icon: `World` (different from Globe which is used by Settings)
- User-added (not auto-created) - existing users have outlines without it
- Consider adding to some templates later (user choice)
