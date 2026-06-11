namespace StoryCADLib.Models;

/// <summary>
///     The templates available when creating a new outline.
///
///     The underlying integer values are deliberately pinned to the historical
///     template indexes (0-5). Several things depend on this:
///     <list type="bullet">
///         <item>The "New" dialog's <c>ComboBox.SelectedIndex</c> maps directly to a value.</item>
///         <item><c>Preferences.json</c> persists the last template as the numeric "LastTemplate".</item>
///         <item>The external API still accepts a legacy numeric string ("0".."5").</item>
///     </list>
///     Do not renumber these members.
/// </summary>
public enum OutlineTemplate
{
    /// <summary>An empty outline - just the Overview, Narrative and Trash nodes.</summary>
    BlankOutline = 0,

    /// <summary>A single story problem with a protagonist and antagonist.</summary>
    OverviewAndStoryProblem = 1,

    /// <summary>Empty Problems, Characters, Settings and Scenes folders.</summary>
    Folders = 2,

    /// <summary>Separate external and internal problems with a protagonist and antagonist.</summary>
    ExternalAndInternalProblems = 3,

    /// <summary>A protagonist and antagonist tied to a story problem.</summary>
    ProtagonistAndAntagonist = 4,

    /// <summary>Folders pre-populated with external/internal problems and characters.</summary>
    ProblemsAndCharacters = 5,
}
