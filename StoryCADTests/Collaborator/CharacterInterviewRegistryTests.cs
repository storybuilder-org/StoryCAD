using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #119: Character Interview registry and transcript.
///
/// Terry rejected the first build because Collaborator answered as the character and the
/// exchange was never saved. These tests hold the two things that changed.
/// </summary>
[TestClass]
public class CharacterInterviewRegistryTests
{
    private static Workflow Interview() =>
        WorkflowRegistry.All.First(w => w.Label == "CharacterInterview");

    [TestMethod]
    public void CharacterInterview_IsConversational_AndTargetsCharacter()
    {
        var workflow = Interview();

        Assert.AreEqual(WorkflowMode.Conversational, workflow.Mode);
        Assert.AreEqual(StoryItemType.Character, workflow.PrimaryElementType);
        Assert.IsTrue(workflow.GetIO().RequiredInputs.Any(r => r.ElementLabel == "Character"));
    }

    [TestMethod]
    public void CharacterInterview_DeclaresOptionalOverviewAndProblem()
    {
        var labels = Interview().GetIO().OptionalInputs.Select(r => r.ElementLabel).ToList();

        CollectionAssert.Contains(labels, "Overview");
        CollectionAssert.Contains(labels, "Problem");
    }

    [TestMethod]
    public void CharacterInterview_ProposesNothing()
    {
        // The interview asks; the writer answers; the transcript is saved verbatim. There
        // is no model pass between what was typed and what the outline keeps, so there is
        // nothing for the proposal path to extract.
        var io = Interview().GetIO();

        Assert.AreEqual(0, io.Outputs.SelectMany(o => o.PropertiesToUpdate).Count());
    }

    [TestMethod]
    public void SummaryWorkflow_IsGone()
    {
        // Terry: "Do not improve the Summarize prose. Save the questions as asked and the
        // answers as typed." A workflow that writes a fresh digest from the transcript has
        // no role left, and registered it would still be reachable.
        Assert.IsFalse(WorkflowRegistry.All.Any(w => w.Label == "CharacterInterviewSummary"));
    }

    [TestMethod]
    public void EachElementType_AppearsInOneContiguousRun()
    {
        // The nav pane opens a new group whenever PrimaryElementType changes as it walks
        // the registry (#129). A Character entry registered after the Scene entries draws
        // a second "Character" header with the interview stranded under it.
        var seen = new List<StoryItemType>();
        StoryItemType? previous = null;

        foreach (var workflow in WorkflowRegistry.All.Where(w => w.ShowInMenu))
        {
            if (previous != null && workflow.PrimaryElementType == previous)
                continue;

            Assert.IsFalse(seen.Contains(workflow.PrimaryElementType),
                $"{workflow.PrimaryElementType} workflows are split into more than one run "
                + $"in the registry; {workflow.Label} would draw a duplicate group header.");
            seen.Add(workflow.PrimaryElementType);
            previous = workflow.PrimaryElementType;
        }
    }

    [TestMethod]
    public void EveryFieldTheBankTargets_ExistsOnCharacterModel()
    {
        // The bank lives on the Worker (ADR-005), so this side cannot check its wording.
        // It can check that every field id the Worker will send back names a real property,
        // which is what makes "every question names a field" mean anything downstream.
        string[] fields =
        {
            "Flaw", "BackStory", "Values", "Enneagram", "Focus", "PsychNotes",
            "Abnormality", "Intelligence", "TraitList", "Role", "StoryRole",
            "Archetype", "Description"
        };

        foreach (var field in fields)
        {
            Assert.IsNotNull(typeof(CharacterModel).GetProperty(field),
                $"The interview bank targets {field}, which is not a property on CharacterModel.");
        }
    }
}
