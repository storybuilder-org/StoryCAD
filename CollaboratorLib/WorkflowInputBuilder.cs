using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CollaboratorLib.Context;
using StoryCADLib.Models;
using StoryCADLib.Models.StoryWorld;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Reports;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;
using StoryCollaborator.Services;
using StoryCollaborator.Workflows;

namespace StoryCollaborator;

/// <summary>
/// Issue #260: builds the "input" request field, one JSON object per workflow run, for a
/// workflow whose registry entry declares <see cref="Workflow.JsonInput"/>. A workflow with no
/// JsonInput gets a null input; Elements and Args stay the only carriers for it until it
/// migrates. Adding the next workflow is a registry entry (its own JsonInput property lists)
/// plus a small shape method here -- <see cref="BuildCharacterCraftInput"/> is the character /
/// relatedProblems / lists / storyContext shape shared by FlawBackstory and CharacterBuilder;
/// <see cref="BuildSettingInput"/> is SettingBuilder's own shape; the eight methods below it are
/// Premise, StoryForm, StoryProblem, InnerOuterProblems, Relationship, DefineStoryWorld,
/// SceneBuilder and ProblemBuilder (StoryCAD proposal #260, second wave).
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
            "Premise" => BuildPremiseInput(workflow, gatheredElements, api, model),
            "StoryForm" => BuildStoryFormInput(workflow, gatheredElements, api, model),
            "StoryProblem" => BuildStoryProblemInput(workflow, gatheredElements, api, model),
            "InnerOuterProblems" => BuildInnerOuterProblemsInput(workflow, gatheredElements, api, model),
            "Relationship" => BuildRelationshipInput(workflow, gatheredElements, api, model),
            "DefineStoryWorld" => BuildDefineStoryWorldInput(workflow, gatheredElements, api, model),
            "SceneBuilder" => BuildSceneBuilderInput(workflow, gatheredElements, api, model),
            "ProblemBuilder" => BuildProblemBuilderInput(workflow, gatheredElements, api, model),
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
            ["character"] = ProjectElement(character, spec["Target"]),
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

            var problemObj = ProjectElement(problem, spec["RelatedProblem"]);
            problemObj["protagonistCharacter"] = ResolveElementRef<CharacterModel>(problem.Protagonist, spec["ResolvedCharacter"], api);
            problemObj["antagonistCharacter"] = ResolveElementRef<CharacterModel>(problem.Antagonist, spec["ResolvedCharacter"], api);
            array.Add(problemObj);
        }

        return array;
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
            ["setting"] = ProjectElement(setting, spec["Target"]),
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

            var sceneObj = ProjectElement(scene, spec["RelatedScene"]);
            sceneObj["cast"] = ResolveCharacterList(scene.CastMembers, spec["ResolvedCharacter"], api);
            array.Add(sceneObj);
        }

        return array;
    }

    /// <summary>
    /// The overview / storyContext shape (StoryCAD proposal #260 section 1, "Premise"). Keyed on
    /// the gathered "Overview" (Workflow.GetDefaultLabel for StoryItemType.StoryOverview).
    /// </summary>
    private static JsonObject? BuildPremiseInput(
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements,
        IStoryCADAPI api,
        StoryModel model)
    {
        if (!gatheredElements.TryGetValue("Overview", out var overviewElement) ||
            overviewElement is not OverviewModel overview)
        {
            return null;
        }

        var spec = workflow.JsonInput!;

        return new JsonObject
        {
            ["overview"] = ProjectElement(overview, spec["Target"]),
            ["storyContext"] = BuildStoryContext(api, model)
        };
    }

    /// <summary>
    /// The overview / lists / storyContext shape (StoryCAD proposal #260 section 2, "StoryForm").
    /// Decision 5: the registry declares Genre and StoryType as example lists, so BuildLists
    /// below returns them under "lists" rather than the empty strings the template reads today.
    /// </summary>
    private static JsonObject? BuildStoryFormInput(
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements,
        IStoryCADAPI api,
        StoryModel model)
    {
        if (!gatheredElements.TryGetValue("Overview", out var overviewElement) ||
            overviewElement is not OverviewModel overview)
        {
            return null;
        }

        var spec = workflow.JsonInput!;

        return new JsonObject
        {
            ["overview"] = ProjectElement(overview, spec["Target"]),
            ["lists"] = BuildLists(workflow, api),
            ["storyContext"] = BuildStoryContext(api, model)
        };
    }

    /// <summary>
    /// The overview / problem / storyContext shape (StoryCAD proposal #260 section 3,
    /// "StoryProblem"). "problem" is null when Overview.StoryProblem has not resolved to a
    /// gathered Problem (the seats then resolve from the Problem's own GUIDs, so a missing
    /// Problem also means no seats).
    /// </summary>
    private static JsonObject? BuildStoryProblemInput(
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements,
        IStoryCADAPI api,
        StoryModel model)
    {
        if (!gatheredElements.TryGetValue("Overview", out var overviewElement) ||
            overviewElement is not OverviewModel overview)
        {
            return null;
        }

        var spec = workflow.JsonInput!;

        JsonObject? problemObj = null;
        if (gatheredElements.TryGetValue("Problem", out var problemElement) &&
            problemElement is ProblemModel problem)
        {
            problemObj = ProjectElement(problem, spec["Problem"]);
            problemObj["protagonistCharacter"] = ResolveElementRef<CharacterModel>(problem.Protagonist, spec["ResolvedCharacter"], api);
            problemObj["antagonistCharacter"] = ResolveElementRef<CharacterModel>(problem.Antagonist, spec["ResolvedCharacter"], api);
        }

        return new JsonObject
        {
            ["overview"] = ProjectElement(overview, spec["Target"]),
            ["problem"] = problemObj,
            ["storyContext"] = BuildStoryContext(api, model)
        };
    }

    /// <summary>
    /// The outerProblem / innerProblem / storyContext shape (StoryCAD proposal #260 section 4,
    /// "InnerOuterProblems"). The Protagonist is also an output (Flaw), so it sits under
    /// outerProblem as protagonistCharacter, per the case-only-label rule (decision 4) applied to
    /// the earlier protagonistCharacter/antagonistCharacter convention, which stays as-is.
    /// </summary>
    private static JsonObject? BuildInnerOuterProblemsInput(
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements,
        IStoryCADAPI api,
        StoryModel model)
    {
        if (!gatheredElements.TryGetValue("OuterProblem", out var outerElement) ||
            outerElement is not ProblemModel outer)
        {
            return null;
        }

        var spec = workflow.JsonInput!;

        var outerObj = ProjectElement(outer, spec["OuterProblem"]);
        outerObj["protagonistCharacter"] = ResolveElementRef<CharacterModel>(outer.Protagonist, spec["ResolvedCharacter"], api);

        JsonObject? innerObj = null;
        if (gatheredElements.TryGetValue("InnerProblem", out var innerElement) &&
            innerElement is ProblemModel inner)
        {
            innerObj = ProjectElement(inner, spec["InnerProblem"]);
        }

        return new JsonObject
        {
            ["outerProblem"] = outerObj,
            ["innerProblem"] = innerObj,
            ["storyContext"] = BuildStoryContext(api, model)
        };
    }

    /// <summary>
    /// The character / partner / lists / storyContext shape (StoryCAD proposal #260 section 5,
    /// "Relationship"). Decision 2: CharacterChoices is dropped from this object -- the partner
    /// is a required input, so the template needs $.partner.GUID, not a scan of every character
    /// in the outline. The registry's own CharacterChoices CollectionInput (an Arg, not this
    /// object) is left untouched: the current template still reads it until it migrates.
    /// </summary>
    private static JsonObject? BuildRelationshipInput(
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements,
        IStoryCADAPI api,
        StoryModel model)
    {
        if (!gatheredElements.TryGetValue("Character", out var characterElement) ||
            characterElement is not CharacterModel character)
        {
            return null;
        }

        if (!gatheredElements.TryGetValue("Partner", out var partnerElement) ||
            partnerElement is not CharacterModel partner)
        {
            return null;
        }

        var spec = workflow.JsonInput!;

        return new JsonObject
        {
            ["character"] = ProjectElement(character, spec["Target"]),
            ["partner"] = ProjectElement(partner, spec["Target"]),
            ["lists"] = BuildLists(workflow, api),
            ["storyContext"] = BuildStoryContext(api, model)
        };
    }

    /// <summary>
    /// The storyWorld / storyProblem / relatedSettings / relatedResearch / lists / storyContext
    /// shape (StoryCAD proposal #260 section 6, "DefineStoryWorld"). "storyProblem" is null when
    /// Overview.StoryProblem is empty, the same case in which Collaborator.cs's
    /// InjectStoryProblemSeatsForWorld leaves "Problem" ungathered.
    /// </summary>
    private static JsonObject? BuildDefineStoryWorldInput(
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements,
        IStoryCADAPI api,
        StoryModel model)
    {
        if (!gatheredElements.TryGetValue("StoryWorld", out var worldElement) ||
            worldElement is not StoryWorldModel world)
        {
            return null;
        }

        var spec = workflow.JsonInput!;

        JsonObject? problemObj = null;
        if (gatheredElements.TryGetValue("Problem", out var problemElement) &&
            problemElement is ProblemModel problem)
        {
            problemObj = ProjectElement(problem, spec["Problem"]);
            problemObj["protagonistCharacter"] = ResolveElementRef<CharacterModel>(problem.Protagonist, spec["ResolvedCharacter"], api);
            problemObj["antagonistCharacter"] = ResolveElementRef<CharacterModel>(problem.Antagonist, spec["ResolvedCharacter"], api);
        }

        return new JsonObject
        {
            ["storyWorld"] = ProjectElement(world, spec["Target"]),
            ["storyProblem"] = problemObj,
            ["relatedSettings"] = BuildAllElements(api, StoryItemType.Setting, spec["Setting"]),
            ["relatedResearch"] = BuildRelatedResearch(gatheredElements, spec["Research"], model),
            ["lists"] = BuildLists(workflow, api),
            ["storyContext"] = BuildStoryContext(api, model)
        };
    }

    /// <summary>Notes and Web elements under the gathered StoryWorld's explorer subtree (#201).</summary>
    private static JsonArray BuildRelatedResearch(
        Dictionary<string, StoryElement> gatheredElements,
        IReadOnlyList<string> properties,
        StoryModel model)
    {
        var array = new JsonArray();
        if (gatheredElements.TryGetValue("StoryWorld", out var world) && world?.Node != null)
        {
            foreach (var research in EnumerateStoryWorldResearch(world.Node, model))
                array.Add(ProjectElement(research, properties));
        }

        return array;
    }

    /// <summary>
    /// Notes and Web under the StoryWorld explorer subtree -- same walk as the private instance
    /// method WorkflowRunner.EnumerateStoryWorldResearch (#201), duplicated here because that one
    /// reads the instance storyModel field and this builder is static (same pattern as IsInTrash
    /// below, already duplicated from WorkflowRunner for the same reason).
    /// </summary>
    private static IEnumerable<StoryElement> EnumerateStoryWorldResearch(StoryNodeItem root, StoryModel model)
    {
        if (root.Children == null || root.Children.Count == 0)
            yield break;

        foreach (var child in root.Children)
        {
            if (model.StoryElements.StoryElementGuids.TryGetValue(child.Uuid, out var element))
            {
                if (element.ElementType is StoryItemType.Notes or StoryItemType.Web)
                    yield return element;
            }

            foreach (var nested in EnumerateStoryWorldResearch(child, model))
                yield return nested;
        }
    }

    /// <summary>
    /// The scene / owningProblem / contributingProblems / storyProblem / precedingScene /
    /// nextScene / characterChoices / settingChoices / problemChoices / lists / storyContext
    /// shape (StoryCAD proposal #260 section 7, "SceneBuilder").
    ///
    /// Decision 3: one Problem projection for the owning Problem and each contributing Problem;
    /// the owner is not repeated in contributingProblems.
    /// Decision 4: Setting and ViewpointCharacter are case-only labels away from their own GUID
    /// property, so their resolved reference is named settingElement / viewpointCharacterElement
    /// rather than setting / viewpointCharacter. Protagonist/Antagonist keep
    /// protagonistCharacter/antagonistCharacter, as FlawBackstory and CharacterBuilder do.
    /// </summary>
    private static JsonObject? BuildSceneBuilderInput(
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements,
        IStoryCADAPI api,
        StoryModel model)
    {
        if (!gatheredElements.TryGetValue("Scene", out var sceneElement) ||
            sceneElement is not SceneModel scene)
        {
            return null;
        }

        var spec = workflow.JsonInput!;

        var sceneObj = ProjectElement(scene, spec["Target"]);
        sceneObj["cast"] = ResolveCharacterList(scene.CastMembers, spec["ResolvedCharacter"], api);
        sceneObj["protagonistCharacter"] = ResolveElementRef<CharacterModel>(scene.Protagonist, spec["SceneCharacter"], api);
        sceneObj["antagonistCharacter"] = ResolveElementRef<CharacterModel>(scene.Antagonist, spec["SceneCharacter"], api);
        sceneObj["viewpointCharacterElement"] = ResolveElementRef<CharacterModel>(scene.ViewpointCharacter, spec["ResolvedCharacter"], api);
        sceneObj["settingElement"] = ResolveElementRef<SettingModel>(scene.Setting, spec["SettingElement"], api);

        ProblemModel? owner = null;
        JsonObject? owningProblemObj = null;
        if (gatheredElements.TryGetValue("Problem", out var problemElement) &&
            problemElement is ProblemModel problem)
        {
            owner = problem;
            owningProblemObj = ProjectProblemForScene(problem, spec, api);
        }

        var resolver = new SceneStructureNeighborResolver(api);
        var owners = resolver.FindStructureOwners(scene.Uuid);
        var contributingArray = new JsonArray();
        foreach (var contributor in owners)
        {
            if (owner != null && contributor.Uuid == owner.Uuid)
                continue;
            contributingArray.Add(ProjectProblemForScene(contributor, spec, api));
        }

        JsonObject? storyProblemObj = null;
        if (gatheredElements.TryGetValue("Overview", out var overviewElement) &&
            overviewElement is OverviewModel overview &&
            overview.StoryProblem != Guid.Empty)
        {
            var found = api.GetStoryElement(overview.StoryProblem);
            if (found.IsSuccess && found.Payload is ProblemModel storyProblem)
                storyProblemObj = ProjectElement(storyProblem, spec["SceneStoryProblem"]);
        }

        JsonObject? precedingObj = gatheredElements.TryGetValue("PrecedingScene", out var precedingElement) &&
            precedingElement is SceneModel preceding
                ? ProjectElement(preceding, spec["NeighborScene"])
                : null;
        JsonObject? nextObj = gatheredElements.TryGetValue("NextScene", out var nextElement) &&
            nextElement is SceneModel next
                ? ProjectElement(next, spec["NeighborScene"])
                : null;

        var explorerParent = resolver.GetExplorerParentProblem(scene);
        var orphan = owners.Count == 0 && explorerParent == null;

        return new JsonObject
        {
            ["scene"] = sceneObj,
            ["owningProblem"] = owningProblemObj,
            ["contributingProblems"] = contributingArray,
            ["storyProblem"] = storyProblemObj,
            ["precedingScene"] = precedingObj,
            ["nextScene"] = nextObj,
            ["characterChoices"] = BuildAllElements(api, StoryItemType.Character, spec["ResolvedCharacter"]),
            ["settingChoices"] = BuildAllElements(api, StoryItemType.Setting, spec["SettingChoice"]),
            ["problemChoices"] = orphan
                ? BuildAllElements(api, StoryItemType.Problem, spec["ProblemChoice"])
                : new JsonArray(),
            ["lists"] = BuildLists(workflow, api),
            ["storyContext"] = BuildStoryContext(api, model)
        };
    }

    /// <summary>The one Problem projection SceneBuilder uses for both owningProblem and each contributingProblems entry.</summary>
    private static JsonObject ProjectProblemForScene(ProblemModel problem, WorkflowJsonInputSpec spec, IStoryCADAPI api)
    {
        var obj = ProjectElement(problem, spec["Problem"]);
        obj["protagonistCharacter"] = ResolveElementRef<CharacterModel>(problem.Protagonist, spec["ResolvedCharacter"], api);
        obj["antagonistCharacter"] = ResolveElementRef<CharacterModel>(problem.Antagonist, spec["ResolvedCharacter"], api);
        return obj;
    }

    /// <summary>
    /// The problem / sceneChoices / problemChoices / characterChoices / lists / storyContext
    /// shape (StoryCAD proposal #260 section 8, "ProblemBuilder"). problem.StructureBeats
    /// replaces the CurrentBeats text arg: each beat carries its own BoundGUID plus the bound
    /// element resolved beside it as "boundElement" (null for an empty beat). sceneChoices and
    /// problemChoices are the same free-candidate rule as the registry's own FreeElementsFor
    /// CollectionInputs (WorkflowRunner.GetCandidates), read here through its static overload.
    /// </summary>
    private static JsonObject? BuildProblemBuilderInput(
        Workflow workflow,
        Dictionary<string, StoryElement> gatheredElements,
        IStoryCADAPI api,
        StoryModel model)
    {
        if (!gatheredElements.TryGetValue("Problem", out var problemElement) ||
            problemElement is not ProblemModel problem)
        {
            return null;
        }

        var spec = workflow.JsonInput!;

        var problemObj = ProjectElement(problem, spec["Target"]);
        problemObj["StructureBeats"] = BuildStructureBeatsWithBoundElement(problem, api);
        problemObj["protagonistCharacter"] = ResolveElementRef<CharacterModel>(problem.Protagonist, spec["ResolvedCharacter"], api);
        problemObj["antagonistCharacter"] = ResolveElementRef<CharacterModel>(problem.Antagonist, spec["ResolvedCharacter"], api);

        var candidates = WorkflowRunner.GetCandidates(api, problem.Uuid);

        var sceneChoices = new JsonArray();
        foreach (var guid in candidates.Scenes)
        {
            var found = api.GetStoryElement(guid);
            if (found.IsSuccess && found.Payload != null)
                sceneChoices.Add(ProjectElement(found.Payload, spec["SceneChoice"]));
        }

        var problemChoices = new JsonArray();
        foreach (var guid in candidates.Problems)
        {
            var found = api.GetStoryElement(guid);
            if (found.IsSuccess && found.Payload != null)
                problemChoices.Add(ProjectElement(found.Payload, spec["ProblemChoice"]));
        }

        return new JsonObject
        {
            ["problem"] = problemObj,
            ["sceneChoices"] = sceneChoices,
            ["problemChoices"] = problemChoices,
            ["characterChoices"] = BuildAllElements(api, StoryItemType.Character, spec["CharacterChoice"]),
            ["lists"] = BuildLists(workflow, api),
            ["storyContext"] = BuildStoryContext(api, model)
        };
    }

    /// <summary>
    /// CurrentBeats becomes problem.StructureBeats (builder change 1 handles the plain
    /// Title/Description/BoundGUID projection generically through ToJsonValue); this method adds
    /// "boundElement" beside each beat -- the bound Problem or Scene resolved to {GUID, Name},
    /// null for an empty beat. "none" becomes [] naturally: an empty StructureBeats collection.
    /// </summary>
    private static JsonArray BuildStructureBeatsWithBoundElement(ProblemModel problem, IStoryCADAPI api)
    {
        var array = new JsonArray();
        foreach (var beat in problem.StructureBeats ?? Enumerable.Empty<StructureBeat>())
        {
            array.Add(new JsonObject
            {
                ["Title"] = ToJsonValue(beat.Title),
                ["Description"] = ToJsonValue(beat.Description),
                ["BoundGUID"] = ToJsonValue(beat.Guid),
                ["boundElement"] = ResolveAnyElementRef(beat.Guid, api)
            });
        }

        return array;
    }

    /// <summary>
    /// Rule 3 of the StoryCAD proposal: a missing reference (empty GUID, or it does not resolve
    /// to a T) is JSON null, always -- never an absent key, never an empty object.
    /// </summary>
    private static JsonObject? ResolveElementRef<T>(Guid guid, IReadOnlyList<string> properties, IStoryCADAPI api)
        where T : StoryElement
    {
        if (guid == Guid.Empty)
            return null;

        var found = api.GetStoryElement(guid);
        if (!found.IsSuccess || found.Payload is not T element)
            return null;

        return ProjectElement(element, properties);
    }

    /// <summary>
    /// A beat's bound element (Problem or Scene, StructureBeat.BoundGUID) resolved to {GUID,
    /// Name} regardless of its type. Null for an empty beat or a GUID that resolves to nothing.
    /// </summary>
    private static JsonObject? ResolveAnyElementRef(Guid guid, IStoryCADAPI api)
    {
        if (guid == Guid.Empty)
            return null;

        var found = api.GetStoryElement(guid);
        if (!found.IsSuccess || found.Payload == null)
            return null;

        return ProjectElement(found.Payload, new[] { "GUID", "Name" });
    }

    /// <summary>
    /// Rule from the SettingBuilder shape, reused by SceneBuilder's "cast": each GUID resolved to
    /// a Character projected with the given properties; a GUID that does not resolve to a
    /// Character is skipped (the caller's own GUID list, projected separately, keeps it). A null
    /// or empty list gives [].
    /// </summary>
    private static JsonArray ResolveCharacterList(IEnumerable<Guid>? guids, IReadOnlyList<string> properties, IStoryCADAPI api)
    {
        var array = new JsonArray();
        if (guids == null)
            return array;

        foreach (var guid in guids)
        {
            var found = api.GetStoryElement(guid);
            if (!found.IsSuccess || found.Payload is not CharacterModel character)
                continue;

            array.Add(ProjectElement(character, properties));
        }

        return array;
    }

    /// <summary>Every element of one type, projected with the given properties. No trash filter (same as the existing CollectionInputs with no FreeElementsFor).</summary>
    private static JsonArray BuildAllElements(IStoryCADAPI api, StoryItemType type, IReadOnlyList<string> properties)
    {
        var array = new JsonArray();
        var result = api.GetElementsByType(type);
        if (result.IsSuccess && result.Payload != null)
        {
            foreach (var element in result.Payload)
                array.Add(ProjectElement(element, properties));
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
            // Builder change 1 (StoryCAD proposal #260, "Facts that apply to all eight"):
            // StoryWorldModel.Cultures / PhysicalWorlds and ProblemModel.StructureBeats are lists
            // of objects; a bare ToString() would yield the CLR type name, not the data. Each
            // entry is projected onto its own [JsonPropertyName] properties.
            IEnumerable<CultureEntry> cultures => ToJsonObjectArray(cultures),
            IEnumerable<PhysicalWorldEntry> worlds => ToJsonObjectArray(worlds),
            IEnumerable<StructureBeat> beats => ToJsonObjectArray(beats),
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
    /// Builder change 1: each entry of a plain-data list (CultureEntry, PhysicalWorldEntry,
    /// StructureBeat) written with its own [JsonPropertyName] names, recursively through
    /// ToJsonValue so a nested Guid/string/list follows the same rules as everywhere else.
    /// </summary>
    private static JsonArray ToJsonObjectArray<T>(IEnumerable<T> items)
    {
        var array = new JsonArray();
        foreach (var item in items)
            array.Add(item == null ? null : ProjectPoco(item));
        return array;
    }

    private static JsonObject ProjectPoco(object item)
    {
        var obj = new JsonObject();
        foreach (var prop in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
            if (jsonName == null)
                continue;
            obj[jsonName] = ToJsonValue(prop.GetValue(item));
        }

        return obj;
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
