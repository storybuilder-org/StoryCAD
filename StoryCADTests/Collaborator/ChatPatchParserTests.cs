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

    // Collaborator #237: a raw line break inside a JSON string is invalid JSON. The Concept
    // what-ifs are one per line, so a model that does not escape them dropped every patch.
    [TestMethod]
    public void TryParse_RawLineBreaksInsideValue_StillReads()
    {
        var raw = "Done.\n{\"patches\":[{\"key\":\"Overview.Concept\",\"value\":\"What if A?\nWhat if B?\"}]}";

        Assert.IsTrue(ChatPatchParser.TryParse(raw, out var display, out var patches));
        Assert.AreEqual(1, patches.Count);
        Assert.AreEqual("What if A?\nWhat if B?", patches[0].Value);
        Assert.AreEqual("Done.", display);
    }

    [TestMethod]
    public void TryParse_TrailingComma_StillReads()
    {
        var raw = "{\"patches\":[{\"key\":\"Overview.Concept\",\"value\":\"A\"},]}";

        Assert.IsTrue(ChatPatchParser.TryParse(raw, out _, out var patches));
        Assert.AreEqual(1, patches.Count);
    }

    [TestMethod]
    public void HasUnreadPatchBlock_TrueOnlyWhenPatchesNamedButNoneParsed()
    {
        var broken = "{\"patches\":[{\"key\":\"Overview.Concept\",\"value\":\"A\"}";
        ChatPatchParser.TryParse(broken, out _, out var none);
        Assert.AreEqual(0, none.Count);
        Assert.IsTrue(ChatPatchParser.HasUnreadPatchBlock(broken, none));

        Assert.IsFalse(ChatPatchParser.HasUnreadPatchBlock("No changes.", none));

        ChatPatchParser.TryParse("{\"patches\":[{\"key\":\"Overview.Concept\",\"value\":\"A\"}]}", out _, out var one);
        Assert.IsFalse(ChatPatchParser.HasUnreadPatchBlock("irrelevant", one));
    }

    [TestMethod]
    public void TryParse_JsonOnly_LeavesTheDisplayEmpty()
    {
        // Collaborator #237 item 5: the parser used to write "Updated 2 proposals." for a
        // JSON-only reply before anything was applied. The caller reports from what it applied.
        var raw = "{\"patches\":[{\"key\":\"Overview.Concept\",\"value\":\"A\"},{\"key\":\"Overview.Premise\",\"value\":\"B\"}]}";

        Assert.IsTrue(ChatPatchParser.TryParse(raw, out var display, out var patches));
        Assert.AreEqual(2, patches.Count);
        Assert.AreEqual(string.Empty, display);
    }
}
