# Issue 1427 Status Log

## 2026-06-28

### Branch
`issue-1427-feedback-validation` (based on `dev`)

### Commit
`0354bfa6` — feat(#1427): enforce min char limits & attach session log to feedback submissions

### What's done
- `FeedbackViewModel.cs`: added `IsValid`, `TitleError`, `BodyError`, `TitleErrorVisibility`, `BodyErrorVisibility`; Title >= 10 chars, Body >= 20 chars required; last 250 lines of today's log appended to GitHub issue body in a `<details>` block
- `FeedbackDialog.xaml`: inline red TextBlocks show errors while editing; Submit button disabled until valid
- `ShellViewModel.cs`: `OpenReportFeedback()` sets `IsPrimaryButtonEnabled` from `IsValid` and tracks it via `PropertyChanged`
- `FeedbackViewModelTests.cs`: 13 tests, all passing
- `docs/Miscellaneous/Leaving_Feedback.md`: updated to document minimum length requirements

### Blocked
Manual testing revealed the Report Feedback button is grayed out. Confirmed pre-existing on `dev` — not caused by our changes. Root cause not yet identified. `ReportFeedbackCommand` is gated by `SerializationLock.CanExecuteCommands`; `IsIdle` appears to be false when it should not be. Context corruption interrupted investigation. Need to run `/doctor` and resume debugging with an unbiased agent prompt.

### Not started
- Integration & Manual Testing section of lifecycle
- PR
