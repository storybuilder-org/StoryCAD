namespace StoryCADLib.Models.Tools;

public class SubTopicModel
{
    public string SubTopicName;
    public string SubTopicNotes;

    public SubTopicModel(string name)
    {
        SubTopicName = name;
        SubTopicNotes = string.Empty;
    }
}
