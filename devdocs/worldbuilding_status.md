# Worldbuilding Feature Status Log

**Issue:** [StoryCAD #782 - Support for Worldbuilding](https://github.com/storybuilder-org/StoryCAD/issues/782)
**Status:** Planning - UI Design Phase
**Last Updated:** 2026-01-14

---

## Session Recovery Info

### Current State
Planning complete. UI patterns research done. Ready for UI design decisions and wireframes.

### Key Files

| Location | File | Purpose |
|----------|------|---------|
| `/mnt/c/temp/worldbuilding/` | `storyworld_plan.md` | **Main plan document** - WBS, open questions, technical checklist |
| `/mnt/c/temp/worldbuilding/` | `ui_patterns_research.md` | **NEW** - Competitor UI analysis (World Anvil, Campfire, etc.) |
| `/mnt/c/temp/worldbuilding/` | `taxonomies_research.md` | 700+ line analysis of 25+ worldbuilding frameworks |
| `/mnt/c/temp/worldbuilding/` | `software_research.md` | 940+ line analysis of 10 worldbuilding tools |
| `/mnt/c/temp/worldbuilding/` | `existing_workflow_sources.md` | Craft knowledge from 4 World Model workflows |
| `/mnt/c/temp/worldbuilding/` | `worldbuilding_categories.md` | World Type classification UI pattern |
| `/mnt/c/temp/worldbuilding/` | `workflow_summary.md` | Current Collaborator workflows (26 total) |
| `/mnt/c/temp/worldbuilding/` | `key_questions.png` | Screenshot of Key Questions dialog UI |
| `/mnt/c/temp/worldbuilding/` | `topic_information.png` | Screenshot of Topic Information dialog UI |

### Next Steps
1. **Decide UI pattern** - Hybrid approach recommended (sidebar + tabs + sections)
2. **Create wireframes** - Visual mockups before coding
3. **Define properties per category** - Use taxonomy research as input
4. **Get stakeholder approval** - Before implementation

---

## Session Log

### 2026-01-13 - Initial Planning & Research

**Requirements gathered:**
- New story element type: **StoryWorld**
- Cardinality: Single instance only (like Overview)
- Optionality: Optional (unlike mandatory Overview)
- UI Pattern: Similar to Topic Information dialog (Topics dropdown → SubTopic → Notes text area)

**Research completed:**
- Analyzed 25+ worldbuilding taxonomy frameworks
- Reviewed 10 worldbuilding software tools (World Anvil, Campfire, LegendKeeper, Kanka, etc.)
- Extracted craft knowledge from existing World Model workflows

**Key findings:**
1. 8 primary categories appear in 80%+ of sources: Physical World, People/Species, Culture, History, Government/Politics, Religion/Spirituality, Economy, Magic/Technology
2. Existing Collaborator World Model workflows (4) align well with core categories
3. Market gaps: AI-assisted worldbuilding, story-world consistency, scene-level world reference

---

## Requirements Summary

### StoryWorld Element
- **Type:** New story element
- **Cardinality:** Single instance per story (like Overview)
- **Optional:** Yes (unlike Overview)
- **Purpose:** Contains worldbuilding information

### UI Pattern (from Topic Information dialog)
```
┌─────────────────────────────────────┐
│ Topics: [Dropdown - Category]       │
│ SubTopic: [Field - Specific facet]  │
│ Notes: [Editable text area]         │
│                                     │
│ [Previous SubTopic] [Next SubTopic] │
│              [Done]                 │
└─────────────────────────────────────┘
```

### World Type Classification (from worldbuilding_categories.md)
Primary control with 8 options:
- Consensus Reality, Enchanted Reality, Hidden World, Divergent World
- Constructed World, Mythic World, Estranged World, Broken World

Progressive disclosure for advanced options (Reality Rules, Relation to Our World, Source of Power).

### Existing Collaborator World Model Workflows
| Label | Title | Maps to... |
|-------|-------|------------|
| WorldRulesLogic | World Rules and Logic | Physical laws, magic systems, what's possible |
| WorldHistory | World History and Background | Timeline, major events, how world got here |
| GeographyCultures | Geography and Cultures | Physical world, peoples, customs |
| TechnologyMagic | Technology and Magic Systems | Tech level, magic rules, limits, costs |

---

## Research Summary

### Taxonomy Consensus (from taxonomies_research.md)
**Primary categories (80%+ of sources):**
1. Physical World/Geography (100%)
2. Culture/Society (96%)
3. History (92%)
4. Government/Politics (88%)
5. Religion/Beliefs (84%)
6. Magic Systems (80%)
7. Economy/Trade (76%)
8. People/Species (72%)

**Notable frameworks:**
- Patricia C. Wrede (SFWA): 7 categories, 30+ subcategories - most comprehensive
- Brandon Sanderson: Story-driven, focus on 1 physical + 2 cultural elements max
- CharacterHub: 3 domains (Physical, Metaphysical, Cultural), 17 subcategories

### Software Patterns (from software_research.md)
**Common entity types across tools:**
Characters, Locations, Items, Organizations, Species/Races, Magic/Abilities, Languages, Events

**Integration approaches:**
- Built-in manuscript (World Anvil, Campfire)
- Export-focused (Plottr)
- Reference sidebar (Campfire)
- Separate tools (most others)

**Market gaps StoryCAD could address:**
- AI-assisted worldbuilding (only Kanka has Bragi AI)
- Story-world consistency checking
- Scene-level world reference
- Character arc integration with worldbuilding

---

## Open Questions

1. What taxonomy of worldbuilding categories/subtopics should StoryWorld use?
2. Where should StoryWorld appear in the navigation tree?
3. How extensible should the category/subtopic system be? (fixed vs. user-customizable)
4. How do World Model workflows connect to StoryWorld element?
5. What new Collaborator workflows would help with worldbuilding?

---

## Design Decisions

### 2026-01-14 Session - UI Design Discussion

**StoryWorld Element Structure:**
- 8 primary categories as tabs: Physical World, Culture, History, Government, Religion, Magic, Economy, People
- Each category has multiple RTF properties (can be page-sized)
- First tab(s) for World Type classification form (selection controls, not RTF)

**UI Challenges Identified:**
- Multiple large RTF fields per category tab - scrolling not ideal
- Considered: nested vertical tabs within each category tab
- Need to research how World Anvil, Campfire, etc. handle this

**World Type Classification (first tab):**
- 8 World Types (Consensus Reality, Enchanted Reality, Hidden World, etc.)
- Advanced options: Reality Rules, Relation to Our World, Source of Power
- Sub-genres derived from combinations (Tolkienesque fantasy, alternate history, urban magic)

**Work Breakdown Structure Identified:**
1. UI Design - tabs, categories, world type selection, property layout
2. Data Analysis - determine specific properties per category
3. Model - StoryWorldModel class with JSON properties
4. ViewModel - StoryWorldViewModel with ISaveable
5. Unit Testing

**Current Phase:** Planning - organizing requirements into structured plan

**Plan Document Created:** `/mnt/c/temp/worldbuilding/storyworld_plan.md`
- Work breakdown structure (8 phases)
- UI design options documented
- Open questions consolidated (12 questions)
- Technical implementation checklist

**Codebase Exploration Completed:**
- Analyzed SettingModel, SettingViewModel, SettingPage as reference
- Documented exact file locations and line numbers for modifications
- Confirmed JSON serialization pattern (JsonIgnore + JsonInclude + JsonPropertyName)
- Identified INavigable + ISaveable interfaces required for ViewModel

**Next Action:** Resolve open questions, particularly UI design pattern for property navigation

### 2026-01-14 - UI Patterns Research Completed

**Research saved to:** `/mnt/c/temp/worldbuilding/ui_patterns_research.md`

**Competitor patterns analyzed:**
| Tool | Pattern |
|------|---------|
| World Anvil | Tabbed prompts, selective field visibility |
| Campfire | Resizable panel grid, drag-and-drop |
| LegendKeeper | Wiki pages with tabs, inline editing |
| Kanka | Form + sidebar, attribute sections |

**Hybrid recommendation for StoryWorld:**
1. Right sidebar for World Type classification (dropdowns)
2. 8 category tabs in main area
3. Within each tab: Section headers with RTF fields
4. Optional collapse/expand for sections
5. Selective field visibility (don't show all fields by default)

**Reference URLs:**
- World Anvil: https://www.worldanvil.com/learn/article-guides/article-edit
- Campfire: https://www.campfirewriting.com/learn/panels-tutorial
- LegendKeeper: https://www.legendkeeper.com/features/
- Kanka: https://docs.kanka.io/en/latest/features/attributes.html

---

## Development Phases (To Be Planned)

1. **Phase: UI Design** - Design StoryWorld element interface in StoryCAD
2. **Phase: Taxonomy** - Formalize worldbuilding categories and subtopics
3. **Phase: Collaborator Integration** - Define how AI workflows interact with StoryWorld
