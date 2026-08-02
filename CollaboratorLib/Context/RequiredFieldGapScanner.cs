using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;

namespace CollaboratorLib.Context;

/// <summary>
/// Outline-wide required-field gaps (issue #107). One GUID per element missing any required property.
/// </summary>
public static class RequiredFieldGapScanner
{
    /// <summary>
    /// Returns distinct element GUIDs that fail at least one required-field check for their type.
    /// Scans Overview, Problem, Character, Setting, and Scene only.
    /// </summary>
    public static IReadOnlyList<Guid> FindGapGuids(IStoryCADAPI api, StoryModel model)
    {
        if (api == null) throw new ArgumentNullException(nameof(api));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var gaps = new List<Guid>();
        foreach (var element in model.StoryElements)
        {
            if (element == null) continue;
            if (HasGap(api, element))
                gaps.Add(element.Uuid);
        }

        return gaps;
    }

    public static bool HasGap(IStoryCADAPI api, StoryElement element)
    {
        if (IsBlank(element.Name) || IsBlank(element.Description))
            return true;

        return element switch
        {
            OverviewModel overview => OverviewHasGap(api, overview),
            ProblemModel problem => ProblemHasGap(api, problem),
            CharacterModel character => CharacterHasGap(character),
            SettingModel => false, // Name + Description only
            SceneModel scene => SceneHasGap(api, scene),
            _ => false
        };
    }

    private static bool OverviewHasGap(IStoryCADAPI api, OverviewModel overview)
    {
        return IsBlank(overview.Author)
            || IsBlank(overview.Concept)
            || IsBlank(overview.Premise)
            || IsBlank(overview.StoryType)
            || IsBlank(overview.StoryGenre)
            || !IsResolvable(api, overview.StoryProblem, StoryItemType.Problem);
    }

    private static bool ProblemHasGap(IStoryCADAPI api, ProblemModel problem)
    {
        return IsBlank(problem.ProblemCategory)
            || IsBlank(problem.Premise)
            || IsBlank(problem.ProtGoal)
            || IsBlank(problem.ProtMotive)
            || IsBlank(problem.ProtConflict)
            || IsBlank(problem.AntagGoal)
            || IsBlank(problem.AntagMotive)
            || IsBlank(problem.AntagConflict)
            || IsBlank(problem.Outcome)
            || !IsResolvable(api, problem.Protagonist, StoryItemType.Character)
            || !IsResolvable(api, problem.Antagonist, StoryItemType.Character);
    }

    private static bool CharacterHasGap(CharacterModel character)
    {
        return IsBlank(character.Role) || IsBlank(character.StoryRole);
    }

    private static bool SceneHasGap(IStoryCADAPI api, SceneModel scene)
    {
        if (!IsResolvable(api, scene.Setting, StoryItemType.Setting))
            return true;

        if (scene.CastMembers == null || scene.CastMembers.Count == 0)
            return true;

        foreach (var member in scene.CastMembers)
        {
            if (!IsResolvable(api, member, StoryItemType.Character))
                return true;
        }

        return false;
    }

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
