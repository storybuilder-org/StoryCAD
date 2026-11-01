using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #77 step 11. ProblemBuilder consolidates ConflictBuilder, GMC, Structure,
///     and BeatScenes. The four stayed registered for A:B; #211 deleted them.
/// </summary>
[TestClass]
public class ProblemBuilderRegistryTests
{
    private static Workflow Builder() =>
        WorkflowRegistry.All.FirstOrDefault(w => w.Label == "ProblemBuilder");

    [TestMethod]
    public void ProblemBuilder_IsRegistered_AsAProblemWorkflow()
    {
        var workflow = Builder();

        Assert.IsNotNull(workflow, "ProblemBuilder must be registered");
        Assert.AreEqual("Problem Builder", workflow.Title);
        Assert.AreEqual(StoryItemType.Problem, workflow.PrimaryElementType);
    }

    [TestMethod]
    public void ProblemBuilder_DeclaresItsCapabilities()
    {
        var workflow = Builder();

        Assert.IsTrue(workflow.RequiresProblemCategory, "the category feeds the conflict fields");
        Assert.IsTrue(workflow.InjectsConflictTaxonomy, "the #483 taxonomy feeds the conflict fields");
        // Collaborator #302: the beat work moved to ProblemBeats.
        Assert.IsFalse(workflow.CreatesScenesForBeats);
        Assert.IsFalse(workflow.CreatesProblemsForBeats);
        Assert.IsFalse(workflow.InjectsBeatSheets);
        Assert.IsFalse(workflow.InjectsStockScenes);
        Assert.IsFalse(workflow.InjectsCurrentBeats);
        Assert.IsFalse(workflow.OffersPlacedScenes);
    }

    [TestMethod]
    public void ProblemBuilder_RequiresProblemOverviewAndBothSeats()
    {
        var required = Builder().GetIO().RequiredInputs.Select(r => r.ElementLabel).ToList();

        CollectionAssert.Contains(required, "Problem");
        CollectionAssert.Contains(required, "Overview");
        CollectionAssert.Contains(required, "Protagonist");
        CollectionAssert.Contains(required, "Antagonist");
    }

    [TestMethod]
    public void ProblemBuilder_CreatesNoElementThroughGather()
    {
        foreach (var requirement in Builder().GetIO().RequiredInputs)
        {
            Assert.IsFalse(requirement.CreateIfMissing,
                $"'{requirement.ElementLabel}' must be a precondition, not something gather invents");
        }
    }

    [TestMethod]
    public void ProblemBuilder_WritesTheRequiredFieldSpine()
    {
        var written = Builder().GetIO().Outputs
            .Single(o => o.ElementLabel == "Problem")
            .PropertiesToUpdate.Select(p => p.Property).ToList();

        foreach (var property in new[]
                 {
                     "Name", "Description", "ProblemType", "ConflictType", "Subject", "Premise",
                     "ProtGoal", "ProtMotive", "ProtConflict",
                     "AntagGoal", "AntagMotive", "AntagConflict", "Outcome"
                 })
        {
            CollectionAssert.Contains(written, property,
                $"'{property}' is on the RequiredFieldGapScanner spine");
        }
    }

    [TestMethod]
    public void ProblemBuilder_DoesNotWriteItsOwnPreconditions()
    {
        var written = Builder().GetIO().Outputs
            .Single(o => o.ElementLabel == "Problem")
            .PropertiesToUpdate.Select(p => p.Property).ToList();

        CollectionAssert.DoesNotContain(written, "ProblemCategory",
            "the user sets the category; nothing on a ProblemBuilder run writes it");
    }

    [TestMethod]
    public void ProblemBuilder_WritesNoStructureOutputs()
    {
        var written = Builder().GetIO().Outputs
            .Single(o => o.ElementLabel == "Problem")
            .PropertiesToUpdate.Select(p => p.Property).ToList();

        CollectionAssert.DoesNotContain(written, "StructureTitle");
        CollectionAssert.DoesNotContain(written, "StructureDescription");
        CollectionAssert.DoesNotContain(written, "StructureBeats");
    }

    [TestMethod]
    public void ProblemBeats_IsRegisteredAfterProblemBuilder_WithTheThreeStructureOutputs()
    {
        var all = WorkflowRegistry.All.ToList();
        var beats = all.SingleOrDefault(w => w.Label == "ProblemBeats");

        Assert.IsNotNull(beats, "ProblemBeats must be registered");
        Assert.AreEqual("Problem Beats", beats.Title);
        Assert.AreEqual(StoryItemType.Problem, beats.PrimaryElementType);
        Assert.AreEqual(all.FindIndex(w => w.Label == "ProblemBuilder") + 1, all.IndexOf(beats),
            "listed right after ProblemBuilder");

        var written = beats.GetIO().Outputs.Single(o => o.ElementLabel == "Problem").PropertiesToUpdate;
        CollectionAssert.AreEqual(
            new[] { "StructureTitle", "StructureDescription", "StructureBeats" },
            written.Select(p => p.Property).ToList());
        Assert.AreEqual(WriteVia.BeatSheet, written.Single(p => p.Property == "StructureBeats").WriteVia);
        Assert.AreEqual("beats", written.Single(p => p.Property == "StructureBeats").JsonKey);

        Assert.IsTrue(beats.OffersPlacedScenes);
        Assert.IsTrue(beats.CreatesScenesForBeats);
        Assert.IsTrue(beats.CreatesProblemsForBeats);
        Assert.IsTrue(beats.RequiresProblemCategory);
        Assert.IsTrue(beats.InjectsConflictTaxonomy);
        Assert.IsTrue(beats.InjectsBeatSheets);
        Assert.IsTrue(beats.InjectsStockScenes);
        Assert.IsTrue(beats.InjectsCurrentBeats);
        Assert.IsNotNull(beats.JsonInput, "ProblemBeats sends the input object");
    }

    [TestMethod]
    public void ProblemBeats_IsNotStarredByDefault()
    {
        CollectionAssert.DoesNotContain(WorkflowRegistry.DefaultStarredLabels.ToList(), "ProblemBeats");
    }

    [TestMethod]
    public void RetiredStructureLabels_ReplaceWithProblemBeats()
    {
        Assert.AreEqual("ProblemBeats", WorkflowRegistry.RetiredWorkflowReplacements["Structure"]);
        Assert.AreEqual("ProblemBeats", WorkflowRegistry.RetiredWorkflowReplacements["BeatScenes"]);
    }

    [TestMethod]
    public void ProblemBuilder_OffersProblemAndSceneChoices()
    {
        var collections = Builder().GetIO().CollectionInputs.Select(c => c.RequestName).ToList();

        CollectionAssert.Contains(collections, "SceneChoices", "an empty beat prefers a free Scene");
        CollectionAssert.Contains(collections, "ProblemChoices", "a beat may hold an existing Problem");
    }

    [TestMethod]
    public void ProblemBuilder_InjectsExamplesForEveryListBackedOutput()
    {
        // A live run returned ProblemType "Integrity". Lists.json allows only Conflict,
        // Decision, Discover. A field whose value must come from a list needs its examples
        // injected, or the model invents a value the dropdown cannot show.
        var lists = Builder().GetIO().ExampleLists;

        foreach (var name in new[]
                 {
                     "ProblemType", "ProblemSource", "Outcome", "Method", "Theme",
                     "Motive", "ConflictType", "ProblemCategory"
                 })
        {
            CollectionAssert.Contains(lists.ToList(), name,
                $"'{name}' is list-backed; its examples must reach the prompt");
        }
    }

    [TestMethod]
    public void ProblemBuilder_OffersCharacterChoicesForStubCast()
    {
        // #208 handoff: a created stub carries CastMembers resolved from CharacterChoices.
        CollectionAssert.Contains(
            Builder().GetIO().CollectionInputs.Select(c => c.RequestName).ToList(),
            "CharacterChoices");
        CollectionAssert.Contains(Builder().GetIO().ExampleLists.ToList(), "SceneType",
            "the stub's SceneType comes from the Lists.json SceneType values");
    }

    [TestMethod]
    public void ProblemBuilder_IsStarredByDefault()
    {
        CollectionAssert.Contains(WorkflowRegistry.DefaultStarredLabels.ToList(), "ProblemBuilder",
            "an unstarred workflow does not appear in the top band");
    }

    [TestMethod]
    public void ReplacedWorkflows_AreGone()
    {
        foreach (var label in new[] { "ConflictBuilder", "GMC", "Structure", "BeatScenes" })
        {
            Assert.IsFalse(WorkflowRegistry.All.Any(w => w.Label == label),
                $"#211 deleted '{label}'; ProblemBuilder is the Problem surface");
        }
    }
}
