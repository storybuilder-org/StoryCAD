using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #119: the saved record is what appeared on screen.
/// </summary>
[TestClass]
public class InterviewTranscriptTests
{
    private static InterviewTranscript TwoTurns()
    {
        var transcript = new InterviewTranscript();
        transcript.Add("Flaw", 1,
            "Mara, you want something in this story you do not have yet. That's fair to say?",
            "Yes. I want the shop back.");
        transcript.Add("Flaw", 3,
            "That habit used to keep you safe. When did it start being useful?",
            "After the fire. I stopped telling anyone anything true.");
        return transcript;
    }

    [TestMethod]
    public void ToNotesText_KeepsEveryQuestionAndAnswerVerbatim()
    {
        var notes = TwoTurns().ToNotesText("Mara");

        StringAssert.Contains(notes,
            "Mara, you want something in this story you do not have yet. That's fair to say?");
        StringAssert.Contains(notes, "Yes. I want the shop back.");
        StringAssert.Contains(notes,
            "That habit used to keep you safe. When did it start being useful?");
        StringAssert.Contains(notes, "After the fire. I stopped telling anyone anything true.");
    }

    [TestMethod]
    public void ToNotesText_AddsNothingButTheHeading()
    {
        // A digest is what Terry rejected. The only text this may introduce is the title.
        var notes = TwoTurns().ToNotesText("Mara");
        var body = notes.Replace("Interview with Mara", string.Empty);

        foreach (var line in body.Split('\n'))
        {
            var text = line.Trim();
            if (text.Length == 0) continue;
            Assert.IsTrue(text.StartsWith("Q: ") || text.StartsWith("A: "),
                $"Saved text contains a line that is neither a question nor an answer: {text}");
        }
    }

    [TestMethod]
    public void ToNotesText_KeepsTurnsInOrder()
    {
        var notes = TwoTurns().ToNotesText("Mara");

        Assert.IsTrue(
            notes.IndexOf("I want the shop back", System.StringComparison.Ordinal)
            < notes.IndexOf("After the fire", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void ToPromptText_TagsEachTurnWithItsCursor()
    {
        // The Worker reads these tags to tell which lines are asked and whether it has
        // already spent its one follow-up on a line.
        var prompt = TwoTurns().ToPromptText();

        StringAssert.Contains(prompt, "[Flaw:1]");
        StringAssert.Contains(prompt, "[Flaw:3]");
    }

    [TestMethod]
    public void Add_IgnoresATurnWithNoAnswer()
    {
        // The writer can close the panel on a question they never replied to. A dangling
        // prompt in the saved record reads as a refusal rather than as an unseen question.
        var transcript = new InterviewTranscript();
        transcript.Add("Flaw", 1, "A question nobody answered", string.Empty);

        Assert.IsTrue(transcript.IsEmpty);
    }
}

/// <summary>
/// Collaborator #119: the verdict the interviewer returns with each question.
///
/// The model reports only its judgement of the answer. Position is arithmetic and
/// lives on the client: an earlier build had the model report a field and line with
/// every question, and it drifted out of step with the line it was actually asking.
/// </summary>
[TestClass]
public class InterviewReplyTests
{
    [TestMethod]
    public void Parse_SplitsVerdictFromQuestion()
    {
        var reply = InterviewReply.Parse(
            "VERDICT: ANSWERED\nThat habit used to keep you safe. When did it start being useful?");

        Assert.AreEqual(InterviewVerdict.Answered, reply.Verdict);
        Assert.AreEqual(
            "That habit used to keep you safe. When did it start being useful?", reply.Question);
    }

    [TestMethod]
    public void Parse_NeverLeavesTheHeaderInTheQuestion()
    {
        var reply = InterviewReply.Parse("VERDICT: RETRY\nWhich one goes first?");

        Assert.IsFalse(reply.Question.Contains("VERDICT"));
    }

    [TestMethod]
    public void Parse_ReadsEveryVerdict()
    {
        Assert.AreEqual(InterviewVerdict.Retry,
            InterviewReply.Parse("VERDICT: RETRY\nq").Verdict);
        Assert.AreEqual(InterviewVerdict.FollowUp,
            InterviewReply.Parse("VERDICT: FOLLOWUP\nq").Verdict);
        Assert.AreEqual(InterviewVerdict.Done,
            InterviewReply.Parse("VERDICT: DONE\nThat is everything.").Verdict);
    }

    [TestMethod]
    public void Parse_AcceptsAHyphenatedFollowUp()
    {
        Assert.AreEqual(InterviewVerdict.FollowUp,
            InterviewReply.Parse("VERDICT: FOLLOW-UP\nWho was in the room?").Verdict);
    }

    [TestMethod]
    public void Parse_MissingHeader_ShowsTheQuestionAndWalksOn()
    {
        // Answered rather than Retry: stalling on the same question is the failure a
        // writer cannot get themselves out of.
        var reply = InterviewReply.Parse("Who was in the room?");

        Assert.AreEqual(InterviewVerdict.Answered, reply.Verdict);
        Assert.AreEqual("Who was in the room?", reply.Question);
    }

    [TestMethod]
    public void Parse_UnknownVerdict_WalksOnRatherThanThrowing()
    {
        var reply = InterviewReply.Parse("VERDICT: MAYBE\nWhat do you not see?");

        Assert.AreEqual(InterviewVerdict.Answered, reply.Verdict);
        Assert.AreEqual("What do you not see?", reply.Question);
    }

    [TestMethod]
    public void Parse_ToleratesSpacingAndCase()
    {
        var reply = InterviewReply.Parse("verdict:retry\nWhat do you not see?");

        Assert.AreEqual(InterviewVerdict.Retry, reply.Verdict);
        Assert.AreEqual("What do you not see?", reply.Question);
    }

    [TestMethod]
    public void Parse_EmptyReply_YieldsNoQuestion()
    {
        Assert.AreEqual(string.Empty, InterviewReply.Parse(string.Empty).Question);
    }

    [TestMethod]
    public void Parse_MultiLineQuestion_KeepsTheWholeBody()
    {
        var reply = InterviewReply.Parse("VERDICT: ANSWERED\nFirst line.\nSecond line.");

        Assert.AreEqual("First line.\nSecond line.", reply.Question);
    }
}

/// <summary>
/// Collaborator #119: the cursor the client advances, so the model never has to.
/// </summary>
[TestClass]
public class InterviewScriptTests
{
    [TestMethod]
    public void FirstQuestion_IsFlawLineOne()
    {
        // Flaw first because it is the field a form is most often missing.
        Assert.AreEqual(("Flaw", 1), InterviewScript.First);
    }

    [TestMethod]
    public void Next_WalksTheLinesWithinABlock()
    {
        Assert.AreEqual(("Flaw", 2), InterviewScript.Next("Flaw", 1));
        Assert.AreEqual(("Flaw", 4), InterviewScript.Next("Flaw", 3));
    }

    [TestMethod]
    public void Next_OpensTheFollowingBlockAtItsFirstLine()
    {
        // The boundary the model got wrong: it re-asked the answered line instead of
        // opening the next block.
        Assert.AreEqual(("BackStory", 1), InterviewScript.Next("Flaw", 4));
        Assert.AreEqual(("Values", 1), InterviewScript.Next("BackStory", 2));
    }

    [TestMethod]
    public void Next_EndsAfterTheLastLineOfTheLastBlock()
    {
        var last = InterviewScript.Blocks[InterviewScript.Blocks.Count - 1];

        Assert.AreEqual("Description", last.Field);
        Assert.IsNull(InterviewScript.Next(last.Field, last.Lines));
    }

    [TestMethod]
    public void Next_AnUnknownFieldEndsTheInterviewRatherThanThrowing()
    {
        // A garbled cursor must close cleanly, not crash a session with answers in it.
        Assert.IsNull(InterviewScript.Next("Nonsense", 1));
        Assert.IsNull(InterviewScript.Next(string.Empty, 0));
    }

    [TestMethod]
    public void Blocks_MatchTerrysOrderAndCounts()
    {
        var expected = new[]
        {
            ("Flaw", 4), ("BackStory", 2), ("Values", 2), ("Enneagram", 3), ("Focus", 2),
            ("PsychNotes", 2), ("Abnormality", 2), ("Intelligence", 2), ("TraitList", 3),
            ("Role", 2), ("StoryRole", 2), ("Archetype", 2), ("Description", 2)
        };

        CollectionAssert.AreEqual(expected,
            InterviewScript.Blocks.Select(b => (b.Field, b.Lines)).ToArray());
    }

    [TestMethod]
    public void ThinBackStoryAnswer_TakesTerrysScriptedFollowUp()
    {
        // "If the answer is only a year or a place" -- the client evaluates the
        // condition; the Worker still owns the wording.
        Assert.IsTrue(InterviewScript.WantsScriptedFollowUp(
            "BackStory", 2, "Nineteen eighty-eight.", followUpUsed: false));
        Assert.IsTrue(InterviewScript.WantsScriptedFollowUp(
            "BackStory", 2, "Back in Newark.", followUpUsed: false));
    }

    [TestMethod]
    public void FullBackStoryAnswer_DoesNotForceAFollowUp()
    {
        Assert.IsFalse(InterviewScript.WantsScriptedFollowUp(
            "BackStory", 2,
            "My brother went to the cops and I watched what it did to him.",
            followUpUsed: false));
    }

    [TestMethod]
    public void ScriptedFollowUp_IsSpentOnce()
    {
        Assert.IsFalse(InterviewScript.WantsScriptedFollowUp(
            "BackStory", 2, "Nineteen eighty-eight.", followUpUsed: true));
    }

    [TestMethod]
    public void ScriptedFollowUp_BelongsToOnePositionOnly()
    {
        // Terry scripts exactly one. Everywhere else the follow-up is judgement.
        Assert.IsFalse(InterviewScript.WantsScriptedFollowUp(
            "BackStory", 1, "Right.", followUpUsed: false));
        Assert.IsFalse(InterviewScript.WantsScriptedFollowUp(
            "Flaw", 3, "Nineteen.", followUpUsed: false));
    }

    [TestMethod]
    public void ScriptedFollowUp_IgnoresAnEmptyAnswer()
    {
        Assert.IsFalse(InterviewScript.WantsScriptedFollowUp(
            "BackStory", 2, "   ", followUpUsed: false));
        Assert.IsFalse(InterviewScript.WantsScriptedFollowUp(
            "BackStory", 2, null, followUpUsed: false));
    }

    [TestMethod]
    public void TheWholeScriptIsThirtyQuestions()
    {
        Assert.AreEqual(30, InterviewScript.TotalLines);
    }

    [TestMethod]
    public void NoQuestionTextLivesOnThisSide()
    {
        // ADR-005: the bank is Terry's craft material and this repo is public. Field
        // ids and counts are structure; the wording stays on the Worker.
        foreach (var block in InterviewScript.Blocks)
        {
            Assert.IsFalse(block.Field.Contains(' '),
                $"{block.Field} reads as prose rather than a property name.");
            Assert.IsFalse(block.Field.Contains('?'));
        }
    }

    [TestMethod]
    public void WalkingFromTheStartVisitsEveryLineExactlyOnce()
    {
        var visited = new List<(string, int)>();
        var position = (Field: InterviewScript.First.Field, Line: InterviewScript.First.Line);

        while (true)
        {
            visited.Add(position);
            var next = InterviewScript.Next(position.Field, position.Line);
            if (next == null) break;
            position = next.Value;
        }

        Assert.AreEqual(InterviewScript.TotalLines, visited.Count);
        Assert.AreEqual(visited.Count, visited.Distinct().Count());
    }
}

/// <summary>
/// Collaborator #119: the cursor, and what a question is allowed to cost.
/// </summary>
[TestClass]
public class InterviewCursorTests
{
    private static InterviewCursor Started()
    {
        var cursor = new InterviewCursor();
        cursor.Start();
        return cursor;
    }

    [TestMethod]
    public void Start_LandsOnTheFirstQuestion()
    {
        var cursor = Started();

        Assert.AreEqual("Flaw", cursor.Field);
        Assert.AreEqual(1, cursor.Line);
        Assert.IsFalse(cursor.NotStarted);
    }

    [TestMethod]
    public void Answered_MovesOn()
    {
        var cursor = Started();

        cursor.Apply(InterviewVerdict.Answered, cursor.Next());

        Assert.AreEqual(("Flaw", 2), (cursor.Field, cursor.Line));
    }

    [TestMethod]
    public void FollowUp_HoldsPositionAndSpendsItsOneChance()
    {
        var cursor = Started();

        cursor.Apply(InterviewVerdict.FollowUp, cursor.Next());

        Assert.AreEqual(("Flaw", 1), (cursor.Field, cursor.Line));
        Assert.IsTrue(cursor.FollowUpUsed);
    }

    [TestMethod]
    public void MovingOn_GivesTheNextQuestionAFreshFollowUp()
    {
        var cursor = Started();
        cursor.Apply(InterviewVerdict.FollowUp, cursor.Next());

        cursor.Apply(InterviewVerdict.Answered, cursor.Next());

        Assert.IsFalse(cursor.FollowUpUsed);
    }

    [TestMethod]
    public void Retry_HoldsPositionWhileRetriesRemain()
    {
        var cursor = Started();

        cursor.Apply(InterviewVerdict.Retry, cursor.Next());

        Assert.AreEqual(("Flaw", 1), (cursor.Field, cursor.Line));
        Assert.AreEqual(1, cursor.Retries);
    }

    [TestMethod]
    public void Retry_MovesOnOnceTheCapIsReached()
    {
        // Observed live: Flaw line 4 asked five times running, with no way past it.
        // Each verdict was defensible; the writer was still stuck.
        var cursor = Started();

        for (var i = 0; i < InterviewCursor.MaxRetries; i++)
            cursor.Apply(InterviewVerdict.Retry, cursor.Next());

        Assert.AreEqual(("Flaw", 1), (cursor.Field, cursor.Line));

        cursor.Apply(InterviewVerdict.Retry, cursor.Next());

        Assert.AreEqual(("Flaw", 2), (cursor.Field, cursor.Line));
        Assert.AreEqual(0, cursor.Retries);
    }

    [TestMethod]
    public void NoQuestionIsEverAskedMoreThanThreeTimes()
    {
        // Whatever the interviewer keeps returning, the writer gets a way forward.
        var cursor = Started();
        var asks = 1;

        for (var turn = 0; turn < 20; turn++)
        {
            var before = (cursor.Field, cursor.Line);
            cursor.Apply(InterviewVerdict.Retry, cursor.Next());
            asks = (cursor.Field, cursor.Line) == before ? asks + 1 : 1;

            Assert.IsTrue(asks <= 3, $"{cursor.Field} line {cursor.Line} was asked {asks} times.");
        }
    }

    [TestMethod]
    public void AnAnswerResetsTheRetryCount()
    {
        var cursor = Started();
        cursor.Apply(InterviewVerdict.Retry, cursor.Next());

        cursor.Apply(InterviewVerdict.Answered, cursor.Next());

        Assert.AreEqual(0, cursor.Retries);
    }

    [TestMethod]
    public void FollowUpResetsTheRetryCount()
    {
        // A detail offered after a dodge is progress, not another dodge.
        var cursor = Started();
        cursor.Apply(InterviewVerdict.Retry, cursor.Next());

        cursor.Apply(InterviewVerdict.FollowUp, cursor.Next());

        Assert.AreEqual(0, cursor.Retries);
    }

    [TestMethod]
    public void AtTheEndOfTheScript_MovingOnStaysPut()
    {
        var cursor = new InterviewCursor();
        cursor.Start();
        while (cursor.Next() != null)
            cursor.Apply(InterviewVerdict.Answered, cursor.Next());

        Assert.AreEqual("Description", cursor.Field);

        cursor.Apply(InterviewVerdict.Answered, cursor.Next());

        Assert.AreEqual("Description", cursor.Field);
    }

    [TestMethod]
    public void Reset_PutsItBackBeforeTheFirstQuestion()
    {
        var cursor = Started();
        cursor.Apply(InterviewVerdict.Retry, cursor.Next());

        cursor.Reset();

        Assert.IsTrue(cursor.NotStarted);
        Assert.AreEqual(0, cursor.Retries);
    }
}
