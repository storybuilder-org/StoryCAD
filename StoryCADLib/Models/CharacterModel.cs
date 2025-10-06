using System.Text.Json.Serialization;

namespace StoryCAD.Models;

public class CharacterModel : StoryElement
{
    #region Properties

    // Character role data
    [JsonIgnore] private string _role;

    [JsonInclude]
    [JsonPropertyName("Role")]
    public string Role
    {
        get => _role;
        set => _role = value;
    }

    [JsonIgnore] private string _storyRole;

    [JsonInclude]
    [JsonPropertyName("StoryRole")]
    public string StoryRole
    {
        get => _storyRole;
        set => _storyRole = value;
    }

    [JsonIgnore] private string _archetype;

    [JsonInclude]
    [JsonPropertyName("Archetype")]
    public string Archetype
    {
        get => _archetype;
        set => _archetype = value;
    }

    // Character physical data

    [JsonIgnore] private string _age;

    [JsonInclude]
    [JsonPropertyName("Age")]
    public string Age
    {
        get => _age;
        set => _age = value;
    }

    [JsonIgnore] private string _sex;

    [JsonInclude]
    [JsonPropertyName("Sex")]
    public string Sex
    {
        get => _sex;
        set => _sex = value;
    }

    [JsonIgnore] private string _eyes;

    [JsonInclude]
    [JsonPropertyName("Eyes")]
    public string Eyes
    {
        get => _eyes;
        set => _eyes = value;
    }

    [JsonIgnore] private string _hair;

    [JsonInclude]
    [JsonPropertyName("Hair")]
    public string Hair
    {
        get => _hair;
        set => _hair = value;
    }

    [JsonIgnore] private string _weight;

    [JsonInclude]
    [JsonPropertyName("Weight")]
    public string Weight
    {
        get => _weight;
        set => _weight = value;
    }

    [JsonIgnore] private string _charHeight;

    [JsonInclude]
    [JsonPropertyName("CharHeight")]
    public string CharHeight
    {
        get => _charHeight;
        set => _charHeight = value;
    }

    [JsonIgnore] private string _build;

    [JsonInclude]
    [JsonPropertyName("Build")]
    public string Build
    {
        get => _build;
        set => _build = value;
    }

    [JsonIgnore] private string _complexion;

    [JsonInclude]
    [JsonPropertyName("Complexion")]
    public string Complexion
    {
        get => _complexion;
        set => _complexion = value;
    }

    [JsonIgnore] private string _race;

    [JsonInclude]
    [JsonPropertyName("Race")]
    public string Race
    {
        get => _race;
        set => _race = value;
    }

    [JsonIgnore] private string _nationality;

    [JsonInclude]
    [JsonPropertyName("Nationality")]
    public string Nationality
    {
        get => _nationality;
        set => _nationality = value;
    }

    [JsonIgnore] private string _health;

    [JsonInclude]
    [JsonPropertyName("Health")]
    public string Health
    {
        get => _health;
        set => _health = value;
    }

    [JsonIgnore] private string _physNotes;

    [JsonInclude]
    [JsonPropertyName("PhysNotes")]
    public string PhysNotes
    {
        get => _physNotes;
        set => _physNotes = value;
    }

    // Character appearance data

    [JsonIgnore] private string _appearance;

    [JsonInclude]
    [JsonPropertyName("Appearance")]
    public string Appearance
    {
        get => _appearance;
        set => _appearance = value;
    }

    // Character relationship data

    [JsonIgnore] private List<RelationshipModel> _relationshipList;

    [JsonInclude]
    [JsonPropertyName("RelationshipList")]
    public List<RelationshipModel> RelationshipList
    {
        get => _relationshipList;
        set => _relationshipList = value;
    }

    // Character social data

    [JsonIgnore] private string _economic;

    [JsonInclude]
    [JsonPropertyName("Economic")]
    public string Economic
    {
        get => _economic;
        set => _economic = value;
    }

    [JsonIgnore] private string _education;

    [JsonInclude]
    [JsonPropertyName("Education")]
    public string Education
    {
        get => _education;
        set => _education = value;
    }

    [JsonIgnore] private string _ethnic;

    [JsonInclude]
    [JsonPropertyName("Ethnic")]
    public string Ethnic
    {
        get => _ethnic;
        set => _ethnic = value;
    }

    [JsonIgnore] private string _religion;

    [JsonInclude]
    [JsonPropertyName("Religion")]
    public string Religion
    {
        get => _religion;
        set => _religion = value;
    }

    // Character psych data

    [JsonIgnore] private string _enneagram;

    [JsonInclude]
    [JsonPropertyName("Enneagram")]
    public string Enneagram
    {
        get => _enneagram;
        set => _enneagram = value;
    }

    [JsonIgnore] private string _intelligence;

    [JsonInclude]
    [JsonPropertyName("Intelligence")]
    public string Intelligence
    {
        get => _intelligence;
        set => _intelligence = value;
    }

    [JsonIgnore] private string _values;

    [JsonInclude]
    [JsonPropertyName("Values")]
    public string Values
    {
        get => _values;
        set => _values = value;
    }

    [JsonIgnore] private string _abnormality;

    [JsonInclude]
    [JsonPropertyName("Abnormality")]
    public string Abnormality
    {
        get => _abnormality;
        set => _abnormality = value;
    }

    [JsonIgnore] private string _focus;

    [JsonInclude]
    [JsonPropertyName("Focus")]
    public string Focus
    {
        get => _focus;
        set => _focus = value;
    }

    [JsonIgnore] private string _psychNotes;

    [JsonInclude]
    [JsonPropertyName("PsychNotes")]
    public string PsychNotes
    {
        get => _psychNotes;
        set => _psychNotes = value;
    }

    // Character trait data

    [JsonIgnore] private string _adventurousness;

    [JsonInclude]
    [JsonPropertyName("Adventureousness")]
    public string Adventureousness
    {
        get => _adventurousness;
        set => _adventurousness = value;
    }

    [JsonIgnore] private string _aggression;

    [JsonInclude]
    [JsonPropertyName("Aggression")]
    public string Aggression
    {
        get => _aggression;
        set => _aggression = value;
    }

    [JsonIgnore] private string _confidence;

    [JsonInclude]
    [JsonPropertyName("Confidence")]
    public string Confidence
    {
        get => _confidence;
        set => _confidence = value;
    }

    [JsonIgnore] private string _conscientiousness;

    [JsonInclude]
    [JsonPropertyName("Conscientiousness")]
    public string Conscientiousness
    {
        get => _conscientiousness;
        set => _conscientiousness = value;
    }

    [JsonIgnore] private string _creativity;

    [JsonInclude]
    [JsonPropertyName("Creativity")]
    public string Creativity
    {
        get => _creativity;
        set => _creativity = value;
    }

    [JsonIgnore] private string _dominance;

    [JsonInclude]
    [JsonPropertyName("Dominance")]
    public string Dominance
    {
        get => _dominance;
        set => _dominance = value;
    }

    [JsonIgnore] private string _enthusiasm;

    [JsonInclude]
    [JsonPropertyName("Enthusiasm")]
    public string Enthusiasm
    {
        get => _enthusiasm;
        set => _enthusiasm = value;
    }

    [JsonIgnore] private string _assurance;

    [JsonInclude]
    [JsonPropertyName("Assurance")]
    public string Assurance
    {
        get => _assurance;
        set => _assurance = value;
    }

    [JsonIgnore] private string _sensitivity;

    [JsonInclude]
    [JsonPropertyName("Sensitivity")]
    public string Sensitivity
    {
        get => _sensitivity;
        set => _sensitivity = value;
    }

    [JsonIgnore] private string _shrewdness;

    [JsonInclude]
    [JsonPropertyName("Shrewdness")]
    public string Shrewdness
    {
        get => _shrewdness;
        set => _shrewdness = value;
    }

    [JsonIgnore] private string _sociability;

    [JsonInclude]
    [JsonPropertyName("Sociability")]
    public string Sociability
    {
        get => _sociability;
        set => _sociability = value;
    }

    [JsonIgnore] private string _stability;

    [JsonInclude]
    [JsonPropertyName("Stability")]
    public string Stability
    {
        get => _stability;
        set => _stability = value;
    }

    // Character likes data

    [JsonIgnore] private string _notes;

    [JsonInclude]
    [JsonPropertyName("Notes")]
    public string Notes
    {
        get => _notes;
        set => _notes = value;
    }

    // Character traits data

    [JsonIgnore] private List<string> _traitList;

    [JsonInclude]
    [JsonPropertyName("TraitList")]
    public List<string> TraitList
    {
        get => _traitList;
        set => _traitList = value;
    }

    // Character flaw data

    [JsonIgnore] private string _flaw;

    [JsonInclude]
    [JsonPropertyName("Flaw")]
    public string Flaw
    {
        get => _flaw;
        set => _flaw = value;
    }

    // Character backstory  

    [JsonIgnore] private string _backStory;

    [JsonInclude]
    [JsonPropertyName("BackStory")]
    public string BackStory
    {
        get => _backStory;
        set => _backStory = value;
    }

    #endregion

    #region Constructors

    public CharacterModel(StoryModel model, StoryNodeItem node)
        : base("New Character", StoryItemType.Character, model, node)
    {
        Role = string.Empty;
        StoryRole = string.Empty;
        Archetype = string.Empty;
        Description = string.Empty;
        Age = string.Empty;
        Sex = string.Empty;
        Eyes = string.Empty;
        Hair = string.Empty;
        Weight = string.Empty;
        CharHeight = string.Empty;
        Build = string.Empty;
        Complexion = string.Empty;
        Race = string.Empty;
        Nationality = string.Empty;
        Health = string.Empty;
        PhysNotes = string.Empty;
        Appearance = string.Empty;
        RelationshipList = new List<RelationshipModel>();
        Economic = string.Empty;
        Education = string.Empty;
        Ethnic = string.Empty;
        Religion = string.Empty;
        Enneagram = string.Empty;
        Intelligence = string.Empty;
        Values = string.Empty;
        Abnormality = string.Empty;
        Focus = string.Empty;
        PsychNotes = string.Empty;
        Adventureousness = string.Empty;
        Aggression = string.Empty;
        Confidence = string.Empty;
        Conscientiousness = string.Empty;
        Creativity = string.Empty;
        Dominance = string.Empty;
        Enthusiasm = string.Empty;
        Assurance = string.Empty;
        Sensitivity = string.Empty;
        Shrewdness = string.Empty;
        Sociability = string.Empty;
        Stability = string.Empty;
        Notes = string.Empty;
        TraitList = new List<string>();
        Flaw = string.Empty;
        BackStory = string.Empty;
    }

    public CharacterModel(string name, StoryModel model, StoryNodeItem Node)
        : base(name, StoryItemType.Character, model, Node)
    {
        Role = string.Empty;
        StoryRole = string.Empty;
        Archetype = string.Empty;
        Description = string.Empty;
        Age = string.Empty;
        Sex = string.Empty;
        Eyes = string.Empty;
        Hair = string.Empty;
        Weight = string.Empty;
        CharHeight = string.Empty;
        Build = string.Empty;
        Complexion = string.Empty;
        Race = string.Empty;
        Nationality = string.Empty;
        Health = string.Empty;
        PhysNotes = string.Empty;
        Appearance = string.Empty;
        RelationshipList = new List<RelationshipModel>();
        Economic = string.Empty;
        Education = string.Empty;
        Ethnic = string.Empty;
        Religion = string.Empty;
        Enneagram = string.Empty;
        Intelligence = string.Empty;
        Values = string.Empty;
        Abnormality = string.Empty;
        Focus = string.Empty;
        PsychNotes = string.Empty;
        Adventureousness = string.Empty;
        Aggression = string.Empty;
        Confidence = string.Empty;
        Conscientiousness = string.Empty;
        Creativity = string.Empty;
        Dominance = string.Empty;
        Enthusiasm = string.Empty;
        Assurance = string.Empty;
        Sensitivity = string.Empty;
        Shrewdness = string.Empty;
        Sociability = string.Empty;
        Stability = string.Empty;
        Notes = string.Empty;
        TraitList = new List<string>();
        Flaw = string.Empty;
        BackStory = string.Empty;
    }


    public CharacterModel()
    {
    }

    #endregion
}
