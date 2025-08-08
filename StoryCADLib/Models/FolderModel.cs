using System.Text.Json.Serialization;

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
    public FolderModel(StoryModel model, StoryNodeItem node) : base("New Folder", StoryItemType.Folder, model, node)
    {
        Notes = string.Empty;
    }
    public FolderModel(string name, StoryModel model, StoryItemType type, StoryNodeItem Node) : base(name, type, model, Node)
    {
        Notes = string.Empty;
    }

	/// <summary>
	/// Json constructor
	/// </summary>
    public FolderModel()
    {

    }
    #endregion
}