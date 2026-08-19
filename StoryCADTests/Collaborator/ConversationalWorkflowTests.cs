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
            "VERDICT: KEEP\nYou want something you do not have yet. That's fair to say?");

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
    public void OpeningTurn_SendsFlawAndEmptyAnswer()
    {
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(
            args, field: "Flaw", nextField: "BackStory", turnsOnField: 0,
            transcript: "", answer: null);

        Assert.AreEqual("Flaw", args["InterviewField"]);
        Assert.AreEqual("BackStory", args["InterviewNextField"]);
        Assert.AreEqual("0", args["InterviewTurnsOnField"]);
        Assert.AreEqual(string.Empty, args["InterviewAnswer"]);
        Assert.IsFalse(args.ContainsKey("InterviewLine"));
    }

    [TestMethod]
    public void AfterOpen_DoesNotSendInterviewLine()
    {
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(
            args, field: "Flaw", nextField: "BackStory", turnsOnField: 0,
            transcript: "[Flaw] Q: That's fair to say?\nA: Yes.",
            answer: "After the fire.");

        Assert.AreEqual("Flaw", args["InterviewField"]);
        Assert.AreEqual("After the fire.", args["InterviewAnswer"]);
        Assert.IsFalse(args.ContainsKey("InterviewLine"));
        Assert.IsFalse(args.ContainsKey("InterviewNextLine"));
        StringAssert.Contains(args["InterviewTranscript"], "[Flaw]");
        Assert.IsFalse(args["InterviewTranscript"].Contains("[Flaw:"));
    }

    [TestMethod]
    public void LastField_SendsNoNextField()
    {
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(
            args, field: "Description", nextField: null, turnsOnField: 0,
            transcript: "", answer: "That is the spine.");

        Assert.AreEqual(string.Empty, args["InterviewNextField"]);
    }

    [TestMethod]
    public void EveryInterviewArgIsAlwaysPresent()
    {
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(args, null, null, 0, null, null);

        foreach (var key in new[]
                 {
                     "InterviewField", "InterviewNextField", "InterviewTurnsOnField",
                     "InterviewTranscript", "InterviewAnswer"
                 })
        {
            CollectionAssert.Contains(args.Keys, key);
        }

        Assert.IsFalse(args.ContainsKey("InterviewLine"));
        Assert.IsFalse(args.ContainsKey("InterviewRetryCount"));
        Assert.IsFalse(args.ContainsKey("InterviewForceFollowUp"));
    }
}
