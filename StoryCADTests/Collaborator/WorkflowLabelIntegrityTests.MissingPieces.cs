using CollaboratorLib.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #261: the Story World fields and the missing pieces name registered workflows.
/// </summary>
public partial class WorkflowLabelIntegrityTests
{
    [TestMethod]
    public void WorkflowsFor_StoryWorldFields_NamesRegisteredWorkflows()
    {
        foreach (var property in new[] { "WorldType", "Description" })
        {
            foreach (var label in GapWorkflowOwnership.WorkflowsFor(StoryItemType.StoryWorld, property))
                Assert.IsNotNull(WorkflowRegistry.Get(label), $"StoryWorld.{property} names '{label}'.");
        }
    }

    [TestMethod]
    public async Task FindMissingPieces_Labels_AreRegisteredWorkflows()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("Label integrity 261", "Author", "0");
        var overview = api.CurrentModel!.StoryElements.OfType<OverviewModel>().First();
        api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "Crown");
        api.AddElement(StoryItemType.Character, overview.Uuid.ToString(), "Banquo");

        var labels = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!).Select(p => p.WorkflowLabel).Distinct().ToList();

        CollectionAssert.AreEquivalent(new[] { "ProblemBeats", "ProblemBuilder" }, labels);
        foreach (var label in labels)
            Assert.IsNotNull(WorkflowRegistry.Get(label), $"missing piece names '{label}'.");
    }
}
