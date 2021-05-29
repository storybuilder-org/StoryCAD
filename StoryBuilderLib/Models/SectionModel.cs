using Windows.Data.Xml.Dom;

namespace StoryBuilder.Models
{
    /// <summary>
    /// A SectionModel is a cousin of FolderModel, but, rather than acting as a general container for
    /// StoryElements, is a container for PlotPoints only in the Narrative View.
    /// </summary>
    public class SectionModel : StoryElement
    {
        #region Properties

        private string _notes;
        public string Notes
        {
            get => _notes;
            set => _notes = value;
        }
        #endregion

        #region Constructors
        public SectionModel() : base("New Section", StoryItemType.Section)
        {
            Notes = string.Empty;
        }
        public SectionModel(string name) : base(name, StoryItemType.Section)
        {
            Notes = string.Empty;
        }
        public SectionModel(IXmlNode xn) : base(xn)
        {
            Notes = string.Empty;
        }

        #endregion
    }
}
