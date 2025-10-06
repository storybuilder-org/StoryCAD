namespace StoryCADLib.Models.Tools;

public class ConflictCategoryModel
{
    public SortedDictionary<string, List<string>> Examples;
    public List<string> SubCategories;
    public string TopicName;

    public ConflictCategoryModel(string topic)
    {
        TopicName = topic;
        SubCategories = new List<string>();
        Examples = new SortedDictionary<string, List<string>>();
    }
}
