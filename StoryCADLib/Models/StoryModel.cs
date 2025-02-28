using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using StoryCAD.DAL;
using Windows.Storage;

namespace StoryCAD.Models;

public class StoryModel
{
	// TODO: Note sorting filtering and grouping depend on ICollectionView (for TreeView?)
	// TODO: See http://msdn.microsoft.com/en-us/library/ms752347.aspx#binding_to_collections
	// TODO: Maybe http://stackoverflow.com/questions/15593166/binding-treeview-with-a-observablecollection

	/// <summary>
	/// If any of the story entities have been modified by other than retrieval from the Story
	/// (that is, by a user modification from a View) 'Changed' is set to true. That is, a change
	/// to OverViewModel, or any ProblemModel, CharacterModel, SettingModel, SceneModel, or
	/// FolderModel, or adding a new node, will result in Changed being set to true. 
	/// 
	/// This amounts to a 'dirty' bit that indicates the StoryModel needs to be written to its backing store. 
	/// </summary>
	[JsonIgnore]
    public bool Changed;

	#region StoryProperties

	/// <summary>
	/// The first version of StoryCAD that this file was created with.
	/// </summary>
	[JsonInclude]
	[JsonPropertyName("CreatedVersion")]
	public string FirstVersion;

	/// <summary>
	/// The last version of StoryCAD that this file was saved with.
	/// </summary>
	[JsonInclude]
	[JsonPropertyName("LastVersion")]
	public string LastVersion;

	#endregion

	#region StoryExplorer and NarratorView (TreeView) properties

	/// <summary>
	/// This is a list of all the StoryNodeItems in the outline
	/// in a format that is easy to save to JSON.
	/// This is only updated on saves.
	/// </summary>
	[JsonInclude]
	internal List<PersistableNode> FlattenedExplorerView;

	/// <summary>
	/// This is a list of all the StoryNodeItems in the outline
	/// in a format that is easy to save to JSON.
	/// This is only updated on saves.
	/// </summary>
	[JsonInclude]
	internal List<PersistableNode> FlattenedNarratorView;

	/// A StoryModel is a collection of StoryElements (an overview, problems, characters, settings,
	/// and scenes, plus containers).
	[JsonInclude]
	[JsonPropertyName("Elements")]
	public StoryElementCollection StoryElements;

	/// StoryModel also contains two persisted TreeView representations, a Story ExplorerView tree which
	/// contains all Story Elements (the StoryOverview and all Problem, Character, Setting, Scene
	/// and Folder elements) and a NarratorView View which contains just Section (chapter, act, etc)
	/// and selected Scene elements. 
	/// 
	/// One of these TreeViews is actively bound in the Shell page view to a StoryNodeItem tree 
	/// based on  whichever of these two TreeView representations is presently selected.
	[JsonIgnore]
	public ObservableCollection<StoryNodeItem> ExplorerView;
	[JsonIgnore]
	public ObservableCollection<StoryNodeItem> NarratorView;

    #endregion


    /// <summary>
    /// Used to prepare tree for serialisation
    /// </summary>
    /// <param name="rootNodes"></param>
    /// <returns></returns>
    private static List<PersistableNode> FlattenTree(ObservableCollection<StoryNodeItem> rootNodes)
    {
        var list = new List<PersistableNode>();
        foreach (var root in rootNodes)
        {
            AddNodeRecursively(root, list);
        }
        return list;
    }

    /// <summary>
    /// used within Flatten Tree to handle serialisation effectively.
    /// </summary>
    private static void AddNodeRecursively(StoryNodeItem node, List<PersistableNode> list)
    {
        list.Add(new PersistableNode
        {
            Uuid = node.Uuid,
            ParentUuid = node.Parent?.Uuid
        });

        foreach (var child in node.Children)
        {
            AddNodeRecursively(child, list);
        }
    }

    /// <summary>
    /// Serialises the model to JSON
    /// </summary>
    /// <param name="model">Story Model to serialise</param>
    /// <returns></returns>
    public string Serialize()
    {
        //Flatten trees (solves issues when deserialization)
        FlattenedExplorerView = FlattenTree(ExplorerView);
        FlattenedNarratorView = FlattenTree(NarratorView);

        //Serialise
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new EmptyGuidConverter(),
                new StoryElementConverter(),
                new JsonStringEnumConverter()
            }
        });
    }

    #region Constructor
    public StoryModel()
    {
        StoryElements = new StoryElementCollection();
        ExplorerView = new ObservableCollection<StoryNodeItem>();
        NarratorView = new ObservableCollection<StoryNodeItem>();

        Changed = false;
    }
    #endregion
}