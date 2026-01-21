# Issue #782: StoryWorld Feature Plan

**Issue:** [StoryCAD #782 - Support for Worldbuilding](https://github.com/storybuilder-org/StoryCAD/issues/782)
**Created:** 2026-01-14
**Updated:** 2026-01-21
**Status:** Code Complete - Documentation Phase

---

## 1. Overview

Add a new story element type `StoryWorld` to StoryCAD for worldbuilding content.

**Characteristics:**
- Single instance per story (like StoryOverview)
- Optional (unlike mandatory StoryOverview)
- Contains worldbuilding information organized by category
- Future: Integrates with Collaborator World Model workflows

---

## 2. Implementation Status

### Completed Work
- [x] StoryWorldModel (with entry classes in Models/StoryWorld/)
- [x] StoryWorldViewModel (INavigable, ISaveable, IReloadable)
- [x] StoryWorldPage.xaml and code-behind
- [x] StoryItemType.StoryWorld enum value
- [x] DI registration in ServiceLocator
- [x] Lists in Lists.json (WorldType, Ontology, etc.)
- [x] Unit tests (Model and ViewModel)
- [x] Navigation wiring (ShellViewModel, App.xaml.cs)
- [x] AddStoryWorldCommand with singleton check
- [x] OutlineService.AddStoryElement case
- [x] Menu/Flyout UI (Icon: Map)
- [x] Responsive Grid layout for all tabs
- [x] Navigation pattern for multiple-occurrence tabs (Prev/Next with position display)
- [x] Single-entry responsive layout for History, Economy, Magic/Tech tabs
- [x] Expander layout for all taxonomy tabs with content indicators (bold when has content)
- [x] **UI Design Complete** (2026-01-21)

### Remaining Work

#### Delete Handling
- [x] StoryWorld uses standard trash system (delete, restore, empty)
- [x] SaveModel() case added for StoryWorldPage (was missing - bug fix)

#### Reports
- [x] Add StoryWorld to PrintReports.cs
- [x] Add StoryWorld to ScrivenerReports.cs (confirmed needed)
- [x] Add StoryWorld formatting in ReportFormatter.cs

#### Documentation

**See `issue_782_documentation_plan.md` for detailed WBS.**

Two parallel tracks:
- **Track A**: Reference documentation (Story Elements, Reports, Screenshots)
- **Track B**: Educational content (worldbuilding craft guide for Writing with StoryCAD)

#### Manual Testing (Acceptance)
- [ ] Test adding StoryWorld via command
- [ ] Test singleton constraint (command disabled when StoryWorld exists)
- [ ] Test navigation to StoryWorldPage
- [ ] Test delete with confirmation
- [ ] Test OutlineService.AddStoryElement for StoryWorld

---

## 3. Key Decisions

| Decision | Rationale |
|----------|-----------|
| World Type (gestalt) + 6 axes | Humans think in gestalts; software needs dimensions for AI |
| 5 list-based tabs | Physical World, People/Species, Cultures, Governments, Religions |
| 4 single-entry tabs | Structure, History, Economy, Magic/Technology |
| Magic + Technology combined | Clarke's Law: "sufficiently advanced technology is indistinguishable from magic" |
| Structure tab first | User picks World Type before populating other tabs |
| PlaceholderText + TeachingTip | Reduce cognitive load on complex fields |
| Icon: `World` | Different from Globe which is used by Settings |
| User-added (not auto-created) | Existing users have outlines without it |
| Can be deleted | Unlike Overview; confirmation dialog required |
| Scrivener export: Yes | StoryWorld report contains all tabs |

---

## 4. Data Model Summary

**See `data_model_research.md` for complete specification.**

### Tab Structure (9 tabs total)

| Tab | Type | Fields |
|-----|------|--------|
| **Structure** | Single | WorldType (gestalt) + 6 axes |
| **Physical World** | List | Name + 6 RTF per entry |
| **People/Species** | List | Name + 5 RTF per entry |
| **Cultures** | List | Name + 6 RTF per entry |
| **Governments** | List | Name + 5 RTF per entry |
| **Religions** | List | Name + 5 RTF per entry |
| **History** | Single | 5 RTF fields |
| **Economy** | Single | 5 RTF fields |
| **Magic/Technology** | Single | 7 fields |

---

## 5. Technical Implementation Reference

Adding a new story element type requires changes in these files:

| File | Change | Status |
|------|--------|--------|
| `StoryCADLib/Enums/StoryItemType.cs` | Add `StoryWorld` to enum | Done |
| `StoryCADLib/Models/StoryWorld/StoryWorldModel.cs` | New model class | Done |
| `StoryCADLib/Models/StoryWorld/*Entry.cs` | List item models | Done |
| `StoryCADLib/ViewModels/StoryWorldViewModel.cs` | New ViewModel + navigation | Done |
| `StoryCAD/Views/StoryWorldPage.xaml` | Page with responsive tabs | Done |
| `StoryCAD/Views/StoryWorldPage.xaml.cs` | Code-behind | Done |
| `StoryCADLib/Services/ServiceLocator.cs` | DI registration | Done |
| `StoryCADLib/DAL/Lists.json` | WorldType, Ontology, etc. | Done |
| `StoryCADLib/ViewModels/ShellViewModel.cs` | Navigation + command | Done |
| `StoryCAD/App.xaml.cs` | Configure navigation | Done |
| `StoryCAD/Shell.xaml` | Menu/Flyout buttons | Done |
| `StoryCADLib/Services/Outline/OutlineService.cs` | AddStoryElement case | Done |
| `StoryCADLib/Services/Reports/PrintReports.cs` | Report generation | Pending |
| `StoryCADLib/Services/Reports/ScrivenerReports.cs` | Scrivener export | Pending |

---

## 6. Research References

All in `/devdocs/worldbuilding/`:

| File | Purpose | Status |
|------|---------|--------|
| `data_model_research.md` | Final data model specification | **PRIMARY** |
| `gestalt_axis_mapping.md` | 8 gestalts mapped to 6 axes | Complete |
| `world_type_examples.md` | Example works per World Type | Complete |
| `issue_782_log.md` | Research session log | Active |
| `issue_782_documentation_plan.md` | Documentation phase WBS | Active |
| `world_type_aware_workflows.md` | Future Collaborator work | Future |
| `taxonomies_research.md` | 25+ frameworks analyzed | Reference |
| `software_research.md` | 10 competitor tools | Reference |
| `existing_workflow_sources.md` | Craft knowledge from workflows | Reference |
| `world_type_source_chatgpt.md` | ChatGPT conversation source | Reference |

---

## 7. Future Work (Separate Issue)

**Collaborator Integration** - depends on this issue completing:
- Connect World Model workflows to StoryWorld element
- Make workflows World-Type-aware
- Define new worldbuilding-specific workflows

See `world_type_aware_workflows.md` for design notes.

---

*Plan created: 2026-01-14*
*Last updated: 2026-01-21 (Documentation plan added)*
