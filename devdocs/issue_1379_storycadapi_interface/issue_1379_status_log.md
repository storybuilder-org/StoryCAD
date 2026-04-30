# Issue 1379: Complete IStoryCADAPI Interface and Remove Deprecated SK Planner Dependency

## Current Status
- **Phase**: Code section approved. Test verifications complete (Windows + sibling builds). macOS step pending user. Then Evaluate + commit/PR.
- **Last updated**: 2026-04-29 (end of session 2)
- **Repo / branch**: `/mnt/d/dev/src/StoryCAD`, branch `issue-1379-complete-storycadapi-interface` (off `dev`). Local-only — not pushed. No commits yet on the branch.
- **Working-tree state**: 4 modified files (interface, class, csproj, packages.props), 3 new test files, 2 new devdocs files. No commits yet.
- **Tests**: 991 total / 977 passed / 14 skipped / **0 failed**. Full regression clean. Build clean on both `net10.0-desktop` and `net10.0-windows10.0.22621`.
- **Milestone**: Release 4.1. **Prerequisite for** #1246.

## To resume

1. `cd /mnt/d/dev/src/StoryCAD && git checkout issue-1379-complete-storycadapi-interface`
2. Read `devdocs/issue_1379_storycadapi_interface/issue_1379_plan.md` — that's the design-review-ready plan.
3. Get sign-off on the plan (issue body Design checkboxes), then start implementation TDD per §6.

## Scope (final, from plan §2)

- **2A.** Add missing public methods of `StoryCADApi` to `IStoryCADAPI` (13 of them; full list in plan §3.3).
- **2B.** Remove the obsolete Semantic Kernel Planner package from `StoryCADLib`.
- **2C.** Fix the list-property update bug — add three new collection-entry methods (`AddCollectionEntry`, `UpdateCollectionEntry`, `RemoveCollectionEntry`) on both class and interface, plus a pre-check on `UpdateElementProperty` that returns a clear error when called on a list-typed property.

All interface changes are additive; no signature changes elsewhere.

## Plan summary (from plan §6)

1. Add 13 missing methods to `IStoryCADAPI`.
2. Add 3 new collection-entry methods (class + interface) + `UpdateElementProperty` pre-check; MCP gets matching tools.
3. Remove the SK Planner package.
4. Build + test (Windows full; macOS `net10.0-desktop`).

TDD throughout (red-green per change).

## Decisions (final)

- **D1** ✅ Full public surface of `StoryCADApi` goes on the interface.
- **D2** ✅ Three concrete-class duplicates (`DeleteStoryElement`, `SetCurrentModel`, `GetElement`) intentionally not on the interface. No deprecation in this issue.
- **D3** ✅ SK Planner: straight delete.
- **D4** ✅ List-property fix = three named collection-entry methods + `UpdateElementProperty` pre-check.

## Reverted / out of scope (do not re-litigate)

- Marking the three duplicates `[Obsolete]` and migrating ~50 callers — was scope-creep, removed.
- Changing `UpdateElementProperty`'s interface return type from `OperationResult<object>` → `OperationResult<StoryElement>` — interface intentionally returns `object`.
- Fixing the fake-async on `DeleteElement` / `RestoreFromTrash` / `EmptyTrash` — not broken, don't fix.
- Expanding the collection-entry methods to handle `ObservableCollection<T>` or non-StoryWorld element types — works for `List<T>` now, leave alone.
- Adding XML doc comments to the 17 new interface members.
- Mocks (we don't use them).

## Session Log

### 2026-04-29 (session 2 — Test phase)
- Code section closed (Human final approval ticked). Test plan posted (5 verification tasks); user approved.
- **Windows full regression**: build clean both targets; `vstest.console.exe` over `StoryCADTests.dll` → 991 / 977 / 14 / 0.
- **Collaborator sibling build** (`/mnt/d/dev/src/Collaborator/Collaborator.sln`): 0 errors. Pre-existing `MSB3277` SK Planner version-conflict warnings unrelated to #1379 (PromptTestRunner project pulls Planners.OpenAI 1.16.0-preview directly).
- **StoryCADAPI sibling build** (no top-level sln; built per-project): `StoryCADCli` 0 errors, `StoryCADMcp` 0 errors. `StoryCADMcp.Tests` and `HeadlessTest` each show 1 error: `error UNOB0005: Uno.WinUI 6.4.229 vs Uno.Sdk 6.5.153`. Stashed my changes and reproduced the same error with clean `dev` state — confirmed pre-existing environment issue unrelated to #1379. Restored changes; stash dropped.
- macOS `net10.0-desktop` step still pending — user-driven, requires Mac Mini.
- Interactive spot-check skipped (Mac required).

### 2026-04-29 (session 2 — Code phase)
- Code section planned with 11 tasks; user approved. Plan deviations recorded in issue body: no mocks (real IoC), MSBuild-only restore (plan §6 step 3's `dotnet restore` overridden), build/test moved to Test section.
- TDD throughout. Each step landed red-then-green with full build + test verification.
- **Step 1+2** (interface presence): wrote `IStoryCADAPIInterfaceTests.cs` with 14 compile-time presence tests through `IStoryCADAPI`-typed reference. RED = 14 CS1061 errors per target. Added the 13 missing signatures verbatim to `IStoryCADAPI.cs` between `GetStoryElement` and `#region Resource API`. GREEN: all 14 tests pass.
- **Step 2 (collection-entry)**: wrote `StoryCADApiCollectionEntryTests.cs` (initially 11 tests covering success + failure paths per plan §6 step 2 8-point contract). RED. Implemented `AddCollectionEntry` / `UpdateCollectionEntry` / `RemoveCollectionEntry` on `StoryCADApi.cs` with two private helpers (`TryResolveListProperty`, `TryConvertEntry`). Added matching signatures to `IStoryCADAPI.cs`. GREEN: 11 tests pass.
- **Step 2 (pre-check)**: wrote `StoryCADApiUpdatePropertyListPreCheckTests.cs` — 1 test asserting `UpdateElementProperty` on `List<T>` property returns error pointing to all three collection methods. RED. Added the pre-check at `StoryCADAPI.cs:680–686` between the writable check and the value-conversion block. GREEN.
- **Step 3 (Planner removal)**: removed `<PackageReference Include="Microsoft.SemanticKernel.Planners.OpenAI" />` from `StoryCADLib.csproj` and the matching `<PackageVersion>` from `Directory.Packages.props`. Verified no source code uses `Microsoft.SemanticKernel.Planning` or `Microsoft.SemanticKernel.Planners`. MSBuild Restore + Build clean.
- **MCP tools (was Step 2 sub-task)**: deferred to a separate PR. StoryCADAPI repo has uncommitted work on `issue-1246-repo-reorg`; user chose option 1 (cleanest scope separation). Issue body updated with strikethrough + deferral note.
- **Code review**: ran code-reviewer agent (independent of implementer). 0 critical, 5 important, 5 minor. Addressed important findings:
  - **#3 (highest)**: `JsonElement` and `Dictionary<string,object>` conversion paths were untested (the actual MCP scenario). Added 3 new tests: `AddCollectionEntry_FromDictionary_DeserializesAndAppends`, `AddCollectionEntry_FromJsonElement_DeserializesAndAppends`, `UpdateCollectionEntry_FromDictionary_ReplacesEntry`. All pass.
  - **#1**: Reordered `UpdateCollectionEntry` to call `TryConvertEntry` before index check so type errors win. Added `UpdateCollectionEntry_BadTypeAndBadIndex_ReportsTypeError` test.
  - **#2**: `TryConvertEntry` now captures and returns `ex.Message` via an `out string error` parameter; failure messages append the inner exception detail.
  - **#4**: Null entry now rejected with explicit "Entry cannot be null." Added `AddCollectionEntry_NullEntry_ReturnsFailure` and `UpdateCollectionEntry_NullEntry_ReturnsFailure` tests.
- Skipped reviewer items: #5 (DIAG-55 logging fires before pre-check return — minor), all minor (#6–#10 style/test-style nits).
- Final state: 991 tests / 977 passed / 14 skipped / 0 failed. Build clean.

### 2026-04-29 (session 1 — Plan phase)
- Read issue #1379.
- Created branch `issue-1379-complete-storycadapi-interface` from `origin/dev` (local only, not pushed).
- Created devdocs folder `devdocs/issue_1379_storycadapi_interface/`.
- Scaffolded this status log and `issue_1379_plan.md`.
- Investigated MCP failure on StoryWorld list properties (Physical Worlds, Species, Cultures, Governments, Religions). Root cause is in the API: `StoryCADApi.UpdateElementProperty` only handles simple-type conversion and throws on list-typed values. The MCP layer is downstream of the bug. Whole-element replacement (`UpdateStoryElement`) works as a workaround.
- Added scope item to issue #1379 body (new Task 5 + "Scope addition" section). Mirrored in design review §2C and a new D6 decision.
- Rewrote design review in plain language so it matches the wording style used in the issue body.
- Did the audit. Results in design review §3:
  - 51 public members on `StoryCADApi`, 34 on `IStoryCADAPI`.
  - 33 already match.
  - 1 signature mismatch (`UpdateElementProperty` return type).
  - 17 missing from the interface — 13 of them recommended **add**, 3 recommended **don't add (deprecate as duplicates)**: `DeleteStoryElement`, `GetElement`, `SetCurrentModel`. Each duplicates another method already covered by the interface.
  - 1 cosmetic finding: `DeleteElement` is shaped async but does no awaiting.
- User filled in the historical background: the interface was Collaborator-only, Collaborator uses UX pickers (so it never needed `AddElement` or other create/move/search methods), and the gap became visible only after the StoryCADAPI samples, MCP, and prompts website were added as new consumers. Recorded in design review §1.
- That history settles **D1**: full surface, mark duplicates `[Obsolete]`. Open Question 1 in §4 marked resolved.
- Updated issue #1379 body with a "History" section.
- Promoted the `DeleteElement` async-but-not-awaiting finding from "cosmetic" to in-scope per user direction. Added Task 6 to issue body and **D7** to the design review.
- Searched all three repos for callers of the three duplicate methods. Findings recorded in design review §3.4 note 1: `DeleteStoryElement` is StoryCAD-test-only (4 sites); `SetCurrentModel` is used in all three repos (~21 sites total); `GetElement` is used in StoryCAD tests + StoryCADAPI samples/CLI/MCP (~13 sites total).
- Confirmed policy with user: deprecate (mark `[Obsolete]`) rather than delete; migrate all in-house callers to non-deprecated signatures. External callers continue compiling with warnings.
- Added Task 7 to issue body covering the deprecation + migration. Updated **D3** with the migration scope. Sibling PRs needed in Collaborator and StoryCADAPI alongside #1379.
- Removed §4 Open Questions per user direction (questions were either already answered, irrelevant, or makework). Replaced with §4 "Development approach: TDD" — implementation lands red-then-green throughout.
- Simplified §5: D1 (interface scope), D2 (duplicates deprecate+migrate), D3 (Planner straight delete) all settled. Two remaining open: D4 (list-property fix shape) and D5 (`DeleteElement` async fix). Old D5 dropped — TDD makes "what tests to add" not a separate decision.
- D4 settled: list-property fix is three new companion methods on both class and interface — `AddCollectionEntry`, `UpdateCollectionEntry`, `RemoveCollectionEntry`. `UpdateElementProperty` stays scalar-only. Simpler than the JSON-deserialization approach I'd been heading toward.
- D5 settled: drop `DeleteElement`'s async signature. Confirmed `outlineService.MoveToTrash` is synchronous `void`. Migration: ~10 caller sites (6 StoryCAD tests + 4 StoryCADAPI; the MCP wrapper also drops async).
- Migration total now ~48 sites: ~38 from the three duplicate deprecations + ~10 from `DeleteElement` async fix. Spans all three repos.
- All five decisions (D1–D5) settled. Plan ready for sign-off.
- Renamed `issue_1379_design_review.md` → `issue_1379_plan.md`. Header and status updated.
- Filled in §6 Plan with full detail: 13 missing-method signatures, 3 new collection-entry method contracts, all 50 migration sites listed by file:line across 4 sub-steps, Planner removal target, build/test, cross-repo merge ordering.
- Trimmed §3.4 note 1's migration table (data now lives in §6 step 6).
- Rolled back the duplicate-deprecation scope creep. User correctly flagged that deprecating `DeleteStoryElement`, `SetCurrentModel`, `GetElement` (and the ~38 caller migrations that follow) had nothing to do with completing the interface — I'd added it during the audit without asking. Removed from plan §3.3, §3.4, §5 D2, and §6 (steps 4 and 6a/6b/6c). Removed Task 7 from the issue body. Step 6d (drop `await` on `DeleteElement` callers) kept — that's tied to D5, which the user approved separately.
- Ran the plan past three reviewers (architect, code-reviewer, csharp-pro). User reviewed the findings and gave direction:
  - `DeleteElement`/`RestoreFromTrash`/`EmptyTrash` async-but-not-awaiting: not broken, don't fix. **D5 removed.** Plan steps 4 and 5 (drop async + drop await) deleted. Issue Task 6 removed.
  - `UpdateElementProperty` return-type "mismatch": no change. Interface stays `OperationResult<object>` — it can hold whatever the method returns. **Plan Step 3 removed**, §3.2 reworded as observation-only.
  - Add the pre-check on `UpdateElementProperty` to short-circuit list-typed properties with a clear error pointing at the new collection methods. Folded into Step 2.
  - Other reviewer findings (ObservableCollection, other element types, Planner grep specifics, mocks, XML comments): user said no — works now, leave alone.
- All interface changes are now additive; no signature changes. Cross-repo coordination simplified to "no coordination needed."
- Plan steps now: 1) add 13 missing methods, 2) add 3 collection methods + pre-check, 3) remove SK Planner, 4) build/test.
- **Next**: design review of the plan. Then implement, TDD throughout.
