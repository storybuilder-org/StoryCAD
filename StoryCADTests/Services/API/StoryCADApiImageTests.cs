using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.Services.API;

/// <summary>
///     Tests the dedicated image API surface (AddImage/RemoveImage/UpdateImageCaption)
///     that wraps <see cref="OutlineService"/>, mirroring the AddCastMember pattern.
/// </summary>
[TestClass]
public class StoryCADApiImageTests
{
    private readonly StoryCADApi _api = new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static StoryImage SampleImage(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Convert.ToBase64String(new byte[] { 5, 6, 7, 8 }), "image/png", "hero.png")
        {
            Caption = "Lead"
        };

    private async Task<Guid> CreateOutlineAndGetCharacterGuid()
    {
        var createResult = await _api.CreateEmptyOutline("Test Outline", "Author", "0");
        Assert.IsTrue(createResult.IsSuccess, "Expected outline creation to succeed.");
        var overview = _api.CurrentModel.StoryElements
            .First(e => e.ElementType == StoryItemType.StoryOverview);
        var add = _api.AddElement(StoryItemType.Character, overview.Uuid.ToString(), "Hero");
        Assert.IsTrue(add.IsSuccess, $"Expected Character add to succeed: {add.ErrorMessage}");
        return add.Payload;
    }

    private async Task<Guid> CreateOutlineAndGetProblemGuid()
    {
        var createResult = await _api.CreateEmptyOutline("Test Outline", "Author", "0");
        Assert.IsTrue(createResult.IsSuccess, "Expected outline creation to succeed.");
        var overview = _api.CurrentModel.StoryElements
            .First(e => e.ElementType == StoryItemType.StoryOverview);
        var add = _api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "Conflict");
        Assert.IsTrue(add.IsSuccess, $"Expected Problem add to succeed: {add.ErrorMessage}");
        return add.Payload;
    }

    // ----- AddImage -----

    [TestMethod]
    public async Task AddImage_ValidElement_SucceedsAndPersists()
    {
        var characterGuid = await CreateOutlineAndGetCharacterGuid();
        var image = SampleImage();

        var result = _api.AddImage(characterGuid, image);

        Assert.IsTrue(result.IsSuccess, $"Expected success, got: {result.ErrorMessage}");
        var character = (CharacterModel)_api.CurrentModel.StoryElements.StoryElementGuids[characterGuid];
        Assert.AreEqual(1, character.Images.Count);
        Assert.AreEqual(image.Id, character.Images[0].Id);
        Assert.IsTrue(_api.CurrentModel.Changed, "AddImage should mark the model changed.");
    }

    [TestMethod]
    public void AddImage_NoOutline_ReturnsFailure()
    {
        var result = _api.AddImage(Guid.NewGuid(), SampleImage());

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage, "No outline is opened");
    }

    [TestMethod]
    public async Task AddImage_ElementNotFound_ReturnsFailure()
    {
        await CreateOutlineAndGetCharacterGuid();

        var result = _api.AddImage(Guid.NewGuid(), SampleImage());

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage, "not found");
    }

    [TestMethod]
    public async Task AddImage_UnsupportedElement_ReturnsFailure()
    {
        var problemGuid = await CreateOutlineAndGetProblemGuid();

        var result = _api.AddImage(problemGuid, SampleImage());

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage, "does not support images");
    }

    // ----- RemoveImage -----

    [TestMethod]
    public async Task RemoveImage_ExistingId_Succeeds()
    {
        var characterGuid = await CreateOutlineAndGetCharacterGuid();
        var image = SampleImage();
        _api.AddImage(characterGuid, image);
        var character = (CharacterModel)_api.CurrentModel.StoryElements.StoryElementGuids[characterGuid];
        Assert.AreEqual(1, character.Images.Count, "Setup: the image must be attached before removal.");

        var result = _api.RemoveImage(characterGuid, image.Id);

        Assert.IsTrue(result.IsSuccess, $"Expected success, got: {result.ErrorMessage}");
        Assert.AreEqual(0, character.Images.Count);
    }

    [TestMethod]
    public void RemoveImage_NoOutline_ReturnsFailure()
    {
        var result = _api.RemoveImage(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage, "No outline is opened");
    }

    [TestMethod]
    public async Task RemoveImage_ElementNotFound_ReturnsFailure()
    {
        await CreateOutlineAndGetCharacterGuid();

        var result = _api.RemoveImage(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage, "not found");
    }

    [TestMethod]
    public async Task RemoveImage_UnsupportedElement_ReturnsFailure()
    {
        var problemGuid = await CreateOutlineAndGetProblemGuid();

        var result = _api.RemoveImage(problemGuid, Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage, "does not support images");
    }

    [TestMethod]
    public async Task RemoveImage_MissingImageId_ReturnsFailure()
    {
        var characterGuid = await CreateOutlineAndGetCharacterGuid();
        _api.AddImage(characterGuid, SampleImage());

        var result = _api.RemoveImage(characterGuid, Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess, "Removing an unattached image Id must fail, not silently succeed.");
        StringAssert.Contains(result.ErrorMessage, "No image with Id");
        var character = (CharacterModel)_api.CurrentModel.StoryElements.StoryElementGuids[characterGuid];
        Assert.AreEqual(1, character.Images.Count, "The existing image must remain.");
    }

    // ----- UpdateImageCaption -----

    [TestMethod]
    public async Task UpdateImageCaption_ExistingId_Succeeds()
    {
        var characterGuid = await CreateOutlineAndGetCharacterGuid();
        var image = SampleImage();
        _api.AddImage(characterGuid, image);

        var result = _api.UpdateImageCaption(characterGuid, image.Id, "Understudy");

        Assert.IsTrue(result.IsSuccess, $"Expected success, got: {result.ErrorMessage}");
        var character = (CharacterModel)_api.CurrentModel.StoryElements.StoryElementGuids[characterGuid];
        Assert.AreEqual("Understudy", character.Images[0].Caption);
    }

    [TestMethod]
    public void UpdateImageCaption_NoOutline_ReturnsFailure()
    {
        var result = _api.UpdateImageCaption(Guid.NewGuid(), Guid.NewGuid(), "x");

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage, "No outline is opened");
    }

    [TestMethod]
    public async Task UpdateImageCaption_ElementNotFound_ReturnsFailure()
    {
        await CreateOutlineAndGetCharacterGuid();

        var result = _api.UpdateImageCaption(Guid.NewGuid(), Guid.NewGuid(), "x");

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage, "not found");
    }

    [TestMethod]
    public async Task UpdateImageCaption_UnsupportedElement_ReturnsFailure()
    {
        var problemGuid = await CreateOutlineAndGetProblemGuid();

        var result = _api.UpdateImageCaption(problemGuid, Guid.NewGuid(), "x");

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage, "does not support images");
    }

    [TestMethod]
    public async Task UpdateImageCaption_MissingImageId_ReturnsFailure()
    {
        var characterGuid = await CreateOutlineAndGetCharacterGuid();
        _api.AddImage(characterGuid, SampleImage());

        var result = _api.UpdateImageCaption(characterGuid, Guid.NewGuid(), "New");

        Assert.IsFalse(result.IsSuccess, "Captioning an unattached image Id must fail, not silently succeed.");
        StringAssert.Contains(result.ErrorMessage, "No image with Id");
        var character = (CharacterModel)_api.CurrentModel.StoryElements.StoryElementGuids[characterGuid];
        Assert.AreEqual("Lead", character.Images[0].Caption, "The existing caption must be unchanged.");
    }
}
