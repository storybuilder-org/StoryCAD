using StoryCADLib.ViewModels.SubViewModels;

namespace StoryCADLib.Controls;

/// <summary>
///     The data of a drag operation between the structure controls (issue #1546):
///     a Scene or a Problem, and the place it comes from.
///     The drag stays inside the app, so the controls read this object and not the data package.
/// </summary>
internal sealed class StructureDrag
{
    /// <summary>The drag operation that is in progress, or null.</summary>
    internal static StructureDrag Current { get; set; }

    /// <summary>The GUID of the Scene or Problem.</summary>
    internal Guid Element { get; init; }

    /// <summary>The beat column that the element comes from, or null for the element list.</summary>
    internal BeatSheetsViewModel Source { get; init; }

    /// <summary>The index of the beat that the element comes from, or -1.</summary>
    internal int BeatIndex { get; init; } = -1;

    /// <summary>The text that the drag operation carries as its data.</summary>
    internal string ToText() => $"storycad-structure|{Element}|{Source?.ProblemGuid ?? Guid.Empty}|{BeatIndex}";
}
