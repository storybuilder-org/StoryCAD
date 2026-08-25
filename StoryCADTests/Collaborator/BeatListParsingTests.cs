using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #77 / #208. The stub contract is only real if the runner parses the
///     fields off the wire. Tests that construct BeatInfo directly skip this layer, so the
///     scene fields reached the Scene writer from the tests and never from a model response.
/// </summary>
[TestClass]
public class BeatListParsingTests
{
    private const string Payload = """
    [
      {
        "title": "Catalyst",
        "description": "the turn",
        "assigned_element": null,
        "scene_name": "The letter arrives",
        "scene_description": "She reads it twice and burns it.",
        "scene_notes": "Dread. The letter stands for the debt she hid.",
        "scene_type": "Crisis scene",
        "scene_cast": ["11111111-1111-1111-1111-111111111111"]
      }
    ]
    """;

    private static BeatInfoView Parse()
    {
        using var doc = JsonDocument.Parse(Payload);
        var beat = WorkflowRunner.ExtractBeatList(doc.RootElement).Single();
        return new BeatInfoView(beat.SceneDescription, beat.SceneNotes, beat.SceneType,
            beat.SceneCast?.Count ?? 0);
    }

    private sealed record BeatInfoView(string Desc, string Notes, string Type, int CastCount);

    [TestMethod]
    public void ExtractBeatList_ReadsSceneDescription() =>
        Assert.AreEqual("She reads it twice and burns it.", Parse().Desc);

    [TestMethod]
    public void ExtractBeatList_ReadsSceneNotes() =>
        Assert.AreEqual("Dread. The letter stands for the debt she hid.", Parse().Notes);

    [TestMethod]
    public void ExtractBeatList_ReadsSceneType() =>
        Assert.AreEqual("Crisis scene", Parse().Type);

    [TestMethod]
    public void ExtractBeatList_ReadsSceneCast() =>
        Assert.AreEqual(1, Parse().CastCount);

    [TestMethod]
    public void ExtractBeatList_ToleratesTheFieldsBeingAbsent()
    {
        using var doc = JsonDocument.Parse("""[{"title":"A","description":"b"}]""");
        var beat = WorkflowRunner.ExtractBeatList(doc.RootElement).Single();

        Assert.IsNull(beat.SceneType);
        Assert.IsNull(beat.SceneCast);
    }
}
