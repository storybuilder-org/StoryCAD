using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryBuilder.ViewModels.Tools;
public class NarrativeToolVM: ObservableRecipient
{
    public ShellViewModel ShellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
    public LogService Logger = Ioc.Default.GetRequiredService<LogService>();
    public RelayCommand CopyCommand { get; } 
    public RelayCommand DeleteCommand { get; } 
    public RelayCommand CopyAllUnusedCommand { get; }

    private string _message;
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public NarrativeToolVM()
    {
        CopyCommand = new RelayCommand(Copy);
        CopyAllUnusedCommand = new RelayCommand(CopyAllUnused);
        DeleteCommand = new RelayCommand(Delete);
    }

    /// <summary>
    /// Deletes a node from the tree.
    /// </summary>
    public void Delete()
    {
        Logger.Log(LogLevel.Trace, "Deleting element");
        try
        {
            //Check for null and ensure the node is suposed to be deleted first, then proceed with deleting.
            if (ShellVM.CurrentNode == null)
            {
                Logger.Log(LogLevel.Warn, "Current node is null, aborting delete");
                return;
            }

            if (ShellVM.CurrentNode.Type == StoryItemType.TrashCan || ShellVM.CurrentNode.IsRoot)
            {
                Logger.Log(LogLevel.Warn, "Cannot delete this node, was either trash can or a Root");
                return;
            }

            //Check the root of the narrator view first, then recurse.
            if (ShellVM.StoryModel.NarratorView[0].Children.Contains(ShellVM.CurrentNode))
            {
                Logger.Log(LogLevel.Info, "Node is not a child of a child, removing it from NarratorView[0]");
                ShellVM.StoryModel.NarratorView[0].Children.Remove(ShellVM.CurrentNode);
                ShellVM.StoryModel.NarratorView[1].Children.Add(ShellVM.CurrentNode);
            }
            
            foreach (StoryNodeItem child in ShellVM.StoryModel.NarratorView[0].Children)
            {
                Logger.Log(LogLevel.Info, "Node is either a child or not in the tree, recursing tree.");
                RecurseDelete(ShellVM.CurrentNode, child);
            }
        }
        catch (Exception e) { Logger.LogException(LogLevel.Error,e, "Error Deleting node in NarrativeTool.Delete()"); }
    }

    /// <summary>
    /// Recursively deletes from NarratorView.
    /// </summary>
    private void RecurseDelete(StoryNodeItem item, StoryNodeItem Parent)
    {
        Logger.Log(LogLevel.Info, "Starting recursive delete instance");
        try
        {
            if (Parent.Children.Contains(item)) //Checks parent contains child we are looking.
            {
                Logger.Log(LogLevel.Info, "StoryNodeItem found, deleting it.");
                Parent.Children.Remove(item); //Deletes child.
                ShellVM.StoryModel.NarratorView[1].Children.Add(ShellVM.CurrentNode);
            }
            else //If child isn't in parent, recurse again.
            {
                Logger.Log(LogLevel.Info, "StoryNodeItem not found, recursing again");
                foreach (StoryNodeItem child in Parent.Children) { RecurseDelete(item, child); }
            }
        }
        catch (Exception ex) { Logger.LogException(LogLevel.Error, ex,"Error deleting node in Recursive delete"); }
    } 

    /// <summary>
    /// Copies all scenes, if the node has children then it will copy all children that are scenes
    /// </summary>
    private void Copy()
    {
        try
        {
            Logger.Log(LogLevel.Info, $"Starting to copy node between trees.");

            //Check if selection is null
            if (ShellVM.CurrentNode == null)
            {
                Logger.Log(LogLevel.Warn, "No node selected");
                return;
            }

            Logger.Log(LogLevel.Info, $"Node Selected is a {ShellVM.CurrentNode.Type}");
            if (ShellVM.CurrentNode.Type == StoryItemType.Scene)  //If its just a scene, add it immediately if not already in.
            {
                if (!RecursiveCheck(ShellVM.StoryModel.NarratorView[0].Children).Any(StoryNodeItem => StoryNodeItem.Uuid == ShellVM.CurrentNode.Uuid)) //checks node isn't in the narrator view
                {
                    _ = new StoryNodeItem((SceneModel)ShellVM.StoryModel.StoryElements.StoryElementGuids[ShellVM.CurrentNode.Uuid], ShellVM.StoryModel.NarratorView[0]);
                    Logger.Log(LogLevel.Info, $"Copied ShellVM.CurrentNode {ShellVM.CurrentNode.Name} ({ShellVM.CurrentNode.Uuid})");
                }
                else
                {
                    Logger.Log(LogLevel.Warn, $"Node {ShellVM.CurrentNode.Name} ({ShellVM.CurrentNode.Uuid}) already exists in the NarratorView");
                    Message = "This scene already appears in the narrative view.";
                }
            }
            else if (ShellVM.CurrentNode.Type is StoryItemType.Folder or StoryItemType.Section) //If its a folder then recurse and add all unused scenes to the narrative view.
            {
                Logger.Log(LogLevel.Info, $"Item is a folder/section, getting flattened list of all children.");
                foreach (var item in RecursiveCheck(ShellVM.CurrentNode.Children))
                {
                    if (item.Type == StoryItemType.Scene && !RecursiveCheck(ShellVM.StoryModel.NarratorView[0].Children).Any(StoryNodeItem => StoryNodeItem.Uuid == item.Uuid))
                    {
                        _ = new StoryNodeItem((SceneModel)ShellVM.StoryModel.StoryElements.StoryElementGuids[item.Uuid], ShellVM.StoryModel.NarratorView[0]);
                        Logger.Log(LogLevel.Info, $"Copied item {ShellVM.CurrentNode.Name} ({ShellVM.CurrentNode.Uuid})");
                    }
                }
            }
            else
            {
                Logger.Log(LogLevel.Warn, $"Node {ShellVM.CurrentNode.Name} ({ShellVM.CurrentNode.Uuid}) wasn't copied, it was a {ShellVM.CurrentNode.Type}");
                Message = "You can't copy that.";
            }
        }
        catch (Exception ex) { Logger.LogException(LogLevel.Error, ex, "Error in NarrativeTool.Copy()"); }
        Logger.Log(LogLevel.Info, "NarrativeTool.Copy() complete.");

    }


    private List<StoryNodeItem> RecursiveCheck(ObservableCollection<StoryNodeItem> List)
    {
        Logger.Log(LogLevel.Info, "New instance of Recursive check starting.");
        List<StoryNodeItem> NewList = new();
        try
        {
            foreach (var VARIABLE in List)
            {
                NewList.Add(VARIABLE);
                NewList.AddRange(RecursiveCheck(VARIABLE.Children));
            }
        }
        catch (Exception exception) { Logger.LogException(LogLevel.Error, exception, "Error in recursive check"); }
        
        return NewList;
    }

    /// <summary>
    /// This copies all unused scenes.
    /// </summary>
    private void CopyAllUnused()
    {
        try { foreach (var item in ShellVM.StoryModel.ExplorerView[0].Children) { RecurseCopyUnused(item); } }
        catch (Exception e) { Logger.LogException(LogLevel.Error, e, "Error in recursive check"); }
    }

    /// <summary>
    /// This recursively copies any unused scene in the Explorer view.
    /// </summary>
    /// <param name="Item">The parent item </param>
    private void RecurseCopyUnused(StoryNodeItem Item)
    {
        Logger.Log(LogLevel.Trace, $"Recursing through {Item.Name} ({Item.Uuid})");
        try
        {
            if (Item.Type == StoryItemType.Scene) //Check if scene/folder/section, if not then just continue.
            {
                //This calls recursive check, which returns flattens the entire the tree and .Any() checks if the UUID is in anywhere in the model.
                if (!RecursiveCheck(ShellVM.StoryModel.NarratorView[0].Children).Any(StoryNodeItem => StoryNodeItem.Uuid == Item.Uuid)) 
                {
                    //Since the node isn't in the node, then we add it here.
                    Logger.Log(LogLevel.Trace, $"{Item.Name} ({Item.Uuid}) not found in Narrative view, adding it to the tree");
                    _ = new StoryNodeItem((SceneModel)ShellVM.StoryModel.StoryElements.StoryElementGuids[Item.Uuid], ShellVM.StoryModel.NarratorView[0]);
                }
            }

            foreach (var child in Item.Children) { RecurseCopyUnused(child); }  
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error, ex, "Error in NarrativeTool.CopyAllUnused()");
            Message = "Error copying nodes.";
        }
    }
}
