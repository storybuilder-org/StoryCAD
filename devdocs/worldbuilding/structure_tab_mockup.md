# Structure Tab Redesign Mockup

**Created:** 2026-01-20
**Purpose:** Visual mockup of simplified Structure tab UI

---

## Current Design (Problematic)

```
┌─────────────────────────────────────────────────────────────────┐
│  World Type: [ Hidden World          ▼]                         │
│                                                                 │
│  ▶ Advanced: Axis Values                                        │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ Ontology:           [ Supernatural        ▼]                ││
│  │ World Relation:     [ Layered             ▼]                ││
│  │ Rule Transparency:  [ Explicit Rules      ▼]                ││
│  │ Scale of Difference:[ Structural          ▼]                ││
│  │ Agency Source:      [ Nonhuman Intelligen ▼]  <- cut off    ││
│  │ Tone Logic:         [ Rational            ▼]                ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘

Problems:
- "Hidden World" is unexplained
- All axis values are cryptic jargon
- User doesn't know why any of this matters
- Axis values get cut off
- No connection to anything the user recognizes
```

---

## Proposed Design: Option A (Description Panel)

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  What kind of world is your story set in?                       │
│                                                                 │
│  World Type: [ Hidden World                              ▼]     │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                                                             ││
│  │  HIDDEN WORLD                                               ││
│  │                                                             ││
│  │  Our world, but with concealed magical or supernatural      ││
│  │  layers. The "real" world operates normally, but beneath    ││
│  │  or alongside it exists a secret realm. Discovery of this   ││
│  │  hidden layer is often dangerous or comes at a cost.        ││
│  │                                                             ││
│  │  Examples:                                                  ││
│  │  • Harry Potter - Wizarding world hidden from Muggles       ││
│  │  • Dresden Files - Magic hidden in modern Chicago           ││
│  │  • American Gods - Old gods living among us                 ││
│  │  • The Matrix - Simulated reality concealing the real world ││
│  │  • Men in Black - Aliens hidden among humans                ││
│  │                                                             ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

Behavior:
- Description panel updates when World Type selection changes
- Axis values auto-populated internally (never shown)
- Clean, educational, actionable
```

---

## Proposed Design: Option B (Rich ComboBox Items)

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  What kind of world is your story set in?                       │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ Hidden World                                            ▼   ││
│  │ Our world + concealed layers (Harry Potter, Dresden Files)  ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  ┌─ Dropdown expanded ─────────────────────────────────────────┐│
│  │                                                             ││
│  │  Consensus Reality                                          ││
│  │  The real world as we know it (literary fiction, thrillers) ││
│  │  ───────────────────────────────────────────────────────    ││
│  │  Enchanted Reality                                          ││
│  │  Our world where magic is accepted (magical realism)        ││
│  │  ───────────────────────────────────────────────────────    ││
│  │  Hidden World                                        ✓      ││
│  │  Our world + concealed layers (Harry Potter, Dresden Files) ││
│  │  ───────────────────────────────────────────────────────    ││
│  │  Divergent World                                            ││
│  │  History forked from ours (alternate history, cyberpunk)    ││
│  │  ───────────────────────────────────────────────────────    ││
│  │  Constructed World                                          ││
│  │  Entirely built reality (epic fantasy, space opera)         ││
│  │  ───────────────────────────────────────────────────────    ││
│  │  Mythic World                                               ││
│  │  Reality follows meaning, not causality (Tolkien, myths)    ││
│  │  ───────────────────────────────────────────────────────    ││
│  │  Estranged World                                            ││
│  │  Unfamiliar, unsettling reality (Lovecraft, New Weird)      ││
│  │  ───────────────────────────────────────────────────────    ││
│  │  Broken World                                               ││
│  │  Systems have failed (post-apocalyptic, dystopian)          ││
│  │                                                             ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

Behavior:
- Each dropdown item has title + one-line description + examples
- Selection shows the full item (both lines)
- Compact but informative
```

---

## Proposed Design: Option C (Combined - RECOMMENDED)

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  What kind of world is your story set in?                       │
│                                                                 │
│  World Type: [ Hidden World - Secret layers (Harry Potter) ▼]   │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                                                             ││
│  │  Our world, but with concealed magical or supernatural      ││
│  │  layers beneath or alongside normal reality. Discovery      ││
│  │  of the hidden layer is often dangerous or costly.          ││
│  │                                                             ││
│  │  Works in this category:                                    ││
│  │  Harry Potter • Dresden Files • American Gods • Percy       ││
│  │  Jackson • The Matrix • Men in Black • Neverwhere           ││
│  │                                                             ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ ℹ️ This classification helps you think about your world's   ││
│  │    relationship to reality. If you use the Collaborator     ││
│  │    AI assistant, it will tailor its worldbuilding           ││
│  │    suggestions based on your selection.                     ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

Features:
- ComboBox shows abbreviated description in selection
- Panel below shows full description + examples
- Info text explains WHY this matters (Collaborator connection)
- No axis values visible anywhere
- Clean, educational, purposeful
```

---

## All 8 World Type Descriptions (for implementation)

### 1. Consensus Reality
**Short:** The real world as we know it
**Full:** The world operates exactly as expected - no magic, no hidden layers, no alternate physics. "Consensus" refers to a specific group or subculture whose reality you're depicting. Every consensus reality story still requires worldbuilding the norms, rules, and insider knowledge of that particular slice of life.
**Examples:** 87th Precinct, Harry Bosch, Rabbit series, Grisham novels, Big Little Lies

### 2. Enchanted Reality
**Short:** Our world where magic is accepted without explanation
**Full:** Our world, but reality is porous. The impossible happens and is accepted rather than analyzed. Magic or the supernatural exists but isn't systematized - it simply is. This is the realm of magical realism and slipstream fiction.
**Examples:** One Hundred Years of Solitude, Like Water for Chocolate, Beloved, Pan's Labyrinth

### 3. Hidden World
**Short:** Our world + concealed supernatural layers
**Full:** Our world, but with concealed magical or supernatural layers beneath or alongside normal reality. The "mundane" world operates normally, but a secret realm exists that most people don't know about. Discovery of this hidden layer is often dangerous or comes at a cost.
**Examples:** Harry Potter, Dresden Files, American Gods, The Matrix, Men in Black, Percy Jackson

### 4. Divergent World
**Short:** History forked from ours
**Full:** Our world, but history or conditions diverged at some point. The rules of reality remain rational and logical, but society, technology, or events developed differently. This includes alternate history, steampunk, cyberpunk, and near-future speculation.
**Examples:** The Man in the High Castle, 11/22/63, Neuromancer, The Handmaid's Tale

### 5. Constructed World
**Short:** Entirely built reality (fantasy, space opera)
**Full:** A fully invented reality with its own geography, history, peoples, and rules. The world doesn't derive from Earth at all - it's built from scratch. This is the realm of epic fantasy and secondary-world science fiction. The author defines everything.
**Examples:** A Song of Ice and Fire, Discworld, Dune, Star Wars, The Stormlight Archive

### 6. Mythic World
**Short:** Reality follows meaning, not causality
**Full:** A world where narrative meaning matters more than physical causality. Fate, prophecy, and archetypes drive events. Things happen because they're meaningful, not because of cause and effect. Gods and destiny are real forces.
**Examples:** The Lord of the Rings, Earthsea, The Chronicles of Narnia, Circe, The Once and Future King

### 7. Estranged World
**Short:** Unfamiliar, unsettling reality
**Full:** A world that feels fundamentally alien or wrong. Rules may exist, but they resist human intuition. The familiar becomes strange. This is the realm of cosmic horror, New Weird, and hard SF that emphasizes how truly alien the universe can be.
**Examples:** Solaris, Annihilation, Perdido Street Station, Blindsight, 2001: A Space Odyssey

### 8. Broken World
**Short:** Systems have failed (post-apocalyptic, dystopian)
**Full:** A world where civilization, environment, or social order has collapsed or been corrupted. Survival replaces progress. Resources are scarce, institutions have failed, and the focus is on enduring rather than building. Includes post-apocalyptic and dystopian settings.
**Examples:** The Road, Mad Max, 1984, The Walking Dead, Station Eleven, A Canticle for Leibowitz

---

## Implementation Notes

### Data Flow
1. User selects World Type from dropdown
2. ViewModel looks up axis values from mapping table
3. Axis values stored in Model (WorldType, Ontology, WorldRelation, etc.)
4. UI never displays axis values
5. Collaborator reads axis values when generating worldbuilding guidance

### XAML Approach
- Use standard ComboBox with custom ItemTemplate for rich items
- Use TextBlock below for description (bound to ViewModel property that updates on selection)
- Use InfoBar or similar for the "why this matters" explanation
- Remove the Expander with axis dropdowns entirely

### ViewModel Changes
- Add `WorldTypeDescription` property (updates when WorldType changes)
- Add `WorldTypeExamples` property
- Add private method to auto-populate axis values from World Type
- Remove axis properties from UI binding (keep in Model for persistence)

---

*Mockup created: 2026-01-20*
