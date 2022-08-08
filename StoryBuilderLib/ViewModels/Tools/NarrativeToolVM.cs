using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.Services.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using LogLevel = StoryBuilder.Services.Logging.LogLevel;

namespace StoryBuilder.ViewModels.Tools;
public class NarrativeToolVM
{
    public StoryNodeItem LastSelectedNode;
    public ShellViewModel ShellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
    public LogService Logger = Ioc.Default.GetRequiredService<LogService>();
    public RelayCommand CopyCommand { get; } 
    public RelayCommand DeleteCommand { get; } 
    public RelayCommand CopyAllUnusedCommand { get; } 
    public RelayCommand MoveUpCommand { get; }
    public RelayCommand MoveDownCommand { get; }
    public  string Message { get; set; }
    private int _sourceIndex;
    private ObservableCollection<StoryNodeItem> _sourceChildren;
    private int _targetIndex;
    private ObservableCollection<StoryNodeItem> _targetCollection;

    public NarrativeToolVM()
    {
        CopyCommand = new RelayCommand(Copy);
        CopyAllUnusedCommand = new RelayCommand(CopyAllUnused);
        MoveUpCommand = new RelayCommand(MoveUp);
        MoveDownCommand = new RelayCommand(MoveDown);
        DeleteCommand = new RelayCommand(Delete);
    }

    public void Delete()
    {
        Logger.Log(LogLevel.Trace, "Deleting element");
        try
        {
            if (LastSelectedNode == null)
            {
                Logger.Log(LogLevel.Error, "L");
                return;
            }

            if (LastSelectedNode.Type == StoryItemType.TrashCan)
            {
                return;
            }

            if (LastSelectedNode.IsRoot)
            {
                return;
            }

            //Even though there should only be one copy, just delete any just in case.
            if (ShellVM.StoryModel.NarratorView[0].Children.Contains(LastSelectedNode))
            {
                ShellVM.StoryModel.NarratorView[0].Children.Remove(LastSelectedNode);
                ShellVM.StoryModel.NarratorView[1].Children.Add(LastSelectedNode);
            }
            foreach (StoryNodeItem child in ShellVM.StoryModel.NarratorView[0].Children) { recurseDelete(LastSelectedNode, child); }
        }
        catch
        {
            Logger.Log(LogLevel.Error, "Error Deleting node in narrative tree");
        }

    }

    /// <summary>
    /// Recursively deletes from NarratorView.
    /// </summary>
    private void recurseDelete(StoryNodeItem item, StoryNodeItem Parent)
    {
        if (Parent.Children.Contains(item)) //Checks parent contains child we are looking.
        {
            Parent.Children.Remove(item); //Deletes child.
            ShellVM.StoryModel.NarratorView[1].Children.Add(LastSelectedNode);
        }
        else //If child isn't in parent, recurse again.
        {
            foreach (StoryNodeItem child in Parent.Children) { recurseDelete(item, child); }
        }
    }   

    private void MoveUp()
    {
        if (LastSelectedNode == null)
        {
            //Messenger.Send(new StatusChangedMessage(new($"Click or touch a node to move", LogLevel.Info)));
            return;
        }

        if (LastSelectedNode.IsRoot)
        {
            //Messenger.Send(new StatusChangedMessage(new($"Cannot move up further", LogLevel.Warn)));
            return;
        }
        _sourceChildren = LastSelectedNode.Parent.Children;
        _sourceIndex = _sourceChildren.IndexOf(LastSelectedNode);
        _targetCollection = null;
        _targetIndex = -1;
        StoryNodeItem _targetParent = LastSelectedNode.Parent;

        // If first child, must move to end parent's predecessor
        if (_sourceIndex == 0)
        {
            if (LastSelectedNode.Parent.Parent == null)
            {
                //Messenger.Send(new StatusChangedMessage(new($"Cannot move up further", LogLevel.Warn)));
                return;
            }
            // find parent's predecessor
            ObservableCollection<StoryNodeItem> grandparentCollection = LastSelectedNode.Parent.Parent.Children;
            int siblingIndex = grandparentCollection.IndexOf(LastSelectedNode.Parent) - 1;
            if (siblingIndex >= 0)
            {
                _targetCollection = grandparentCollection[siblingIndex].Children;
                _targetParent = grandparentCollection[siblingIndex];
            }
            else
            {
                //Messenger.Send(new StatusChangedMessage(new($"Cannot move up further", LogLevel.Warn)));
                return;
            }
        }
        // Otherwise, move up a notch
        else
        {
            _targetCollection = _sourceChildren;
            _targetIndex = _sourceIndex - 1;
        }

        //TODO: port ShellVM.VerifyMove() when it isn't stubbed
        _sourceChildren.RemoveAt(_sourceIndex);
        if (_targetIndex == -1)
            _targetCollection.Add(LastSelectedNode);
        else
            _targetCollection.Insert(_targetIndex, LastSelectedNode);
        LastSelectedNode.Parent = _targetParent;
        Logger.Log(LogLevel.Info, $"Moving {LastSelectedNode.Name} up to parent {LastSelectedNode.Parent.Name}");
    }

    public void MoveDown()
    {
        if (LastSelectedNode == null)
        {
            //Messenger.Send(new StatusChangedMessage(new($"Click or touch a node to move", LogLevel.Info)));
            return;
        }
        if (LastSelectedNode.IsRoot)
        {
            //Messenger.Send(new StatusChangedMessage(new($"Cannot move a root node", LogLevel.Info)));
            return;
        }

        _sourceChildren = LastSelectedNode.Parent.Children;
        _sourceIndex = _sourceChildren.IndexOf(LastSelectedNode);
        _targetCollection = null;
        _targetIndex = 0;
        StoryNodeItem _targetParent = LastSelectedNode.Parent;

        // If last child, must move to end parent's successor
        if (_sourceIndex == _sourceChildren.Count - 1)
        {
            if (LastSelectedNode.Parent.Parent == null)
            {
                //Messenger.Send(new StatusChangedMessage(new($"Cannot move down further", LogLevel.Warn)));
                return;
            }
            // find parent's successor
            ObservableCollection<StoryNodeItem> grandparentCollection = LastSelectedNode.Parent.Parent.Children;
            int siblingIndex = grandparentCollection.IndexOf(LastSelectedNode.Parent) + 1;
            if (siblingIndex == grandparentCollection.Count)
            {
                LastSelectedNode.Parent = ShellVM.StoryModel.NarratorView[1];
                _sourceChildren.RemoveAt(_sourceIndex);
                ShellVM.StoryModel.NarratorView[1].Children.Insert(_targetIndex, LastSelectedNode);
                //Messenger.Send(new StatusChangedMessage(new($"Moved to trash", LogLevel.Info)));

                return;
            }
            if (grandparentCollection[siblingIndex].IsRoot)
            {
                //Messenger.Send(new StatusChangedMessage(new($"Cannot move down further", LogLevel.Warn)));
                return;
            }
            _targetCollection = grandparentCollection[siblingIndex].Children;
            _targetParent = grandparentCollection[siblingIndex];
        }
        // Otherwise, move down a notch
        else
        {
            _targetCollection = _sourceChildren;
            _targetIndex = _sourceIndex + 1;
        }
        _sourceChildren.RemoveAt(_sourceIndex);
        _targetCollection.Insert(_targetIndex, LastSelectedNode);
        LastSelectedNode.Parent = _targetParent;
        Logger.Log(LogLevel.Info, $"Moving {LastSelectedNode.Name} down up to parent {LastSelectedNode.Parent.Name}");
    }

    /// <summary>
    /// Copies all scenes, if the node has children then it will copy all children that are scenes
    /// </summary>
    private void Copy()
    {
        try
        {
            //Check if selection is null
            if (LastSelectedNode == null)
            {
                Logger.Log(LogLevel.Warn, "No node selected");
                return;
            }

            Logger.Log(LogLevel.Info, $"Node Selected is a {LastSelectedNode.Type}");
            if (LastSelectedNode.Type == StoryItemType.Scene)  //check the node is either a scene OR has children
            {
                if (!RecursiveCheck(ShellVM.StoryModel.NarratorView[0].Children).Any(StoryNodeItem => StoryNodeItem.Uuid == LastSelectedNode.Uuid))
                {
                    _ = new StoryNodeItem((SceneModel)ShellVM.StoryModel.StoryElements.StoryElementGuids[LastSelectedNode.Uuid], ShellVM.StoryModel.NarratorView[0]);
                    Logger.Log(LogLevel.Info, $"Copied LastSelectedNode {LastSelectedNode.Name} ({LastSelectedNode.Uuid})");

                }
                else
                {
                    Logger.Log(LogLevel.Warn, $"Node {LastSelectedNode.Name} ({LastSelectedNode.Uuid}) already exists in the NarratorView");
                    Message = "This scene already appears in the narrative view.";
                }
            }
            else
            {
                Logger.Log(LogLevel.Warn, $"Node {LastSelectedNode.Name} ({LastSelectedNode.Uuid}) wasn't copied, it was a {LastSelectedNode.Type}");
                Message = "You can't copy that.";
            }
        }
        catch (Exception ex) { Logger.LogException(LogLevel.Error, ex, "Error in NarrativeTool.Copy()"); }
        Logger.Log(LogLevel.Info, "NarrativeTool.Copy() complete.");

    }


    private List<StoryNodeItem> RecursiveCheck(ObservableCollection<StoryNodeItem> List)
    {
        List<StoryNodeItem> NewList = new();
        foreach (var VARIABLE in List)
        {
            NewList.Add(VARIABLE);
            NewList.AddRange(RecursiveCheck(VARIABLE.Children));
        }
        return NewList;
    }

    /// <summary>
    /// This copies all unused scenes.
    /// </summary>
    private void CopyAllUnused()
    {
        foreach (var VARIABLE in ShellVM.StoryModel.ExplorerView[0].Children) { RecurseCopyUnused(VARIABLE); }
    }

    /// <summary>
    /// This recursively copies any unused scene in the Explorer view.
    /// </summary>
    /// <param name="Item"></param>
    private void RecurseCopyUnused(StoryNodeItem Item)
    {
        try
        {
            if (Item.Type == StoryItemType.Scene) //Check if scene/folder/section, if not then just continue.
            {
                //This calls recursive check, which returns flattens the entire the tree and .Any() checks if the UUID is in anywhere in the model.
                if (!RecursiveCheck(ShellVM.StoryModel.NarratorView[0].Children).Any(StoryNodeItem => StoryNodeItem.Uuid == Item.Uuid)) 
                {
                    //Since the node isn't in the node, then we add it here.
                    _ = new StoryNodeItem((SceneModel)ShellVM.StoryModel.StoryElements.StoryElementGuids[Item.Uuid], ShellVM.StoryModel.NarratorView[0]);
                }
            }

            foreach (var Child in Item.Children) { RecurseCopyUnused(Child); }  
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error, ex, "Error in NarrativeTool.CopyAllUnused()");
            Message = "Error copying nodes.";
        }
    }
}
