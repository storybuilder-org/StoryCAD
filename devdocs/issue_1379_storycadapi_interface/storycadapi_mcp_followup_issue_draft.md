# Draft: StoryCADAPI follow-up issue for collection-entry MCP tools

This is a draft for a new issue to be filed in `storybuilder-org/StoryCADAPI` once #1379 is ready for parallel work in StoryCADAPI. File it via `gh issue create --repo storybuilder-org/StoryCADAPI` or the web UI.

---

## Title

Add MCP tools for indexed list-property editing (`add_collection_entry`, `update_collection_entry`, `remove_collection_entry`)

---

## Body

## Description

StoryCAD's StoryWorld element has list-typed properties (Physical Worlds, Species, Cultures, Governments, Religions). Today the MCP can update single-value fields on StoryWorld but not these list fields, because `update_property` resolves to the API's `UpdateElementProperty`, which only handles scalar conversions. The MCP user has no way to add, change, or remove a single entry in one of these lists short of replacing the whole element.

storybuilder-org/StoryCAD#1379 fixes this on the API side by adding three new methods — `AddCollectionEntry`, `UpdateCollectionEntry`, `RemoveCollectionEntry` — to both `StoryCADApi` and `IStoryCADAPI`, plus a pre-check in `UpdateElementProperty` that returns a clear error pointing at the new methods when called on a list property. This issue exposes those API methods through three matching MCP tools.

This issue completes the user-visible fix: without it, the StoryWorld list-property bug stays open in production for any MCP user.

## Dependency

Blocked by storybuilder-org/StoryCAD#1379. Local development can proceed in parallel because StoryCADMcp references StoryCADLib via `ProjectReference`, so this branch can be developed against the local `issue-1379-complete-storycadapi-interface` branch in the sibling StoryCAD clone. Merge order: #1379 to StoryCAD `dev` first, then this PR to StoryCADAPI `dev`. Both ride into Release 4.1.

## Tasks

1. Branch off StoryCADAPI's working branch (coordinate with whoever owns `issue-1246-repo-reorg`).
2. Add three tools in `StoryCADMcp/Tools/WriteTools.cs`:
   - `add_collection_entry` — wraps `api.AddCollectionEntry(elementGuid, propertyName, entry)`. Returns the new index on success.
   - `update_collection_entry` — wraps `api.UpdateCollectionEntry(elementGuid, propertyName, index, entry)`.
   - `remove_collection_entry` — wraps `api.RemoveCollectionEntry(elementGuid, propertyName, index)`.
3. The `entry` parameter on Add/Update should be accepted as a JSON string and parsed into `JsonElement` before calling the API; the API's `TryConvertEntry` already handles `JsonElement` and `Dictionary<string, object>` payloads.
4. Add `[Description]` annotations matching the style of existing MCP tools (e.g. `update_property`).
5. Add tests in `StoryCADMcp.Tests/` (note: per #1379 verification, that project currently has a pre-existing `UNOB0005` Uno SDK build issue that needs sorting before tests can run; if not blocking, file separately).
6. **Real-world MCP testing**: with `issue-1379-...` checked out in StoryCAD and this branch checked out in StoryCADAPI, run the StoryCADMcp server, connect an MCP client (Claude Desktop), open a real outline, and exercise add/update/remove on StoryWorld's `PhysicalWorlds`, `Cultures`, etc. Verify the indexed-list fix end-to-end.
7. Update MCP-side documentation (README in `StoryCADMcp/`) listing the new tools.

## Out of scope

- Changes to StoryCAD's `IStoryCADAPI` (done in #1379).
- ObservableCollection or non-`List<T>` support (rejected in #1379).
- New CLI tools or sample apps (file separately if needed).

## Environment
**StoryCAD Version:** 4.1
**OS:** Windows/macOS

---

### Design tasks / sign-off
- [ ] Plan this section
- [ ] Human approves plan
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
- [ ] Human final approval
