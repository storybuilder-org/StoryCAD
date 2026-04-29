namespace StoryCADLib.Models;

/// <summary>
///     The TrashCanModel is a container for deleted StoryElements. It is the root node of
///     <see cref="StoryModel.TrashView"/>, a separate top-level collection parallel to
///     ExplorerView and NarratorView. (Older outline files used a dual-root structure where
///     TrashCan was a second root inside ExplorerView; <see cref="DAL.StoryIO"/> migrates
///     those automatically on load.) Has no properties of its own.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class TrashCanModel : StoryElement
{
    #region Constructors

    /// <summary>
    ///     JSON Constructor
    /// </summary>
    public TrashCanModel() { }

    public TrashCanModel(StoryModel model, StoryNodeItem node)
        : base("Deleted Story Elements", StoryItemType.TrashCan, model, node)
    {
    }

    #endregion
}
