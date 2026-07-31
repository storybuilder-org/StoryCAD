using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;

namespace CollaboratorLib.Context;

/// <summary>
/// Role of a character on a problem (Problem.Protagonist / Antagonist links).
/// </summary>
public enum ProblemCharacterRole
{
    Protagonist,
    Antagonist
}

/// <summary>
/// One resolvable Problem → Character cast link.
/// </summary>
public sealed record ProblemCharacterEdge(
    Guid ProblemGuid,
    string ProblemName,
    ProblemCharacterRole Role,
    Guid CharacterGuid,
    string CharacterName,
    bool IsStoryProblem);

/// <summary>
/// Reverse index of problem prot/antag links for StoryContext (issue #107 spine map).
/// Built once per context assembly from existing Problem GUID fields.
/// </summary>
public sealed class ProblemCharacterIndex
{
    public const int DefaultMaxDetailedProblems = 2;

    private readonly List<ProblemCharacterEdge> _edges;
    private readonly Dictionary<Guid, List<ProblemCharacterEdge>> _byCharacter;
    private readonly Dictionary<Guid, List<ProblemCharacterEdge>> _byProblem;
    private readonly HashSet<Guid> _majorCast;
    private readonly Guid _storyProblemGuid;

    private ProblemCharacterIndex(
        List<ProblemCharacterEdge> edges,
        Guid storyProblemGuid)
    {
        _edges = edges;
        _storyProblemGuid = storyProblemGuid;
        _byCharacter = new Dictionary<Guid, List<ProblemCharacterEdge>>();
        _byProblem = new Dictionary<Guid, List<ProblemCharacterEdge>>();
        _majorCast = new HashSet<Guid>();

        foreach (var edge in edges)
        {
            if (!_byCharacter.TryGetValue(edge.CharacterGuid, out var charList))
            {
                charList = new List<ProblemCharacterEdge>();
                _byCharacter[edge.CharacterGuid] = charList;
            }
            charList.Add(edge);

            if (!_byProblem.TryGetValue(edge.ProblemGuid, out var probList))
            {
                probList = new List<ProblemCharacterEdge>();
                _byProblem[edge.ProblemGuid] = probList;
            }
            probList.Add(edge);

            _majorCast.Add(edge.CharacterGuid);
        }
    }

    public IReadOnlyList<ProblemCharacterEdge> Edges => _edges;

    public Guid StoryProblemGuid => _storyProblemGuid;

    public IReadOnlyCollection<Guid> MajorCastGuids => _majorCast;

    /// <summary>
    /// Build the index from all problems in the open outline.
    /// Empty prot/antag GUIDs and unresolvable GUIDs produce no edge.
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
            TryAddEdge(api, edges, problem, problem.Protagonist, ProblemCharacterRole.Protagonist, isStoryProblem);
            TryAddEdge(api, edges, problem, problem.Antagonist, ProblemCharacterRole.Antagonist, isStoryProblem);
        }

        return new ProblemCharacterIndex(edges, storyProblemGuid);
    }

    private static void TryAddEdge(
        IStoryCADAPI api,
        List<ProblemCharacterEdge> edges,
        ProblemModel problem,
        Guid characterGuid,
        ProblemCharacterRole role,
        bool isStoryProblem)
    {
        if (characterGuid == Guid.Empty)
            return;

        var result = api.GetStoryElement(characterGuid);
        if (!result.IsSuccess || result.Payload is not CharacterModel character)
            return;

        edges.Add(new ProblemCharacterEdge(
            problem.Uuid,
            problem.Name ?? string.Empty,
            role,
            character.Uuid,
            character.Name ?? string.Empty,
            isStoryProblem));
    }

    public IReadOnlyList<ProblemCharacterEdge> EdgesForCharacter(Guid characterGuid)
    {
        if (characterGuid == Guid.Empty)
            return Array.Empty<ProblemCharacterEdge>();
        return _byCharacter.TryGetValue(characterGuid, out var list)
            ? list
            : Array.Empty<ProblemCharacterEdge>();
    }

    public IReadOnlyList<ProblemCharacterEdge> EdgesForProblem(Guid problemGuid)
    {
        if (problemGuid == Guid.Empty)
            return Array.Empty<ProblemCharacterEdge>();
        return _byProblem.TryGetValue(problemGuid, out var list)
            ? list
            : Array.Empty<ProblemCharacterEdge>();
    }

    /// <summary>
    /// Prefer Story Problem edges, then Protagonist, then Antagonist; cap count.
    /// </summary>
    public IReadOnlyList<ProblemCharacterEdge> SelectDetailedEdgesForCharacter(
        Guid characterGuid,
        int maxProblems = DefaultMaxDetailedProblems)
    {
        var edges = EdgesForCharacter(characterGuid);
        if (edges.Count == 0)
            return edges;

        // Distinct problems in priority order, keep both roles if same problem
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
