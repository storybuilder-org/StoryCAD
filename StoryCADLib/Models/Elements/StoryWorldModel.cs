using System.Text.Json.Serialization;
using StoryCADLib.Models.StoryWorld;

namespace StoryCADLib.Models;

/// <summary>
/// Model for the StoryWorld story element.
/// Contains worldbuilding information organized by category.
/// Single instance per story (like StoryOverview), but optional.
/// </summary>
public class StoryWorldModel : StoryElement
{
    #region Structure Tab Properties (World Type Classification)

    /// <summary>
    /// High-level world classification (gestalt). One of 8 types:
    /// Consensus Reality, Enchanted Reality, Hidden World, Divergent World,
    /// Constructed World, Mythic World, Estranged World, Broken World.
    /// Selecting a World Type auto-populates the 6 axis values below.
    /// </summary>
    [JsonIgnore] private string _worldType = string.Empty;

    [JsonInclude]
    [JsonPropertyName("WorldType")]
    public string WorldType { get => _worldType; set => _worldType = value; }

    /// <summary>
    /// What exists in this world? (Mundane, Supernatural, Scientific Speculative, Hybrid, Symbolic)
    /// Determines whether magic, advanced tech, or only real-world elements exist.
    /// </summary>
    [JsonIgnore] private string _ontology = string.Empty;

    [JsonInclude]
    [JsonPropertyName("Ontology")]
    public string Ontology { get => _ontology; set => _ontology = value; }

    /// <summary>
    /// Relationship to our reality. (Primary World, Divergent Earth, Secondary World, Layered, Multiversal)
    /// Is this our world, an alternate version, or entirely fictional?
    /// </summary>
    [JsonIgnore] private string _worldRelation = string.Empty;

    [JsonInclude]
    [JsonPropertyName("WorldRelation")]
    public string WorldRelation { get => _worldRelation; set => _worldRelation = value; }

    /// <summary>
    /// How the world's rules behave. (Explicit Rules, Implicit Rules, Capricious, Symbolic Rules)
    /// Are the rules clear and consistent, or mysterious and unpredictable?
    /// </summary>
    [JsonIgnore] private string _ruleTransparency = string.Empty;

    [JsonInclude]
    [JsonPropertyName("RuleTransparency")]
    public string RuleTransparency { get => _ruleTransparency; set => _ruleTransparency = value; }

    /// <summary>
    /// How different from reality. (Cosmetic, Structural, Cosmological)
    /// Surface-level changes vs. fundamental differences in how the world works.
    /// </summary>
    [JsonIgnore] private string _scaleOfDifference = string.Empty;

    [JsonInclude]
    [JsonPropertyName("ScaleOfDifference")]
    public string ScaleOfDifference { get => _scaleOfDifference; set => _scaleOfDifference = value; }

    /// <summary>
    /// What drives change in the world. (Human-Centric, Nonhuman Intelligences, Fate/Providence, Systemic Forces)
    /// Who or what has power to affect events?
    /// </summary>
    [JsonIgnore] private string _agencySource = string.Empty;

    [JsonInclude]
    [JsonPropertyName("AgencySource")]
    public string AgencySource { get => _agencySource; set => _agencySource = value; }

    /// <summary>
    /// The emotional/logical feel of the world. (Rational, Mythic, Whimsical, Dark/Entropic, Transcendent)
    /// Does the world operate on logic, wonder, darkness, or spiritual themes?
    /// </summary>
    [JsonIgnore] private string _toneLogic = string.Empty;

    [JsonInclude]
    [JsonPropertyName("ToneLogic")]
    public string ToneLogic { get => _toneLogic; set => _toneLogic = value; }

    #endregion

    #region List Tab Properties

    [JsonIgnore] private List<PhysicalWorldEntry> _physicalWorlds = new();

    [JsonInclude]
    [JsonPropertyName("PhysicalWorlds")]
    public List<PhysicalWorldEntry> PhysicalWorlds { get => _physicalWorlds; set => _physicalWorlds = value; }

    [JsonIgnore] private List<SpeciesEntry> _species = new();

    [JsonInclude]
    [JsonPropertyName("Species")]
    public List<SpeciesEntry> Species { get => _species; set => _species = value; }

    [JsonIgnore] private List<CultureEntry> _cultures = new();

    [JsonInclude]
    [JsonPropertyName("Cultures")]
    public List<CultureEntry> Cultures { get => _cultures; set => _cultures = value; }

    [JsonIgnore] private List<GovernmentEntry> _governments = new();

    [JsonInclude]
    [JsonPropertyName("Governments")]
    public List<GovernmentEntry> Governments { get => _governments; set => _governments = value; }

    [JsonIgnore] private List<ReligionEntry> _religions = new();

    [JsonInclude]
    [JsonPropertyName("Religions")]
    public List<ReligionEntry> Religions { get => _religions; set => _religions = value; }

    #endregion

    #region History Tab Properties (Single)

    [JsonIgnore] private string _foundingEvents = string.Empty;

    [JsonInclude]
    [JsonPropertyName("FoundingEvents")]
    public string FoundingEvents { get => _foundingEvents; set => _foundingEvents = value; }

    [JsonIgnore] private string _majorConflicts = string.Empty;

    [JsonInclude]
    [JsonPropertyName("MajorConflicts")]
    public string MajorConflicts { get => _majorConflicts; set => _majorConflicts = value; }

    [JsonIgnore] private string _eras = string.Empty;

    [JsonInclude]
    [JsonPropertyName("Eras")]
    public string Eras { get => _eras; set => _eras = value; }

    [JsonIgnore] private string _technologicalShifts = string.Empty;

    [JsonInclude]
    [JsonPropertyName("TechnologicalShifts")]
    public string TechnologicalShifts { get => _technologicalShifts; set => _technologicalShifts = value; }

    [JsonIgnore] private string _lostKnowledge = string.Empty;

    [JsonInclude]
    [JsonPropertyName("LostKnowledge")]
    public string LostKnowledge { get => _lostKnowledge; set => _lostKnowledge = value; }

    #endregion

    #region Economy Tab Properties (Single)

    [JsonIgnore] private string _economicSystem = string.Empty;

    [JsonInclude]
    [JsonPropertyName("EconomicSystem")]
    public string EconomicSystem { get => _economicSystem; set => _economicSystem = value; }

    [JsonIgnore] private string _currency = string.Empty;

    [JsonInclude]
    [JsonPropertyName("Currency")]
    public string Currency { get => _currency; set => _currency = value; }

    [JsonIgnore] private string _tradeRoutes = string.Empty;

    [JsonInclude]
    [JsonPropertyName("TradeRoutes")]
    public string TradeRoutes { get => _tradeRoutes; set => _tradeRoutes = value; }

    [JsonIgnore] private string _professions = string.Empty;

    [JsonInclude]
    [JsonPropertyName("Professions")]
    public string Professions { get => _professions; set => _professions = value; }

    [JsonIgnore] private string _wealthDistribution = string.Empty;

    [JsonInclude]
    [JsonPropertyName("WealthDistribution")]
    public string WealthDistribution { get => _wealthDistribution; set => _wealthDistribution = value; }

    #endregion

    #region Magic/Technology Tab Properties (Single)

    [JsonIgnore] private string _systemType = string.Empty;

    [JsonInclude]
    [JsonPropertyName("SystemType")]
    public string SystemType { get => _systemType; set => _systemType = value; }

    [JsonIgnore] private string _source = string.Empty;

    [JsonInclude]
    [JsonPropertyName("Source")]
    public string Source { get => _source; set => _source = value; }

    [JsonIgnore] private string _rules = string.Empty;

    [JsonInclude]
    [JsonPropertyName("Rules")]
    public string Rules { get => _rules; set => _rules = value; }

    [JsonIgnore] private string _limitations = string.Empty;

    [JsonInclude]
    [JsonPropertyName("Limitations")]
    public string Limitations { get => _limitations; set => _limitations = value; }

    [JsonIgnore] private string _cost = string.Empty;

    [JsonInclude]
    [JsonPropertyName("Cost")]
    public string Cost { get => _cost; set => _cost = value; }

    [JsonIgnore] private string _practitioners = string.Empty;

    [JsonInclude]
    [JsonPropertyName("Practitioners")]
    public string Practitioners { get => _practitioners; set => _practitioners = value; }

    [JsonIgnore] private string _socialImpact = string.Empty;

    [JsonInclude]
    [JsonPropertyName("SocialImpact")]
    public string SocialImpact { get => _socialImpact; set => _socialImpact = value; }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new StoryWorld with default name.
    /// </summary>
    public StoryWorldModel(StoryModel model, StoryNodeItem node)
        : base("Story World", StoryItemType.StoryWorld, model, node)
    {
        InitializeProperties();
    }

    /// <summary>
    /// Creates a new StoryWorld with specified name.
    /// </summary>
    public StoryWorldModel(string name, StoryModel model, StoryNodeItem node)
        : base(name, StoryItemType.StoryWorld, model, node)
    {
        InitializeProperties();
    }

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    public StoryWorldModel()
    {
    }

    private void InitializeProperties()
    {
        // Structure tab
        _worldType = string.Empty;
        _ontology = string.Empty;
        _worldRelation = string.Empty;
        _ruleTransparency = string.Empty;
        _scaleOfDifference = string.Empty;
        _agencySource = string.Empty;
        _toneLogic = string.Empty;

        // Lists (already initialized in field declarations)
        _physicalWorlds = new List<PhysicalWorldEntry>();
        _species = new List<SpeciesEntry>();
        _cultures = new List<CultureEntry>();
        _governments = new List<GovernmentEntry>();
        _religions = new List<ReligionEntry>();

        // History tab
        _foundingEvents = string.Empty;
        _majorConflicts = string.Empty;
        _eras = string.Empty;
        _technologicalShifts = string.Empty;
        _lostKnowledge = string.Empty;

        // Economy tab
        _economicSystem = string.Empty;
        _currency = string.Empty;
        _tradeRoutes = string.Empty;
        _professions = string.Empty;
        _wealthDistribution = string.Empty;

        // Magic/Technology tab
        _systemType = string.Empty;
        _source = string.Empty;
        _rules = string.Empty;
        _limitations = string.Empty;
        _cost = string.Empty;
        _practitioners = string.Empty;
        _socialImpact = string.Empty;
    }

    #endregion
}
