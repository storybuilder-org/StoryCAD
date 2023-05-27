using Windows.Data.Xml.Dom;

namespace StoryCAD.Models;

/// <summary>
/// OverviewModel contains overview information for the entire story, such as title, author, and so on.
/// It's a good place to capture the original idea which prompted the story.
///
/// There is only one OverviewModel instance for each story. It's also the root of the Shell Page's
/// StoryExplorer TreeView.
/// </summary>
public class OverviewModel : StoryElement
{
    /* Handing date fields and author:
     * System.DateTime wrkDate = DateTime.FromOADate(0);
       wrkDate = DateTime.Parse(DateTime.Parse(frmStory.DefInstance.mskDateCreated.Text).ToString("MM/dd/yy"));
     * StoryRec.DateCreated.Value = wrkDate.ToString("MM-dd-yy");
     */

    #region Properties

    // Because Overview is always the root of the StoryExplorer, there is only one instance of it.

    // Overview data

    private string _dateCreated;
    public string DateCreated
    {
        get => _dateCreated;
        set => _dateCreated = value;
    }

    private string _author;
    public string Author
    {
        get => _author;
        set => _author = value;

    }

    private string _dateModified;
    public string DateModified
    {
        get => _dateModified;
        set => _dateModified = value;
    }

    private string _storyIdea;
    public string StoryIdea
    {
        get => _storyIdea;
        set => _storyIdea = value;
    }


    private string _concept;
    public string Concept
    {
        get => _concept;
        set => _concept = value;
    }

    /// Premise is a property on every ProblemModel. Your story can (and usually will) have
    /// more than one Problem. Every Problem has a Premise, but only one of your ProblemModels
    /// the story's problem, and it's identified by the StoryProblem property.
    /// The StoryProblem the spine of your story; when it’s resolved,  your story is over.
    /// All other Problem StoryElements are complications or subplots of the StoryProblem.
    private string _storyProblem;  // The Guid of a Problem StoryElement
    public string StoryProblem
    {
        get => _storyProblem;
        set => _storyProblem = value;
    }

    /// The OverviewModel Premise is the story's premise. If a StoryProblem has been created
    /// and selected (which may not be true in the story's early formulation), this
    /// Premise property and the StoryProblem Premise will be synchronized: when you update
    /// either of them, the other is also updated. The contents of the StoryProblem Premise
    /// (or any Problem's Premise) relate to other properties on the ProblemModel, and defining
    /// the story's Premise here is a headstart on understanding the story's Problem.
    /// 
    private string _premise;
    public string Premise
    {
        get => _premise;
        set => _premise = value;
    }

    // Structure data

    private string _storyType;
    public string StoryType
    {
        get => _storyType;
        set => _storyType = value;
    }

    private string _storyGenre;
    public string StoryGenre
    {
        get => _storyGenre;
        set => _storyGenre = value;
    }

    private string _viewpoint;
    public string Viewpoint
    {
        get => _viewpoint;
        set => _viewpoint = value;
    }

    private string _viewpointCharacter;
    public string ViewpointCharacter
    {
        get => _viewpointCharacter;
        set => _viewpointCharacter = value;
    }

    private string _voice;
    public string Voice
    {
        get => _voice;
        set => _voice = value;
    }

    private string _literaryDevice;
    public string LiteraryDevice
    {
        get => _literaryDevice;
        set => _literaryDevice = value;
    }

    private string _tense;
    public string Tense
    {
        get => _tense;
        set => _tense = value;
    }

    private string _style;
    public string Style
    {
        get => _style;
        set => _style = value;
    }

    private string _structureNotes;
    public string StructureNotes
    {
        get => _structureNotes;
        set => _structureNotes = value;
    }
    private string _tone;
    public string Tone
    {
        get => _tone;
        set => _tone = value;
    }

    // Notes data

    private string _notes;
    public string Notes
    {
        get => _notes;
        set => _notes = value;
    }

    #endregion

    #region Constructor

    public OverviewModel(string name, StoryModel model) : base(name, StoryItemType.StoryOverview, model)
    {
        DateCreated = string.Empty;
        Author = string.Empty;
        DateModified = string.Empty;
        StoryType = string.Empty;
        StoryGenre = string.Empty;
        Viewpoint = string.Empty;
        ViewpointCharacter = string.Empty;
        Voice = string.Empty;
        LiteraryDevice = string.Empty;
        Tense = string.Empty;
        Style = string.Empty;
        Tone = string.Empty;
        StoryIdea = string.Empty;
        Concept = string.Empty;
        StructureNotes = string.Empty;
        Notes = string.Empty;
        StoryProblem = string.Empty;
    }

    public OverviewModel(IXmlNode xn, StoryModel model) : base(xn, model)
    {
        DateCreated = string.Empty;
        Author = string.Empty;
        DateModified = string.Empty;
        StoryType = string.Empty;
        StoryGenre = string.Empty;
        Viewpoint = string.Empty;
        ViewpointCharacter = string.Empty;
        Voice = string.Empty;
        LiteraryDevice = string.Empty;
        Style = string.Empty;
        Tense = string.Empty;
        Style = string.Empty;
        Tone = string.Empty;
        StoryIdea = string.Empty;
        Concept = string.Empty;
        StructureNotes = string.Empty;
        Notes = string.Empty;
        StoryProblem = string.Empty;
    }

    #endregion
}