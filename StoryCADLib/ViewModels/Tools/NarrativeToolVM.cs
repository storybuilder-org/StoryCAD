using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Dialogs.Tools;
using StoryCAD.Services.Locking;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.ViewModels.Tools;

/// <summary>
/// This is the ViewModel for the Narrative tool.
/// It contains data used via the NarrativeTool ContentDialog.
///
/// NarrativeTool is used to construct the Narrator view, which is
/// the order a story is really told in rather than explorer.
/// Explorer view is the main view and is how the planning.
///
/// To open Narrative Tool you can press CRTL + N or access it
/// through the Shell via the Tools Menu (Spanner Icon)
/// </summary>
public class NarrativeToolVM: ObservableRecipient
{
    private readonly ShellViewModel _shellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
    private readonly OutlineViewModel outlineVM = Ioc.Default.GetRequiredService<OutlineViewModel>();
    private readonly LogService _logger = Ioc.Default.GetRequiredService<LogService>(); 
    public StoryNodeItem SelectedNode; //Currently selected node
    public bool IsNarratorSelected = false;

    #region Relay Commands
    public RelayCommand CopyCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand CopyAllUnusedCommand { get; }
    public RelayCommand CreateFlyout { get; }

    #endregion


    private string _newSectionName;
    /// <summary>
    /// Name of a new section
    /// Bound to the new section name in the new section flyout area.
    /// </summary>
    public string NewSectionName
    {
        get => _newSectionName;
        set => SetProperty(ref _newSectionName, value);
    }

    private string _message;
    /// <summary>
    /// Message show to the user on the UI
    /// </summary>
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public NarrativeToolVM()
    {
        CreateFlyout = new RelayCommand(MakeSection);
        CopyCommand = new RelayCommand(Copy);
        CopyAllUnusedCommand = new RelayCommand(CopyAllUnused);
        DeleteCommand = new RelayCommand(Delete);
    }

    /// <summary>
    /// Retrieve the single root node for the narrator view.
    /// Returns null if the structure is invalid.
    /// </summary>
    private StoryNodeItem? GetNarratorRoot()
    {
        var roots = outlineVM.StoryModel.NarratorView
            .Where(n => n.IsRoot)
            .ToList();

        if (roots.Count != 1)
        {
            _logger.Log(LogLevel.Warn, $"NarratorView root invalid: {roots.Count}");
            Message = "Narrative view has an invalid structure.";
            return null;
        }

        return roots[0];
    }

    /// <summary>
    /// This opens the Narrative Tool Content Dialog.
    /// </summary>
    public async Task OpenNarrativeTool()
    {
        if (_shellVM.OutlineManager.VerifyToolUse(false, false))
        {
            var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
            var backupService = Ioc.Default.GetRequiredService<BackupService>();

            using (var serializationLock = new SerializationLock(autoSaveService, backupService, _logger))
            {
                try
                {
                    ContentDialog _dialog = new()
                    {
                        Title = "Narrative Editor",
                        PrimaryButtonText = "Done",
                        Content = new NarrativeTool()
                    };
                    await Ioc.Default.GetService<Windowing>().ShowContentDialog(_dialog);
                }
                catch (Exception ex)
                {
                    _logger.LogException(LogLevel.Error, ex, "Error in OpenNarrativeTool()");
                }

            }
        }
    }


    /// <summary>
    /// Deletes a node from the tree.
    ///
    /// TODO: Possibly Merge with StoryElement.Delete() Method
    /// </summary>
    public void Delete()
    {
        try
        {
            if (SelectedNode != null)
            {
                if (SelectedNode.Type == StoryItemType.TrashCan || SelectedNode.IsRoot) { Message = "You can't delete this node!"; }

                if (IsNarratorSelected)
                {
                    SelectedNode.Delete(StoryViewType.NarratorView);
                    Message = $"Deleted {SelectedNode}";
                }
                else { Message = "You can't delete from here!"; }
            }
            else { _logger.Log(LogLevel.Warn, "Selected node was null, doing nothing"); }
        }
        catch (Exception _ex) { _logger.LogException(LogLevel.Error, _ex, "Error in NarrativeToolVM.Delete()"); }
    }
    

    /// <summary>
    /// Copies all scenes, if the node has children then it will copy all children that are scenes
    ///
    /// TODO: Possibly move to StoryElement and make bi-directional
    /// </summary>
    private void Copy()
    {
        try
        {
            _logger.Log(LogLevel.Info, "Starting to copy node between trees.");

            //Check if selection is null
            if (SelectedNode == null)
            {
                _logger.Log(LogLevel.Warn, "No node selected");
                return;
            }

            _logger.Log(LogLevel.Info, $"Node Selected is a {SelectedNode.Type}");
            StoryNodeItem? root = GetNarratorRoot();
            if (root == null)
                return;

            if (SelectedNode.Type == StoryItemType.Scene)  //If its just a scene, add it immediately if not already in.
            {
                if (RecursiveCheck(root.Children).All(storyNodeItem => storyNodeItem.Uuid != SelectedNode.Uuid)) //checks node isn't in the narrator view
                {
                    _ = new StoryNodeItem((SceneModel)outlineVM.StoryModel.StoryElements.StoryElementGuids[SelectedNode.Uuid], root);
                    _logger.Log(LogLevel.Info, $"Copied SelectedNode {SelectedNode.Name} ({SelectedNode.Uuid})");
                    Message = $"Copied {SelectedNode.Name}";
                }
                else
                {
                    _logger.Log(LogLevel.Warn, $"Node {SelectedNode.Name} ({SelectedNode.Uuid}) already exists in the NarratorView");
                    Message = "This scene already appears in the narrative view.";
                }
            }
            else if (SelectedNode.Type is StoryItemType.Folder or StoryItemType.Section) //If its a folder then recurse and add all unused scenes to the narrative view.
            {
                _logger.Log(LogLevel.Info, "Item is a folder/section, getting flattened list of all children.");
                foreach (StoryNodeItem _item in RecursiveCheck(SelectedNode.Children))
                {
                    if (_item.Type == StoryItemType.Scene && RecursiveCheck(root.Children).All(storyNodeItem => storyNodeItem.Uuid != _item.Uuid))
                    {
                        _ = new StoryNodeItem((SceneModel)outlineVM.StoryModel.StoryElements.StoryElementGuids[_item.Uuid], root);
                        _logger.Log(LogLevel.Info, $"Copied item {SelectedNode.Name} ({SelectedNode.Uuid})");
                    }
                }

                Message = $"Copied {SelectedNode.Children} and child scenes.";
            }
            else
            {
                _logger.Log(LogLevel.Warn, $"Node {SelectedNode.Name} ({SelectedNode.Uuid}) wasn't copied, it was a {SelectedNode.Type}");
                Message = "You can't copy that."; 
            }
        }
        catch (Exception _ex) { _logger.LogException(LogLevel.Error, _ex, "Error in NarrativeTool.Copy()"); }
        _logger.Log(LogLevel.Info, "NarrativeTool.Copy() complete.");

    }

    private List<StoryNodeItem> RecursiveCheck(ObservableCollection<StoryNodeItem> list)
    {
        _logger.Log(LogLevel.Info, "New instance of Recursive check starting.");
        List<StoryNodeItem> _newList = new();
        try
        {
            foreach (StoryNodeItem _variable in list)
            {
                _newList.Add(_variable);
                _newList.AddRange(RecursiveCheck(_variable.Children));
            }
        }
        catch (Exception _exception) { _logger.LogException(LogLevel.Error, _exception, "Error in recursive check"); }
        
        return _newList;
    }

    /// <summary>
    /// This copies all unused scenes.
    /// </summary>
    private void CopyAllUnused()
    {
        //Recursively goes through the children of NarratorView View.
        StoryNodeItem? root = GetNarratorRoot();
        if (root == null)
            return;

        try { foreach (StoryNodeItem _item in outlineVM.StoryModel.ExplorerView[0].Children) { RecurseCopyUnused(_item, root); } }
        catch (Exception _e) { _logger.LogException(LogLevel.Error, _e, "Error in recursive check"); }
    }

    /// <summary>
    /// Creates new section, with the name of NewSectionName
    /// in the NarratorView tree
    /// </summary>
    private void MakeSection()
    {
        if (_shellVM.DataSource == null || _shellVM.DataSource.Count < 0)
        {
            _logger.Log(LogLevel.Warn, "DataSource is empty or null, not adding section");
            return;
        }
        StoryNodeItem? root = GetNarratorRoot();
        if (root == null)
            return;

        _ = new FolderModel(NewSectionName, outlineVM.StoryModel, StoryItemType.Folder, root);
    }

    /// <summary>
    /// This recursively copies any unused scene in the ExplorerView view.
    /// </summary>
    /// <param name="item">The parent item </param>
    private void RecurseCopyUnused(StoryNodeItem item, StoryNodeItem root)
    {
        _logger.Log(LogLevel.Trace, $"Recursing through {item.Name} ({item.Uuid})");
        try
        {
            if (item.Type == StoryItemType.Scene) //Check if scene/folder/section, if not then just continue.
            {
                //This calls recursive check, which returns flattens the entire the tree and .Any() checks if the UUID is in anywhere in the model.
                if (RecursiveCheck(root.Children).All(storyNodeItem => storyNodeItem.Uuid != item.Uuid))
                {
                    //Since the node isn't in the node, then we add it here.
                    _logger.Log(LogLevel.Trace, $"{item.Name} ({item.Uuid}) not found in Narrative view, adding it to the tree");
                    _ = new StoryNodeItem((SceneModel)outlineVM.StoryModel.StoryElements.StoryElementGuids[item.Uuid], root);
                }
            }

            foreach (StoryNodeItem _child in item.Children) { RecurseCopyUnused(_child, root); }
        }
        catch (Exception _ex)
        {
            _logger.LogException(LogLevel.Error, _ex, "Error in NarrativeTool.CopyAllUnused()");
            Message = "Error copying nodes.";
        }
    }
}
