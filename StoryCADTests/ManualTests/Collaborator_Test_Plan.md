# Collaborator Manual Test Plan

Tests for the AI Collaborator feature (CollaboratorLib).

**Scope:** StoryCAD ↔ Collaborator ↔ proxy interactions — open/close, gates, autosave/backup pause, run a workflow without hang/timeout, accept/reject, basic proxy error, credits dialog entry. Requires a document open in StoryCAD and a GUID enrolled on the dev Worker's allowlist.

**Out of scope:** Prompt quality, workflow content debugging, Try Again behavior, element-picker memory across runs.

Set `COLLAB_DEV_ACTIVATION=1` to activate through the allowlist instead of the platform store (issue #90 D7/D8). It is a routing hint only; the Worker's allowlist row grants access.

## Environment variables (one method)

Use **Windows user environment variables** and launch StoryCAD from the **Start menu** or **File Explorer** so a new process loads them. Do not rely on PowerShell, Visual Studio F5, or an already-open StoryCAD process (those often keep an old environment block).

### Set or change a variable

1. Fully quit StoryCAD (and Visual Studio if it was used only as a launcher).
2. Open Start, search for **Edit environment variables for your account**, open it.
3. Under **User variables**, New or Edit:
   - Name: `COLLAB_DEV_ACTIVATION` — Value: `1` (required for these tests)
   - Name: `COLLAB_PROXY_URL` — Value: dev Worker base, e.g. `https://storycad-collaborator-proxy.storybuilder-foundation.workers.dev/v1` (optional; omit to use the build default)
4. OK out of all dialogs.
5. Start StoryCAD from **Start** or **File Explorer** (sideloaded/MSIX install, or a deployed build path). Do not use a terminal or VS instance that was open before step 3.

### Temporary change (e.g. TC-118)

1. Quit StoryCAD completely.
2. Edit the user variable in the same dialog as above.
3. Start StoryCAD again from Start or Explorer.
4. After the case, quit StoryCAD, restore the previous value (or delete the variable), start StoryCAD again the same way.

### Verify before testing

- `COLLAB_DEV_ACTIVATION` is `1` (user variable).
- Your `StoreUserGuid` is **approved** on the dev Worker allowlist (first Collaborator open with the flag set may self-enroll as `pending`; approve with category `developer` or `beta_tester` per the workflow guide). If Subscribe appears instead of Collaborator, the process did not see the flag or the GUID is not approved.

## Session Setup (Run Once Before Testing)

1. Set environment variables per **Environment variables** above; start StoryCAD from Start or Explorer.
2. Tools > Preferences > Backup tab: enable autosave every **30** seconds; enable timed backups every **1** minute. Note the **Backup directory** path shown under Save Locations (that is where TC-113 looks — not a fixed LocalAppData path).
3. Save preferences.
4. Open the sample outline **Danger Calls** (File > Open Sample Outline).
5. Confirm Collaborator opens without the Subscribe dialog (allowlist activation worked).

## Session Cleanup (After All Tests)

1. Close Collaborator if open (window X or Exit).
2. Restore original autosave/backup preferences if you changed them.
3. Restore `COLLAB_PROXY_URL` if TC-118 changed it; start StoryCAD once from Start/Explorer to confirm.
4. Close StoryCAD.

## Collaborator session cleanup (between cases)

When a case says reset Collaborator or discard AI work:

1. Do **not** Accept / Accept All unless the case requires it.
2. **Close the Collaborator window** (ends the session; next open is a new instance).
3. If the outline must be clean: close the outline and reopen **Danger Calls** (or revert edits).
4. Open Collaborator again and re-select the workflow if the next case needs a run.

Closing only the outline without closing Collaborator does not fully reset Collaborator UI state.

---

## Launch and Window

### TC-110: Open Collaborator Window
**Priority:** Critical  
**Time:** ~2 minutes

**Setup:**
- Session Setup done; "Danger Calls" open

**Steps:**
1. Click the Collaborator button in the toolbar (or Tools > Story Collaborator)  
   **Expected:** Story Collaborator window opens alongside the main window
2. Verify the workflow navigation shell loads  
   **Expected:** Left panel lists workflows (Ideation/Premise, GMC, etc.)
3. Verify the main StoryCAD window remains responsive  
   **Expected:** Can click elements in the navigation pane without the app hanging

**Cleanup:**
1. Close the Collaborator window (X).
2. Leave "Danger Calls" open for the next case.

---

### TC-111: No Document Open
**Priority:** High  
**Time:** ~1 minute

**Setup:**
- Close any open outline (File > Close or equivalent) so no story is loaded

**Steps:**
1. Click the Collaborator button  
   **Expected:** Collaborator does **not** open. Status bar shows a warning such as "No story is open. Open an outline before using Collaborator." No crash. (Close leaves an empty document in AppState; the gate uses empty `CurrentView`, not a null model.)
2. Confirm the main window still works  
   **Expected:** Can open a file or sample

**Cleanup:**
1. Open sample **Danger Calls** again.
2. Collaborator stays closed until the next case opens it.

---

## Autosave and Backup Suspension

### TC-112: Autosave Paused While Collaborator Is Open
**Priority:** High  
**Time:** ~4 minutes

**Setup:**
- "Danger Calls" open; autosave 30 seconds (Session Setup)
- Note the outline file's last-modified time (sample is often under `%TEMP%\Danger Calls.stbx`)

**Steps:**
1. Open Collaborator  
   **Expected:** Window opens
2. Make a visible edit in StoryCAD's main window (e.g. change a character name)  
   **Expected:** Changed indicator in the status bar
3. Wait 90 seconds without closing Collaborator  
   **Expected:** File last-modified time does **not** change
4. Close Collaborator  
   **Expected:** Window closes
5. Wait 60 seconds  
   **Expected:** Autosave updates the file last-modified time

**Cleanup:**
1. Close Collaborator if still open.
2. Undo or re-open **Danger Calls** so the sample is not left dirty for later cases.

---

### TC-113: Backup Timer Paused While Collaborator Is Open
**Priority:** High  
**Time:** ~4 minutes

**Setup:**
- "Danger Calls" open; timed backup every 1 minute (Session Setup)
- Note contents of the **Backup directory** from Preferences > Save Locations (e.g. a Google Drive or Documents folder). Sort by date modified.

**Steps:**
1. Open Collaborator  
   **Expected:** Window opens
2. Wait 90 seconds  
   **Expected:** No **new** `Danger Calls as of ….zip` in that backup directory
3. Close Collaborator  
   **Expected:** Window closes
4. Wait 90 seconds  
   **Expected:** A new backup zip appears in that directory

**Cleanup:**
1. Close Collaborator if still open.
2. Optionally delete the zip created in step 4 from the backup directory.

---

## Workflow Execution

### TC-114: Run a Workflow
**Priority:** Critical  
**Time:** ~5 minutes

**Scope note:** Assert run completes and UI stays usable. Do not score premise quality or prompt wording.

**Setup:**
- "Danger Calls" open; Story Overview selected
- Collaborator open

**Steps:**
1. Select a short workflow from the list (e.g. Ideation / Premise)  
   **Expected:** Workflow page loads; element gathering can complete
2. Run / Execute when required  
   **Expected:** Progress or "Running…" in the conversation list
3. Wait for completion (up to 3 minutes)  
   **Expected:** Response appears; Accept controls available if the workflow produces property updates; no crash or hang treated as timeout before 3 minutes
4. Leave Accept unused  
   **Expected:** Pending updates not applied yet

**Cleanup:**
1. Do **not** Accept All (TC-115/116 may use output, or discard below).
2. If continuing to TC-115 with this output, leave Collaborator open.
3. If stopping here: close Collaborator without Accept; reopen sample if the outline must stay clean.

---

### TC-115: Accept All Workflow Changes
**Priority:** Critical  
**Time:** ~2 minutes

**Setup:**
- TC-114 completed with Accept controls visible

**Steps:**
1. Click **Accept All**  
   **Expected:** Output fields write to the outline
2. Close Collaborator  
   **Expected:** Window closes; StoryCAD reloads the current view from the model
3. Select Story Overview (or the element that was updated)  
   **Expected:** Fields show Collaborator's applied text

**Cleanup:**
1. Collaborator already closed in step 2.
2. Close outline and reopen **Danger Calls** (or revert) so later cases start from a clean sample.

---

### TC-116: Accept Individual Property
**Priority:** High  
**Time:** ~3 minutes

**Setup:**
- Run a workflow (as in TC-114) so multiple Accept controls appear

**Steps:**
1. Accept **one** field only  
   **Expected:** That field applies; others remain pending
2. Accept a **second** field  
   **Expected:** Second applies; first remains applied
3. Close Collaborator without accepting the rest  
   **Expected:** Only the accepted fields are updated in StoryCAD

**Cleanup:**
1. Collaborator closed in step 3.
2. Close outline and reopen **Danger Calls** (or revert) for a clean sample.

---

### TC-117: Close Without Accepting
**Priority:** High  
**Time:** ~2 minutes

**Setup:**
- Run a workflow to produce output; note one or two StoryCAD field values first

**Steps:**
1. Close Collaborator without any Accept  
   **Expected:** Window closes
2. Check those fields in StoryCAD  
   **Expected:** Unchanged — no edits applied

**Cleanup:**
1. Collaborator already closed.
2. No outline revert required if nothing was accepted.

---

## Error Handling

### TC-118: Proxy Unreachable
**Priority:** Medium  
**Time:** ~3 minutes

**Setup:**
1. Quit StoryCAD completely.
2. Set user env `COLLAB_PROXY_URL` = `https://invalid.example.com` (see **Environment variables**).
3. Start StoryCAD from Start or Explorer; open **Danger Calls**.
4. Keep `COLLAB_DEV_ACTIVATION=1`.

**Steps:**
1. Open Collaborator and start any workflow run  
   **Expected:** After failure/timeout, conversation list shows an error (not a crash). Host name `invalid.example.com` may appear in the message.
2. Use the main StoryCAD window  
   **Expected:** Outline still navigable; Collaborator can be closed normally

**Cleanup:**
1. Close Collaborator.
2. Quit StoryCAD.
3. Restore `COLLAB_PROXY_URL` to the dev Worker URL (or remove the variable).
4. Start StoryCAD from Start or Explorer and confirm a normal workflow can run again.

---

### TC-119: Close and Reopen Collaborator in Same Session
**Priority:** Medium  
**Time:** ~3 minutes

**Setup:**
- "Danger Calls" open; `COLLAB_PROXY_URL` restored if TC-118 ran

**Steps:**
1. Open Collaborator  
   **Expected:** Window opens
2. Close Collaborator  
   **Expected:** Window closes; autosave/backup may resume
3. Open Collaborator again  
   **Expected:** Fresh window; workflow list available
4. Run a short workflow  
   **Expected:** Completes without hang (no need to Accept)

**Cleanup:**
1. Close Collaborator without Accept (unless you want applied edits).
2. Reopen sample only if the outline was dirtied.

---

## Credits

### TC-120: Buy Credits Button Opens the Dialog
**Priority:** High  
**Time:** ~2 minutes

**Setup:**
- "Danger Calls" open
- Buy Credits toolbar button visible next to Collaborator (same visibility as Collaborator)

**Steps:**
1. Confirm the button is enabled (not stuck gray after a save/open). Tooltip: "Buy Collaborator Credits"  
   **Expected:** Clickable when the command bar is idle
2. Click **Buy Credits**  
   **Expected:** Buy Credits dialog opens. On a sideloaded/dev build, pack list may be empty or show a store error — assertion is **dialog opens**, not a successful purchase
3. Click **Not now** (or equivalent)  
   **Expected:** Dialog closes; no purchase; main window responsive

**Cleanup:**
- None (dialog already closed)

---

## Notes

- **COLLAB_DEBUG=1**: Debugger-attach dialog on Collaborator open (dev only); set via the same user-env method if needed.
- Proxy responses can take up to 3 minutes — do not treat a slow response as a hang before that.
- Logs: under the app's `RootDirectory` logs folder (package or unpackaged); useful if open/activation fails.
- Manual pass for issue #90 Test (lane A) is this suite TC-110–TC-120, not prompt review.
