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

            (StoryItemType.Problem, "ProtGoal") => new[] { "GMC" },
            (StoryItemType.Problem, "ProtMotive") => new[] { "GMC" },
            (StoryItemType.Problem, "ProtConflict") => new[] { "GMC" },
            (StoryItemType.Problem, "AntagGoal") => new[] { "GMC" },
            (StoryItemType.Problem, "AntagMotive") => new[] { "GMC" },
            (StoryItemType.Problem, "AntagConflict") => new[] { "GMC" },
            (StoryItemType.Problem, "Outcome") => new[] { "GMC" },
            (StoryItemType.Problem, "Protagonist") => new[] { "StoryProblem", "GMC" },
            (StoryItemType.Problem, "Antagonist") => new[] { "StoryProblem", "GMC" },
            (StoryItemType.Problem, "ProblemCategory") => new[] { "StoryProblem" },
            (StoryItemType.Problem, "ProblemType") => new[] { "StoryProblem" },
            (StoryItemType.Problem, "ConflictType") => new[] { "StoryProblem" },
            (StoryItemType.Problem, "Subject") => new[] { "StoryProblem" },
            (StoryItemType.Problem, "Premise") => new[] { "StoryProblem" },
            (StoryItemType.Problem, "Description") => new[] { "StoryProblem" },

            // #182 occupation Role; #183 Story Function + Character Sketch; #107 essentials
            (StoryItemType.Character, "Role") => new[] { "DefineCharacter" },
            (StoryItemType.Character, "Age") => new[] { "DefineCharacter" },
            (StoryItemType.Character, "Sex") => new[] { "DefineCharacter" },
            (StoryItemType.Character, "Appearance") => new[] { "DefineCharacter" },
            (StoryItemType.Character, "StoryRole") => new[] { "StoryFunction" },
            (StoryItemType.Character, "Description") => new[] { "StoryFunction" },
            (StoryItemType.Character, "BackStory") => new[] { "FlawBackstory" },
            (StoryItemType.Character, "Name") => Array.Empty<string>(),

            (StoryItemType.Setting, "Description") => new[] { "SettingTimeSpace" },
            (StoryItemType.Setting, "Name") => Array.Empty<string>(),

            (StoryItemType.Scene, "Description") => new[] { "SceneSummary" },
            (StoryItemType.Scene, "CastMembers") => new[] { "CastSceneRoles" },
            (StoryItemType.Scene, "Setting") => new[] { "SceneSummary", "CastSceneRoles" },
            (StoryItemType.Scene, "Name") => Array.Empty<string>(),

            (_, "Name") => Array.Empty<string>(),
            (_, "Description") => Array.Empty<string>(),
            _ => Array.Empty<string>()
        };
    }

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
            _ => propertyName
        };
    }
}
