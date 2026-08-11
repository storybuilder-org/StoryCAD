using CollaboratorLib.Context;
using StoryCADLib.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #142 / #182: Story function owns Character Sketch (Description).
/// Occupation Role is DefineCharacter (#182).
/// </summary>
[TestClass]
public class RoleAndStoryRoleSketchTests
{
    [TestMethod]
    public void RoleAndStoryRole_Outputs_IncludeDescription_NotOccupationRole()
    {
        var wf = WorkflowRegistry.Get("RoleAndStoryRole");
        Assert.IsNotNull(wf);
        var props = wf!.GetIO().Outputs
            .SelectMany(o => o.PropertiesToUpdate)
            .Select(p => p.Property)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.IsFalse(props.Contains("Role"), "Occupation Role moved to DefineCharacter (#182)");
        Assert.IsTrue(props.Contains("StoryRole"));
        Assert.IsTrue(props.Contains("Archetype"));
        Assert.IsTrue(props.Contains("Description"),
            "Character Sketch is Description on Character");
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
    public void GapOwnership_CharacterDescription_PointsToRoleAndStoryRole()
    {
        var owners = GapWorkflowOwnership.WorkflowsFor(
            StoryItemType.Character, "Description");
        Assert.AreEqual(1, owners.Count);
        Assert.AreEqual("RoleAndStoryRole", owners[0]);
        Assert.AreEqual("Character Sketch",
            GapWorkflowOwnership.DisplayLabel(StoryItemType.Character, "Description"));
    }
}
