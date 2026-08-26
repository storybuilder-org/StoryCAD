using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>Collaborator #208: SceneBuilder registry shape. Five Scene micro-workflows stay.</summary>
[TestClass]
public class SceneBuilderWorkflowRegistryTests
{
    [TestMethod]
    public void SceneBuilder_Registry_IsScenePrimary_WithEmptyOptionalInputs()
    {
        var workflow = WorkflowRegistry.Get("SceneBuilder");
        Assert.IsNotNull(workflow);
        Assert.AreEqual(StoryItemType.Scene, workflow!.PrimaryElementType);

        var io = workflow.GetIO();
        var scene = io.RequiredInputs.Single(r => r.ElementLabel == "Scene");
        Assert.AreEqual(StoryItemType.Scene, scene.ElementType);
        Assert.IsFalse(scene.CreateIfMissing);
        Assert.AreEqual(0, io.OptionalInputs.Count);

        var choices = io.CollectionInputs.Single(c => c.RequestName == "CharacterChoices");
        Assert.AreEqual(StoryItemType.Character, choices.ElementType);
        Assert.AreEqual(ElementProjection.IdAndName, choices.Projection);
        Assert.IsFalse(io.CollectionInputs.Any(c => c.RequestName == "ProblemChoices"));
    }

    [TestMethod]
    public void SceneBuilder_Outputs_IncludeSceneTypeAndCast_OmitImages()
    {
        var io = WorkflowRegistry.Get("SceneBuilder")!.GetIO();
        var props = io.Outputs.Single(o => o.ElementLabel == "Scene").PropertiesToUpdate;
        var names = props.Select(p => p.Property).ToHashSet();

        Assert.IsTrue(names.Contains("SceneType"));
        Assert.IsTrue(names.Contains("CastMembers"));
        Assert.IsTrue(names.Contains("Description"));
        Assert.IsTrue(names.Contains("Notes"));
        Assert.IsFalse(names.Contains("Images"));

        var cast = props.Single(p => p.Property == "CastMembers");
        Assert.AreEqual(WriteVia.CastMembers, cast.WriteVia);
        Assert.AreEqual("cast", cast.JsonKey);

        var purpose = props.Single(p => p.Property == "ScenePurpose");
        Assert.AreEqual(WriteVia.SimpleList, purpose.WriteVia);
    }

    [TestMethod]
    public void FiveSceneMicroWorkflows_AreGone()
    {
        // #211: they stayed registered for A:B against SceneBuilder. That comparison is over.
        foreach (var label in new[] { "SceneSummary", "CastSceneRoles", "SceneDevelopment", "SceneConflict", "Sequel" })
            Assert.IsNull(WorkflowRegistry.Get(label), label);
    }

    [TestMethod]
    public void DefaultStarredLabels_IncludeSceneBuilder_WithoutTheMicroWorkflows()
    {
        var starred = WorkflowRegistry.DefaultStarredLabels.ToList();
        CollectionAssert.Contains(starred, "SceneBuilder");
        CollectionAssert.DoesNotContain(starred, "SceneSummary");
        CollectionAssert.DoesNotContain(starred, "SceneConflict");
    }
}
