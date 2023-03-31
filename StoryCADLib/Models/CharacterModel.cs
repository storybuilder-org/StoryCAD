using System.Collections.Generic;
using Windows.Data.Xml.Dom;

namespace StoryCAD.Models;

public class CharacterModel : StoryElement
{

    #region Properties

    // Character role data

    private string _role;
    public string Role
    {
        get => _role;
        set => _role = value;
    }

    private string _storyRole;
    public string StoryRole
    {
        get => _storyRole;
        set => _storyRole = value;
    }

    private string _archetype;
    public string Archetype
    {
        get => _archetype;
        set => _archetype = value;
    }

    private string _characterSketch;
    public string CharacterSketch
    {
        get => _characterSketch;
        set => _characterSketch = value;
    }

    // Character physical data

    private string _age;
    public string Age
    {
        get => _age;
        set => _age = value;
    }

    private string _sex;
    public string Sex
    {
        get => _sex;
        set => _sex = value;
    }

    private string _eyes;
    public string Eyes
    {
        get => _eyes;
        set => _eyes = value;
    }

    private string _hair;
    public string Hair
    {
        get => _hair;
        set => _hair = value;
    }

    private string _weight;
    public string Weight
    {
        get => _weight;
        set => _weight = value;
    }

    private string _charHeight;
    public string CharHeight
    {
        get => _charHeight;
        set => _charHeight = value;
    }

    private string _build;
    public string Build
    {
        get => _build;
        set => _build = value;
    }

    private string _complexion;
    public string Complexion
    {
        get => _complexion;
        set => _complexion = value;
    }

    private string _race;
    public string Race
    {
        get => _race;
        set => _race = value;
    }

    private string _nationality;
    public string Nationality
    {
        get => _nationality;
        set => _nationality = value;
    }

    private string _health;
    public string Health
    {
        get => _health;
        set => _health = value;
    }

    private string _physNotes;
    public string PhysNotes
    {
        get => _physNotes;
        set => _physNotes = value;
    }

    // Character appearance data

    private string _appearance;
    public string Appearance
    {
        get => _appearance;
        set => _appearance = value;
    }

    // Character relationship data

    private List<RelationshipModel> _relationshipList;
    public List<RelationshipModel> RelationshipList
    {
        get => _relationshipList;
        set => _relationshipList = value;
    }

    // Character social data

    private string _economic;
    public string Economic
    {
        get => _economic;
        set => _economic = value;
    }

    private string _education;
    public string Education
    {
        get => _education;
        set => _education = value;
    }

    private string _ethnic;
    public string Ethnic
    {
        get => _ethnic;
        set => _ethnic = value;
    }

    private string _religion;
    public string Religion
    {
        get => _religion;
        set => _religion = value;
    }

    // Character psych data

    private string _enneagram;
    public string Enneagram
    {
        get => _enneagram;
        set => _enneagram = value;
    }

    private string _intelligence;
    public string Intelligence
    {
        get => _intelligence;
        set => _intelligence = value;
    }

    private string _values;
    public string Values
    {
        get => _values;
        set => _values = value;
    }

    private string _abnormality;
    public string Abnormality
    {
        get => _abnormality;
        set => _abnormality = value;
    }

    private string _focus;
    public string Focus
    {
        get => _focus;
        set => _focus = value;
    }

    private string _psychNotes;
    public string PsychNotes
    {
        get => _psychNotes;
        set => _psychNotes = value;
    }

    // Character trait data

    private string _adventurousness;
    public string Adventureousness
    {
        get => _adventurousness;
        set => _adventurousness = value;
    }

    private string _aggression;
    public string Aggression
    {
        get => _aggression;
        set => _aggression = value;
    }

    private string _confidence;
    public string Confidence
    {
        get => _confidence;
        set => _confidence = value;
    }

    private string _conscientiousness;
    public string Conscientiousness
    {
        get => _conscientiousness;
        set => _conscientiousness = value;
    }

    private string _creativity;
    public string Creativity
    {
        get => _creativity;
        set => _creativity = value;
    }

    private string _dominance;
    public string Dominance
    {
        get => _dominance;
        set => _dominance = value;
    }

    private string _enthusiasm;
    public string Enthusiasm
    {
        get => _enthusiasm;
        set => _enthusiasm = value;
    }

    private string _assurance;
    public string Assurance
    {
        get => _assurance;
        set => _assurance = value;
    }

    private string _sensitivity;
    public string Sensitivity
    {
        get => _sensitivity;
        set => _sensitivity = value;
    }

    private string _shrewdness;
    public string Shrewdness
    {
        get => _shrewdness;
        set => _shrewdness = value;
    }

    private string _sociability;
    public string Sociability
    {
        get => _sociability;
        set => _sociability = value;
    }

    private string _stability;
    public string Stability
    {
        get => _stability;
        set => _stability = value;
    }

    // Character likes data

    private string _notes;
    public string Notes
    {
        get => _notes;
        set => _notes = value;
    }

    // Character traits data


    private List<string> _traitList;
    public List<string> TraitList
    {
        get => _traitList;
        set => _traitList = value;
    }

    // Character flaw data

    private string _flaw;
    public string Flaw
    {
        get => _flaw;
        set => _flaw = value;
    }

    // Character backstory  

    private string _backStory;
    public string BackStory
    {
        get => _backStory;
        set => _backStory = value;
    }

    #endregion

    #region Constructors
    public CharacterModel(StoryModel model) : base("New Character", StoryItemType.Character, model)
    {
        Role = string.Empty;
        StoryRole = string.Empty;
        Archetype = string.Empty;
        CharacterSketch = string.Empty;
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

    public CharacterModel(string name, StoryModel model) : base(name, StoryItemType.Character, model)
    {
        Role = string.Empty;
        StoryRole = string.Empty;
        Archetype = string.Empty;
        CharacterSketch = string.Empty;
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

    public CharacterModel(IXmlNode xn, StoryModel model) : base(xn, model)
    {
        Role = string.Empty;
        StoryRole = string.Empty;
        Archetype = string.Empty;
        CharacterSketch = string.Empty;
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

    #endregion

}