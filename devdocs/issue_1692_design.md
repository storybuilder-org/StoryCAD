# Issue #1692: A preference that hides Collaborator

**Status:** draft for approval by Terry  
**Date:** 2026-11-06  
**Issue:** [StoryCAD #1692](https://github.com/storybuilder-org/StoryCAD/issues/1692)  
**Branch:** `issue-1692-hide-collaborator`  
**Code home:** `StoryCADLib`, `StoryCAD`  
**No ADR.** The change is local to one preference and the controls that it hides. To reverse it, remove one preference. No other repository changes.

---

## 1. Document kind

| Question | Answer |
|----------|--------|
| What is this file? | The design and the implementation recipe for #1692. |
| What language? | ASD-STE100 for this design. |
| What does this issue change? | One new preference. When the writer clears it, StoryCAD hides each Collaborator entry point. |
| What does this issue not change? | The documentation about Collaborator: the startup help dialog, its Meet Collaborator video link, and the user manual. The Collaborator code in the build. The store activation at startup. A Collaborator window that is open. |

---

## 2. Problem

Some markets do not accept work from a writer who uses AI.  
A writer for these markets sees Collaborator in StoryCAD.  
StoryCAD shows the Collaborator button and the Buy Credits button on the command bar.  
On macOS, StoryCAD also shows a Collaborator item in the Tools menu.  
StoryCAD has no preference that hides these items.  
On 2026-11-05, support told a writer that this preference will come.

---

## 3. Goal

The writer clears one check box in Preferences.  
StoryCAD then hides each Collaborator entry point.  
The change occurs when the writer clicks Save. The writer does not restart StoryCAD.  
The writer can set the check box again. StoryCAD then shows the entry points again.

With the check box clear, the writer uses StoryCAD as before StoryCAD had Collaborator.  
The preference hides access to Collaborator. It does not hide the fact that Collaborator exists.  
StoryCAD keeps all documentation about Collaborator. This includes the Meet Collaborator video link in the startup help dialog and the user manual.

---

## 4. Terms

| Term | Meaning |
|------|---------|
| **Show Collaborator** | The new preference. A check box on the Other tab of the Preferences dialog. The default value is set (true). |
| **Collaborator entry point** | A control that opens Collaborator, or that starts a purchase for Collaborator. Section 5 gives the list. A link to documentation is not an entry point. |
| **Collaborator build** | A build of StoryCAD that contains CollaboratorLib. In this build, `CollaboratorService.HasCollaborator` is true. |
| **Entry point rule** | The rule that decides if StoryCAD shows the entry points. Section 6 gives the rule. |
| **Startup help dialog** | The dialog with the title "Need help getting started?". `HelpPage` is its content. This issue does not change it. |
| **TDD** | Test-driven development. Write a test that fails, then write the code that makes the test pass. |

---

## 5. Collaborator entry points

| Entry point | Location | Control |
|-------------|----------|---------|
| Collaborator button | `StoryCAD/Views/Shell.xaml` | `CollaboratorButton` |
| Buy Credits button | `StoryCAD/Views/Shell.xaml` | `BuyCreditsButton` |
| Collaborator menu item and its separator (macOS) | `StoryCADLib/Services/MacMenuBar/MacMenuBarService.cs` | `_collaboratorItem`, `_collaboratorSeparator` |

The two buttons and the macOS menu item use one property: `ShellViewModel.CollaboratorVisibility`.  
The macOS menu bar monitors this property. It does not need a change.

---

## 6. Entry point rule

StoryCAD shows the entry points only when both of these conditions are true:

1. The build is a Collaborator build.
2. The Show Collaborator preference is set.

If one condition is false, StoryCAD hides each entry point.

`CollaboratorService` holds the rule in one property, `ShowEntryPoints`.  
Each entry point gets its visibility from this property.  
A new entry point must also use this property. A code comment on the property gives this instruction.

---

## 7. Behavior

1. At startup, `Shell_Loaded` calls `ShellViewModel.UpdateCollaboratorVisibility`.
2. `UpdateCollaboratorVisibility` reads `CollaboratorService.ShowEntryPoints`.
3. `UpdateCollaboratorVisibility` sets `CollaboratorVisibility` to `Visible` or `Collapsed`.
4. The command bar buttons and the macOS menu item change to agree with the property.
5. The writer opens Preferences and clears Show Collaborator.
6. The writer clicks Save.
7. `OpenPreferences` saves the preferences, then calls `UpdateCollaboratorVisibility`.
8. StoryCAD hides the entry points immediately.

If the writer clicks Cancel in Preferences, StoryCAD does not change the entry points.

---

## 8. Changes by file

| File | Change |
|------|--------|
| `StoryCADLib/Models/Tools/PreferencesModel.cs` | Add `ShowCollaborator` (bool, JSON name `ShowCollaborator`). Set it to true in the constructor. |
| `StoryCADLib/ViewModels/Tools/PreferencesViewModel.cs` | Add `ShowCollaborator`. Copy it in `LoadModel` and `SaveModel`. |
| `StoryCADLib/Services/Dialogs/Tools/PreferencesDialog.xaml` | Add a check box "Show Collaborator" on the Other tab, row 2, column 0. Bind it two-way to `PreferencesVm.ShowCollaborator`. |
| `StoryCADLib/Services/Collaborator/CollaboratorService.cs` | Add `ShowEntryPoints`: `HasCollaborator && _preferenceService.Model.ShowCollaborator`. |
| `StoryCADLib/ViewModels/ShellViewModel.cs` | Add `UpdateCollaboratorVisibility`. Call it in `OpenPreferences` after `SaveAsync`. |
| `StoryCAD/Views/Shell.xaml.cs` | In `Shell_Loaded`, replace the `HasCollaborator` test with a call to `UpdateCollaboratorVisibility`. |

The check box shows in all builds.  
In a build without CollaboratorLib, the check box has no effect, because rule condition 1 is false.

---

## 9. Names

### 9.1 Code

The names obey `.claude/docs/architecture/coding-standards.md`:

| Kind | Rule | Names in this issue |
|------|------|---------------------|
| Property | PascalCase noun or noun phrase | `ShowCollaborator`, `ShowEntryPoints` |
| Method | PascalCase verb phrase | `UpdateCollaboratorVisibility` |
| Private field | `_camelCase` | `_showCollaborator` |
| JSON property | Same as the C# property | `ShowCollaborator` |

### 9.2 AutomationId

The new check box gets the AutomationId `PreferencesShowCollaboratorCheck`.  
This name obeys `devdocs/automation_naming_convention.md`: PascalCase, the dialog prefix `Preferences`, the function, and the role suffix `Check`.  
`AutomationConventionTests` checks this name.

### 9.3 Tests

| Kind | Rule |
|------|------|
| Test file | `[SourceFileName]Tests.cs` in `StoryCADTests`, in the folder that agrees with the source folder. Exception: the `PreferencesModel` tests go in `DAL/PreferencesIOTests.cs`, with the current `CollaboratorTerseness` tests of the same kind. |
| Test method | `MethodName_Scenario_ExpectedResult` |
| Test body | Arrange, Act, Assert |

---

## 10. Tests

Use TDD. Write each test before its code. Run each test and see it fail. Then write the code.

Use real services from `Ioc.Default`. Do not use mocks (StoryCAD ADR-008).  
A test that changes `PreferenceService.Model.ShowCollaborator` must set the value back in `[TestCleanup]`.

| Test file | Test method |
|-----------|-------------|
| `DAL/PreferencesIOTests.cs` | `PreferencesModel_WhenJsonLacksShowCollaborator_DefaultsToTrue` |
| `DAL/PreferencesIOTests.cs` | `WritePreferences_RoundTripsShowCollaborator` |
| `ViewModels/PreferencesViewModelTests.cs` | `LoadModel_ShowCollaboratorFalse_CopiesShowCollaborator` |
| `ViewModels/PreferencesViewModelTests.cs` | `SaveModel_ShowCollaboratorCleared_WritesShowCollaboratorToModel` |
| `Services/Collaborator/CollaboratorServiceTests.cs` | `ShowEntryPoints_WithFactoryAndPreferenceSet_ReturnsTrue` |
| `Services/Collaborator/CollaboratorServiceTests.cs` | `ShowEntryPoints_WithFactoryAndPreferenceCleared_ReturnsFalse` |
| `Services/Collaborator/CollaboratorServiceTests.cs` | `ShowEntryPoints_NoFactoryAndPreferenceSet_ReturnsFalse` |
| `ViewModels/ShellViewModelTests.cs` | `UpdateCollaboratorVisibility_PreferenceCleared_SetsCollapsed` |

The test project registers no Collaborator factory. Thus the `ShellViewModel` test can examine only the `Collapsed` result.  
The `CollaboratorService` tests examine the full rule, because they give the service a factory.

The current tests must continue to pass:

- `AutomationConventionTests` (the new check box).
- The `Smoke_Test.scs`, `Collaborator.scs` and `Meet-Collaborator-Video.scs` scripts. The default value of Show Collaborator is true, so these scripts see no change.

Run the tests with `vstest.console.exe`.

---

## 11. Manual test plan

Add test case PF-009 to `StoryCADTests/ManualTests/Preferences_Test_Plan.md`.  
Change the result line from 8 to 9.

PF-009 steps:

1. Open Preferences. Select the Other tab. Make sure that Show Collaborator is set.
2. Clear Show Collaborator. Click Save.
3. Make sure that the Collaborator button and the Buy Credits button are not on the command bar.
4. On macOS, make sure that the Tools menu has no Collaborator item.
5. Set Show startup page. Restart StoryCAD.
6. Make sure that the startup help dialog still has the Meet Collaborator (Video) link.
7. Make sure that the Collaborator button is not on the command bar after the restart.
8. Open Preferences. Set Show Collaborator. Click Save.
9. Make sure that the Collaborator button and the Buy Credits button are on the command bar again.
10. Clear Show Collaborator. Click Cancel. Make sure that the buttons stay on the command bar.

---

## 12. User manual

The manual uses its own voice (usermanual ADR-001), not ASD-STE100.

| Page | Change |
|------|--------|
| `docs/Preferences/Preferences.md` | Add Show Collaborator to the Other tab text. Make the Other tab text agree with the tab. The current text names items that are on other tabs (Send error logs, Send newsletters, Automatically Save). |
| `docs/media/Preferences-Other-tab.png` | Replace with a new image of the Other tab. Terry must approve a run of StoryCAD on the screen, or supply the image. |
| `docs/StoryCAD Collaborator/Opening_Collaborator.md` | Add a short paragraph: the writer can hide the Collaborator buttons with Show Collaborator on the Other tab of Preferences. Do not remove or change other text. |

These changes add text about the new preference. They do not remove documentation about Collaborator.

---

## 13. Failure modes and edge cases

| Case | Behavior |
|------|----------|
| A preferences file from an earlier version does not have `ShowCollaborator`. | The constructor value (true) stays. StoryCAD shows Collaborator as before. |
| The writer clears Show Collaborator while the Collaborator window is open. | The window stays open. The writer closes it as usual. The buttons are hidden. |
| A build without CollaboratorLib. | The entry points stay hidden. The check box has no effect. |
| Store activation at startup (`App.xaml.cs`). | No change. It is a license check, not an entry point. |

---

## 14. Work to come

| Item | Instruction |
|------|-------------|
| #1656 (in-app advertising for Collaborator) | Each advertisement uses `ShowEntryPoints`. Add a comment to #1656 with this instruction. |

---

## 15. Design review

Result of `wiki/checklists/design/design-review.md`:

| Item | Result |
|------|--------|
| 0. Language | This file is ASD-STE100. Section 12 is the exception, because the manual has its own standard. |
| 1. Patterns | MVVM. The rule is in a service; the view models read it. No persistence change other than one preference. |
| 2. ADRs | ADR-008 (no mocks): the tests use real services. ADR-010: the PR closes the issue. No conflict. |
| 3. ADR needed | No. See the header. |
| 4. Failure modes | Section 13. |
| 5. Observability | No new log lines. The preference is in the preferences file. |
| 6. Security | No new input, network call or secret. |
| 7. Dependencies | None. |
| 8. Cross-platform | The macOS menu item uses the same property. PF-009 step 4 examines it. |
| 9. Fitness function | The `CollaboratorService` tests fail if the rule ignores the preference. `AutomationConventionTests` checks the new check box. |
| 10. API surface | No change to `IStoryCADAPI` or `StoryCADApi`. |
| 11. Wrong or trade-off | Trade-off: the check box shows in builds without CollaboratorLib. This is simpler than a second visibility rule. |
| 12. Work to come | Section 14. |
