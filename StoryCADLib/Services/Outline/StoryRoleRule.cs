using StoryCADLib.Models;
using StoryCADLib.ViewModels;

namespace StoryCADLib.Services.Outline;

/// <summary>
/// Collaborator #261: a Character seated as a Problem's Protagonist or Antagonist gets a
/// Story Role that matches the seat. A seat on the Story Problem gives Protagonist or
/// Antagonist; a seat on any other Problem gives Major Role. The rule fills an empty or lesser
/// role only. It never replaces Protagonist, Antagonist, or a value outside the Story Role list,
/// and it never lowers a role when a Character leaves a seat.
/// </summary>
public static class StoryRoleRule
{
    private const string Protagonist = "Protagonist";
    private const string Antagonist = "Antagonist";
    private const string MajorRole = "Major Role";
    private static readonly string[] LesserRoles = { "Supporting Role", "Minor Role", "Service Role" };

    /// <summary>
    /// Sets the Story Role of the Characters in the two seats of <paramref name="problem"/>.
    /// The Protagonist is examined first.
    /// </summary>
    public static void ApplySeats(StoryModel model, ProblemModel problem)
    {
        if (model == null || problem == null || IsInTrash(problem))
            return;

        var isStoryProblem = StoryProblemOf(model) == problem.Uuid;
        Apply(model, problem.Protagonist, isStoryProblem ? Protagonist : MajorRole, isStoryProblem);
        Apply(model, problem.Antagonist, isStoryProblem ? Antagonist : MajorRole, isStoryProblem);
    }

    private static void Apply(StoryModel model, Guid seat, string newRole, bool isStoryProblem)
    {
        if (seat == Guid.Empty
            || !model.StoryElements.StoryElementGuids.TryGetValue(seat, out var element)
            || element is not CharacterModel character
            || IsInTrash(character))
        {
            return;
        }

        var current = (character.StoryRole ?? string.Empty).Trim();
        var replace = current.Length == 0
                      || LesserRoles.Any(r => string.Equals(r, current, StringComparison.OrdinalIgnoreCase))
                      || (isStoryProblem && string.Equals(current, MajorRole, StringComparison.OrdinalIgnoreCase));
        if (replace)
            character.StoryRole = newRole;
    }

    /// <summary>
    /// The Story Problem from the first Overview, or Guid.Empty when it names no available Problem.
    /// </summary>
    private static Guid StoryProblemOf(StoryModel model)
    {
        var overview = model.StoryElements.OfType<OverviewModel>().FirstOrDefault();
        if (overview == null || overview.StoryProblem == Guid.Empty)
            return Guid.Empty;

        return model.StoryElements.StoryElementGuids.TryGetValue(overview.StoryProblem, out var element)
               && element is ProblemModel
               && !IsInTrash(element)
            ? overview.StoryProblem
            : Guid.Empty;
    }

    private static bool IsInTrash(StoryElement element) =>
        element.Node != null && StoryNodeItem.RootNodeType(element.Node) == StoryItemType.TrashCan;
}
