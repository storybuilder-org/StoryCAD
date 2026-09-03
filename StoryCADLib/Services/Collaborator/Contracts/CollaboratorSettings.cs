namespace StoryCADLib.Services.Collaborator.Contracts;

/// <summary>
/// Settings that control Collaborator behavior.
/// Passed from StoryCAD to Collaborator at initialization.
/// </summary>
public sealed class CollaboratorSettings
{
    /// <summary>
    /// Controls AI response length. Sent to the Worker as the <c>Terseness</c> workflow arg;
    /// the coach system message states the tier for every workflow (Collaborator #49).
    /// Persists across sessions via <c>PreferencesModel.CollaboratorTerseness</c>.
    /// </summary>
    public TersenessLevel Terseness { get; set; } = TersenessLevel.Balanced;

    /// <summary>
    /// User's preferred genres (comma-separated).
    /// Used to guide AI suggestions toward preferred styles.
    /// </summary>
    public string GenrePreferences { get; set; } = string.Empty;

    /// <summary>
    /// Story forms the user likes (comma-separated).
    /// Example: "Novel, Short Story, Screenplay"
    /// </summary>
    public string StoryFormLikes { get; set; } = string.Empty;

    /// <summary>
    /// Story forms the user wants to avoid (comma-separated).
    /// Example: "Poetry, Flash Fiction"
    /// </summary>
    public string StoryFormDislikes { get; set; } = string.Empty;

    /// <summary>
    /// Controls visibility of Collaborator logs.
    /// </summary>
    public LoggingVisibility LoggingLevel { get; set; } = LoggingVisibility.Off;

    /// <summary>
    /// Shows the per-run cost line on the shell's status bar. Off by default: accounting is
    /// noise while drafting, and the figure only matters to someone watching credit spend.
    ///
    /// Persists across sessions like <see cref="Terseness"/>: seeded from
    /// <c>PreferencesModel.ShowCollaboratorCost</c> when Collaborator opens and written back
    /// when the settings dialog changes it. The other members reset to their defaults every open.
    /// </summary>
    public bool ShowCostDetails { get; set; }

    /// <summary>
    /// Creates default settings.
    /// </summary>
    public static CollaboratorSettings Default => new();
}

/// <summary>
/// Controls AI response verbosity.
/// </summary>
public enum TersenessLevel
{
    /// <summary>
    /// Brief, to-the-point responses. Minimal explanation.
    /// </summary>
    Concise,

    /// <summary>
    /// Moderate detail with some explanation. Default.
    /// </summary>
    Balanced,

    /// <summary>
    /// Detailed responses with full explanations and examples.
    /// </summary>
    Detailed
}

/// <summary>
/// Controls visibility of Collaborator internal logs.
/// </summary>
public enum LoggingVisibility
{
    /// <summary>
    /// No user-visible logging. Default for privacy.
    /// </summary>
    Off,

    /// <summary>
    /// Status messages only. Safe for users.
    /// </summary>
    Basic,

    /// <summary>
    /// Full logs including prompts. Developer mode.
    /// Warning: May expose IP (prompt templates, API calls).
    /// </summary>
    Detailed
}
