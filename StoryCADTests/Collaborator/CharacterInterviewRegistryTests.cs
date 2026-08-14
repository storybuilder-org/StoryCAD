using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>Collaborator #119: Character Interview registry entries.</summary>
[TestClass]
public class CharacterInterviewRegistryTests
{
    [TestMethod]
    public void CharacterInterview_IsConversational_AndTargetsCharacter()
    {
        var workflow = WorkflowRegistry.All.First(w => w.Label == "CharacterInterview");

        Assert.AreEqual(WorkflowMode.Conversational, workflow.Mode);
        Assert.AreEqual(StoryItemType.Character, workflow.PrimaryElementType);
        Assert.IsTrue(workflow.GetIO().RequiredInputs.Any(r => r.ElementLabel == "Character"));
    }

    [TestMethod]
    public void CharacterInterview_DeclaresOptionalOverviewAndProblem()
    {
        var io = WorkflowRegistry.All.First(w => w.Label == "CharacterInterview").GetIO();
        var labels = io.OptionalInputs.Select(r => r.ElementLabel).ToList();

        CollectionAssert.Contains(labels, "Overview");
        CollectionAssert.Contains(labels, "Problem");
    }

    [TestMethod]
    public void CharacterInterview_ProposesNothingItself()
    {
        // The interview turns return prose. Summarize owns every write.
        var io = WorkflowRegistry.All.First(w => w.Label == "CharacterInterview").GetIO();
        Assert.AreEqual(0, io.Outputs.SelectMany(o => o.PropertiesToUpdate).Count());
    }

    [TestMethod]
    public void Summary_IsOneShot_AndTargetsTheAgreedFields()
    {
        var workflow = WorkflowRegistry.All.First(w => w.Label == "CharacterInterviewSummary");
        var properties = workflow.GetIO().Outputs
            .SelectMany(o => o.PropertiesToUpdate)
            .Select(s => s.Property)
            .ToList();

        Assert.AreEqual(WorkflowMode.OneShot, workflow.Mode);
        CollectionAssert.Contains(properties, "Notes");
        CollectionAssert.Contains(properties, "BackStory");
        CollectionAssert.Contains(properties, "Flaw");
        CollectionAssert.Contains(properties, "Values");
        CollectionAssert.Contains(properties, "PsychNotes");
        CollectionAssert.Contains(properties, "Education");
        CollectionAssert.Contains(properties, "Nationality");
        CollectionAssert.Contains(properties, "Ethnic");
    }

    [TestMethod]
    public void Summary_ExcludesFieldsOwnedElsewhereOrPresentTense()
    {
        // Economic holds present-tense means; Q9 asks about childhood (design: Outputs).
        // Description is the Character Sketch, owned by RoleAndStoryRole.
        var properties = WorkflowRegistry.All
            .First(w => w.Label == "CharacterInterviewSummary")
            .GetIO().Outputs
            .SelectMany(o => o.PropertiesToUpdate)
            .Select(s => s.Property)
            .ToList();

        CollectionAssert.DoesNotContain(properties, "Economic");
        CollectionAssert.DoesNotContain(properties, "Description");
        CollectionAssert.DoesNotContain(properties, "RelationshipList");
    }

    [TestMethod]
    public void Summary_WritesBackToTheProblemItAskedAbout()
    {
        // Terry, 2026-08-12: the interview must inform the rest of the outline. The
        // presupposition questions take a Problem field as their premise, so the answer
        // belongs on that Problem, not only on the Character.
        var outputs = WorkflowRegistry.All
            .First(w => w.Label == "CharacterInterviewSummary")
            .GetIO().Outputs;

        var problem = outputs.FirstOrDefault(o => o.ElementLabel == "Problem");
        Assert.IsNotNull(problem, "Summary declares no Problem output");
        Assert.AreEqual(StoryItemType.Problem, problem!.ElementType);

        var props = problem.PropertiesToUpdate.Select(s => s.Property).ToList();
        CollectionAssert.Contains(props, "ProtMotive");
        CollectionAssert.Contains(props, "AntagMotive");
        CollectionAssert.Contains(props, "ProtConflict");
    }

    [TestMethod]
    public void Summary_TakesTheProblemAsAnOptionalInput()
    {
        // Declared, or the Problem_* placeholders never merge. Optional, because an
        // interview can run on a character with no linked problem at all.
        var io = WorkflowRegistry.All
            .First(w => w.Label == "CharacterInterviewSummary").GetIO();

        Assert.IsTrue(io.OptionalInputs.Any(r => r.ElementLabel == "Problem"));
        Assert.IsFalse(io.RequiredInputs.Any(r => r.ElementLabel == "Problem"));
    }

    [TestMethod]
    public void Summary_IsOffTheMenu()
    {
        // Its only meaningful input is a transcript that exists solely inside an interview
        // session. Picked from the nav pane it would propose a life story from nothing.
        var summary = WorkflowRegistry.All.First(w => w.Label == "CharacterInterviewSummary");
        var interview = WorkflowRegistry.All.First(w => w.Label == "CharacterInterview");

        Assert.IsFalse(summary.ShowInMenu);
        Assert.IsTrue(interview.ShowInMenu);
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
}
