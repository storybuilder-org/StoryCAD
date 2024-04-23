using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.ViewModels;

namespace StoryCADTests;

[TestClass]
public class TreeviewMoveTests
{
	private StoryNodeItem root;
	private StoryNodeItem child;
	private StoryNodeItem grandchild;
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
		child = new StoryNodeItem(new StoryElement("Child", StoryItemType.Problem, storyModel), root, StoryItemType.Problem) { Parent = root };
		grandchild = new StoryNodeItem(new StoryElement("grandchild", StoryItemType.Problem, storyModel), root, StoryItemType.Problem) { Parent = root };
		root.Children.Add(child);
		child.Children.Add(grandchild);
		ShellVM.StoryModel.ExplorerView.Add(root);
	}

	/// <summary>
	/// simulates moving left with a bad node caused by DND.
	/// </summary>
	[TestMethod]
	public void TestMoveLeftWithBadTree()
	{
		root = new StoryNodeItem(new StoryElement("Overview", StoryItemType.StoryOverview, storyModel), null, StoryItemType.StoryOverview) { IsRoot = true };
		child = new StoryNodeItem(new StoryElement("Child", StoryItemType.Problem, storyModel), root, StoryItemType.Problem) { Parent = root };
		grandchild = new StoryNodeItem(new StoryElement("grandchild", StoryItemType.Problem, storyModel), root, StoryItemType.Problem) { Parent = child };
		root.Children.Add(child);
		root.Children.Add(grandchild);
		ShellVM.StoryModel.ExplorerView.Add(root);
		ShellVM.RightTappedNode = grandchild;
		ShellVM.CurrentNode = grandchild;
		ShellVM.MoveLeftCommand.Execute(null);
	}
}