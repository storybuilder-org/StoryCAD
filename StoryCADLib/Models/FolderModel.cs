using System.Text.Json.Serialization;
using Windows.Data.Xml.Dom;

namespace StoryCAD.Models;

public class FolderModel : StoryElement
{
	#region Properties
	[JsonIgnore]
	private string _notes;
	[JsonInclude]
	[JsonPropertyName("Notes")]
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