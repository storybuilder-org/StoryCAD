using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.ViewModels;

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
		ShellViewModel Shell = Ioc.Default.GetRequiredService<ShellViewModel>();
		StoryModel model = new();
		ProblemModel Overview = new("Overview", model);
		StoryNodeItem overview = new(null, Overview, null);
		ProblemModel Problem = new("Test",model);
		StoryNodeItem Prob = new(null, Problem, overview);

		Shell.StoryModel = model;
		Prob.Delete(StoryViewType.ExplorerView);
	}
}