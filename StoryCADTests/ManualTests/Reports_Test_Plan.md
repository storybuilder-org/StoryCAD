# Reports Test Plan
**Time**: ~15 minutes
**Purpose**: Verify Print, PDF export, and Scrivener export functionality
**Priority**: Tier 2

## Platform Notes

| Feature | Windows | macOS |
|---------|---------|-------|
| Print to physical printer | Yes (Windows Print Manager) | No — use PDF export instead |
| PDF export | Yes (SkiaSharp) | Yes (SkiaSharp) — **primary output method** |
| Scrivener export | Yes | Yes |
| File save picker | Windows native | macOS native |

**macOS-specific concerns**:
- Print to physical printer is Windows-only. The Print button may not appear or should fall back to PDF export on macOS.
- PDF export uses SkiaSharp on both platforms — verify font rendering and page layout on macOS.
- Scrivener .scriv project format should work cross-platform.

## Setup
1. Launch StoryCAD
2. Open sample outline "Danger Calls" (File > Open Sample Outline) or use an outline with multiple element types
3. Save As to a test location if needed

---

### RP-001: Open Print Reports Dialog
**Priority:** Critical
**Time:** ~1 minute

**Steps:**
1. Click Tools > Print Reports (or File > Print Reports if in File menu)
   **Expected:** Print Reports dialog opens

2. Verify report options are displayed
   **Expected:** Checkboxes/toggles for: Story Overview, Story World, Story Synopsis, Story Problem Structure, and lists (Problems, Characters, Settings, Scenes, Websites)

3. Verify individual element selection lists are available
   **Expected:** Can select specific Problems, Characters, Settings, Scenes for detailed reports

**Pass/Fail:** ______

---

### RP-002: PDF Export — Summary Reports
**Priority:** Critical
**Time:** ~3 minutes

**Steps:**
1. In the Print Reports dialog, check "Story Overview" and "List of Characters"
   **Expected:** Options selected

2. Select PDF export mode (if a mode selector exists) or click Export/Print
   **Expected:** File save picker opens

3. Choose a location and filename (e.g., "TestReport.pdf")
   **Expected:** PDF file created

4. Open the PDF in a viewer
   **Expected:** Contains Story Overview section and a list of all characters; text is readable; pages are US Letter size

**Pass/Fail:** ______

---

### RP-003: PDF Export — Individual Element Reports
**Priority:** High
**Time:** ~2 minutes

**Steps:**
1. In the dialog, select 1-2 specific Characters and 1 Problem for detailed reports
   **Expected:** Elements selected

2. Export to PDF
   **Expected:** PDF created

3. Verify PDF contains detailed information for selected elements only
   **Expected:** Character details (role, physical, social, etc.) and Problem details present; unselected elements not included

**Pass/Fail:** ______

---

### RP-004: PDF Export — All Reports
**Priority:** Medium
**Time:** ~2 minutes

**Steps:**
1. Check all report options and select all individual elements
   **Expected:** Everything selected

2. Export to PDF
   **Expected:** PDF created (may take a moment for large outlines)

3. Verify PDF is complete and paginated correctly
   **Expected:** Multiple pages; page breaks between sections; no overlapping text; no truncated content

**Pass/Fail:** ______

---

### RP-005: Print to Printer (Windows Only)
**Priority:** Medium
**Time:** ~2 minutes

**Steps:**
1. Select a few report options
   **Expected:** Options selected

2. Click Print (not PDF export)
   **Expected:** Windows Print Manager dialog opens

3. Select a printer (or "Microsoft Print to PDF")
   **Expected:** Print dialog shows printer options

4. Print the report
   **Expected:** Report prints correctly; page breaks are clean

**macOS:** Skip this test — printing is Windows-only. Verify that attempting to print on macOS either falls back to PDF export or shows an appropriate message.

**Pass/Fail:** ______

---

### RP-006: Scrivener Export
**Priority:** Medium
**Time:** ~3 minutes

**Steps:**
1. Click Tools > Scrivener Reports (or the Scrivener option in Print Reports dialog)
   **Expected:** File picker opens for selecting a .scriv project file

2. Select or create a target .scriv project
   **Expected:** Export begins

3. Verify export completes without errors
   **Expected:** Success message or status update

4. Open the .scriv project in Scrivener (if available)
   **Expected:** StoryCAD folder exists in the Binder with ExplorerView, NarratorView, and Miscellaneous subfolders

5. Verify document content matches StoryCAD outline
   **Expected:** Characters, Problems, Scenes, Settings have corresponding Scrivener documents with RTF content

**Pass/Fail:** ______

---

### RP-007: Empty Outline Report
**Priority:** Low
**Time:** ~1 minute

**Steps:**
1. Create a new story with only a Story Overview (no other elements)
   **Expected:** Outline created

2. Open Print Reports and try to export a PDF
   **Expected:** PDF generated with just the Story Overview; no errors from empty lists

**Pass/Fail:** ______

---

### RP-008: Print Button Visibility by Platform
**Priority:** High
**Time:** ~1 minute
**Platform:** Both

**Steps:**
1. Open the Print Reports dialog
   **Expected:** Dialog opens

2. **Windows**: Verify a "Print" button is visible alongside PDF export
   **Expected:** Print button present (uses Windows Print Manager via `#if WINDOWS10_0_18362_0_OR_GREATER`)

3. **macOS**: Verify the Print button is absent or replaced by PDF export only
   **Expected:** No Print button; only PDF export available (compile-time conditional excludes Windows print path)

**Pass/Fail:** ______

---

## REPORTS RESULT: _____ / 8 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________ **Platform**: _________
