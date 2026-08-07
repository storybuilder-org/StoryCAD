using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>Collaborator #167: Structure collections and outputs.</summary>
[TestClass]
public class StructureWorkflowRegistryTests
{
    [TestMethod]
    public void Structure_Declares_Problem_And_Scene_Collections()
    {
        var workflow = WorkflowRegistry.All.First(w => w.Label == "Structure");
        var io = workflow.GetIO();

        Assert.AreEqual(StoryItemType.Problem, workflow.PrimaryElementType);
        Assert.IsTrue(io.RequiredInputs.Any(r => r.ElementLabel == "Problem"));

        var problemChoices = io.CollectionInputs.FirstOrDefault(c => c.RequestName == "ProblemChoices");
        var sceneChoices = io.CollectionInputs.FirstOrDefault(c => c.RequestName == "SceneChoices");
        Assert.IsNotNull(problemChoices);
        Assert.IsNotNull(sceneChoices);
        Assert.AreEqual(StoryItemType.Problem, problemChoices!.ElementType);
        Assert.AreEqual(StoryItemType.Scene, sceneChoices!.ElementType);
        Assert.AreEqual(ElementProjection.BaseStoryElement, problemChoices.Projection);
        Assert.AreEqual(ElementProjection.BaseStoryElement, sceneChoices.Projection);
    }

    [TestMethod]
    public void Structure_Outputs_Include_BeatSheet()
    {
        var workflow = WorkflowRegistry.All.First(w => w.Label == "Structure");
        var specs = workflow.GetIO().Outputs
            .SelectMany(o => o.PropertiesToUpdate)
            .ToList();

        Assert.IsTrue(specs.Any(s => s.Property == "StructureTitle"));
        Assert.IsTrue(specs.Any(s => s.Property == "StructureDescription"));
        var beats = specs.First(s => s.Property == "StructureBeats");
        Assert.AreEqual(WriteVia.BeatSheet, beats.WriteVia);
        Assert.AreEqual("beats", beats.JsonKey);
    }
}
