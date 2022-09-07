using System.Collections.Generic;
using Windows.Data.Xml.Dom;

namespace StoryBuilder.Models.Scrivener;

/// <summary>
/// A simple data transfer object (DTO) that contains raw data about a BinderItem   
/// </summary>
public class BinderItem
{
    #region Properties

    public BinderItem Parent { get; set; }

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    public List<BinderItem> Children { get; private set; }

    public int Id { get; set; }

    public string Uuid { get; set; }

    public string Created { get; set; }

    public string Modified { get; set; }
    /// <summary>
    /// The displayed node name (Header property)
    /// </summary>
    public string Title { get; set; }

    public BinderItemType Type { get; set; }

    public string StbUuid { get; set; }

    public IXmlNode Node { get; set; }

    public override string ToString() { return Title; }

    #endregion

    #region Constructors

    public BinderItem(int id, string uuid, BinderItemType type, string header)
        : this(id, uuid, type, header, null)
    {
        Node = null;
        Children = new List<BinderItem>();
    }

    public BinderItem(int id, string uuid, BinderItemType type, string header, BinderItem parent)
    {
        Id = id;
        Uuid = uuid;
        Created = string.Empty;
        Modified = string.Empty;
        Type = type;
        Title = header;
        parent?.Children.Add(this);
        Parent = parent;
        Children = new List<BinderItem>();
    }

    public BinderItem(int id, string uuid, BinderItemType type, string header, BinderItem parent, string created, string modified, string stbUuid)
    {
        Id = id;
        Uuid = uuid;
        Created = created;
        Modified = modified;
        Type = type;
        Title = header;
        parent?.Children.Add(this);
        Parent = parent;
        Children = new List<BinderItem>();
        StbUuid = stbUuid;
    }

    #endregion // Constructors

    #region Methods

    /// <summary>
    /// This method allows a dept-first search (DFS) or 'pre-order traversal' of
    /// a of a BinderItem tree or subtree with a simple C# foreach.
    /// 
    /// In a DFS, you visit the root first and then search deeper into the tree
    /// visiting each node and then the node’s children.
    ///
    /// To use the enumerator, you code a foreach loop anywhere in your program:
    /// 
    /// foreach (TreeNode node in root)
    /// {
    ///     //perform action on node in DFS order here
    /// }
    ///
    ///  ref: http://www.timlabonne.com/2013/07/performing-a-dfs-over-a-rooted-tree-with-a-c-foreach-loop/
    ///  </summary>
    /// <returns>BinderItem for current node, then for each child in a loop</returns>
    public IEnumerator<BinderItem> GetEnumerator()
    {
        yield return this;
        foreach (BinderItem _child in Children)
        {
            // ReSharper disable once GenericEnumeratorNotDisposed
            IEnumerator<BinderItem> _childEnumerator = _child.GetEnumerator();
            while (_childEnumerator.MoveNext())
                yield return _childEnumerator.Current;
        }
    }

    #endregion
}