using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #77 step 11. ProblemBuilder consolidates ConflictBuilder, GMC, Structure,
///     and BeatScenes. The four stay registered for A:B until the cleanup issue.
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

        Assert.IsTrue(workflow.CreatesScenesForBeats, "it creates Scene stubs for empty beats");
        Assert.IsTrue(workflow.RequiresProblemCategory, "the category picks the beat sheet class");
        Assert.IsTrue(workflow.InjectsConflictTaxonomy, "the #483 taxonomy feeds the conflict fields");
        Assert.IsTrue(workflow.InjectsBeatSheets, "the model picks from the built-in sheets");
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
    public void ProblemBuilder_WritesBeatsViaTheBeatSheetPath()
    {
        var beats = Builder().GetIO().Outputs
            .Single(o => o.ElementLabel == "Problem")
            .PropertiesToUpdate.Single(p => p.Property == "StructureBeats");

        Assert.AreEqual(WriteVia.BeatSheet, beats.WriteVia);
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
    public void ReplacedWorkflows_StayRegisteredForAbTesting()
    {
        foreach (var label in new[] { "ConflictBuilder", "GMC", "Structure", "BeatScenes" })
        {
            Assert.IsTrue(WorkflowRegistry.All.Any(w => w.Label == label),
                $"'{label}' stays until the cleanup issue retires it");
        }
    }
}
