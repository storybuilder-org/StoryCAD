# Issue #1645 status log

## 2026-11-11

**Branch:** `issue-1645-flush-on-deactivate`, from `dev` at `03f7c193`.

**Done:** `EditFlushService.OnMainWindowActivationChanged` copies the open page's edits into the model when the main window goes inactive; `App.xaml.cs` connects it to `MainWindow.Activated`. The method takes the state as an enum because the type differs by head (`Microsoft.UI.Xaml.WindowActivationState` on Windows, `Windows.UI.Core.CoreWindowActivationState` on the desktop head).

**Tests:** three new tests in `EditFlushServiceTests`, each seen failing first. Full StoryCADTests: 2312 tests, 2295 passed, 17 skipped, 0 failed. Both heads build.

**Review (separate agent):** one finding fixed: the copy is skipped while `SerializationLock` is held, so it cannot change the model while AutoSave or Backup writes it (third test). Two findings checked and not changed: Collaborator Accept already reloads the open page from the model (`WorkflowViewModelWiring.AfterWrite`), so no stale page overwrites an accepted value; a page left registered after navigation is already flushed by AutoSave today.

**Next:** manual test on Windows and on a Mac (issue steps 1 to 3: Story Form reads the new Genre).
