namespace StoryBuilder.Services.Messages;

public class NameChangeMessage
{
    public string OldName
    {
        get; private set;
    }
    public string NewName
    {
        get; private set;
    }

    public NameChangeMessage(string oldName, string newName)
    {
        OldName = oldName;
        NewName = newName;
    }
}