namespace StoryCADLib.Models;

/// <summary>
///     How the structure of the outline uses a Scene or a Problem (issue #1546).
/// </summary>
public enum StructureUse
{
    /// <summary>No Problem has the element as a structure element.</summary>
    NotUsed,

    /// <summary>One Problem or more has the element on a beat.</summary>
    OnBeat,

    /// <summary>A Problem has the element as a structure element, and no Problem has it on a beat.</summary>
    OnNoBeat
}
