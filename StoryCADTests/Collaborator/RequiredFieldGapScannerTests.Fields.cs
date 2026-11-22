using CollaboratorLib.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #261: Setting Locale and Period, the Story World, the trash, and link fields
/// that point at an element in the trash.
/// </summary>
public partial class RequiredFieldGapScannerTests
{
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
