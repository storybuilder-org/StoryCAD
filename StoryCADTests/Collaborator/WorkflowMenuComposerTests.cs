using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Band ordering for the Collaborator navigation pane: starred workflows in a short band at the
/// top, the rest in collapsed element-type groups.
/// </summary>
[TestClass]
public class WorkflowMenuComposerTests
{
    private static string GroupTitle(StoryItemType type) => type.ToString();

    /// <summary>Two Overview workflows, two Problem workflows, one Character workflow.</summary>
    private static List<Workflow> SampleWorkflows() => new()
    {
        new Workflow("Alpha", "Alpha Title", "Alpha description", StoryItemType.StoryOverview),
        new Workflow("Beta", "Beta Title", "Beta description", StoryItemType.StoryOverview),
        new Workflow("Gamma", "Gamma Title", "Gamma description", StoryItemType.Problem),
        new Workflow("Delta", "Delta Title", "Delta description", StoryItemType.Problem),
        new Workflow("Epsilon", "Epsilon Title", "Epsilon description", StoryItemType.Character)
    };

    [TestMethod]
    public void Compose_WithStarredLabels_PutsStarredBandFirst()
    {
        var bands = WorkflowMenuComposer.Compose(
            SampleWorkflows(), new[] { "Gamma", "Alpha" }, GroupTitle);

        Assert.AreEqual(WorkflowMenuComposer.StarredBandTitle, bands[0].Title);
        Assert.IsTrue(bands[0].IsExpanded, "Starred band must open; it is the point of the pane.");
        // Registry order, not the order the user starred them.
        CollectionAssert.AreEqual(
            new[] { "Alpha", "Gamma" },
            bands[0].Items.Select(i => i.Label).ToArray());
        Assert.IsTrue(bands[0].Items.All(i => i.IsStarred));
    }

    [TestMethod]
    public void Compose_WithStarredLabels_OmitsStarredFromTypeGroups()
    {
        var bands = WorkflowMenuComposer.Compose(
            SampleWorkflows(), new[] { "Gamma", "Alpha" }, GroupTitle);

        // Two rows on one registry instance would collide in RestoreSelection, which matches by tag.
        var groupLabels = bands.Skip(1).SelectMany(b => b.Items).Select(i => i.Label).ToList();
        CollectionAssert.DoesNotContain(groupLabels, "Alpha");
        CollectionAssert.DoesNotContain(groupLabels, "Gamma");
        CollectionAssert.AreEquivalent(new[] { "Beta", "Delta", "Epsilon" }, groupLabels);
    }

    [TestMethod]
    public void Compose_WithStarredLabels_CollapsesTypeGroups()
    {
        var bands = WorkflowMenuComposer.Compose(
            SampleWorkflows(), new[] { "Alpha" }, GroupTitle);

        Assert.IsTrue(bands.Skip(1).All(b => !b.IsExpanded),
            "Catalog groups must start closed or the pane is still the full table.");
    }

    [TestMethod]
    public void Compose_WhenEveryWorkflowOfATypeIsStarred_SkipsTheEmptyGroup()
    {
        var bands = WorkflowMenuComposer.Compose(
            SampleWorkflows(), new[] { "Epsilon" }, GroupTitle);

        Assert.IsFalse(bands.Any(b => b.Title == StoryItemType.Character.ToString()),
            "A group whose only workflow is starred must not render as an empty header.");
    }

    [TestMethod]
    public void Compose_WithNoStars_OmitsStarredBand()
    {
        var bands = WorkflowMenuComposer.Compose(
            SampleWorkflows(), new List<string>(), GroupTitle);

        Assert.AreNotEqual(WorkflowMenuComposer.StarredBandTitle, bands[0].Title);
        Assert.AreEqual(3, bands.Count, "Overview, Problem and Character groups only.");
        Assert.AreEqual(5, bands.SelectMany(b => b.Items).Count());
    }

    [TestMethod]
    public void Compose_WithUnknownStarredLabel_IgnoresIt()
    {
        // A workflow withdrawn from the registry keeps its star in preferences; it must not
        // put a dead row in the pane.
        var bands = WorkflowMenuComposer.Compose(
            SampleWorkflows(), new[] { "Alpha", "WorkflowThatNoLongerExists" }, GroupTitle);

        Assert.AreEqual(1, bands[0].Items.Count);
        Assert.AreEqual("Alpha", bands[0].Items[0].Label);
    }

    [TestMethod]
    public void Compose_WithNullStarredLabels_ReturnsGroupsOnly()
    {
        var bands = WorkflowMenuComposer.Compose(SampleWorkflows(), null, GroupTitle);

        Assert.AreEqual(3, bands.Count);
        Assert.AreEqual(5, bands.SelectMany(b => b.Items).Count());
    }

    [TestMethod]
    public void Compose_AgainstTheRealRegistry_StarsEveryDefaultLabel()
    {
        var bands = WorkflowMenuComposer.Compose(
            WorkflowRegistry.All, WorkflowRegistry.DefaultStarredLabels, GroupTitle);

        Assert.AreEqual(WorkflowMenuComposer.StarredBandTitle, bands[0].Title);
        Assert.AreEqual(WorkflowRegistry.DefaultStarredLabels.Count, bands[0].Items.Count);

        // Nothing is lost or duplicated between the band and the groups.
        var rendered = bands.SelectMany(b => b.Items).Select(i => i.Label).ToList();
        Assert.AreEqual(WorkflowRegistry.All.Count, rendered.Count);
        Assert.AreEqual(rendered.Count, rendered.Distinct().Count());
    }
}
