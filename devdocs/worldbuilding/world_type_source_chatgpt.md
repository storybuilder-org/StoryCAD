# World Type Framework - ChatGPT Source

**Source:** ChatGPT conversation with Terry Cox
**Date:** Prior to 2026-01-19
**Purpose:** Original development of the World Type taxonomy used in worldbuilding_categories.md

---

## Conversation Record

### Prompt 1

> Are there categories of fictional worlds? I'm thinking of things like magical realism, alternate histories, Tolkienesque fantasy.

### Response 1 - Initial Classification

Yes. Commonly used categories of fictional worlds (grouped by how they diverge from consensus reality) include:

#### 1. Real-World Adjacent

- **Realist** – No speculative elements.
- **Magical Realism** – Reality plus unexplained magic, treated as normal.
- **Slipstream** – Subtle, disorienting reality shifts.

#### 2. Speculative Variants of Earth

- **Alternate History** – Historical divergence.
- **Secret History** – Hidden magic/forces shaping real history.
- **Near-Future / Soft SF** – Plausible extrapolations.

#### 3. Secondary Worlds

- **High / Epic Fantasy (Tolkienesque)** – Fully separate world, deep history, mythic scope.
- **Low Fantasy** – Secondary world, limited or rare magic.
- **Sword & Sorcery** – Localized, personal stakes.

#### 4. Science-Based Worlds

- **Hard Science Fiction** – Physics-constrained.
- **Space Opera** – Galactic scale, soft science.
- **Cyberpunk / Post-Cyberpunk**
- **Post-Apocalyptic / Dystopian**

#### 5. Hybrid / Boundary Worlds

- **Science Fantasy** – Magic and tech coexist.
- **Portal Fantasy** – Travel between worlds.
- **Mythic / Legendary Worlds** – Operate on symbolic or archetypal logic.

#### 6. Ontologically Strange

- **Multiverse**
- **Virtual / Simulated Worlds**
- **Dream / Afterlife Realms**
- **Absurdist / Surreal**

> If you want, I can collapse these into a small, orthogonal taxonomy suitable for StoryCAD world-building (e.g., axes instead of buckets).

---

### Response 2 - Orthogonal Taxonomy (6 Axes)

Here's a compact, orthogonal taxonomy you can use directly for world-building (and software modeling). Each world is defined by values on a few independent axes.

#### 1. Ontology (What exists)

- **Mundane** – Only consensus reality.
- **Supernatural** – Magic, gods, spirits exist.
- **Scientific Speculative** – Non-current but naturalistic phenomena.
- **Hybrid** – Magic and science both operative.
- **Symbolic** – Reality follows mythic/dream logic.

#### 2. World Relation (Relation to our reality)

- **Primary World** – Our world.
- **Divergent Earth** – Alternate / secret history.
- **Secondary World** – Fully separate reality.
- **Layered** – Hidden realms overlapping ours.
- **Multiversal** – Many coexisting worlds.

#### 3. Rule Transparency (How rules behave)

- **Explicit Rules** – Knowable, consistent systems.
- **Implicit Rules** – Consistent but unexplained.
- **Capricious** – Inconsistent or will-based.
- **Symbolic Rules** – Meaning overrides causality.

#### 4. Scale of Difference

- **Cosmetic** – Surface changes only.
- **Structural** – Social, historical, technological shifts.
- **Cosmological** – Different laws of reality.

#### 5. Agency Source (What drives change)

- **Human-Centric**
- **Nonhuman Intelligences**
- **Fate / Providence**
- **Systemic Forces** (technology, magic, ecology)

#### 6. Tone Logic (How the world feels)

- **Rational**
- **Mythic**
- **Whimsical**
- **Dark / Entropic**
- **Transcendent**

#### Examples (compressed)

**Magical Realism**
- Ontology: Supernatural
- Relation: Primary World
- Rules: Implicit
- Scale: Cosmetic
- Tone: Symbolic

**Tolkienesque Fantasy**
- Ontology: Supernatural
- Relation: Secondary World
- Rules: Explicit
- Scale: Cosmological
- Tone: Mythic

**Alternate History**
- Ontology: Mundane
- Relation: Divergent Earth
- Rules: Explicit
- Scale: Structural
- Tone: Rational

---

### Prompt 2

> I can see the orthogonal taxonomy would be useful to software because you have multiple choices to make, but I don't see it as quite so useful for a human agent because there are multiple choices to make.

---

### Response 3 - Human-Centered Gestalts (8 World Types)

That's a fair critique—and you're right.

For humans, orthogonal axes impose cognitive load. We don't naturally think "dimensionally"; we think in gestalts. So the taxonomy needs a collapsed layer that maps multiple axes into a single recognizable pattern.

Here's the human-usable version.

#### Human-Centered World Categories (Gestalts)

Each category is a stable bundle of assumptions, not choices.

**1. Consensus Reality**
The world works as expected.
No hidden rules.
→ Literary realism, most mainstream fiction.

**2. Enchanted Reality**
Our world, but reality is porous.
The impossible is accepted, not analyzed.
→ Magical realism, slipstream.

**3. Hidden World**
Our world + concealed layers.
Discovery is dangerous or costly.
→ Urban fantasy, secret history.

**4. Divergent World**
History or conditions forked.
Logic remains rational.
→ Alternate history, near-future SF.

**5. Constructed World**
Entirely built reality.
Coherent internal logic.
→ Epic fantasy, secondary-world SF.

**6. Mythic World**
Reality follows meaning, not causality.
Fate, prophecy, archetypes dominate.
→ Tolkien, Le Guin (Earthsea), myth retellings.

**7. Estranged World**
Reality is unfamiliar and unsettling.
Rules exist but resist intuition.
→ Hard SF, surrealism, New Weird.

**8. Broken World**
Systems have failed.
Survival replaces progress.
→ Post-apocalyptic, dystopian.

#### Why this works for humans

- **One decision, not many**
- Each category answers: "What assumptions do I carry into this story?"
- Categories are mentally sticky
- Authors can move between them deliberately

The orthogonal model still matters—but as the underlying structure.

---

### Response 4 - Practical Synthesis

**Practical synthesis (this is the key insight)**

- Writers select: a world type
- Software stores: axis values
- UI shows: one labeled choice
- Advanced mode: exposes axes only if needed

> If you want, next I can:
> - Show how each gestalt maps to the axes, or
> - Propose a StoryCAD UI pattern that hides complexity until needed.

---

## Key Design Insight

The World Type framework operates on two layers:

| Layer | Purpose | User |
|-------|---------|------|
| **Gestalt Layer** | 8 recognizable world types | Human writers |
| **Axis Layer** | 6 orthogonal dimensions | Software/AI |

Writers choose one gestalt. Software decomposes it into axis values. Advanced users can adjust axes if needed.

---

## Application to StoryCAD

Per discussion on 2026-01-19:

1. **Genre/sub-genre**: Already handled by existing Genre control in StoryOverview. StoryWorld could recommend sub-genres that align with selected World Type.

2. **Examples**: Concrete examples (especially series/shared worlds) help clarify abstract categories:
   - Harry Potter (Hidden World)
   - Lord of the Rings (Mythic World / Constructed World)
   - Updike's Rabbit series (Consensus Reality)
   - 87th Precinct novels (Consensus Reality)
   - Jesse Stone movies (Consensus Reality)

---

*Document created: 2026-01-19*
