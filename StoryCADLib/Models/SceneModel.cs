using System.Text.Json.Serialization;

namespace StoryCAD.Models;

public class SceneModel : StoryElement
{
	#region Properties

	[JsonIgnore]
	private string _sceneDescription;

	[JsonInclude]
	[JsonPropertyName("Description")]
	public string SceneDescription
	{
		get => _sceneDescription;
		set => _sceneDescription = value;
	}

	[JsonIgnore]
	private Guid _viewpointCharacter;

	[JsonInclude]
	[JsonPropertyName("ViewpointCharacter")]
	public Guid ViewpointCharacter
	{
		get => _viewpointCharacter;
		set => _viewpointCharacter = value;
	}

	[JsonIgnore]
	private string _date;

	[JsonInclude]
	[JsonPropertyName("Date")]
	public string Date
	{
		get => _date;
		set => _date = value;
	}

	[JsonIgnore]
	private string _time;

	[JsonInclude]
	[JsonPropertyName("Time")]
	public string Time
	{
		get => _time;
		set => _time = value;
	}

	[JsonIgnore]
	private Guid _setting;

	[JsonInclude]
	[JsonPropertyName("Setting")]
	public Guid Setting
	{
		get => _setting;
		set => _setting = value;
	}

	[JsonIgnore]
	private string _sceneType;

	[JsonInclude]
	[JsonPropertyName("SceneType")]
	public string SceneType
	{
		get => _sceneType;
		set => _sceneType = value;
	}

	[JsonIgnore]
	private List<Guid> _castMembers;
	//TODO: Convert to GUIDs	
	[JsonInclude]
	[JsonPropertyName("CastMembers")]
	public List<Guid> CastMembers
	{
		get => _castMembers;
		set => _castMembers = value;
	}

	[JsonIgnore]
	private Guid _protagonist;

	[JsonInclude]
	[JsonPropertyName("Protagonist")]
	public Guid Protagonist
	{
		get => _protagonist;
		set => _protagonist = value;
	}

	[JsonIgnore]
	private string _protagEmotion;

	[JsonInclude]
	[JsonPropertyName("ProtagEmotion")]
	public string ProtagEmotion
	{
		get => _protagEmotion;
		set => _protagEmotion = value;
	}

	[JsonIgnore]
	private string _protagGoal;

	[JsonInclude]
	[JsonPropertyName("ProtagGoal")]
	public string ProtagGoal
	{
		get => _protagGoal;
		set => _protagGoal = value;
	}

	[JsonIgnore]
	private Guid _antagonist;

	[JsonInclude]
	[JsonPropertyName("Antagonist")]
	public Guid Antagonist
	{
		get => _antagonist;
		set => _antagonist = value;
	}

	[JsonIgnore]
	private string _antagEmotion;

	[JsonInclude]
	[JsonPropertyName("AntagEmotion")]
	public string AntagEmotion
	{
		get => _antagEmotion;
		set => _antagEmotion = value;
	}

	[JsonIgnore]
	private string _antagGoal;

	[JsonInclude]
	[JsonPropertyName("AntagGoal")]
	public string AntagGoal
	{
		get => _antagGoal;
		set => _antagGoal = value;
	}

	[JsonIgnore]
	private string _opposition;

	[JsonInclude]
	[JsonPropertyName("Opposition")]
	public string Opposition
	{
		get => _opposition;
		set => _opposition = value;
	}

	[JsonIgnore]
	private string _outcome;

	[JsonInclude]
	[JsonPropertyName("Outcome")]
	public string Outcome
	{
		get => _outcome;
		set => _outcome = value;
	}
	// Scene Development (Story Genius) data

	[JsonIgnore]
	private List<string> _scenePurpose;

	[JsonInclude]
	[JsonPropertyName("ScenePurpose")]
	public List<string> ScenePurpose
	{
		get => _scenePurpose;
		set => _scenePurpose = value;
	}

	[JsonIgnore]
	private string _valueExchange;

	[JsonInclude]
	[JsonPropertyName("ValueExchange")]
	public string ValueExchange
	{
		get => _valueExchange;
		set => _valueExchange = value;
	}

	[JsonIgnore]
	private string _events;

	[JsonInclude]
	[JsonPropertyName("Events")]
	public string Events
	{
		get => _events;
		set => _events = value;
	}

	[JsonIgnore]
	private string _consequences;

	[JsonInclude]
	[JsonPropertyName("Consequences")]
	public string Consequences
	{
		get => _consequences;
		set => _consequences = value;
	}

	[JsonIgnore]
	private string _significance;

	[JsonInclude]
	[JsonPropertyName("Significance")]
	public string Significance
	{
		get => _significance;
		set => _significance = value;
	}

	[JsonIgnore]
	private string _realization;

	[JsonInclude]
	[JsonPropertyName("Realization")]
	public string Realization
	{
		get => _realization;
		set => _realization = value;
	}

	[JsonIgnore]
	private string _emotion;

	[JsonInclude]
	[JsonPropertyName("Emotion")]
	public string Emotion
	{
		get => _emotion;
		set => _emotion = value;
	}

	[JsonIgnore]
	private string _newGoal;

	[JsonInclude]
	[JsonPropertyName("NewGoal")]
	public string NewGoal
	{
		get => _newGoal;
		set => _newGoal = value;
	}

	[JsonIgnore]
	private string _review;

	[JsonInclude]
	[JsonPropertyName("Review")]
	public string Review
	{
		get => _review;
		set => _review = value;
	}

	[JsonIgnore]
	private string _notes;

	[JsonInclude]
	[JsonPropertyName("Notes")]
	public string Notes
	{
		get => _notes;
		set => _notes = value;
	}


	#endregion

	#region Constructors
	public SceneModel(StoryModel model, StoryNodeItem Node) : base("New Scene", StoryItemType.Scene, model, Node)
    {
        SceneDescription = string.Empty;
        ViewpointCharacter = Guid.Empty;
        Date = string.Empty;
        Time = string.Empty;
        Setting = Guid.Empty;
        SceneType = string.Empty;
        CastMembers = new List<Guid>();
        Description = string.Empty;
        ScenePurpose = new List<string>();
        ValueExchange = string.Empty;
        Protagonist = Guid.Empty;
        ProtagEmotion = string.Empty;
        ProtagGoal = string.Empty;
        Antagonist = Guid.Empty;
        AntagEmotion = string.Empty;
        AntagGoal = string.Empty;
        Opposition = string.Empty;
        Outcome = string.Empty;
        Emotion = string.Empty;
        NewGoal = string.Empty;
        Events = string.Empty;
        Consequences = string.Empty;
        Significance = string.Empty;
        Realization = string.Empty;
        Review = string.Empty;
        Notes = string.Empty;
    }

    public SceneModel(string name, StoryModel model, StoryNodeItem Node) : base(name, StoryItemType.Scene, model, Node)

	{
	    SceneDescription = string.Empty;
	    ViewpointCharacter = Guid.Empty;
	    Date = string.Empty;
	    Time = string.Empty;
	    Setting = Guid.Empty;
	    SceneType = string.Empty;
	    CastMembers = new List<Guid>();
	    Description = string.Empty;
	    ScenePurpose = new List<string>();
	    ValueExchange = string.Empty;
	    Protagonist = Guid.Empty;
	    ProtagEmotion = string.Empty;
	    ProtagGoal = string.Empty;
	    Antagonist = Guid.Empty;
	    AntagEmotion = string.Empty;
	    AntagGoal = string.Empty;
	    Opposition = string.Empty;
	    Outcome = string.Empty;
	    Emotion = string.Empty;
	    NewGoal = string.Empty;
	    Events = string.Empty;
	    Consequences = string.Empty;
	    Significance = string.Empty;
	    Realization = string.Empty;
	    Review = string.Empty;
	    Notes = string.Empty;
    }


	public SceneModel()
	{

	}
    #endregion
}