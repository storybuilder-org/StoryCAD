# StoryWorld Feature Plan

**Issue:** [StoryCAD #782 - Support for Worldbuilding](https://github.com/storybuilder-org/StoryCAD/issues/782)
**Created:** 2026-01-14
**Updated:** 2026-01-19
**Status:** Data Model Complete, UI Design Next

---

## 1. Overview

Add a new story element type `StoryWorld` to StoryCAD for worldbuilding content.

**Characteristics:**
- Single instance per story (like StoryOverview)
- Optional (unlike mandatory StoryOverview)
- Contains worldbuilding information organized by category
- Future: Integrates with Collaborator World Model workflows

---

## 2. Work Breakdown Structure

### Phase 1: Research & Data Model Design ✅ COMPLETE
- [x] Research UI patterns from worldbuilding software (World Anvil, Campfire, etc.)
- [x] Research competitor field structures for data model
- [x] Research craft book categories (Gillett, Card, Athans)
- [x] Define World Type framework (8 gestalts, 6 axes)
- [x] Map gestalts to axis values
- [x] Create example lists for each World Type
- [x] Finalize data model properties (5 list tabs, 4 single tabs)
- [x] Document UX requirements (PlaceholderText, TeachingTip)

**Output:** `data_model_research.md`, `gestalt_axis_mapping.md`, `world_type_examples.md`

### Phase 2: UI Design (CURRENT)
- [ ] Design Structure tab layout (World Type dropdown + axis controls)
- [ ] Design list-based tab pattern (Physical World, People/Species, Cultures, Governments, Religions)
- [ ] Design single-entry tab pattern (History, Economy, Magic/Technology)
- [ ] Determine tab order for 8 non-Structure tabs
- [ ] Create wireframes/mockups
- [ ] Review with stakeholder

### Phase 3: Model Implementation
- [ ] Add `StoryWorld` to `StoryItemType` enum
- [ ] Create `StoryWorldModel.cs` with all properties
- [ ] Create list item models (PhysicalWorldModel, SpeciesModel, CultureModel, etc.)
- [ ] Update serialization (verify JSON handles new types)
- [ ] Update `StoryNodeItem.cs` icon handling

### Phase 4: ViewModel Implementation
- [ ] Create `StoryWorldViewModel.cs` implementing ISaveable
- [ ] Register in `BootStrapper.cs`
- [ ] Implement property change handlers
- [ ] Implement list management (add/remove entries)

### Phase 5: View Implementation
- [ ] Create `StoryWorldPage.xaml` with tab structure
- [ ] Create list tab user controls
- [ ] Implement PlaceholderText for all RTF fields
- [ ] Implement TeachingTips for complex terms
- [ ] Wire up navigation (node selection → page)
- [ ] Implement ISaveable registration in page

### Phase 6: Integration
- [ ] Update `OutlineService` for StoryWorld element creation
- [ ] Update context menus for adding StoryWorld
- [ ] Tree position: under root, after Overview
- [ ] Single-instance enforcement

### Phase 7: Testing
- [ ] Unit tests for StoryWorldModel
- [ ] Unit tests for list item models
- [ ] Unit tests for StoryWorldViewModel
- [ ] Integration tests for serialization
- [ ] Manual UI testing

### Phase 8: Collaborator Integration (FUTURE - Separate Issue)
- [ ] Connect World Model workflows to StoryWorld element
- [ ] Make workflows World-Type-aware
- [ ] Define new worldbuilding-specific workflows

**Note:** Phase 8 is a separate issue that depends on this one completing. See `world_type_aware_workflows.md` for design notes.

---

## 3. Data Model Summary

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

### Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| World Type (gestalt) + 6 axes | Humans think in gestalts; software needs dimensions for AI |
| Milieu IS Culture | Each Culture entry represents a milieu/social environment |
| Magic + Technology combined | Clarke's Law |
| Physical World is a list | For portal stories, space opera, multi-world settings |
| List tabs start empty | User clicks (+) to add entries |
| Structure tab first | User picks World Type before populating other tabs |
| PlaceholderText + TeachingTip | Reduce cognitive load on complex fields |

---

## 4. Implementation Checklist (Technical)

Adding a new story element type requires changes in these files:

| File | Change |
|------|--------|
| `StoryCADLib/Enums/StoryItemType.cs` | Add `StoryWorld` to enum |
| `StoryCADLib/Models/Elements/StoryWorldModel.cs` | New model class |
| `StoryCADLib/Models/Elements/PhysicalWorldModel.cs` | New list item model |
| `StoryCADLib/Models/Elements/SpeciesModel.cs` | New list item model |
| `StoryCADLib/Models/Elements/CultureModel.cs` | New list item model |
| `StoryCADLib/Models/Elements/GovernmentModel.cs` | New list item model |
| `StoryCADLib/Models/Elements/ReligionModel.cs` | New list item model |
| `StoryCADLib/ViewModels/StoryWorldViewModel.cs` | New ViewModel |
| `StoryCAD/Views/StoryWorldPage.xaml` | New page with TabView |
| `StoryCAD/Views/StoryWorldPage.xaml.cs` | Code-behind |
| `StoryCADLib/ViewModels/StoryNodeItem.cs` | Add icon case |
| `StoryCADLib/Services/Outline/OutlineService.cs` | Add case in `AddStoryElement()` |
| `StoryCADLib/ViewModels/ShellViewModel.cs` | Add navigation case |
| `BootStrapper.cs` | Register StoryWorldViewModel |

### Model Pattern (from SettingModel.cs)
```csharp
public class StoryWorldModel : StoryElement
{
    [JsonIgnore] private string _field;

    [JsonInclude]
    [JsonPropertyName("Field")]
    public string Field { get => _field; set => _field = value; }

    public StoryWorldModel(StoryModel model, StoryNodeItem node)
        : base("New World", StoryItemType.StoryWorld, model, node) { }

    public StoryWorldModel(string name, StoryModel model, StoryNodeItem node)
        : base(name, StoryItemType.StoryWorld, model, node) { }

    public StoryWorldModel() { }
}
```

### ViewModel Pattern (from SettingViewModel.cs)
```csharp
public class StoryWorldViewModel : ObservableRecipient, INavigable, ISaveable
{
    private bool _changeable;
    private StoryWorldModel _model;

    public void Activate(object parameter) {
        _changeable = false;
        Model = (StoryWorldModel)parameter;
        LoadModel();
    }

    public void Deactivate(object parameter) => SaveModel();

    public void SaveModel() {
        Model.Field = Field;
        // ... all properties
    }

    private void LoadModel() {
        _changeable = false;
        Field = Model.Field;
        // ... all properties
        _changeable = true;
    }
}
```

---

## 5. Research References

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
| `worldbuilding_categories.md` | World Type UI pattern (original) | Superseded |
| `world_type_source_chatgpt.md` | ChatGPT conversation source | Reference |

---

## 6. Superseded Content

The following sections from the original plan have been superseded by `data_model_research.md`:

- **Section 3 (UI Design Details)** - Early concepts replaced by finalized data model
- **Section 4 (Data Model Details)** - Draft properties replaced by complete specification
- **Section 5 (Open Questions)** - All resolved; decisions documented in `issue_782_log.md`

---

*Plan created: 2026-01-14*
*Last updated: 2026-01-19*
