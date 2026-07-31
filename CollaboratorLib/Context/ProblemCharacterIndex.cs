using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;

namespace CollaboratorLib.Context;

/// <summary>
/// Role of a character slot on a problem (Problem.Protagonist / Antagonist).
/// </summary>
public enum ProblemCharacterRole
{
    Protagonist,
    Antagonist
}

/// <summary>
/// Resolution of one problem cast slot.
/// </summary>
public enum ProblemCharacterLinkStatus
{
    /// <summary>GUID set and resolves to a Character.</summary>
    Linked,

    /// <summary>GUID is Empty; slot not assigned.</summary>
    Unassigned,

    /// <summary>GUID set but does not resolve to a Character.</summary>
    Unresolved
}

/// <summary>
/// One problem cast slot (always two per problem: protagonist and antagonist).
/// Unassigned and unresolved slots are first-class; they are not omitted from the index.
/// </summary>
public sealed record ProblemCharacterEdge(
    Guid ProblemGuid,
    string ProblemName,
    ProblemCharacterRole Role,
    Guid CharacterGuid,
    string CharacterName,
    bool IsStoryProblem,
    ProblemCharacterLinkStatus Status,
    bool ProblemHasGmcText);

/// <summary>
/// Slot-complete index of problem prot/antag links for StoryContext (issue #107 spine map).
/// Built once per context assembly. Every problem contributes two slots.
/// </summary>
public sealed class ProblemCharacterIndex
{
    public const int DefaultMaxDetailedProblems = 2;

    private readonly List<ProblemCharacterEdge> _edges;
    private readonly Dictionary<Guid, List<ProblemCharacterEdge>> _byCharacter;
    private readonly Dictionary<Guid, List<ProblemCharacterEdge>> _byProblem;
    private readonly HashSet<Guid> _majorCast;
    private readonly Guid _storyProblemGuid;

    private ProblemCharacterIndex(List<ProblemCharacterEdge> edges, Guid storyProblemGuid)
    {
        _edges = edges;
        _storyProblemGuid = storyProblemGuid;
        _byCharacter = new Dictionary<Guid, List<ProblemCharacterEdge>>();
        _byProblem = new Dictionary<Guid, List<ProblemCharacterEdge>>();
        _majorCast = new HashSet<Guid>();

        foreach (var edge in edges)
        {
            if (!_byProblem.TryGetValue(edge.ProblemGuid, out var probList))
            {
                probList = new List<ProblemCharacterEdge>();
                _byProblem[edge.ProblemGuid] = probList;
            }
            probList.Add(edge);

            if (edge.Status != ProblemCharacterLinkStatus.Linked)
                continue;
            if (edge.CharacterGuid == Guid.Empty)
                continue;

            if (!_byCharacter.TryGetValue(edge.CharacterGuid, out var charList))
            {
                charList = new List<ProblemCharacterEdge>();
                _byCharacter[edge.CharacterGuid] = charList;
            }
            charList.Add(edge);
            _majorCast.Add(edge.CharacterGuid);
        }
    }

    /// <summary>All slots (Linked, Unassigned, Unresolved), two per problem.</summary>
    public IReadOnlyList<ProblemCharacterEdge> Edges => _edges;

    /// <summary>Slots that resolve to a character only.</summary>
    public IEnumerable<ProblemCharacterEdge> LinkedEdges =>
        _edges.Where(e => e.Status == ProblemCharacterLinkStatus.Linked);

    public Guid StoryProblemGuid => _storyProblemGuid;

    /// <summary>Characters with at least one Linked slot.</summary>
    public IReadOnlyCollection<Guid> MajorCastGuids => _majorCast;

    /// <summary>
    /// Build the index from all problems. Each problem yields two slots
    /// (protagonist and antagonist), including Unassigned and Unresolved.
    /// </summary>
    public static ProblemCharacterIndex Build(IStoryCADAPI api, StoryModel model)
    {
        if (api == null) throw new ArgumentNullException(nameof(api));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var storyProblemGuid = Guid.Empty;
        var overviewResult = api.GetElementsByType(StoryItemType.StoryOverview);
        if (overviewResult.IsSuccess)
        {
            var overview = overviewResult.Payload?.OfType<OverviewModel>().FirstOrDefault();
            if (overview != null)
                storyProblemGuid = overview.StoryProblem;
        }

        var edges = new List<ProblemCharacterEdge>();
        var problemsResult = api.GetElementsByType(StoryItemType.Problem);
        if (!problemsResult.IsSuccess || problemsResult.Payload == null)
            return new ProblemCharacterIndex(edges, storyProblemGuid);

        foreach (var element in problemsResult.Payload)
        {
            if (element is not ProblemModel problem)
                continue;

            bool isStoryProblem = storyProblemGuid != Guid.Empty && problem.Uuid == storyProblemGuid;
            bool hasGmc = ProblemHasGmcText(problem);
            edges.Add(CreateSlot(api, problem, problem.Protagonist, ProblemCharacterRole.Protagonist, isStoryProblem, hasGmc));
            edges.Add(CreateSlot(api, problem, problem.Antagonist, ProblemCharacterRole.Antagonist, isStoryProblem, hasGmc));
        }

        return new ProblemCharacterIndex(edges, storyProblemGuid);
    }

    private static ProblemCharacterEdge CreateSlot(
        IStoryCADAPI api,
        ProblemModel problem,
        Guid characterGuid,
        ProblemCharacterRole role,
        bool isStoryProblem,
        bool hasGmc)
    {
        var name = problem.Name ?? string.Empty;

        if (characterGuid == Guid.Empty)
        {
            return new ProblemCharacterEdge(
                problem.Uuid, name, role, Guid.Empty, string.Empty,
                isStoryProblem, ProblemCharacterLinkStatus.Unassigned, hasGmc);
        }

        var result = api.GetStoryElement(characterGuid);
        if (result.IsSuccess && result.Payload is CharacterModel character)
        {
            return new ProblemCharacterEdge(
                problem.Uuid, name, role, character.Uuid, character.Name ?? string.Empty,
                isStoryProblem, ProblemCharacterLinkStatus.Linked, hasGmc);
        }

        return new ProblemCharacterEdge(
            problem.Uuid, name, role, characterGuid, string.Empty,
            isStoryProblem, ProblemCharacterLinkStatus.Unresolved, hasGmc);
    }

    public static bool ProblemHasGmcText(ProblemModel problem)
    {
        return !string.IsNullOrWhiteSpace(problem.ProtGoal)
               || !string.IsNullOrWhiteSpace(problem.ProtMotive)
               || !string.IsNullOrWhiteSpace(problem.ProtConflict)
               || !string.IsNullOrWhiteSpace(problem.AntagGoal)
               || !string.IsNullOrWhiteSpace(problem.AntagMotive)
               || !string.IsNullOrWhiteSpace(problem.AntagConflict);
    }

    /// <summary>Linked slots only for this character.</summary>
    public IReadOnlyList<ProblemCharacterEdge> EdgesForCharacter(Guid characterGuid)
    {
        if (characterGuid == Guid.Empty)
            return Array.Empty<ProblemCharacterEdge>();
        return _byCharacter.TryGetValue(characterGuid, out var list)
            ? list
            : Array.Empty<ProblemCharacterEdge>();
    }

    /// <summary>All slots for this problem (typically two).</summary>
    public IReadOnlyList<ProblemCharacterEdge> EdgesForProblem(Guid problemGuid)
    {
        if (problemGuid == Guid.Empty)
            return Array.Empty<ProblemCharacterEdge>();
        return _byProblem.TryGetValue(problemGuid, out var list)
            ? list
            : Array.Empty<ProblemCharacterEdge>();
    }

    /// <summary>
    /// Linked slots for the character, prefer Story Problem, then Protagonist role; cap problems.
    /// </summary>
    public IReadOnlyList<ProblemCharacterEdge> SelectDetailedEdgesForCharacter(
        Guid characterGuid,
        int maxProblems = DefaultMaxDetailedProblems)
    {
        var edges = EdgesForCharacter(characterGuid);
        if (edges.Count == 0)
            return edges;

        var ordered = edges
            .OrderByDescending(e => e.IsStoryProblem)
            .ThenBy(e => e.Role == ProblemCharacterRole.Protagonist ? 0 : 1)
            .ThenBy(e => e.ProblemName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var selectedProblems = new List<Guid>();
        var result = new List<ProblemCharacterEdge>();
        foreach (var edge in ordered)
        {
            if (!selectedProblems.Contains(edge.ProblemGuid))
            {
                if (selectedProblems.Count >= maxProblems)
                    continue;
                selectedProblems.Add(edge.ProblemGuid);
            }
            if (selectedProblems.Contains(edge.ProblemGuid))
                result.Add(edge);
        }

        return result;
    }
}
