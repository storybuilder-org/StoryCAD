# StoryWorld Data Model Research

**Created:** 2026-01-19
**Purpose:** Research competitor field structures and craft sources to inform StoryWorldModel properties

---

## Sources Consulted

### Competitor Software
- [World Anvil Templates](https://www.worldanvil.com/learn/article-templates) - 25+ article templates with specific field prompts
- [Campfire Writing Modules](https://www.campfirewriting.com/worldbuilding-tools) - 17 modules with panel-based fields
- [Campfire Writing Review](https://selfpublishing.com/campfire-writing-review/) - Module structure details

### Craft Books
- **"World-Building"** by Stephen L. Gillett (Science Fiction Writing Series, ed. Ben Bova) - 8 chapters on SF worldbuilding from astronomy to planetary systems
- **"The Guide to Writing Fantasy and Science Fiction"** by Philip Athans - Worldbuilding sections
- **"Writing Fantasy and Science Fiction"** by Orson Scott Card, Philip Athans, Jay Lake (per user)

### Internal Research
- `taxonomies_research.md` - 25+ frameworks synthesized into 8 primary categories

---

## Competitor Field Analysis

### World Anvil Template Fields

World Anvil organizes by **entity type** (Geography, Species, Character, Tradition, etc.). Each template has tabbed prompts.

#### Geography Template
| Tab | Fields |
|-----|--------|
| Generic | Geography description, Ecosystem, Ecosystem Cycles, Parent Location |
| Extended | Climate, Natural Resources, Flora, Fauna |
| Features | Notable landmarks, Points of interest |

#### Species Template
| Tab | Fields |
|-----|--------|
| Generic | Origin, Anatomy, Biological Cycle |
| Extended | Average Lifespan, Intelligence, Height, Weight |
| Sapience | Social Structure, History, Traditions |
| Culture | Naming Traditions, Beauty Ideals, Gender Roles, Taboos |
| Customs | Rituals (eating, coming of age, death, birth, greetings), Way of Life |

#### Tradition/Culture Template
| Field | Purpose |
|-------|---------|
| Celebration/Festival | What is celebrated |
| Superstitions | Beliefs |
| Dress Codes | Appearance norms |
| Customs | Repeated behaviors |
| Rituals | Formal practices |

#### Character Template (for reference)
| Tab | Fields |
|-----|--------|
| Personal | Motivations, Likes/Dislikes, Virtues, Flaws, Legacy |
| Social | Wealth, Family Ties, Friendships, Hobbies |
| Divine | Domain, Religious Organizations (for deities) |

### Campfire Writing Module Fields

Campfire organizes by **module** with customizable **panels** within each.

| Module | Purpose | Typical Fields |
|--------|---------|----------------|
| **Cultures** | Communities, traditions | Customs, Traditions, Social Structures, Daily Life, Interactions |
| **Religions** | Belief systems | Deities, Rituals, Beliefs, Sacred Texts, Evolution, Influence |
| **Species** | Flora/fauna | Appearance, Behavior, Habitat, Interactions |
| **Magic** | Power systems | Source, Cost, Limitations, History, Mechanics, Rituals |
| **Languages** | Communication | Script/Symbols, Dictionary, Grammar |
| **Philosophies** | Ideas/ethics | Ethics, Morality, Self, Impact |
| **Items** | Objects | Properties, History, Significance |
| **Locations** | Places | Images, Histories, Stats |
| **Systems** | Custom frameworks | Flowcharts, Relationships |

### Campfire Magic System Default Template
| Field | Purpose |
|-------|---------|
| Magic Source | Where power comes from |
| Cost | What using it requires |
| Limitations | What it can't do |
| History | How it developed |

---

## Craft Book Categories

### Gillett's "World-Building" (SF Focus) - 8 Chapters

1. **Why World-Build?** - Purpose and necessity
2. **The Astronomical Setting** - Planets, stars, gravity, orbits, seasons, tides
3. **Making a Planet** - Planetary system formation, options
4. **The Earth** - Plate tectonics, water, air, magnetic field, colors
5. **The Ancient Earth** - Historical Earth as inspiration (avoiding "Cenozoic Earth Syndrome")
6. **The Other Planets** - Deep space probe data as inspiration
7. **Stars and Suns** - How heavenly bodies affect worldbuilding
8. **Hypothetical Worlds** - Exotic possibilities (chloroxygen worlds, etc.)

**Note:** Very science-heavy, includes formulas and tables. Best for hard SF.

### Taxonomy Research Synthesis (from taxonomies_research.md)

**Primary Categories (80%+ frequency across 25+ sources):**

| Category | Subcategories |
|----------|---------------|
| **Physical World** | Terrain, Climate, Natural Features, Flora, Fauna, Resources |
| **People/Species** | Inhabitants, Physical Traits, Diversity, Evolution |
| **Culture** | Values, Beliefs, Art, Entertainment, Daily Practices |
| **History** | Major Events, Wars, Eras, Founding Myths |
| **Government/Politics** | Leadership, Laws, Crime, Foreign Relations, Class |
| **Religion/Spirituality** | Deities, Practices, Rituals, Creation Stories |
| **Economy** | Currency, Wealth Distribution, Professions, Trade Routes |
| **Magic/Technology** | Rules, Limitations, Sources, Practitioners, Advancement |

---

## Proposed StoryWorldModel Properties

### Design Decisions

**List-based tabs (repeatable entries) - 5 tabs:**
- Physical World (for portal stories, space opera, multi-world settings like The Expanse)
- People/Species (multiple races, species, peoples)
- Cultures (a world can have multiple cultures/milieus)
- Governments (multiple nations, factions, power structures)
- Religions (multiple belief systems)

**Single-entry tabs - 4 tabs:**
- Structure (World Type classification)
- History (the world's history)
- Economy (global economic context)
- Magic/Technology (combined per Clarke's Law: "Any sufficiently advanced technology is indistinguishable from magic")

**UI Pattern for Lists:**
- (+) button to add new entry
- ListView or Accordion to display entries
- Each entry has a Name/Label plus its fields
- List tabs start empty; user adds entries as needed
- Nested tabs possible: major tabs horizontal, field groups vertical (designer's discretion)

**UX Requirement: Reduce Cognitive Load**

Many fields have ambiguous labels. Use XAML's `PlaceholderText` and `TeachingTip` to provide context:

- **PlaceholderText**: Hint text inside empty fields (disappears when user types)
- **TeachingTip**: Popup explanation for unfamiliar terms

Example guidance for selected fields:

| Field | PlaceholderText | TeachingTip |
|-------|-----------------|-------------|
| WealthDistribution | "Who has money? Who doesn't? Why?" | "Describe how wealth is distributed in your world - are there distinct classes? Is there mobility between them?" |
| Taboos | "What is never done or spoken of?" | "Every culture has forbidden acts or topics. What would shock or horrify members of this culture?" |
| Limitations | "What can't this system do?" | "The most interesting magic/tech has clear limits. What problems CAN'T it solve?" |
| Cost | "What does using this require?" | "Power has a price - energy, materials, health, sanity, moral compromise. What's the cost here?" |
| Values | "What matters most to these people?" | "Core beliefs that drive behavior - honor, family, wealth, knowledge, faith, survival?" |
| ForeignRelations | "Allies? Enemies? Neutral parties?" | "How does this government interact with others? Trade partners, rivals, vassals, threats?" |

**Every RTF field should have PlaceholderText at minimum.** TeachingTips for less obvious terms.

**Settings Linkage:**
- No explicit linkage needed between Setting story elements and StoryWorld
- One StoryWorld per StoryModel; every Setting is implicitly "in" that world

---

### Structure Tab (World Type Classification)

```csharp
// World Type Gestalt (user-facing)
public string WorldType { get; set; }  // 8 options

// Axis Values (stored for Collaborator use)
public string Ontology { get; set; }           // 5 options
public string WorldRelation { get; set; }      // 5 options
public string RuleTransparency { get; set; }   // 4 options
public string ScaleOfDifference { get; set; }  // 3 options
public string AgencySource { get; set; }       // 4 options
public string ToneLogic { get; set; }          // 5 options
```

*Note: Milieu concept is now captured via the Cultures list - each culture IS a milieu.*

---

### Physical World Tab (List)

```csharp
public List<PhysicalWorldModel> PhysicalWorlds { get; set; }

public class PhysicalWorldModel
{
    public string Name { get; set; }             // e.g., "Earth", "Mars", "The Upside Down", "Narnia"
    public string Geography { get; set; }        // RTF - Terrain, landforms, bodies of water
    public string Climate { get; set; }          // RTF - Weather patterns, seasons
    public string NaturalResources { get; set; } // RTF - What's available, scarce
    public string Flora { get; set; }            // RTF - Plant life
    public string Fauna { get; set; }            // RTF - Animal life
    public string Astronomy { get; set; }        // RTF - Moons, stars, celestial features
}
```

*Note: For single-world stories, user creates one entry. For portal/space opera, multiple entries.*
*Specific locations within a world are handled by Setting story elements.*

---

### People/Species Tab (List)

```csharp
public List<SpeciesModel> Species { get; set; }

public class SpeciesModel
{
    public string Name { get; set; }             // e.g., "Elves", "Dothraki", "Police Officers"
    public string PhysicalTraits { get; set; }   // RTF - Appearance, abilities
    public string Lifespan { get; set; }         // RTF - How long they live
    public string Origins { get; set; }          // RTF - Where they come from
    public string SocialStructure { get; set; }  // RTF - How they organize
    public string Diversity { get; set; }        // RTF - Variation within the group
}
```

---

### Cultures Tab (List)

```csharp
public List<CultureModel> Cultures { get; set; }

public class CultureModel
{
    public string Name { get; set; }             // e.g., "Stark North", "Wall Street", "Wizarding World"
    public string Values { get; set; }           // RTF - What matters to these people
    public string Customs { get; set; }          // RTF - How they greet, marry, mourn
    public string Taboos { get; set; }           // RTF - What's forbidden
    public string Art { get; set; }              // RTF - Aesthetic expression
    public string DailyLife { get; set; }        // RTF - Food, clothing, housing, routines
    public string Entertainment { get; set; }    // RTF - Leisure, sports, games
}
```

*Note: For Consensus Reality, each Culture entry represents a milieu (social environment).*

---

### History Tab (Single)

```csharp
public string FoundingEvents { get; set; }   // RTF - Origin of current order
public string MajorConflicts { get; set; }   // RTF - Wars, struggles that shaped the world
public string Eras { get; set; }             // RTF - Major periods/ages
public string TechnologicalShifts { get; set; } // RTF - Changes in capability
public string LostKnowledge { get; set; }    // RTF - What's been forgotten/destroyed
```

---

### Governments Tab (List)

```csharp
public List<GovernmentModel> Governments { get; set; }

public class GovernmentModel
{
    public string Name { get; set; }             // e.g., "The Iron Throne", "NYPD Hierarchy", "The Ministry of Magic"
    public string Type { get; set; }             // RTF - How power is organized (monarchy, democracy, etc.)
    public string PowerStructures { get; set; }  // RTF - Who has power, how it's maintained
    public string Laws { get; set; }             // RTF - Legal system, punishments
    public string ClassStructure { get; set; }   // RTF - Social hierarchy
    public string ForeignRelations { get; set; } // RTF - Alliances, enemies, diplomacy
}
```

---

### Religions Tab (List)

```csharp
public List<ReligionModel> Religions { get; set; }

public class ReligionModel
{
    public string Name { get; set; }             // e.g., "Faith of the Seven", "R'hllor", "Catholicism"
    public string Deities { get; set; }          // RTF - Gods, spirits, divine beings
    public string Beliefs { get; set; }          // RTF - Cosmology, afterlife, purpose
    public string Practices { get; set; }        // RTF - Rituals, worship, observances
    public string Organizations { get; set; }    // RTF - Churches, temples, clergy
    public string CreationMyths { get; set; }    // RTF - How the world began (per this religion)
}
```

---

### Economy Tab (Single)

```csharp
public string EconomicSystem { get; set; }   // RTF - How economy works (capitalism, barter, etc.)
public string Currency { get; set; }         // RTF - Money, exchange medium
public string TradeRoutes { get; set; }      // RTF - How goods move
public string Professions { get; set; }      // RTF - Common jobs, guilds
public string WealthDistribution { get; set; } // RTF - Rich/poor, who has what
```

---

### Magic/Technology Tab (Single)

```csharp
public string SystemType { get; set; }       // Magic, Technology, Both, or Neither
public string Source { get; set; }           // RTF - Where power comes from
public string Rules { get; set; }            // RTF - How it works mechanically
public string Limitations { get; set; }      // RTF - What it can't do
public string Cost { get; set; }             // RTF - What using it requires
public string Practitioners { get; set; }    // RTF - Who can use it, why
public string SocialImpact { get; set; }     // RTF - How it affects society
```

*Note: Magic and Technology combined per Clarke's Law.*

---

## Property Count Summary

| Tab | Type | Model Fields | Notes |
|-----|------|--------------|-------|
| Structure | Single | 7 | 1 gestalt + 6 axes |
| Physical World | **List** | 7 per entry | Name + 6 RTF (for multi-world stories) |
| People/Species | **List** | 6 per entry | Name + 5 RTF |
| Cultures | **List** | 7 per entry | Name + 6 RTF |
| History | Single | 5 | World's timeline |
| Governments | **List** | 6 per entry | Name + 5 RTF |
| Religions | **List** | 6 per entry | Name + 5 RTF |
| Economy | Single | 5 | Global economics |
| Magic/Technology | Single | 7 | Combined system |

**Single-entry tabs:** 4 tabs (Structure, History, Economy, Magic/Technology) - ~24 fields
**List-entry tabs:** 5 tabs (Physical World, People/Species, Cultures, Governments, Religions) - ~32 fields per entry type

---

## Resolved Questions

1. ~~Should Milieu field be Consensus Reality-specific?~~ → Milieu IS Culture; use Cultures list for all World Types
2. ~~Should Magic/Technology be two separate tabs?~~ → No, combined per Clarke's Law
3. ~~Which categories should be lists?~~ → Physical World, People/Species, Cultures, Governments, Religions (5 total)
4. ~~Which should be single?~~ → Structure, History, Economy, Magic/Technology (4 total)
5. ~~Field granularity?~~ → 5-7 fields per entry is fine
6. ~~Settings linkage?~~ → No explicit link needed; one StoryWorld per StoryModel, all Settings implicitly belong
7. ~~Default entries?~~ → List tabs start empty; user clicks (+) to add entries

## Remaining Questions (For UI Designer Agent)

1. **Layout approach**: Nested tabs? Accordion? Other pattern for list entries with multiple fields?
2. **Tab order**: Structure tab first (user picks World Type before anything else). Remaining 8 tabs ordered once UI is visible/testable.

---

## Sources

- [World Anvil Geography Template](https://www.worldanvil.com/learn/article-templates/geography)
- [World Anvil Species Template](https://www.worldanvil.com/learn/article-templates/species)
- [Campfire Worldbuilding Tools](https://www.campfirewriting.com/worldbuilding-tools)
- [Campfire Writing Review - 17 Modules](https://selfpublishing.com/campfire-writing-review/)
- [World-Building by Stephen Gillett (Amazon)](https://www.amazon.com/World-Building-Science-Fiction-Writing-Stephen/dp/0898797071)
- [Mythcreants - Five Books on Writing Fantasy](https://mythcreants.com/blog/five-books-on-writing-fantasy-compared/)

---

*Research compiled: 2026-01-19*
