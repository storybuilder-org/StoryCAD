using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;
using StoryCollaborator;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Issue #260 StoryCAD proposal: the "input" request field, one JSON object built for a
///     migrated workflow. Tonight's scope is FlawBackstory only -- character, relatedProblems,
///     lists, storyContext (see devdocs/issue_260_json_input_storycad_proposal.md section 2a).
/// </summary>
[TestClass]
public class WorkflowInputBuilderTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static async Task<StoryCADApi> NewOutline()
    {
        var api = CreateApi();
        var create = await api.CreateEmptyOutline("Input Test", "Author", "0");
        Assert.IsTrue(create.IsSuccess, create.ErrorMessage);
        return api;
    }

    private static OverviewModel Overview(StoryCADApi api) =>
        api.CurrentModel.StoryElements.OfType<OverviewModel>().First();

    private static CharacterModel AddCharacter(StoryCADApi api, string name)
    {
        var add = api.AddElement(StoryItemType.Character, Overview(api).Uuid.ToString(), name);
        Assert.IsTrue(add.IsSuccess, add.ErrorMessage);
        return (CharacterModel)api.GetStoryElement(add.Payload).Payload;
    }

    private static ProblemModel AddProblem(StoryCADApi api, string name)
    {
        var add = api.AddElement(StoryItemType.Problem, Overview(api).Uuid.ToString(), name);
        Assert.IsTrue(add.IsSuccess, add.ErrorMessage);
        return (ProblemModel)api.GetStoryElement(add.Payload).Payload;
    }

    private static WorkflowRunner Runner(StoryCADApi api, string label) =>
        new(api.CurrentModel, WorkflowRegistry.Get(label), api);

    /// <summary>Recursively asserts no string in the JSON tree starts with an RTF header.</summary>
    private static void AssertNoRtf(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var pair in obj)
                    AssertNoRtf(pair.Value);
                break;
            case JsonArray arr:
                foreach (var item in arr)
                    AssertNoRtf(item);
                break;
            case JsonValue val when val.TryGetValue(out string s):
                Assert.IsFalse(s != null && s.StartsWith(@"{\rtf"), $"RTF leaked into the input object: {s}");
                break;
        }
    }

    [TestMethod]
    public async Task FlawBackstory_ProtagonistProblem_RelatedProblemsHoldsResolvedProtagonist()
    {
        var api = await NewOutline();
        var character = AddCharacter(api, "Mira");
        character.Description = "A sketch";
        character.Role = "Detective";
        character.StoryRole = "Protagonist";
        character.Flaw = "Pride";
        character.BackStory = "Grew up poor";

        var problem = AddProblem(api, "The Heist");
        problem.Protagonist = character.Uuid;
        problem.ProtGoal = "Steal the painting";

        var body = Runner(api, "FlawBackstory").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Character"] = character });

        Assert.IsNotNull(body.Input, "FlawBackstory declares a JsonInput property list; input must be built");

        var characterObj = body.Input["character"].AsObject();
        Assert.AreEqual(character.Uuid.ToString(), characterObj["GUID"].GetValue<string>());
        Assert.AreEqual("Mira", characterObj["Name"].GetValue<string>());
        CollectionAssert.AreEquivalent(
            new[] { "GUID", "Name", "Description", "Role", "StoryRole", "Flaw", "BackStory" },
            characterObj.Select(p => p.Key).ToList());

        var related = body.Input["relatedProblems"].AsArray();
        Assert.AreEqual(1, related.Count);
        var problemObj = related[0].AsObject();
        Assert.AreEqual(problem.Uuid.ToString(), problemObj["GUID"].GetValue<string>());

        var protagonistCharacter = problemObj["protagonistCharacter"].AsObject();
        Assert.AreEqual(character.Uuid.ToString(), protagonistCharacter["GUID"].GetValue<string>());
        Assert.AreEqual("Mira", protagonistCharacter["Name"].GetValue<string>());
        Assert.AreEqual(2, protagonistCharacter.Count, "a resolved character carries GUID and Name only");

        CollectionAssert.AreEquivalent(
            new[]
            {
                "GUID", "Name", "ProblemCategory", "ProblemType", "ConflictType",
                "Protagonist", "ProtGoal", "ProtMotive", "ProtConflict",
                "Antagonist", "AntagGoal", "AntagMotive", "AntagConflict",
                "Premise", "Outcome", "Theme", "Notes",
                "protagonistCharacter", "antagonistCharacter"
            },
            problemObj.Select(p => p.Key).ToList(),
            "no property outside the registry lists may appear");
    }

    [TestMethod]
    public async Task FlawBackstory_PersonVsSelf_BothResolvedKeysAreSameCharacter()
    {
        var api = await NewOutline();
        var character = AddCharacter(api, "Solo");
        var problem = AddProblem(api, "Inner Struggle");
        problem.Protagonist = character.Uuid;
        problem.Antagonist = character.Uuid;
        problem.ConflictType = "Person vs. Self";

        var body = Runner(api, "FlawBackstory").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Character"] = character });

        var problemObj = body.Input["relatedProblems"].AsArray()[0].AsObject();
        var prot = problemObj["protagonistCharacter"].AsObject();
        var antag = problemObj["antagonistCharacter"].AsObject();

        Assert.AreEqual(character.Uuid.ToString(), prot["GUID"].GetValue<string>());
        Assert.AreEqual(character.Uuid.ToString(), antag["GUID"].GetValue<string>());
        Assert.AreEqual("Solo", prot["Name"].GetValue<string>());
        Assert.AreEqual("Solo", antag["Name"].GetValue<string>());
    }

    [TestMethod]
    public async Task FlawBackstory_ProblemWithNoAntagonist_AntagonistCharacterIsJsonNull()
    {
        var api = await NewOutline();
        var character = AddCharacter(api, "Lone");
        var problem = AddProblem(api, "Solo Problem");
        problem.Protagonist = character.Uuid;
        // Antagonist left at Guid.Empty -- never set.

        var body = Runner(api, "FlawBackstory").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Character"] = character });

        var problemObj = body.Input["relatedProblems"].AsArray()[0].AsObject();
        Assert.IsTrue(problemObj.ContainsKey("antagonistCharacter"), "the key must be present");
        Assert.IsNull(problemObj["antagonistCharacter"], "an unresolved reference is JSON null, never an absent key");
    }

    [TestMethod]
    public async Task FlawBackstory_RtfInDescriptionAndProblemField_IsStrippedEverywhere()
    {
        var api = await NewOutline();
        var character = AddCharacter(api, "Ray");
        character.Description = @"{\rtf1\ansi Plain description}";

        var problem = AddProblem(api, "The Case");
        problem.Protagonist = character.Uuid;
        problem.ProtGoal = @"{\rtf1\ansi Steal the painting}";

        var body = Runner(api, "FlawBackstory").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Character"] = character });

        AssertNoRtf(body.Input);
        StringAssert.Contains(body.Input.ToJsonString(), "Plain description");
        StringAssert.Contains(body.Input.ToJsonString(), "Steal the painting");
    }

    [TestMethod]
    public async Task FlawBackstory_InputObject_ParsesAndHasExactlyFourTopLevelKeys()
    {
        var api = await NewOutline();
        var character = AddCharacter(api, "Nell");

        var body = Runner(api, "FlawBackstory").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Character"] = character });

        Assert.IsNotNull(body.Input);
        using var doc = JsonDocument.Parse(body.Input.ToJsonString());
        Assert.AreEqual(JsonValueKind.Object, doc.RootElement.ValueKind);

        CollectionAssert.AreEquivalent(
            new[] { "character", "relatedProblems", "lists", "storyContext" },
            body.Input.Select(p => p.Key).ToList());
    }

    [TestMethod]
    public async Task CharacterBuilder_NoJsonInputDeclared_GetsNoInputField()
    {
        var api = await NewOutline();
        var character = AddCharacter(api, "Anyone");

        var body = Runner(api, "CharacterBuilder").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Character"] = character });

        Assert.IsNull(body.Input, "a workflow without a JsonInput property list gets no input field");
    }

    [TestMethod]
    public void BuildProxyPayload_BodyInputNull_OmitsInputKey()
    {
        var body = new WorkflowProxyBody();

        var payload = WorkflowRunner.BuildProxyPayload("CharacterBuilder", body, false);

        using var doc = JsonDocument.Parse(payload);
        Assert.IsFalse(doc.RootElement.TryGetProperty("input", out _),
            "a workflow with no input built must omit the wire key entirely");
    }

    [TestMethod]
    public void BuildProxyPayload_BodyInputSet_IncludesInputKey()
    {
        var body = new WorkflowProxyBody { Input = new JsonObject { ["character"] = "x" } };

        var payload = WorkflowRunner.BuildProxyPayload("FlawBackstory", body, false);

        using var doc = JsonDocument.Parse(payload);
        Assert.IsTrue(doc.RootElement.TryGetProperty("input", out var input));
        Assert.AreEqual("x", input.GetProperty("character").GetString());
    }
}
