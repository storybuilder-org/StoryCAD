using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.ViewModels;

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
    /// Collaborator #237 item 10: the number the Outline gaps nav row shows. It is the number
    /// of missing fields across every element, which is what the Outline gaps page lists.
    /// </summary>
    public static int MissingFieldCount(IReadOnlyList<GapDetail> details) =>
        details?.Sum(d => d.MissingProperties.Count) ?? 0;

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
            if (element is not (OverviewModel or ProblemModel or CharacterModel or SettingModel or StoryWorldModel or SceneModel))
                continue;
            // Collaborator #261: elements in the trash have no gaps (decision of 2026-11-12).
            if (IsInTrash(element))
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

    /// <summary>
    /// Collaborator #261: true when the element's node chain ends at the TrashCan root, the same
    /// test as WorkflowInputBuilder.IsInTrash. An element with no node, or whose root cannot be
    /// found (RootNodeType gives Unknown), is not in the trash.
    /// </summary>
    internal static bool IsInTrash(StoryElement element) =>
        element?.Node != null && StoryNodeItem.RootNodeType(element.Node) == StoryItemType.TrashCan;

    /// <summary>
    /// Collaborator #261: true when an available Scene sits on a beat of <paramref name="problem"/>,
    /// or on a beat of an available Problem in its beat tree. Each Problem is read once, so a loop
    /// of Problems on beats ends.
    /// </summary>
    internal static bool HasSceneOnBeats(ProblemModel problem, StoryModel model) =>
        HasSceneOnBeats(problem, model, new HashSet<Guid>());

    private static bool HasSceneOnBeats(ProblemModel problem, StoryModel model, HashSet<Guid> read)
    {
        if (problem?.StructureBeats == null || !read.Add(problem.Uuid))
            return false;

        foreach (var beat in problem.StructureBeats)
        {
            if (beat == null || beat.Guid == Guid.Empty)
                continue;
            if (!model.StoryElements.StoryElementGuids.TryGetValue(beat.Guid, out var element) || IsInTrash(element))
                continue;
            if (element is SceneModel)
                return true;
            if (element is ProblemModel subProblem && HasSceneOnBeats(subProblem, model, read))
                return true;
        }

        return false;
    }

    private static readonly string[] StoryRolesNotListed = { "Supporting Role", "Minor Role", "Service Role" };

    /// <summary>
    /// Collaborator #261: the missing pieces of the outline. A Problem with no Scene in its beat
    /// tree (link: Problem Beats), and a Character that no Problem seats as Protagonist or
    /// Antagonist (link: Problem Builder), unless its Story Role is Supporting, Minor or Service.
    /// Elements in the trash are not read. Problems first, then Characters, each by name.
    /// </summary>
    public static IReadOnlyList<MissingPiece> FindMissingPieces(StoryModel model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        var problems = model.StoryElements.OfType<ProblemModel>().Where(p => !IsInTrash(p)).ToList();
        var characters = model.StoryElements.OfType<CharacterModel>().Where(c => !IsInTrash(c)).ToList();
        var seated = new HashSet<Guid>(problems.SelectMany(p => new[] { p.Protagonist, p.Antagonist }));

        var pieces = new List<MissingPiece>();
        foreach (var problem in problems.Where(p => !HasSceneOnBeats(p, model)))
        {
            pieces.Add(new MissingPiece
            {
                ElementGuid = problem.Uuid,
                ElementName = DisplayName(problem),
                ElementType = StoryItemType.Problem,
                Text = "No Scene is on the beats of this Problem.",
                WorkflowLabel = "ProblemBeats"
            });
        }

        foreach (var character in characters)
        {
            if (seated.Contains(character.Uuid))
                continue;
            var role = (character.StoryRole ?? string.Empty).Trim();
            if (StoryRolesNotListed.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase)))
                continue;

            pieces.Add(new MissingPiece
            {
                ElementGuid = character.Uuid,
                ElementName = DisplayName(character),
                ElementType = StoryItemType.Character,
                Text = "This Character is not the Protagonist or the Antagonist of a Problem.",
                WorkflowLabel = "ProblemBuilder"
            });
        }

        return pieces
            .OrderBy(p => p.ElementType == StoryItemType.Problem ? 0 : 1)
            .ThenBy(p => p.ElementName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Collaborator #261: the number in the nav row, field gaps plus missing pieces.</summary>
    public static int GapCount(IReadOnlyList<GapDetail> details, IReadOnlyList<MissingPiece> pieces) =>
        MissingFieldCount(details) + (pieces?.Count ?? 0);

    private static string DisplayName(StoryElement element) =>
        string.IsNullOrWhiteSpace(element.Name) ? "(unnamed)" : element.Name.Trim();

    public static bool HasGap(IStoryCADAPI api, StoryElement element)
    {
        return GetMissingProperties(api, element).Count > 0;
    }

    public static IReadOnlyList<string> GetMissingProperties(IStoryCADAPI api, StoryElement element)
    {
        if (element is not (OverviewModel or ProblemModel or CharacterModel or SettingModel or StoryWorldModel or SceneModel))
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
                // Collaborator #160: ConflictType, ProblemType, Subject are spine-required
                // (multi-workflow problem craft). Flaw/BackStory stay Character input-only.
                if (IsBlank(problem.ProblemCategory)) missing.Add("ProblemCategory");
                if (IsBlank(problem.ProblemType)) missing.Add("ProblemType");
                if (IsBlank(problem.ConflictType)) missing.Add("ConflictType");
                if (IsBlank(problem.Subject)) missing.Add("Subject");
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
                if (IsBlank(character.Age)) missing.Add("Age");
                if (IsBlank(character.Sex)) missing.Add("Sex");
                if (IsBlank(character.Appearance)) missing.Add("Appearance");
                if (IsBlank(character.BackStory)) missing.Add("BackStory");
                break;

            case SettingModel setting:
                // Collaborator #261: place fields (#198).
                if (IsBlank(setting.Locale)) missing.Add("Locale");
                if (IsBlank(setting.Period)) missing.Add("Period");
                break;

            case StoryWorldModel world:
                if (IsBlank(world.WorldType)) missing.Add("WorldType");
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
        StoryItemType.StoryWorld => 4,
        StoryItemType.Scene => 5,
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

        // Collaborator #261: a link to an element in the trash does not fill the field.
        return result.Payload.ElementType == expectedType && !IsInTrash(result.Payload);
    }
}
