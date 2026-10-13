# Issue 1421 status log

Session recovery for #1421 (UI automation DSL and runner). The issue body is the checklist; this file holds build state, test counts and progress only.

## 2026-10-04

- Restarted under the milestone plan in the #1421 milestone 1 scope comment.
- Merged `dev` into the branch (0 behind). Removed `devdocs/issue_1421_handoff.md`.
- Baseline before changes: full solution build, both targets. MSTest WinAppSDK 1745 passed, 15 skipped; desktop 1744 passed, 12 skipped. AutomationConventionTests 7/7.
- Done: review B1, M1, M5; `STORYCAD_ROOT_DIR` replaces the build copy; minimal runner; `StatusMessageText` AutomationId; smoke script.
- Smoke script: three consecutive passes on Brigid (28.3 s, 28.2 s, 27.8 s). Milestone 1 gate met.
- Tests after changes: WinAppSDK 1755 passed, 15 skipped (one earlier full run had 2 Scrivener report failures that passed on rerun); desktop 1746 passed, 12 skipped; AutomationConventionTests 7/7.
- Next: milestone 2 (CI on PRs; one manual screenshot from a script).

How to run locally: build Debug x64, move `StoryCAD/bin/x64/Debug/net10.0-windows10.0.22621/win-x64/.env` aside, run `StoryCADAutomation\bin\x64\Debug\net10.0-windows10.0.22621\StoryCADAutomation.exe run StoryCADTests\ManualTests\Smoke_Test.scs`, then restore `.env`.

## 2026-10-04 (later)

- Review follow-ups committed (`900ff0e9`, `cc82dad0`). Smoke script passes at `cc82dad0` (28.2 s, exit 0). WinAppSDK 1757 passed, 15 skipped; desktop 1746 passed, 12 skipped.
- Added `StoryCADTests/AutomationScripts/SaveDialog.scs`: PDF export through the native Save dialog. Passes (21.8 s).
- Manual check after any change to driver teardown: run a script that fails while the Open dialog is up (launch, `click OpenFromFileNavItem`, then any failing `expect`). Pass when the output shows `teardown: Cancelled a file dialog left open.` and no `PickerHost` process remains.

## 2026-10-04 (Milestone 2 started)

- Added `check`, `--ci`, `--out` and the `screenshot` verb; CI job `ui-smoke` for PRs (not run yet; first run comes with the PR).
- WinAppSDK tests 1763 passed, 15 skipped. `check StoryCADTests`: 2 scripts, 0 errors. `Smoke_Test.scs` and `SaveDialog.scs` pass.
- Next: the manual screenshot script once D-SCREENSHOT is answered on StoryCADWiki #7.

## 2026-10-05 (Milestone 4)

- PR #1604 merged (`982cd909`): `Intro-Video.scs`, tree rows match without trailing spaces.
- Draft PR #1605: presentation profile (`--profile presentation`, `--pacing`, `--window`, `.srt` output) and two driver fixes (root-row click point, `menu` real click).
- Test profile: `Smoke_Test.scs` (35.0 s), `Intro-Video.scs` (69.6 s), `SaveDialog.scs` (22.1 s), `File-Open-Dialog.scs` (8.3 s) pass. Presentation: `Intro-Video.scs` passed four runs, 227.4 s to 232.6 s. Automation unit tests 26 passed. Full suite not run locally; CI runs it.
- Open: OBS recording check against the `.srt`; Milestone 3 manual text and close-out.

## 2026-10-13 (Milestone 5, item 2)

- PR #1608 merged into `dev` 2026-10-08 (`36b9608e`): the design and item 1. Item 2 is on branch `issue-1421-video-m5-step-timing`, cut from `dev` at `02afce24`.
- Item 2, step timing (design section 7): narrate no longer stops the run inside a step; clips start one after the other at the step start; the step waits for its narration, then its hold. A missing clip uses the reading time. The clip-length source returns nothing until item 3 (`voice`).
- Automation unit tests: 74 passed (12 new timing tests; independent review found no defect against section 7). Not run live on Brigid in this item.
- Item 3, the `voice` command (section 8.1): one 24 kHz WAV clip per narrate text in `<out>/voice/`, named from the voice and the text; `run` in the presentation profile reads clip lengths from there when `STORYCAD_VOICE_ID` is set. Automation unit tests: 87 passed (fake ElevenLabs). No live ElevenLabs call yet: `ELEVENLABS_API_KEY` and `STORYCAD_VOICE_ID` are not set on Brigid.
- Next: item 4, the `video` command and assembly. ffmpeg and ffprobe are not on the PATH on Brigid.

## 2026-10-13 (Milestone 5, item 4)

- PR #1616 (items 2 and 3) merged into `dev` as `01cf76a3`. Item 4 is on branch `issue-1421-video-m5-video`, cut from `dev` at `04c46971`.
- Item 4, `video <script> --out <dir> --assets <dir>`: the window is sized so StoryCAD has a 1920x1080 layout at the display scale (2880x1620 at 150%), ffmpeg records it, and assembly scales it to 1920x1080 with the title card, show cards, clips and closing card. ffmpeg comes from `STORYCAD_FFMPEG` (design section 10 changed; Terry approved the scale method 2026-10-13).
- Live run on Brigid at 150%: a four-step test script passed, `.mp4` 31.8 s, 1920x1080. Text in the app frames is sharp. Live finding fixed: moving the window gave the foreground to Explorer; the driver brings StoryCAD back to the front.
- Automation unit tests: 98 passed, including one that assembles a real `.mp4` when `STORYCAD_FFMPEG` is set.
- Next: item 5, remove the `.srt` output and the OBS text.
