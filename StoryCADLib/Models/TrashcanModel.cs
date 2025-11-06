namespace StoryCADLib.Models;

/// <summary>
///     The TrashCanModel is a container for deleted StoryElements. It's the second root node
///     in both the Explorer View and Narrator Views, and contains no properties.
/// </summary>
public class TrashCanModel : StoryElement
{
    #region Constructors

    /// <summary>
    ///     JSON Constructor
    /// </summary>
    public TrashCanModel()
    {
    }

    public TrashCanModel(StoryModel model, StoryNodeItem node)
        : base("Deleted Story Elements", StoryItemType.TrashCan, model, node)
    {
    }

    #endregion
}
