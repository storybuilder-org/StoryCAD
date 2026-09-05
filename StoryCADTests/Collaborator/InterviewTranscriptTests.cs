using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

[TestClass]
public class InterviewTranscriptTests
{
    private static InterviewTranscript TwoTurns()
    {
        var transcript = new InterviewTranscript();
        transcript.Add("Flaw",
            "Mara, you want something in this story you do not have yet. That's fair to say?",
            "Yes. I want the shop back.");
        transcript.Add("Flaw",
            "What did Chronos take from you the first time?",
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
        StringAssert.Contains(notes, "What did Chronos take from you the first time?");
        StringAssert.Contains(notes, "After the fire. I stopped telling anyone anything true.");
    }

    [TestMethod]
    public void ToNotesText_AddsNothingButTheHeading()
    {
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
    public void ToPromptText_TagsFieldOnly()
    {
        var prompt = TwoTurns().ToPromptText();

        StringAssert.Contains(prompt, "[Flaw]");
        Assert.IsFalse(prompt.Contains("[Flaw:"), "Prompt tags must not carry a bank line index.");
    }

    [TestMethod]
    public void Add_IgnoresATurnWithNoAnswer()
    {
        var transcript = new InterviewTranscript();
        transcript.Add("Flaw", "A question nobody answered", string.Empty);

        Assert.IsTrue(transcript.IsEmpty);
    }
}

[TestClass]
public class InterviewReplyTests
{
    [TestMethod]
    public void Parse_KeepRetryFollowUp_AreKeepAsking()
    {
        Assert.AreEqual(InterviewVerdict.KeepAsking,
            InterviewReply.Parse("VERDICT: KEEP\nq").Verdict);
        Assert.AreEqual(InterviewVerdict.KeepAsking,
            InterviewReply.Parse("VERDICT: RETRY\nq").Verdict);
        Assert.AreEqual(InterviewVerdict.KeepAsking,
            InterviewReply.Parse("VERDICT: FOLLOWUP\nq").Verdict);
        Assert.AreEqual(InterviewVerdict.KeepAsking,
            InterviewReply.Parse("VERDICT: FOLLOW-UP\nq").Verdict);
    }

    [TestMethod]
    public void Parse_AnsweredAndGotIt_AreGotIt()
    {
        Assert.AreEqual(InterviewVerdict.GotIt,
            InterviewReply.Parse("VERDICT: GOTIT\nq").Verdict);
        Assert.AreEqual(InterviewVerdict.GotIt,
            InterviewReply.Parse("VERDICT: ANSWERED\nq").Verdict);
    }

    [TestMethod]
    public void Parse_NotThis_WithOrWithoutHyphen()
    {
        Assert.AreEqual(InterviewVerdict.NotThis,
            InterviewReply.Parse("VERDICT: NOTTHIS\nq").Verdict);
        Assert.AreEqual(InterviewVerdict.NotThis,
            InterviewReply.Parse("VERDICT: NOT-THIS\nq").Verdict);
    }

    [TestMethod]
    public void Parse_MissingHeaderEmptyAndUnknown_AreKeepAsking()
    {
        Assert.AreEqual(InterviewVerdict.KeepAsking,
            InterviewReply.Parse("Who was in the room?").Verdict);
        Assert.AreEqual(InterviewVerdict.KeepAsking,
            InterviewReply.Parse(string.Empty).Verdict);
        Assert.AreEqual(InterviewVerdict.KeepAsking,
            InterviewReply.Parse("VERDICT: MAYBE\nWhat do you not see?").Verdict);
    }

    [TestMethod]
    public void Parse_EmptyBody_KeepsVerdictAndEmptyQuestion()
    {
        var reply = InterviewReply.Parse("VERDICT: GOTIT\n");

        Assert.AreEqual(InterviewVerdict.GotIt, reply.Verdict);
        Assert.AreEqual(string.Empty, reply.Question);
    }

    [TestMethod]
    public void EmptyGotItBody_DoesNotApply()
    {
        Assert.IsFalse(InterviewReply.ShouldApply(opening: false, question: string.Empty));
    }

    [TestMethod]
    public void Parse_NeverLeavesTheHeaderInTheQuestion()
    {
        var reply = InterviewReply.Parse("VERDICT: KEEP\nWhich one goes first?");

        Assert.IsFalse(reply.Question.Contains("VERDICT"));
    }

    [TestMethod]
    public void Parse_ToleratesSpacingAndCase()
    {
        var reply = InterviewReply.Parse("verdict:retry\nWhat do you not see?");

        Assert.AreEqual(InterviewVerdict.KeepAsking, reply.Verdict);
        Assert.AreEqual("What do you not see?", reply.Question);
    }

    [TestMethod]
    public void Parse_LeadingBlankLine_StillReadsTheHeader()
    {
        var reply = InterviewReply.Parse("\n\nVERDICT: GOTIT\nWhen did that first work?");

        Assert.AreEqual(InterviewVerdict.GotIt, reply.Verdict);
        Assert.AreEqual("When did that first work?", reply.Question);
    }

    [TestMethod]
    public void Parse_SpacedTokens_GotItAndNotThis()
    {
        Assert.AreEqual(InterviewVerdict.GotIt,
            InterviewReply.Parse("VERDICT: GOT IT\nq").Verdict);
        Assert.AreEqual(InterviewVerdict.NotThis,
            InterviewReply.Parse("Verdict: Not this\nq").Verdict);
    }

    [TestMethod]
    public void ExtendLastAnswer_AppendsToTheLastTurn()
    {
        var transcript = new InterviewTranscript();
        transcript.Add("Flaw", "That's fair to say?", "Yes.");

        var extended = transcript.ExtendLastAnswer("I want the Box shut.");

        Assert.IsTrue(extended);
        Assert.AreEqual(1, transcript.Turns.Count);
        Assert.AreEqual("Yes. I want the Box shut.", transcript.Turns[0].Answer);
        Assert.AreEqual("That's fair to say?", transcript.Turns[0].Question);
    }

    [TestMethod]
    public void ExtendLastAnswer_WithNothingToExtend_ReturnsFalse()
    {
        var transcript = new InterviewTranscript();

        Assert.IsFalse(transcript.ExtendLastAnswer("anything"));

        transcript.Add("Flaw", "q", "a");
        Assert.IsFalse(transcript.ExtendLastAnswer("   "));
        Assert.AreEqual("a", transcript.Turns[0].Answer);
    }
}

[TestClass]
public class InterviewScriptTests
{
    [TestMethod]
    public void First_IsFlaw()
    {
        Assert.AreEqual("Flaw", InterviewScript.First);
    }

    [TestMethod]
    public void NextField_Flaw_IsBackStory()
    {
        Assert.AreEqual("BackStory", InterviewScript.NextField("Flaw"));
    }

    [TestMethod]
    public void NextField_Unknown_IsNull()
    {
        Assert.IsNull(InterviewScript.NextField("Nonsense"));
        Assert.IsNull(InterviewScript.NextField(string.Empty));
    }

    [TestMethod]
    public void NextField_Description_IsNull()
    {
        Assert.IsNull(InterviewScript.NextField("Description"));
    }
}

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
    public void Start_LandsOnFlaw()
    {
        var cursor = Started();

        Assert.AreEqual("Flaw", cursor.Field);
        Assert.AreEqual(0, cursor.TurnsOnField);
        Assert.IsFalse(cursor.NotStarted);
    }

    [TestMethod]
    public void Apply_KeepAsking_HoldsFlawAndIncrementsTurns()
    {
        var cursor = Started();

        cursor.Apply(InterviewVerdict.KeepAsking);

        Assert.AreEqual("Flaw", cursor.Field);
        Assert.AreEqual(1, cursor.TurnsOnField);
    }

    [TestMethod]
    public void Apply_GotIt_MovesToBackStory()
    {
        var cursor = Started();

        cursor.Apply(InterviewVerdict.GotIt);

        Assert.AreEqual("BackStory", cursor.Field);
        Assert.AreEqual(0, cursor.TurnsOnField);
    }

    [TestMethod]
    public void Apply_NotThis_MovesToBackStory()
    {
        var cursor = Started();

        cursor.Apply(InterviewVerdict.NotThis);

        Assert.AreEqual("BackStory", cursor.Field);
    }

    [TestMethod]
    public void Apply_KeepAsking_ManyTimes_StillHoldsFlaw()
    {
        var cursor = Started();

        for (var i = 0; i < 20; i++)
            cursor.Apply(InterviewVerdict.KeepAsking);

        Assert.AreEqual("Flaw", cursor.Field);
        Assert.AreEqual(20, cursor.TurnsOnField);
    }

    [TestMethod]
    public void Reset_PutsItBackBeforeTheFirstQuestion()
    {
        var cursor = Started();
        cursor.Apply(InterviewVerdict.KeepAsking);

        cursor.Reset();

        Assert.IsTrue(cursor.NotStarted);
        Assert.AreEqual(0, cursor.TurnsOnField);
    }
}
