# StoryWorld Documentation Templates

**Purpose:** Templates for StoryWorld reference documentation in `/docs/Story Elements/`
**Created:** 2026-01-22

---

## Template 1: Main Form Page (StoryWorld_Form.md)

```markdown
---
title: StoryWorld Form
layout: default
nav_enabled: true
nav_order: [TBD]
parent: Story Elements
has_toc: false
---
## StoryWorld Form

The StoryWorld form captures worldbuilding information for your story. It provides a structured way to document the setting, cultures, history, and systems that make your fictional world consistent and believable.

![](../media/StoryWorld-Structure-Tab.png)

StoryWorld is optional—add it when your story benefits from organized worldbuilding. Unlike most story elements, only one StoryWorld can exist per story (like Story Overview).

### When to Use StoryWorld

StoryWorld is valuable for:
- Fantasy and science fiction with invented worlds
- Mystery and thriller series set in specific milieus (a precinct, a law firm)
- Historical fiction requiring period accuracy
- Any story where setting details affect the plot

### Tabs

StoryWorld organizes worldbuilding into nine tabs:

| Tab | Purpose |
|-----|---------|
| [Structure](StoryWorld_Structure_Tab.html) | World Type classification |
| [Physical Worlds](StoryWorld_Physical_Worlds_Tab.html) | Geography, climate, flora, fauna |
| [People/Species](StoryWorld_Species_Tab.html) | Inhabitants and their characteristics |
| [Cultures](StoryWorld_Cultures_Tab.html) | Values, customs, daily life |
| [Governments](StoryWorld_Governments_Tab.html) | Political structures and laws |
| [Religions](StoryWorld_Religions_Tab.html) | Belief systems and practices |
| [History](StoryWorld_History_Tab.html) | Past events that shaped the world |
| [Economy](StoryWorld_Economy_Tab.html) | Trade, currency, professions |
| [Magic/Technology](StoryWorld_Magic_Technology_Tab.html) | Power systems and their rules |

### Adding StoryWorld

To add a StoryWorld to your story:
1. Select **Add > Add StoryWorld** from the menu, or
2. Right-click in the Navigation Pane and select **Add StoryWorld**

StoryWorld appears as a top-level node in your story outline.

### Sharing Across a Series

A StoryWorld can be shared across multiple novels in a series. Copy the StoryWorld from one story file to the next to maintain consistency and build on your worldbuilding investment.
```

---

## Template 2: Single-Entry Tab (History, Economy, Magic/Technology)

```markdown
---
title: [Tab Name] Tab
layout: default
nav_enabled: true
nav_order: [TBD]
parent: StoryWorld Form
has_toc: false
---
### [Tab Name] Tab

[One-sentence description of what this tab captures.]

![](../media/StoryWorld-[TabName]-Tab.png)

[Optional: Brief guidance on when/why to use this tab.]

#### Fields

**[FieldName]**
[Description of what to enter. Use placeholder text from the app as guidance.]

**[FieldName]**
[Description.]

[Repeat for each field...]

#### Tips

- [Practical tip for using this tab effectively]
- [Another tip if applicable]
```

---

## Template 3: List-Based Tab (Physical Worlds, Species, Cultures, Governments, Religions)

```markdown
---
title: [Tab Name] Tab
layout: default
nav_enabled: true
nav_order: [TBD]
parent: StoryWorld Form
has_toc: false
---
### [Tab Name] Tab

[One-sentence description of what this tab captures.]

![](../media/StoryWorld-[TabName]-Tab.png)

This is a list-based tab—you can add multiple entries. [Brief explanation of why multiples make sense, e.g., "A world may have multiple cultures, each with its own values and customs."]

#### Adding and Navigating Entries

- Click **+ Add [Entry]** to create a new entry
- Use the **◀ Prev** and **▶ Next** buttons to navigate between entries
- The position indicator (e.g., "2 of 5") shows your current location
- Click **Remove** to delete the current entry

#### Fields

Each entry contains:

**Name**
[The identifier for this entry, displayed in the navigation.]

**[FieldName]**
[Description of what to enter.]

**[FieldName]**
[Description.]

[Repeat for each field...]

#### Tips

- [Practical tip for using this tab effectively]
- [When to create multiple entries vs. a single entry]
```

---

## Screenshot Placement Guidelines

Based on existing documentation patterns:

1. **Screenshot goes early** - Right after the introductory text, before field descriptions
2. **One screenshot per tab page** - Shows the tab with representative content
3. **Filename convention**: `StoryWorld-[TabName]-Tab.png`
4. **Markdown syntax**: `![](../media/StoryWorld-[TabName]-Tab.png)`

---

## Front Matter Reference

| Field | Purpose | Example |
|-------|---------|---------|
| `title` | Page title in nav and browser | `StoryWorld Structure Tab` |
| `layout` | Jekyll layout | `default` |
| `nav_enabled` | Show in navigation | `true` |
| `nav_order` | Position in navigation | `60` (assign sequentially) |
| `parent` | Parent page for hierarchy | `StoryWorld Form` |
| `has_toc` | Show table of contents | `false` |

---

## File Naming Convention

| Page | Filename |
|------|----------|
| Main form | `StoryWorld_Form.md` |
| Structure tab | `StoryWorld_Structure_Tab.md` |
| Physical Worlds tab | `StoryWorld_Physical_Worlds_Tab.md` |
| People/Species tab | `StoryWorld_Species_Tab.md` |
| Cultures tab | `StoryWorld_Cultures_Tab.md` |
| Governments tab | `StoryWorld_Governments_Tab.md` |
| Religions tab | `StoryWorld_Religions_Tab.md` |
| History tab | `StoryWorld_History_Tab.md` |
| Economy tab | `StoryWorld_Economy_Tab.md` |
| Magic/Technology tab | `StoryWorld_Magic_Technology_Tab.md` |

---

## nav_order Assignment

Existing Story Elements use nav_order values roughly:
- Character Form: 42, tabs 43-50
- Problem Form: ~30s
- Scene Form: ~60s
- Setting Form: 53-55

**Suggested for StoryWorld:**
- StoryWorld Form: 56
- Structure Tab: 57
- Physical Worlds Tab: 58
- Species Tab: 59
- Cultures Tab: 60
- Governments Tab: 61
- Religions Tab: 62
- History Tab: 63
- Economy Tab: 64
- Magic/Technology Tab: 65

(Verify against actual nav_order values in existing docs before finalizing.)
