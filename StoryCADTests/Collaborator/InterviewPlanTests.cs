using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CollaboratorLib.Context;
using StoryCADLib.Models;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #119, design section 25: the goal is chosen, not walked. The writer names
/// targets in chat or says "you choose"; the client holds the plan either way.
/// </summary>
[TestClass]
public class InterviewPlanTests
{
    [TestMethod]
    public void Default_IsTerrysOrder()
    {
        var plan = InterviewPlan.Default();

        Assert.IsTrue(plan.IsDefault);
        Assert.AreEqual("Flaw", plan.First);
        Assert.AreEqual(13, plan.Targets.Count);
        Assert.AreEqual("Description", plan.Targets[^1]);
        Assert.IsFalse(plan.Targets.Contains(InterviewScript.WantNeed));
    }

    [TestMethod]
    public void From_KeepsTheOrderGiven()
    {
        var plan = InterviewPlan.From(new[] { "BackStory", "Flaw", "WantNeed" });

        CollectionAssert.AreEqual(new[] { "BackStory", "Flaw", "WantNeed" }, plan.Targets.ToList());
        Assert.AreEqual("BackStory", plan.First);
        Assert.AreEqual("Flaw", plan.Next("BackStory"));
        Assert.AreEqual("WantNeed", plan.Next("Flaw"));
        Assert.IsNull(plan.Next("WantNeed"));
        Assert.IsFalse(plan.IsDefault);
    }

    [TestMethod]
    public void From_DropsUnknownAndDuplicateIds()
    {
        var plan = InterviewPlan.From(new[] { "Nonsense", "flaw", "Flaw", "Notes", "traits" });

        CollectionAssert.AreEqual(new[] { "Flaw", "TraitList" }, plan.Targets.ToList());
    }

    [TestMethod]
    public void From_NothingUsable_FallsBackToTerrysOrder()
    {
        Assert.IsTrue(InterviewPlan.From(new[] { "Nonsense" }).IsDefault);
        Assert.IsTrue(InterviewPlan.From(new string[0]).IsDefault);
        Assert.IsTrue(InterviewPlan.From(null).IsDefault);
    }

    [TestMethod]
    public void ToArg_IsCommaSeparatedIds()
    {
        Assert.AreEqual("Values,WantNeed", InterviewPlan.From(new[] { "Values", "WantNeed" }).ToArg());
    }

    [TestMethod]
    public void Describe_SpeaksFormLabelsNeverIds()
    {
        var text = InterviewPlan.From(new[] { "Flaw", "BackStory", "WantNeed" }).Describe();

        Assert.AreEqual("Starting with Flaw, then Backstory, then Want and need.", text);
        Assert.IsFalse(text.Contains("BackStory"));
        Assert.IsFalse(text.Contains("WantNeed"));
    }

    [TestMethod]
    public void FromKnown_IsNullWhenNothingIsATarget_AndCapsTheCount()
    {
        // Review 2026-09-07: a "you choose" reply with no usable TARGETS must read as a
        // failed opening, not as a silent walk of all thirteen fields. And the Worker is
        // asked for up to three.
        Assert.IsNull(InterviewPlan.FromKnown(new[] { "Nonsense", "Notes" }, maxCount: 3));
        Assert.IsNull(InterviewPlan.FromKnown(new string[0], maxCount: 3));
        Assert.IsNull(InterviewPlan.FromKnown(null, maxCount: 3));

        var capped = InterviewPlan.FromKnown(new[] { "Flaw", "Values", "Focus", "Role", "Archetype" }, maxCount: 3);
        CollectionAssert.AreEqual(new[] { "Flaw", "Values", "Focus" }, capped!.Targets.ToList());

        var spaced = InterviewPlan.FromKnown(new[] { "Back story", "Want and need" }, maxCount: 3);
        CollectionAssert.AreEqual(new[] { "BackStory", "WantNeed" }, spaced!.Targets.ToList());
    }

    [TestMethod]
    public void Describe_DefaultPlan_DoesNotListThirteenFields()
    {
        var text = InterviewPlan.Default().Describe();

        StringAssert.StartsWith(text, "Starting with Flaw");
        Assert.IsFalse(text.Contains("Description"));
    }
}

[TestClass]
public class InterviewChoiceTests
{
    [TestMethod]
    public void Numbers_MapToTheListInOrder()
    {
        var choice = InterviewChoice.Parse("2, 1 and 14");

        Assert.AreEqual(InterviewChoiceKind.Targets, choice.Kind);
        CollectionAssert.AreEqual(new[] { "BackStory", "Flaw", "WantNeed" }, choice.Targets.ToList());
    }

    [TestMethod]
    public void Numbers_TolerateListPunctuation()
    {
        var choice = InterviewChoice.Parse("1. 3) #5");

        CollectionAssert.AreEqual(new[] { "Flaw", "Values", "Focus" }, choice.Targets.ToList());
    }

    [TestMethod]
    public void Names_ResolveByIdLabelOrAlias()
    {
        var choice = InterviewChoice.Parse("backstory, Traits then psychological notes, sketch");

        CollectionAssert.AreEqual(
            new[] { "BackStory", "TraitList", "PsychNotes", "Description" },
            choice.Targets.ToList());
    }

    [TestMethod]
    public void MultiWordLabels_DoNotReadAsTheirLastWord()
    {
        // "story role" must not land on Role, and "back story" must not vanish.
        var choice = InterviewChoice.Parse("story role and back story");

        CollectionAssert.AreEqual(new[] { "StoryRole", "BackStory" }, choice.Targets.ToList());
    }

    [TestMethod]
    public void WantAndNeed_SurvivesItsOwnAnd()
    {
        CollectionAssert.AreEqual(new[] { "WantNeed" }, InterviewChoice.Parse("want and need").Targets.ToList());
        CollectionAssert.AreEqual(new[] { "WantNeed" }, InterviewChoice.Parse("want vs need").Targets.ToList());
        CollectionAssert.AreEqual(
            new[] { "Flaw", "WantNeed" },
            InterviewChoice.Parse("flaw, then want and need").Targets.ToList());
    }

    [TestMethod]
    public void ASentence_KeepsWhatItCanPlace()
    {
        var choice = InterviewChoice.Parse("I want to dig into her flaw and where it came from");

        Assert.AreEqual(InterviewChoiceKind.Targets, choice.Kind);
        CollectionAssert.AreEqual(new[] { "Flaw" }, choice.Targets.ToList());
    }

    [TestMethod]
    public void ASentence_DoesNotReadEverydayWordsAsHeaders()
    {
        // Review 2026-09-07: "focus on her flaw" was a plan of Focus then Flaw.
        CollectionAssert.AreEqual(new[] { "Flaw" }, InterviewChoice.Parse("focus on her flaw").Targets.ToList());
        CollectionAssert.AreEqual(new[] { "Flaw" }, InterviewChoice.Parse("a description of her flaw").Targets.ToList());
        Assert.AreEqual(InterviewChoiceKind.Unreadable, InterviewChoice.Parse("her role in the story").Kind);
    }

    [TestMethod]
    public void AList_TrustsEverydayWordsAsHeaders()
    {
        CollectionAssert.AreEqual(new[] { "Focus" }, InterviewChoice.Parse("Focus").Targets.ToList());
        CollectionAssert.AreEqual(new[] { "Role", "Values" }, InterviewChoice.Parse("role, values").Targets.ToList());
        CollectionAssert.AreEqual(new[] { "Description" }, InterviewChoice.Parse("13").Targets.ToList());
    }

    [TestMethod]
    public void NamedTargets_BeatAYouChoosePhraseInTheSameReply()
    {
        // Review 2026-09-07: "you pick" anywhere handed the whole plan to the Worker.
        var choice = InterviewChoice.Parse("Flaw and back story, then you pick the rest");

        Assert.AreEqual(InterviewChoiceKind.Targets, choice.Kind);
        CollectionAssert.AreEqual(new[] { "Flaw", "BackStory" }, choice.Targets.ToList());
    }

    [TestMethod]
    public void PageHeaders_Resolve()
    {
        CollectionAssert.AreEqual(new[] { "Description" }, InterviewChoice.Parse("character sketch").Targets.ToList());
        CollectionAssert.AreEqual(new[] { "PsychNotes" }, InterviewChoice.Parse("psych notes").Targets.ToList());
        CollectionAssert.AreEqual(new[] { "BackStory" }, InterviewChoice.Parse("Backstory").Targets.ToList());
    }

    [TestMethod]
    public void YouChoose_HandsTheChoiceToTheWorker()
    {
        foreach (var text in new[] { "you choose", "You pick.", "up to you", "your call", "surprise me" })
        {
            Assert.AreEqual(InterviewChoiceKind.ModelChooses, InterviewChoice.Parse(text).Kind, text);
            Assert.AreEqual(0, InterviewChoice.Parse(text).Targets.Count, text);
        }
    }

    [TestMethod]
    public void All_IsEveryFormField()
    {
        var choice = InterviewChoice.Parse("all");

        Assert.AreEqual(InterviewChoiceKind.Targets, choice.Kind);
        Assert.AreEqual(13, choice.Targets.Count);
    }

    [TestMethod]
    public void NothingReadable_IsUnreadable()
    {
        Assert.AreEqual(InterviewChoiceKind.Unreadable, InterviewChoice.Parse("banana").Kind);
        Assert.AreEqual(InterviewChoiceKind.Unreadable, InterviewChoice.Parse("   ").Kind);
        Assert.AreEqual(InterviewChoiceKind.Unreadable, InterviewChoice.Parse("99").Kind);
        Assert.AreEqual(InterviewChoiceKind.Unreadable, InterviewChoice.Parse(null).Kind);
    }

    [TestMethod]
    public void ListText_NumbersEveryTargetByFormLabelAndMarksBlanks()
    {
        var blank = new HashSet<string> { "Flaw", "BackStory" };

        var text = InterviewChoice.ListText("Mara", blank);

        StringAssert.Contains(text, "explore about Mara?");
        StringAssert.Contains(text, "you choose");
        StringAssert.Contains(text, "1. Flaw (empty)");
        StringAssert.Contains(text, "2. Backstory (empty)");
        StringAssert.Contains(text, "3. Values");
        Assert.IsFalse(text.Contains("3. Values (empty)"), "Values is filled, so it is not marked.");
        StringAssert.Contains(text, "6. Psych Notes");
        StringAssert.Contains(text, "11. Story Role");
        StringAssert.Contains(text, "13. Character Sketch");
        StringAssert.Contains(text, "14. Want and need");
        Assert.IsFalse(text.Contains("BackStory"), "The list speaks page headers, not ids.");
        Assert.IsFalse(text.Contains("WantNeed"));
    }
}

[TestClass]
public class InterviewScriptTableTests
{
    [TestMethod]
    public void Labels_AgreeWithTheGapSurface()
    {
        // Review 2026-09-07: the choice list and the Outline gaps page must call a field
        // the same thing. GapWorkflowOwnership.DisplayLabel is the older of the two.
        foreach (var id in new[] { "BackStory", "StoryRole", "Description" })
        {
            Assert.AreEqual(
                GapWorkflowOwnership.DisplayLabel(StoryItemType.Character, id),
                InterviewScript.Label(id),
                id);
        }
    }

    [TestMethod]
    public void Targets_AreTheFieldsThenWantNeed()
    {
        Assert.AreEqual(13, InterviewScript.Fields.Count);
        Assert.AreEqual(14, InterviewScript.Targets.Count);
        Assert.AreEqual(InterviewScript.WantNeed, InterviewScript.Targets[^1]);
        Assert.AreEqual("Description", InterviewScript.Fields[^1]);
    }

    [TestMethod]
    public void BlankFields_ComeFromTheSameTableAsTheList()
    {
        var character = new CharacterModel { Flaw = "Trusts paper over people", TraitList = new List<string> { "Impulsive" } };

        var blank = InterviewScript.BlankFields(character);

        Assert.IsFalse(blank.Contains("Flaw"));
        Assert.IsFalse(blank.Contains("TraitList"));
        Assert.IsTrue(blank.Contains("BackStory"));
        Assert.IsTrue(blank.Contains("Description"));
        Assert.IsFalse(blank.Contains(InterviewScript.WantNeed), "Not a field; never marked.");
        foreach (var id in InterviewScript.Fields)
        {
            if (id is "Flaw" or "TraitList") continue;
            Assert.IsTrue(blank.Contains(id), $"{id} is empty on a fresh character and must be marked.");
        }
        Assert.AreEqual(0, InterviewScript.BlankFields(null).Count);
    }
}

[TestClass]
public class InterviewCursorPlanTests
{
    [TestMethod]
    public void Start_WithAPlan_LandsOnItsFirstTarget()
    {
        var cursor = new InterviewCursor();

        cursor.Start(InterviewPlan.From(new[] { "Values", "Flaw" }));

        Assert.AreEqual("Values", cursor.Field);
        Assert.AreEqual("Flaw", cursor.NextField);
    }

    [TestMethod]
    public void GotIt_WalksThePlanNotTerrysOrder()
    {
        var cursor = new InterviewCursor();
        cursor.Start(InterviewPlan.From(new[] { "Values", "Flaw" }));

        cursor.Apply(InterviewVerdict.GotIt);

        Assert.AreEqual("Flaw", cursor.Field);
        Assert.IsNull(cursor.NextField, "Flaw is the last target here, not the first of thirteen.");

        cursor.Apply(InterviewVerdict.NotThis);
        Assert.AreEqual("Flaw", cursor.Field);
    }

    [TestMethod]
    public void NextField_BeforeStart_IsNull()
    {
        Assert.IsNull(new InterviewCursor().NextField);
    }

    [TestMethod]
    public void Reset_ForgetsThePlan()
    {
        var cursor = new InterviewCursor();
        cursor.Start(InterviewPlan.From(new[] { "WantNeed" }));

        cursor.Reset();

        Assert.IsTrue(cursor.Plan.IsDefault);
    }
}

[TestClass]
public class InterviewReplyTargetsTests
{
    [TestMethod]
    public void Parse_LiftsTheTargetsLineOutOfTheQuestion()
    {
        var reply = InterviewReply.Parse(
            "VERDICT: KEEP\nTARGETS: Flaw, BackStory, WantNeed\nWhat does closing the Box cost you?");

        Assert.AreEqual(InterviewVerdict.KeepAsking, reply.Verdict);
        CollectionAssert.AreEqual(new[] { "Flaw", "BackStory", "WantNeed" }, reply.Targets.ToList());
        Assert.AreEqual("What does closing the Box cost you?", reply.Question);
    }

    [TestMethod]
    public void Parse_ToleratesCaseSpacingAndABlankLine()
    {
        var reply = InterviewReply.Parse("VERDICT: KEEP\n\n targets : Values;Role \n\nWho taught you?");

        CollectionAssert.AreEqual(new[] { "Values", "Role" }, reply.Targets.ToList());
        Assert.AreEqual("Who taught you?", reply.Question);
    }

    [TestMethod]
    public void Parse_NoTargetsLine_IsEmptyTargets()
    {
        var reply = InterviewReply.Parse("VERDICT: GOTIT\nWhen did that first work?");

        Assert.AreEqual(0, reply.Targets.Count);
        Assert.AreEqual("When did that first work?", reply.Question);
    }

    [TestMethod]
    public void Parse_TargetsLineAnywhere_IsLiftedOutOfTheQuestion()
    {
        // Review 2026-09-07: left in the body it would be posted to the chat and saved
        // into the Note, ids and all. Wherever it lands, the writer never sees it.
        var reply = InterviewReply.Parse("VERDICT: KEEP\nWhat now?\nTARGETS: Flaw");

        CollectionAssert.AreEqual(new[] { "Flaw" }, reply.Targets.ToList());
        Assert.AreEqual("What now?", reply.Question);
    }

    [TestMethod]
    public void Parse_TargetsWithSpacedHeaders_StayWhole()
    {
        // Split on spaces, "Back story, Want and need" shredded into five useless tokens.
        var reply = InterviewReply.Parse("VERDICT: KEEP\nTARGETS: Back story, Want and need\nWho taught you?");

        CollectionAssert.AreEqual(new[] { "Back story", "Want and need" }, reply.Targets.ToList());
        CollectionAssert.AreEqual(new[] { "BackStory", "WantNeed" }, InterviewPlan.From(reply.Targets).Targets.ToList());
    }

    [TestMethod]
    public void LooksLikeAClose_IsNeverAQuestion()
    {
        Assert.IsFalse(InterviewReply.LooksLikeAClose("Your marriage is failing. Do you still believe you can save it?"));
        Assert.IsFalse(InterviewReply.LooksLikeAClose("Is this the end of the interview for you, or the start?"));
    }

    [TestMethod]
    public void Parse_TargetsThenEmptyBody_KeepsTargetsAndEmptyQuestion()
    {
        var reply = InterviewReply.Parse("VERDICT: KEEP\nTARGETS: Flaw\n");

        CollectionAssert.AreEqual(new[] { "Flaw" }, reply.Targets.ToList());
        Assert.AreEqual(string.Empty, reply.Question);
    }

    [TestMethod]
    public void LooksLikeAQuestion_IsTheClosingLineGuard()
    {
        // Sample-outline runs 2026-09-07: the Got it that ended the interview came back as
        // a question twice. The client keeps that off the screen; Saved is the close.
        Assert.IsTrue(InterviewReply.LooksLikeAQuestion("What frightens you more than poverty?"));
        Assert.IsTrue(InterviewReply.LooksLikeAQuestion("Who was there?  "));
        Assert.IsFalse(InterviewReply.LooksLikeAQuestion("That is the interview. You can save it now."));
        Assert.IsFalse(InterviewReply.LooksLikeAQuestion(string.Empty));
        Assert.IsFalse(InterviewReply.LooksLikeAQuestion(null));
    }

    [TestMethod]
    public void LooksLikeAClose_IsTheEarlyStopGuard()
    {
        // Retest 2026-09-07 (Santiago): a second refusal came back Not this with the
        // closing line as its body and two fields still to go.
        Assert.IsTrue(InterviewReply.LooksLikeAClose("That is the end of the interview. You can save it now."));
        Assert.IsTrue(InterviewReply.LooksLikeAClose("The interview is complete and you can save it."));
        Assert.IsFalse(InterviewReply.LooksLikeAClose("What did the boy say when you told him?"));
        Assert.IsFalse(InterviewReply.LooksLikeAClose(null));
    }
}
