using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;

namespace CollaboratorLib.Context;

/// <summary>
/// Outline-wide required-field gaps (issue #107). GUIDs for StoryContext; details for gap workflow.
/// </summary>
public static class RequiredFieldGapScanner
{
    /// <summary>
    /// Returns distinct element GUIDs that fail at least one required-field check for their type.
    /// </summary>
    public static IReadOnlyList<Guid> FindGapGuids(IStoryCADAPI api, StoryModel model)
    {
        return FindGapDetails(api, model).Select(g => g.ElementGuid).ToList();
    }

    /// <summary>
    /// Full gap report for the Outline gaps workflow (phase 6).
    /// </summary>
    public static IReadOnlyList<GapDetail> FindGapDetails(IStoryCADAPI api, StoryModel model)
    {
        if (api == null) throw new ArgumentNullException(nameof(api));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var details = new List<GapDetail>();
        foreach (var element in model.StoryElements)
        {
            if (element == null) continue;
            if (element is not (OverviewModel or ProblemModel or CharacterModel or SettingModel or SceneModel))
                continue;

            var missing = GetMissingProperties(api, element);
            if (missing.Count == 0)
                continue;

            var labels = missing
                .Select(p => GapWorkflowOwnership.DisplayLabel(element.ElementType, p))
                .ToList();

            var helpers = missing
                .SelectMany(p => GapWorkflowOwnership.WorkflowsFor(element.ElementType, p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            details.Add(new GapDetail
            {
                ElementGuid = element.Uuid,
                ElementName = string.IsNullOrWhiteSpace(element.Name) ? "(unnamed)" : element.Name.Trim(),
                ElementType = element.ElementType,
                MissingProperties = missing,
                MissingPropertyLabels = labels,
                HelperWorkflowLabels = helpers
            });
        }

        // Spine order: Overview, Problem, Character, Setting, Scene
        return details
            .OrderBy(d => TypeOrder(d.ElementType))
            .ThenBy(d => d.ElementName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool HasGap(IStoryCADAPI api, StoryElement element)
    {
        return GetMissingProperties(api, element).Count > 0;
    }

    public static IReadOnlyList<string> GetMissingProperties(IStoryCADAPI api, StoryElement element)
    {
        if (element is not (OverviewModel or ProblemModel or CharacterModel or SettingModel or SceneModel))
            return Array.Empty<string>();

        var missing = new List<string>();
        if (IsBlank(element.Name))
            missing.Add("Name");
        if (IsBlank(element.Description))
            missing.Add("Description");

        switch (element)
        {
            case OverviewModel overview:
                if (IsBlank(overview.Author)) missing.Add("Author");
                if (IsBlank(overview.Concept)) missing.Add("Concept");
                if (IsBlank(overview.Premise)) missing.Add("Premise");
                if (IsBlank(overview.StoryType)) missing.Add("StoryType");
                if (IsBlank(overview.StoryGenre)) missing.Add("StoryGenre");
                if (!IsResolvable(api, overview.StoryProblem, StoryItemType.Problem))
                    missing.Add("StoryProblem");
                break;

            case ProblemModel problem:
                if (IsBlank(problem.ProblemCategory)) missing.Add("ProblemCategory");
                if (IsBlank(problem.Premise)) missing.Add("Premise");
                if (IsBlank(problem.ProtGoal)) missing.Add("ProtGoal");
                if (IsBlank(problem.ProtMotive)) missing.Add("ProtMotive");
                if (IsBlank(problem.ProtConflict)) missing.Add("ProtConflict");
                if (IsBlank(problem.AntagGoal)) missing.Add("AntagGoal");
                if (IsBlank(problem.AntagMotive)) missing.Add("AntagMotive");
                if (IsBlank(problem.AntagConflict)) missing.Add("AntagConflict");
                if (IsBlank(problem.Outcome)) missing.Add("Outcome");
                if (!IsResolvable(api, problem.Protagonist, StoryItemType.Character))
                    missing.Add("Protagonist");
                if (!IsResolvable(api, problem.Antagonist, StoryItemType.Character))
                    missing.Add("Antagonist");
                break;

            case CharacterModel character:
                if (IsBlank(character.Role)) missing.Add("Role");
                if (IsBlank(character.StoryRole)) missing.Add("StoryRole");
                break;

            case SettingModel:
                break;

            case SceneModel scene:
                if (!IsResolvable(api, scene.Setting, StoryItemType.Setting))
                    missing.Add("Setting");
                if (scene.CastMembers == null || scene.CastMembers.Count == 0)
                    missing.Add("CastMembers");
                else
                {
                    foreach (var member in scene.CastMembers)
                    {
                        if (!IsResolvable(api, member, StoryItemType.Character))
                        {
                            missing.Add("CastMembers");
                            break;
                        }
                    }
                }
                break;
        }

        return missing;
    }

    private static int TypeOrder(StoryItemType type) => type switch
    {
        StoryItemType.StoryOverview => 0,
        StoryItemType.Problem => 1,
        StoryItemType.Character => 2,
        StoryItemType.Setting => 3,
        StoryItemType.Scene => 4,
        _ => 9
    };

    private static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);

    private static bool IsResolvable(IStoryCADAPI api, Guid guid, StoryItemType expectedType)
    {
        if (guid == Guid.Empty)
            return false;

        var result = api.GetStoryElement(guid);
        if (!result.IsSuccess || result.Payload == null)
            return false;

        return result.Payload.ElementType == expectedType;
    }
}
