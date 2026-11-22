using CollaboratorLib.Context;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #160 / #107: Problem spine essentials; Character essentials include BackStory.
/// Flaw is not gap-required.
/// </summary>
[TestClass]
public class RequiredFieldGapScannerTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    [TestMethod]
    public void MissingFieldCount_SumsFieldsAcrossElements_NotElements()
    {
        // Collaborator #237 item 10: the nav row read "Outline gaps (1)" for an Overview with
        // five empty fields, because it counted elements. The page lists fields, so the count
        // is fields.
        var api = CreateApi();
        var model = new StoryModel();
        api.CurrentModel = model;
        _ = new OverviewModel("Scorecard", model, null); // the base constructor registers it

        var details = RequiredFieldGapScanner.FindGapDetails(api, model);

        Assert.AreEqual(1, details.Count, "one element has gaps");
        Assert.AreEqual(details[0].MissingProperties.Count, RequiredFieldGapScanner.MissingFieldCount(details));
        Assert.IsTrue(RequiredFieldGapScanner.MissingFieldCount(details) > 1, "the Overview has more than one empty field");
        Assert.AreEqual(0, RequiredFieldGapScanner.MissingFieldCount(Array.Empty<GapDetail>()));
    }

    [TestMethod]
    public void Problem_BlankScalars_Includes_ProblemType_ConflictType_Subject()
    {
        var api = CreateApi();
        var model = new StoryModel();
        var problem = new ProblemModel("Test Problem", model, null)
        {
            Description = "Story question text",
            // leave ProblemType, ConflictType, Subject, GMC, etc. empty
        };

        var missing = RequiredFieldGapScanner.GetMissingProperties(api, problem);

        CollectionAssert.Contains(missing.ToList(), "ProblemType");
        CollectionAssert.Contains(missing.ToList(), "ConflictType");
        CollectionAssert.Contains(missing.ToList(), "Subject");
    }

    [TestMethod]
    public void Problem_FilledNewFields_DoesNotReportThemMissing()
    {
        var api = CreateApi();
        var model = new StoryModel();
        var problem = new ProblemModel("Test Problem", model, null)
        {
            Description = "Will they succeed?",
            ProblemCategory = "Story problem",
            ProblemType = "Conflict",
            ConflictType = "Person vs. Self",
            Subject = "Ambition",
            Premise = "A man destroys himself for power",
            ProtGoal = "Kingship",
            ProtMotive = "Prophecy",
            ProtConflict = "Conscience",
            AntagGoal = "Kingship",
            AntagMotive = "Fear",
            AntagConflict = "Conscience",
            Outcome = "Both die"
        };

        var missing = RequiredFieldGapScanner.GetMissingProperties(api, problem);

        CollectionAssert.DoesNotContain(missing.ToList(), "ProblemType");
        CollectionAssert.DoesNotContain(missing.ToList(), "ConflictType");
        CollectionAssert.DoesNotContain(missing.ToList(), "Subject");
        // Links still empty → still a gap
        CollectionAssert.Contains(missing.ToList(), "Protagonist");
        CollectionAssert.Contains(missing.ToList(), "Antagonist");
    }

    [TestMethod]
    public void Character_Requires_BackStory_Not_Flaw()
    {
        var api = CreateApi();
        var model = new StoryModel();
        var character = new CharacterModel("Macbeth", model, null)
        {
            Description = "Thane",
            Role = "General",
            StoryRole = "Protagonist",
            Age = "38",
            Sex = "Male",
            Appearance = "Weathered"
            // Flaw / BackStory intentionally empty
        };

        var missing = RequiredFieldGapScanner.GetMissingProperties(api, character);

        CollectionAssert.Contains(missing.ToList(), "BackStory");
        CollectionAssert.DoesNotContain(missing.ToList(), "Flaw");
    }

    [TestMethod]
    public void Character_ThinSheet_Reports_Age_Sex_Appearance_BackStory()
    {
        var api = CreateApi();
        var model = new StoryModel();
        var character = new CharacterModel("Macbeth", model, null)
        {
            Description = "Thane",
            Role = "General",
            StoryRole = "Protagonist"
        };

        var missing = RequiredFieldGapScanner.GetMissingProperties(api, character);

        CollectionAssert.Contains(missing.ToList(), "Age");
        CollectionAssert.Contains(missing.ToList(), "Sex");
        CollectionAssert.Contains(missing.ToList(), "Appearance");
        CollectionAssert.Contains(missing.ToList(), "BackStory");
        CollectionAssert.DoesNotContain(missing.ToList(), "Flaw");
        CollectionAssert.DoesNotContain(missing.ToList(), "Archetype");
        CollectionAssert.DoesNotContain(missing.ToList(), "Enneagram");
        CollectionAssert.DoesNotContain(missing.ToList(), "Economic");
    }

    [TestMethod]
    public void Character_EssentialEightFilled_DoesNotReportCharacterGaps()
    {
        var api = CreateApi();
        var model = new StoryModel();
        var character = new CharacterModel("Macbeth", model, null)
        {
            Description = "Thane",
            Role = "General",
            StoryRole = "Protagonist",
            Age = "38",
            Sex = "Male",
            Appearance = "Weathered",
            BackStory = "The witches spoke first"
        };

        var missing = RequiredFieldGapScanner.GetMissingProperties(api, character);

        Assert.AreEqual(0, missing.Count, string.Join(", ", missing));
        CollectionAssert.DoesNotContain(missing.ToList(), "Flaw");
    }

    [TestMethod]
    public void GapOwnership_NewCharacterFields_PointToSurfaces()
    {
        foreach (var prop in new[] { "Age", "Sex", "Appearance" })
        {
            var owners = GapWorkflowOwnership.WorkflowsFor(StoryItemType.Character, prop);
            Assert.AreEqual(1, owners.Count, prop);
            Assert.AreEqual("CharacterBuilder", owners[0], prop);
        }

        var backStory = GapWorkflowOwnership.WorkflowsFor(StoryItemType.Character, "BackStory");
        Assert.AreEqual(1, backStory.Count);
        Assert.AreEqual("FlawBackstory", backStory[0]);
        Assert.AreEqual("Backstory",
            GapWorkflowOwnership.DisplayLabel(StoryItemType.Character, "BackStory"));
        Assert.AreEqual(0,
            GapWorkflowOwnership.WorkflowsFor(StoryItemType.Character, "Name").Count);
    }

    [TestMethod]
    public void GapOwnership_NewProblemFields_OfferProblemBuilderThenStoryProblem()
    {
        // Collaborator #77: ProblemBuilder fills the Problem spine and is offered first.
        // A gap means the Problem already exists, and StoryProblem is the premise-to-Problem
        // creation path, so it is the weaker suggestion here. It stays listed while it is
        // registered for A:B.
        foreach (var prop in new[] { "ProblemType", "ConflictType", "Subject" })
        {
            var owners = GapWorkflowOwnership.WorkflowsFor(StoryItemType.Problem, prop);
            Assert.AreEqual(2, owners.Count, prop);
            Assert.AreEqual("ProblemBuilder", owners[0], prop);
            Assert.AreEqual("StoryProblem", owners[1], prop);
        }

        Assert.AreEqual("Problem Type",
            GapWorkflowOwnership.DisplayLabel(StoryItemType.Problem, "ProblemType"));
        Assert.AreEqual("Conflict Type",
            GapWorkflowOwnership.DisplayLabel(StoryItemType.Problem, "ConflictType"));
        Assert.AreEqual("Subject",
            GapWorkflowOwnership.DisplayLabel(StoryItemType.Problem, "Subject"));
    }

    // Collaborator #261: missing pieces (a Problem with no Scene in its beat tree, a Character in no Problem), the trash rule, and the gap number.

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
        var api = await DevelopmentGuessTests.LoopOutline();
        var problemA = api.CurrentModel!.StoryElements.OfType<ProblemModel>().First(p => p.Name == "ProblemA");

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
        // "Beta" sorts before "alpha" when case counts, and after it when case is ignored.
        var api = await BlankOutline();
        Add<ProblemModel>(api, StoryItemType.Problem, "Beta");
        Add<ProblemModel>(api, StoryItemType.Problem, "alpha");
        var unnamed = Add<ProblemModel>(api, StoryItemType.Problem, "x");
        unnamed.Name = string.Empty;

        var names = RequiredFieldGapScanner.FindMissingPieces(api.CurrentModel!)
            .Where(p => p.ElementType == StoryItemType.Problem)
            .Select(p => p.ElementName)
            .ToList();

        CollectionAssert.AreEqual(new[] { "(unnamed)", "alpha", "Beta" }, names);
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

    // Collaborator #261: Setting Locale and Period, the Story World, the trash, and link fields that point at an element in the trash.

    [TestMethod]
    public async Task FindGapDetails_SettingInTrash_DoesNotGiveSetting()
    {
        var api = await BlankOutline();
        var setting = Add<SettingModel>(api, StoryItemType.Setting, "Inverness");
        Trash(api, setting);

        var details = RequiredFieldGapScanner.FindGapDetails(api, api.CurrentModel!);

        Assert.IsFalse(details.Any(d => d.ElementGuid == setting.Uuid));
    }

    [TestMethod]
    public async Task FindGapDetails_StoryWorldInTrash_DoesNotGiveStoryWorld()
    {
        // (regression)
        var api = await BlankOutline();
        var world = Add<StoryWorldModel>(api, StoryItemType.StoryWorld, "Scotland");
        Trash(api, world);

        var details = RequiredFieldGapScanner.FindGapDetails(api, api.CurrentModel!);

        Assert.IsFalse(details.Any(d => d.ElementGuid == world.Uuid));
    }

    [TestMethod]
    [DataRow("Locale")]
    [DataRow("Period")]
    public async Task FindGapDetails_SettingWithEmptyField_GivesFieldGap(string property)
    {
        var api = await BlankOutline();
        var setting = Add<SettingModel>(api, StoryItemType.Setting, "Inverness");
        setting.Description = "A castle";
        setting.Locale = property == "Locale" ? string.Empty : "Highlands";
        setting.Period = property == "Period" ? string.Empty : "1040";

        var detail = RequiredFieldGapScanner.FindGapDetails(api, api.CurrentModel!).Single(d => d.ElementGuid == setting.Uuid);

        CollectionAssert.AreEqual(new[] { property }, detail.MissingProperties.ToList());
    }

    [TestMethod]
    public async Task FindGapDetails_NoStoryWorld_GivesNoStoryWorld()
    {
        // (regression)
        var api = await BlankOutline();

        var details = RequiredFieldGapScanner.FindGapDetails(api, api.CurrentModel!);

        Assert.IsFalse(details.Any(d => d.ElementType == StoryItemType.StoryWorld));
    }

    [TestMethod]
    [DataRow("WorldType")]
    [DataRow("Description")]
    [DataRow("Name")]
    public async Task FindGapDetails_StoryWorldWithEmptyField_GivesFieldGap(string property)
    {
        var api = await BlankOutline();
        var world = Add<StoryWorldModel>(api, StoryItemType.StoryWorld, "Scotland");
        world.Name = property == "Name" ? string.Empty : "Scotland";
        world.Description = property == "Description" ? string.Empty : "Thanes feud.";
        world.WorldType = property == "WorldType" ? string.Empty : "Consensus Reality";

        var detail = RequiredFieldGapScanner.FindGapDetails(api, api.CurrentModel!).Single(d => d.ElementGuid == world.Uuid);

        CollectionAssert.AreEqual(new[] { property }, detail.MissingProperties.ToList());
    }

    [TestMethod]
    public async Task FindGapDetails_SettingStoryWorldScene_GivesStoryWorldBetween()
    {
        var api = await BlankOutline();
        var scene = Add<SceneModel>(api, StoryItemType.Scene, "Battle");
        var world = Add<StoryWorldModel>(api, StoryItemType.StoryWorld, "Scotland");
        var setting = Add<SettingModel>(api, StoryItemType.Setting, "Inverness");

        var order = RequiredFieldGapScanner.FindGapDetails(api, api.CurrentModel!).Select(d => d.ElementGuid).ToList();

        Assert.IsTrue(order.IndexOf(setting.Uuid) < order.IndexOf(world.Uuid));
        Assert.IsTrue(order.IndexOf(world.Uuid) < order.IndexOf(scene.Uuid));
    }

    [TestMethod]
    public async Task FindGapGuids_SettingWithEmptyLocale_GivesSetting()
    {
        var api = await BlankOutline();
        var setting = Add<SettingModel>(api, StoryItemType.Setting, "Inverness");
        setting.Description = "A castle";
        setting.Period = "1040";

        CollectionAssert.Contains(RequiredFieldGapScanner.FindGapGuids(api, api.CurrentModel!).ToList(), setting.Uuid);
    }

    [TestMethod]
    public async Task FindGapGuids_SettingInTrash_DoesNotGiveSetting()
    {
        var api = await BlankOutline();
        var setting = Add<SettingModel>(api, StoryItemType.Setting, "Inverness");
        Trash(api, setting);

        CollectionAssert.DoesNotContain(RequiredFieldGapScanner.FindGapGuids(api, api.CurrentModel!).ToList(), setting.Uuid);
    }

    [TestMethod]
    public async Task FindGapDetails_ProtagonistInTrash_GivesProtagonistFieldGap()
    {
        var api = await BlankOutline();
        var problem = Add<ProblemModel>(api, StoryItemType.Problem, "Crown");
        var character = Add<CharacterModel>(api, StoryItemType.Character, "Macbeth");
        Trash(api, character);
        problem.Protagonist = character.Uuid;

        var detail = RequiredFieldGapScanner.FindGapDetails(api, api.CurrentModel!).Single(d => d.ElementGuid == problem.Uuid);

        CollectionAssert.Contains(detail.MissingProperties.ToList(), "Protagonist");
    }
}
