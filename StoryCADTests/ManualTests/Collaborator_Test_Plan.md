# Collaborator Manual Test Plan

Tests for the AI Collaborator feature (CollaboratorLib). Requires a document open in StoryCAD
and a GUID enrolled on the dev Worker's allowlist. Set `COLLAB_DEV_ACTIVATION=1` to activate
through that allowlist instead of the platform store (issue #90 D7/D8); it does not bypass the
purchase gate, which now requires holding a valid activation however obtained.

## Session Setup (Run Once Before Testing)
1. Launch StoryCAD
2. Go to Tools > Preferences > General tab; set Auto-save interval to 30 seconds and enable it
3. Go to Backup tab; enable Automatic backups with a 1-minute interval
4. Click OK
5. Open the sample outline "Danger Calls" (File > Open Sample Outline)
6. Verify `COLLAB_DEV_ACTIVATION=1` is set in your environment and your GUID is approved on the allowlist

## Session Cleanup (After All Tests)
1. Restore original autosave and backup preferences
2. Close StoryCAD

---

## Launch and Window

### TC-110: Open Collaborator Window
**Priority:** Critical
**Time:** ~2 minutes

**Setup:**
- "Danger Calls" outline open
- `COLLAB_DEV_ACTIVATION=1`

**Steps:**
1. Click the Collaborator button in the toolbar (or Tools > Story Collaborator)
   **Expected:** Story Collaborator window opens alongside the main window

2. Verify the workflow navigation shell loads
   **Expected:** Left panel shows list of available workflows (Premise, GMC, etc.)

3. Verify the main StoryCAD window remains responsive
   **Expected:** Can click elements in the navigation pane without the app hanging

**Cleanup:**
- Close Collaborator window

---

### TC-111: No Document Open
**Priority:** High
**Time:** ~1 minute

**Setup:**
- No outline open (close any open document first)

**Steps:**
1. Click the Collaborator button
   **Expected:** Error message appears — "No StoryModel available" or similar; no crash

2. Dismiss the message
   **Expected:** Returns to main window normally

**Cleanup:**
- Reopen "Danger Calls" for subsequent tests

---

## Autosave and Backup Suspension

### TC-112: Autosave Paused While Collaborator Is Open
**Priority:** High
**Time:** ~4 minutes

**Setup:**
- "Danger Calls" open
- Autosave set to 30 seconds (Session Setup)
- Note the file's last-modified timestamp before starting

**Steps:**
1. Open Collaborator
   **Expected:** Story Collaborator window opens

2. Make a visible edit in StoryCAD's main window (e.g., change a character name)
   **Expected:** Changed indicator appears in status bar

3. Wait 90 seconds without closing Collaborator
   **Expected:** File's last-modified timestamp does NOT change during this period

4. Close Collaborator window
   **Expected:** Collaborator closes

5. Wait 60 seconds
   **Expected:** Autosave fires — file's last-modified timestamp updates

**Cleanup:**
- Revert any edits made to "Danger Calls"

---

### TC-113: Backup Timer Paused While Collaborator Is Open
**Priority:** High
**Time:** ~4 minutes

**Setup:**
- "Danger Calls" open
- Timed backup enabled at 1-minute interval (Session Setup)
- Note backup folder contents before starting (typically `%LOCALAPPDATA%\StoryCAD\Backups`)

**Steps:**
1. Open Collaborator
   **Expected:** Story Collaborator window opens

2. Wait 90 seconds
   **Expected:** No new backup file appears in the backup folder

3. Close Collaborator
   **Expected:** Collaborator closes

4. Wait 90 seconds
   **Expected:** New backup file appears in backup folder

**Cleanup:**
- Delete the test backup file created in step 4

---

## Workflow Execution

### TC-114: Run a Workflow — Premise
**Priority:** Critical
**Time:** ~5 minutes

**Setup:**
- "Danger Calls" open; Story Overview node selected
- Collaborator window open

**Steps:**
1. Select "Premise" from the workflow list
   **Expected:** Premise workflow page loads, showing input fields for the story

2. Click Execute (or equivalent run button)
   **Expected:** Progress indicator appears; conversation list shows "Running Premise..."

3. Wait for response (up to 3 minutes)
   **Expected:** AI response appears in conversation list; Accept buttons appear next to output fields

4. Verify no crash or timeout error
   **Expected:** Response text is coherent; progress indicator disappears

**Cleanup:**
- Do not accept changes yet (used in TC-115 and TC-116)

---

### TC-115: Accept All Workflow Changes
**Priority:** Critical
**Time:** ~2 minutes

**Setup:**
- TC-114 completed; workflow output visible with Accept buttons

**Steps:**
1. Click "Accept All" button
   **Expected:** All output fields write to the StoryCAD outline

2. Close Collaborator
   **Expected:** Collaborator closes; StoryCAD reloads the current ViewModel

3. Select Story Overview in the navigation pane
   **Expected:** Fields updated with Collaborator's output (e.g., Premise text now populated)

**Cleanup:**
- Revert any changes if "Danger Calls" should remain unmodified

---

### TC-116: Accept Individual Property
**Priority:** High
**Time:** ~3 minutes

**Setup:**
- Run a workflow (TC-114) to produce output with multiple fields

**Steps:**
1. Click the Accept button next to one specific output field only
   **Expected:** That field writes to the outline; other fields unchanged

2. Click the Accept button for a second field
   **Expected:** Second field writes; first field still shows accepted value

3. Close Collaborator without accepting remaining fields
   **Expected:** Only the two accepted fields are updated in StoryCAD

**Cleanup:**
- Revert changes

---

### TC-117: Reject All / Close Without Accepting
**Priority:** High
**Time:** ~2 minutes

**Setup:**
- Run a workflow to produce output

**Steps:**
1. Note the current values of one or two output fields in StoryCAD before accepting anything

2. Close Collaborator window without clicking any Accept button
   **Expected:** Collaborator closes; ViewModel reloads

3. Verify the fields in StoryCAD are unchanged
   **Expected:** Original values intact — Collaborator made no edits

**Cleanup:**
- None

---

## Error Handling

### TC-118: Proxy Unreachable
**Priority:** Medium
**Time:** ~3 minutes

**Setup:**
- Set environment variable `COLLAB_PROXY_URL=https://invalid.example.com` (temporarily)
- Restart StoryCAD for the variable to take effect

**Steps:**
1. Open Collaborator and select any workflow

2. Click Execute
   **Expected:** Progress indicator appears; after timeout, an error message appears in the conversation list (not a crash)

3. Verify main StoryCAD window is still functional
   **Expected:** Can navigate outline, close Collaborator normally

**Cleanup:**
- Unset `COLLAB_PROXY_URL` and restart StoryCAD

---

### TC-119: Close and Reopen Collaborator in Same Session
**Priority:** Medium
**Time:** ~3 minutes

**Setup:**
- "Danger Calls" open

**Steps:**
1. Open Collaborator
   **Expected:** Collaborator window opens

2. Close Collaborator
   **Expected:** Window closes; autosave/backup resume

3. Open Collaborator again
   **Expected:** Fresh Collaborator window opens; workflow list available

4. Run a workflow
   **Expected:** Executes normally — no stale state from previous session

**Cleanup:**
- Close Collaborator

---

## Credits

### TC-120: Buy Credits Button Opens the Dialog
**Priority:** High
**Time:** ~2 minutes

Wiring landed in issue #90 step 8 (the dialog itself was built in step 10); this case is the manual
click-through that a headless session cannot verify.

**Setup:**
- "Danger Calls" open

**Steps:**
1. Locate the Buy Credits button on the Shell toolbar, next to the Collaborator button
   **Expected:** Visible exactly when the Collaborator button is; tooltip reads "Buy Collaborator Credits"

2. Click the Buy Credits button
   **Expected:** Buy Credits dialog opens (on a sideloaded dev build with no store license context,
   the dialog's pack listing may show its failure state instead of products — the assertion is that
   the button opens the dialog, not that the store answers)

3. Close the dialog without purchasing
   **Expected:** Dialog closes; no purchase initiated; main window responsive

**Cleanup:**
- None

---

## Notes

- **COLLAB_DEBUG=1**: Set this to trigger the debugger-attach dialog on Collaborator open (dev use only).
- **COLLAB_PROXY_URL**: Overrides the default proxy endpoint. Leave unset for production Worker.
- Proxy responses can take up to 3 minutes — do not treat a slow response as a hang before the timeout elapses.
- TC-112 and TC-113 require watching timestamps or log output; NLog writes to `%LOCALAPPDATA%\StoryCAD\logs\` and will show `Collaborator logging initialized` at session start.
