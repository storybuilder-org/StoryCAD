using System;
using System.Collections.Generic;

namespace StoryCADLib.Services.Collaborator.Contracts;

/// <summary>
/// Represents the result of a Collaborator session returned to StoryCAD when the plugin closes.
/// </summary>
public sealed class CollaboratorResult
{
    /// <summary>
    /// Indicates whether Collaborator completed successfully.
    /// </summary>
    public bool Completed { get; init; }

    /// <summary>
    /// Human-readable summary of what Collaborator did.
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// Optional detail messages (warnings, follow-up notes, etc.).
    /// </summary>
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();
}
