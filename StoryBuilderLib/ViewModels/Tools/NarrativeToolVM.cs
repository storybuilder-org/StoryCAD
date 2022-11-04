using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;

namespace StoryBuilder.ViewModels.Tools;
public class NarrativeToolVM: ObservableRecipient
{
    private ShellViewModel ShellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
    private LogService Logger = Ioc.Default.GetRequiredService<LogService>();
    public StoryNodeItem SelectedNode;
    public bool IsNarratorSelected = false;
    public RelayCommand CopyCommand { get; } 
    public RelayCommand DeleteCommand { get; } 
    public RelayCommand CopyAllUnusedCommand { get; }
    public RelayCommand CreateFlyout { get; }

    //Name of the section in the flyout
    private string _flyoutText;
    public string FlyoutText
    {
        get => _flyoutText;
        set => SetProperty(ref _flyoutText, value);
    }
    private string _message;
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
    /// Deletes a node from the tree.
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
            else { Logger.Log(LogLevel.Warn, "Selected node was null, doing nothing"); }
        }
        catch (Exception ex) { Logger.LogException(LogLevel.Error, ex, "Error in NarrativeToolVM.Delete()"); }
    }
    

    /// <summary>
    /// Copies all scenes, if the node has children then it will copy all children that are scenes
    /// </summary>
    private void Copy()
    {
        try
        {
            Logger.Log(LogLevel.Info, "Starting to copy node between trees.");

            //Check if selection is null
            if (SelectedNode == null)
            {
                Logger.Log(LogLevel.Warn, "No node selected");
                return;
            }

            Logger.Log(LogLevel.Info, $"Node Selected is a {SelectedNode.Type}");
            if (SelectedNode.Type == StoryItemType.Scene)  //If its just a scene, add it immediately if not already in.
            {
                if (!RecursiveCheck(ShellVM.StoryModel.NarratorView[0].Children).Any(StoryNodeItem => StoryNodeItem.Uuid == SelectedNode.Uuid)) //checks node isn't in the narrator view
                {
                    _ = new StoryNodeItem((SceneModel)ShellVM.StoryModel.StoryElements.StoryElementGuids[SelectedNode.Uuid], ShellVM.StoryModel.NarratorView[0]);
                    Logger.Log(LogLevel.Info, $"Copied SelectedNode {SelectedNode.Name} ({SelectedNode.Uuid})");
                    Message = $"Copied {SelectedNode.Name}";
                }
                else
                {
                    Logger.Log(LogLevel.Warn, $"Node {SelectedNode.Name} ({SelectedNode.Uuid}) already exists in the NarratorView");
                    Message = "This scene already appears in the narrative view.";
                }
            }
            else if (SelectedNode.Type is StoryItemType.Folder or StoryItemType.Section) //If its a folder then recurse and add all unused scenes to the narrative view.
            {
                Logger.Log(LogLevel.Info, "Item is a folder/section, getting flattened list of all children.");
                foreach (var item in RecursiveCheck(SelectedNode.Children))
                {
                    if (item.Type == StoryItemType.Scene && !RecursiveCheck(ShellVM.StoryModel.NarratorView[0].Children).Any(StoryNodeItem => StoryNodeItem.Uuid == item.Uuid))
                    {
                        _ = new StoryNodeItem((SceneModel)ShellVM.StoryModel.StoryElements.StoryElementGuids[item.Uuid], ShellVM.StoryModel.NarratorView[0]);
                        Logger.Log(LogLevel.Info, $"Copied item {SelectedNode.Name} ({SelectedNode.Uuid})");
                    }
                }

                Message = $"Copied {SelectedNode.Children} and child scenes.";
            }
            else
            {
                Logger.Log(LogLevel.Warn, $"Node {SelectedNode.Name} ({SelectedNode.Uuid}) wasn't copied, it was a {SelectedNode.Type}");
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
        //Recurses the children of NarratorView View.
        try { foreach (var item in ShellVM.StoryModel.ExplorerView[0].Children) { RecurseCopyUnused(item); } }
        catch (Exception e) { Logger.LogException(LogLevel.Error, e, "Error in recursive check"); }
    }

    /// <summary>
    /// Creates new section
    /// </summary>
    private void MakeSection()
    {
        if (ShellVM.DataSource == null || ShellVM.DataSource.Count < 0)
        {
            Logger.Log(LogLevel.Warn, "DataSource is empty or null, not adding section");
            return;
        }
        _ = new StoryNodeItem(new SectionModel(FlyoutText, ShellVM.StoryModel), ShellVM.StoryModel.NarratorView[0]);
    }

    /// <summary>
    /// This recursively copies any unused scene in the ExplorerView view.
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
