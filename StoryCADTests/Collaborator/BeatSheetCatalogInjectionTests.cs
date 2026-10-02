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
///     Collaborator #77 step 10. The Worker template holds a baked beat sheet list that
///     disagrees with Tools.json: different spellings, no W-Diagram, and two invented Mini
///     spines. Inject the built-in catalog so the model picks from what the app actually has.
/// </summary>
[TestClass]
public class BeatSheetCatalogInjectionTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static string Inject()
    {
        var model = new StoryModel();
        var api = CreateApi();
        api.CurrentModel = model;
        var workflow = new Workflow("ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem);
        var args = new Dictionary<string, string>();
        new WorkflowRunner(model, workflow, api).EnrichWithBeatSheets(args);
        return args["BeatSheets"];
    }

    [TestMethod]
    public void EnrichWithBeatSheets_CarriesTheBuiltInSheetNames()
    {
        var catalog = Inject();

        foreach (var sheet in new[]
                 {
                     "Three Act Play", "Save The Cat", "Hero's Journey", "W-Diagram",
                     "Character Arc (Mini)", "Romantic Subplot Beat Sheet (Mini)"
                 })
        {
            StringAssert.Contains(catalog, sheet, $"'{sheet}' is in Tools.json and must reach the prompt");
        }
    }

    [TestMethod]
    public void EnrichWithBeatSheets_ExcludesTheUserActions()
    {
        var catalog = Inject();

        Assert.IsFalse(catalog.Contains("Load Custom Beat Sheet from file"),
            "loading a sheet from disk is a user action, not a model choice");
        Assert.IsFalse(catalog.Contains("Custom Beat Sheet"),
            "an empty custom sheet is a user action, not a model choice");
    }

    [TestMethod]
    public void EnrichWithBeatSheets_CarriesBeatNamesForEachSheet()
    {
        var catalog = Inject();

        StringAssert.Contains(catalog, "Opening Image",
            "the model needs the beats, not only the sheet name");
        StringAssert.Contains(catalog, "Ordinary World");
    }
}
