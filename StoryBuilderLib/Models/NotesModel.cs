using Windows.Data.Xml.Dom;

namespace StoryBuilder.Models;

public class NotesModel : StoryElement
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
    public NotesModel(StoryModel model) : base("New note", StoryItemType.Notes, model) { Notes = string.Empty; }
    public NotesModel(string name, StoryModel model) : base(name, StoryItemType.Notes, model) { Notes = string.Empty; }
    public NotesModel(IXmlNode xn, StoryModel model) : base(xn, model) { Notes = string.Empty; }

    #endregion
}