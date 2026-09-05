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

    [TestMethod]
    public void ReplaceFromPending_Relationship_ShowsPartnerNameNotGuid()
    {
        var partnerGuid = Guid.Parse("6ede4860-411b-43f5-829d-47b3a6b1aa21");
        var spec = new PropertySpec("RelationshipList", WriteVia.Relationships, JsonKey: "relationship");
        var update = new PendingUpdate(
            "Character",
            Guid.Parse("e2ab5b51-1f5e-4067-b4d4-7e99ba4e642f"),
            spec,
            new List<RelationshipInfo> { new(partnerGuid, "Rival", Notes: "A tries to impose order") },
            UpdateKind.Fill,
            null);

        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[] { update }, guid => guid == partnerGuid ? "B" : null);

        var text = set.Get("Character.RelationshipList")!.ProposedText;
        StringAssert.Contains(text, "B");
        StringAssert.Contains(text, "A tries to impose order");
        Assert.IsFalse(text.Contains(partnerGuid.ToString("D"), StringComparison.Ordinal), text);
    }

    // Collaborator #237 item 5: the Scorecard S01 chat said "Updated 2 proposals." while the
    // list and the file kept the originals. A patch key the session did not know was one way
    // to get there. Keys now resolve by exact key, by bare property name, or by the property
    // part of a Label.Property key, when the match is unique.
    [TestMethod]
    public void ResolveKey_ExactKey_ReturnsIt()
    {
        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[] { Make("Overview", "Concept", "A"), Make("Overview", "Premise", "B") });

        Assert.AreEqual("Overview.Concept", set.ResolveKey("overview.concept"));
    }

    [TestMethod]
    public void ResolveKey_BarePropertyName_MatchesTheOneEntry()
    {
        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[] { Make("Overview", "Concept", "A"), Make("Overview", "Premise", "B") });

        Assert.AreEqual("Overview.Premise", set.ResolveKey("Premise"));
        Assert.AreEqual("Overview.Premise", set.ResolveKey("StoryOverview.Premise"));
    }

    [TestMethod]
    public void ResolveKey_AmbiguousOrUnknown_IsNull()
    {
        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[] { Make("Problem", "Name", "A"), Make("Character", "Name", "B") });

        Assert.IsNull(set.ResolveKey("Name"), "two entries share the property name");
        Assert.IsNull(set.ResolveKey("Theme"));
        Assert.IsNull(set.ResolveKey(""));
    }

    [TestMethod]
    public void TryApplyPatch_BarePropertyName_PatchesTheEntry()
    {
        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[] { Make("Overview", "Concept", "Five what-ifs") });

        Assert.IsTrue(set.TryApplyPatch("Concept", "One what-if and two follow-ons", out _));
        Assert.AreEqual("One what-if and two follow-ons", set.Get("Overview.Concept")!.ProposedText);
        Assert.AreEqual("One what-if and two follow-ons", set.OpenAsPendingUpdates().Single().Value);
    }

    // Collaborator #237 item 3: the run's order is the dependency order, so the chat is told
    // it and told to re-derive everything after a change.
    [TestMethod]
    public void All_And_Snapshot_KeepTheRunOrder_NotAlphabetical()
    {
        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[]
        {
            Make("Overview", "Description", "idea"),
            Make("Overview", "Concept", "what if"),
            Make("Overview", "Premise", "one sentence")
        });

        CollectionAssert.AreEqual(
            new[] { "Overview.Description", "Overview.Concept", "Overview.Premise" },
            set.OrderedKeys.ToList());
        CollectionAssert.AreEqual(
            new[] { "Description", "Concept", "Premise" },
            set.All.Select(e => e.DisplayName).ToList());

        var snap = set.BuildSnapshotText();
        Assert.IsTrue(snap.IndexOf("### Description", StringComparison.Ordinal) < snap.IndexOf("### Concept", StringComparison.Ordinal));
        Assert.IsTrue(snap.IndexOf("### Concept", StringComparison.Ordinal) < snap.IndexOf("### Premise", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BuildSnapshotText_NamesTheProposal_AndCarriesThePatchKeyAside()
    {
        // Collaborator #237 item 9: the writer saw "Concept (Overview.Concept)" and "Open" in
        // chat. The heading leads with the name; the key is labelled as the patch key.
        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[] { Make("Overview", "Concept", "what if") });

        var snap = set.BuildSnapshotText();
        StringAssert.Contains(snap, "### Concept [open] (patch key: Overview.Concept)");
    }

    [TestMethod]
    public void BuildSystemInstructions_StatesTheOrder_AndTheDownstreamRule()
    {
        var text = SessionProposalSet.BuildSystemInstructions(
            "Ideation (Story idea => Concept => Premise)", new[] { "Description", "Concept", "Premise" });

        StringAssert.Contains(text, "in this order, each from the ones before it: Description, Concept, Premise.");
        StringAssert.Contains(text, "also rewrite every proposal after it in that order");
        StringAssert.Contains(text, "call a proposal by its name");
        StringAssert.Contains(text, "Do not show the writer a patch key or a status word");
    }

    [TestMethod]
    public void BuildSystemInstructions_OneProposal_HasNoOrderRule()
    {
        var text = SessionProposalSet.BuildSystemInstructions("Story Form", new[] { "StoryType" });

        Assert.IsFalse(text.Contains("in this order"), text);
        StringAssert.Contains(text, "patches");
    }

    // Collaborator #237: the Scorecard chat of 2026-09-05 said "I can't see or control your
    // patch system's current state" and told the writer to apply the patches. The model was
    // never told the app applies its JSON to the list.
    [TestMethod]
    public void BuildSystemInstructions_SaysTheAppAppliesPatches_AndForbidsHandingWorkBack()
    {
        var text = SessionProposalSet.BuildSystemInstructions("Ideation", new[] { "Concept", "Premise" });

        StringAssert.Contains(text, "The app reads the patches JSON out of your reply");
        StringAssert.Contains(text, "complete new text");
        StringAssert.Contains(text, "Never tell the writer to apply");
        StringAssert.Contains(text, "Never say you cannot see or change the list");
        StringAssert.Contains(text, "If the writer says a change is not there");
    }

    // Same session: asked to make the mother the protagonist, the model kept the stadium
    // employee the run's Description proposal had named, because the order rule said later
    // proposals follow earlier ones. The writer's request outranks the proposals.
    [TestMethod]
    public void BuildSystemInstructions_WriterRequestOutranksEarlierProposals()
    {
        var text = SessionProposalSet.BuildSystemInstructions(
            "Ideation (Story idea => Concept => Premise)", new[] { "Description", "Concept", "Premise" });

        StringAssert.Contains(text, "The writer's request outranks every proposal in the snapshot.");
        StringAssert.Contains(text, "rewrite that earlier proposal too");
        StringAssert.Contains(text, "Do not keep a role, a name, or an event from an earlier proposal");
        Assert.IsTrue(
            text.IndexOf("outranks every proposal", StringComparison.Ordinal) <
            text.IndexOf("in this order, each from the ones before it", StringComparison.Ordinal),
            "the request rule comes before the order rule");
    }

    // Same session: the model re-sent the text already in the list and the app answered
    // "Changed Concept, Premise." over a list that looked the same.
    [TestMethod]
    public void TryApplyPatch_SameText_AppliesButReportsUnchanged()
    {
        var set = new SessionProposalSet();
        set.ReplaceFromPending(new[] { Make("Overview", "Concept", "What if A?\nWhat if B?") });

        Assert.IsTrue(set.TryApplyPatch("Overview.Concept", "What if A?\r\nWhat if B?\n", out _, out var changed));
        Assert.IsFalse(changed, "line endings and edge whitespace do not make a change");

        Assert.IsTrue(set.TryApplyPatch("Overview.Concept", "What if C?", out _, out changed));
        Assert.IsTrue(changed);
        Assert.AreEqual("What if C?", set.Get("Overview.Concept")!.ProposedText);
    }
}
