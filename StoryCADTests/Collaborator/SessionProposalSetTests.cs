using StoryCollaborator.Models;
using StoryCollaborator.Services;

namespace StoryCADTests.Collaborator;

/// <summary>Collaborator #145: full session proposal set for chat.</summary>
[TestClass]
public class SessionProposalSetTests
{
    private static PendingUpdate Make(string label, string prop, string value, UpdateKind kind = UpdateKind.Fill) =>
        new(label, Guid.NewGuid(), new PropertySpec(prop), value, kind, "current");

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
    public void BuildSnapshotText_IncludesStatus()
    {
        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[] { Make("Problem", "Name", "Prophecy") });
        set.MarkSkipped("Problem.Name");

        var snap = set.BuildSnapshotText();
        StringAssert.Contains(snap, "Problem.Name");
        StringAssert.Contains(snap, "skipped");
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
}
