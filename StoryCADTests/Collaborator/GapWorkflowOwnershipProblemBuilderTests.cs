using CollaboratorLib.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #77 step 12. The gap report tells the user which workflow closes a gap.
///     ProblemBuilder fills the Problem spine, so it must be named. GMC and StoryProblem stay
///     listed while they remain registered for A:B.
/// </summary>
[TestClass]
public class GapWorkflowOwnershipProblemBuilderTests
{
    [TestMethod]
    public void GmcFields_NameProblemBuilderFirst()
    {
        foreach (var property in new[]
                 {
                     "ProtGoal", "ProtMotive", "ProtConflict",
                     "AntagGoal", "AntagMotive", "AntagConflict", "Outcome"
                 })
        {
            var owners = GapWorkflowOwnership.WorkflowsFor(StoryItemType.Problem, property);

            CollectionAssert.Contains(owners.ToList(), "ProblemBuilder",
                $"'{property}' is filled by ProblemBuilder after the merge");
            Assert.AreEqual("ProblemBuilder", owners.First(),
                $"'{property}' should offer ProblemBuilder before the workflow it replaces");
        }
    }

    [TestMethod]
    public void StoryProblemSpineFields_AlsoNameProblemBuilder()
    {
        foreach (var property in new[]
                 {
                     "ProblemType", "ConflictType", "Subject", "Premise", "Description"
                 })
        {
            CollectionAssert.Contains(
                GapWorkflowOwnership.WorkflowsFor(StoryItemType.Problem, property).ToList(),
                "ProblemBuilder",
                $"'{property}' is filled by ProblemBuilder after the merge");
        }
    }

    [TestMethod]
    public void PreconditionFields_DoNotNameProblemBuilder()
    {
        foreach (var property in new[] { "ProblemCategory", "Protagonist", "Antagonist" })
        {
            CollectionAssert.DoesNotContain(
                GapWorkflowOwnership.WorkflowsFor(StoryItemType.Problem, property).ToList(),
                "ProblemBuilder",
                $"'{property}' is a ProblemBuilder precondition; it cannot close its own gap");
        }
    }

    [TestMethod]
    public void ReplacedWorkflows_StayListedForAbTesting()
    {
        CollectionAssert.Contains(
            GapWorkflowOwnership.WorkflowsFor(StoryItemType.Problem, "ProtGoal").ToList(), "GMC",
            "GMC stays registered until the cleanup issue retires it");
    }
}
