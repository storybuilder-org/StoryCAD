using StoryCADLib.Models;

namespace CollaboratorLib.Context;

/// <summary>
/// Maps required-field property names to Collaborator workflow labels (issue #107 phase 6).
/// Empty workflow list means host-element edit only.
/// </summary>
public static class GapWorkflowOwnership
{
    public const string OutlineGapsNavTitle = "Outline gaps";
    public const string OutlineGapsTag = "OutlineGaps";

    /// <summary>
    /// Returns helper workflow labels for a missing property on an element type.
    /// </summary>
    public static IReadOnlyList<string> WorkflowsFor(StoryItemType elementType, string propertyName)
    {
        return (elementType, propertyName) switch
        {
            (StoryItemType.StoryOverview, "StoryType") => new[] { "StoryForm" },
            (StoryItemType.StoryOverview, "StoryGenre") => new[] { "StoryForm" },
            (StoryItemType.StoryOverview, "Concept") => new[] { "Premise" },
            (StoryItemType.StoryOverview, "Premise") => new[] { "Premise" },
            (StoryItemType.StoryOverview, "Description") => new[] { "Premise" },
            (StoryItemType.StoryOverview, "StoryProblem") => new[] { "StoryProblem" },
            (StoryItemType.StoryOverview, "Author") => Array.Empty<string>(),

            (StoryItemType.Problem, "ProtGoal") => new[] { "ProblemBuilder" },
            (StoryItemType.Problem, "ProtMotive") => new[] { "ProblemBuilder" },
            (StoryItemType.Problem, "ProtConflict") => new[] { "ProblemBuilder" },
            (StoryItemType.Problem, "AntagGoal") => new[] { "ProblemBuilder" },
            (StoryItemType.Problem, "AntagMotive") => new[] { "ProblemBuilder" },
            (StoryItemType.Problem, "AntagConflict") => new[] { "ProblemBuilder" },
            (StoryItemType.Problem, "Outcome") => new[] { "ProblemBuilder" },
            (StoryItemType.Problem, "Protagonist") => new[] { "StoryProblem" },
            (StoryItemType.Problem, "Antagonist") => new[] { "StoryProblem" },
            (StoryItemType.Problem, "ProblemCategory") => new[] { "StoryProblem" },
            (StoryItemType.Problem, "ProblemType") => new[] { "ProblemBuilder", "StoryProblem" },
            (StoryItemType.Problem, "ConflictType") => new[] { "ProblemBuilder", "StoryProblem" },
            (StoryItemType.Problem, "Subject") => new[] { "ProblemBuilder", "StoryProblem" },
            (StoryItemType.Problem, "Premise") => new[] { "ProblemBuilder", "StoryProblem" },
            (StoryItemType.Problem, "Description") => new[] { "ProblemBuilder", "StoryProblem" },

            // #244 CharacterBuilder owns the sheet and the sketch.
            (StoryItemType.Character, "Role") => new[] { "CharacterBuilder" },
            (StoryItemType.Character, "Age") => new[] { "CharacterBuilder" },
            (StoryItemType.Character, "Sex") => new[] { "CharacterBuilder" },
            (StoryItemType.Character, "Appearance") => new[] { "CharacterBuilder" },
            (StoryItemType.Character, "StoryRole") => new[] { "CharacterBuilder" },
            (StoryItemType.Character, "Description") => new[] { "CharacterBuilder" },
            (StoryItemType.Character, "BackStory") => new[] { "FlawBackstory" },
            (StoryItemType.Character, "Name") => Array.Empty<string>(),

            // #224: SettingBuilder absorbed SettingTimeSpace and Sensations.
            (StoryItemType.Setting, "Description") => new[] { "SettingBuilder" },
            (StoryItemType.Setting, "Name") => Array.Empty<string>(),
            // #261: place fields and the Story World (#198).
            (StoryItemType.Setting, "Locale") => new[] { "SettingBuilder" },
            (StoryItemType.Setting, "Period") => new[] { "SettingBuilder" },
            (StoryItemType.StoryWorld, "WorldType") => new[] { "DefineStoryWorld" },
            (StoryItemType.StoryWorld, "Description") => new[] { "DefineStoryWorld" },

            (StoryItemType.Scene, "Description") => new[] { "SceneBuilder" },
            (StoryItemType.Scene, "CastMembers") => new[] { "SceneBuilder" },
            (StoryItemType.Scene, "Setting") => new[] { "SceneBuilder" },
            (StoryItemType.Scene, "Name") => Array.Empty<string>(),

            (_, "Name") => Array.Empty<string>(),
            (_, "Description") => Array.Empty<string>(),
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Collaborator #261: the type label on an Outline gaps group. The enum name, except the
    /// Story World, which the page shows with a space.
    /// </summary>
    public static string GroupLabel(StoryItemType elementType) =>
        elementType == StoryItemType.StoryWorld ? "Story World" : elementType.ToString();

    public static string DisplayLabel(StoryItemType elementType, string propertyName)
    {
        return (elementType, propertyName) switch
        {
            (StoryItemType.StoryOverview, "Description") => "Story Idea",
            (StoryItemType.Problem, "Description") => "Story Question",
            (StoryItemType.Character, "Description") => "Character Sketch",
            (StoryItemType.Setting, "Description") => "Setting Summary",
            (StoryItemType.Scene, "Description") => "Scene Sketch",
            (StoryItemType.StoryOverview, "StoryType") => "Type",
            (StoryItemType.StoryOverview, "StoryGenre") => "Genre",
            (StoryItemType.StoryOverview, "StoryProblem") => "Story Problem",
            (StoryItemType.Character, "StoryRole") => "Story Role",
            (StoryItemType.Character, "BackStory") => "Backstory",
            (StoryItemType.Problem, "ProblemCategory") => "Problem Category",
            (StoryItemType.Problem, "ProblemType") => "Problem Type",
            (StoryItemType.Problem, "ConflictType") => "Conflict Type",
            (StoryItemType.Problem, "Subject") => "Subject",
            (StoryItemType.Scene, "CastMembers") => "Cast",
            (StoryItemType.StoryWorld, "WorldType") => "World Type",
            (StoryItemType.StoryWorld, "Description") => "World tell",
            _ => propertyName
        };
    }
}
