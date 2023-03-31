using Windows.Data.Xml.Dom;
using StoryCAD.ViewModels;

namespace StoryCAD.Models;

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
    public FolderModel(StoryModel model) : base("New Folder", StoryItemType.Folder, model)
    {
        Notes = string.Empty;
    }
    public FolderModel(string name, StoryModel model, StoryItemType type) : base(name, type, model)
    {
        Notes = string.Empty;
    }
    public FolderModel(IXmlNode xn, StoryModel model) : base(xn, model)
    {
        Notes = string.Empty;
    }

    #endregion
}