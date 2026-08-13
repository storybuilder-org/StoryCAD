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
            "I was born in East Harlem. My mother worked a register downtown.");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.PendingUpdates.Count);
        StringAssert.Contains(result.RawResponse, "East Harlem");
    }

    [TestMethod]
    public void EmptyReply_Fails()
    {
        var result = WorkflowRunner.BuildConversationalResult("   ");

        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void SectionTurn_SetsSectionAndClearsFreeQuestion()
    {
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(
            args, sectionId: "Origin", freeQuestion: null,
            transcript: "", chosenSections: "PresentWork,Origin");

        Assert.AreEqual("Origin", args["InterviewSection"]);
        Assert.AreEqual(string.Empty, args["InterviewFreeQuestion"]);
        Assert.AreEqual("PresentWork,Origin", args["InterviewChosenSections"]);
    }

    [TestMethod]
    public void FreeQuestionTurn_SetsQuestionAndClearsSection()
    {
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(
            args, sectionId: null, freeQuestion: "Do you use?",
            transcript: "[section Origin]\nEast Harlem.", chosenSections: "Origin");

        Assert.AreEqual(string.Empty, args["InterviewSection"]);
        Assert.AreEqual("Do you use?", args["InterviewFreeQuestion"]);
        StringAssert.Contains(args["InterviewTranscript"], "East Harlem.");
    }

    [TestMethod]
    public void EveryInterviewArgIsAlwaysPresent()
    {
        // The Worker merges {{$Var}} placeholders; a missing key merges as empty and
        // silently changes the prompt. Always write all four.
        var args = new Dictionary<string, string>();

        WorkflowRunner.SetInterviewArgs(args, null, null, null, null);

        CollectionAssert.Contains(args.Keys, "InterviewSection");
        CollectionAssert.Contains(args.Keys, "InterviewFreeQuestion");
        CollectionAssert.Contains(args.Keys, "InterviewTranscript");
        CollectionAssert.Contains(args.Keys, "InterviewChosenSections");
    }
}
