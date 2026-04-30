# Issue 1379 — Plan

**Issue**: [#1379](https://github.com/storybuilder-org/StoryCAD/issues/1379) — Complete `IStoryCADAPI` interface and remove deprecated SK Planner dependency
**Branch**: `issue-1379-complete-storycadapi-interface`
**Date opened**: 2026-04-29
**Status**: Ready for design review

---

## 1. Background

`IStoryCADAPI` was built for Collaborator. Collaborator uses StoryCAD's UX pickers to let the writer choose which story elements a workflow operates on, so it only ever needs to read and update those elements — never create them. That's why `AddElement` and similar create/move/search methods were never put on the interface. It wasn't an oversight; it was the right shape for the only consumer at the time.

The consumer base has since grown well beyond Collaborator. There's the StoryCADAPI repo with about half a dozen sample apps, an MCP server, and an always-on website that generates writer's prompts. None of those use pickers — they need the full API surface, including create, move, search, and trash operations. The gap became obvious when an interface reference was added to one of the samples and `AddElement` wasn't there.

The actual `StoryCADApi` class has all the methods these new consumers need; the interface just hasn't been kept up.

Separately, StoryCADLib still references an old Semantic Kernel Planner package that's no longer maintained and isn't used by any current StoryCAD code. It needs to come out before we publish more samples or new versions of the library.

This issue blocks #1246 (samples and docs). Samples can't be written against an interface with holes, and the old Planner package shows up as a warning in any project that uses StoryCADLib.

## 2. Scope

### 2A. Make the interface complete
- Look at every public method and property on `StoryCADApi`.
- Make a list showing, for each one, whether it's already on `IStoryCADAPI` and whether it should be.
- Add the missing ones to `IStoryCADAPI`.

### 2B. Remove the old Planner package
- Find the Semantic Kernel Planner package reference in StoryCADLib's project files.
- Confirm nothing in StoryCADLib actually uses it.
- Remove it. Build and test on Windows. Spot-check on Mac.

### 2C. Fix list-property updates (added 2026-04-29)
StoryWorld has properties that hold lists — Physical Worlds, Species, Cultures, Governments, Religions. The MCP can update single-value fields on StoryWorld but not these list fields. The cause is in the API itself, not the MCP: `UpdateElementProperty` only knows how to convert simple values (strings, numbers, GUIDs), so when given list data it throws. This affects every list-typed property across every element type, not just StoryWorld. Whole-element replacement (`UpdateStoryElement`) does work, because it goes through the JSON serializer, but that's a heavy workaround.

Where the fix has to go: `StoryCADLib/Services/API/StoryCADAPI.cs`, in the value-conversion block of `UpdateElementProperty` (around lines 680–691).

To do:
- Teach `UpdateElementProperty` how to take list values and turn them into the right typed list.
- Add a test that sets one of StoryWorld's list properties (e.g. `Cultures`) and reads it back.
- The interface and the actual class disagree on the return type of this method (`OperationResult<object>` vs `OperationResult<StoryElement>`). Note this in the audit list and pick one.

## 3. Audit results (2026-04-29)

Rule applied: if a method works on a story or returns story data, it belongs on the interface. Internal helpers and methods that only make sense inside the picker UI can stay off, but each one needs a short reason.

`StoryCADApi` is in `StoryCADLib/Services/API/StoryCADAPI.cs`. `IStoryCADAPI` is in `StoryCADLib/Services/Collaborator/Contracts/IStoryCADAPI.cs`. Counts: 51 public members on the class, 34 on the interface. Gap: 17 missing members and 1 signature mismatch.

### 3.1 Already on the interface (no action needed unless flagged)

These match between class and interface:

`CurrentModel`, `CreateEmptyOutline`, `WriteOutline`, `GetAllElements`, `GetElementsByType`, `UpdateStoryElement`, `UpdateElementProperties`, `GetStoryElement`, `GetExamples`, `GetConflictCategories`, `GetConflictSubcategories`, `GetConflictExamples`, `ApplyConflictToProtagonist`, `ApplyConflictToAntagonist`, `GetKeyQuestionElements`, `GetKeyQuestions`, `GetMasterPlotNames`, `GetMasterPlotNotes`, `GetMasterPlotScenes`, `GetStockSceneCategories`, `GetStockScenes`, `GetBeatSheetNames`, `GetBeatSheet`, `ApplyBeatSheetToProblem`, `GetProblemStructure`, `AssignElementToBeat`, `ClearBeatAssignment`, `CreateBeat`, `UpdateBeat`, `DeleteBeat`, `MoveBeat`, `SaveBeatSheet`, `LoadBeatSheet`.

### 3.2 Return-type observation (no change)

| Member | Interface says | Class says | What to do |
|---|---|---|---|
| `UpdateElementProperty` | returns `OperationResult<object>` | returns `OperationResult<StoryElement>` | No change. `OperationResult<object>` on the interface is intentional — it can hold whatever the method returns. Audit-time flag was overzealous. |

### 3.3 Missing from the interface

| Member | What to do | Why |
|---|---|---|
| `AddElement(type, parent, name, GUIDOverride)` | Add | The known gap. Any caller building an outline needs this. |
| `AddElement(type, parent, name, properties, GUIDOverride)` | Add | Add-with-properties overload, same reason. |
| `GetStoryWorld()` | Add | Convenience getter for the singleton StoryWorld element. Useful for any caller that needs worldbuilding data. |
| `OpenOutline(path)` | Add | Loading an outline from disk is a basic operation any caller needs. |
| `DeleteElement(Guid)` | Add | Standard delete. See note below — this overlaps with `DeleteStoryElement(string)`. |
| `DeleteStoryElement(string)` | Don't add | Duplicate of `DeleteElement(Guid)`. |
| `SetCurrentModel(model)` | Don't add | Duplicate of the `CurrentModel` property setter. |
| `GetElement(Guid)` | Don't add | Duplicate of `GetStoryElement(Guid)`. |
| `AddCastMember(scene, character)` | Add | Linking a character to a scene is a basic outline operation. |
| `AddRelationship(source, target, desc, mirror)` | Add | Relationship between two characters; basic operation. |
| `MoveElement(elementGuid, newParent)` | Add | Moving an element under a new parent is a basic outline operation. |
| `SearchForText(text)` | Add | Search across the outline; useful for any caller. |
| `SearchForReferences(targetUuid)` | Add | Find every place that references an element; useful for any caller. |
| `RemoveReferences(targetUuid)` | Add | Counterpart to `SearchForReferences`. |
| `SearchInSubtree(rootNodeGuid, text)` | Add | Search scoped to a subtree. |
| `RestoreFromTrash(elementToRestore)` | Add | Trashcan workflow; pairs with `DeleteElement`. |
| `EmptyTrash()` | Add | Trashcan workflow. |

### 3.4 Notes that came out of the audit

1. **`UpdateElementProperty` and list-typed properties** — surfaced in §2C. The method can't handle list values today; the fix is the three new collection-entry methods (D4) plus a pre-check in `UpdateElementProperty` itself that returns a clear error when called on a list-typed property.

## 4. Development approach

TDD. Every interface addition, every caller migration, and every fix lands red-then-green: failing test first, then the code that makes it pass.

## 5. Decisions

- [x] **D1** — Interface scope: full public surface of `StoryCADApi`. See §3 for the list.
- [x] **D2** — Three concrete-class duplicates exist (`DeleteStoryElement`, `SetCurrentModel`, `GetElement`). Not added to the interface. No deprecation or caller migration in this issue — that's a separate cleanup if desired.
- [x] **D3** — SK Planner package: straight delete. Planner has been retired upstream; nothing in current code uses it.
- [x] **D4** — List-property fix: add three new methods on both the class and the interface — `AddCollectionEntry(guid, propertyName, entry)`, `UpdateCollectionEntry(guid, propertyName, index, entry)`, `RemoveCollectionEntry(guid, propertyName, index)`. Each does one thing. The MCP grows three matching tools. `UpdateElementProperty` stays scalar-only and gains a pre-check that returns a clear error when called on a list-typed property.

## 6. Plan

TDD throughout: each step lands red-then-green, with a failing test added before the change. Steps are written in dependency order; tests for each step are part of that step, not a separate phase.

All file paths in this section are relative to repo root.

### Step 1 — Add 13 missing methods to `IStoryCADAPI`

File: `StoryCADLib/Services/Collaborator/Contracts/IStoryCADAPI.cs`

The class signatures already exist on `StoryCADApi` (`StoryCADLib/Services/API/StoryCADAPI.cs`); copy each one to the interface verbatim.

```csharp
OperationResult<Guid>                                  AddElement(StoryItemType typeToAdd, string parentGUID, string name, string GUIDOverride = "");
OperationResult<Guid>                                  AddElement(StoryItemType typeToAdd, string parentGUID, string name, Dictionary<string, object> properties, string GUIDOverride = "");
OperationResult<StoryElement>                          GetStoryWorld();
Task<OperationResult<bool>>                            OpenOutline(string path);
Task<OperationResult<bool>>                            DeleteElement(Guid elementToDelete);
OperationResult<bool>                                  AddCastMember(Guid scene, Guid character);
OperationResult<bool>                                  AddRelationship(Guid source, Guid recipient, string desc, bool mirror = false);
OperationResult<bool>                                  MoveElement(Guid elementGuid, Guid newParentGuid);
OperationResult<List<Dictionary<string, object>>>      SearchForText(string searchText);
OperationResult<List<Dictionary<string, object>>>      SearchForReferences(Guid targetUuid);
OperationResult<int>                                   RemoveReferences(Guid targetUuid);
OperationResult<List<Dictionary<string, object>>>      SearchInSubtree(Guid rootNodeGuid, string searchText);
Task<OperationResult<bool>>                            RestoreFromTrash(Guid elementToRestore);
Task<OperationResult<bool>>                            EmptyTrash();
```

That is 14 lines for 13 methods because `AddElement` has two overloads.

### Step 2 — Add three new collection-entry methods on class and interface

Files:
- `StoryCADLib/Services/API/StoryCADAPI.cs` (implementation)
- `StoryCADLib/Services/Collaborator/Contracts/IStoryCADAPI.cs` (declaration)

```csharp
OperationResult<int>  AddCollectionEntry   (Guid elementGuid, string propertyName, object entry);
OperationResult<bool> UpdateCollectionEntry(Guid elementGuid, string propertyName, int index, object entry);
OperationResult<bool> RemoveCollectionEntry(Guid elementGuid, string propertyName, int index);
```

Behavior, common to all three:
1. Look up the element by GUID. Failure → "StoryElement not found."
2. Reflect to find the property. Failure → "Property '<name>' not found on type <X>."
3. Confirm the property is `[JsonInclude]` and writable. Failure → "Property is not editable."
4. Confirm the property type is `List<T>`. Failure → "Property '<name>' is not a collection."
5. For Add/Update: confirm `entry` converts to `T`. Use `JsonSerializer.Deserialize` with the property's element type if `entry` is a `JsonElement`/`JObject`/`Dictionary<string,object>`; otherwise direct cast. Failure → "Entry type does not match collection element type."
6. For Update/Remove: confirm `index` is in range `[0, list.Count - 1]`. Failure → "Index out of range."
7. Mutate: `Add` calls `list.Add(entry)` and returns the new index. `Update` does `list[index] = entry`. `Remove` does `list.RemoveAt(index)`.
8. Set `CurrentModel.Changed = true`.

The MCP gets three matching tools: `add_collection_entry`, `update_collection_entry`, `remove_collection_entry` in `StoryCADMcp/Tools/WriteTools.cs`.

Pre-check on `UpdateElementProperty` (file: `StoryCADLib/Services/API/StoryCADAPI.cs`, around lines 680–691): before the existing value-conversion block, detect when the target property type is `List<T>` and short-circuit:

```csharp
if (property.PropertyType.IsGenericType
    && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
{
    return OperationResult<StoryElement>.Failure(
        $"Property '{propertyName}' is a collection. Use AddCollectionEntry, UpdateCollectionEntry, or RemoveCollectionEntry.");
}
```

This replaces the cryptic `Convert.ChangeType` failure with a clear pointer to the right methods.

### Step 3 — Remove the SK Planner package

File: `StoryCADLib/StoryCADLib.csproj`

Remove the `<PackageReference Include="Microsoft.SemanticKernel.Planners.*" ... />` lines and any `using Microsoft.SemanticKernel.Planning;` imports that go unreferenced after the package is gone. Run `dotnet restore` and confirm the build is clean.

### Step 4 — Build and test

- Windows: full solution build, full test run.
- macOS: build `net10.0-desktop`, run tests.

### Coordination

All changes here are additive to the interface; no method signatures change. StoryCADAPI and Collaborator continue to build against StoryCAD without modification.

## 7. Out of scope

- Reorganizing `StoryCADApi` internals.
- Changing the Collaborator picker.
- Renaming or restructuring `IStoryCADAPI`.
- Publishing a new NuGet release — that's a separate release-engineering step.

## 8. References

- Issue [#1379](https://github.com/storybuilder-org/StoryCAD/issues/1379) (this one)
- Issue [#1246](https://github.com/storybuilder-org/StoryCAD/issues/1246) (samples and docs — depends on this)
- `devdocs/issue_1246_api_samples/issue_1246_status_log.md`
- StoryCAD wiki entries for the API and interface (under `wiki/repos/StoryCAD/entities/`, when present)
