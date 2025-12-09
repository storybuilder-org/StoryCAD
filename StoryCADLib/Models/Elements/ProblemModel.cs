using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Models;

public class ProblemModel : StoryElement
{
    #region Properties

    [JsonIgnore] private string _problemType;

    [JsonInclude]
    [JsonPropertyName("ProblemType")]
    public string ProblemType
    {
        get => _problemType;
        set => _problemType = value;
    }

    [JsonIgnore] private string _conflictType;

    [JsonInclude]
    [JsonPropertyName("ConflictType")]
    public string ConflictType
    {
        get => _conflictType;
        set => _conflictType = value;
    }

    [JsonIgnore] private string _problemCategory;

    [JsonInclude]
    [JsonPropertyName("ProblemCategory")]
    public string ProblemCategory
    {
        get => _problemCategory;
        set => _problemCategory = value;
    }

    [JsonIgnore] private string _subject;

    [JsonInclude]
    [JsonPropertyName("Subject")]
    public string Subject
    {
        get => _subject;
        set => _subject = value;
    }

    [JsonIgnore] private string _problemSource;

    [JsonInclude]
    [JsonPropertyName("ProblemSource")]
    public string ProblemSource
    {
        get => _problemSource;
        set => _problemSource = value;
    }
    // Problem protagonist data

    [JsonIgnore] private Guid _protagonist;

    [JsonInclude]
    [JsonPropertyName("Protagonist")]
    public Guid Protagonist
    {
        get => _protagonist;
        set => _protagonist = value;
    }

    [JsonIgnore] private string _protGoal;

    [JsonInclude]
    [JsonPropertyName("ProtGoal")]
    public string ProtGoal
    {
        get => _protGoal;
        set => _protGoal = value;
    }

    [JsonIgnore] private string _protMotive;

    [JsonInclude]
    [JsonPropertyName("ProtMotive")]
    public string ProtMotive
    {
        get => _protMotive;
        set => _protMotive = value;
    }

    [JsonIgnore] private string _protConflict;

    [JsonInclude]
    [JsonPropertyName("ProtConflict")]
    public string ProtConflict
    {
        get => _protConflict;
        set => _protConflict = value;
    }

    // Problem antagonist data

    [JsonIgnore] private Guid _antagonist;

    [JsonInclude]
    [JsonPropertyName("Antagonist")]
    public Guid Antagonist
    {
        get => _antagonist;
        set => _antagonist = value;
    }

    [JsonIgnore] private string _antagGoal;

    [JsonInclude]
    [JsonPropertyName("AntagGoal")]
    public string AntagGoal
    {
        get => _antagGoal;
        set => _antagGoal = value;
    }

    [JsonIgnore] private string _antagMotive;

    [JsonInclude]
    [JsonPropertyName("AntagMotive")]
    public string AntagMotive
    {
        get => _antagMotive;
        set => _antagMotive = value;
    }

    [JsonIgnore] private string _antagConflict;

    [JsonInclude]
    [JsonPropertyName("AntagConflict")]
    public string AntagConflict
    {
        get => _antagConflict;
        set => _antagConflict = value;
    }

    // Problem resolution data

    [JsonIgnore] private string _outcome;

    [JsonInclude]
    [JsonPropertyName("Outcome")]
    public string Outcome
    {
        get => _outcome;
        set => _outcome = value;
    }

    [JsonIgnore] private string _method;

    [JsonInclude]
    [JsonPropertyName("Method")]
    public string Method
    {
        get => _method;
        set => _method = value;
    }

    [JsonIgnore] private string _theme;

    [JsonInclude]
    [JsonPropertyName("Theme")]
    public string Theme
    {
        get => _theme;
        set => _theme = value;
    }

    [JsonIgnore] private string _premise;

    [JsonInclude]
    [JsonPropertyName("Premise")]
    public string Premise
    {
        get => _premise;
        set => _premise = value;
    }

    // Problem notes data

    [JsonIgnore] private string _notes;

    [JsonInclude]
    [JsonPropertyName("Notes")]
    public string Notes
    {
        get => _notes;
        set => _notes = value;
    }

    // StructureModelTitle Tab Data

    [JsonIgnore] private string _structureTitle;

    /// <summary>
    ///     Name of StructureBeatsModel used in structure tab
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("StructureTitle")]
    public string StructureTitle
    {
        get => _structureTitle;
        set => _structureTitle = value;
    }

    [JsonIgnore] private string _structureDescription;

    /// <summary>
    ///     Description of StructureBeatsModel used in structure tab
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("StructureDescription")]
    public string StructureDescription
    {
        get => _structureDescription;
        set => _structureDescription = value;
    }

    [JsonIgnore] private ObservableCollection<StructureBeatViewModel> structureBeats;

    /// <summary>
    ///     Beat nodes of the structure
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("StructureBeats")]
    public ObservableCollection<StructureBeatViewModel> StructureBeats
    {
        get => structureBeats;
        set => structureBeats = value;
    }

    [JsonIgnore] private string _boundStructure;

    /// <summary>
    ///     A problem cannot be bound to more than one structure
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("BoundStructure")]
    public string BoundStructure
    {
        get => _boundStructure;
        set => _boundStructure = value;
    }

    #endregion

    #region Constructors

    public ProblemModel(StoryModel model, StoryNodeItem Node) : base("New Problem", StoryItemType.Problem, model, Node)
    {
        ProblemType = string.Empty;
        ConflictType = string.Empty;
        ProblemCategory = string.Empty;
        Subject = string.Empty;
        ProblemSource = string.Empty;
        Description = string.Empty;
        Protagonist = Guid.Empty; // Protagonist Guid 
        ProtGoal = string.Empty;
        ProtMotive = string.Empty;
        ProtConflict = string.Empty;
        Antagonist = Guid.Empty; // Antagonist Guid 
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
        StructureBeats = new ObservableCollection<StructureBeatViewModel>();
        BoundStructure = string.Empty;
    }

    public ProblemModel(string name, StoryModel model, StoryNodeItem Node) : base(name, StoryItemType.Problem, model,
        Node)
    {
        ProblemType = string.Empty;
        ConflictType = string.Empty;
        ProblemCategory = string.Empty;
        Subject = string.Empty;
        ProblemSource = string.Empty;
        Description = string.Empty;
        Protagonist = Guid.Empty;
        ProtGoal = string.Empty;
        ProtMotive = string.Empty;
        ProtConflict = string.Empty;
        Antagonist = Guid.Empty;
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
        StructureBeats = new ObservableCollection<StructureBeatViewModel>();
        BoundStructure = string.Empty;
    }

    public ProblemModel()
    {
    }

    #endregion
}
