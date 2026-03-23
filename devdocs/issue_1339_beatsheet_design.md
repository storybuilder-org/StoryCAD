# Issue #1339: Improve Structure Tab Beat Sheet Usability — Design Document

Detailed design document for the beat sheet refactoring. The issue (#1339) covers requirements,
phases, and risks. This document covers the code-level details needed to implement the changes.

## UX Review

UX review completed via `ui-designer` agent with corrected workflow context (two rounds). Screenshots reviewed: `beatsheet_description.png`, `beatsheet_4.png`, `beatsheet_6.png` (in `/mnt/c/temp/`).

### Workflow Understanding

The bottom description panels are **essential to the workflow**, not secondary reference:
1. Writer selects a beat in the left list → Beat Description populates at bottom-left
2. Writer browses scenes/problems in the right list → selects one → Element Description populates at bottom-right
3. Writer **compares** the beat description (what this beat needs) with the element description (what the scene/problem contains) to decide if the assignment makes sense
4. Writer clicks `<` (assign) to link the element to the beat

**Beat Description has dual purpose:**
- Template beat sheets: read-only guidance explaining the structural beat (e.g., "The catalyst is the event that sets the story in motion")
- Custom/modified beat sheets: editable — the writer defines what each beat means
- After redesign: always editable (all beat sheets become modifiable starting points)

### Current Layout Problems

1. **Beat Sheet Description (Expander content)** renders in a narrow column, causing excessive wrapping and taking ~40% of vertical space when expanded — squeezes beats list to 2 visible items
2. **Buttons are 32x32** with cryptic single characters (`<`, `>`, `+`, `-`, `∧`, `∨`, `S`) — unreadable, below 48px minimum touch target
3. **Assign button (`<`) has no click handler** — does nothing when clicked (bug)
4. **Add/delete/move/save buttons hidden** for non-custom beat sheets
5. **"(none)" in elements list** — looks like a selectable item named "none", not an empty state
6. **`Symbol.Cancel` (X) icon** for unassigned beats — looks like a delete/close button
7. **No visual distinction** between assigned and unassigned beats beyond the icon
8. **Description panels get squeezed** — current `2*`/`*` row split gives descriptions only ~150px (~4-5 lines), insufficient for their role as the decision-making context
9. **Scene/Problem radio** sits in Expander header — this is the correct location (stays fixed, doesn't scroll away with long element lists)
10. **No tooltips** on any button

### Proposed Layout

#### Overall Structure

```
Row 0  (Auto)    Expander: beat sheet selector + description (keep existing Expander, fix width)
Row 1  (3*)      Lists area: Beats list | buttons | Elements list
Row 2  (Auto)    Horizontal divider (1px)
Row 3  (2*)      Descriptions: Beat Description | Element Description
```

The `3*`/`2*` split (60/40) gives descriptions ~240px on a typical window vs the current ~150px. Both zones remain usable.

#### Row 0 — Expander (Keep, Fix Width)

Keep the existing Expander pattern — it works well because writers collapse it when not needed. Fix the narrow rendering by ensuring the description RichEditBox gets proper width when expanded (set `HorizontalAlignment="Stretch"` and remove any column constraints that squeeze it).

Keep the Scene/Problem toggle in the Expander header row (Row 0) where it currently is. Moving it into Row 1 would cause it to scroll off when the elements list is long. Its current position is fixed and accessible.

#### Row 1 — Lists and Buttons

Three columns:

```
Column 0 (5*)    Beats ListView (slightly wider — items are two-line)
Column 1 (Auto)  Button strip (48x48 icon buttons, two groups)
Column 2 (4*)    Elements ListView (with Scene/Problem toggle at top)
```

**Beats ListView:**

Item template:
```
[Status dot (12px)]  [Beat title (bold)]
                     [Assigned element name OR "Unassigned" in italic gray]
```

- **Status indicator**: Replace `Symbol.Cancel` with a 12px colored circle
  - Unassigned: hollow circle (1px border, no fill) — communicates "empty slot"
  - Assigned to Scene: filled circle in Scene accent color
  - Assigned to Problem: filled circle in Problem accent color
- **"No element Selected"** → **"Unassigned"** in `FontStyle="Italic"` with `TextFillColorTertiaryBrush`
- Optional: subtle background tint (accent color at 5-8% opacity) on assigned beats for at-a-glance progress scanning
- Give the ListView card-like styling: `BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"`, `BorderThickness="1"`, `CornerRadius="4"`

**Button Strip (Column 1):**

Two groups of 48x48 icon buttons, separated by 16px vertical gap. 8px margin on each side. Remove the vertical Rectangle separators — card-bordered lists provide sufficient separation.

Group 1 — Assignment:
| Button | Icon | Tooltip |
|--------|------|---------|
| Assign | `Symbol.Back` or FontIcon `\uE72B` (ChevronLeft) | "Assign selected element to selected beat" |
| Unassign | `Symbol.Forward` or FontIcon `\uE72A` (ChevronRight) | "Remove element assignment from selected beat" |

Group 2 — Beat Management:
| Button | Icon | Tooltip |
|--------|------|---------|
| Add Beat | `Symbol.Add` | "Add a new beat" |
| Delete Beat | `Symbol.Delete` | "Delete selected beat" |
| Move Up | FontIcon `\uE70E` (ChevronUp) | "Move beat up" |
| Move Down | FontIcon `\uE70D` (ChevronDown) | "Move beat down" |
| Save Sheet | `Symbol.Save` or FontIcon `\uE74E` | "Save beat sheet to file" |

Buttons are always enabled — each command method guards itself with early returns and status bar messages (e.g., Assign checks for selected beat and element, rejects self-assignment).

**Elements ListView (Column 2):**

- Scene/Problem toggle remains in Row 0 (Expander header) — stays fixed, doesn't scroll with the list
- When list is empty: centered overlay TextBlock — "No scenes in this outline" or "No problems in this outline" in italic secondary color. Remove the "(none)" fake list item.
- Same card-like border as beats list
- Keep both assignment paths: clicking an element in the list assigns it (fast path), and the Assign button also works (explicit path)

#### Row 2 — Horizontal Divider

`Rectangle` Height="1", `Fill="{ThemeResource DividerStrokeColorDefaultBrush}"`, `Margin="0,4"`.

#### Row 3 — Description Panels

Two equal columns, `ColumnSpacing="12"`. No vertical Rectangle separator — the gap is sufficient.

| Panel | Header | Editable | PlaceholderText |
|-------|--------|----------|-----------------|
| Beat Description | "Beat Description" | Yes (always, post-redesign) | "Select a beat to see its description" |
| Element Description | "Assigned Element Synopsis" | No (always read-only) | "Select an element to see its description" |

Both: `TextWrapping="Wrap"`, `ScrollViewer.VerticalScrollBarVisibility="Auto"`, `VerticalAlignment="Stretch"`, `MinHeight="100"`.

### Responsive Behavior

- **Wide mode**: The navigation tree can be toggled hidden via the hamburger button, giving the Structure tab full window width. Proportional column widths (`5*`/`Auto`/`4*`) scale naturally.
- **Narrow mode**: With the tree visible, the tab content area is narrower. The same ratios compress proportionally. Button column stays at 48px + padding regardless.
- **Short windows**: `MinHeight="120"` on Row 1 and Row 3 prevents either from collapsing.
- The Expander (Row 0) collapses to a single-line header when closed, maximizing space for the working area.

---

## Current Architecture

### File Map

| File | Role |
|------|------|
| `StoryCADLib/ViewModels/Tools/StructureBeatViewModel.cs` | Individual beat (data + element resolution + IoC) |
| `StoryCADLib/ViewModels/Tools/BeatSheetsViewModel.cs` | Loads predefined beat sheet templates from ToolsData |
| `StoryCADLib/ViewModels/ProblemViewModel.cs` | Contains all beat manipulation methods + Structure tab state |
| `StoryCADLib/Models/Elements/ProblemModel.cs` | Stores StructureTitle, StructureDescription, StructureBeats, BoundStructure |
| `StoryCADLib/Models/SavedBeatsheet.cs` | Serialization model for .stbeat files |
| `StoryCADLib/Models/Tools/ToolsData.cs` | Source of predefined beat sheet templates (BeatSheetSource) |
| `StoryCADLib/Services/API/StoryCADAPI.cs` | External API — beat sheet CRUD, assign/unassign, save/load |
| `StoryCADLib/Services/Collaborator/Contracts/IStoryCADAPI.cs` | API interface |
| `StoryCADLib/Services/Reports/ReportFormatter.cs` | Beat sheet report formatting (two methods) |
| `StoryCADLib/Assets/Install/reports/Structure Beats.txt` | Report template |
| `StoryCAD/Views/ProblemPage.xaml` | Structure tab UI (beats list, buttons, elements list) |
| `StoryCAD/Views/ProblemPage.xaml.cs` | Minimal code-behind (pointer press handler, BeatSheetsViewModel reference) |
| `docs/Story Elements/Structure_Tab.md` | User manual topic |

### StructureBeatViewModel (Current)

A beat with embedded element resolution. Serialized as part of the story file via ProblemModel.

```csharp
public class StructureBeatViewModel : ObservableObject
{
    // IoC dependencies (resolved in constructor)
    private readonly AppState _appState;
    private readonly OutlineService _outlineService;
    public ProblemViewModel ProblemViewModel;
    public Windowing Windowing;

    public StructureBeatViewModel(string title, string description)
    {
        Windowing = Ioc.Default.GetRequiredService<Windowing>();
        ProblemViewModel = Ioc.Default.GetRequiredService<ProblemViewModel>();
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        Title = title;
        Description = description;
        PropertyChanged += ProblemViewModel.OnPropertyChanged; // Memory leak: never unsubscribed
    }

    // --- Serialized fields ---
    [JsonInclude] [JsonPropertyName("Title")]       public string Title { get; set; }
    [JsonInclude] [JsonPropertyName("Description")] public string Description { get; set; }
    [JsonInclude] [JsonPropertyName("BoundGUID")]   public Guid Guid { get; set; }
    // Guid setter fires OnPropertyChanged for Element, ElementName, ElementDescription, ElementIcon

    // --- Computed (JsonIgnore) ---
    internal StoryElement Element =>
        (guid != Guid.Empty)
            ? _outlineService.GetStoryElementByGuid(_appState.CurrentDocument!.Model, guid)
            : new StoryElement();
    public string ElementName => (Element.Uuid == Guid.Empty) ? "No element Selected" : Element.Name;
    public string ElementDescription => /* casts to ProblemModel or SceneModel for .Description */
    public Symbol ElementIcon => /* Symbol.Help for Problem, Symbol.World for Scene, Symbol.Cancel for none */
}
```

**Problems:**
1. IoC calls in constructor — breaks deserialization outside app context
2. `PropertyChanged += ProblemViewModel.OnPropertyChanged` — never unsubscribed, memory leak, cross-talk between problems
3. Computed properties do live lookups every time they're read (3-4 throwaway StoryElement allocations per UI refresh for unbound beats)
4. Circular dependency: child beat resolves its parent ProblemViewModel from IoC

### ProblemModel (Data Storage)

```csharp
// In ProblemModel (StoryCADLib/Models/Elements/ProblemModel.cs)
public string StructureTitle { get; set; }        // e.g., "Save the Cat", "Custom Beat Sheet"
public string StructureDescription { get; set; }  // RTF description of the beat sheet
public ObservableCollection<StructureBeatViewModel> StructureBeats { get; set; } // The beats
public string BoundStructure { get; set; }         // GUID string of parent problem (reverse pointer)
```

**Note:** The model layer stores ViewModel objects (`ObservableCollection<StructureBeatViewModel>`). This is existing technical debt. After refactoring, these become `StructureBeat` plain data objects, which is more appropriate for a model.

### Beat Manipulation Methods (in ProblemViewModel)

All currently in `ProblemViewModel.cs`:

**CreateBeat** (line 398):
```csharp
private void CreateBeat()
{
    StructureBeats.Add(new StructureBeat("New Beat", "Describe your beat here"));
}
```

**DeleteBeatAsync** (line 400):
1. Guard: if `SelectedBeat == null`, returns
2. If not headless, shows confirmation dialog (context-aware message if beat is bound)
3. If user cancels, returns
4. Calls `_outlineService.DeleteBeat(_storyModel, _problemModel, SelectedBeatIndex)`
5. Catches exceptions and sends status warning

**MoveUp** (line 435): `StructureBeats.Move(SelectedBeatIndex, SelectedBeatIndex - 1)` with bounds check.

**MoveDown** (line 446): `StructureBeats.Move(SelectedBeatIndex, SelectedBeatIndex + 1)` with bounds check.

**AssignBeatAsync** — the most complex method:
1. Guard: if no beat selected, sends status warning "Select a beat" and returns
2. Guard: if no element selected, sends status warning "Select an element" and returns
3. Guard: if selected element is the problem whose beat sheet we're editing, sends status warning "Cannot assign a problem as a beat on itself" and returns
4. Looks up element in `StoryElements` by UUID
5. If element is a Problem:
   a. Checks if problem already has a `BoundStructure` (Guid, assigned elsewhere)
   b. If yes, shows dialog: "Already assigned to {name}. Assign here instead?"
   c. If user confirms, calls `RemoveBindData` to clear old binding
   d. Sets `problem.BoundStructure = this problem's Uuid`
6. Sets `SelectedBeat.Guid = element's UUID`
7. Clears beat selection (`SelectedBeat = null`, `SelectedBeatIndex = -1`)

No `CanExecute` — the method guards itself with status bar messages.

**UnbindElement** (line 223):
1. Checks SelectedBeat is not null and has a bound GUID
2. Calls `OutlineService.UnasignBeat(model, problemModel, selectedBeatIndex)`
3. Sets `SelectedBeat.Guid = Guid.Empty`
4. Clears selection

**removeBindData** (line 250) — helper for reassignment:
- Finds the beat in the old containing structure that points to the problem
- Sets that beat's Guid to `Guid.Empty`

**SaveBeatSheetAsync** (line 457): File picker → null check → `OutlineService.SaveBeatsheet(path, description, beats)`. Try/catch with status error message.

**LoadBeatSheet** (line 292): File picker → `OutlineService.LoadBeatsheet(path)`. **Missing null check on picker result** (bug).

**UpdateSelectedBeat** (line 961 — called from ComboBox SelectionChanged):
1. If beats exist, shows confirmation dialog
2. **BUG: Deletes all beats BEFORE checking dialog result** (lines 982-988)
3. Checks if "Load Custom Beat Sheet from file..." selected → calls LoadBeatSheet
4. Toggles `BeatsheetEditButtonsVisibility` and `IsBeatSheetReadOnly` based on "Custom Beat Sheet" string match
5. Loads new beat sheet template from `BeatSheetsViewModel.BeatSheets[value]`
6. Creates new `StructureBeatViewModel` instances from template's `PlotPatternScenes`

### XAML Structure (ProblemPage.xaml, Structure Tab)

Three-row layout:

**Row 0 — Expander Header:**
- Beat sheet label + "Elements" radio buttons (Scene/Problem) in header
- ComboBox bound to `BeatSheetsViewModel.PlotPatternNames`, `SelectionChanged` → `ProblemVm.UpdateSelectedBeat`
- Expander content: RichEditBox for beat sheet description (read-only when `IsBeatSheetReadOnly`)

**Row 1 — Three-column main area:**
- **Column 0 (Beats ListView):** `ItemsSource="{x:Bind ProblemVm.StructureBeats}"`, custom DataTemplate showing SymbolIcon + Title + ElementName. `PointerPressed` code-behind handler for beat selection.
- **Column 1 (Command Buttons):** 7 buttons between vertical separator lines:

| Button | Content | Size | Click Binding | Visibility |
|--------|---------|------|---------------|------------|
| AssignButton | `<` | 32x32 | **NONE (BUG)** | Always |
| UnassignButton | `>` | 32x32 | `ProblemVm.UnbindElement` | Always |
| AddBeatButton | `+` | 32x32 | `ProblemVm.CreateBeat` | `BeatsheetEditButtonsVisibility` |
| DeleteBeatButton | `-` | 32x32 | `ProblemVm.DeleteBeat` | `BeatsheetEditButtonsVisibility` |
| MoveUpButton | `∧` | 32x32 | `ProblemVm.MoveUp` | `BeatsheetEditButtonsVisibility` |
| MoveDownButton | `∨` | 32x32 | `ProblemVm.MoveDown` | `BeatsheetEditButtonsVisibility` |
| SaveBeatSheetButton | `S` | 32x32 | `ProblemVm.SaveBeatSheet` | `BeatsheetEditButtonsVisibility` |

- **Column 2 (Elements ListView):** `ItemsSource="{x:Bind ProblemVm.CurrentElementSource}"`, `ItemClick="{x:Bind ProblemVm.AssignBeat}"`, `DisplayMemberPath="Name"`. Shows scenes or problems based on radio button selection.

**Row 2 — Descriptions:**
- Beat Description TextBox (read-only when `IsBeatSheetReadOnly`)
- Element Description RichEditBox (always read-only)

### Visibility/ReadOnly Logic

In `ProblemViewModel.LoadModel()` (line 847) and `UpdateSelectedBeat()` (line 1002):
```csharp
if (StructureModelTitle == "Custom Beat Sheet")
{
    BeatsheetEditButtonsVisibility = Visibility.Visible;
    IsBeatSheetReadOnly = false;
}
else
{
    BeatsheetEditButtonsVisibility = Visibility.Collapsed;
    IsBeatSheetReadOnly = true;
}
```
This is the wall between "custom" and "template" beat sheets. Removing these conditionals (Phase 2) unifies the experiences.

### Dirty Tracking

Current flow:
1. Each `StructureBeatViewModel` subscribes `PropertyChanged += ProblemViewModel.OnPropertyChanged` in its constructor
2. When any beat property changes, `ProblemViewModel.OnPropertyChanged` fires
3. `OnPropertyChanged` checks `_changeable` flag and sets `_changed = true`
4. `_changed` triggers the save/dirty state indicator

**Replacement in BeatEditorViewModel:**
- Subscribe to `StructureBeats.CollectionChanged` for add/remove/move
- On add: subscribe to new beat's `PropertyChanged`
- On remove: unsubscribe from removed beat's `PropertyChanged`
- Forward change notifications to ProblemViewModel (or use Messenger)

### Report Code

**`FormatStoryProblemStructureReport()`** (ReportFormatter.cs line 584):
- Gets Story Problem from Overview
- Walks beats recursively via `ProcessBeat()`
- Returns plaintext string

**`ProcessBeat()`** (line 630):
- Indents by level, uses `HashSet<Guid>` to prevent infinite recursion
- Outputs: `{indent}- {beat.Title}` then `{indent}   {beat.ElementName}`
- If element is a Problem, recurses into its StructureBeats
- **Omits:** beat description, element description
- **Omits:** unassigned beats (does nothing when `beat.Guid == Guid.Empty`)

**`FormatStructureBeatsElements()`** (line 677):
- Flat (non-recursive) per-problem report
- Outputs: Title, Description, ElementName, ElementDescription for each beat
- Used in the per-problem report template

### Serialization (.stbeat files)

`SavedBeatsheet.cs`:
```csharp
public class SavedBeatsheet
{
    [JsonInclude] public string Description { get; set; }
    [JsonInclude] public List<StructureBeatViewModel> Beats { get; set; }
}
```

`OutlineService.SaveBeatsheet()` creates copies of beats (stripping GUIDs) and serializes to JSON.
`OutlineService.LoadBeatsheet()` deserializes from JSON — **requires IoC container because constructor calls IoC**.

After refactoring: `SavedBeatsheet.Beats` becomes `List<StructureBeat>`. Constructor no longer needs IoC. Deserialization works anywhere.

**JSON property names to preserve for backward compatibility:**
- `"Title"` → `Title`
- `"Description"` → `Description`
- `"BoundGUID"` → `Guid`

### ProblemPage.xaml.cs (Code-Behind)

```csharp
public sealed partial class ProblemPage : Page
{
    public BeatSheetsViewModel BeatSheetsViewModel = Ioc.Default.GetService<BeatSheetsViewModel>();
    public ProblemViewModel ProblemVm;

    // ExpanderSet — sets SelectedBeat from expander DataContext (appears unused in current XAML)
    // ListViewItem_PointerPressed — sets SelectedBeat from clicked beat item
    // OnNavigatedTo — sets appState.CurrentSaveable
}
```

After refactoring: `ListViewItem_PointerPressed` should be eliminated if possible (beat selection should work through ListView's `SelectedItem` binding). The code-behind may need a `BeatEditorViewModel` property for direct XAML binding.

### API (StoryCADAPI.cs)

The API has a comprehensive beat sheet interface that operates **directly on ProblemModel**, bypassing both ProblemViewModel and the new BeatEditorViewModel.

**API methods:**

| Method | What it does |
|--------|-------------|
| `GetBeatSheetNames()` | Returns predefined template names from ToolsData |
| `GetBeatSheet(name)` | Returns template description + beats for preview |
| `ApplyBeatSheetToProblem(problemGuid, name)` | Loads template beats onto a Problem (line 1443) |
| `GetProblemStructure(problemGuid)` | Returns current beats with assignments (line 1483) |
| `AssignElementToBeat(problemGuid, beatIndex, elementGuid)` | Sets beat GUID (line 1516) |
| `ClearBeatAssignment(problemGuid, beatIndex)` | Sets beat GUID to Empty (line 1551) |
| `CreateBeat(problemGuid, title, description)` | Adds new beat (line 1580) |
| `UpdateBeat(problemGuid, beatIndex, title, description)` | Modifies beat title/description (line 1606) |
| `DeleteBeat(problemGuid, beatIndex)` | Removes beat by index (line 1635) |
| `MoveBeat(problemGuid, fromIndex, toIndex)` | Reorders beats (line 1664) |
| `SaveBeatSheet(problemGuid, filePath)` | Saves beats to .stbeat file (line 1770) |
| `LoadBeatSheet(problemGuid, filePath)` | Loads beats from .stbeat file (line 1805) |

**API bugs (same class of bugs as the UI):**

| Bug | Location |
|-----|----------|
| `AssignElementToBeat` does NOT enforce the one-parent rule for Problems | line 1536 — just sets GUID, no BoundStructure check |
| `AssignElementToBeat` does NOT set `BoundStructure` on assigned Problems | line 1536 |
| `ClearBeatAssignment` does NOT clear `BoundStructure` on the unassigned Problem | line 1564 |
| `DeleteBeat` does NOT clear `BoundStructure` on the deleted beat's bound Problem | line 1648 |
| `LoadBeatSheet` hardcodes `"Custom Beat Sheet"` as title | line 1822 |
| `ApplyBeatSheetToProblem` creates `new StructureBeatViewModel(...)` (IoC in constructor) | line 1463 |
| `CreateBeat` creates `new StructureBeatViewModel(...)` (IoC in constructor) | line 1590 |
| `LoadBeatSheet` creates `new StructureBeatViewModel(...)` (IoC in constructor) | line 1827 |

**Key insight:** The API and UI share the same business logic gaps because there is no shared service layer for beat assignment rules. Both directly manipulate `ProblemModel.StructureBeats` and `BoundStructure` independently (and inconsistently).

---

## Target Architecture

### Shared Business Logic in OutlineService

Extract beat assignment business rules into `OutlineService` methods that both the API and BeatEditorViewModel call:

```csharp
// In OutlineService — shared by API and BeatEditorViewModel

/// Assigns an element to a beat, enforcing the one-parent rule for Problems.
/// Returns (success, message) — message explains why if failed, or what happened if reassigned.
public (bool Success, string Message) AssignElementToBeat(
    StoryModel model, ProblemModel parentProblem, int beatIndex, Guid elementGuid);

/// Clears a beat assignment, cleaning up BoundStructure if the assigned element is a Problem.
public void ClearBeatAssignment(
    StoryModel model, ProblemModel parentProblem, int beatIndex);

/// Deletes a beat, cleaning up BoundStructure if needed.
public void DeleteBeatWithCleanup(
    StoryModel model, ProblemModel parentProblem, int beatIndex);
```

The one-parent enforcement logic (currently in `ProblemViewModel.AssignBeat` lines 170-207) moves here. Both the API and the UI call these methods. The API methods become thin wrappers around OutlineService calls. BeatEditorViewModel calls the same methods but adds UI concerns (showing a dialog when reassignment is needed).

**The dialog flow in BeatEditorViewModel:**
1. Call `OutlineService.CheckAssignment(...)` to see if the problem is already assigned elsewhere
2. If yes, show dialog to user
3. If user confirms, call `OutlineService.AssignElementToBeat(...)` which handles the cleanup
4. The API skips the dialog — it just calls `AssignElementToBeat(...)` directly (the caller decides)

### StructureBeat (renamed from StructureBeatViewModel)

Plain data object. No IoC, no computed properties, no event subscriptions.

```csharp
public class StructureBeat : ObservableObject
{
    [JsonInclude] [JsonPropertyName("Title")]       public string Title { get; set; }
    [JsonInclude] [JsonPropertyName("Description")] public string Description { get; set; }
    [JsonInclude] [JsonPropertyName("BoundGUID")]   public Guid Guid { get; set; }
}
```

Must keep `ObservableObject` base for `PropertyChanged` support (dirty tracking in BeatEditorViewModel). Must preserve `[JsonPropertyName]` values for backward compatibility with existing `.stbx` and `.stbeat` files.

### BeatEditorViewModel (new)

Owns all beat manipulation logic and element resolution for the UI.

**Properties:**
- `ObservableCollection<StructureBeat> StructureBeats`
- `StructureBeat SelectedBeat`
- `int SelectedBeatIndex`
- `string StructureModelTitle`
- `string StructureDescription`
- `string BoundStructure`
- `ObservableCollection<StoryElement> CurrentElementSource`
- `StoryElement SelectedListElement`
- `string SelectedElementSource` (Scene/Problem radio)
- `IReadOnlyList<string> ElementSource` (the radio options)

**Element resolution methods** (moved from StructureBeatViewModel):
- `GetElementName(StructureBeat beat)` → string
- `GetElementDescription(StructureBeat beat)` → string
- `GetElementIcon(StructureBeat beat)` → Symbol
- These can be exposed as methods that the XAML DataTemplate calls via a converter, or the beat ListView ItemTemplate can bind to BeatEditorViewModel properties that update when SelectedBeat changes.

**Commands (RelayCommand/AsyncRelayCommand):**

| Command | Guard logic (inline, no CanExecute) |
|---------|--------------------------------------|
| `CreateBeatCommand` | None — always executes |
| `DeleteBeatCommand` | Returns if `SelectedBeat == null` |
| `MoveUpCommand` | Returns if `SelectedBeat == null` or already first |
| `MoveDownCommand` | Returns if `SelectedBeat == null` or already last |
| `AssignBeatCommand` | Returns with status warning if no beat selected, no element selected, or self-assignment |
| `UnbindElementCommand` | Returns if `SelectedBeat == null` or beat has no assignment |
| `SaveBeatSheetCommand` | None — always executes |
| `LoadBeatSheetCommand` | None — always executes |

**Dirty tracking:**
- Subscribe to `StructureBeats.CollectionChanged`
- On item add: subscribe to beat's `PropertyChanged`
- On item remove: unsubscribe from beat's `PropertyChanged`
- Raise a `BeatSheetChanged` event or use Messenger to notify ProblemViewModel

**Beat sheet loading (from UpdateSelectedBeat):**
- Moved into BeatEditorViewModel
- ComboBox selection change triggers a method on BeatEditorViewModel
- Dialog confirmation BEFORE deleting existing beats (fixes the data-loss bug)
- No visibility/readonly toggles (all beat sheets are editable)

### ProblemViewModel Changes

- Create `BeatEditorViewModel` in constructor (never reassign)
- `LoadModel()`: populate `BeatEditorVm` properties from `ProblemModel`
- `SaveModel()`: write `BeatEditorVm` properties back to `ProblemModel`
- Remove all beat manipulation methods, beat-related properties, visibility/readonly logic
- Keep non-beat responsibilities (Problem tab, Protagonist, Antagonist, Resolution, Notes)

### API Changes

The API methods that create `new StructureBeatViewModel(...)` must change to `new StructureBeat(...)`.

The API methods that manipulate assignments must delegate to `OutlineService` shared methods:

| API Method | Change |
|------------|--------|
| `AssignElementToBeat` | Call `OutlineService.AssignElementToBeat(...)` — gains one-parent enforcement + BoundStructure management |
| `ClearBeatAssignment` | Call `OutlineService.ClearBeatAssignment(...)` — gains BoundStructure cleanup |
| `DeleteBeat` | Call `OutlineService.DeleteBeatWithCleanup(...)` — gains BoundStructure cleanup |
| `ApplyBeatSheetToProblem` | Change `new StructureBeatViewModel(...)` → `new StructureBeat(...)` |
| `CreateBeat` | Change `new StructureBeatViewModel(...)` → `new StructureBeat(...)` |
| `LoadBeatSheet` | Change `new StructureBeatViewModel(...)` → `new StructureBeat(...)`, remove hardcoded "Custom Beat Sheet" |

### XAML Binding Changes

Structure tab bindings change from `ProblemVm.*` to `ProblemVm.BeatEditorVm.*`:
- `ProblemVm.StructureBeats` → `ProblemVm.BeatEditorVm.StructureBeats`
- `ProblemVm.SelectedBeat` → `ProblemVm.BeatEditorVm.SelectedBeat`
- Button `Click` → `Command="{x:Bind ProblemVm.BeatEditorVm.AssignBeatCommand}"`
- Remove `ItemClick` from elements ListView
- Remove `Visibility` bindings from buttons (all always visible)
- Remove `IsReadOnly` binding from beat description TextBox

Alternative: Expose `BeatEditorViewModel` directly on page code-behind to avoid deep `x:Bind` chains.

### Element Resolution in ListView ItemTemplate

The beat ListView currently uses a DataTemplate bound to `StructureBeatViewModel` properties (`ElementIcon`, `Title`, `ElementName`). After refactoring, `StructureBeat` won't have `ElementIcon` or `ElementName`.

**Options:**
1. **Converter approach**: Create an `IValueConverter` that takes a GUID and returns the element name/icon. Requires access to OutlineService.
2. **Wrapper approach**: BeatEditorViewModel maintains a parallel collection of display objects that combine beat data with resolved element info.
3. **SelectedBeat-only approach**: Only resolve element info for the selected beat (show in the description area below). The ListView only shows Title. Simplest but loses the current UX where you can see assigned elements at a glance.

**Recommendation**: Option 2 or keep `ElementName`/`ElementIcon` as computed properties on `StructureBeat` but have `BeatEditorViewModel` set them explicitly (not via IoC lookup in the beat itself). This avoids the converter complexity while keeping the data object clean.

---

## Bugs to Fix During Refactoring

These existing bugs must be fixed during the extraction, not deferred:

| Bug | Severity | Location | Fix |
|-----|----------|----------|-----|
| `UpdateSelectedBeat` deletes all beats BEFORE checking dialog result | CRITICAL | ProblemViewModel lines 982-988 | Move deletion loop inside `Result == Primary` check |
| Memory leak: `PropertyChanged += ProblemViewModel.OnPropertyChanged` never unsubscribed | CRITICAL | StructureBeatViewModel line 31 | Remove; BeatEditorViewModel manages dirty tracking via CollectionChanged |
| `LoadBeatSheet` doesn't null-check file picker result | HIGH | ProblemViewModel line 297 | Add null check like `SaveBeatSheet` has |
| `DeleteBeat` doesn't clear `BoundStructure` on bound problem | HIGH | OutlineService + API | Move to `OutlineService.DeleteBeatWithCleanup` shared method |
| API `AssignElementToBeat` skips one-parent enforcement + BoundStructure | HIGH | StoryCADAPI line 1536 | Delegate to `OutlineService.AssignElementToBeat` shared method |
| API `ClearBeatAssignment` doesn't clear BoundStructure | HIGH | StoryCADAPI line 1564 | Delegate to `OutlineService.ClearBeatAssignment` shared method |
| `async void` on AssignBeat, SaveBeatSheet, LoadBeatSheet, UpdateSelectedBeat | HIGH | ProblemViewModel | Convert to `AsyncRelayCommand` (or `async Task` with try/catch) |
| Unsafe `as` cast in AssignBeat (NullReferenceException) | HIGH | ProblemViewModel line 159 | Use direct cast or null check |
| `BoundStructure` string-encoded GUID ambiguity (Empty vs null vs Guid.Empty.ToString()) | MEDIUM | ProblemModel | Standardize on `string.Empty` for unbound |
| "Custom Beat Sheet" magic string in 4+ locations | MEDIUM | Multiple files | Extract to constant (then remove conditional in Phase 2) |
| API `LoadBeatSheet` hardcodes "Custom Beat Sheet" | MEDIUM | StoryCADAPI line 1822 | Remove after unification |
| `public int _selectedBeatIndex` backing field | LOW | ProblemViewModel line 676 | Make private |

---

## API Testing

### Existing API Tests (StoryCADAPITests.cs)

18+ tests covering all beat sheet CRUD operations:

| Test | What it covers |
|------|---------------|
| `GetBeatSheetNames_WhenCalled_ReturnsNames` | Template listing |
| `GetBeatSheet_ValidName_ReturnsDescriptionAndBeats` | Template preview |
| `GetBeatSheet_InvalidName_ReturnsFailure` | Invalid template name |
| `ApplyBeatSheetToProblem_ValidInputs_AppliesTemplate` | Loading template onto Problem |
| `ApplyBeatSheetToProblem_InvalidProblem_ReturnsFailure` | Invalid Problem GUID |
| `ApplyBeatSheetToProblem_InvalidBeatSheet_ReturnsFailure` | Invalid template name |
| `AssignElementToBeat_ValidInputs_AssignsElement` | Scene assignment |
| `ClearBeatAssignment_ValidInputs_ClearsAssignment` | Clearing assignment |
| `CreateBeat_ValidInputs_AddsBeat` | Adding a beat |
| `UpdateBeat_ValidInputs_UpdatesBeat` | Modifying beat title/description |
| `DeleteBeat_ValidInputs_RemovesBeat` | Removing a beat |
| `MoveBeat_ValidInputs_ReordersBeat` | Reordering beats |
| `GetProblemStructure_*` | Reading structure (with/without beats) |
| `SaveBeatSheet_*` | Saving to .stbeat file (valid, invalid GUID, no beats) |
| `LoadBeatSheet_*` | Loading from .stbeat file (valid, invalid GUID, invalid file) |

### Missing API Tests (to add in Phase 0)

These test the business rules that are currently missing from both UI and API:

| Test | What it should verify |
|------|----------------------|
| `AssignElementToBeat_Problem_SetsBoundStructure` | `BoundStructure` on the assigned Problem is set to the parent Problem's GUID |
| `AssignElementToBeat_ProblemAlreadyAssigned_EnforcesOneParent` | Assigning a Problem that's already assigned elsewhere either fails or reassigns (depending on API contract) |
| `ClearBeatAssignment_Problem_ClearsBoundStructure` | `BoundStructure` on the unassigned Problem is cleared |
| `DeleteBeat_WithBoundProblem_ClearsBoundStructure` | `BoundStructure` on the deleted beat's bound Problem is cleared |
| `AssignElementToBeat_ProblemToSelf_Fails` | Cannot assign a Problem as a beat on itself |
| `AssignElementToBeat_Scene_NoBoundStructure` | Scenes don't get `BoundStructure` set (only Problems do) |
| `AssignElementToBeat_SceneToMultipleBeats_Succeeds` | A Scene can appear in multiple beats/problems |

These tests should be written against `OutlineService` shared methods (the single source of truth for beat assignment business rules) and also via the API surface to verify end-to-end behavior.

### OutlineService as Single Source of Truth

Both `ProblemViewModel` (UI) and `StoryCADAPI` call `OutlineService`. Currently only *some* operations go through `OutlineService` (`DeleteBeat`, `UnasignBeat`), while others (assign with one-parent enforcement, `BoundStructure` management) are done inline in `ProblemViewModel` and **skipped entirely** in the API.

After refactoring, ALL beat assignment business rules live in `OutlineService`:

```
UI (BeatEditorViewModel)  ──→  OutlineService  ←──  API (StoryCADAPI)
                                    │
                           Shared business rules:
                           - One-parent enforcement
                           - BoundStructure management
                           - Beat CRUD with cleanup
```

No beat assignment logic should exist outside `OutlineService`. The UI adds only UI concerns (dialogs, selection state). The API adds only API concerns (GUID validation, OperationResult wrapping).

---

## Risk Mitigations

| Risk | Mitigation |
|------|------------|
| Serialization break from constructor/rename change | Phase 0 round-trip test verifies backward compat before any changes |
| Data loss in UpdateSelectedBeat | Fix during Phase 1 extraction |
| XAML deep x:Bind null-chain failure | Create BeatEditorVM in ProblemVM constructor, never reassign |
| Naming confusion (BeatSheetsVM vs new VM) | Named BeatEditorViewModel to clearly distinguish |
| Thread safety with concurrent async commands | CanExecute guards prevent concurrent invocation |
| No test safety net | Phase 0 characterization tests before refactoring |
| API regression | API methods delegate to same OutlineService methods as UI; shared tests cover both paths |
