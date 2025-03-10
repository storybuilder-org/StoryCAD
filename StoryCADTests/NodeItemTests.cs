using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCADTests;

[TestClass]
class NodeItemTests
{
	/// <summary>
	/// Creates a new node and tries to delete it.
	/// </summary>
	[TestMethod]
    internal void TryDelete()
	{
        OutlineViewModel OutlineVM = Ioc.Default.GetRequiredService<OutlineViewModel>();
		StoryModel model = new();
		OverviewModel overview = new("Overview", model, null);
		ProblemModel problem = new("Test", model, overview.Node);

        OutlineVM.StoryModel = model;
		problem.Node.Delete(StoryViewType.ExplorerView);
	}
}