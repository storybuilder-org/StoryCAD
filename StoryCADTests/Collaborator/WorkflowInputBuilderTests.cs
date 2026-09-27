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
///     migrated workflow. FlawBackstory and CharacterBuilder share the character,
///     relatedProblems, lists, storyContext shape (see
///     devdocs/issue_260_json_input_storycad_proposal.md section 2a). SettingBuilder has its
///     own setting, relatedScenes, lists, storyContext shape.
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

    private static SettingModel AddSetting(StoryCADApi api, string name)
    {
        var add = api.AddElement(StoryItemType.Setting, Overview(api).Uuid.ToString(), name);
        Assert.IsTrue(add.IsSuccess, add.ErrorMessage);
        return (SettingModel)api.GetStoryElement(add.Payload).Payload;
    }

    private static SceneModel AddScene(StoryCADApi api, string name)
    {
        var add = api.AddElement(StoryItemType.Scene, Overview(api).Uuid.ToString(), name);
        Assert.IsTrue(add.IsSuccess, add.ErrorMessage);
        return (SceneModel)api.GetStoryElement(add.Payload).Payload;
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
    public async Task BuildWorkflowRequestBody_CharacterIsProtagonist_ResolvesProtagonistCharacter()
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
    public async Task BuildWorkflowRequestBody_PersonVsSelfProblem_ResolvesBothSeatsToSameCharacter()
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
    public async Task BuildWorkflowRequestBody_NoAntagonist_SetsAntagonistCharacterNull()
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
    public async Task BuildWorkflowRequestBody_RtfInNestedStrings_StripsRtf()
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
    public async Task BuildWorkflowRequestBody_FlawBackstory_InputHasFourTopLevelKeys()
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
    public async Task BuildWorkflowRequestBody_NoJsonInputDeclared_LeavesInputNull()
    {
        var api = await NewOutline();
        var character = AddCharacter(api, "Anyone");
        var partner = AddCharacter(api, "Someone");

        var body = Runner(api, "Relationship").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Character"] = character, ["Partner"] = partner });

        Assert.IsNull(body.Input, "a workflow without a JsonInput property list gets no input field");
    }

    [TestMethod]
    public async Task BuildWorkflowRequestBody_CharacterBuilder_InputHasFourTopLevelKeysAndCharacterKeysMatchSpec()
    {
        var api = await NewOutline();
        var character = AddCharacter(api, "Nell");

        var body = Runner(api, "CharacterBuilder").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Character"] = character });

        Assert.IsNotNull(body.Input, "CharacterBuilder declares a JsonInput property list; input must be built");
        CollectionAssert.AreEquivalent(
            new[] { "character", "relatedProblems", "lists", "storyContext" },
            body.Input.Select(p => p.Key).ToList());

        var characterObj = body.Input["character"].AsObject();
        CollectionAssert.AreEquivalent(
            WorkflowRegistry.Get("CharacterBuilder").JsonInput.TargetProperties.ToList(),
            characterObj.Select(p => p.Key).ToList());
    }

    [TestMethod]
    public async Task BuildWorkflowRequestBody_CharacterBuilder_TraitListArrivesAsJsonArray()
    {
        var api = await NewOutline();
        var character = AddCharacter(api, "Traity");
        character.TraitList = new List<string> { "Brave", "Stubborn" };

        var body = Runner(api, "CharacterBuilder").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Character"] = character });

        var traitList = body.Input["character"].AsObject()["TraitList"].AsArray();
        CollectionAssert.AreEqual(
            new[] { "Brave", "Stubborn" },
            traitList.Select(v => v.GetValue<string>()).ToList());
    }

    [TestMethod]
    public async Task BuildWorkflowRequestBody_CharacterBuilder_AdventurousnessArrivesUnderCorrectSpelling()
    {
        var api = await NewOutline();
        var character = AddCharacter(api, "Daring");
        character.Adventurousness = "High";

        var body = Runner(api, "CharacterBuilder").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Character"] = character });

        var characterObj = body.Input["character"].AsObject();
        Assert.AreEqual("High", characterObj["Adventurousness"].GetValue<string>());
    }

    [TestMethod]
    public async Task BuildWorkflowRequestBody_SettingUsedByOneScene_ResolvesRelatedSceneAndCast()
    {
        var api = await NewOutline();
        var setting = AddSetting(api, "The Old Mill");
        setting.Description = "Abandoned and damp";
        setting.Locale = "Countryside";

        var scene = AddScene(api, "Confrontation");
        scene.Setting = setting.Uuid;
        scene.Description = "They meet at last";
        var alice = AddCharacter(api, "Alice");
        var bob = AddCharacter(api, "Bob");
        scene.CastMembers = new List<Guid> { alice.Uuid, bob.Uuid };

        var body = Runner(api, "SettingBuilder").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Setting"] = setting });

        Assert.IsNotNull(body.Input, "SettingBuilder declares a JsonInput property list; input must be built");

        var related = body.Input["relatedScenes"].AsArray();
        Assert.AreEqual(1, related.Count);
        var sceneObj = related[0].AsObject();
        Assert.AreEqual(scene.Uuid.ToString(), sceneObj["GUID"].GetValue<string>());
        Assert.AreEqual("Confrontation", sceneObj["Name"].GetValue<string>());
        Assert.AreEqual("They meet at last", sceneObj["Description"].GetValue<string>());

        var castMembers = sceneObj["CastMembers"].AsArray().Select(v => v.GetValue<string>()).ToList();
        CollectionAssert.AreEquivalent(new[] { alice.Uuid.ToString(), bob.Uuid.ToString() }, castMembers);

        var cast = sceneObj["cast"].AsArray();
        Assert.AreEqual(2, cast.Count);
        var castNames = cast.Select(c => c.AsObject()["Name"].GetValue<string>()).ToList();
        CollectionAssert.AreEquivalent(new[] { "Alice", "Bob" }, castNames);
    }

    [TestMethod]
    public async Task BuildWorkflowRequestBody_SettingUsedByNoScene_RelatedScenesIsEmptyArray()
    {
        var api = await NewOutline();
        var setting = AddSetting(api, "Unused Attic");

        var body = Runner(api, "SettingBuilder").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Setting"] = setting });

        var related = body.Input["relatedScenes"].AsArray();
        Assert.AreEqual(0, related.Count);
    }

    [TestMethod]
    public async Task BuildWorkflowRequestBody_SceneInTrashUsesSetting_ExcludedFromRelatedScenes()
    {
        var api = await NewOutline();
        var setting = AddSetting(api, "The Vault");
        var liveScene = AddScene(api, "Live Scene");
        liveScene.Setting = setting.Uuid;
        var trashedScene = AddScene(api, "Trashed Scene");
        trashedScene.Setting = setting.Uuid;

        var delete = await api.DeleteElement(trashedScene.Uuid);
        Assert.IsTrue(delete.IsSuccess, delete.ErrorMessage);

        var body = Runner(api, "SettingBuilder").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Setting"] = setting });

        var related = body.Input["relatedScenes"].AsArray();
        Assert.AreEqual(1, related.Count, "the trashed scene must not appear alongside the live one");
        Assert.AreEqual(liveScene.Uuid.ToString(), related[0].AsObject()["GUID"].GetValue<string>());
    }

    [TestMethod]
    public async Task BuildWorkflowRequestBody_SettingBuilder_InputHasFourTopLevelKeysAndSettingKeysMatchSpec()
    {
        var api = await NewOutline();
        var setting = AddSetting(api, "Harbor Town");

        var body = Runner(api, "SettingBuilder").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Setting"] = setting });

        Assert.IsNotNull(body.Input, "SettingBuilder declares a JsonInput property list; input must be built");
        CollectionAssert.AreEquivalent(
            new[] { "setting", "relatedScenes", "lists", "storyContext" },
            body.Input.Select(p => p.Key).ToList());

        var settingObj = body.Input["setting"].AsObject();
        CollectionAssert.AreEquivalent(
            WorkflowRegistry.Get("SettingBuilder").JsonInput.TargetProperties.ToList(),
            settingObj.Select(p => p.Key).ToList());
    }

    [TestMethod]
    public async Task BuildWorkflowRequestBody_CastMemberGuidDoesNotResolveToCharacter_KeptInCastMembersSkippedFromCast()
    {
        var api = await NewOutline();
        var setting = AddSetting(api, "Ruined Chapel");
        var scene = AddScene(api, "Standoff");
        scene.Setting = setting.Uuid;
        var alice = AddCharacter(api, "Alice");
        var missingGuid = Guid.NewGuid();
        scene.CastMembers = new List<Guid> { alice.Uuid, missingGuid };

        var body = Runner(api, "SettingBuilder").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Setting"] = setting });

        var sceneObj = body.Input["relatedScenes"].AsArray()[0].AsObject();
        var castMembers = sceneObj["CastMembers"].AsArray().Select(v => v.GetValue<string>()).ToList();
        CollectionAssert.AreEquivalent(new[] { alice.Uuid.ToString(), missingGuid.ToString() }, castMembers);

        var cast = sceneObj["cast"].AsArray();
        Assert.AreEqual(1, cast.Count, "an unresolved GUID is skipped from cast, but stays in CastMembers");
        Assert.AreEqual("Alice", cast[0].AsObject()["Name"].GetValue<string>());
    }

    [TestMethod]
    public async Task BuildWorkflowRequestBody_SceneWithEmptyCastMembers_CastIsEmptyArray()
    {
        var api = await NewOutline();
        var setting = AddSetting(api, "Quiet Library");
        var scene = AddScene(api, "Reading");
        scene.Setting = setting.Uuid;
        // CastMembers left at its constructor default: an empty list.

        var body = Runner(api, "SettingBuilder").BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Setting"] = setting });

        var sceneObj = body.Input["relatedScenes"].AsArray()[0].AsObject();
        Assert.AreEqual(0, sceneObj["cast"].AsArray().Count);
        Assert.AreEqual(0, sceneObj["CastMembers"].AsArray().Count);
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

    /// <summary>
    ///     The builder reads each listed name by its JSON key, and a name that matches no property
    ///     comes back as an empty string. So a misspelled registry entry would send empty data
    ///     instead of failing. Every name in every JsonInput list must match a real property.
    /// </summary>
    [TestMethod]
    public void JsonInput_EveryPropertyName_MatchesAModelProperty()
    {
        var targetTypes = new Dictionary<StoryItemType, Type>
        {
            [StoryItemType.Character] = typeof(CharacterModel),
            [StoryItemType.Problem] = typeof(ProblemModel),
            [StoryItemType.Setting] = typeof(SettingModel),
        };

        var migrated = WorkflowRegistry.All.Where(w => w.JsonInput != null).ToList();
        Assert.IsTrue(migrated.Count > 0, "at least one workflow declares JsonInput");

        foreach (var workflow in migrated)
        {
            Assert.IsTrue(targetTypes.TryGetValue(workflow.PrimaryElementType, out var targetType),
                $"{workflow.Label}: add {workflow.PrimaryElementType} to this test's type map");
            AssertAllResolve(workflow.Label, "TargetProperties", targetType, workflow.JsonInput.TargetProperties);
            AssertAllResolve(workflow.Label, "RelatedProblemProperties", typeof(ProblemModel), workflow.JsonInput.RelatedProblemProperties);
            AssertAllResolve(workflow.Label, "ResolvedCharacterProperties", typeof(CharacterModel), workflow.JsonInput.ResolvedCharacterProperties);
            AssertAllResolve(workflow.Label, "RelatedSceneProperties", typeof(SceneModel), workflow.JsonInput.RelatedSceneProperties);
        }
    }

    private static void AssertAllResolve(string label, string list, Type type, IReadOnlyList<string> names)
    {
        foreach (var name in names)
        {
            var found = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Any(p => p.Name == name ||
                          (p.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), true)
                              .FirstOrDefault() as System.Text.Json.Serialization.JsonPropertyNameAttribute)?.Name == name);
            Assert.IsTrue(found, $"{label}.{list}: \"{name}\" matches no property of {type.Name}");
        }
    }
}
