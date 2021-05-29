using Windows.Data.Xml.Dom;

namespace StoryBuilder.Models
{
    /// <summary>
    /// The TrashCanModel is a container for deleted StoryElements. It's the second root node
    /// in both the Explorer and Narrator Views, and contains no properties.
    /// </summary>
    public class TrashCanModel : StoryElement
    {
        #region Constructors
        public TrashCanModel() : base("Deleted Story Elements", StoryItemType.TrashCan)
        {
        }

        public TrashCanModel(IXmlNode xn) : base(xn)
        {
        }

        #endregion
    }
}
