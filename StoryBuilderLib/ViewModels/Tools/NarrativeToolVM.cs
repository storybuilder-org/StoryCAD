using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StoryBuilder.ViewModels.Tools;
public class NarrativeToolVM
{
    public ShellViewModel ShellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
    public LogService Logger = Ioc.Default.GetRequiredService<LogService>();
    public RelayCommand CopyCommand { get; } 
    public RelayCommand DeleteCommand { get; } 
    public RelayCommand CopyAllUnusedCommand { get; } 
    public  string Message { get; set; }

    public NarrativeToolVM()
    {
        CopyCommand = new RelayCommand(Copy);
        CopyAllUnusedCommand = new RelayCommand(CopyAllUnused);
        DeleteCommand = new RelayCommand(Delete);
    }

    public void Delete()
    {
        Logger.Log(LogLevel.Trace, "Deleting element");
        try
        {
            if (ShellVM.CurrentNode == null)
            {
                Logger.Log(LogLevel.Error, "Current node is null, aborting delete");
                return;
            }

            if (ShellVM.CurrentNode.Type == StoryItemType.TrashCan || ShellVM.CurrentNode.IsRoot)
            {
                Logger.Log(LogLevel.Error, "Cannot delete this node, was either trash can or a Root");
                return;
            }

            //Even though there should only be one copy, just delete any just in case.
            if (ShellVM.StoryModel.NarratorView[0].Children.Contains(ShellVM.CurrentNode))
            {
                ShellVM.StoryModel.NarratorView[0].Children.Remove(ShellVM.CurrentNode);
                ShellVM.StoryModel.NarratorView[1].Children.Add(ShellVM.CurrentNode);
            }
            foreach (StoryNodeItem child in ShellVM.StoryModel.NarratorView[0].Children) { recurseDelete(ShellVM.CurrentNode, child); }
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
        try
        {
            if (Parent.Children.Contains(item)) //Checks parent contains child we are looking.
            {
                Parent.Children.Remove(item); //Deletes child.
                ShellVM.StoryModel.NarratorView[1].Children.Add(ShellVM.CurrentNode);
            }
            else //If child isn't in parent, recurse again.
            {
                foreach (StoryNodeItem child in Parent.Children) { recurseDelete(item, child); }
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error, ex,"Error deleting node in Recursive delete");
        }

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
            if (ShellVM.CurrentNode.Type == StoryItemType.Scene)  //check the node is either a scene OR has children
            {
                if (!RecursiveCheck(ShellVM.StoryModel.NarratorView[0].Children).Any(StoryNodeItem => StoryNodeItem.Uuid == ShellVM.CurrentNode.Uuid))
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

            foreach (var child in Item.Children) { RecurseCopyUnused(child); }  
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error, ex, "Error in NarrativeTool.CopyAllUnused()");
            Message = "Error copying nodes.";
        }
    }
}
