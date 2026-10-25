# Issue 1660 status log

Collaborator crash (0xC000027B) when a workflow is invoked while the overwrite confirm dialog is open.

## 2026-10-25

**Branch:** `issue-1660-dialog-nav-crash` (worktree `StoryCAD-1660`), from `dev` at `efaec86a`.

**Cause:** Review Each, Accept on a "Has your text" row, Skip on the last row opens the "Replace existing content?" ContentDialog. A nav item invoke delivered through UI Automation reaches the NavigationView under the overlay; the workflow opens its element picker, a second ContentDialog on the same XamlRoot, WinUI throws, and the exception escapes the `async void` ItemInvoked handler.

**Fix:** `WorkflowShellViewModel.IsDialogOpen` blocks a workflow invoke while Collaborator shows a dialog (set around the overwrite confirm and the element picker in `Collaborator.cs`). `RunInvokedWorkflowAsync` runs the workflow inside try/catch, logs the exception, and puts it on the shell status line.

**Tests:** three new tests in `WorkflowShellViewModelTests`; the class passes (38 passed, 7 skipped). Live check with the automation runner: Ideation, Review Each, Accept, Skip, Problem Builder click under the dialog, cancel, Problem Builder again opens the picker. Passed on this branch; the same script crashed the app on `dev`.

**Next:** PR to `dev`; after merge, remove the close-and-reopen workaround from `Meet-Collaborator-Video.scs` on `issue-1626-youtube-upload`.
