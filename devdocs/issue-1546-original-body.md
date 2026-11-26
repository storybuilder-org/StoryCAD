## Document kind

| Question | Answer |
|----------|--------|
| What language? | ASD-STE100 for this design. |
| What is this? | One issue for the StoryCAD side of beat-sheet structure: service rules, model, API, and a Plot Board window. |
| Delivery | One issue. Three pull requests, in the order in section 8. |

## 1. Problem statement

The beat tree is the narrative structure of an outline. The Story Problem holds a beat sheet. Its beats hold Problems and Scenes. Each Problem on a beat holds its own sheet, down to Scenes. The explorer tree is filing only. Under the folders template every Scene sits in a Scenes folder, so tree position says nothing about structure.

StoryCAD does not state this rule and does not protect it:

1. `AssignElementToBeat` accepts a Problem on a beat of its own descendant. That makes a cycle. A walk of the beat tree does not terminate.
2. A sheet replacement deletes every beat. The elements that were on the sheet lose their place. StoryCAD keeps no record of them. Three code paths replace a sheet and they do not agree. The Structure tab deletes beats one by one through `OutlineService.DeleteBeat`, then adds the template rows. `StoryCADApi.ApplyBeatSheetToProblem` and `StoryCADApi.LoadBeatSheet` call `StructureBeats.Clear()` directly, which skips `DeleteBeat`, so a Problem bound on the old sheet keeps a `BoundStructure` that still points at the parent. `OutlineService.SetBeatSheet` exists for this job and has no production caller.
3. The Structure tab shows one Problem's sheet. No view shows the whole tree, or which beat of a parent holds a subproblem.
4. The public API and the MCP tools expose the same twelve beat operations. Every rule must hold there too.

storybuilder-org/Collaborator#217 (ProblemBuilder scene assignment) is a consumer of these rules. storybuilder-org/StoryCADAPI#40 shows the failure class for model changes: a field added to the model did not reach `get_element`.

## 2. Terms

| Term | Meaning |
|------|---------|
| Sheet | A Problem's `StructureBeats` list, with `StructureTitle` and `StructureDescription`. |
| Beat | One row of a sheet. Holds a title, a description, and an element GUID or `Guid.Empty`. |
| Beat tree | The Story Problem's sheet and, through the Problems bound on beats, every sheet below it. |
| Sheet parent | The one Problem whose sheet holds a given Problem. Stored in `ProblemModel.BoundStructure`. |
| Placed | Bound to a beat on a given Problem's sheet. Placement is per sheet. |
| Free element | A Scene or Problem that is on no sheet. |
| Unplaced element | An element that was on a Problem's sheet before that sheet was replaced, and is not on the new sheet. Stored on the Problem. |
| Strip | One Problem's sheet drawn as a row of beat cards on the Plot Board. |
| Palette | The Plot Board list of elements that can be dropped on a beat. |

## 3. Rules

1. The beat tree is the structure. Explorer tree position has no structural meaning. No code derives membership from the explorer tree.
2. A Scene may be on any number of sheets. This is by design. One Problem's resolution beat can be another Problem's inciting beat (The Old Man and the Sea). Two Problems can share a climax scene (Star Trek VI). StoryCAD does not add a guard.
3. A Scene appears at most once on one sheet. A sheet is a linear order.
4. Problems nest. Any Problem's beats may hold Scenes and Problems together. A Problem has one sheet parent. The Story Problem is never bound to a beat.
5. The beat tree has no cycles. A Problem cannot be bound on a beat of itself or of any Problem below it.
6. When a sheet is replaced, the elements that were on it become unplaced elements of that Problem. They stay unplaced until they are bound on that Problem's new sheet or the user removes them from the list.

## 4. Scope

| In | Out |
|----|-----|
| `OutlineService` rules 5 and 6 | Collaborator changes. storybuilder-org/Collaborator#217 covers ProblemBuilder now. A small Collaborator follow-on reads the unplaced list and shows proposals on the board. |
| `ProblemModel.UnplacedElements` and `.stbx` persistence | Timeline (#747). The board is related, not the same feature. |
| `IStoryCADAPI` and `StoryCADApi` changes, and their companions in StoryCADAPI | Master Plots tool. |
| Plot Board window | Automated UI tests. Tests cover the service, the model, the API, and the board ViewModel. |
| Tests for every rule | |

## 5. Design: service and model

### 5.1 Cycle guard (rule 5)

`OutlineService.AssignElementToBeat`, Problem branch:

1. Start at `Parent`.
2. Follow `BoundStructure` upward until `Guid.Empty`.
3. If the walk reaches `DesiredBind`, throw `InvalidOperationException` with the message `Cannot assign a Problem to a beat of its own descendant.`
4. The self-assignment check and the Story Problem check stay as they are.

Add one comment at the Scene branch: no one-sheet check for Scenes is deliberate (rule 2).

### 5.2 Unplaced elements (rule 6)

`ProblemModel` gets `List<Guid> UnplacedElements`. It serializes into the `.stbx` file. A file without the field loads with an empty list. No migration.

Rule 6 lives in one place, `OutlineService.SetBeatSheet`:

1. Collect every non-empty beat GUID from the current sheet.
2. Add each GUID to `Parent.UnplacedElements` if it is not already there.
3. Delete the beats through `DeleteBeat`, so a bound Problem's `BoundStructure` is reset.
4. Install the new title, description, and beats.

Route the three replacement paths through it:

| Path | Today | After |
|------|-------|-------|
| `BeatSheetsViewModel.ConfirmAndApplyBeatSheet` | `DeleteBeat` loop, then adds rows itself | Calls `SetBeatSheet` with the template rows |
| `StoryCADApi.ApplyBeatSheetToProblem` | `StructureBeats.Clear()` and adds rows | Calls `SetBeatSheet` |
| `StoryCADApi.LoadBeatSheet` | `StructureBeats.Clear()` and adds rows | Calls `SetBeatSheet` |

`OutlineService.AssignElementToBeat`: after the bind, remove `DesiredBind` from `Parent.UnplacedElements`.

New `OutlineService.RemoveUnplacedElement(Parent, elementGuid)`: removes one GUID from the list.

Readers skip a GUID that does not resolve or resolves to a trashed element. The list is not cleaned on trash or delete.

## 6. Design: API and companions

### 6.1 `IStoryCADAPI` and `StoryCADApi`

| Member | Change |
|--------|--------|
| `AssignElementToBeat` | No signature change. The cycle guard surfaces as an `OperationResult` failure with the message from 5.1. |
| `GetUnplacedElements(Guid problemGuid)` | New. Returns `OperationResult<IEnumerable<(Guid Guid, string Name, string Type)>>`. Skips unresolved and trashed GUIDs. |
| `RemoveUnplacedElement(Guid problemGuid, Guid elementGuid)` | New. Returns `OperationResult<bool>`. |
| `ApplyBeatSheetToProblem`, `LoadBeatSheet` | No signature change. Doc comment states rule 6. |
| `GetProblemStructure` | No change. The return tuple is public; unplaced elements have their own method. |
| `GetStoryElement`, `get_element` payload | `UnplacedElements` appears on a Problem. A test asserts it, so the StoryCADAPI#40 failure class does not repeat. |

### 6.2 StoryCADAPI repo

1. `BeatTools.cs`: `assign_beat` description names the cycle failure. New tools `get_unplaced_elements` and `remove_unplaced_element`. `apply_beat_sheet` and `load_beat_sheet` descriptions state rule 6.
2. Regenerate the API docs at api.storybuilder.org.
3. Samples that apply a sheet show the unplaced list.
4. Python bindings gain `get_unplaced_elements` and `remove_unplaced_element`.
5. StoryCADLib NuGet: minor version bump. StoryCADAPI consumes the new version.

## 7. Design: Plot Board window

### 7.1 Window

1. Opens from the Shell (menu and toolbar) on a `new Window`, the way the Collaborator window opens.
2. Dialogs raised from the board use the board's `XamlRoot`, not the Shell's.
3. The board has its own `KeyDown` handler for its shortcuts.
4. The board closes at the start of file open, new, and close, and when the main window closes.

### 7.2 Layout

1. One strip per Problem in the beat tree. The Story Problem strip is at the bottom. A child Problem's strip sits above its parent, with a connector from the parent beat that holds it to the child strip's first card. Sketch: `Beatsheet Plot Board.jpeg` (2026-09-03), to be attached to this issue.
2. A strip is a row of beat cards. A card shows the beat title, the bound element name or `Unassigned`, and the element icon. It reuses the Structure tab's beat item template.
3. Strips do not share a column grid. Sheets differ in length. The connector carries the position.
4. A Problem with a sheet and no sheet parent is a root strip in an `Unattached` group.
5. The palette on the right lists free elements, grouped by type. When a strip is selected, that Problem's unplaced elements show first.

### 7.3 Operations

All operations call `OutlineService`. Each mutation sends `IsChangedMessage`.

| Action | Service call |
|--------|--------------|
| Assign the selected palette item to the selected empty beat | `AssignElementToBeat` |
| Unbind the selected beat | `UnasignBeat` |
| Move the selected beat up or down | `MoveBeat` |
| Create or delete a beat | `CreateBeat`, `DeleteBeat` |
| Change the sheet, after a confirm dialog | `SetBeatSheet` |
| Remove an item from the unplaced list | `RemoveUnplacedElement` |

The buttons are the keyboard path and stay after drag and drop lands.

### 7.4 Refresh between windows

New message `BeatsChangedMessage(Guid problemGuid)`.

1. The board sends it after each mutation. The Problem page's `BeatSheetsViewModel` reloads that Problem's sheet when it receives it.
2. The Structure tab sends it after each of its own mutations. The board reloads that strip when it receives it.

Without this, a Problem page open in the main window writes its stale copy back on the next navigation and reverts the board's edits.

### 7.5 Drag and drop

Second increment, after 7.3 works.

| Drag | Result |
|------|--------|
| Scene from the palette to an empty beat | Placed. The Scene stays in the palette with a placement count. |
| Scene from one strip to another strip | Placed on the second strip. Not moved. |
| Scene within a strip | Moved. |
| Problem from the palette or a strip to an empty beat | Moved. A drop on a beat of its own descendant is rejected with the 5.1 message. |
| Any card to the palette | Unbound. |
| Any card to a filled beat | Rejected. Unbind first. |

Cross-window drag is not supported. Palette-to-strip drag on the desktop head is not yet exercised anywhere in StoryCAD, so 7.5 begins with a build check on macOS.

### 7.6 Accessibility

Every interactive control gets an `AutomationId` per `devdocs/automation_naming_convention.md`. The buttons in 7.3 are the keyboard path.

## 8. Delivery order

| PR | Content | Size |
|----|---------|------|
| 1 | 5.1 cycle guard, the Scene-branch comment, `assign_beat` description, tests | Small. Additive. Eligible for Release 4.3. |
| 2 | 5.2 including the three-path routing, 6.1, 6.2, `.stbx` tests, NuGet and docs release | Medium. Coordinated StoryCAD and StoryCADAPI release. |
| 3 | 7.1 to 7.4, then 7.5, then 7.6 | Large. Depends on PR 2. |

Release split (Terry, 2026-11-02): PRs 1 and 2 are Release 4.4. PR 3 (the Plot Board window) is Release 4.5. When PR 2 merges, move this issue to the Release 4.5 milestone and to Project 14 (Release 4.5).

## 9. Tests

| Test | Passes when |
|------|-------------|
| Cycle, depth 1 | A on B's beat, then B on A's beat: the second bind fails with the 5.1 message. |
| Cycle, depth 3 | A holds B, B holds C: binding A on C's beat fails. |
| Existing guards | Self-assignment and Story Problem binding still fail. |
| Scene on two sheets | Both binds succeed (rule 2). |
| Sheet replace | After `SetBeatSheet`, every element that was bound is in `UnplacedElements`, once. |
| Three paths agree | The Structure tab path, `ApplyBeatSheetToProblem`, and `LoadBeatSheet` each fill `UnplacedElements` and each reset `BoundStructure` on a Problem that was bound. The API cases fail today. |
| Bind clears unplaced | After `AssignElementToBeat` on that Problem, the GUID leaves the list. |
| `.stbx` round trip | A file with the field reloads it. A file without the field loads an empty list. |
| API failure | `StoryCADApi.AssignElementToBeat` returns a failure with the 5.1 message on a cycle. |
| `get_element` | A Problem payload carries `UnplacedElements`. |
| MCP tools | `get_unplaced_elements` and `remove_unplaced_element` round trip in StoryCADAPI's test project. |
| Refresh | `BeatsChangedMessage` causes `BeatSheetsViewModel` to reload the named Problem. |
| Board ViewModel | Strip order, connectors, palette contents, and each 7.3 operation, headless. |

## 10. Related

- storybuilder-org/Collaborator#217: ProblemBuilder scene assignment. Consumer of rules 1 to 5. Not blocked by this issue.
- storybuilder-org/StoryCADAPI#40: `get_element` omits Images. Same failure class as 6.1's last row.
- #747: Timeline. The Story Problem strip is most of a timeline.

### Design tasks / sign-off
- [x] Plan this section
- [x] Human approves plan
- [ ] Human final approval

### Code tasks / sign-off
- [ ] Plan this section
- [ ] Human approves plan
- [ ] Human final approval

### Test tasks / sign-off
- [ ] Plan this section
- [ ] Human approves plan
- [ ] Human final approval

### Evaluate tasks / sign-off
- [ ] Plan this section
- [ ] Human approves plan
- [ ] Wiki pages updated + `log.md` merge entry (or postponement recorded in `log.md`)
- [ ] Repo `devdocs/*_status_log.md` updated
- [ ] Human final approval


