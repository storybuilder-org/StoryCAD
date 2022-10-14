namespace StoryBuilder.Services.Messages;

public class NameChangeMessage
{
    public string OldName { get; }
    public string NewName { get; }

    public NameChangeMessage(string oldName, string newName)
    {
        OldName = oldName;
        NewName = newName;
    }
}