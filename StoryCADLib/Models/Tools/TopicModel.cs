namespace StoryCADLib.Models.Tools;

public enum TopicTypeEnum
{
    Notepad,
    Inline
}

public class TopicModel
{
    public string Filename;
    public List<SubTopicModel> SubTopics;
    public string TopicName;
    public TopicTypeEnum TopicType;

    public TopicModel(string topic, string filename)
    {
        TopicName = topic;
        TopicType = TopicTypeEnum.Notepad;
        Filename = filename;
        SubTopics = new List<SubTopicModel>();
    }

    public TopicModel(string topic)
    {
        TopicName = topic;
        TopicType = TopicTypeEnum.Inline;
        Filename = string.Empty;
        SubTopics = new List<SubTopicModel>();
    }
}
