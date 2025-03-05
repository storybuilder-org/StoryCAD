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
	public void TryDelete()
	{
        OutlineViewModel OutlineVM = Ioc.Default.GetRequiredService<OutlineViewModel>();
		StoryModel model = new();
		ProblemModel Overview = new("Overview", model);
		StoryNodeItem overview = new(null, Overview, null);
		ProblemModel Problem = new("Test",model);
		StoryNodeItem Prob = new(null, Problem, overview);

        OutlineVM.StoryModel = model;
		Prob.Delete(StoryViewType.ExplorerView);
	}
}