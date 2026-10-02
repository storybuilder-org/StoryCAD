using System.Text.Json.Serialization;

namespace StoryCADLib.Models;

[Microsoft.UI.Xaml.Data.Bindable]
public class FolderModel : StoryElement
{
    #region Properties

    // Pictures attached to a Notes element. FolderModel also backs Folder and
    // Section nodes; those carry an empty list (no Images tab is shown for them).
    [JsonIgnore] private List<StoryImage> _images;

    [JsonInclude]
    [JsonPropertyName("Images")]
    public List<StoryImage> Images
    {
        get => _images;
        set => _images = value;
    }

    #endregion

    #region Constructor

    public FolderModel(string name, StoryModel model, StoryItemType type, StoryNodeItem Node) : base(name, type, model,
        Node)
    {
        Description = string.Empty;
        Images = new List<StoryImage>();
    }

    /// <summary>
    ///     Json constructor
    /// </summary>
    public FolderModel()
    {
        Images = new List<StoryImage>();
    }

    #endregion
}
