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
    public  string Message { get; set; }


    public NarrativeToolVM()
    {
        CopyCommand = new RelayCommand(Copy);
        CopyAllUnusedCommand = new RelayCommand(CopyAllUnused);
        MoveUpCommand = new RelayCommand(MoveUp);
        DeleteCommand = new RelayCommand(Delete);
    }

    public void Delete()
    {
        Logger.Log(LogLevel.Trace, "RemoveStoryElement");
        if (LastSelectedNode == null)
        {
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
        if (LastSelectedNode == null) { return;} //Null check.

        if (LastSelectedNode.Parent.Equals(ShellVM.StoryModel.NarratorView[0])) //Checks parent isn't root.
        {
            int OldIndex = LastSelectedNode.Parent.Children.IndexOf(LastSelectedNode);
            int NewIndex = OldIndex--; //Moving up, take one from index.

            if (NewIndex != -1) //If the item isn't at position 0, just move in list.
            {
                LastSelectedNode.Parent.Children.Move(OldIndex, NewIndex);
            }
            else //Remove from current parent and place in parent of parent.
            {
                LastSelectedNode.Parent.Children.Remove(LastSelectedNode);
                LastSelectedNode.Parent.Parent.Children.Add(LastSelectedNode);
            }
        }
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
                if (!RecursiveCheck(ShellVM.StoryModel.NarratorView).Any(StoryNodeItem => StoryNodeItem.Uuid == LastSelectedNode.Uuid))
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
