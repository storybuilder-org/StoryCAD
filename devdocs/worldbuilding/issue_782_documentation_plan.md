# Issue #782: StoryWorld Documentation Plan

**Issue:** [StoryCAD #782 - Support for Worldbuilding](https://github.com/storybuilder-org/StoryCAD/issues/782)
**Created:** 2026-01-21
**Status:** Planning
**Parent Plan:** `issue_782_storyworld_plan.md` (code complete)

---

## Overview

User manual documentation for the StoryWorld feature. Two parallel tracks:
- **Track A**: Reference documentation (how to use the forms)
- **Track B**: Educational content (worldbuilding craft guide)

**Context Management Strategy:**
- Intermediate summary artifacts after research phases
- Templates before batch work
- Status log updates at checkpoints
- Focused sessions with clear deliverables

---

## TRACK A: Reference Documentation

### A1. Sample Data / sign-off

- [x] Plan this section
- [x] Human approves plan
- [x] Research public domain worldbuilding examples (Tolkien? Oz? Other?)
- [x] Select example with sufficient detail for all 9 tabs
- [x] **CHECKPOINT**: Document selected example in status log
- [ ] Human final approval

**Selected: Land of Oz (L. Frank Baum)** - Clear public domain, covers all 9 tabs, excellent wiki resources. See agent output for detailed content outline per tab.

- [x] Human final approval

### A2. Screenshots Preparation / sign-off

- [x] Plan this section
- [x] Human approves plan
- [x] Create manual script/checklist for populating StoryWorld with sample data
- [x] **CHECKPOINT**: Script reviewed and ready → `screenshot_data_script.md`
- [x] Human final approval

### A3. Screenshot Capture / sign-off

*Depends on: A1 (sample data), A2 (script)*

- [x] Plan this section
- [x] Human approves plan
- [x] Populate StoryWorld with sample data using script
- [x] Capture Structure tab screenshot
- [x] Capture Physical Worlds tab screenshot
- [x] Capture People/Species tab screenshot
- [x] Capture Cultures tab screenshot
- [x] Capture Governments tab screenshot
- [x] Capture Religions tab screenshot
- [x] Capture History tab screenshot
- [x] Capture Economy tab screenshot
- [x] Capture Magic/Technology tab screenshot
- [x] Capture Generate Reports dialog screenshot (with Story World checked)
- [x] **CHECKPOINT**: All screenshots in `/docs/media/`, named consistently
- [x] Human final approval

### A4. Reference Doc Template / sign-off

- [x] Plan this section
- [x] Human approves plan
- [x] Review existing tab documentation patterns (e.g., `Setting_Tab.md`, `Role_Tab.md`)
- [x] Update screenshot script with filenames (10 screenshots defined)
- [x] Create `StoryWorld_Tab_Template.md` with standard structure
- [x] **CHECKPOINT**: Template approved, ready for batch use
- [x] Human final approval

**Deliverables:**
- Screenshot filenames added to `screenshot_data_script.md`
- Three templates in `StoryWorld_Tab_Template.md`: Main Form, Single-Entry Tab, List-Based Tab
- File naming convention, nav_order suggestions, screenshot placement guidelines

### A5. Story Elements - Main Form Doc / sign-off

*Depends on: A3 (screenshots), A4 (template)*

- [x] Plan this section
- [x] Human approves plan
- [x] Create `StoryWorld_Form.md` (main overview)
- [x] **CHECKPOINT**: Form doc complete
- [x] Human final approval

### A6. Story Elements - Single-Entry Tabs / sign-off

*Batch: Structure, History, Economy, Magic/Technology (similar structure)*

- [x] Plan this section
- [x] Human approves plan
- [x] Create `StoryWorld_Structure_Tab.md`
- [x] Create `StoryWorld_History_Tab.md`
- [x] Create `StoryWorld_Economy_Tab.md`
- [x] Create `StoryWorld_Magic_Technology_Tab.md`
- [x] **CHECKPOINT**: 4 single-entry tab docs complete
- [x] Human final approval

### A7. Story Elements - List-Based Tabs / sign-off

*Batch: Physical Worlds, People/Species, Cultures, Governments, Religions (similar structure)*

- [x] Plan this section
- [x] Human approves plan
- [x] Create `StoryWorld_Physical_Worlds_Tab.md`
- [x] Create `StoryWorld_Species_Tab.md`
- [x] Create `StoryWorld_Cultures_Tab.md`
- [x] Create `StoryWorld_Governments_Tab.md`
- [x] Create `StoryWorld_Religions_Tab.md`
- [x] **CHECKPOINT**: 5 list-based tab docs complete
- [x] Human final approval

### A8. Reports Section Updates / sign-off

*Depends on: A3 (screenshots)*

- [x] Plan this section
- [x] Human approves plan
- [x] Update `Print_Reports.md` to mention Story World checkbox
- [x] Update `Scrivener_Reports.md` to mention Story World export
- [x] **CHECKPOINT**: Reports docs updated
- [x] Human final approval

---

## TRACK B: Educational Content

### B1. Citation Research - Batch 1 / sign-off

*Citations 1-4*

- [x] Plan this section
- [x] Human approves plan
- [x] Read: Dabblewriter worldbuilding guide
- [x] Read: Madeline James Writes guide
- [x] Read: Wikipedia - Worldbuilding
- [x] Read: Campfire worldbuilding tools
- [x] Extract key points to `worldbuilding_research_notes.md`
- [x] **CHECKPOINT**: Batch 1 notes complete
- [ ] Human final approval

**ARTIFACT**: `worldbuilding_research_notes.md` created with key points from all 4 sources.

- [x] Human final approval

### B2. Citation Research - Batch 2 / sign-off

*Citations 5-8*

- [x] Plan this section
- [x] Human approves plan
- [x] Read: Study.com - Cultural Milieu
- [x] Read: Jerry Jenkins worldbuilding
- [x] Read: Malinda Lo - Worldbuilding for contemporary fiction
- [x] Read: Reddit worldbuilding table of elements
- [x] Add key points to `worldbuilding_research_notes.md`
- [x] **CHECKPOINT**: Batch 2 notes complete
- [x] Human final approval

**Note:** Julia Amante source was unavailable; substituted Malinda Lo's "Building a Real World" which covers the same topic (contemporary fiction worldbuilding) with excellent content.

### B3. Citation Research - Batch 3 / sign-off

*Citations 9-11*

- [x] Plan this section
- [x] Human approves plan
- [x] Read: Well-Storied introduction to worldbuilding
- [x] Read: Katie Bachelder fantasy worldbuilding fundamentals
- [x] Read: Myers Fiction worldbuilding 101
- [x] Add key points to `worldbuilding_research_notes.md`
- [x] **CHECKPOINT**: All citation research complete
- [x] **ARTIFACT**: `worldbuilding_research_notes.md` - condensed summary with Final Synthesis section
- [x] Human final approval

### B4. Competitor Review / sign-off

- [x] Plan this section
- [x] Human approves plan
- [x] Review `software_research.md` for how competitors present worldbuilding
- [x] Note relevant patterns for our guide
- [x] Add competitor insights to `worldbuilding_research_notes.md`
- [x] **CHECKPOINT**: Research phase complete
- [x] Human final approval

### B5. Guide Outline / sign-off

*Depends on: B1-B4 (research complete)*

- [x] Plan this section
- [x] Human approves plan
- [x] Create outline for worldbuilding guide
- [x] Map outline sections to StoryWorld tabs/features
- [x] **CHECKPOINT**: Outline approved
- [x] **ARTIFACT**: Outline in `worldbuilding_guide_outline.md` (14 sections + appendix)
- [x] Human final approval

### B6. Guide Draft / sign-off

*Depends on: B5 (outline), A5-A7 (reference docs for cross-links)*

- [x] Plan this section
- [x] Human approves plan
- [x] Draft introduction (what is worldbuilding)
- [x] Draft "where to start" section
- [x] Draft "how to proceed" section
- [x] Draft section linking to StoryWorld features
- [x] Add cross-links to Story Elements reference pages
- [x] **CHECKPOINT**: Full draft complete
- [x] Human final approval

**ARTIFACT**: `/docs/Writing with StoryCAD/Worldbuilding_Guide.md` - 14-section guide (~2500 words) with cross-links to all StoryWorld reference docs. Used `tutorial-engineer` agent for initial draft, reviewed for style consistency.

### B7. Guide Review and Polish / sign-off

- [x] Plan this section
- [x] Human approves plan
- [x] Review for plain language (non-technical audience)
- [x] Check all cross-links work
- [x] Final polish (fixed nav_order for StoryWorld to appear after Scene Form)
- [x] **CHECKPOINT**: Guide ready for publication
- [x] Human final approval

---

## Status Log

*Update after each checkpoint*

| Date | Checkpoint | Notes |
|------|------------|-------|
| 2026-01-21 | Plan created | Two parallel tracks defined |
| 2026-01-21 | A1 complete | Selected Land of Oz (Baum) - public domain, covers all 9 tabs |
| 2026-01-21 | B1 complete | Created worldbuilding_research_notes.md with 4 citations |
| 2026-01-21 | A1 approved | Human final approval |
| 2026-01-21 | B1 approved | Human final approval |
| 2026-01-22 | A2 complete | Created screenshot_data_script.md with Land of Oz content for all 9 tabs |
| 2026-01-22 | B2 complete | Added citations 5-8 to worldbuilding_research_notes.md |
| 2026-01-22 | A2 approved | Human final approval |
| 2026-01-22 | B2 approved | Human final approval |
| 2026-01-22 | B3 complete | Added citations 9-11 + Final Synthesis to worldbuilding_research_notes.md |
| 2026-01-22 | B3 approved | Human final approval |
| 2026-01-22 | B4 complete | Competitor review - patterns, terminology, approaches for guide |
| 2026-01-22 | B4 approved | Human final approval - Track B research phase complete |
| 2026-01-22 | B5 complete | Guide outline created - 14 sections mapped to StoryWorld tabs |
| 2026-01-22 | B5 approved | Human final approval - added series/collaborative world sharing |
| 2026-01-22 | A4 complete | Template created with 3 templates, screenshot filenames, nav_order suggestions |
| 2026-01-22 | A4 approved | Human final approval |
| 2026-01-22 | A3 complete | All 10 screenshots captured to /docs/media/ |
| 2026-01-22 | A5 complete | Created StoryWorld_Form.md (main form overview) |
| 2026-01-22 | A6 complete | Created 4 single-entry tab docs (Structure, History, Economy, Magic/Technology) |
| 2026-01-22 | A7 complete | Created 5 list-based tab docs (Physical Worlds, Species, Cultures, Governments, Religions) |
| 2026-01-22 | A8 complete | Updated Print_Reports.md and Scrivener_Reports.md with Story World mentions |
| 2026-01-22 | B6 complete | Created Worldbuilding_Guide.md - 14-section guide with cross-links |
| 2026-01-22 | A5-A8 approved | Human final approval for all Track A reference docs |
| 2026-01-22 | B6 approved | Human final approval for worldbuilding guide draft |
| 2026-01-22 | B7 complete | Review passed, fixed nav_order (StoryWorld after Scene Form) |

---

## Research Sources

### Competitor Tools
See `software_research.md` for detailed analysis of:
- World Anvil, Campfire, LegendKeeper, Kanka
- Fantasia Archive, Notebook.ai, Plottr
- Obsidian, Scrivener, Arcweave

### Worldbuilding Citations
See `worldbuilding citations.png`:
1. https://www.dabblewriter.com/articles/worldbuilding-guide
2. Madeline James Writes - Worldbuilding Guide
3. Wikipedia - Worldbuilding
4. https://www.campfirewriting.com/worldbuilding-tools
5. Study.com - Cultural Milieu
6. https://jerryjenkins.com/worldbuilding/
7. https://juliaamante.medium.com/world-building-for-contemporary-fiction-writers
8. https://www.reddit.com/r/worldbuilding/comments/u55hfb/worldbuilding_table_of_elements/
9. https://www.well-storied.com/blog/an-introduction-to-world-building
10. https://katiebachelder.com/2021/07/30/fantasy-world-building-fundamentals/
11. https://myersfiction.com/2024/02/13/world-building-101-crafting-immersive-fictional-worlds/

---

## Artifacts Created

| Artifact | Purpose | Status |
|----------|---------|--------|
| `worldbuilding_research_notes.md` | Condensed citation research for future sessions | **Complete** (B1-B4) |
| `StoryWorld_Tab_Template.md` | Template for reference docs | **Complete** (A4) |
| `worldbuilding_guide_outline.md` | Outline for educational guide | **Complete** (B5) |
| `/docs/Story Elements/StoryWorld_Form.md` | Main form documentation | **Complete** (A5) |
| `/docs/Story Elements/StoryWorld_*_Tab.md` (9 files) | Tab documentation | **Complete** (A6-A7) |
| `/docs/Reports/Print_Reports.md` | Updated with Story World | **Complete** (A8) |
| `/docs/Reports/Scrivener_Reports.md` | Updated with Story World | **Complete** (A8) |
| `/docs/Writing with StoryCAD/Worldbuilding_Guide.md` | Educational worldbuilding guide | **Complete** (B6) |

---

## Context for B7 (Guide Review)

**Draft location:** `/docs/Writing with StoryCAD/Worldbuilding_Guide.md`

**Review checklist:**
1. Check plain language (non-technical audience)
2. Verify all cross-links work
3. Ensure consistency with StoryCAD documentation voice
4. Final polish

**Audience:** Fiction writers (non-technical), need practical guidance

---

*Plan created: 2026-01-21*
*Last updated: 2026-01-22 (B7 complete - All documentation tasks complete)*
