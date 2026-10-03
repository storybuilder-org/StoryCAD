namespace StoryCollaborator.Models;

/// <summary>
/// How much of each element is sent for a declared collection input (issue #106).
/// </summary>
public enum ElementProjection
{
    /// <summary>GUID + Name only (today's CharacterChoices shape).</summary>
    IdAndName = 0,

    /// <summary>GUID, Name, ElementDescription, Type only.</summary>
    BaseStoryElement = 1,

    /// <summary>Full runtime model (same as gathered elements).</summary>
    FullModel = 2
}
