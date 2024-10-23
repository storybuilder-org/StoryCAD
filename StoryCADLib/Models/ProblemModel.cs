using StoryCAD.ViewModels.Tools;
using System.Collections.ObjectModel;
using Windows.Data.Xml.Dom;

namespace StoryCAD.Models;

public class ProblemModel : StoryElement
{
    #region Properties

    // Problem problem data
    private string _problemType;
    public string ProblemType
    {
        get => _problemType;
        set => _problemType = value;
    }

    private string _conflictType;
    public string ConflictType
    {
        get => _conflictType;
        set => _conflictType = value;
    }

    private string _problemCategory;
    public string ProblemCategory
    {
        get => _problemCategory;
        set => _problemCategory = value;
    }

    private string _subject;
    public string Subject
    {
        get => _subject;
        set => _subject = value;
    }

    private string _problemSource;
    public string ProblemSource
    {
        get => _problemSource;
        set => _problemSource = value;
    }

    private string _storyQuestion;
    public string StoryQuestion
    {
        get => _storyQuestion;
        set => _storyQuestion = value;
    }

    // Problem protagonist data

    private string _protagonist;
    public string Protagonist
    {
        get => _protagonist;
        set => _protagonist = value;
    }

    private string _protGoal;
    public string ProtGoal
    {
        get => _protGoal;
        set => _protGoal = value;
    }

    private string _protMotive;
    public string ProtMotive
    {
        get => _protMotive;
        set => _protMotive = value;
    }

    private string _protConflict;
    public string ProtConflict
    {
        get => _protConflict;
        set => _protConflict = value;
    }
    // Problem antagonist data

    private string _antagonist;
    public string Antagonist
    {
        get => _antagonist;
        set => _antagonist = value;
    }

    private string _antagGoal;
    public string AntagGoal
    {
        get => _antagGoal;
        set => _antagGoal = value;
    }

    private string _antagMotive;
    public string AntagMotive
    {
        get => _antagMotive;
        set => _antagMotive = value;
    }

    private string _antagConflict;
    public string AntagConflict
    {
        get => _antagConflict;
        set => _antagConflict = value;
    }
    // Problem resolution data

    private string _outcome;
    public string Outcome
    {
        get => _outcome;
        set => _outcome = value;
    }

    private string _method;
    public string Method
    {
        get => _method;
        set => _method = value;
    }

    private string _theme;
    public string Theme
    {
        get => _theme;
        set => _theme = value;
    }

    private string _premise;
    public string Premise
    {
        get => _premise;
        set => _premise = value;
    }

    // Problem notes data

    private string _notes;
    public string Notes
    {
        get => _notes;
        set => _notes = value;
    }

	// StructureModelTitle Tab Data


	private string _structureTitle;
	/// <summary>
	/// Name of StructureBeatsModel used in structure tab
	/// </summary>
	public string StructureTitle
	{
		get => _structureTitle;
		set => _structureTitle = value;
	}

	private string _structureDescription;
	/// <summary>
	/// Description of StructureBeatsModel used in structure tab
	/// </summary>
	public string StructureDescription
	{
		get => _structureDescription;
		set => _structureDescription = value;
	}

	private ObservableCollection<StructureBeatViewModel> structureBeats;
	/// <summary>
	/// Beat nodes of the structure
	/// </summary>
	public ObservableCollection<StructureBeatViewModel> StructureBeats
	{
		get => structureBeats;
		set => structureBeats = value;
	}

	private string _boundStructure;
	/// <summary>
	/// A problem cannot be bound to more than one structure
	/// </summary>
	public string BoundStructure
	{
		get => _boundStructure;
		set => _boundStructure = value;
	}
	#endregion

	#region Constructors
	public ProblemModel(StoryModel model) : base("New Problem", StoryItemType.Problem, model)
    {
        ProblemType = string.Empty;
        ConflictType = string.Empty;
        ProblemCategory = string.Empty;
        Subject = string.Empty;
        ProblemSource = string.Empty;
        StoryQuestion = string.Empty;
        Protagonist = string.Empty;     // Protagonist Guid 
        ProtGoal = string.Empty;
        ProtMotive = string.Empty;
        ProtConflict = string.Empty;
        Antagonist = string.Empty;      // Antagonist Guid 
        AntagGoal = string.Empty;
        AntagMotive = string.Empty;
        AntagConflict = string.Empty;
        Outcome = string.Empty;
        Method = string.Empty;
        Theme = string.Empty;
        Premise = string.Empty;
        Notes = string.Empty;
        StructureTitle = string.Empty;
        StructureDescription = string.Empty;
		StructureBeats = new();
    }
    public ProblemModel(string name, StoryModel model) : base(name, StoryItemType.Problem, model)
    {
        ProblemType = string.Empty;
        ConflictType = string.Empty;
        ProblemCategory = string.Empty;
        Subject = string.Empty;
        ProblemSource = string.Empty;
        StoryQuestion = string.Empty;
        Protagonist = string.Empty;
        ProtGoal = string.Empty;
        ProtMotive = string.Empty;
        ProtConflict = string.Empty;
        Antagonist = string.Empty;
        AntagGoal = string.Empty;
        AntagMotive = string.Empty;
        AntagConflict = string.Empty;
        Outcome = string.Empty;
        Method = string.Empty;
        Theme = string.Empty;
        Premise = string.Empty;
        Notes = string.Empty;
        StructureTitle = string.Empty;
        StructureDescription = string.Empty;
		StructureBeats = new();
	}
    public ProblemModel(IXmlNode xn, StoryModel model) : base(xn, model)
    {
        ProblemType = string.Empty;
        ConflictType = string.Empty;
        Subject = string.Empty;
        ProblemSource = string.Empty;
        StoryQuestion = string.Empty;
        Protagonist = string.Empty;
        ProtGoal = string.Empty;
        ProtMotive = string.Empty;
        ProtConflict = string.Empty;
        Antagonist = string.Empty;
        AntagGoal = string.Empty;
        AntagMotive = string.Empty;
        AntagConflict = string.Empty;
        Outcome = string.Empty;
        Method = string.Empty;
        Theme = string.Empty;
        Premise = string.Empty;
        Notes = string.Empty;
        StructureTitle = string.Empty;
        StructureBeats = new();
        StructureDescription = string.Empty;
    }

    #endregion
}