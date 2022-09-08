using Windows.Data.Xml.Dom;

namespace StoryBuilder.Models;

/// <summary>
/// The TrashCanModel is a container for deleted StoryElements. It's the second root node
/// in both the Explorer and Narrator Views, and contains no properties.
/// </summary>
public class TrashCanModel : StoryElement
{
    #region Constructors
    public TrashCanModel(StoryModel model) : base("Deleted Story Elements", StoryItemType.TrashCan, model) { }
    public TrashCanModel(IXmlNode xn, StoryModel model) : base(xn, model) { }

    #endregion
}