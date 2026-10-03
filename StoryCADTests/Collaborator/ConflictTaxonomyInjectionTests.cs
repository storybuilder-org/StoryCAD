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
///     Collaborator #77 step 3 / StoryCAD #483. The Conflict Builder taxonomy lives in
///     Controls.json. ExampleLists reads Lists.json and cannot reach it, so no workflow has
///     ever seen it. This injects it through the IStoryCADAPI conflict methods.
/// </summary>
[TestClass]
public class ConflictTaxonomyInjectionTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static Dictionary<string, string> Inject()
    {
        var model = new StoryModel();
        var api = CreateApi();
        api.CurrentModel = model;
        var workflow = new Workflow("ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem);
        var args = new Dictionary<string, string>();
        new WorkflowRunner(model, workflow, api).EnrichWithConflictTaxonomy(args);
        return args;
    }

    [TestMethod]
    public void EnrichWithConflictTaxonomy_WritesTheArgKey()
    {
        var args = Inject();

        Assert.IsTrue(args.ContainsKey("ConflictTaxonomy"),
            "the prompt reads the taxonomy from this arg");
        Assert.IsFalse(string.IsNullOrWhiteSpace(args["ConflictTaxonomy"]));
    }

    [TestMethod]
    public void EnrichWithConflictTaxonomy_CarriesEveryCategory()
    {
        var taxonomy = Inject()["ConflictTaxonomy"];

        foreach (var category in new[]
                 {
                     "Relationship", "Information", "Interest", "Structural",
                     "Value", "Identity", "Criminal activities", "Criminal psychology"
                 })
        {
            StringAssert.Contains(taxonomy, category,
                $"category '{category}' must reach the prompt");
        }
    }

    [TestMethod]
    public void EnrichWithConflictTaxonomy_CarriesSubcategoriesAndExamples()
    {
        var taxonomy = Inject()["ConflictTaxonomy"];

        StringAssert.Contains(taxonomy, "Family",
            "subcategories must reach the prompt, not just category names");
        StringAssert.Contains(taxonomy, "aging parent",
            "examples are the point of #483; they must reach the prompt");
    }
}
