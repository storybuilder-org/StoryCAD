using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>Tests of <see cref="StoryCollaborator.Collaborator"/>.</summary>
[TestClass]
public class CollaboratorTests
{
    private static readonly string[] TenKept =
    {
        "ProtGoal", "ProtMotive", "Premise", "Outcome", "Method",
        "Theme", "StoryQuestion", "AntagGoal", "AntagMotive", "Notes"
    };

    /// <summary>Collaborator #272: the summary names the properties the model kept.</summary>
    [TestMethod]
    public void BuildSummaryMessage_ProposalsAndKeptProperties_NamesEachKeptProperty()
    {
        var text = StoryCollaborator.Collaborator.BuildSummaryMessage(16, 8, TenKept);

        Assert.AreEqual(
            "Found 16 property update(s). 8 of them would replace text you wrote; those ask for confirmation. " +
            "10 fields are kept as they are: ProtGoal, ProtMotive, Premise, Outcome, Method, Theme, StoryQuestion, " +
            "AntagGoal, AntagMotive, Notes. Choose Accept All, Review Each, or Try Again. Chat can revise these proposals.",
            text);
    }

    [TestMethod]
    public void BuildSummaryMessage_NoProposalsAndKeptProperties_SaysNoUpdatesAndNamesThem()
    {
        var text = StoryCollaborator.Collaborator.BuildSummaryMessage(0, 0, new[] { "ProtGoal", "Premise" });

        Assert.AreEqual("No property updates. 2 fields are kept as they are: ProtGoal, Premise.", text);
    }

    [TestMethod]
    public void BuildSummaryMessage_OneKeptProperty_UsesSingular()
    {
        var text = StoryCollaborator.Collaborator.BuildSummaryMessage(3, 0, new[] { "ProtGoal" });

        StringAssert.Contains(text, " 1 field is kept as it is: ProtGoal. ");
    }

    [TestMethod]
    public void BuildSummaryMessage_NoKeptProperty_IsTheSameAsBefore()
    {
        Assert.AreEqual(
            "Found 16 property update(s). 8 of them would replace text you wrote; those ask for confirmation. " +
            "Choose Accept All, Review Each, or Try Again. Chat can revise these proposals.",
            StoryCollaborator.Collaborator.BuildSummaryMessage(16, 8, new string[0]));
        Assert.AreEqual(
            "No property updates were extracted from the response.",
            StoryCollaborator.Collaborator.BuildSummaryMessage(0, 0, new string[0]));
    }
}
