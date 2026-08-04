using CollaboratorLib.Context;
using StoryCADLib.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #142: Role and Story Role owns Character Sketch (Description).
/// </summary>
[TestClass]
public class RoleAndStoryRoleSketchTests
{
    [TestMethod]
    public void RoleAndStoryRole_Outputs_IncludeDescription()
    {
        var wf = WorkflowRegistry.Get("RoleAndStoryRole");
        Assert.IsNotNull(wf);
        var props = wf!.GetIO().Outputs
            .SelectMany(o => o.PropertiesToUpdate)
            .Select(p => p.Property)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(props.Contains("Role"));
        Assert.IsTrue(props.Contains("StoryRole"));
        Assert.IsTrue(props.Contains("Archetype"));
        Assert.IsTrue(props.Contains("Description"),
            "Character Sketch is Description on Character");
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
