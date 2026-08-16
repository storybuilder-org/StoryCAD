using CollaboratorLib.Context;
using StoryCADLib.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #142 / #183: StoryFunction owns Character Sketch (Description).
/// Occupation Role is DefineCharacter (#182).
/// </summary>
[TestClass]
public class StoryFunctionSketchTests
{
    [TestMethod]
    public void StoryFunction_Outputs_IncludeDescription_NotOccupationRole()
    {
        var wf = WorkflowRegistry.Get("StoryFunction");
        Assert.IsNotNull(wf);
        Assert.AreEqual("Character Story Function", wf!.Title);
        var props = wf.GetIO().Outputs
            .SelectMany(o => o.PropertiesToUpdate)
            .Select(p => p.Property)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.IsFalse(props.Contains("Role"), "Occupation Role is DefineCharacter (#182)");
        Assert.IsTrue(props.Contains("StoryRole"));
        Assert.IsTrue(props.Contains("Archetype"));
        Assert.IsTrue(props.Contains("Description"),
            "Character Sketch is Description on Character");
        Assert.IsNull(WorkflowRegistry.Get("RoleAndStoryRole"));
    }

    [TestMethod]
    public void DefineCharacter_Outputs_IncludeOccupationRole()
    {
        var wf = WorkflowRegistry.Get("DefineCharacter");
        Assert.IsNotNull(wf);
        var props = wf!.GetIO().Outputs
            .SelectMany(o => o.PropertiesToUpdate)
            .Select(p => p.Property)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.IsTrue(props.Contains("Role"));
        Assert.IsFalse(props.Contains("StoryRole"));
        Assert.IsFalse(props.Contains("Description"));
    }

    [TestMethod]
    public void GapOwnership_CharacterSketchAndStoryRole_PointToStoryFunction()
    {
        var desc = GapWorkflowOwnership.WorkflowsFor(
            StoryItemType.Character, "Description");
        Assert.AreEqual(1, desc.Count);
        Assert.AreEqual("StoryFunction", desc[0]);
        Assert.AreEqual("Character Sketch",
            GapWorkflowOwnership.DisplayLabel(StoryItemType.Character, "Description"));

        var storyRole = GapWorkflowOwnership.WorkflowsFor(
            StoryItemType.Character, "StoryRole");
        Assert.AreEqual(1, storyRole.Count);
        Assert.AreEqual("StoryFunction", storyRole[0]);
    }

    [TestMethod]
    public void GapOwnership_CharacterRole_PointsToDefineCharacter()
    {
        var owners = GapWorkflowOwnership.WorkflowsFor(
            StoryItemType.Character, "Role");
        Assert.AreEqual(1, owners.Count);
        Assert.AreEqual("DefineCharacter", owners[0]);
    }

    [TestMethod]
    public void GapOwnership_HasNo_RoleAndStoryRole()
    {
        foreach (var prop in new[] { "Role", "StoryRole", "Description", "Age", "Sex", "Appearance", "BackStory" })
        {
            var owners = GapWorkflowOwnership.WorkflowsFor(StoryItemType.Character, prop);
            CollectionAssert.DoesNotContain(owners.ToList(), "RoleAndStoryRole");
        }
    }
}
