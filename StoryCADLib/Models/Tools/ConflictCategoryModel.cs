namespace StoryCAD.Models.Tools;

public class ConflictCategoryModel
{
    public string TopicName;
    public List<string> SubCategories;
    public SortedDictionary<string, List<string>> Examples;
    public ConflictCategoryModel(string topic)
    {
        TopicName = topic;
        SubCategories = new List<string>();
        Examples = new SortedDictionary<string, List<string>>();
    }
}