namespace StoryCADLib.Services.Collaborator.Contracts;

/// <summary>
/// Settings that control Collaborator behavior.
/// Passed from StoryCAD to Collaborator at initialization.
/// </summary>
public sealed class CollaboratorSettings
{
    /// <summary>
    /// Controls AI response verbosity.
    /// </summary>
    public TersenessLevel Terseness { get; set; } = TersenessLevel.Balanced;

    /// <summary>
    /// Controls how much AI can modify user's existing content.
    /// </summary>
    public ContentPreservationLevel ContentPreservation { get; set; } = ContentPreservationLevel.Balanced;

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
    /// Unlike the other members, this one persists across sessions — it is seeded from
    /// <c>PreferencesModel.ShowCollaboratorCost</c> when Collaborator opens and written back
    /// when the settings dialog changes it. The rest reset to their defaults every open.
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
/// Controls how much AI can modify user's existing content.
/// </summary>
public enum ContentPreservationLevel
{
    /// <summary>
    /// Preserve user's exact wording. Only fill gaps, don't rewrite.
    /// </summary>
    Strict,

    /// <summary>
    /// Light editing allowed. Preserve intent but improve clarity.
    /// </summary>
    Balanced,

    /// <summary>
    /// AI can freely rewrite and enhance content.
    /// </summary>
    Flexible
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
