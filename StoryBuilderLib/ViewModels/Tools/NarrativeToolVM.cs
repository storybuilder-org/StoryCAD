using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StoryBuilder.ViewModels.Tools;
public class NarrativeToolVM
{
    public StoryNodeItem LastSelectedNode;
    public ShellViewModel ShellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
    public LogService Logger = Ioc.Default.GetRequiredService<LogService>();
    public RelayCommand CopyCommand { get; } 
    public RelayCommand CopyAllUnusedCommand { get; } 
    public RelayCommand MoveUpCommand { get; }

    public string Message
    {
        get => _message;
        set => _message = value;
    }
    private string _message = "";


    public NarrativeToolVM()
    {
        CopyCommand = new RelayCommand(Copy);
        CopyAllUnusedCommand = new RelayCommand(CopyAllUnused);
        MoveUpCommand = new RelayCommand(MoveUp);
    }

    private void MoveUp()
    {/*
        if (LastSelectedNode == null)
        {
            ShellVM.ShowMessage(LogLevel.Info, "Click or touch a node to move", false);
            return;
        }

        if (LastSelectedNode.IsRoot)
        {
            ShellVM.ShowMessage(LogLevel.Warn,"Cannot move up further",false);
            return;
        }
        _sourceChildren = CurrentNode.Parent.Children;
        _sourceIndex = _sourceChildren.IndexOf(CurrentNode);
        _targetCollection = null;
        _targetIndex = -1;
        StoryNodeItem _targetParent = CurrentNode.Parent;

        // If first child, must move to end parent's predecessor
        if (_sourceIndex == 0)
        {
            if (LastSelectedNode.Parent.Parent == null)
            {
                ShellVM.ShowMessage(LogLevel.Info, "Cannot move up further", false);
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
                Messenger.Send(new StatusChangedMessage(new($"Cannot move up further", LogLevel.Warn)));
                return;
            }
        }
        // Otherwise, move up a notch
        else
        {
            _targetCollection = _sourceChildren;
            _targetIndex = _sourceIndex - 1;
        }

        if (MoveIsValid()) // Verify move
        {
            _sourceChildren.RemoveAt(_sourceIndex);
            if (_targetIndex == -1) { _targetCollection.Add(LastSelectedNode); }
            else { _targetCollection.Insert(_targetIndex, LastSelectedNode); }
            LastSelectedNode.Parent = _targetParent;
            Logger.Log(LogLevel.Info, $"Moving {LastSelectedNode.Name} up to parent {LastSelectedNode.Parent.Name}");
        }*/
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

            Logger.Log(LogLevel.Info, $"Node Selected is a {LastSelectedNode.Type} and has {LastSelectedNode.Children.Count} children");
            if (LastSelectedNode.Type == StoryItemType.Scene || ((LastSelectedNode.Type == StoryItemType.Folder || LastSelectedNode.Type == StoryItemType.Section) && LastSelectedNode.Children.Count > 0))  //check the node is either a scene OR has children
            {
                if (LastSelectedNode.Children.Count != 0) //has Children
                {
                    Logger.Log(LogLevel.Info, $"Iterating through children");
                    foreach (var child in LastSelectedNode.Children) //Iterates through children and checks they are a Scene
                    {
                        if (child.Type == StoryItemType.Scene || child.Type == StoryItemType.Folder || child.Type == StoryItemType.Section) //Does nothing if not a scene.
                        {
                            Logger.Log(LogLevel.Info, $"Found Child {child.Name}");
                            _ = new StoryNodeItem((SceneModel)ShellVM.StoryModel.StoryElements.StoryElementGuids[child.Uuid], ShellVM.StoryModel.NarratorView[0]);
                        }
                    }
                }
                Logger.Log(LogLevel.Info, $"Copied LastSelectedNode {LastSelectedNode.Name} ({LastSelectedNode.Uuid})");
                _ = new StoryNodeItem((SceneModel)ShellVM.StoryModel.StoryElements.StoryElementGuids[LastSelectedNode.Uuid], ShellVM.StoryModel.NarratorView[0]);
                Logger.Log(LogLevel.Info, $"NarrativeTool.Copy() complete.");
            }
            else
            {
                Message = "You can't copy that.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error, ex, "Error in NarrativeTool.Copy()");
        }
    }

    /// <summary>
    /// This copies all unused scenes.
    /// </summary>
    private void CopyAllUnused()
    {
        foreach (var VARIABLE in ShellVM.StoryModel.ExplorerView[0].Children) { RecurseCopyUnused(VARIABLE); }
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

    private void RecurseCopyUnused(StoryNodeItem Item)
    {
        try
        {
            if (Item.Type == StoryItemType.Scene) //Check if scene/folder/section, if not then just continue.
            {

                if (!RecursiveCheck(ShellVM.StoryModel.NarratorView).Any(StoryNodeItem => StoryNodeItem.Uuid == Item.Uuid)) //Checks it doesn't already exist in narrator
                {
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
