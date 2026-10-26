# Issue #1626 status log

## 2026-10-26: youtube update

**Branch:** `issue-1626-youtube-update`.

**Done:** `youtube update <video id> --meta <meta.json>` replaces an uploaded video's title, description, tags and category (videos.update, part=snippet). Privacy, playlists, captions and thumbnail stay as they are. Sign-in and revoke are the same as `youtube upload`.

**Tests:** YouTube tests 77 passed, 0 failed.

## 2026-10-25: Meet Collaborator video, first recording

**Branch:** `issue-1626-youtube-upload`.

**Done:** `wait <target> condense` (waits up to 5 minutes; `video` cuts the wait to 1.5 s at each end). The video body ends before the app closes. `Meet-Collaborator-Video.scs` passes a full test run and recorded a first take (3:26) with captions. Workaround in the script: after Review Each, the next element picker crashes StoryCAD (0xC000027B), so the script closes and reopens Collaborator first.

**Tests:** StoryCADAutomation tests 311 passed, 0 failed.

**Next:** Terry reviews the first take. The Collaborator window opens smaller than the recorded area; the script should enlarge it. File the Review Each picker crash as a StoryCAD issue.

## 2026-10-23: captions and sign-in per run

**Branch:** `issue-1626-youtube-upload`.

**Done:** `video` writes `<script name>.srt` next to the `.mp4`, from the `narrate` lines and the clip times (video design section 9.1); `narrate` takes an optional `caption "..."` clause for the written form. `youtube upload` signs in through the browser on each run, keeps the access token in memory, and revokes it when the run ends, after Ctrl+C too, with a 15 s limit on the revoke; the video body stops when StoryCAD starts to close (the start of `close`, or of the statement before `expect-exit`), so the desktop no longer shows under the closing card; `wait <target> condense` waits up to 5 minutes and `video` cuts the wait to 1.5 s at each end; `youtube auth` and `token.json` are gone. `.gitignore` lists `client_secret*.json` and `token.json`.

**Tests:** StoryCADAutomation tests 311 passed, 0 failed. `check` on `StoryCADTests/AutomationScripts/`: 0 scripts with errors. No live upload run yet with the new sign-in.

## 2026-10-20

**Done:** `youtube upload` command; `video` takes `--assets` (outlines copied to `{scratch}/outlines`) and `--shared` (logo, intro music and credit for every video); card fades and dissolves; no app flash between adjacent cards; fast typing in the presentation profile; `\n` in `show text`. The intro video script runs and records end to end. `Windowing.ShowFileOpenPicker` logs the exception type and HResult; the file picker failure under an elevated process is #1652.

**Tests:** full suite 2023 tests, 2004 passed, 17 skipped, 2 failed (`CheckDotEnvFile`, `CheckDoppler`: no `.env` in this worktree). Video tests 73 passed.
