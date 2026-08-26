using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Reports;
using StoryCADLib.ViewModels;
using StoryCollaborator;
using StoryCollaborator.Models;
using StoryCollaborator.Services;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>Collaborator #208: SceneBuilder owner resolve, bail, and Protect. Real StoryCADApi.</summary>
[TestClass]
public class SceneBuilderResolverTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    [TestMethod]
    public async Task ExplorerParentSubproblem_MiddleScene_HasBothNeighbors()
    {
        var api = CreateApi();
        var fx = await BuildThreeSceneComplication(api);
        var resolver = new SceneStructureNeighborResolver(api);

        var resolved = resolver.ResolveForSceneBuilder(fx.Middle);

        Assert.AreEqual(
            SceneStructureNeighborResolver.SceneBuilderOwnerState.ExplorerParentSubproblem,
            resolved.OwnerState);
        Assert.AreEqual(fx.Complication.Uuid, resolved.OwnerProblem!.Uuid);
        Assert.AreEqual(1, resolved.ContributingProblems.Count);
        Assert.AreEqual(fx.First.Uuid, resolved.PrecedingScene!.Uuid);
        Assert.AreEqual(fx.Last.Uuid, resolved.NextScene!.Uuid);
        Assert.IsNull(resolved.BailReason);
    }

    [TestMethod]
    public async Task ExplorerParentStoryProblem_BailsEvenWhenComplicationAlsoOwns()
    {
        var api = CreateApi();
        var fx = await BuildThreeSceneComplication(api);
        // Reparent middle Scene under the Story Problem while leaving it on the Complication sheet.
        var add = api.AddElement(StoryItemType.Scene, fx.StoryProblem.Uuid.ToString(), "Under Story Problem");
        Assert.IsTrue(add.IsSuccess);
        var underSp = (SceneModel)api.GetStoryElement(add.Payload).Payload!;
        api.AssignElementToBeat(fx.Complication.Uuid, 1, underSp.Uuid);

        var resolved = new SceneStructureNeighborResolver(api).ResolveForSceneBuilder(underSp);

        Assert.AreEqual(SceneStructureNeighborResolver.SceneBuilderOwnerState.StoryProblemBail, resolved.OwnerState);
        Assert.AreEqual(fx.StoryProblem.Uuid, resolved.OwnerProblem!.Uuid);
        StringAssert.Contains(resolved.BailReason, "does not run on a Story Problem");
    }

    [TestMethod]
    public async Task OnlyStructureOwnerIsStoryProblem_Bails()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("SP only", "Author", "0");
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var spAdd = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "Story Problem");
        var sp = (ProblemModel)api.GetStoryElement(spAdd.Payload).Payload!;
        api.UpdateElementProperty(sp.Uuid, "ProblemCategory", "Story problem");
        api.UpdateElementProperty(overview.Uuid, "StoryProblem", sp.Uuid);
        api.CreateBeat(sp.Uuid, "Beat", "d");
        var sceneAdd = api.AddElement(StoryItemType.Folder, overview.Uuid.ToString(), "Folder");
        var folder = api.GetStoryElement(sceneAdd.Payload).Payload!;
        var scAdd = api.AddElement(StoryItemType.Scene, folder.Uuid.ToString(), "On SP sheet");
        var scene = api.GetStoryElement(scAdd.Payload).Payload!;
        api.AssignElementToBeat(sp.Uuid, 0, scene.Uuid);

        var resolved = new SceneStructureNeighborResolver(api).ResolveForSceneBuilder(scene);

        Assert.AreEqual(SceneStructureNeighborResolver.SceneBuilderOwnerState.StoryProblemBail, resolved.OwnerState);
        Assert.IsNull(resolved.PrecedingScene);
        Assert.IsNull(resolved.NextScene);
    }

    [TestMethod]
    public async Task EmptyCategoryOnChosenOwner_Bails()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("Empty cat", "Author", "0");
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var pAdd = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "Uncategorized");
        var problem = (ProblemModel)api.GetStoryElement(pAdd.Payload).Payload!;
        api.CreateBeat(problem.Uuid, "Beat", "d");
        var scAdd = api.AddElement(StoryItemType.Scene, problem.Uuid.ToString(), "Child");
        var scene = api.GetStoryElement(scAdd.Payload).Payload!;
        api.AssignElementToBeat(problem.Uuid, 0, scene.Uuid);

        var resolved = new SceneStructureNeighborResolver(api).ResolveForSceneBuilder(scene);

        Assert.AreEqual(SceneStructureNeighborResolver.SceneBuilderOwnerState.EmptyCategoryBail, resolved.OwnerState);
        Assert.AreEqual(problem.Uuid, resolved.OwnerProblem!.Uuid);
        StringAssert.Contains(resolved.BailReason, "Problem Category");
    }

    [TestMethod]
    public async Task EmptyExplorerParent_UniqueCategorizedContributor_Runs()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("Fall-through", "Author", "0");
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var parentAdd = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "Empty parent");
        var parent = (ProblemModel)api.GetStoryElement(parentAdd.Payload).Payload!;
        var cAdd = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "Complication");
        var complication = (ProblemModel)api.GetStoryElement(cAdd.Payload).Payload!;
        api.UpdateElementProperty(complication.Uuid, "ProblemCategory", "Complication");
        api.CreateBeat(complication.Uuid, "B1", "d");
        api.CreateBeat(complication.Uuid, "B2", "d");
        api.CreateBeat(complication.Uuid, "B3", "d");
        var firstAdd = api.AddElement(StoryItemType.Scene, parent.Uuid.ToString(), "First");
        var midAdd = api.AddElement(StoryItemType.Scene, parent.Uuid.ToString(), "Middle");
        var lastAdd = api.AddElement(StoryItemType.Scene, parent.Uuid.ToString(), "Last");
        var first = api.GetStoryElement(firstAdd.Payload).Payload!;
        var middle = api.GetStoryElement(midAdd.Payload).Payload!;
        var last = api.GetStoryElement(lastAdd.Payload).Payload!;
        api.AssignElementToBeat(complication.Uuid, 0, first.Uuid);
        api.AssignElementToBeat(complication.Uuid, 1, middle.Uuid);
        api.AssignElementToBeat(complication.Uuid, 2, last.Uuid);

        var resolved = new SceneStructureNeighborResolver(api).ResolveForSceneBuilder(middle);

        Assert.AreEqual(SceneStructureNeighborResolver.SceneBuilderOwnerState.UniqueSubproblem, resolved.OwnerState);
        Assert.AreEqual(complication.Uuid, resolved.OwnerProblem!.Uuid);
        Assert.AreEqual(first.Uuid, resolved.PrecedingScene!.Uuid);
        Assert.AreEqual(last.Uuid, resolved.NextScene!.Uuid);
        Assert.IsNull(resolved.BailReason);
    }

    [TestMethod]
    public async Task EmptyExplorerParent_TwoComplications_EmptyCategoryBail()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("Two comps", "Author", "0");
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var parentAdd = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "Empty parent");
        var parent = api.GetStoryElement(parentAdd.Payload).Payload!;
        var c1Add = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "C1");
        var c2Add = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "C2");
        var c1 = (ProblemModel)api.GetStoryElement(c1Add.Payload).Payload!;
        var c2 = (ProblemModel)api.GetStoryElement(c2Add.Payload).Payload!;
        api.UpdateElementProperty(c1.Uuid, "ProblemCategory", "Complication");
        api.UpdateElementProperty(c2.Uuid, "ProblemCategory", "Complication");
        api.CreateBeat(c1.Uuid, "B", "d");
        api.CreateBeat(c2.Uuid, "B", "d");
        var scAdd = api.AddElement(StoryItemType.Scene, parent.Uuid.ToString(), "Shared");
        var scene = api.GetStoryElement(scAdd.Payload).Payload!;
        api.AssignElementToBeat(c1.Uuid, 0, scene.Uuid);
        api.AssignElementToBeat(c2.Uuid, 0, scene.Uuid);

        var resolved = new SceneStructureNeighborResolver(api).ResolveForSceneBuilder(scene);

        Assert.AreEqual(SceneStructureNeighborResolver.SceneBuilderOwnerState.EmptyCategoryBail, resolved.OwnerState);
        Assert.AreEqual(parent.Uuid, resolved.OwnerProblem!.Uuid);
        Assert.IsNull(resolved.PrecedingScene);
    }

    [TestMethod]
    public async Task MixedCaseStoryProblemCategory_Bails()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("Mixed case", "Author", "0");
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var pAdd = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "SP mixed");
        var problem = (ProblemModel)api.GetStoryElement(pAdd.Payload).Payload!;
        api.UpdateElementProperty(problem.Uuid, "ProblemCategory", "Story Problem");
        var scAdd = api.AddElement(StoryItemType.Scene, problem.Uuid.ToString(), "Child");
        var scene = api.GetStoryElement(scAdd.Payload).Payload!;

        var resolved = new SceneStructureNeighborResolver(api).ResolveForSceneBuilder(scene);

        Assert.AreEqual(SceneStructureNeighborResolver.SceneBuilderOwnerState.StoryProblemBail, resolved.OwnerState);
    }

    [TestMethod]
    public async Task FolderParent_TwoComplications_Ambiguous_NoBail()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("Ambiguous", "Author", "0");
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var folderAdd = api.AddElement(StoryItemType.Folder, overview.Uuid.ToString(), "Folder");
        var folder = api.GetStoryElement(folderAdd.Payload).Payload!;
        var c1Add = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "C1");
        var c2Add = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "C2");
        var c1 = (ProblemModel)api.GetStoryElement(c1Add.Payload).Payload!;
        var c2 = (ProblemModel)api.GetStoryElement(c2Add.Payload).Payload!;
        api.UpdateElementProperty(c1.Uuid, "ProblemCategory", "Complication");
        api.UpdateElementProperty(c2.Uuid, "ProblemCategory", "Complication");
        api.CreateBeat(c1.Uuid, "B", "d");
        api.CreateBeat(c2.Uuid, "B", "d");
        var scAdd = api.AddElement(StoryItemType.Scene, folder.Uuid.ToString(), "Shared");
        var scene = api.GetStoryElement(scAdd.Payload).Payload!;
        api.AssignElementToBeat(c1.Uuid, 0, scene.Uuid);
        api.AssignElementToBeat(c2.Uuid, 0, scene.Uuid);

        var resolved = new SceneStructureNeighborResolver(api).ResolveForSceneBuilder(scene);

        Assert.AreEqual(SceneStructureNeighborResolver.SceneBuilderOwnerState.AmbiguousSubproblems, resolved.OwnerState);
        Assert.IsNull(resolved.OwnerProblem);
        Assert.AreEqual(2, resolved.ContributingProblems.Count);
        Assert.IsNull(resolved.PrecedingScene);
        Assert.IsNull(resolved.BailReason);
    }

    [TestMethod]
    public async Task OrphanUnderFolder_OwnerStateNone_NoBail()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("Orphan", "Author", "0");
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var folderAdd = api.AddElement(StoryItemType.Folder, overview.Uuid.ToString(), "Folder");
        var folder = api.GetStoryElement(folderAdd.Payload).Payload!;
        var scAdd = api.AddElement(StoryItemType.Scene, folder.Uuid.ToString(), "Orphan");
        var scene = api.GetStoryElement(scAdd.Payload).Payload!;

        var resolved = new SceneStructureNeighborResolver(api).ResolveForSceneBuilder(scene);

        Assert.AreEqual(SceneStructureNeighborResolver.SceneBuilderOwnerState.None, resolved.OwnerState);
        Assert.IsNull(resolved.OwnerProblem);
        Assert.AreEqual(0, resolved.ContributingProblems.Count);
        Assert.IsNull(resolved.BailReason);
    }

    [TestMethod]
    public async Task ValidateSceneBuilderOwner_Orphan_NoBail()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("Orphan run", "Author", "0");
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var folderAdd = api.AddElement(StoryItemType.Folder, overview.Uuid.ToString(), "Folder");
        var folder = api.GetStoryElement(folderAdd.Payload).Payload!;
        var scAdd = api.AddElement(StoryItemType.Scene, folder.Uuid.ToString(), "Orphan");
        var scene = api.GetStoryElement(scAdd.Payload).Payload!;

        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("SceneBuilder")!, api);
        var gathered = new Dictionary<string, StoryElement> { ["Scene"] = scene };
        var gate = runner.ValidateSceneBuilderOwner(gathered);

        // An orphan Scene has no Problem map entry, which is not an empty category.
        Assert.IsNull(gate);
    }

    [TestMethod]
    public async Task OrphanAccept_ValidComplication_BindsFirstEmptyBeat()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("Bind", "Author", "0");
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var folderAdd = api.AddElement(StoryItemType.Folder, overview.Uuid.ToString(), "Folder");
        var folder = api.GetStoryElement(folderAdd.Payload).Payload!;
        var scAdd = api.AddElement(StoryItemType.Scene, folder.Uuid.ToString(), "Orphan");
        var scene = api.GetStoryElement(scAdd.Payload).Payload!;
        var cAdd = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "Home");
        var complication = (ProblemModel)api.GetStoryElement(cAdd.Payload).Payload!;
        api.UpdateElementProperty(complication.Uuid, "ProblemCategory", "Complication");
        api.CreateBeat(complication.Uuid, "B1", "d");
        api.CreateBeat(complication.Uuid, "B2", "d");

        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("SceneBuilder")!, api);
        var result = WorkflowResult.Succeeded();
        result.ProposedOwnerGuid = complication.Uuid;
        result.ProposedOwnerName = complication.Name;
        var gathered = new Dictionary<string, StoryElement> { ["Scene"] = scene };

        var msg = runner.TryApplySceneBuilderOrphanBind(result, gathered);

        Assert.IsNotNull(msg);
        StringAssert.Contains(msg, "assigned Scene");
        var structure = api.GetProblemStructure(complication.Uuid);
        Assert.AreEqual(scene.Uuid, structure.Payload.Beats.First().LinkedElement);
        Assert.AreEqual(folder.Uuid, scene.Node!.Parent!.Uuid);
    }

    [TestMethod]
    public async Task OrphanAccept_StoryProblemGuid_WritesNotesWhenEmpty()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("SP bind", "Author", "0");
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var folderAdd = api.AddElement(StoryItemType.Folder, overview.Uuid.ToString(), "Folder");
        var folder = api.GetStoryElement(folderAdd.Payload).Payload!;
        var scAdd = api.AddElement(StoryItemType.Scene, folder.Uuid.ToString(), "Orphan");
        var scene = (SceneModel)api.GetStoryElement(scAdd.Payload).Payload!;
        var spAdd = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "SP");
        var sp = (ProblemModel)api.GetStoryElement(spAdd.Payload).Payload!;
        api.UpdateElementProperty(sp.Uuid, "ProblemCategory", "Story problem");
        api.UpdateElementProperty(overview.Uuid, "StoryProblem", sp.Uuid);
        api.CreateBeat(sp.Uuid, "B1", "d");

        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("SceneBuilder")!, api);
        var result = WorkflowResult.Succeeded();
        result.ProposedOwnerGuid = sp.Uuid;
        var gathered = new Dictionary<string, StoryElement> { ["Scene"] = scene };

        var msg = runner.TryApplySceneBuilderOrphanBind(result, gathered);

        StringAssert.Contains(msg, "could not bind");
        var structure = api.GetProblemStructure(sp.Uuid);
        var linked = structure.Payload.Beats.First().LinkedElement;
        Assert.IsTrue(linked is not Guid assigned || assigned == Guid.Empty);
        Assert.IsFalse(string.IsNullOrEmpty(((SceneModel)api.GetStoryElement(scene.Uuid).Payload!).Notes));
    }

    [TestMethod]
    public async Task ScenePurpose_LiveListNotTouched_Protect()
    {
        var api = CreateApi();
        var fx = await BuildThreeSceneComplication(api);
        fx.Middle.ScenePurpose.Add("Reveal");
        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("SceneBuilder")!, api);
        var pending = new PendingUpdate(
            "Scene",
            fx.Middle.Uuid,
            new PropertySpec("ScenePurpose", WriteVia.SimpleList, ListEntryType: typeof(string)),
            new List<string> { "Turn" });
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(pending);

        runner.ClassifyScalarUpdates(result, new HashSet<string>(), "SceneBuilder");

        Assert.AreEqual(1, result.PendingUpdates.Count);
        Assert.AreEqual(UpdateKind.Protect, result.PendingUpdates[0].Kind);
    }

    [TestMethod]
    public async Task ScenePurpose_EmptyLive_Fill()
    {
        var api = CreateApi();
        var fx = await BuildThreeSceneComplication(api);
        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("SceneBuilder")!, api);
        var pending = new PendingUpdate(
            "Scene",
            fx.Middle.Uuid,
            new PropertySpec("ScenePurpose", WriteVia.SimpleList, ListEntryType: typeof(string)),
            new List<string> { "Reveal" });
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(pending);

        runner.ClassifyScalarUpdates(result, new HashSet<string>(), "SceneBuilder");

        Assert.AreEqual(UpdateKind.Fill, result.PendingUpdates[0].Kind);
    }

    [TestMethod]
    public async Task NonSceneBuilderWorkflow_ScenePurpose_StaysUnclassified()
    {
        var api = CreateApi();
        var fx = await BuildThreeSceneComplication(api);
        fx.Middle.ScenePurpose.Add("Reveal");
        // #116 Fill/Protect classification is SceneBuilder-only. #211 deleted SceneDevelopment,
        // which used to stand in for "some other workflow"; StoryProblem serves the same purpose.
        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("StoryProblem")!, api);
        var pending = new PendingUpdate(
            "Scene",
            fx.Middle.Uuid,
            new PropertySpec("ScenePurpose", WriteVia.SimpleList, ListEntryType: typeof(string)),
            new List<string> { "Turn" });
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(pending);

        runner.ClassifyScalarUpdates(result, new HashSet<string>(), "StoryProblem");

        Assert.AreEqual(UpdateKind.Unclassified, result.PendingUpdates[0].Kind);
    }

    [TestMethod]
    public void NormalizeCompareText_RtfApostropheAndLineBreak_MatchesPlain()
    {
        var rtf = @"{\rtf1\ansi{\fonttbl{\f0 Segoe UI;}}\pard Leonard\rquote s badge-out approach.\par The trap is real.\par}";
        var plain = "Leonard's badge-out approach. The trap is real.";

        Assert.AreEqual(
            WorkflowRunner.NormalizeCompareText(plain),
            WorkflowRunner.NormalizeCompareText(rtf),
            ignoreCase: true);
    }

    [TestMethod]
    public async Task Classify_SameVisibleWordsAsRtfLive_DropsAsNoOp()
    {
        var api = CreateApi();
        var fx = await BuildThreeSceneComplication(api);
        var rtf = @"{\rtf1\ansi{\fonttbl{\f0 Segoe UI;}}\pard Leonard\rquote s badge-out approach.\par}";
        api.UpdateElementProperty(fx.Middle.Uuid, "Description", rtf);

        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("SceneBuilder")!, api);
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(new PendingUpdate(
            "Scene",
            fx.Middle.Uuid,
            new PropertySpec("Description"),
            "Leonard's badge-out approach."));

        runner.ClassifyScalarUpdates(result, new HashSet<string>(), "SceneBuilder");

        Assert.AreEqual(0, result.PendingUpdates.Count);
    }

    [TestMethod]
    public async Task Classify_EmptyProposedVsFilled_DropsAsNoOp()
    {
        var api = CreateApi();
        var fx = await BuildThreeSceneComplication(api);
        api.UpdateElementProperty(fx.Middle.Uuid, "Description", "Leonard walks in.");

        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("SceneBuilder")!, api);
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(new PendingUpdate(
            "Scene",
            fx.Middle.Uuid,
            new PropertySpec("Description"),
            string.Empty));

        runner.ClassifyScalarUpdates(result, new HashSet<string>(), "SceneBuilder");

        Assert.AreEqual(0, result.PendingUpdates.Count);
    }

    [TestMethod]
    public async Task Classify_ShootoutRtfEcho_DropsAsNoOp()
    {
        var api = CreateApi();
        var fx = await BuildThreeSceneComplication(api);
        var rtf =
            @"{\rtf1\ansi{\fonttbl{\f0 Segoe UI;}}\pard Leonard and Tony slowly approach a housing construction site in Fall Creek, a house Charlie Lacas is having built.\par}";
        api.UpdateElementProperty(fx.Middle.Uuid, "Description", rtf);
        var echo = new RichTextStripper().StripRichTextFormat(rtf);

        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("SceneBuilder")!, api);
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(new PendingUpdate(
            "Scene",
            fx.Middle.Uuid,
            new PropertySpec("Description"),
            echo));

        runner.ClassifyScalarUpdates(result, new HashSet<string>(), "SceneBuilder");

        Assert.AreEqual(0, result.PendingUpdates.Count);
    }

    [TestMethod]
    public async Task Classify_DifferentWords_StaysProtect()
    {
        var api = CreateApi();
        var fx = await BuildThreeSceneComplication(api);
        api.UpdateElementProperty(fx.Middle.Uuid, "Description", "Leonard walks in.");

        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("SceneBuilder")!, api);
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(new PendingUpdate(
            "Scene",
            fx.Middle.Uuid,
            new PropertySpec("Description"),
            "Leonard rolls up to the site."));

        runner.ClassifyScalarUpdates(result, new HashSet<string>(), "SceneBuilder");

        Assert.AreEqual(1, result.PendingUpdates.Count);
        Assert.AreEqual(UpdateKind.Protect, result.PendingUpdates[0].Kind);
    }

    [TestMethod]
    public async Task ApplySeats_AddsProtagonistAntagonistAndViewpointToCast()
    {
        var api = CreateApi();
        var fx = await BuildThreeSceneComplication(api);
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var protAdd = api.AddElement(StoryItemType.Character, overview.Uuid.ToString(), "Prot");
        var antAdd = api.AddElement(StoryItemType.Character, overview.Uuid.ToString(), "Antag");
        var vpAdd = api.AddElement(StoryItemType.Character, overview.Uuid.ToString(), "VP");
        var prot = api.GetStoryElement(protAdd.Payload).Payload!;
        var antag = api.GetStoryElement(antAdd.Payload).Payload!;
        var vp = api.GetStoryElement(vpAdd.Payload).Payload!;

        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("SceneBuilder")!, api);
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(new PendingUpdate("Scene", fx.Middle.Uuid, new PropertySpec("Protagonist"), prot.Uuid.ToString()));
        result.PendingUpdates.Add(new PendingUpdate("Scene", fx.Middle.Uuid, new PropertySpec("Antagonist"), antag.Uuid.ToString()));
        result.PendingUpdates.Add(new PendingUpdate("Scene", fx.Middle.Uuid, new PropertySpec("ViewpointCharacter"), vp.Uuid.ToString()));
        var gathered = new Dictionary<string, StoryElement> { ["Scene"] = fx.Middle };

        runner.ApplyUpdates(result, gathered);

        var scene = (SceneModel)api.GetStoryElement(fx.Middle.Uuid).Payload!;
        CollectionAssert.Contains(scene.CastMembers, prot.Uuid);
        CollectionAssert.Contains(scene.CastMembers, antag.Uuid);
        CollectionAssert.Contains(scene.CastMembers, vp.Uuid);
    }

    [TestMethod]
    public async Task InventedProtagonistGuid_Dropped()
    {
        var api = CreateApi();
        var fx = await BuildThreeSceneComplication(api);
        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("SceneBuilder")!, api);
        var pending = new PendingUpdate(
            "Scene",
            fx.Middle.Uuid,
            new PropertySpec("Protagonist"),
            Guid.NewGuid().ToString());
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(pending);

        runner.ClassifyScalarUpdates(result, new HashSet<string>(), "SceneBuilder");

        Assert.AreEqual(0, result.PendingUpdates.Count);
        Assert.IsTrue(result.StatusMessages.Any(m => m.Contains("dropped")));
    }

    private static async Task<ThreeSceneFixture> BuildThreeSceneComplication(StoryCADApi api)
    {
        var create = await api.CreateEmptyOutline("Three scenes", "Author", "0");
        Assert.IsTrue(create.IsSuccess);
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var spAdd = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "Story Problem");
        var sp = (ProblemModel)api.GetStoryElement(spAdd.Payload).Payload!;
        api.UpdateElementProperty(sp.Uuid, "ProblemCategory", "Story problem");
        api.UpdateElementProperty(overview.Uuid, "StoryProblem", sp.Uuid);

        var cAdd = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "Complication");
        var complication = (ProblemModel)api.GetStoryElement(cAdd.Payload).Payload!;
        api.UpdateElementProperty(complication.Uuid, "ProblemCategory", "Complication");
        api.CreateBeat(complication.Uuid, "B1", "d");
        api.CreateBeat(complication.Uuid, "B2", "d");
        api.CreateBeat(complication.Uuid, "B3", "d");

        var firstAdd = api.AddElement(StoryItemType.Scene, complication.Uuid.ToString(), "First");
        var midAdd = api.AddElement(StoryItemType.Scene, complication.Uuid.ToString(), "Middle");
        var lastAdd = api.AddElement(StoryItemType.Scene, complication.Uuid.ToString(), "Last");
        var first = (SceneModel)api.GetStoryElement(firstAdd.Payload).Payload!;
        var middle = (SceneModel)api.GetStoryElement(midAdd.Payload).Payload!;
        var last = (SceneModel)api.GetStoryElement(lastAdd.Payload).Payload!;
        api.AssignElementToBeat(complication.Uuid, 0, first.Uuid);
        api.AssignElementToBeat(complication.Uuid, 1, middle.Uuid);
        api.AssignElementToBeat(complication.Uuid, 2, last.Uuid);

        return new ThreeSceneFixture(sp, complication, first, middle, last);
    }

    private sealed record ThreeSceneFixture(
        ProblemModel StoryProblem,
        ProblemModel Complication,
        SceneModel First,
        SceneModel Middle,
        SceneModel Last);
}
