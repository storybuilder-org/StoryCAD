using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator;

namespace StoryCADTests.Collaborator;

/// <summary>Collaborator #119: conversational runs skip JSON extraction.</summary>
[TestClass]
public class ConversationalWorkflowTests
{
    [TestMethod]
    public void ProseReply_SucceedsWithNoPendingUpdates()
    {
        var result = WorkflowRunner.BuildConversationalResult(
            "FIELD: Flaw | LINE: 1\nYou want something you do not have yet. That's fair to say?");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.PendingUpdates.Count);
        StringAssert.Contains(result.RawResponse, "That's fair to say?");
    }

    [TestMethod]
    public void EmptyReply_Fails()
    {
        var result = WorkflowRunner.BuildConversationalResult("   ");

        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void OpeningTurn_SendsNoCurrentQuestionAndTheFirstLineAsNext()
    {
        // Nothing has been asked yet, so the Worker resolves an empty current question
        // and the model simply asks the next one.
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(
            args, field: "", line: 0, nextField: "Flaw", nextLine: 1,
            followUpUsed: false, forceFollowUp: false, retryCount: 0, transcript: "", answer: null);

        Assert.AreEqual(string.Empty, args["InterviewField"]);
        Assert.AreEqual(string.Empty, args["InterviewLine"]);
        Assert.AreEqual("Flaw", args["InterviewNextField"]);
        Assert.AreEqual("1", args["InterviewNextLine"]);
        Assert.AreEqual(string.Empty, args["InterviewAnswer"]);
    }

    [TestMethod]
    public void AnsweredTurn_SendsBothPositionsPrecomputed()
    {
        // The model picks between two resolved lines rather than retrieving one. Asked
        // to do the arithmetic itself it drifted, reporting one position while asking
        // another's question.
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(
            args, field: "Flaw", line: 4, nextField: "BackStory", nextLine: 1,
            followUpUsed: false, forceFollowUp: false, retryCount: 0,
            transcript: "[Flaw:1] Q: That's fair to say?\nA: Yes.",
            answer: "After the fire.");

        Assert.AreEqual("Flaw", args["InterviewField"]);
        Assert.AreEqual("4", args["InterviewLine"]);
        Assert.AreEqual("BackStory", args["InterviewNextField"]);
        Assert.AreEqual("1", args["InterviewNextLine"]);
        Assert.AreEqual("After the fire.", args["InterviewAnswer"]);
        StringAssert.Contains(args["InterviewTranscript"], "[Flaw:1]");
    }

    [TestMethod]
    public void LastTurn_SendsNoNextPosition()
    {
        // How the Worker is told the interview is over.
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(
            args, field: "Description", line: 2, nextField: null, nextLine: 0,
            followUpUsed: false, forceFollowUp: false, retryCount: 0,
            transcript: "", answer: "That is the spine.");

        Assert.AreEqual(string.Empty, args["InterviewNextField"]);
        Assert.AreEqual(string.Empty, args["InterviewNextLine"]);
    }

    [TestMethod]
    public void FollowUpUsed_IsSentRatherThanCounted()
    {
        // One bit the client already knows. Deriving it from the transcript is what
        // made the model lose its place.
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(
            args, "Flaw", 3, "Flaw", 4, followUpUsed: true, forceFollowUp: false,
            retryCount: 0, transcript: "", answer: "x");

        Assert.AreEqual("yes", args["InterviewFollowUpUsed"]);
    }

    [TestMethod]
    public void RetryCount_TellsTheWorkerWhenToRewordRatherThanRepeat()
    {
        // Terry: repeat the question with the premise still in it. The first re-ask is
        // that repeat; a second identical one is the wall a writer cannot get past, so
        // from there the Worker rewords instead, holding the premise.
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(
            args, "Flaw", 4, "BackStory", 1, followUpUsed: false, forceFollowUp: false,
            retryCount: 1, transcript: "", answer: "I don't know");

        Assert.AreEqual("1", args["InterviewRetryCount"]);
    }

    [TestMethod]
    public void EveryInterviewArgIsAlwaysPresent()
    {
        // The Worker merges {{$Var}} placeholders; a missing key merges as empty and
        // silently changes the prompt. Always write all of them.
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(args, null, 0, null, 0, false, false, 0, null, null);

        foreach (var key in new[]
                 {
                     "InterviewField", "InterviewLine", "InterviewNextField",
                     "InterviewNextLine", "InterviewFollowUpUsed", "InterviewForceFollowUp",
                     "InterviewRetryCount", "InterviewTranscript", "InterviewAnswer"
                 })
        {
            CollectionAssert.Contains(args.Keys, key);
        }
    }
}
