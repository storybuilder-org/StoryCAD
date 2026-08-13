using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>Collaborator #119: section catalog and setting gating.</summary>
[TestClass]
public class InterviewSectionCatalogTests
{
    [TestMethod]
    public void Catalog_HasTenSectionsInArcOrder()
    {
        var ids = InterviewSectionCatalog.All.Select(s => s.Id).ToList();

        Assert.AreEqual(10, ids.Count);
        Assert.AreEqual("PresentWork", ids[0]);
        Assert.AreEqual("Origin", ids[1]);
        Assert.AreEqual("Schooling", ids[2]);
        Assert.AreEqual("Advice", ids[8]);
        Assert.AreEqual("StoryYouAreIn", ids[9]);
    }

    [TestMethod]
    public void Schooling_IsWithheldFromNonModernSettings()
    {
        var modern = InterviewSectionCatalog.ForSetting(isModernSetting: true).Select(s => s.Id).ToList();
        var other = InterviewSectionCatalog.ForSetting(isModernSetting: false).Select(s => s.Id).ToList();

        CollectionAssert.Contains(modern, "Schooling");
        CollectionAssert.DoesNotContain(other, "Schooling");
    }

    [TestMethod]
    public void NonModernSettings_KeepEverySectionThatDoesNotAssumeSchooling()
    {
        var other = InterviewSectionCatalog.ForSetting(isModernSetting: false).Select(s => s.Id).ToList();

        Assert.AreEqual(9, other.Count);
        CollectionAssert.Contains(other, "LowPoint");
        CollectionAssert.Contains(other, "StoryYouAreIn");
    }

    [TestMethod]
    public void NoSectionCarriesQuestionText()
    {
        // ADR-005: question wording lives on the Worker, never in this public repo.
        foreach (var section in InterviewSectionCatalog.All)
        {
            Assert.IsFalse(section.Title.Contains('?'), $"{section.Id} title looks like a question");
            Assert.IsFalse(section.Blurb.Contains('?'), $"{section.Id} blurb looks like a question");
        }
    }

    [TestMethod]
    public void StorySection_IsFlaggedAsWantingAProblem()
    {
        var story = InterviewSectionCatalog.All.Single(s => s.Id == "StoryYouAreIn");
        Assert.IsTrue(story.NeedsProblem);
    }
}
