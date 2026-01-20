# Issue #782: StoryWorld Feature Plan

**Issue:** [StoryCAD #782 - Support for Worldbuilding](https://github.com/storybuilder-org/StoryCAD/issues/782)
**Created:** 2026-01-14
**Updated:** 2026-01-19
**Status:** Integration Phase

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

### Remaining Work

#### Navigation
- [ ] Add `private const string StoryWorldPage = "StoryWorldPage"` in ShellViewModel.cs
- [ ] Add `nav.Configure(StoryWorldPage, typeof(StoryWorldPage))` in App.xaml.cs
- [ ] Add StoryWorld case in TreeViewNodeClicked switch (ShellViewModel.cs)

#### Add StoryWorld Command
- [ ] Add `AddStoryWorldCommand` property in ShellViewModel.cs
- [ ] Initialize command with singleton check (only one StoryWorld per story)
- [ ] Add `CanAddStoryWorld()` method - returns false if StoryWorld already exists
- [ ] Add `NotifyCanExecuteChanged()` call where other commands are notified

#### OutlineService
- [ ] Add StoryWorld case in AddStoryElement switch statement
- [ ] Add singleton validation before creating StoryWorld

#### Menu and Flyout UI (Shell.xaml)
- [ ] Add StoryWorld button to Menu Bar flyout
- [ ] Add StoryWorld button to right-click flyout
- [ ] Determine icon (Globe is used by Settings - need different icon)
- [ ] Add keyboard shortcut (Alt+W / ⌥W)

#### Delete Handling
- [ ] Add confirmation dialog when deleting StoryWorld ("Are you sure?")
- [ ] StoryWorld CAN be deleted (unlike Overview)

#### Reports
- [ ] Add StoryWorld to PrintReports.cs
- [ ] Add StoryWorld to ScrivenerReports.cs (confirmed needed)
- [ ] Add StoryWorld formatting in ReportFormatter.cs

#### Tests
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
| `StoryCADLib/ViewModels/StoryWorldViewModel.cs` | New ViewModel | Done |
| `StoryCAD/Views/StoryWorldPage.xaml` | New page with TabView | Done |
| `StoryCAD/Views/StoryWorldPage.xaml.cs` | Code-behind | Done |
| `StoryCADLib/Services/ServiceLocator.cs` | DI registration | Done |
| `StoryCADLib/DAL/Lists.json` | WorldType, Ontology, etc. | Done |
| `StoryCADLib/ViewModels/ShellViewModel.cs` | Navigation + command | Pending |
| `StoryCAD/App.xaml.cs` | Configure navigation | Pending |
| `StoryCAD/Shell.xaml` | Menu/Flyout buttons | Pending |
| `StoryCADLib/Services/Outline/OutlineService.cs` | AddStoryElement case | Pending |
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
*Last updated: 2026-01-19*
