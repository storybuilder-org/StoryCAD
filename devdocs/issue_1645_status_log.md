# Issue #1645 status log

## 2026-11-11

**Branch:** `issue-1645-flush-on-deactivate`, from `dev` at `03f7c193`.

**Done:** `EditFlushService.OnMainWindowActivationChanged` copies the open page's edits into the model when the main window goes inactive; `App.xaml.cs` connects it to `MainWindow.Activated`. The method takes the state as an enum because the type differs by head (`Microsoft.UI.Xaml.WindowActivationState` on Windows, `Windows.UI.Core.CoreWindowActivationState` on the desktop head).

**Tests:** two new tests in `EditFlushServiceTests`, each seen failing first. Full StoryCADTests: 2311 tests, 2294 passed, 17 skipped, 0 failed. Both heads build.

**Next:** manual test on Windows and on a Mac (issue steps 1 to 3: Story Form reads the new Genre).
