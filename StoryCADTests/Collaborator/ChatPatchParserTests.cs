using StoryCollaborator.Services;

namespace StoryCADTests.Collaborator;

/// <summary>Collaborator #145: parse JSON patches from chat replies.</summary>
[TestClass]
public class ChatPatchParserTests
{
    [TestMethod]
    public void TryParse_ExtractsPatches_AndStripsJsonFromDisplay()
    {
        var raw =
            "OK, renamed it.\n\n" +
            "{\"patches\":[{\"key\":\"Problem.Name\",\"value\":\"Witch Prophecy\"}]}\n";

        var ok = ChatPatchParser.TryParse(raw, out var display, out var patches);

        Assert.IsTrue(ok);
        Assert.AreEqual(1, patches.Count);
        Assert.AreEqual("Problem.Name", patches[0].Key);
        Assert.AreEqual("Witch Prophecy", patches[0].Value);
        StringAssert.Contains(display, "OK, renamed");
        Assert.IsFalse(display.Contains("patches", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TryParse_MultiplePatches()
    {
        var raw = """{"patches":[{"key":"Problem.Name","value":"A"},{"key":"Problem.Premise","value":"B"}]}""";

        Assert.IsTrue(ChatPatchParser.TryParse(raw, out _, out var patches));
        Assert.AreEqual(2, patches.Count);
    }

    [TestMethod]
    public void TryParse_NoJson_ReturnsDisplayOnly_EmptyPatches()
    {
        var raw = "That's outside this proposal chat.";

        var ok = ChatPatchParser.TryParse(raw, out var display, out var patches);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, patches.Count);
        Assert.AreEqual(raw, display.Trim());
    }

    [TestMethod]
    public void TryParse_EmbeddedInFences()
    {
        var raw =
            "Done.\n```json\n{\"patches\":[{\"key\":\"Overview.Concept\",\"value\":\"Shorter\"}]}\n```\n";

        Assert.IsTrue(ChatPatchParser.TryParse(raw, out var display, out var patches));
        Assert.AreEqual(1, patches.Count);
        Assert.AreEqual("Overview.Concept", patches[0].Key);
        Assert.IsFalse(display.Contains("```"));
    }
}
