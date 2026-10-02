using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCollaborator;
using StoryCollaborator.Models;
using StoryCollaborator.Services;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Tests of <see cref="WorkflowRunner"/>. The first covers the outgoing-request half of the
///     PR #1470 review item (issue #89 omission):
///     the credential resolved by <c>KernelFactory.ResolveWorkflowCredential</c> is the value
///     that leaves the machine in the Authorization header of every /workflow POST.
/// </summary>
[TestClass]
public class WorkflowRunnerTests
{
    [TestMethod]
    public void CreateWorkflowRequest_WithCredential_SetsBearerAuthorizationHeader()
    {
        using var request = WorkflowRunner.CreateWorkflowRequest(
            "https://proxy.example/v1", "activation-jwt", "{\"workflowId\":\"Tone\"}");

        Assert.AreEqual("Bearer", request.Headers.Authorization.Scheme);
        Assert.AreEqual("activation-jwt", request.Headers.Authorization.Parameter,
            "the resolved credential must ride the Authorization header of the workflow call");
        Assert.AreEqual("https://proxy.example/v1/workflow", request.RequestUri.ToString());
        Assert.AreEqual("application/json", request.Content.Headers.ContentType.MediaType);
    }

    /// <summary>
    ///     Collaborator #273: Accept applies a chat-renamed Problem Create row with the new name.
    ///     Before the fix the patch wrote SceneName, the row then named both stub kinds, and the
    ///     plan refused it, so Accept created nothing.
    /// </summary>
    [TestMethod]
    public void ApplyUpdates_PatchedProblemCreateRow_CreatesProblemWithPatchedName()
    {
        var model = new StoryModel();
        var parent = new ProblemModel("Story Problem", model, null);
        var api = new StoryCADApi(
            Ioc.Default.GetRequiredService<OutlineService>(),
            Ioc.Default.GetRequiredService<ListData>(),
            Ioc.Default.GetRequiredService<ControlData>(),
            Ioc.Default.GetRequiredService<ToolsData>());
        api.CurrentModel = model;
        var workflow = new Workflow("ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem)
        {
            CreatesScenesForBeats = true,
            CreatesProblemsForBeats = true
        };
        var runner = new WorkflowRunner(model, workflow, api);
        var proposal = new List<BeatInfo>
        {
            new("Midpoint", "the nested fight",
                ProblemName: "The Yellow Brick Road", ProblemCategory: "Sequence")
        };
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(new PendingUpdate("Problem", parent.Uuid,
            new PropertySpec("StructureBeats", WriteVia.BeatSheet, JsonKey: "beats"), proposal));
        runner.ExpandBeatSheetUpdates(result);
        var set = new SessionProposalSet();
        set.ReplaceFromPending(result.PendingUpdates, null);
        Assert.IsTrue(set.TryApplyPatch("Problem.StructureBeats[00]", "The Road to Oz", out _));

        var slice = WorkflowResult.Succeeded();
        slice.PendingUpdates.Add(set.Get("Problem.StructureBeats[00]").Update);
        var applied = runner.ApplyUpdates(slice, new Dictionary<string, StoryElement>());

        Assert.AreEqual(1, applied);
        var created = model.StoryElements.OfType<ProblemModel>().Single(p => p.Uuid != parent.Uuid);
        Assert.AreEqual("The Road to Oz", created.Name);
        Assert.AreEqual(created.Uuid, parent.StructureBeats[0].Guid);
        Assert.AreEqual(0, model.StoryElements.OfType<SceneModel>().Count(), "no Scene stub");
    }

    // ---- Collaborator #251: the Relationship workflow adds an inverse row, not a copy ----

    private static (StoryModel Model, WorkflowRunner Runner, CharacterModel Character, CharacterModel Partner)
        ArrangeRelationship()
    {
        var model = new StoryModel();
        var api = new StoryCADApi(
            Ioc.Default.GetRequiredService<OutlineService>(),
            Ioc.Default.GetRequiredService<ListData>(),
            Ioc.Default.GetRequiredService<ControlData>(),
            Ioc.Default.GetRequiredService<ToolsData>());
        api.CurrentModel = model;
        var workflow = new Workflow("Relationship", "Relationship", "test", StoryItemType.Character);
        var runner = new WorkflowRunner(model, workflow, api);
        var character = new CharacterModel("Sarah Osborne", model, null);
        var partner = new CharacterModel("Irene Campbell", model, null);
        return (model, runner, character, partner);
    }

    private static List<RelationshipInfo> ParseOne(string json)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var results = new List<RelationshipInfo>();
        WorkflowRunner.ParseRelationshipEntry(doc.RootElement, results);
        return results;
    }

    private static WorkflowResult RelationshipResult(CharacterModel character, List<RelationshipInfo> rows)
    {
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(new PendingUpdate("Character", character.Uuid,
            new PropertySpec("RelationshipList", WriteVia.Relationships, JsonKey: "relationship"), rows));
        return result;
    }

    [TestMethod]
    public void ParseRelationshipEntry_WithInverseRelationType_HoldsTheValue()
    {
        var partner = Guid.NewGuid();
        var rows = ParseOne($$"""
            {"recipient_guid":"{{partner}}","RelationType":"Captor","InverseRelationType":"Captive","Trait":"t","Attitude":"a","Notes":"n"}
            """);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("Captive", rows[0].InverseRelationType);
        Assert.AreEqual("Captor", rows[0].RelationType);
    }

    [TestMethod]
    public void ParseRelationshipEntry_WithMirrorTrue_KeepsNoMirrorValue()
    {
        var partner = Guid.NewGuid();
        var rows = ParseOne($$"""
            {"recipient_guid":"{{partner}}","RelationType":"Captor","mirror":true,"Notes":"n"}
            """);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(partner, rows[0].RecipientGuid);
        Assert.AreEqual("Captor", rows[0].RelationType);
        Assert.AreEqual("n", rows[0].Notes);
        Assert.AreEqual("", rows[0].InverseRelationType);
        Assert.IsFalse(typeof(RelationshipInfo).GetProperties().Any(p =>
                p.Name.Contains("mirror", StringComparison.OrdinalIgnoreCase)),
            "no property holds the mirror value");
    }

    [TestMethod]
    public void ApplyUpdates_MirrorTrueAndEmptyInverseRelationType_PartnerHasNoRow()
    {
        var (_, runner, character, partner) = ArrangeRelationship();
        var rows = ParseOne($$"""
            {"recipient_guid":"{{partner.Uuid}}","RelationType":"Captor","mirror":true,"Trait":"t","Attitude":"a","Notes":"n"}
            """);

        runner.ApplyUpdates(RelationshipResult(character, rows), new Dictionary<string, StoryElement>());

        Assert.AreEqual(1, character.RelationshipList.Count);
        Assert.AreEqual(0, partner.RelationshipList.Count);
    }

    [TestMethod]
    public void ApplyUpdates_RelationshipWithInverseRelationType_AddsFullRowAndInverseRow()
    {
        var (_, runner, character, partner) = ArrangeRelationship();
        var rows = new List<RelationshipInfo>
        {
            new(partner.Uuid, "Captor", InverseRelationType: "Captive",
                Trait: "wary", Attitude: "cold", Notes: "Sarah holds Irene")
        };

        runner.ApplyUpdates(RelationshipResult(character, rows), new Dictionary<string, StoryElement>());

        var full = character.RelationshipList.Single();
        Assert.AreEqual(partner.Uuid, full.PartnerUuid);
        Assert.AreEqual("Captor", full.RelationType);
        Assert.AreEqual("wary", full.Trait);
        Assert.AreEqual("cold", full.Attitude);
        Assert.AreEqual("Sarah holds Irene", full.Notes);
        var inverse = partner.RelationshipList.Single();
        Assert.AreEqual(character.Uuid, inverse.PartnerUuid);
        Assert.AreEqual("Captive", inverse.RelationType);
        Assert.AreEqual("", inverse.Trait);
        Assert.AreEqual("", inverse.Attitude);
        Assert.AreEqual("", inverse.Notes);
    }

    [TestMethod]
    public void ApplyUpdates_PartnerAlreadyHasRowForCharacter_LeavesThatRowUnchanged()
    {
        var (_, runner, character, partner) = ArrangeRelationship();
        partner.RelationshipList.Add(new RelationshipModel(character.Uuid, "Captive")
        {
            Trait = "keeps", Attitude = "afraid", Notes = "Irene wrote this"
        });
        var rows = ParseOne($$"""
            {"recipient_guid":"{{partner.Uuid}}","RelationType":"Captor","InverseRelationType":"Prisoner","mirror":true,"Notes":"n"}
            """);

        runner.ApplyUpdates(RelationshipResult(character, rows), new Dictionary<string, StoryElement>());

        Assert.AreEqual(1, character.RelationshipList.Count);
        var kept = partner.RelationshipList.Single();
        Assert.AreEqual("Captive", kept.RelationType);
        Assert.AreEqual("keeps", kept.Trait);
        Assert.AreEqual("afraid", kept.Attitude);
        Assert.AreEqual("Irene wrote this", kept.Notes);
    }

    [TestMethod]
    public void ApplyUpdates_CharacterAlreadyHasRowForPartner_AddsNoRowAndSaysSo()
    {
        var (_, runner, character, partner) = ArrangeRelationship();
        character.RelationshipList.Add(new RelationshipModel(partner.Uuid, "Rival") { Notes = "first" });
        var rows = new List<RelationshipInfo>
        {
            new(partner.Uuid, "Captor", InverseRelationType: "Captive", Notes: "second")
        };
        var result = RelationshipResult(character, rows);

        runner.ApplyUpdates(result, new Dictionary<string, StoryElement>());

        var only = character.RelationshipList.Single();
        Assert.AreEqual("Rival", only.RelationType);
        Assert.AreEqual("first", only.Notes);
        Assert.AreEqual(0, partner.RelationshipList.Count, "no inverse row when no row was added");
        CollectionAssert.Contains(result.StatusMessages,
            "Relationships: Sarah Osborne already has a relationship with Irene Campbell; change it on the Relationships tab.");
    }

    /// <summary>Collaborator #251: a relationship update on an element that is not a Character writes nothing.</summary>
    [TestMethod]
    public void ApplyUpdates_ElementIsNotACharacter_AddsNoRowAndSaysSo()
    {
        var (model, runner, _, partner) = ArrangeRelationship();
        var problem = new ProblemModel("Not a person", model, null);
        var rows = ParseOne($$"""
            {"recipient_guid":"{{partner.Uuid}}","RelationType":"Captor","InverseRelationType":"Captive","Notes":"n"}
            """);
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(new PendingUpdate("Character", problem.Uuid,
            new PropertySpec("RelationshipList", WriteVia.Relationships, JsonKey: "relationship"), rows));

        runner.ApplyUpdates(result, new Dictionary<string, StoryElement>());

        Assert.AreEqual(0, partner.RelationshipList.Count);
        StringAssert.Contains(string.Join(" | ", result.StatusMessages), "is not a character");
    }

    private static (WorkflowRunner Runner, ProblemModel Problem) ArrangeProblem(string protGoal)
    {
        var (model, runner, _, _) = ArrangeRelationship();
        var problem = new ProblemModel("Dealing with a problem tourist", model, null) { ProtGoal = protGoal };
        return (runner, problem);
    }

    private static WorkflowResult GoalResult(ProblemModel problem, string proposed, OutputFieldState? state)
    {
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(new PendingUpdate("Problem", problem.Uuid, new PropertySpec("ProtGoal"), proposed));
        if (state.HasValue)
            result.FieldStates["ProtGoal"] = state.Value;
        return result;
    }

    /// <summary>Collaborator #272: the model kept the writer's filled value; the summary names it.</summary>
    [TestMethod]
    public void ClassifyScalarUpdates_UnchangedOnFilledProperty_IsKeptNotPending()
    {
        var (runner, problem) = ArrangeProblem("Getting Francis happy despite the failed outing");
        var result = GoalResult(problem, "Getting Francis happy despite the failed outing", OutputFieldState.Unchanged);

        runner.ClassifyScalarUpdates(result, null, "ProblemBuilder");

        Assert.AreEqual(0, result.PendingUpdates.Count);
        CollectionAssert.AreEqual(new[] { "ProtGoal" }, result.KeptProperties);
    }

    [TestMethod]
    public void ClassifyScalarUpdates_ProposalEqualToFilledValue_IsKept()
    {
        var (runner, problem) = ArrangeProblem("Getting Francis happy despite the failed outing");
        var result = GoalResult(problem, "Getting Francis happy despite the failed outing", null);

        runner.ClassifyScalarUpdates(result, null, "ProblemBuilder");

        CollectionAssert.AreEqual(new[] { "ProtGoal" }, result.KeptProperties);
    }

    [TestMethod]
    public void ClassifyScalarUpdates_EmptyProposalOnFilledProperty_IsNotKept()
    {
        var (runner, problem) = ArrangeProblem("Getting Francis happy despite the failed outing");
        var result = GoalResult(problem, "", null);

        runner.ClassifyScalarUpdates(result, null, "ProblemBuilder");

        Assert.AreEqual(0, result.PendingUpdates.Count, "the wipe guard still drops the row");
        Assert.AreEqual(0, result.KeptProperties.Count, "the model did not keep the value");
    }

    [TestMethod]
    public void ClassifyScalarUpdates_UnchangedOnEmptyPropertyWithEmptyProposal_IsNotKept()
    {
        var (runner, problem) = ArrangeProblem("");
        var result = GoalResult(problem, "", OutputFieldState.Unchanged);

        runner.ClassifyScalarUpdates(result, null, "ProblemBuilder");

        Assert.AreEqual(0, result.KeptProperties.Count);
    }
}
