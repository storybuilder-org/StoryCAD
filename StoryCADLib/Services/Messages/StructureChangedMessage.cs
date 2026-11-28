namespace StoryCADLib.Services.Messages;

/// <summary>
///     Sent after a change to the structure of one Problem: its beats, its structure elements,
///     its beat sheet name or its beat sheet description (issue #1546).
///     Each screen that shows that Problem reads the model again.
/// </summary>
public class StructureChangedMessage
{
    public StructureChangedMessage(Guid problemGuid, object source)
    {
        ProblemGuid = problemGuid;
        Source = source;
    }

    /// <summary>The Problem that changed.</summary>
    public Guid ProblemGuid { get; }

    /// <summary>The object that sent the message. A receiver ignores its own messages.</summary>
    public object Source { get; }
}
