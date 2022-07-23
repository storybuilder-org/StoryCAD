using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using ABI.Microsoft.UI;
using ABI.System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;

namespace StoryBuilder.ViewModels.Tools;
public class NarrativeToolVM
{
    public StoryNodeItem LastSelectedNode;
    public ShellViewModel ShellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
    public LogService Logger = Ioc.Default.GetRequiredService<LogService>();
    public RelayCommand CopyCommand { get; } 
    public RelayCommand CopyAllUnusedCommand { get; } 

    public NarrativeToolVM()
    {
        CopyCommand = new RelayCommand(Copy);
        CopyAllUnusedCommand = new RelayCommand(CopyAllUnused);
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
            if (LastSelectedNode.Type == StoryItemType.Scene || LastSelectedNode.Children.Count != 0)  //check the node is either a scene OR has children
            {
                if (LastSelectedNode.Children.Count != 0) //has Children
                {
                    Logger.Log(LogLevel.Info, $"Iterating through children");
                    foreach (var child in LastSelectedNode.Children) //Iterates through children and checks they are a Scene
                    {
                        if (child.Type == StoryItemType.Scene) //Does nothing if not a scene.
                        {
                            Logger.Log(LogLevel.Info, $"Found Child {child.Name}");
                            ShellVM.StoryModel.NarratorView.Add(child); //TODO: Make recursive to children of Child.
                        }
                    }
                    Logger.Log(LogLevel.Info, $"Creating new parent Child");
                    StoryNodeItem _newParent = LastSelectedNode;
                    _newParent.Children.Clear();
                    ShellVM.StoryModel.NarratorView.Add(_newParent);
                    Logger.Log(LogLevel.Info, $"Creating copied parent without children.");
                }
                else //No Children
                {
                    ShellVM.StoryModel.NarratorView.Add(LastSelectedNode);
                    Logger.Log(LogLevel.Info, $"Creating copied parent without children.");
                }
                Logger.Log(LogLevel.Info, $"NarrativeTool.Copy() complete.");
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

    private void RecurseCopyUnused(StoryNodeItem Item)
    {
        try
        {
            if (Item.Type == StoryItemType.Scene) //Check if scene, if not then just continue.
            {
                if (!ShellVM.StoryModel.NarratorView.Contains(Item)) //Checks it doesn't already exist in narrator
                {
                    StoryNodeItem _newItem = new StoryNodeItem(
                            (SceneModel)ShellVM.StoryModel.StoryElements.StoryElementGuids[LastSelectedNode.Uuid],
                            ShellVM.StoryModel.NarratorView[0]);

                    _newItem.Children.Clear();
                }
            }

            foreach (var Child in Item.Children) { RecurseCopyUnused(Child); }
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error, ex, "Error in NarrativeTool.CopyAllUnused()");
        }
    }
}
