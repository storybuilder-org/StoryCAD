using CollaboratorLib.Context;
using StoryCADLib.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #244: CharacterBuilder owns Character Sketch (Description) and occupation Role.
/// </summary>
[TestClass]
public class StoryFunctionSketchTests
{
    [TestMethod]
    public void CharacterBuilder_Outputs_IncludeDescriptionAndOccupationRole()
    {
        var wf = WorkflowRegistry.Get("CharacterBuilder");
        Assert.IsNotNull(wf);
        Assert.AreEqual("Character Builder", wf!.Title);
        var props = wf.GetIO().Outputs
            .SelectMany(o => o.PropertiesToUpdate)
            .Select(p => p.Property)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(props.Contains("Role"), "Occupation Role is CharacterBuilder (#244)");
        Assert.IsTrue(props.Contains("StoryRole"));
        Assert.IsTrue(props.Contains("Archetype"));
        Assert.IsTrue(props.Contains("Description"),
            "Character Sketch is Description on Character");
        Assert.IsNull(WorkflowRegistry.Get("RoleAndStoryRole"));
        Assert.IsNull(WorkflowRegistry.Get("StoryFunction"));
        Assert.IsNull(WorkflowRegistry.Get("DefineCharacter"));
    }

    [TestMethod]
    public void GapOwnership_CharacterSketchAndStoryRole_PointToCharacterBuilder()
    {
        var desc = GapWorkflowOwnership.WorkflowsFor(
            StoryItemType.Character, "Description");
        Assert.AreEqual(1, desc.Count);
        Assert.AreEqual("CharacterBuilder", desc[0]);
        Assert.AreEqual("Character Sketch",
            GapWorkflowOwnership.DisplayLabel(StoryItemType.Character, "Description"));

        var storyRole = GapWorkflowOwnership.WorkflowsFor(
            StoryItemType.Character, "StoryRole");
        Assert.AreEqual(1, storyRole.Count);
        Assert.AreEqual("CharacterBuilder", storyRole[0]);
    }

    [TestMethod]
    public void GapOwnership_CharacterRole_PointsToCharacterBuilder()
    {
        var owners = GapWorkflowOwnership.WorkflowsFor(
            StoryItemType.Character, "Role");
        Assert.AreEqual(1, owners.Count);
        Assert.AreEqual("CharacterBuilder", owners[0]);
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
