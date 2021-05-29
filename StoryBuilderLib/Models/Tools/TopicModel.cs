using System.Collections.Generic;

namespace StoryBuilder.Models.Tools
{
    public enum TopicTypeEnum {Notepad, Inline}
    public class TopicModel
    {
        public TopicTypeEnum TopicType;
        public string Filename;
        public string TopicName;
        public List<SubTopicModel> SubTopics;

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
}
