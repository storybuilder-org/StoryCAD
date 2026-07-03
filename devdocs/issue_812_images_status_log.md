# Issue 812: Image Support for Story Elements (PR #1435)

## Current Status
- **Phase**: CLOSED. PR #1435 merged to `dev` 2026-07-03 (merge commit 588d95ba); issue #812 closed; `visuals` branch deleted locally and remotely
- **Date**: 2026-07-03
- **Branch**: was `visuals` (PR #1435, base `dev`); deleted after merge
- **Owner**: Jake (handed off 2026-07-01)
- **Handoff comment**: https://github.com/storybuilder-org/StoryCAD/pull/1435#issuecomment-4859975663

## Remaining Scope (all under this issue/PR, per Terry 2026-07-01)
1. Fix finding 4: `ImageService.ToImageSourceAsync`/`BuildImageSourceAsync` (`ImageService.cs:252-320`) decode & resize synchronously on the UI thread; move the CPU-bound SkiaSharp work to `Task.Run`.
2. Legacy-file test: deserialize a `.stbx` JSON with no `Images` key, assert empty list not null (test doc comments claim this scenario; no test covers it).
3. OutlineService methods: `AddImage`/`RemoveImage`/`UpdateImageCaption` modeled on `AddCastMember` (`OutlineService.cs:501-535`), with tests to the #1069 100%-coverage standard.
4. Design decision gating item 3: gallery is snapshot-based (`ElementImageGallery.Load()`/`ToModelList()` at LoadModel/SaveModel) vs `AddCastMember`-style immediate model mutation. Pick live mutation or keep `ISaveable` for UI + OutlineService for API/validation.
5. API tests: round-trip a Base64 image through `AddCollectionEntry` → `GetElement` → `RemoveCollectionEntry` (`StoryCADAPI.cs:767-827`). Generic path already reaches `Images`; dedicated API methods optional.

## Session Log

### 2026-07-03
- Reviewed Jake's 4 commits since handoff (b345791f, 73277987, 9037871f, b9b524d5; 1,016 insertions across 16 files) against the 5 remaining-scope items. All 5 done:
  1. Finding 4 fixed: `PickImageWithThumbnailAsync` and `ToImageSourceAsync` now wrap decode/re-encode/resize in `Task.Run`; only the `WriteableBitmap` build stays on the calling thread. `ResizeToBgra` returns `bgra.Bytes` (a managed copy) before the `SKBitmap` disposes, so no use-after-dispose crossing threads.
  2. Legacy-file tests added: `SerializeWithoutImagesKey` strips the `Images` key from real serialized JSON; missing-key deserialization asserted empty-not-null for `CharacterModel` and `FolderModel` (Notes).
  3. `OutlineService.AddImage`/`RemoveImage`/`UpdateImageCaption` added (`OutlineService.cs:537-686`), modeled on `AddCastMember`: null/empty-Guid/empty-data validation, `InvalidOperationException` for unsupported element types, logging. 22 new OutlineService tests cover all 4 supported element types, 2 unsupported, and every validation path.
  4. Design decision resolved as the split option: UI keeps the snapshot gallery (`ISaveable`), OutlineService methods are the validated API entry point. Documented in the `AddImage` doc comment; `ImageGalleryControl.xaml.cs` untouched.
  5. API coverage: generic-path Base64 round trip (`AddCollectionEntry` → `GetElement` → `RemoveCollectionEntry`) in `StoryCADApiCollectionEntryTests`, plus dedicated `AddImage`/`RemoveImage`/`UpdateImageCaption` on `IStoryCADAPI`/`StoryCADApi` with 13 tests in new `StoryCADApiImageTests.cs` and 3 interface-presence tests.
- Full suite: 1,118 tests, 1,099 passed, 19 skipped (pre-existing), 0 failed, 9.5s.
- CI green on all PR checks: macOS arm64, Windows arm64/x64/x86, python-wheels.
- User manual updated in the same commits: new `docs/Story Elements/Images_Tab.md`, plus Character/Scene/Setting/Notes/Print Reports pages reference it.
- Non-blocking notes: missing-`Images`-key JSON test covers 2 of the 4 declaring models (Character, Folder/Notes; Setting and Scene rely on the 0827a5a7 constructor tests plus the identical System.Text.Json mechanism); `AddImage` sets `model.Changed = true` where `AddCastMember` never has (the new behavior is the correct one); the three new API methods should eventually appear on the api.storybuilder.org docs site (post-merge item). The 2026-07-01 gaps not promoted to remaining scope (non-Setting VM wiring tests, `GatherAppendixImages` Setting/Scene branches) stay open as known gaps.
- Verdict: ready to merge.
- Spin-off: filed StoryCAD #1436 (bug) — `AddCastMember`/`AddRelationship` don't set `model.Changed` at either the OutlineService or API layer, so `AutoSaveService.cs:117` skips the save; fix targets `main`, not `dev`, per Terry.
- Terry ran a manual test (verified the Images tab appears on Character, Setting, Scene, and Notes only) and approved the merge.
- Merged PR #1435 to `dev` (merge commit 588d95ba, 2,775 insertions / 16 deletions across 45 files). Issue #812 closed via the PR link. `visuals` deleted locally and on origin.
- Known open gaps carried out of this issue (non-blocking, documented 2026-07-01): non-Setting VM wiring tests, `GatherAppendixImages` Setting/Scene branches, missing-`Images`-key JSON test for Setting/Scene models.

### 2026-07-01
- Re-reviewed PR #1435 at commit 0827a5a7 ("Clean up Image impl, fix issues in PR") using two Sonnet 5 agents.
- Verdicts on the 10 prior review findings: 9 fixed, finding 4 (UI-thread decode) open. No new defects in the cleanup commit; `.stbx` backward compatibility and `_changeable` dirty-flag gating both verified intact through the `ElementViewModelBase` refactor.
- Architecture research answers:
  - OutlineService: bypassed entirely; image add/remove is direct collection mutation in `ImageGalleryControl.xaml.cs:41-74`.
  - API: reachable via generic `AddCollectionEntry`/`RemoveCollectionEntry`/`GetElement` reflection path, but zero tests exercise `Images` through it.
  - Tests: 9 new files, naming convention compliant; gaps in legacy-file deserialization, non-Setting VM wiring, `GatherAppendixImages` Setting/Scene branches, API path.
- Sizing: items 3-5 are ~100-150 lines of service code plus similar tests; rework risk limited to the two mutation call sites in `ImageGalleryControl.xaml.cs` and (if live mutation wins) `ElementImageGallery.Load`/`ToModelList`.
- Terry decided all remaining work stays under this issue/PR rather than a follow-up issue; posted handoff comment on PR #1435.
- Wiki pages consulted: `wiki/checklists/code-test/pr-review.md`, `wiki/repos/StoryCAD/concepts/patterns-overview.md`, `wiki/repos/StoryCAD/architecture/relationships-and-cast.md`, `wiki/repos/StoryCAD/entities/StoryCADApi.md`.
