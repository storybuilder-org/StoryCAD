namespace StoryCADLib.Services.Messages;

public class NameChangeMessage
{
    public NameChangeMessage(string oldName, string newName)
    {
        OldName = oldName;
        NewName = newName;
    }

    public string OldName { get; private set; }

    public string NewName { get; private set; }
}
