using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.ViewModels.SubViewModels;

namespace StoryCADTests.ViewModels;

[TestClass]
public class StoryNodeItemTests
{
    /// <summary>
    ///     Creates a new node and tries to delete it.
    /// </summary>
    [TestMethod]
    public void TryDelete()
    {
        var OutlineVM = Ioc.Default.GetService<OutlineViewModel>();
        StoryModel model = new();
        OverviewModel overview = new("Overview", model, null);
        model.ExplorerView.Add(overview.Node);

        TrashCanModel Trash = new(model, null);
        model.TrashView.Add(Trash.Node); // Add to TrashView so MoveToTrash can find it

        ProblemModel problem = new("Test", model, overview.Node);

        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = new StoryDocument(model);
        problem.Node.Delete(StoryViewType.ExplorerView);

        // Verify the problem was moved to trash
        Assert.AreEqual(Trash.Node, problem.Node.Parent, "Problem should be moved to TrashCan");
    }
}
