using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCADTests;

[TestClass]
public class NodeItemTests
{
	/// <summary>
	/// Creates a new node and tries to delete it.
	/// </summary>
	[TestMethod]
    public void TryDelete()
	{
        OutlineViewModel OutlineVM = Ioc.Default.GetService<OutlineViewModel>();
		StoryModel model = new();
		OverviewModel overview = new("Overview", model, null);
		TrashCanModel Trash = new(model, null);
		ProblemModel problem = new("Test", model, overview.Node);

        var appState = Ioc.Default.GetRequiredService<StoryCAD.Models.AppState>();
        appState.CurrentDocument = new StoryCAD.Models.StoryDocument(model, null);
		problem.Node.Delete(StoryViewType.ExplorerView);
	}
}