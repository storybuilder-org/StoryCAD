using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;

namespace StoryCADTests.Models;

/// <summary>
///     Verifies attached pictures embed in the .stbx and survive a
///     serialize/deserialize round-trip for each supported element type.
/// </summary>
[TestClass]
public class StoryImageSerializationTests
{
    private static StoryImage SampleImage() =>
        new(Guid.NewGuid(), Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 }), "image/png", "ref.png")
        {
            Caption = "Reference shot"
        };

    [TestMethod]
    public void CharacterImages_SurviveSerializationRoundTrip()
    {
        var model = new StoryModel();
        var character = new CharacterModel("Hero", model, null);
        StoryImage img = SampleImage();
        character.Images.Add(img);

        string json = character.Serialize();
        StringAssert.Contains(json, "\"Images\"");
        StringAssert.Contains(json, img.ImageData);

        var restored = (CharacterModel)StoryElement.Deserialize(json);
        Assert.AreEqual(1, restored.Images.Count);
        Assert.AreEqual(img.Id, restored.Images[0].Id);
        Assert.AreEqual(img.ImageData, restored.Images[0].ImageData);
        Assert.AreEqual("image/png", restored.Images[0].ContentType);
        Assert.AreEqual("ref.png", restored.Images[0].FileName);
        Assert.AreEqual("Reference shot", restored.Images[0].Caption);
    }

    [TestMethod]
    public void SettingImages_SurviveSerializationRoundTrip()
    {
        var model = new StoryModel();
        var setting = new SettingModel(model, null);
        StoryImage img = SampleImage();
        setting.Images.Add(img);

        var restored = (SettingModel)StoryElement.Deserialize(setting.Serialize());
        Assert.AreEqual(1, restored.Images.Count);
        Assert.AreEqual(img.ImageData, restored.Images[0].ImageData);
    }

    [TestMethod]
    public void SceneImages_SurviveSerializationRoundTrip()
    {
        var model = new StoryModel();
        var scene = new SceneModel(model, null);
        StoryImage img = SampleImage();
        scene.Images.Add(img);

        var restored = (SceneModel)StoryElement.Deserialize(scene.Serialize());
        Assert.AreEqual(1, restored.Images.Count);
        Assert.AreEqual(img.ImageData, restored.Images[0].ImageData);
    }

    [TestMethod]
    public void NotesImages_SurviveSerializationRoundTrip()
    {
        var model = new StoryModel();
        var notes = new FolderModel("My Note", model, StoryItemType.Notes, null);
        StoryImage img = SampleImage();
        notes.Images.Add(img);

        var restored = (FolderModel)StoryElement.Deserialize(notes.Serialize());
        Assert.AreEqual(1, restored.Images.Count);
        Assert.AreEqual(img.ImageData, restored.Images[0].ImageData);
        Assert.AreEqual("Reference shot", restored.Images[0].Caption);
    }

    [TestMethod]
    public void NoImages_SerializesEmptyList()
    {
        var model = new StoryModel();
        var character = new CharacterModel("Hero", model, null);

        var restored = (CharacterModel)StoryElement.Deserialize(character.Serialize());
        Assert.IsNotNull(restored.Images);
        Assert.AreEqual(0, restored.Images.Count);
    }

    /// <summary>Serializes <paramref name="element"/> and strips the "Images" property
    /// entirely, reproducing a legacy .stbx written before the Images feature (which
    /// has no "Images" key at all, not an empty array).</summary>
    private static string SerializeWithoutImagesKey(StoryElement element)
    {
        JsonObject obj = JsonNode.Parse(element.Serialize())!.AsObject();
        Assert.IsTrue(obj.Remove("Images"), "Expected serialized element to contain an Images key to remove.");
        return obj.ToJsonString();
    }

    /// <summary>
    ///     A legacy .stbx predating the Images feature has no "Images" key at all.
    ///     System.Text.Json only overwrites properties present in the JSON, so the
    ///     constructor-initialized list must survive deserialization as empty, not null.
    /// </summary>
    [TestMethod]
    public void LegacyPayload_MissingImagesKey_DeserializesToEmptyList()
    {
        var model = new StoryModel();
        var character = new CharacterModel("Hero", model, null);
        string legacyJson = SerializeWithoutImagesKey(character);
        StringAssert.DoesNotMatch(legacyJson, new System.Text.RegularExpressions.Regex("\"Images\""),
            "The legacy payload must not contain an Images key.");

        var restored = (CharacterModel)StoryElement.Deserialize(legacyJson);

        Assert.IsNotNull(restored.Images, "Missing Images key must not leave the list null.");
        Assert.AreEqual(0, restored.Images.Count);
    }

    /// <summary>
    ///     As above for a Notes element, which declares its own Images list on
    ///     <see cref="FolderModel"/> rather than inheriting it, so the missing-key
    ///     invariant is verified on that separate declaration too.
    /// </summary>
    [TestMethod]
    public void LegacyNotesPayload_MissingImagesKey_DeserializesToEmptyList()
    {
        var model = new StoryModel();
        var notes = new FolderModel("My Note", model, StoryItemType.Notes, null);
        string legacyJson = SerializeWithoutImagesKey(notes);

        var restored = (FolderModel)StoryElement.Deserialize(legacyJson);

        Assert.IsNotNull(restored.Images, "Missing Images key must not leave the list null.");
        Assert.AreEqual(0, restored.Images.Count);
    }
}
