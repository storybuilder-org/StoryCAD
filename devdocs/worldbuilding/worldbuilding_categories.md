# Worldbuilding Categories -- Human-First UI Pattern

## Core Principle

Writers think in **gestalts**, not axes.\
The UI should ask for *one meaningful choice*, then hide complexity
unless explicitly requested.

------------------------------------------------------------------------

## Core Pattern: World Type → Reveal Details

### 1. Primary Control (Writer-Facing)

**World Type** *(required, single choice)*

Presented as large labeled cards or a simple dropdown.

-   **Consensus Reality**\
    The world works as expected.

-   **Enchanted Reality**\
    Our world, but reality is porous.

-   **Hidden World**\
    Our world plus concealed layers.

-   **Divergent World**\
    History or conditions forked.

-   **Constructed World**\
    A fully invented reality.

-   **Mythic World**\
    Reality follows meaning over causality.

-   **Estranged World**\
    The world is unfamiliar and unsettling.

-   **Broken World**\
    Systems have failed; survival dominates.

Only this decision is required.

------------------------------------------------------------------------

### 2. Auto-Configured Defaults (System Behavior)

Selecting a World Type automatically sets:

-   Ontology
-   World Relation
-   Rule Transparency
-   Scale of Difference
-   Tone Logic

These values are hidden initially.

------------------------------------------------------------------------

### 3. Optional Disclosure

A subtle control invites refinement without pressure:

> ▸ Adjust world logic (advanced)

Collapsed by default.

------------------------------------------------------------------------

### 4. Advanced Panel (Progressive Disclosure)

Displayed only if expanded.\
Language is narrative-focused, not technical.

#### Reality Rules

How consistent are the rules?

-   Fixed and knowable
-   Consistent but mysterious
-   Symbolic or dreamlike

#### Relation to Our World

This story takes place in:

-   Our world
-   A changed version of our world
-   A separate world entirely
-   Multiple connected worlds

#### Source of Power

What truly drives change?

-   People
-   Institutions or systems
-   Supernatural forces
-   Fate or prophecy

Each control defaults to the selected World Type.

------------------------------------------------------------------------

### 5. Visual Feedback

A read-only summary reassures the writer:

> **World Profile:**\
> A constructed, myth-driven world with explicit rules and cosmological
> differences.

This confirms intent without exposing mechanics.

------------------------------------------------------------------------

## Design Rationale

-   One primary decision
-   Zero mandatory complexity
-   Advanced control is optional and reversible
-   No genre debates
-   Full expressiveness retained internally

------------------------------------------------------------------------

## StoryCAD Integration Notes

-   Store **World Type** as a first-class field
-   Store detailed dimensions as derived defaults
-   Allow overrides with a reset-to-default option
-   Scenes may reference but not modify the world profile
