using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
/// The default starred set is written as bare label strings, so a renamed or withdrawn workflow
/// would silently produce an empty band rather than a compile error.
/// </summary>
[TestClass]
public class WorkflowStarDefaultsTests
{
    [TestMethod]
    public void DefaultStarredLabels_AllResolveToRegistryWorkflows()
    {
        foreach (var label in WorkflowRegistry.DefaultStarredLabels)
        {
            Assert.IsNotNull(WorkflowRegistry.Get(label),
                $"Default starred label '{label}' does not match any registry workflow.");
        }
    }

    [TestMethod]
    public void DefaultStarredLabels_AreDistinct()
    {
        var labels = WorkflowRegistry.DefaultStarredLabels.ToList();
        Assert.AreEqual(labels.Count, labels.Distinct().Count());
    }

    [TestMethod]
    public void DefaultStarredLabels_StayShortEnoughToBeATable()
    {
        // The whole point is a short first surface. If this grows, the band is a catalog again.
        Assert.IsTrue(WorkflowRegistry.DefaultStarredLabels.Count is > 0 and <= 8,
            $"Expected a short default set, found {WorkflowRegistry.DefaultStarredLabels.Count}.");
    }
}
