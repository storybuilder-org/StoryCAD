# Issue #482 - Copy Characters and Settings Log

**Issue:** [StoryCAD #482 - Copy Characters and Settings](https://github.com/storybuilder-org/StoryCAD/issues/482)
**Log Started:** 2026-01-23
**Status:** Planning
**Branch:** `issue-482-copy-elements`

---

## Context

This issue is a dependency for Issue #782 (StoryWorld). The StoryWorld user documentation references copying worldbuilding elements between series outlines, which requires this feature to exist.

## Copyable Element Types (Decided)

| Type | Copyable | Notes |
|------|----------|-------|
| Character | ✅ Yes | Series recurring characters |
| Setting | ✅ Yes | Series recurring locations |
| StoryWorld | ✅ Yes | Primary use case for series/shared worlds |
| Problem | ✅ Yes | Series often inherit problems - document considerations |
| Notes | ✅ Yes | Research and notes transfer |
| Web | ✅ Yes | Webpage references |
| StoryOverview | ❌ No | Singleton, story-specific |
| Scene | ❌ No | Story-specific plot content |
| Folder | ❌ No | Organizational only |
| Section | ❌ No | Organizational (Narrator view) |
| TrashCan | ❌ No | System |
| Unknown | ❌ No | System |

## Key Technical Challenges

1. **Two Open Outlines**: StoryCAD currently has a 'current StoryModel' design - needs to support reading a second outline
2. **Deep Copy**: Elements may have references to other elements (e.g., Character relationships) - need to handle or document
3. **Singleton Handling**: StoryWorld is singleton - what happens if destination already has one?

---

## Log Entries

### 2026-01-23 - Session 1: Setup

**Participants:** User (Terry), Claude Code

**Context:** Pausing #782 review work to address #482 dependency.

#### Actions Taken

1. Created branch `issue-482-copy-elements`
2. Created `devdocs/issue_482/` folder
3. Moved session status file to `devdocs/issue_482/session_status.md`
4. Updated GitHub issue with PIE checklist format
5. Created this log file

#### Next Steps

- Begin Design phase planning
- Research existing copy/paste or import/export patterns in codebase
- Design UI approach (ElementPicker-style dialog)

---

### 2026-01-23 - Session 1 (continued): Design Research

**Participants:** User (Terry), Claude Code

**Context:** Researching codebase patterns for the copy elements feature.

#### Research Completed (via Explore agents)

1. **StoryModel Management**
   - `AppState.CurrentDocument` holds exactly ONE story (single-file design)
   - `OutlineService.OpenFile(path)` returns a StoryModel with NO side effects on AppState
   - This means we CAN load a second file for copying without affecting the current document

2. **TreeView Patterns**
   - `NarrativeTool.xaml` has dual TreeView layout we can adapt
   - `StoryNodeItem.CopyToNarratorView()` is the copy pattern to adapt
   - Selection tracked via Tag property

3. **Dialog Patterns**
   - `Windowing.ShowContentDialog()` is standard approach
   - `ElementPicker` shows filtered elements by type (flattened list)
   - Page-based content inside ContentDialog

4. **SemanticKernelAPI**
   - Has `CurrentModel` property separate from `AppState.CurrentDocument`
   - Can use `outlineService.OpenFile(path)` directly to load source file

#### UI Design Discussion

**Layout (user-directed):**
```
┌─────────────────────┬───┬─────────────────────┐
│  DESTINATION (TO)   │   │   SOURCE (FROM)     │
│  Current Document   │ ← │   Opened File       │
│                     │ → │                     │
│  [View]             │ ↑ │   [View]            │
│                     │ ↓ │                     │
└─────────────────────┴───┴─────────────────────┘
```

- Left side: Current document (destination) - left-to-right reading order
- Right side: Source file to copy from
- Vertical button bar between:
  - ← Copy selected element from source to destination
  - → Remove element from destination (ONLY elements copied this session - prevents accidental deletion)
  - ↑ Navigate up in selected tree
  - ↓ Navigate down in selected tree

**View Options Discussed:**

| Approach | Pros | Cons |
|----------|------|------|
| Flattened List | Simple, easy filtering, like ElementPicker | Can't see hierarchy, harder to choose placement |
| Full TreeView | See structure, better placement control | Complex population, filters complicate things |

**Hybrid Idea (needs discussion):**
Flattened list but show parent path as context, e.g., "John Smith (Characters > Protagonists)"

**Key Implementation Notes:**
- Track copied elements in session list (for → button safety)
- User can bail (close without saving) at any time
- Filter to copyable types only: Character, Setting, StoryWorld, Problem, Notes, Web

#### Session Resumed - Design Decisions with Jake

**View Type Decision:** Flattened lists filtered by element type (like ElementPicker)

**Button Bar Simplified:**
- ← Copy selected from source to destination
- → Remove (only session-copied elements)
- ↑ Navigate up
- ↓ Navigate down

**NOT included:** Copy all, Add section/folder, Trash/Delete

**Template:** Clone NarrativeTool for layout, use ListViews instead of TreeViews

#### Design Plan Created

Full design documented in `devdocs/issue_482/design_plan.md`

Key decisions:
- Dual ListView layout with vertical button bar
- ComboBox filter for element type
- Session tracking via HashSet<Guid> for safe removal
- Cross-references cleared on copy (Guid.Empty)
- StoryWorld singleton enforced (block if exists)

---

*Log maintained by Claude Code sessions*
