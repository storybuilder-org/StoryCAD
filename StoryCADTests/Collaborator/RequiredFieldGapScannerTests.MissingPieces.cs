using CollaboratorLib.Context;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #261: missing pieces (a Problem with no Scene in its beat tree, a Character in
/// no Problem), the trash rule, and the gap number.
/// </summary>
public partial class RequiredFieldGapScannerTests
{
    [TestMethod]
    public async Task FindMissingPieces_ProblemWithNoScene_GivesProblem()
    {
        var api = await BlankOutline();
        var problem = Add<ProblemModel>(api, StoryItemType.Problem, "Crown");

        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        Assert.IsTrue(pieces.Any(p => p.ElementGuid == problem.Uuid));
    }

    [TestMethod]
    public async Task FindMissingPieces_ProblemWithNoScene_LinksToProblemBeats()
    {
        var api = await BlankOutline();
        var problem = Add<ProblemModel>(api, StoryItemType.Problem, "Crown");

        var piece = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!).Single(p => p.ElementGuid == problem.Uuid);

        Assert.AreEqual("ProblemBeats", piece.WorkflowLabel);
    }

    [TestMethod]
    public async Task FindMissingPieces_SceneOnBeatOfProblemBelow_DoesNotGiveUpperProblem()
    {
        var api = await BlankOutline();
        var problemA = Add<ProblemModel>(api, StoryItemType.Problem, "ProblemA");
        var problemB = Add<ProblemModel>(api, StoryItemType.Problem, "ProblemB");
        var scene = Add<SceneModel>(api, StoryItemType.Scene, "Battle");
        AddBeats(api, problemA, 1);
        AddBeats(api, problemB, 1);
        PutOnBeat(api, problemB, 0, problemA);
        PutOnBeat(api, problemA, 0, scene);

        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        Assert.IsFalse(pieces.Any(p => p.ElementGuid == problemB.Uuid));
    }

    [TestMethod]
    public async Task FindMissingPieces_SceneInTrashOnBeat_GivesProblem()
    {
        var api = await BlankOutline();
        var problem = Add<ProblemModel>(api, StoryItemType.Problem, "Crown");
        var scene = Add<SceneModel>(api, StoryItemType.Scene, "Battle");
        AddBeats(api, problem, 1);
        PutOnBeat(api, problem, 0, scene);
        Trash(api, scene);
        problem.StructureBeats[0].Guid = scene.Uuid; // a file made another way can hold this

        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        Assert.IsTrue(pieces.Any(p => p.ElementGuid == problem.Uuid));
    }

    [TestMethod]
    public async Task HasSceneOnBeats_LoopOfProblems_GivesFalse()
    {
        var api = await BlankOutline();
        var problemA = Add<ProblemModel>(api, StoryItemType.Problem, "ProblemA");
        var problemB = Add<ProblemModel>(api, StoryItemType.Problem, "ProblemB");
        AddBeats(api, problemA, 1);
        AddBeats(api, problemB, 1);
        PutOnBeat(api, problemA, 0, problemB);
        problemB.StructureBeats[0].Guid = problemA.Uuid; // the loop, past the one-parent rule

        Assert.IsFalse(RequiredFieldGapScanner.HasSceneOnBeats(problemA, api.CurrentModel!));
    }

    [TestMethod]
    public async Task FindMissingPieces_CharacterInNoProblem_GivesCharacter()
    {
        var api = await BlankOutline();
        var character = Add<CharacterModel>(api, StoryItemType.Character, "Banquo");

        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        Assert.IsTrue(pieces.Any(p => p.ElementGuid == character.Uuid));
    }

    [TestMethod]
    public async Task FindMissingPieces_CharacterInNoProblem_LinksToProblemBuilder()
    {
        var api = await BlankOutline();
        var character = Add<CharacterModel>(api, StoryItemType.Character, "Banquo");

        var piece = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!).Single(p => p.ElementGuid == character.Uuid);

        Assert.AreEqual("ProblemBuilder", piece.WorkflowLabel);
    }

    [TestMethod]
    [DataRow("Minor Role")]
    [DataRow("Supporting Role")]
    [DataRow("Service Role")]
    [DataRow(" minor role ")]
    public async Task FindMissingPieces_CharacterWithLesserStoryRole_DoesNotGiveCharacter(string storyRole)
    {
        var api = await BlankOutline();
        var character = Add<CharacterModel>(api, StoryItemType.Character, "Porter");
        character.StoryRole = storyRole;

        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        Assert.IsFalse(pieces.Any(p => p.ElementGuid == character.Uuid));
    }

    [TestMethod]
    [DataRow("Major Role")]
    [DataRow("Villain")]
    public async Task FindMissingPieces_CharacterWithOtherStoryRole_GivesCharacter(string storyRole)
    {
        var api = await BlankOutline();
        var character = Add<CharacterModel>(api, StoryItemType.Character, "Banquo");
        character.StoryRole = storyRole;

        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        Assert.IsTrue(pieces.Any(p => p.ElementGuid == character.Uuid));
    }

    [TestMethod]
    public async Task FindMissingPieces_ProtagonistOfAvailableProblem_DoesNotGiveProtagonist()
    {
        var api = await BlankOutline();
        var problem = Add<ProblemModel>(api, StoryItemType.Problem, "Crown");
        var character = Add<CharacterModel>(api, StoryItemType.Character, "Macbeth");
        problem.Protagonist = character.Uuid;

        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        Assert.IsFalse(pieces.Any(p => p.ElementGuid == character.Uuid));
    }

    [TestMethod]
    public async Task FindMissingPieces_ProblemInTrash_GivesItsProtagonist()
    {
        var api = await BlankOutline();
        var problem = Add<ProblemModel>(api, StoryItemType.Problem, "Crown");
        var character = Add<CharacterModel>(api, StoryItemType.Character, "Macbeth");
        problem.Protagonist = character.Uuid;
        Trash(api, problem);

        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        Assert.IsTrue(pieces.Any(p => p.ElementGuid == character.Uuid));
    }

    [TestMethod]
    public async Task FindMissingPieces_ElementsInTrash_DoesNotGiveThem()
    {
        var api = await BlankOutline();
        var problem = Add<ProblemModel>(api, StoryItemType.Problem, "Crown");
        var character = Add<CharacterModel>(api, StoryItemType.Character, "Banquo");
        Trash(api, problem);
        Trash(api, character);

        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        Assert.IsFalse(pieces.Any(p => p.ElementGuid == problem.Uuid || p.ElementGuid == character.Uuid));
    }

    [TestMethod]
    public async Task FindMissingPieces_OverviewWithNoStoryProblem_DoesNotGiveOverview()
    {
        var api = await BlankOutline();
        var overview = Overview(api);
        overview.StoryProblem = Guid.Empty;

        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        Assert.IsFalse(pieces.Any(p => p.ElementGuid == overview.Uuid));
    }

    [TestMethod]
    public async Task FindGapDetails_OverviewWithNoStoryProblem_GivesStoryProblemFieldGap()
    {
        // (regression)
        var api = await BlankOutline();
        var overview = Overview(api);
        overview.StoryProblem = Guid.Empty;

        var detail = RequiredFieldGapScanner.FindGapDetails(api, api.CurrentModel!).Single(d => d.ElementGuid == overview.Uuid);

        CollectionAssert.Contains(detail.MissingProperties.ToList(), "StoryProblem");
    }

    [TestMethod]
    public async Task FindMissingPieces_SceneOnNoBeat_DoesNotGiveScene()
    {
        var api = await BlankOutline();
        var scene = Add<SceneModel>(api, StoryItemType.Scene, "Battle");

        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        Assert.IsFalse(pieces.Any(p => p.ElementGuid == scene.Uuid));
    }

    [TestMethod]
    public async Task FindMissingPieces_ProblemNames_SortsIgnoringCaseWithUnnamedFirst()
    {
        var api = await BlankOutline();
        Add<ProblemModel>(api, StoryItemType.Problem, "beta");
        Add<ProblemModel>(api, StoryItemType.Problem, "Alpha");
        var unnamed = Add<ProblemModel>(api, StoryItemType.Problem, "x");
        unnamed.Name = string.Empty;

        var names = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!)
            .Where(p => p.ElementType == StoryItemType.Problem)
            .Select(p => p.ElementName)
            .ToList();

        CollectionAssert.AreEqual(new[] { "(unnamed)", "Alpha", "beta" }, names);
    }

    [TestMethod]
    public async Task FindMissingPieces_ProblemAndCharacter_GivesProblemFirst()
    {
        var api = await BlankOutline();
        Add<CharacterModel>(api, StoryItemType.Character, "Aaron");
        Add<ProblemModel>(api, StoryItemType.Problem, "Zeal");

        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        Assert.AreEqual(StoryItemType.Problem, pieces[0].ElementType);
    }

    [TestMethod]
    public async Task GapCount_FieldGapsAndMissingPieces_GivesSum()
    {
        var api = await BlankOutline();
        Add<ProblemModel>(api, StoryItemType.Problem, "Crown");
        Add<CharacterModel>(api, StoryItemType.Character, "Banquo");
        var details = RequiredFieldGapScanner.FindGapDetails(api, api.CurrentModel!);
        var pieces = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!);

        var count = RequiredFieldGapScanner.GapCount(details, pieces);

        Assert.AreEqual(RequiredFieldGapScanner.MissingFieldCount(details) + pieces.Count, count);
    }

    [TestMethod]
    public void GapCount_OneMissingPieceNoFieldGap_GivesOne()
    {
        var pieces = new[] { new MissingPiece { ElementName = "Banquo", ElementType = StoryItemType.Character } };

        Assert.AreEqual(1, RequiredFieldGapScanner.GapCount(Array.Empty<GapDetail>(), pieces));
    }

    // ---- helpers ----

    internal static async Task<StoryCADApi> BlankOutline()
    {
        var api = CreateApi();
        var create = await api.CreateEmptyOutline("Gaps 261", "Author", "0");
        Assert.IsTrue(create.IsSuccess, create.ErrorMessage);
        return api;
    }

    internal static OverviewModel Overview(StoryCADApi api) =>
        api.CurrentModel!.StoryElements.OfType<OverviewModel>().First();

    internal static T Add<T>(StoryCADApi api, StoryItemType type, string name) where T : StoryElement
    {
        var add = api.AddElement(type, Overview(api).Uuid.ToString(), name);
        Assert.IsTrue(add.IsSuccess, add.ErrorMessage);
        return (T)api.GetStoryElement(add.Payload).Payload!;
    }

    internal static void Trash(StoryCADApi api, StoryElement element) =>
        Ioc.Default.GetRequiredService<OutlineService>().MoveToTrash(element, api.CurrentModel!);

    internal static void AddBeats(StoryCADApi api, ProblemModel problem, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var beat = api.CreateBeat(problem.Uuid, $"Beat {i + 1}", string.Empty);
            Assert.IsTrue(beat.IsSuccess, beat.ErrorMessage);
        }
    }

    internal static void PutOnBeat(StoryCADApi api, ProblemModel problem, int index, StoryElement element)
    {
        var assign = api.AssignElementToBeat(problem.Uuid, index, element.Uuid);
        Assert.IsTrue(assign.IsSuccess, assign.ErrorMessage);
    }
}
