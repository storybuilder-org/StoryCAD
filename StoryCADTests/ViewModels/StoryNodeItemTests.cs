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
        TrashCanModel Trash = new(model, null);
        ProblemModel problem = new("Test", model, overview.Node);

        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = new StoryDocument(model);
        problem.Node.Delete(StoryViewType.ExplorerView);
    }
}
