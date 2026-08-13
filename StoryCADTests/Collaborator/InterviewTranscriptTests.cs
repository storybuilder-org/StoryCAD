using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator.Models;

namespace StoryCADTests.Collaborator;

/// <summary>Collaborator #119: transcript assembly.</summary>
[TestClass]
public class InterviewTranscriptTests
{
    [TestMethod]
    public void NewTranscript_IsEmpty()
    {
        var transcript = new InterviewTranscript();

        Assert.IsTrue(transcript.IsEmpty);
        Assert.AreEqual(string.Empty, transcript.ToPromptText());
    }

    [TestMethod]
    public void SectionTurn_IsLabelledBySectionId()
    {
        var transcript = new InterviewTranscript();
        transcript.AddSection("Origin", "I was born in East Harlem.");

        var text = transcript.ToPromptText();

        StringAssert.Contains(text, "Origin");
        StringAssert.Contains(text, "I was born in East Harlem.");
        Assert.IsFalse(transcript.IsEmpty);
    }

    [TestMethod]
    public void FreeQuestion_KeepsTheWritersWording()
    {
        var transcript = new InterviewTranscript();
        transcript.AddFreeQuestion("Why did you stay?", "Because leaving costs more.");

        var text = transcript.ToPromptText();

        StringAssert.Contains(text, "Why did you stay?");
        StringAssert.Contains(text, "Because leaving costs more.");
    }

    [TestMethod]
    public void TurnsAreOrderedAsAsked()
    {
        var transcript = new InterviewTranscript();
        transcript.AddSection("PresentWork", "I supply.");
        transcript.AddFreeQuestion("Do you use?", "Never.");
        transcript.AddSection("LowPoint", "The year my mother moved us.");

        Assert.AreEqual(3, transcript.Turns.Count);
        Assert.AreEqual("PresentWork", transcript.Turns[0].Label);
        Assert.AreEqual("Do you use?", transcript.Turns[1].Label);
        Assert.AreEqual("LowPoint", transcript.Turns[2].Label);
    }

    [TestMethod]
    public void NotesText_IsPlainAndNamesTheCharacter()
    {
        var transcript = new InterviewTranscript();
        transcript.AddSection("Origin", "I was born in East Harlem.");

        var notes = transcript.ToNotesText("Charlie Lacas");

        StringAssert.Contains(notes, "Charlie Lacas");
        StringAssert.Contains(notes, "I was born in East Harlem.");
        // Notes renders no markup; the plain-text rule applies (COACH_STANCE).
        Assert.IsFalse(notes.Contains('*'));
        Assert.IsFalse(notes.Contains('#'));
    }

    [TestMethod]
    public void EmptyReplies_AreNotRecorded()
    {
        var transcript = new InterviewTranscript();
        transcript.AddSection("Origin", "   ");

        Assert.IsTrue(transcript.IsEmpty);
    }
}
