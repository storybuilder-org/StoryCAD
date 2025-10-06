namespace StoryCAD.Models;

public class FolderModel : StoryElement
{
    #region Constructor

    public FolderModel(StoryModel model, StoryNodeItem node) : base("New Folder", StoryItemType.Folder, model, node)
    {
        Description = string.Empty;
    }

    public FolderModel(string name, StoryModel model, StoryItemType type, StoryNodeItem Node) : base(name, type, model,
        Node)
    {
        Description = string.Empty;
    }

    /// <summary>
    ///     Json constructor
    /// </summary>
    public FolderModel()
    {
    }

    #endregion
}
