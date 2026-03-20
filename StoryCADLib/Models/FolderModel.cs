namespace StoryCADLib.Models;

[Microsoft.UI.Xaml.Data.Bindable]
public class FolderModel : StoryElement
{
    #region Constructor

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
