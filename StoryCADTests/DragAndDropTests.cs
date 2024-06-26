﻿using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;

namespace StoryCADTests;

[TestClass]
public class DragAndDropTests
{/*
    private StoryNodeItem root;
    private StoryNodeItem child1;
    private StoryNodeItem child2;
    private StoryNodeItem grandchild1;
    private StoryNodeItem trash;
    private StoryNodeItem trashedItem;
    private StoryModel storyModel = new();
    private ShellViewModel ShellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
    
    /// <summary>
    /// Sets up the tests.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        //Simulates a real story tree
        root = new StoryNodeItem(new StoryElement("Overview", StoryItemType.StoryOverview, storyModel), null, StoryItemType.StoryOverview) { IsRoot = true };
        child1 = new StoryNodeItem(new StoryElement("Child1", StoryItemType.Problem, storyModel),root, StoryItemType.Problem) { Parent = root };
        child2 = new StoryNodeItem(new StoryElement("Child2", StoryItemType.Problem, storyModel),root, StoryItemType.Problem) { Parent = root };
        grandchild1 = new StoryNodeItem(new StoryElement("grandchild1", StoryItemType.Problem, storyModel), root, StoryItemType.Problem) { Parent = root };
        trash = new StoryNodeItem(new StoryElement("trash", StoryItemType.TrashCan, storyModel), root, StoryItemType.TrashCan) { Parent = root };
        trashedItem = new StoryNodeItem(new StoryElement("trashedItem", StoryItemType.Problem, storyModel), trash, StoryItemType.Problem) { Parent = root };
        root.Children.Add(child1);
        root.Children.Add(child2);
        child1.Children.Add(grandchild1);
        trash.Children.Add(trashedItem);
        ShellVM.StoryModel.ExplorerView.Add(root);
        ShellVM.StoryModel.ExplorerView.Add(trash);
    }

    /// <summary>
    /// Checks elements can't be made roots.
    /// </summary>
    [TestMethod]
    public void MovingToRoot()
    {
        // Not an error?
        var result = ShellVM.ValidateDragAndDrop(child1, root);
        Assert.IsTrue(result, "Moving a node to a root node should be considered an invalid move.");
    }
    
    
    /// <summary>
    /// Checks elements can't become their own parents
    /// </summary>
    [TestMethod]
    public void MovingIntoDescendant()
    {
        var result = ShellVM.ValidateDragAndDrop(root, grandchild1);
        Assert.IsTrue(result, "Moving a node into one of its own descendants should be considered an invalid move.");
    }

    /// <summary>
    /// Checks elements can be moved normally
    /// </summary>
    [TestMethod]
    public void MovingToNonDescendant()
    {
        Thread.Sleep(8000); //8-second delay appears to be needed to ensure the test passes consistently.
        var result = ShellVM.ValidateDragAndDrop(child1, child2);
        Assert.IsFalse(result, "Moving a node to a non-descendant node should be considered a valid move.");
    }
    
    /// <summary>
    /// Checks the trash node itself can't be moved
    /// </summary>
    [TestMethod]
    public void MovingTrash()
    {
        var result = ShellVM.ValidateDragAndDrop(trash, child2);
        Assert.IsTrue(result, "Moving the trash node should be considered an invalid move.");
    }
    
    /// <summary>
    /// Checks items can't be dragged from trash
    /// </summary>
    [TestMethod]
    public void MovingOutOfTrash()
    {
        var result = ShellVM.ValidateDragAndDrop(trashedItem, child1);
        Assert.IsTrue(result, "Moving an item from trash node should be considered an invalid move.");
    }

    /// <summary>
    /// Checks items can't be dragged to trash
    /// </summary>
    [TestMethod]
    public void MovingToTrash()
    {
        var result = ShellVM.ValidateDragAndDrop(child1, trashedItem);
        Assert.IsTrue(result, "Moving an item to the trash node should be considered an invalid move.");
    }*/

}