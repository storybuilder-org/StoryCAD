using Windows.Data.Xml.Dom;

namespace StoryCAD.Models;

public class SceneModel : StoryElement
{
    #region Properties

    //  Scene General data
    private string _description;
    public string Description
    {
        get => _description;
        set => _description = value;
    }

    private string _viewpointCharacter;
    public string ViewpointCharacter
    {
        get => _viewpointCharacter;
        set => _viewpointCharacter = value;
    }

    private string _date;
    public string Date
    {
        get => _date;
        set => _date = value;
    }

    private string _time;
    public string Time
    {
        get => _time;
        set => _time = value;
    }

    private string _setting;
    public string Setting
    {
        get => _setting;
        set => _setting = value;
    }

    private string _sceneType;
    public string SceneType
    {
        get => _sceneType;
        set => _sceneType = value;
    }

    private List<string> _castMembers;
    public List<string> CastMembers
    {
        get => _castMembers;
        set => _castMembers = value;
    }

    private string _remarks;
    public string Remarks
    {
        get => _remarks;
        set => _remarks = value;
    }

    //  Scene Goal data

    private string _protagonist;
    public string Protagonist
    {
        get => _protagonist;
        set => _protagonist = value;
    }

    private string _protagEmotion;
    public string ProtagEmotion
    {
        get => _protagEmotion;
        set => _protagEmotion = value;
    }

    private string _protagGoal;
    public string ProtagGoal
    {
        get => _protagGoal;
        set => _protagGoal = value;
    }

    private string _antagonist;
    public string Antagonist
    {
        get => _antagonist;
        set => _antagonist = value;
    }

    private string _antagEmotion;
    public string AntagEmotion
    {
        get => _antagEmotion;
        set => _antagEmotion = value;
    }

    private string _antagGoal;
    public string AntagGoal
    {
        get => _antagGoal;
        set => _antagGoal = value;
    }

    private string _opposition;
    public string Opposition
    {
        get => _opposition;
        set => _opposition = value;
    }

    private string _outcome;
    public string Outcome
    {
        get => _outcome;
        set => _outcome = value;
    }
    // Scene Development (Story Genius) data

    private List<string> _scenePurpose;
    public List<string> ScenePurpose
    {
        get => _scenePurpose;
        set => _scenePurpose = value;
    }

    private string _valueExchange;
    public string ValueExchange
    {
        get => _valueExchange;
        set => _valueExchange = value;
    }

    private string _events;
    public string Events
    {
        get => _events;
        set => _events = value;
    }

    private string _consequences;
    public string Consequences
    {
        get => _consequences;
        set => _consequences = value;
    }

    private string _significance;
    public string Significance
    {
        get => _significance;
        set => _significance = value;
    }

    private string _realization;
    public string Realization
    {
        get => _realization;
        set => _realization = value;
    }

    //  Scene Sequel data

    private string _emotion;
    public string Emotion
    {
        get => _emotion;
        set => _emotion = value;
    }

    private string _newGoal;
    public string NewGoal
    {
        get => _newGoal;
        set => _newGoal = value;
    }

    private string _review;
    public string Review
    {
        get => _review;
        set => _review = value;
    }

    //  Scene Note data

    private string _notes;
    public string Notes
    {
        get => _notes;
        set => _notes = value;
    }

    #endregion

    #region Constructors
    public SceneModel(StoryModel model) : base("New Scene", StoryItemType.Scene, model)
    {
        ViewpointCharacter = string.Empty;
        Date = string.Empty;
        Time = string.Empty;
        Setting = string.Empty;
        SceneType = string.Empty;
        CastMembers = new List<string>();
        Remarks = string.Empty;
        ScenePurpose = new List<string>();
        ValueExchange = string.Empty;
        Protagonist = string.Empty;
        ProtagEmotion = string.Empty;
        ProtagGoal = string.Empty;
        Antagonist = string.Empty;
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

    public SceneModel(string name, StoryModel model) : base(name, StoryItemType.Scene, model)

	{
	    ViewpointCharacter = string.Empty;
	    Date = string.Empty;
	    Time = string.Empty;
	    Setting = string.Empty;
	    SceneType = string.Empty;
	    CastMembers = new List<string>();
	    Remarks = string.Empty;
	    ScenePurpose = new List<string>();
	    ValueExchange = string.Empty;
	    Protagonist = string.Empty;
	    ProtagEmotion = string.Empty;
	    ProtagGoal = string.Empty;
	    Antagonist = string.Empty;
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

	public SceneModel(IXmlNode xn, StoryModel model) : base(xn, model)
    {
        ViewpointCharacter = string.Empty;
        Date = string.Empty;
        Time = string.Empty;
        Setting = string.Empty;
        SceneType = string.Empty;
        CastMembers = new List<string>();
        Remarks = string.Empty;
        ScenePurpose = new List<string>();
        ValueExchange = string.Empty;
        Protagonist = string.Empty;
        ProtagEmotion = string.Empty;
        ProtagGoal = string.Empty;
        Antagonist = string.Empty;
        AntagEmotion = string.Empty;
        AntagGoal = string.Empty;
        Opposition = string.Empty;
        Outcome = string.Empty;
        Emotion = string.Empty;
        Events = string.Empty;
        Consequences = string.Empty;
        Significance = string.Empty;
        Realization = string.Empty;
        NewGoal = string.Empty;
        Review = string.Empty;
        Notes = string.Empty;
    }

    #endregion
}