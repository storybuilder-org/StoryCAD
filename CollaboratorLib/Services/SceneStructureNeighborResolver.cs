using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;

namespace StoryCollaborator.Services;

/// <summary>
/// Collaborator #174: resolve owner Problem and beat-neighbor Scenes for Scene Summary.
/// Full elements for the gather map; no StoryContext prose.
/// </summary>
public sealed class SceneStructureNeighborResolver
{
    private readonly IStoryCADAPI _api;
    private readonly ILogger<SceneStructureNeighborResolver>? _logger;

    public SceneStructureNeighborResolver(IStoryCADAPI api, ILogger<SceneStructureNeighborResolver>? logger = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger;
    }

    public enum OwnerState
    {
        None,
        Unique,
        Ambiguous,
        ExplorerParentOnly
    }

    /// <summary>
    /// Collaborator #208: owner state for SceneBuilder. Separate from Scene Summary
    /// <see cref="OwnerState"/> so ResolveForSceneSummary is unchanged.
    /// </summary>
    public enum SceneBuilderOwnerState
    {
        None,
        UniqueSubproblem,
        ExplorerParentSubproblem,
        AmbiguousSubproblems,
        StoryProblemBail,
        EmptyCategoryBail
    }

    public sealed class ResolveResult
    {
        public ProblemModel? OwnerProblem { get; init; }
        public SceneModel? PrecedingScene { get; init; }
        public SceneModel? NextScene { get; init; }
        public OwnerState OwnerState { get; init; }
        public IReadOnlyList<ProblemModel> OwnerCandidates { get; init; } = Array.Empty<ProblemModel>();
        public IReadOnlyList<string> StatusLines { get; init; } = Array.Empty<string>();
    }

    public sealed class NeighborPair
    {
        public SceneModel? PrecedingScene { get; init; }
        public SceneModel? NextScene { get; init; }
    }

    public sealed class SceneBuilderResolveResult
    {
        public IReadOnlyList<ProblemModel> ContributingProblems { get; init; } = Array.Empty<ProblemModel>();
        public ProblemModel? OwnerProblem { get; init; }
        public SceneModel? PrecedingScene { get; init; }
        public SceneModel? NextScene { get; init; }
        public SceneBuilderOwnerState OwnerState { get; init; }
        public string? BailReason { get; init; }
        public IReadOnlyList<string> StatusLines { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Full resolve for Scene Summary gather (no UI).
    /// Calls FindNeighbors only for Unique, or ExplorerParentOnly when Scene is on that sheet.
    /// </summary>
    public ResolveResult ResolveForSceneSummary(StoryElement scene)
    {
        if (scene == null || scene.ElementType != StoryItemType.Scene)
        {
            return new ResolveResult
            {
                OwnerState = OwnerState.None,
                StatusLines = new[] { "Scene Summary: no Scene element; owner omitted." }
            };
        }

        var status = new List<string>();
        var owners = FindStructureOwners(scene.Uuid, status, "Scene Summary");
        var explorerParent = GetExplorerParentProblem(scene);
        var (state, owner, candidates) = ResolveOwner(scene, owners, explorerParent);

        if (state == OwnerState.Ambiguous)
        {
            var names = string.Join(", ", candidates.Select(p => p.Name));
            status.Add($"Scene Summary: owner ambiguous ({candidates.Count}): {names}. Choose one or skip.");
            return new ResolveResult
            {
                OwnerState = OwnerState.Ambiguous,
                OwnerCandidates = candidates,
                StatusLines = status
            };
        }

        if (state == OwnerState.None)
        {
            status.Add("Scene Summary: no owner Problem (not on a structure; explorer parent is not a Problem).");
            return new ResolveResult
            {
                OwnerState = OwnerState.None,
                StatusLines = status
            };
        }

        // Unique or ExplorerParentOnly
        SceneModel? preceding = null;
        SceneModel? next = null;
        if (owner != null)
        {
            bool onSheet = owners.Any(o => o.Uuid == owner.Uuid);
            if (state == OwnerState.Unique || (state == OwnerState.ExplorerParentOnly && onSheet))
            {
                var neighbors = FindNeighbors(owner, scene.Uuid);
                preceding = neighbors.PrecedingScene;
                next = neighbors.NextScene;
            }

            status.Add($"Scene Summary: owner Problem = {owner.Name}");
            status.Add(preceding != null
                ? $"Scene Summary: preceding Scene = {preceding.Name}"
                : "Scene Summary: preceding Scene = (none)");
            status.Add(next != null
                ? $"Scene Summary: next Scene = {next.Name}"
                : "Scene Summary: next Scene = (none)");
        }

        return new ResolveResult
        {
            OwnerProblem = owner,
            PrecedingScene = preceding,
            NextScene = next,
            OwnerState = state,
            OwnerCandidates = candidates,
            StatusLines = status
        };
    }

    /// <summary>
    /// Problems whose structure assigns <paramref name="sceneGuid"/> on any beat.
    /// </summary>
    public IReadOnlyList<ProblemModel> FindStructureOwners(
        Guid sceneGuid,
        List<string>? statusLines = null,
        string scanLabel = "Scene Summary")
    {
        var owners = new List<ProblemModel>();
        var problemsResult = _api.GetElementsByType(StoryItemType.Problem);
        if (!problemsResult.IsSuccess || problemsResult.Payload == null)
        {
            statusLines?.Add($"{scanLabel}: could not list Problems for structure scan.");
            _logger?.LogWarning("FindStructureOwners: GetElementsByType(Problem) failed: {Msg}",
                problemsResult.ErrorMessage);
            return owners;
        }

        foreach (var el in problemsResult.Payload)
        {
            if (el is not ProblemModel problem)
                continue;

            var structure = _api.GetProblemStructure(problem.Uuid);
            if (!structure.IsSuccess || structure.Payload.Beats == null)
            {
                _logger?.LogDebug("FindStructureOwners: skip Problem {Name} (structure failed)", problem.Name);
                continue;
            }

            foreach (var beat in structure.Payload.Beats)
            {
                if (beat.LinkedElement is Guid linked && linked == sceneGuid)
                {
                    owners.Add(problem);
                    break;
                }
            }
        }

        return owners;
    }

    /// <summary>
    /// Explorer parent when parent node resolves to a Problem.
    /// </summary>
    public ProblemModel? GetExplorerParentProblem(StoryElement scene)
    {
        var parentUuid = scene.Node?.Parent?.Uuid ?? Guid.Empty;
        if (parentUuid == Guid.Empty)
            return null;

        var result = _api.GetStoryElement(parentUuid);
        if (result.IsSuccess && result.Payload is ProblemModel problem)
            return problem;

        return null;
    }

    /// <summary>
    /// Apply multi-owner law (design §5.10).
    /// </summary>
    public (OwnerState State, ProblemModel? Owner, IReadOnlyList<ProblemModel> Candidates)
        ResolveOwner(StoryElement scene, IReadOnlyList<ProblemModel> owners, ProblemModel? explorerParent)
    {
        var ownerList = owners?.ToList() ?? new List<ProblemModel>();

        if (ownerList.Count == 0)
        {
            if (explorerParent != null)
                return (OwnerState.ExplorerParentOnly, explorerParent, Array.Empty<ProblemModel>());
            return (OwnerState.None, null, Array.Empty<ProblemModel>());
        }

        if (ownerList.Count == 1)
            return (OwnerState.Unique, ownerList[0], Array.Empty<ProblemModel>());

        // Multi-owner: prefer explorer parent when it is among owners
        if (explorerParent != null)
        {
            var match = ownerList.FirstOrDefault(o => o.Uuid == explorerParent.Uuid);
            if (match != null)
                return (OwnerState.Unique, match, Array.Empty<ProblemModel>());
        }

        return (OwnerState.Ambiguous, null, ownerList);
    }

    /// <summary>
    /// Immediate previous and next neighbor Scenes on the owner sheet (1+1).
    /// Prev/next Problem slots expand to last/first nested Scene.
    /// </summary>
    public NeighborPair FindNeighbors(ProblemModel owner, Guid sceneGuid)
    {
        if (owner == null)
            return new NeighborPair();

        var structure = _api.GetProblemStructure(owner.Uuid);
        if (!structure.IsSuccess || structure.Payload.Beats == null)
            return new NeighborPair();

        var beats = structure.Payload.Beats.ToList();
        int index = -1;
        for (int i = 0; i < beats.Count; i++)
        {
            if (beats[i].LinkedElement is Guid g && g == sceneGuid)
            {
                index = i;
                break; // first hit if duplicate on one sheet
            }
        }

        if (index < 0)
            return new NeighborPair();

        if (beats.Count(b => b.LinkedElement is Guid g && g == sceneGuid) > 1)
            _logger?.LogDebug("FindNeighbors: Scene {Guid} appears more than once on {Problem}; using first index",
                sceneGuid, owner.Name);

        SceneModel? preceding = null;
        if (index > 0)
            preceding = ResolvePrecedingFromLinked(beats[index - 1].LinkedElement);

        SceneModel? next = null;
        if (index < beats.Count - 1)
            next = ResolveNextFromLinked(beats[index + 1].LinkedElement);

        return new NeighborPair { PrecedingScene = preceding, NextScene = next };
    }

    /// <summary>
    /// Resolve previous slot: Scene as-is; Problem → last Scene under that structure.
    /// </summary>
    public SceneModel? ResolvePrecedingFromLinked(Guid? linked)
    {
        if (linked is not Guid guid || guid == Guid.Empty)
            return null;

        var elResult = _api.GetStoryElement(guid);
        if (!elResult.IsSuccess || elResult.Payload == null)
            return null;

        if (elResult.Payload is SceneModel scene)
            return scene;

        if (elResult.Payload is ProblemModel problem)
            return LastSceneInStructure(problem, new HashSet<Guid>());

        return null;
    }

    /// <summary>
    /// Resolve next slot: Scene as-is; Problem → first Scene under that structure.
    /// </summary>
    public SceneModel? ResolveNextFromLinked(Guid? linked)
    {
        if (linked is not Guid guid || guid == Guid.Empty)
            return null;

        var elResult = _api.GetStoryElement(guid);
        if (!elResult.IsSuccess || elResult.Payload == null)
            return null;

        if (elResult.Payload is SceneModel scene)
            return scene;

        if (elResult.Payload is ProblemModel problem)
            return FirstSceneInStructure(problem, new HashSet<Guid>());

        return null;
    }

    // Fix FindNeighbors to use ResolvePreceding/NextFromLinked
    // (rewritten below in patch if needed)

    public SceneModel? LastSceneInStructure(ProblemModel problem, HashSet<Guid> visited)
    {
        if (problem == null)
            return null;
        if (!visited.Add(problem.Uuid))
            return null;

        var structure = _api.GetProblemStructure(problem.Uuid);
        if (!structure.IsSuccess || structure.Payload.Beats == null)
            return null;

        SceneModel? last = null;
        foreach (var beat in structure.Payload.Beats)
        {
            if (beat.LinkedElement is not Guid guid || guid == Guid.Empty)
                continue;

            var elResult = _api.GetStoryElement(guid);
            if (!elResult.IsSuccess || elResult.Payload == null)
                continue;

            if (elResult.Payload is SceneModel scene)
            {
                last = scene;
            }
            else if (elResult.Payload is ProblemModel nested)
            {
                var nestedLast = LastSceneInStructure(nested, visited);
                if (nestedLast != null)
                    last = nestedLast;
            }
        }

        return last;
    }

    public SceneModel? FirstSceneInStructure(ProblemModel problem, HashSet<Guid> visited)
    {
        if (problem == null)
            return null;
        if (!visited.Add(problem.Uuid))
            return null;

        var structure = _api.GetProblemStructure(problem.Uuid);
        if (!structure.IsSuccess || structure.Payload.Beats == null)
            return null;

        foreach (var beat in structure.Payload.Beats)
        {
            if (beat.LinkedElement is not Guid guid || guid == Guid.Empty)
                continue;

            var elResult = _api.GetStoryElement(guid);
            if (!elResult.IsSuccess || elResult.Payload == null)
                continue;

            if (elResult.Payload is SceneModel scene)
                return scene;

            if (elResult.Payload is ProblemModel nested)
            {
                var nestedFirst = FirstSceneInStructure(nested, visited);
                if (nestedFirst != null)
                    return nestedFirst;
            }
        }

        return null;
    }

    public const string SceneBuilderStoryProblemBailMessage =
        "Scene Builder does not run on a Story Problem. Use Problem Builder or Structure for story-problem beats.";

    public const string SceneBuilderEmptyCategoryBailMessage =
        "Set Problem Category on the owning problem before Scene Builder.";

    /// <summary>
    /// Collaborator #208: contributing Problems, owning problem, Story Problem / empty-category bail,
    /// and 1+1 neighbors from OwnerProblem when the Scene GUID is on that sheet.
    /// </summary>
    public SceneBuilderResolveResult ResolveForSceneBuilder(StoryElement scene)
    {
        if (scene == null || scene.ElementType != StoryItemType.Scene)
        {
            return new SceneBuilderResolveResult
            {
                OwnerState = SceneBuilderOwnerState.None,
                StatusLines = new[] { "Scene Builder: no Scene element; owner omitted." }
            };
        }

        var status = new List<string>();
        var owners = FindStructureOwners(scene.Uuid, status, "Scene Builder");
        var explorerParent = GetExplorerParentProblem(scene);
        var overviewStoryProblem = GetOverviewStoryProblemUuid();
        var (state, owner, bail) = ResolveSceneBuilderOwner(owners, explorerParent, overviewStoryProblem);

        if (state is SceneBuilderOwnerState.StoryProblemBail or SceneBuilderOwnerState.EmptyCategoryBail)
        {
            if (owner != null)
                status.Add($"Scene Builder: owner Problem = {owner.Name}");
            if (!string.IsNullOrEmpty(bail))
                status.Add(bail);
            return new SceneBuilderResolveResult
            {
                ContributingProblems = owners,
                OwnerProblem = owner,
                OwnerState = state,
                BailReason = bail,
                StatusLines = status
            };
        }

        SceneModel? preceding = null;
        SceneModel? next = null;
        if (owner != null)
        {
            bool onSheet = owners.Any(o => o.Uuid == owner.Uuid);
            if (onSheet)
            {
                var neighbors = FindNeighbors(owner, scene.Uuid);
                preceding = neighbors.PrecedingScene;
                next = neighbors.NextScene;
            }

            status.Add($"Scene Builder: owner Problem = {owner.Name}");
            status.Add(preceding != null
                ? $"Scene Builder: preceding Scene = {preceding.Name}"
                : "Scene Builder: preceding Scene = (none)");
            status.Add(next != null
                ? $"Scene Builder: next Scene = {next.Name}"
                : "Scene Builder: next Scene = (none)");
        }
        else if (state == SceneBuilderOwnerState.AmbiguousSubproblems)
        {
            var names = string.Join(", ", owners.Where(o => !IsStoryProblem(o, overviewStoryProblem)).Select(p => p.Name));
            status.Add($"Scene Builder: ambiguous subproblems ({owners.Count} contributing): {names}. Neighbors omitted.");
        }
        else
        {
            status.Add("Scene Builder: no owner Problem (orphan); neighbors omitted.");
        }

        status.Add($"Scene Builder: contributing Problems = {owners.Count}");

        return new SceneBuilderResolveResult
        {
            ContributingProblems = owners,
            OwnerProblem = owner,
            PrecedingScene = preceding,
            NextScene = next,
            OwnerState = state,
            StatusLines = status
        };
    }

    internal Guid GetOverviewStoryProblemUuid()
    {
        var result = _api.GetElementsByType(StoryItemType.StoryOverview);
        if (!result.IsSuccess || result.Payload == null || result.Payload.Count == 0)
            return Guid.Empty;
        if (result.Payload[0] is OverviewModel overview)
            return overview.StoryProblem;
        return Guid.Empty;
    }

    internal bool IsStoryProblem(ProblemModel problem, Guid overviewStoryProblem)
    {
        if (overviewStoryProblem != Guid.Empty && problem.Uuid == overviewStoryProblem)
            return true;
        return string.Equals(
            (problem.ProblemCategory ?? string.Empty).Trim(),
            Collaborator.StoryProblemCategoryListValue,
            StringComparison.OrdinalIgnoreCase);
    }

    internal (SceneBuilderOwnerState State, ProblemModel? Owner, string? BailReason)
        ResolveSceneBuilderOwner(
            IReadOnlyList<ProblemModel> owners,
            ProblemModel? explorerParent,
            Guid overviewStoryProblem)
    {
        bool IsSp(ProblemModel p) => IsStoryProblem(p, overviewStoryProblem);
        bool IsCategorizedSubproblem(ProblemModel p) =>
            !IsSp(p) && !string.IsNullOrWhiteSpace(p.ProblemCategory);

        var ownerList = owners?.ToList() ?? new List<ProblemModel>();

        // 5.4 step 1
        if (explorerParent != null && IsSp(explorerParent))
            return (SceneBuilderOwnerState.StoryProblemBail, explorerParent, SceneBuilderStoryProblemBailMessage);

        // 5.4 step 2
        if (explorerParent != null && string.IsNullOrWhiteSpace(explorerParent.ProblemCategory))
        {
            var categorized = ownerList.Where(IsCategorizedSubproblem).ToList();
            if (categorized.Count == 1)
                return (SceneBuilderOwnerState.UniqueSubproblem, categorized[0], null);
            return (SceneBuilderOwnerState.EmptyCategoryBail, explorerParent, SceneBuilderEmptyCategoryBailMessage);
        }

        // 5.4 step 3
        if (explorerParent != null)
            return (SceneBuilderOwnerState.ExplorerParentSubproblem, explorerParent, null);

        // 5.4 step 4
        var nonStory = ownerList.Where(o => !IsSp(o)).ToList();
        if (nonStory.Count == 1)
        {
            var only = nonStory[0];
            if (string.IsNullOrWhiteSpace(only.ProblemCategory))
                return (SceneBuilderOwnerState.EmptyCategoryBail, only, SceneBuilderEmptyCategoryBailMessage);
            return (SceneBuilderOwnerState.UniqueSubproblem, only, null);
        }

        // 5.4 step 5
        if (ownerList.Count > 0 && ownerList.All(IsSp))
            return (SceneBuilderOwnerState.StoryProblemBail, ownerList[0], SceneBuilderStoryProblemBailMessage);

        // 5.4 step 6
        if (nonStory.Count >= 2)
            return (SceneBuilderOwnerState.AmbiguousSubproblems, null, null);

        // 5.4 step 7
        return (SceneBuilderOwnerState.None, null, null);
    }
}
