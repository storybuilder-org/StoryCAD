# Issue #782 - StoryWorld Feature Log

**Issue:** [StoryCAD #782 - Support for Worldbuilding](https://github.com/storybuilder-org/StoryCAD/issues/782)
**Log Started:** 2026-01-19
**Status:** UI Design Phase
**Location:** `/devdocs/worldbuilding/` (moved from `/mnt/c/temp/worldbuilding/` on 2026-01-19)

---

## Log Entries

### 2026-01-19 - Session 1: Understanding the Problem Space

**Participants:** User (Terry), Claude Code

**Context:** User requested help understanding how worldbuilding_categories.md fits into the overall StoryWorld feature design.

#### Documents Reviewed
All files in `/mnt/c/temp/worldbuilding/`:
- storyworld_plan.md - Master implementation plan
- workflow_summary.md - 26 Collaborator workflows
- worldbuilding_categories.md - World Type classification UI pattern
- software_research.md - 10 competitor tools analyzed
- taxonomies_research.md - 25+ frameworks, 8 primary categories identified
- existing_workflow_sources.md - Craft knowledge from 4 World Model workflows
- ui_patterns_research.md - Competitor UI patterns for large text fields
- key_questions.png - Existing StoryCAD dialog screenshot
- topic_information.png - Existing StoryCAD dialog screenshot

#### Key Discussion: World Type vs. Sub-Genres vs. Taxonomies

**Problem identified:** Confusion about how World Type classification (worldbuilding_categories.md) relates to sub-genres (steampunk, etc.) and the 8 taxonomy categories.

**Clarification reached:**

| Concept | Question It Answers | Examples |
|---------|---------------------|----------|
| **World Type** | What is the relationship between this world and reality? | Consensus Reality, Hidden World, Divergent World |
| **Sub-genre** | What aesthetic/technological/thematic flavor? | Steampunk, cyberpunk, magical realism |
| **Taxonomies** | What details define this world? | Physical World, Culture, History, etc. |

**Key insight:** World Type is an *ontological* classification about how reality works (can magic exist? do physical laws apply?). Sub-genres describe aesthetic flavor that can exist across multiple World Types.

**Open question identified:** Should StoryCAD capture World Type, sub-genre, or both? If both, how do they interact?

#### Next Action
Research how competitor software (World Anvil, Campfire, etc.) handles the World Type / sub-genre classification problem.

---

### 2026-01-19 - Session 1 (continued): World Type Framework Source Identified

#### Competitor Research Finding

Web search and review of software_research.md revealed: **None of the competitor tools (World Anvil, Campfire, LegendKeeper, etc.) have a World Type classification.** They all organize by entity types (Characters, Locations, etc.) rather than ontological world classification.

This means the World Type approach in worldbuilding_categories.md is **novel** - not derived from competitor patterns.

#### Source Material Identified

User provided the original ChatGPT conversation that generated the World Type framework. Saved to: `world_type_source_chatgpt.md`

**Key Architecture Discovered:**

The framework operates on two layers:

| Layer | Purpose | User | Content |
|-------|---------|------|---------|
| **Gestalt Layer** | 8 recognizable world types | Human writers | Single dropdown choice |
| **Axis Layer** | 6 orthogonal dimensions | Software/AI | Decomposed values for each gestalt |

**The 6 Axes:**
1. Ontology (What exists) - 5 values
2. World Relation (Relation to our reality) - 5 values
3. Rule Transparency (How rules behave) - 4 values
4. Scale of Difference - 3 values
5. Agency Source (What drives change) - 4 values
6. Tone Logic (How the world feels) - 5 values

**The 8 Gestalts (World Types):**
1. Consensus Reality
2. Enchanted Reality
3. Hidden World
4. Divergent World
5. Constructed World
6. Mythic World
7. Estranged World
8. Broken World

**Design Pattern:**
- Writers select: a world type (gestalt)
- Software stores: axis values
- UI shows: one labeled choice
- Advanced mode: exposes axes only if needed

#### Decisions Made This Session

**Genre/Sub-genre Question Resolved:**
- StoryOverview already has a Genre control
- No need to duplicate in StoryWorld
- StoryWorld could optionally *recommend* sub-genres that align with selected World Type
- Sub-genres (steampunk, cyberpunk, etc.) are aesthetic flavors that can exist across multiple World Types

**Examples Strategy:**
- Concrete examples (books, movies, series) clarify abstract categories
- Series and shared-world fiction are especially good examples (sustained worldbuilding)
- Examples mentioned:
  - Harry Potter (Hidden World)
  - Lord of the Rings (Mythic World / Constructed World)
  - Updike's Rabbit series (Consensus Reality)
  - 87th Precinct novels (Consensus Reality)
  - Jesse Stone movies (Consensus Reality)

#### Next Actions
1. Map each gestalt to its axis values (complete the ChatGPT framework)
2. Draft comprehensive example lists for each World Type
3. Investigate how World Type connects to the 8 taxonomy categories

---

### 2026-01-19 - Session 1 (continued): Gestalt Mapping and Examples

#### Gestalt-to-Axis Mapping Completed

Created `gestalt_axis_mapping.md` with:
- Full mapping of all 8 gestalts to 6 axis values
- Summary matrix for quick reference
- Observations about clear vs. variable mappings
- Notes on hybrid possibilities (works spanning multiple gestalts)

**Key observations:**
- Consensus Reality and Enchanted Reality have cleanest mappings
- Constructed World has most flexibility (defined by "fully built" rather than specific axis values)
- Some works naturally combine gestalts (LOTR = Constructed + Mythic)

#### Examples Compilation Completed

Created `world_type_examples.md` with:
- 10+ examples per World Type (books, series, films, TV)
- Emphasis on series and shared-world fiction
- Cross-category examples showing hybrid works
- Suggestions for using examples in UI, Collaborator, and User Manual

**Notable series examples by World Type:**
- Consensus Reality: 87th Precinct (55 books), Harry Bosch, Rabbit series
- Hidden World: Harry Potter, Dresden Files (17+ books), Percy Jackson
- Constructed World: Discworld (41 books), Wheel of Time (14), Malazan (10)
- Broken World: Walking Dead, Hunger Games trilogy

#### Next Action
Investigate relationship between World Type selection and the 8 taxonomy categories.

---

### 2026-01-19 - Session 1 (continued): Key Architectural Insight

#### The Question
"Does World Type selection affect which taxonomy categories are relevant or how they're presented?"

#### Initial (Rejected) Approach
Vague ideas about hiding tabs, minimizing categories, or changing focus based on World Type. Problems:
- Complex UI logic
- Not actionable for user sitting at PC
- Hand-wavy ("might focus on...")

#### Better Approach: Collaborator Provides the Intelligence

**UI stays simple and static:**
- World Type dropdown (8 choices)
- 8 taxonomy category tabs (always visible)
- User fills in what they want, when they want

**Collaborator provides contextual guidance:**
- Reads selected World Type (and underlying 6-axis values)
- Reads Genre from StoryOverview (input to inform guidance)
- When user asks for help with any taxonomy category, Collaborator tailors its suggestions based on World Type + Genre
- Can also recommend Genre choices based on World Type (output)

**Examples of World-Type-aware guidance:**
- "You selected Broken World - for Economy, let's focus on scarcity, barter systems, resource conflicts..."
- "You selected Consensus Reality - Magic/Technology should reflect real-world constraints..."
- "You selected Hidden World - consider how the magical economy stays hidden from mundane commerce..."

#### Architectural Implications

| Layer | Responsibility |
|-------|----------------|
| **StoryWorld UI** | Static: World Type dropdown + 8 taxonomy tabs |
| **Data Model** | Store World Type gestalt + 6-axis values |
| **Collaborator** | Use World Type + Genre to guide all worldbuilding assistance |
| **World Model Workflows** | Become World-Type-aware via modified prompts |

**Key insight:** The 6-axis data layer primarily serves Collaborator, not the UI. UI stays simple; intelligence is in AI assistance.

#### Sequencing Clarification

**Issue #782 (StoryCAD) - Must Come First:**
- Create StoryWorld story element
- World Type dropdown (8 gestalts)
- 8 taxonomy category tabs
- Data model (gestalt + axis values)

**Future Issue (Collaborator) - Depends on #782:**
- Update World Model workflows to be World-Type-aware
- Add Genre reading and recommendation
- Cannot begin until StoryWorld element exists
- Separate scope, separate issue

Created `world_type_aware_workflows.md` as design notes for the future Collaborator work, clearly marked as dependent on Issue #782.

---

### 2026-01-19 - Session 1 (continued): Consensus Reality Refinement

#### Key Insight

Consensus Reality is not simply "no speculative elements." It still requires worldbuilding because reality is **consensual to a particular group/subculture**.

Every Consensus Reality story builds a specific slice of reality:

| Subculture/Milieu | Examples | Worldbuilding Focus |
|-------------------|----------|---------------------|
| Police procedural | 87th Precinct, Harry Bosch | Precinct culture, hierarchy, procedures |
| Medical procedural | ER, House | Hospital culture, medical ethics, professional norms |
| Legal procedural | Grisham novels | Courtroom rules, firm politics, legal culture |
| Wall Street/Finance | Bonfire of the Vanities, House of Cards | Excess, moral logic of money, power dynamics |
| Suburbia | Big Little Lies, Stepford Wives, Virgin Suicides | Conformity, dysfunction, dark secrets, appearances |

#### Implication

The 8 taxonomy categories **still apply** to Consensus Reality - they're grounded in research rather than invention:
- Physical World → Specific locations (the precinct, the hospital, the suburb)
- Culture → Subculture's values, norms, taboos, insider knowledge
- Government → Power structures and hierarchy within that world
- Economy → How money/resources/status work in that context

**UI Implication:** The World Type tab for Consensus Reality should prompt writers to identify their specific milieu/subculture, not just dismiss worldbuilding as unnecessary.

---

## Research Tasks

- [x] Review all existing research documents
- [x] Clarify World Type vs. sub-genre distinction
- [x] Research competitor approaches to world classification (finding: none have this)
- [x] Determine whether to include World Type, sub-genre, or both (decision: World Type in StoryWorld, genre in StoryOverview)
- [x] Document source of World Type framework (ChatGPT conversation)
- [x] Map each gestalt to axis values
- [x] Create example lists for each World Type
- [x] Define relationship between World Type and 8 taxonomies (answer: Collaborator, not UI)
- [x] Document World-Type-aware workflow design (future work, depends on #782)
- [x] Research competitor field structures for data model
- [x] Review and finalize data model properties (5 list tabs, 4 single tabs)
- [x] Review field granularity (5-7 fields per entry - confirmed)
- [ ] UI design (UI designer agent - layout, list patterns)
- [ ] Implementation (Model, ViewModel, View, integration)

## Decisions Made

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-01-19 | World Type (gestalt) in StoryWorld, Genre in StoryOverview | Avoid duplication; each serves different purpose |
| 2026-01-19 | Include concrete examples for each World Type | Clarifies abstract categories for users |
| 2026-01-19 | Two-layer architecture (gestalt UI + axis data) | Humans think in gestalts; software needs dimensions |
| 2026-01-19 | UI static; Collaborator provides World-Type intelligence | Simpler UI, no complex show/hide logic; AI does the smart work |
| 2026-01-19 | Collaborator work is separate future issue | Depends on #782; different scope |
| 2026-01-19 | Milieu IS Culture - no separate field | Each Culture list entry represents a milieu/social environment |
| 2026-01-19 | 4 list-based tabs: Cultures, People/Species, Religions, Governments | Worlds can have multiples of these |
| 2026-01-19 | 5 single-entry tabs: Structure, Physical World, History, Economy, Magic/Technology | Global/singular aspects |
| 2026-01-19 | Magic + Technology combined | Per Clarke's Law; both are power systems with rules/costs |
| 2026-01-19 | Settings handle specific locations | No explicit link needed; one StoryWorld per StoryModel |
| 2026-01-19 | Physical World is a list | For portal stories, space opera (The Expanse), multi-world settings |
| 2026-01-19 | 5 list tabs, 4 single tabs | Lists: Physical World, People/Species, Cultures, Governments, Religions |
| 2026-01-19 | List tabs start empty | User clicks (+) to add entries |
| 2026-01-19 | 5-7 fields per entry is fine | Nested tabs possible (designer's discretion) |
| 2026-01-19 | Structure tab first | User picks World Type before populating other tabs |
| 2026-01-19 | Tab order for remaining 8 tabs | Decided later once UI is visible/testable |
| 2026-01-19 | Use PlaceholderText and TeachingTip | Reduce cognitive load; labels alone are ambiguous |

---

### 2026-01-19 - Session 1 Summary: Research Phase Complete

**Status:** Data model research phase is complete. Ready for UI design and implementation.

**Final Data Model:**
- **9 tabs total** (1 Structure + 8 taxonomy)
- **5 list-based tabs:** Physical World, People/Species, Cultures, Governments, Religions
- **4 single-entry tabs:** Structure, History, Economy, Magic/Technology
- **~24 single-tab fields + ~32 fields per list entry type**

**Key Architectural Decisions:**
- World Type gestalt (8 options) + 6 axes (for Collaborator)
- Milieu = Culture (no separate field)
- Magic + Technology combined (Clarke's Law)
- Settings handle specific locations (no explicit linkage)
- List tabs start empty; (+) to add
- Structure tab first

**Next Phase:** UI design (UI designer agent)

**Files Created This Session:**
- issue_782_log.md (this log)
- world_type_source_chatgpt.md
- gestalt_axis_mapping.md
- world_type_examples.md
- world_type_aware_workflows.md
- data_model_research.md

---

### 2026-01-19 - Session 2: Project Organization

**Context:** User requested organization of Issue #782 work before proceeding with UI design.

#### Actions Taken

1. **Created branch**: `issue-782-storyworld` from `dev`

2. **Moved files**: All worldbuilding files moved from `/mnt/c/temp/worldbuilding/` to `/devdocs/worldbuilding/` for version control

3. **Updated Issue #782**: Added proper content with:
   - Feature description
   - Key design decisions summary
   - Links to design documents
   - Standard lifecycle checkboxes (Design/Code/Test/Evaluate)

4. **Updated storyworld_plan.md**:
   - Marked Phase 1 (Research & Data Model) as COMPLETE
   - Updated Phase 2 (UI Design) as CURRENT
   - Removed obsolete sections (superseded by data_model_research.md)
   - Updated file paths to reflect new location

#### Current State

- **Research phase**: Complete
- **Data model**: Finalized in `data_model_research.md`
- **Next phase**: UI Design (Phase 2)
- **Branch**: `issue-782-storyworld`

---

## Open Questions

All major research questions resolved. Remaining items are for UI design/implementation phase:

1. Layout pattern for list tabs with multiple fields (designer agent)
2. Exact tab order for the 8 non-Structure tabs (decided when visible)

## Files in /devdocs/worldbuilding/

| File | Purpose | Status |
|------|---------|--------|
| **data_model_research.md** | Final data model specification | **PRIMARY** |
| storyworld_plan.md | Master implementation plan (updated) | Active |
| gestalt_axis_mapping.md | 8 gestalts mapped to 6 axes | Complete |
| world_type_examples.md | Comprehensive examples per World Type | Complete |
| world_type_aware_workflows.md | Future Collaborator work (depends on #782) | Future |
| issue_782_log.md | This log | Active |
| taxonomies_research.md | 25+ frameworks analyzed | Reference |
| software_research.md | 10 competitor tools | Reference |
| existing_workflow_sources.md | Craft knowledge from workflows | Reference |
| ui_patterns_research.md | Competitor UI patterns | Reference |
| workflow_summary.md | 26 Collaborator workflows | Reference |
| worldbuilding_categories.md | World Type UI pattern (original) | Superseded |
| world_type_source_chatgpt.md | ChatGPT conversation source | Reference |

---

*Log maintained by Claude Code sessions*
