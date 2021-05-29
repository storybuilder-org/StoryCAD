using Windows.Data.Xml.Dom;

namespace StoryBuilder.Models
{
    public class FolderModel : StoryElement
    {
        #region Properties

        private string _notes;
        public string Notes
        {
            get => _notes;
            set => _notes = value;
        }
        #endregion

        #region Constructor
        public FolderModel() : base("New Folder", StoryItemType.Folder)
        {
            Notes = string.Empty;
        }
        public FolderModel(string name) : base(name, StoryItemType.Folder)
        {
            Notes = string.Empty;
        }
        public FolderModel(IXmlNode xn) : base(xn)
        {
            Notes = string.Empty;
        }

        #endregion
    }
}
