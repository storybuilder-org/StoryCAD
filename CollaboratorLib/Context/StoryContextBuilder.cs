using System.Text;
using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;

namespace CollaboratorLib.Context;

/// <summary>
/// Builds structured context strings for workflow prompts.
/// Gathers relevant story information based on ContextSpec requirements.
/// Detects development phase and provides appropriate context.
/// </summary>
public class StoryContextBuilder
{
    private readonly IStoryCADAPI _api;

    public StoryContextBuilder(IStoryCADAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <summary>
    /// Build a context string for the given element and specification.
    /// </summary>
    /// <param name="targetElement">The element being processed (may be null for story-level context)</param>
    /// <param name="spec">What context to include</param>
    /// <param name="model">The story model to gather context from</param>
    /// <returns>Formatted context string for prompt injection</returns>
    public string BuildContext(StoryElement? targetElement, ContextSpec spec, StoryModel model)
    {
        if (model == null)
            return string.Empty;

        var sb = new StringBuilder();

        // Detect development phase
        var phase = DetectDevelopmentPhase(model);
        AppendPhaseContext(sb, phase);

        if (spec.IncludeStoryConstraints)
            AppendStoryConstraints(sb, model);

        if (spec.IncludeBeatHierarchy && targetElement is ProblemModel problem)
            AppendBeatHierarchy(sb, problem, model);

        if (spec.IncludeCharacterContext && targetElement != null)
            AppendCharacterContext(sb, targetElement, model);

        if (spec.IncludePrecedingEvents && targetElement is ProblemModel problemForEvents)
            AppendPrecedingEvents(sb, problemForEvents, model, spec.MaxPrecedingBeats);

        return sb.ToString();
    }

    #region Phase Detection

    /// <summary>
    /// Development phases based on outline state
    /// </summary>
    public enum DevelopmentPhase
    {
        Ideation,           // No overview or empty Type/Genre/Premise
        ProblemDevelopment, // Overview set, but no Story Problem or no beat sheet
        StructureBuilding,  // Story Problem has beat sheet, hierarchy being built
        SceneWork           // Scenes assigned to beats
    }

    /// <summary>
    /// Detect the current development phase by examining the StoryModel
    /// </summary>
    private DevelopmentPhase DetectDevelopmentPhase(StoryModel model)
    {
        var overview = GetOverview(model);

        // No overview or missing basic constraints = Ideation
        if (overview == null ||
            string.IsNullOrWhiteSpace(overview.StoryType) ||
            string.IsNullOrWhiteSpace(overview.StoryGenre) ||
            string.IsNullOrWhiteSpace(overview.Premise))
        {
            return DevelopmentPhase.Ideation;
        }

        // No Story Problem assigned = Problem Development
        if (overview.StoryProblem == Guid.Empty)
        {
            return DevelopmentPhase.ProblemDevelopment;
        }

        var storyProblem = ResolveElement(overview.StoryProblem, model) as ProblemModel;
        if (storyProblem == null)
        {
            return DevelopmentPhase.ProblemDevelopment;
        }

        // Story Problem exists but no beat sheet = Problem Development
        if (storyProblem.StructureBeats == null || storyProblem.StructureBeats.Count == 0)
        {
            return DevelopmentPhase.ProblemDevelopment;
        }

        // Check if any beats have scenes assigned (vs just sub-problems)
        bool hasSceneAssignments = HasSceneAssignments(storyProblem, model);

        return hasSceneAssignments ? DevelopmentPhase.SceneWork : DevelopmentPhase.StructureBuilding;
    }

    /// <summary>
    /// Check if any beats in the hierarchy have scenes assigned
    /// </summary>
    private bool HasSceneAssignments(ProblemModel problem, StoryModel model)
    {
        if (problem.StructureBeats == null)
            return false;

        foreach (var beat in problem.StructureBeats)
        {
            if (beat.Guid == Guid.Empty)
                continue;

            var element = ResolveElement(beat.Guid, model);
            if (element == null)
                continue;

            if (element.ElementType == StoryItemType.Scene)
                return true;

            // Recursively check sub-problems
            if (element is ProblemModel subProblem && HasSceneAssignments(subProblem, model))
                return true;
        }

        return false;
    }

    private void AppendPhaseContext(StringBuilder sb, DevelopmentPhase phase)
    {
        sb.AppendLine("## Development Phase");
        sb.AppendLine(phase switch
        {
            DevelopmentPhase.Ideation => "Early ideation - establishing basic story parameters",
            DevelopmentPhase.ProblemDevelopment => "Problem/character development - building story elements",
            DevelopmentPhase.StructureBuilding => "Structure building - organizing problems into beat sheet",
            DevelopmentPhase.SceneWork => "Scene work - detailed scene-level development",
            _ => "Unknown phase"
        });
        sb.AppendLine();
    }

    #endregion

    #region Story Constraints

    private void AppendStoryConstraints(StringBuilder sb, StoryModel model)
    {
        var overview = GetOverview(model);
        if (overview == null)
            return;

        sb.AppendLine("## Story Constraints");

        if (!string.IsNullOrWhiteSpace(overview.StoryType))
            sb.AppendLine($"Type: {overview.StoryType}");

        if (!string.IsNullOrWhiteSpace(overview.StoryGenre))
            sb.AppendLine($"Genre: {overview.StoryGenre}");

        if (!string.IsNullOrWhiteSpace(overview.Premise))
            sb.AppendLine($"Premise: {overview.Premise}");

        sb.AppendLine();
    }

    #endregion

    #region Beat Hierarchy (Recursive Traversal)

    private void AppendBeatHierarchy(StringBuilder sb, ProblemModel targetProblem, StoryModel model)
    {
        sb.AppendLine("## Problem Structure");

        var overview = GetOverview(model);
        if (overview == null || overview.StoryProblem == Guid.Empty)
        {
            // No Story Problem defined - show what we can
            sb.AppendLine($"Current Problem: {targetProblem.Name}");
            sb.AppendLine("(Story Problem not yet defined in Overview)");
            sb.AppendLine();
            return;
        }

        var storyProblem = ResolveElement(overview.StoryProblem, model) as ProblemModel;
        if (storyProblem == null)
        {
            sb.AppendLine($"Current Problem: {targetProblem.Name}");
            sb.AppendLine("(Story Problem reference invalid)");
            sb.AppendLine();
            return;
        }

        // Find the path to the target problem in the hierarchy
        var path = new List<(ProblemModel Problem, int BeatIndex, string BeatTitle)>();
        bool found = FindProblemInHierarchy(storyProblem, targetProblem.Uuid, model, path);

        if (!found)
        {
            // Target problem not in beat sheet hierarchy - show standalone
            sb.AppendLine($"Story Problem: {storyProblem.Name}");
            sb.AppendLine($"Current Problem: {targetProblem.Name}");
            sb.AppendLine("(Not yet assigned to beat sheet structure)");
            sb.AppendLine();
            return;
        }

        // Show the hierarchy with position marker
        sb.AppendLine($"Story Problem: {storyProblem.Name}");

        if (storyProblem.StructureBeats != null && storyProblem.StructureBeats.Count > 0)
        {
            sb.AppendLine($"Beat Sheet: {storyProblem.StructureTitle ?? "Custom"}");
            AppendBeatList(sb, storyProblem, targetProblem.Uuid, model, 0);
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Recursively find a problem in the beat sheet hierarchy
    /// </summary>
    /// <returns>True if found, path contains the route to it</returns>
    private bool FindProblemInHierarchy(ProblemModel currentProblem, Guid targetGuid, StoryModel model,
        List<(ProblemModel Problem, int BeatIndex, string BeatTitle)> path)
    {
        if (currentProblem.Uuid == targetGuid)
            return true;

        if (currentProblem.StructureBeats == null)
            return false;

        for (int i = 0; i < currentProblem.StructureBeats.Count; i++)
        {
            var beat = currentProblem.StructureBeats[i];
            if (beat.Guid == Guid.Empty)
                continue;

            if (beat.Guid == targetGuid)
            {
                path.Add((currentProblem, i, beat.Title));
                return true;
            }

            var element = ResolveElement(beat.Guid, model);
            if (element is ProblemModel subProblem)
            {
                path.Add((currentProblem, i, beat.Title));
                if (FindProblemInHierarchy(subProblem, targetGuid, model, path))
                    return true;
                path.RemoveAt(path.Count - 1);
            }
        }

        return false;
    }

    /// <summary>
    /// Append beat list with hierarchy visualization
    /// </summary>
    private void AppendBeatList(StringBuilder sb, ProblemModel problem, Guid targetGuid, StoryModel model, int depth)
    {
        if (problem.StructureBeats == null)
            return;

        var indent = new string(' ', depth * 2);

        foreach (var beat in problem.StructureBeats)
        {
            var elementName = "(unassigned)";
            var marker = "";
            ProblemModel? subProblem = null;

            if (beat.Guid != Guid.Empty)
            {
                var element = ResolveElement(beat.Guid, model);
                if (element != null)
                {
                    elementName = element.Name;
                    if (beat.Guid == targetGuid)
                    {
                        marker = " ← YOU ARE HERE";
                    }
                    subProblem = element as ProblemModel;
                }
            }

            sb.AppendLine($"{indent}- {beat.Title}: {elementName}{marker}");

            // Recursively show sub-problem's beats if it has them
            if (subProblem != null && subProblem.StructureBeats != null && subProblem.StructureBeats.Count > 0)
            {
                AppendBeatList(sb, subProblem, targetGuid, model, depth + 1);
            }
        }
    }

    #endregion

    #region Character Context

    private void AppendCharacterContext(StringBuilder sb, StoryElement targetElement, StoryModel model)
    {
        if (targetElement is not ProblemModel problem)
            return;

        sb.AppendLine("## Characters in this Problem");

        // Resolve protagonist
        if (problem.Protagonist != Guid.Empty)
        {
            var protagonist = ResolveElement(problem.Protagonist, model) as CharacterModel;
            if (protagonist != null)
            {
                sb.AppendLine($"Protagonist: {protagonist.Name}");
                if (!string.IsNullOrWhiteSpace(protagonist.Role))
                    sb.AppendLine($"  Role: {protagonist.Role}");
                if (!string.IsNullOrWhiteSpace(protagonist.Description))
                    sb.AppendLine($"  Description: {TruncateText(protagonist.Description, 100)}");
            }
        }

        // Resolve antagonist
        if (problem.Antagonist != Guid.Empty)
        {
            var antagonist = ResolveElement(problem.Antagonist, model) as CharacterModel;
            if (antagonist != null)
            {
                sb.AppendLine($"Antagonist: {antagonist.Name}");
                if (!string.IsNullOrWhiteSpace(antagonist.Role))
                    sb.AppendLine($"  Role: {antagonist.Role}");
                if (!string.IsNullOrWhiteSpace(antagonist.Description))
                    sb.AppendLine($"  Description: {TruncateText(antagonist.Description, 100)}");
            }
        }

        sb.AppendLine();
    }

    #endregion

    #region Preceding Events (from beat hierarchy)

    private void AppendPrecedingEvents(StringBuilder sb, ProblemModel targetProblem, StoryModel model, int maxBeats)
    {
        var overview = GetOverview(model);
        if (overview == null || overview.StoryProblem == Guid.Empty)
            return;

        var storyProblem = ResolveElement(overview.StoryProblem, model) as ProblemModel;
        if (storyProblem == null)
            return;

        // Collect all elements in order by traversing the beat hierarchy
        var orderedElements = new List<(Guid Guid, string Name, string Type)>();
        CollectElementsInOrder(storyProblem, model, orderedElements);

        // Find the target's position
        int targetIndex = orderedElements.FindIndex(e => e.Guid == targetProblem.Uuid);
        if (targetIndex <= 0)
        {
            // Not found or first element
            return;
        }

        sb.AppendLine("## Preceding Events");

        var preceding = orderedElements
            .Take(targetIndex)
            .TakeLast(maxBeats)
            .ToList();

        foreach (var item in preceding)
        {
            var element = ResolveElement(item.Guid, model);
            var description = GetElementDescription(element);
            sb.AppendLine($"- {item.Name}");
            if (!string.IsNullOrWhiteSpace(description))
                sb.AppendLine($"  {TruncateText(description, 80)}");
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Collect all assigned elements in narrative order by traversing beat sheets
    /// </summary>
    private void CollectElementsInOrder(ProblemModel problem, StoryModel model,
        List<(Guid Guid, string Name, string Type)> elements)
    {
        if (problem.StructureBeats == null)
            return;

        foreach (var beat in problem.StructureBeats)
        {
            if (beat.Guid == Guid.Empty)
                continue;

            var element = ResolveElement(beat.Guid, model);
            if (element == null)
                continue;

            elements.Add((beat.Guid, element.Name, element.ElementType.ToString()));

            // Recursively collect from sub-problems
            if (element is ProblemModel subProblem)
            {
                CollectElementsInOrder(subProblem, model, elements);
            }
        }
    }

    private string GetElementDescription(StoryElement? element)
    {
        return element switch
        {
            ProblemModel p => p.Description,
            SceneModel s => s.Description,
            _ => string.Empty
        };
    }

    #endregion

    #region Helper Methods

    private OverviewModel? GetOverview(StoryModel model)
    {
        var result = _api.GetElementsByType(StoryItemType.StoryOverview);
        return result.IsSuccess ? result.Payload?.FirstOrDefault() as OverviewModel : null;
    }

    private StoryElement? ResolveElement(Guid guid, StoryModel model)
    {
        if (guid == Guid.Empty)
            return null;

        var result = _api.GetStoryElement(guid);
        return result.IsSuccess ? result.Payload : null;
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - 3) + "...";
    }

    #endregion
}
