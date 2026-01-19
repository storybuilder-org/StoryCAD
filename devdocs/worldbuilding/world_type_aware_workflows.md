# World-Type-Aware Collaborator Workflows

**Created:** 2026-01-19
**Status:** Future Work - Design Notes
**Depends On:** Issue #782 (StoryWorld story element must exist first)

---

## Overview

This document explores how Collaborator's World Model workflows could become World-Type-aware. This work **cannot begin until Issue #782 is complete** because:

1. The StoryWorld element must exist to store World Type selection
2. The data model must expose World Type + axis values to Collaborator
3. The taxonomy category fields must exist for workflows to populate

**This is a separate issue from #782.**

---

## Current World Model Workflows

From `workflow_summary.md`, the 4 existing World Model workflows are:

| Workflow | Title | Purpose |
|----------|-------|---------|
| WorldRulesLogic | World Rules and Logic | Define fundamental rules and internal logic |
| WorldHistory | World History and Background | Develop historical context and background events |
| GeographyCultures | Geography and Cultures | Define physical geography and cultural groups |
| TechnologyMagic | Technology and Magic Systems | Design technological or magical systems |

---

## How Workflows Become World-Type-Aware

### The Pattern

Each workflow prompt gains a **World Type context block** that:
1. Reads the selected World Type (gestalt)
2. Reads the Genre from StoryOverview
3. Provides World-Type-specific guidance for that workflow's focus area

### Input Context (Available to All Workflows)

```
World Type: {StoryWorld.WorldType}
Genre: {StoryOverview.Genre}

Axis Values (for advanced reasoning):
- Ontology: {axis.ontology}
- World Relation: {axis.worldRelation}
- Rule Transparency: {axis.ruleTransparency}
- Scale of Difference: {axis.scaleDifference}
- Agency Source: {axis.agencySource}
- Tone Logic: {axis.toneLogic}
```

---

## Workflow-Specific World Type Guidance

### WorldRulesLogic - By World Type

| World Type | Guidance Focus |
|------------|----------------|
| **Consensus Reality** | Rules are real-world physics and social norms. Focus on what's plausible. No supernatural elements. |
| **Enchanted Reality** | Rules exist but aren't explained. Magic is accepted, not analyzed. Focus on emotional/symbolic truth over mechanics. |
| **Hidden World** | Two rule sets: mundane (what normal people see) and hidden (the real rules). Focus on the masquerade and its maintenance. |
| **Divergent World** | Rules diverged at a specific point. Focus on the "what if?" and its logical consequences. No magic unless genre specifies. |
| **Constructed World** | Author-defined rules. Focus on internal consistency. Sanderson's Laws apply if magic exists. |
| **Mythic World** | Symbolic rules override physical causality. Fate, prophecy, and meaning drive events. Focus on archetypal logic. |
| **Estranged World** | Rules exist but resist human intuition. Focus on alienation, strangeness, cosmic indifference. |
| **Broken World** | Rules have failed or been corrupted. Focus on what collapsed, what survives, and the new harsh logic. |

### WorldHistory - By World Type

| World Type | Guidance Focus |
|------------|----------------|
| **Consensus Reality** | Real history or plausible fictional history. No mythic elements. Focus on social, political, economic forces. |
| **Enchanted Reality** | History blends fact and legend. The impossible may have happened. Focus on stories that shaped belief. |
| **Hidden World** | Two histories: the official record and the secret truth. Focus on hidden interventions, concealed events. |
| **Divergent World** | History diverged at a specific point. Focus on the divergence and its cascading effects. |
| **Constructed World** | Entirely invented history. Focus on founding events, major conflicts, technological/magical shifts. |
| **Mythic World** | History is legend. Gods, heroes, and prophecies are historical facts. Focus on mythic ages and their meaning. |
| **Estranged World** | History may be unknowable or incomprehensible. Focus on what's lost, what can't be understood. |
| **Broken World** | History is "before" and "after" the collapse. Focus on what was lost, what caused the fall, what remains. |

### GeographyCultures - By World Type

| World Type | Guidance Focus |
|------------|----------------|
| **Consensus Reality** | Real-world geography or plausible fictional geography. Cultures based on environment, history, resources. |
| **Enchanted Reality** | Geography may have impossible elements accepted as normal. Cultures incorporate the magical as everyday. |
| **Hidden World** | Geography has hidden layers (wizard enclaves, fairy realms, underground cities). Cultures exist in parallel. |
| **Divergent World** | Geography may differ due to historical changes. Cultures evolved differently from the divergence point. |
| **Constructed World** | Geography is author-created. Cultures should reflect environment, resources, history logically. |
| **Mythic World** | Geography may be symbolic (the Dark Forest, the Shining City). Cultures embody archetypal values. |
| **Estranged World** | Geography may be alien, hostile, or incomprehensible. Cultures are shaped by strangeness. |
| **Broken World** | Geography is scarred, poisoned, or transformed. Cultures are survival-focused, scavenger societies. |

### TechnologyMagic - By World Type

| World Type | Guidance Focus |
|------------|----------------|
| **Consensus Reality** | Technology only, appropriate to time period. No magic. Focus on how tech shapes society. |
| **Enchanted Reality** | Magic exists but isn't systematized. It's mysterious, emotional, intuitive. Soft magic only. |
| **Hidden World** | Magic/tech exists in secret. Focus on how it's concealed and what happens when discovered. |
| **Divergent World** | Technology diverged (steampunk, diesel punk, etc.) or advanced differently. Magic rare or absent. |
| **Constructed World** | Author defines systems. Hard or soft magic per author choice. Focus on rules, limits, costs. |
| **Mythic World** | Magic is providence, divine gift, or fate. It serves story meaning, not mechanics. Soft magic. |
| **Estranged World** | Technology or magic is alien, unsettling, or beyond comprehension. Focus on the disturbing implications. |
| **Broken World** | Technology has failed or caused the collapse. Focus on scarcity, salvage, dangerous remnants. |

---

## Genre as Additional Context

Genre from StoryOverview provides additional guidance layer:

| Genre | Additional Context |
|-------|-------------------|
| **Mystery/Thriller** | World rules must allow for clues, investigation, fair play with reader |
| **Romance** | World rules must create circumstances that bring characters together/apart |
| **Horror** | World rules should enable threat, vulnerability, dread |
| **Literary** | World rules serve thematic exploration over plot mechanics |
| **Adventure** | World rules should enable quests, journeys, challenges |

### Genre + World Type Combinations

Some combinations have specific guidance:

- **Hidden World + Mystery**: The detective discovers the hidden layer; clues must be fair
- **Broken World + Romance**: Love in the ruins; scarcity creates stakes
- **Mythic World + Horror**: Cosmic dread; fate is inescapable
- **Divergent World + Thriller**: "What if?" creates the threat scenario

---

## Output: Genre Recommendations

Collaborator can also suggest genres based on World Type:

| World Type | Commonly Associated Genres |
|------------|---------------------------|
| Consensus Reality | Literary, Mystery, Thriller, Romance, Historical |
| Enchanted Reality | Literary, Magical Realism |
| Hidden World | Urban Fantasy, Paranormal Romance, Secret History |
| Divergent World | Alternate History, Sci-Fi, Thriller |
| Constructed World | Epic Fantasy, Space Opera, Adventure |
| Mythic World | Epic Fantasy, Myth Retelling, Literary Fantasy |
| Estranged World | Horror, Hard SF, New Weird, Literary |
| Broken World | Post-Apocalyptic, Dystopian, Horror, Survival |

---

## Implementation Notes

### Prompt Structure

Each workflow prompt would be modified to include:

```
## World Type Context

The user has selected World Type: **{WorldType}**
The story's genre is: **{Genre}**

For this World Type, when helping with {workflow topic}:
{World-Type-specific guidance from tables above}

Keep this context in mind as you help the user develop their world.
```

### Data Flow

```
StoryWorld.WorldType (gestalt)
        ↓
    [lookup]
        ↓
Axis values + World-Type-specific guidance
        ↓
    [combine with]
        ↓
StoryOverview.Genre
        ↓
    [inject into]
        ↓
Workflow prompt context
```

### New Workflows to Consider

Beyond updating the 4 existing workflows, the 8 taxonomy categories suggest potential new workflows:

| Taxonomy Category | Existing Workflow? | Notes |
|-------------------|-------------------|-------|
| Physical World | GeographyCultures (partial) | Could expand or split |
| Culture | GeographyCultures (partial) | Could expand or split |
| History | WorldHistory | Exists |
| Government | None | New workflow needed |
| Religion | None | New workflow needed |
| Economy | None | New workflow needed |
| Magic/Technology | TechnologyMagic | Exists |
| People/Species | None | New workflow needed |

---

## Sequencing

1. **Issue #782** - Implement StoryWorld element in StoryCAD
   - World Type dropdown (8 gestalts)
   - 8 taxonomy category tabs with RTF fields
   - Data model stores gestalt + axis values

2. **Future Issue** - Update Collaborator workflows
   - Modify 4 existing World Model workflows to be World-Type-aware
   - Consider new workflows for uncovered taxonomy categories
   - Add Genre reading and recommendation capability

---

*Design notes created: 2026-01-19*
*This is future work, not part of Issue #782*
