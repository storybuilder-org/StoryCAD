using StoryCollaborator.Models;
using StoryCollaborator.Services;

namespace StoryCADTests.Collaborator;

/// <summary>Collaborator #145: full session proposal set for chat.</summary>
[TestClass]
public class SessionProposalSetTests
{
    private static PendingUpdate Make(
        string label, string prop, string value,
        UpdateKind kind = UpdateKind.Fill, string? current = "current") =>
        new(label, Guid.NewGuid(), new PropertySpec(prop), value, kind, current);

    [TestMethod]
    public void LoadFromPending_KeepsAllKeys()
    {
        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[]
        {
            Make("Problem", "Name", "Prophecy of the Crown"),
            Make("Problem", "Premise", "Long premise", UpdateKind.Protect),
        });

        Assert.AreEqual(2, set.Count);
        Assert.IsTrue(set.ContainsKey("Problem.Name"));
        Assert.AreEqual(ProposalSessionStatus.Open, set.Get("Problem.Name")!.Status);
    }

    [TestMethod]
    public void MarkAccepted_DoesNotRemove_AllowsPatchAndReopen()
    {
        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[] { Make("Overview", "Concept", "Wordy concept") });

        set.MarkAccepted("Overview.Concept");
        Assert.AreEqual(ProposalSessionStatus.Accepted, set.Get("Overview.Concept")!.Status);

        Assert.IsTrue(set.TryApplyPatch("Overview.Concept", "Three what-ifs only", out var reopened));
        Assert.IsTrue(reopened);
        Assert.AreEqual(ProposalSessionStatus.Open, set.Get("Overview.Concept")!.Status);
        Assert.AreEqual("Three what-ifs only", set.Get("Overview.Concept")!.ProposedText);
    }

    [TestMethod]
    public void MarkSkipped_StillPatchable()
    {
        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[] { Make("Problem", "Description", "Story Q") });
        set.MarkSkipped("Problem.Description");

        Assert.IsTrue(set.TryApplyPatch("Problem.Description", "Revised Q", out _));
        Assert.AreEqual(ProposalSessionStatus.Open, set.Get("Problem.Description")!.Status);
    }

    [TestMethod]
    public void TryApplyPatch_UnknownKey_Fails()
    {
        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[] { Make("Problem", "Name", "A") });

        Assert.IsFalse(set.TryApplyPatch("Problem.Theme", "No", out _));
    }

    [TestMethod]
    public void BuildSnapshotText_IncludesProposedAndOutline_FullTextAfterSkip()
    {
        var set = new SessionProposalSet();
        var proposed = "PROPOSED sketch " + new string('p', 400);
        var outline = "OUTLINE sketch " + new string('o', 400) + " END_OUTLINE";
        set.ReplaceFromPending(new[]
        {
            Make("Character", "Description", proposed, UpdateKind.Protect, outline)
        });
        set.MarkSkipped("Character.Description");

        var snap = set.BuildSnapshotText();
        StringAssert.Contains(snap, "Character.Description");
        StringAssert.Contains(snap, "skipped");
        StringAssert.Contains(snap, "Proposed (Collaborator):");
        StringAssert.Contains(snap, "Outline (value on the story element when classified):");
        StringAssert.Contains(snap, " END_OUTLINE");
        StringAssert.Contains(snap, proposed.Substring(0, 40));
        Assert.IsFalse(snap.Contains('…'), "Default snapshot must not truncate outline/proposed");
    }

    [TestMethod]
    public void OpenProposals_AsPendingUpdates_ForUi()
    {
        var set = new SessionProposalSet();
        var a = Make("Problem", "Name", "A");
        var b = Make("Problem", "Premise", "B");
        set.ReplaceFromPending(new[] { a, b });
        set.MarkAccepted("Problem.Name");

        var open = set.OpenAsPendingUpdates().ToList();
        Assert.AreEqual(1, open.Count);
        Assert.AreEqual("Problem.Premise", open[0].Key);
    }

    [TestMethod]
    public void SimpleList_ProposedText_NotClrTypeName_AndOpenKeepsList()
    {
        // DefineCharacter TraitList: List.ToString() used to leak List`1[System.String] in the UI.
        var traits = new List<string> { "guarded", "precise", "dry humor" };
        var spec = new PropertySpec("TraitList", WriteVia.SimpleList, ListEntryType: typeof(string));
        var update = new PendingUpdate("Character", Guid.NewGuid(), spec, traits, UpdateKind.Fill, null);

        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[] { update });

        var entry = set.Get("Character.TraitList");
        Assert.IsNotNull(entry);
        Assert.IsFalse(entry!.ProposedText.Contains("System.Collections", StringComparison.Ordinal),
            entry.ProposedText);
        StringAssert.Contains(entry.ProposedText, "guarded");
        StringAssert.Contains(entry.ProposedText, "precise");

        var open = set.OpenAsPendingUpdates().Single();
        Assert.IsInstanceOfType(open.Value, typeof(List<string>));
        CollectionAssert.AreEqual(traits, (List<string>)open.Value!);
    }
}
