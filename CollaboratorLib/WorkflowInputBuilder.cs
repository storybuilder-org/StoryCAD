using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CollaboratorLib.Context;
using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Reports;
using StoryCADLib.ViewModels;
using StoryCollaborator.Workflows;

namespace StoryCollaborator;

/// <summary>
/// Issue #260: builds the "input" request field, one JSON object per workflow run, for a
/// workflow whose registry entry declares <see cref="Workflow.JsonInput"/>. A workflow with no
/// JsonInput gets a null input; Elements and Args stay the only carriers for it until it
/// migrates. Adding the next workflow is a registry entry (its own JsonInput property list)
/// plus a small branch here for its own top-level shape -- <see cref="BuildCharacterCraftInput"/>
/// is the character / relatedProblems / lists / storyContext shape shared by FlawBackstory and
/// CharacterBuilder; <see cref="BuildSettingInput"/> is SettingBuilder's own shape.
/// </summary>
internal static class WorkflowInputBuilder
{
    public static JsonObject? Build(
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements,
        IStoryCADAPI api,
        StoryModel model)
    {
        if (workflow.JsonInput == null)
            return null;

        JsonObject? result = workflow.Label switch
        {
            "FlawBackstory" or "CharacterBuilder" => BuildCharacterCraftInput(workflow, gatheredElements, api, model),
            "SettingBuilder" => BuildSettingInput(workflow, gatheredElements, api, model),
            _ => null
        };

        if (result != null)
            StripRtfRecursive(result);

        return result;
    }

    /// <summary>
    /// The character / relatedProblems / lists / storyContext shape (StoryCAD proposal #260,
    /// "The shape for FlawBackstory"), shared by FlawBackstory and CharacterBuilder. Keyed on a
    /// gathered "Character".
    /// </summary>
    private static JsonObject? BuildCharacterCraftInput(
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements,
        IStoryCADAPI api,
        StoryModel model)
    {
        if (!gatheredElements.TryGetValue("Character", out var targetElement) ||
            targetElement is not CharacterModel character)
        {
            return null;
        }

        var spec = workflow.JsonInput!;

        return new JsonObject
        {
            ["character"] = ProjectElement(character, spec.TargetProperties),
            ["relatedProblems"] = BuildRelatedProblems(character, spec, api, model),
            ["lists"] = BuildLists(workflow, api),
            ["storyContext"] = BuildStoryContext(api, model)
        };
    }

    /// <summary>Same Problems the RelatedProblems arg carries for this character (ProblemCharacterIndex).</summary>
    private static JsonArray BuildRelatedProblems(
        CharacterModel character,
        WorkflowJsonInputSpec spec,
        IStoryCADAPI api,
        StoryModel model)
    {
        var array = new JsonArray();
        var index = ProblemCharacterIndex.Build(api, model);
        foreach (var problemGuid in index.RelatedProblemGuids(character.Uuid))
        {
            var found = api.GetStoryElement(problemGuid);
            if (!found.IsSuccess || found.Payload is not ProblemModel problem)
                continue;

            var problemObj = ProjectElement(problem, spec.RelatedProblemProperties);
            problemObj["protagonistCharacter"] = ResolveCharacterRef(problem.Protagonist, spec, api);
            problemObj["antagonistCharacter"] = ResolveCharacterRef(problem.Antagonist, spec, api);
            array.Add(problemObj);
        }

        return array;
    }

    /// <summary>
    /// Rule 3 of the StoryCAD proposal: a missing reference (empty GUID, or it does not resolve
    /// to a Character) is JSON null, always -- never an absent key, never an empty object.
    /// </summary>
    private static JsonObject? ResolveCharacterRef(Guid guid, WorkflowJsonInputSpec spec, IStoryCADAPI api)
    {
        if (guid == Guid.Empty)
            return null;

        var found = api.GetStoryElement(guid);
        if (!found.IsSuccess || found.Payload is not CharacterModel character)
            return null;

        return ProjectElement(character, spec.ResolvedCharacterProperties);
    }

    /// <summary>
    /// The setting / relatedScenes / lists / storyContext shape (StoryCAD proposal #260,
    /// "Setting Builder"). Keyed on a gathered "Setting" (Workflow.GetDefaultLabel for
    /// StoryItemType.Setting -- SettingBuilder uses the simple constructor's default label).
    /// </summary>
    private static JsonObject? BuildSettingInput(
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements,
        IStoryCADAPI api,
        StoryModel model)
    {
        if (!gatheredElements.TryGetValue("Setting", out var targetElement) ||
            targetElement is not SettingModel setting)
        {
            return null;
        }

        var spec = workflow.JsonInput!;

        return new JsonObject
        {
            ["setting"] = ProjectElement(setting, spec.TargetProperties),
            ["relatedScenes"] = BuildRelatedScenes(setting, spec, api),
            ["lists"] = BuildLists(workflow, api),
            ["storyContext"] = BuildStoryContext(api, model)
        };
    }

    /// <summary>
    /// Every Scene in the story, not in the trash, whose Setting GUID equals this Setting's
    /// Uuid. Same source and trash test as WorkflowRunner.GetLiveElements/IsInTrash: all Scenes
    /// via api.GetElementsByType, then drop any whose node chain ends at the TrashCan root.
    /// Story order is StoryElements enumeration order (the same order GetElementsByType returns).
    /// </summary>
    private static JsonArray BuildRelatedScenes(SettingModel setting, WorkflowJsonInputSpec spec, IStoryCADAPI api)
    {
        var array = new JsonArray();
        var scenesResult = api.GetElementsByType(StoryItemType.Scene);
        if (!scenesResult.IsSuccess || scenesResult.Payload == null)
            return array;

        foreach (var element in scenesResult.Payload)
        {
            if (element is not SceneModel scene)
                continue;
            if (scene.Setting != setting.Uuid)
                continue;
            if (IsInTrash(scene))
                continue;

            var sceneObj = ProjectElement(scene, spec.RelatedSceneProperties);
            sceneObj["cast"] = BuildCast(scene.CastMembers, spec, api);
            array.Add(sceneObj);
        }

        return array;
    }

    /// <summary>
    /// Rule from the SettingBuilder shape: each CastMembers GUID resolved to {GUID, Name} of
    /// that Character; a GUID that does not resolve to a Character is skipped (CastMembers
    /// itself, projected separately, keeps it). An empty or null CastMembers gives [].
    /// </summary>
    private static JsonArray BuildCast(List<Guid>? castMembers, WorkflowJsonInputSpec spec, IStoryCADAPI api)
    {
        var array = new JsonArray();
        if (castMembers == null)
            return array;

        foreach (var guid in castMembers)
        {
            var found = api.GetStoryElement(guid);
            if (!found.IsSuccess || found.Payload is not CharacterModel character)
                continue;

            array.Add(ProjectElement(character, spec.ResolvedCharacterProperties));
        }

        return array;
    }

    /// <summary>
    /// Trash: the node chain ends at the TrashCan root, the same test
    /// WorkflowRunner.IsInTrash makes on ProblemBuilder's candidate list.
    /// </summary>
    private static bool IsInTrash(StoryElement element) =>
        element.Node != null && StoryNodeItem.RootNodeType(element.Node) == StoryItemType.TrashCan;

    /// <summary>Same values EnrichWithExamples sends for this workflow's ExampleLists, as arrays.</summary>
    private static JsonObject BuildLists(Workflow workflow, IStoryCADAPI api)
    {
        var lists = new JsonObject();
        foreach (var name in workflow.GetIO().ExampleLists)
        {
            var result = api.GetExamples(name);
            var values = result.IsSuccess && result.Payload != null
                ? result.Payload.Where(v => !string.IsNullOrWhiteSpace(v))
                : Enumerable.Empty<string>();

            var array = new JsonArray();
            foreach (var value in values)
                array.Add(JsonValue.Create(value));
            lists[name] = array;
        }

        return lists;
    }

    private static JsonObject BuildStoryContext(IStoryCADAPI api, StoryModel model)
    {
        var context = new StoryContextBuilder(api).BuildContextObject(model);

        var gaps = new JsonArray();
        foreach (var gap in context.Gaps)
            gaps.Add(JsonValue.Create(gap));

        return new JsonObject
        {
            ["phase"] = context.Phase,
            ["gaps"] = gaps,
            ["StoryType"] = context.StoryType,
            ["StoryGenre"] = context.StoryGenre,
            ["Premise"] = context.Premise
        };
    }

    /// <summary>Projects the named properties (their JSON key, e.g. "GUID") onto a JsonObject.</summary>
    private static JsonObject ProjectElement(StoryElement element, IReadOnlyList<string> properties)
    {
        var obj = new JsonObject();
        foreach (var jsonKey in properties)
            obj[jsonKey] = ToJsonValue(GetPropertyValue(element, jsonKey));
        return obj;
    }

    /// <summary>
    /// Reads a property by its JSON name (a [JsonPropertyName] attribute -- Uuid is "GUID"),
    /// falling back to a same-named CLR property.
    /// </summary>
    private static object? GetPropertyValue(object element, string jsonKey)
    {
        var type = element.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == jsonKey)
                return prop.GetValue(element);
        }

        return type.GetProperty(jsonKey, BindingFlags.Public | BindingFlags.Instance)?.GetValue(element);
    }

    private static JsonNode? ToJsonValue(object? value)
    {
        return value switch
        {
            null => JsonValue.Create(string.Empty),
            Guid g => JsonValue.Create(g == Guid.Empty ? string.Empty : g.ToString()),
            string s => JsonValue.Create(s),
            // TraitList and other string lists (Issue #260, CharacterBuilder): a JSON array of
            // strings, not "System.Collections.Generic.List`1[...]" from a bare ToString().
            IEnumerable<string> strings => ToJsonArray(strings),
            // CastMembers (Issue #260, SettingBuilder's "relatedScenes"): a JSON array of GUID
            // strings, same rule as a lone Guid -- Guid.Empty becomes "".
            IEnumerable<Guid> guids => ToJsonGuidArray(guids),
            _ => JsonValue.Create(value.ToString())
        };
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
            array.Add(JsonValue.Create(value ?? string.Empty));
        return array;
    }

    private static JsonArray ToJsonGuidArray(IEnumerable<Guid> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
            array.Add(JsonValue.Create(value == Guid.Empty ? string.Empty : value.ToString()));
        return array;
    }

    /// <summary>
    /// Rule 8 of the StoryCAD proposal: RTF stripped from every string in the object, not only
    /// the top level -- reuses <see cref="RichTextStripper"/>, the same stripper
    /// SerializeElementOutbound uses for Elements.
    /// </summary>
    private static void StripRtfRecursive(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(p => p.Key).ToList())
            {
                var child = obj[key];
                if (TryStripRtf(child, out var stripped))
                    obj[key] = stripped;
                else
                    StripRtfRecursive(child);
            }
        }
        else if (node is JsonArray arr)
        {
            for (var i = 0; i < arr.Count; i++)
            {
                var child = arr[i];
                if (TryStripRtf(child, out var stripped))
                    arr[i] = stripped;
                else
                    StripRtfRecursive(child);
            }
        }
    }

    private static bool TryStripRtf(JsonNode? node, out string? stripped)
    {
        stripped = null;
        if (node is JsonValue value && value.TryGetValue<string>(out var s) &&
            s != null && s.StartsWith(@"{\rtf", StringComparison.Ordinal))
        {
            stripped = new RichTextStripper().StripRichTextFormat(s);
            return true;
        }

        return false;
    }
}
