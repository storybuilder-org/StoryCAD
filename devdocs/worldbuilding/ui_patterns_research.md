# UI Patterns Research: Worldbuilding Software

**Research Date:** 2026-01-14
**Purpose:** Understand how competitor tools handle entities with multiple large text fields

---

## Summary of Patterns Found

| Pattern | Used By | Description |
|---------|---------|-------------|
| **Tabbed Field Groups** | World Anvil | Fields organized into logical tabs (Generic, Personal, Social) |
| **Resizable Panel Grid** | Campfire | Drag-and-drop panels in customizable grid |
| **Wiki Pages with Tabs** | LegendKeeper | Each entity can have multiple page/map/board tabs |
| **Form + Sidebar Properties** | Kanka, Notion, Scrivener | Main content area + right sidebar for metadata |
| **Selective Field Visibility** | World Anvil | "Prompts" button reveals only fields user wants |
| **Sections/Accordions** | Kanka | Collapsible grouped attributes |

---

## 1. World Anvil

**Architecture:** Two-column layout with tabbed prompts

**Key UI:**
- Free-form "vignette" text at top
- Template prompts accessed via "+ Prompts" button
- Prompts organized into tabbed sections
- Sidebar for quick data fields

**Character Template Tabs:**
- Generic: eye color, gender, location, dates
- Naming: honorific, given name, middle name
- Personal: motivations, likes/dislikes, virtues, flaws
- Social: wealth, family ties, friendships
- Divine: domain, religious organizations

**Key Insight:** Users click prompts to add only needed fields - not all fields shown at once.

**Sources:**
- https://www.worldanvil.com/learn/article-guides/article-edit
- https://www.worldanvil.com/learn/article-templates/character

---

## 2. Campfire

**Architecture:** Fully customizable panel-based interface

**7 Panel Types:**
1. Attributes Panel - structured data
2. Text Panel - free-form content
3. List Panel - entries with titles/descriptions
4. Stats Panel - numerical values
5. Image Panel - gallery or grid
6. Table Panel - spreadsheet-like
7. Links Panel - bidirectional connections

**Key UI:**
- Grid layout with adjustable columns
- Drag-and-drop positioning
- Resize handles on panels
- Collapse/expand per panel
- Template system for reusable layouts

**Sources:**
- https://www.campfirewriting.com/learn/panels-tutorial
- https://www.campfirewriting.com/learn/characters-tutorial

---

## 3. LegendKeeper

**Architecture:** Wiki-style with multi-tab elements

**Key UI:**
- Each element can have multiple tabs (Page, Map, Board)
- Inline editing without "edit mode"
- Slash commands (`/layout` for columns)
- `[[double brackets]]` auto-creates nested elements
- Properties sidebar on right

**Sources:**
- https://www.legendkeeper.com/features/
- https://www.legendkeeper.com/legendkeeper-101-the-basics-beyond/

---

## 4. Kanka

**Architecture:** Form-based with attribute sections

**Key UI:**
- Most-used fields at top
- Text area full width
- Attribute sections group related fields
- Pinned attributes in profile sidebar
- `e` keyboard shortcut for edit mode

**Sources:**
- https://docs.kanka.io/en/latest/entities/characters.html
- https://docs.kanka.io/en/latest/features/attributes.html

---

## 5. UX Best Practices

**Progressive Disclosure:**
- Show only what's needed at each step
- Reveal additional fields as users progress
- Use accordions for hierarchical content

**Accordion Guidelines:**
- Keep expanded items open until user closes
- Don't auto-close when opening new sections
- Make scrollable if content exceeds space

**Tab Guidelines:**
- Limit to reasonable count
- If too many tabs, use accordion instead
- Max two levels of nested tabs
- Keep parent tab visible when scrolling

**One Thing Per Page:**
- Break complex forms into steps
- Progress indicators help gauge effort

---

## 6. Recommendations for StoryWorld

Based on research, recommended approach:

### Option A: World Anvil Style
- **9 tabs** (World Type + 8 categories)
- Each tab has tabbed prompts for properties
- "+ Add Property" reveals fields on demand
- Sidebar for metadata (World Type selections)

### Option B: Kanka/Notion Style
- **Main content area** with scrolling sections
- **Right sidebar** for World Type classification
- Section headers break up content
- Collapsible sections optional

### Option C: LegendKeeper Style
- **Wiki-like pages** within element
- Sub-tabs per category
- Inline editing
- Slash commands for formatting

### Hybrid Recommendation
1. **Right sidebar** for World Type classification (dropdowns)
2. **8 category tabs** in main area
3. **Within each tab:** Section headers with RTF fields
4. **Optional:** Collapse/expand for sections
5. **Don't show all fields** - let user add what they need

---

## Screenshots/References

**World Anvil Article Edit:**
https://www.worldanvil.com/learn/article-guides/article-edit

**Campfire Panels Tutorial:**
https://www.campfirewriting.com/learn/panels-tutorial

**LegendKeeper Features:**
https://www.legendkeeper.com/features/

**Kanka Attributes:**
https://docs.kanka.io/en/latest/features/attributes.html
