# Issue #1551 Status Log

ComboBox-bound fields copy the previous element's value onto an element whose field is empty.

## 2026-09-04 (session recovery block)

| Item | State |
|------|-------|
| Issue | #1551 OPEN, milestone Release 4.3. Design plan approved. Code plan re-approved in session after the click test. |
| Branch | `issue-1551-combobox-empty-carryover` off `dev` `20d24907`. PR #1552 open against `dev`. |
| Cause (proven by trace) | Setting `Text` on an editable WinUI ComboBox never moves its selection. When `LoadModel` pushes an empty value, the control restores its selected item's text through the two-way `Text` binding, and `SaveModel` stores it. #1267 fixed the non-editable boxes, which bind `SelectedItem`; the editable boxes bind `Text` and were not covered. |
| Fix | A one-way `SelectedItem` binding to the same ViewModel property on every editable ComboBox: Problem 12, Character 23, Scene 9, Setting 2. XAML only, no C# change. Overview and StoryWorld have no editable boxes. |
| Tried and reverted | Loading an empty value as the blank row in `LoadModel` (the write-back stayed). Removing `NavigationCacheMode="Required"` from every page (the write-back stayed, and every page instance stayed alive and bound to the singleton ViewModel). |
| Verified | Problem Category by trace at 12:19: a category picked on one Problem, the empty Problem loaded with no write-back, its `SaveModel` stored empty, the first Problem kept its value. |
| Open | Click test on one editable box each on Character, Scene and Setting. Human final approval on Code and Test. |
| Tests | No new unit tests. The change is XAML and the control is not unit-tested. The 17 blank-row tests were reverted with that code. |
