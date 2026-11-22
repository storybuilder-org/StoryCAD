using CollaboratorLib.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #261: workflows, labels and the group label for the Setting and Story World
/// fields.
/// </summary>
[TestClass]
public class GapWorkflowOwnershipTests
{
    [TestMethod]
    [DataRow(StoryItemType.Setting, "Locale", "SettingBuilder")]
    [DataRow(StoryItemType.Setting, "Period", "SettingBuilder")]
    [DataRow(StoryItemType.StoryWorld, "WorldType", "DefineStoryWorld")]
    [DataRow(StoryItemType.StoryWorld, "Description", "DefineStoryWorld")]
    public void WorkflowsFor_NewField_GivesWorkflow(StoryItemType type, string property, string workflow)
    {
        CollectionAssert.AreEqual(new[] { workflow }, GapWorkflowOwnership.WorkflowsFor(type, property).ToList());
    }

    [TestMethod]
    [DataRow(StoryItemType.Setting, "Locale", "Locale")]
    [DataRow(StoryItemType.Setting, "Period", "Period")]
    [DataRow(StoryItemType.StoryWorld, "WorldType", "World Type")]
    [DataRow(StoryItemType.StoryWorld, "Description", "World tell")]
    public void DisplayLabel_NewField_GivesLabel(StoryItemType type, string property, string label)
    {
        Assert.AreEqual(label, GapWorkflowOwnership.DisplayLabel(type, property));
    }

    [TestMethod]
    public void GroupLabel_StoryWorld_GivesStoryWorldWithSpace()
    {
        Assert.AreEqual("Story World", GapWorkflowOwnership.GroupLabel(StoryItemType.StoryWorld));
    }

    [TestMethod]
    public void GroupLabel_Setting_GivesTypeName()
    {
        Assert.AreEqual("Setting", GapWorkflowOwnership.GroupLabel(StoryItemType.Setting));
    }
}
