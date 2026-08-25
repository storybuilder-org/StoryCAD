using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCollaborator;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #77. The enrich methods were covered but their wiring was not: deleting
///     the calls that run them left every test green. These assert the declared capability
///     actually causes the injection.
/// </summary>
[TestClass]
public class DeclaredInjectionWiringTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static Dictionary<string, string> Inject(Workflow workflow)
    {
        var model = new StoryModel();
        var api = CreateApi();
        api.CurrentModel = model;
        var args = new Dictionary<string, string>();
        new WorkflowRunner(model, workflow, api).ApplyDeclaredInjections(args);
        return args;
    }

    [TestMethod]
    public void DeclaringNothing_InjectsNothing()
    {
        var args = Inject(new Workflow("Plain", "Plain", "t", StoryItemType.Problem));

        Assert.AreEqual(0, args.Count, "a workflow that declares no injection gets none");
    }

    [TestMethod]
    public void DeclaringTaxonomy_InjectsIt()
    {
        var workflow = new Workflow("ProblemBuilder", "PB", "t", StoryItemType.Problem)
        {
            InjectsConflictTaxonomy = true
        };

        CollectionAssert.Contains(Inject(workflow).Keys.ToList(), "ConflictTaxonomy");
    }

    [TestMethod]
    public void DeclaringBeatSheets_InjectsThem()
    {
        var workflow = new Workflow("ProblemBuilder", "PB", "t", StoryItemType.Problem)
        {
            InjectsBeatSheets = true
        };

        CollectionAssert.Contains(Inject(workflow).Keys.ToList(), "BeatSheets");
    }

    [TestMethod]
    public void DeclaringStockScenes_InjectsThem()
    {
        var workflow = new Workflow("ProblemBuilder", "PB", "t", StoryItemType.Problem)
        {
            InjectsStockScenes = true
        };

        CollectionAssert.Contains(Inject(workflow).Keys.ToList(), "StockScenes");
    }

    [TestMethod]
    public void BeatScenes_KeepsStockScenesByLabel()
    {
        // BeatScenes has no flag in the registry; the label path must keep working for A:B.
        var workflow = new Workflow("BeatScenes", "Scenes from Beats", "t", StoryItemType.Problem);

        CollectionAssert.Contains(Inject(workflow).Keys.ToList(), "StockScenes");
    }

    [TestMethod]
    public void RegisteredProblemBuilder_InjectsAllThree()
    {
        var registered = WorkflowRegistry.All.Single(w => w.Label == "ProblemBuilder");
        var keys = Inject(registered).Keys.ToList();

        CollectionAssert.Contains(keys, "ConflictTaxonomy");
        CollectionAssert.Contains(keys, "BeatSheets");
        CollectionAssert.Contains(keys, "StockScenes");
    }
}
