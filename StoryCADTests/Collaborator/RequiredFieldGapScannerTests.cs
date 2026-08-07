using CollaboratorLib.Context;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #160: Problem spine essentials include ConflictType, ProblemType, Subject.
/// Flaw/BackStory remain Character input-only (not gap-required).
/// </summary>
[TestClass]
public class RequiredFieldGapScannerTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    [TestMethod]
    public void Problem_BlankScalars_Includes_ProblemType_ConflictType_Subject()
    {
        var api = CreateApi();
        var model = new StoryModel();
        var problem = new ProblemModel("Test Problem", model, null)
        {
            Description = "Story question text",
            // leave ProblemType, ConflictType, Subject, GMC, etc. empty
        };

        var missing = RequiredFieldGapScanner.GetMissingProperties(api, problem);

        CollectionAssert.Contains(missing.ToList(), "ProblemType");
        CollectionAssert.Contains(missing.ToList(), "ConflictType");
        CollectionAssert.Contains(missing.ToList(), "Subject");
    }

    [TestMethod]
    public void Problem_FilledNewFields_DoesNotReportThemMissing()
    {
        var api = CreateApi();
        var model = new StoryModel();
        var problem = new ProblemModel("Test Problem", model, null)
        {
            Description = "Will they succeed?",
            ProblemCategory = "Story problem",
            ProblemType = "Conflict",
            ConflictType = "Person vs. Self",
            Subject = "Ambition",
            Premise = "A man destroys himself for power",
            ProtGoal = "Kingship",
            ProtMotive = "Prophecy",
            ProtConflict = "Conscience",
            AntagGoal = "Kingship",
            AntagMotive = "Fear",
            AntagConflict = "Conscience",
            Outcome = "Both die"
        };

        var missing = RequiredFieldGapScanner.GetMissingProperties(api, problem);

        CollectionAssert.DoesNotContain(missing.ToList(), "ProblemType");
        CollectionAssert.DoesNotContain(missing.ToList(), "ConflictType");
        CollectionAssert.DoesNotContain(missing.ToList(), "Subject");
        // Links still empty → still a gap
        CollectionAssert.Contains(missing.ToList(), "Protagonist");
        CollectionAssert.Contains(missing.ToList(), "Antagonist");
    }

    [TestMethod]
    public void Character_DoesNotRequire_Flaw_Or_BackStory()
    {
        var api = CreateApi();
        var model = new StoryModel();
        var character = new CharacterModel("Macbeth", model, null)
        {
            Description = "Thane",
            Role = "General",
            StoryRole = "Protagonist"
            // Flaw / BackStory intentionally empty
        };

        var missing = RequiredFieldGapScanner.GetMissingProperties(api, character);

        Assert.AreEqual(0, missing.Count, string.Join(", ", missing));
        CollectionAssert.DoesNotContain(missing.ToList(), "Flaw");
        CollectionAssert.DoesNotContain(missing.ToList(), "BackStory");
    }

    [TestMethod]
    public void GapOwnership_NewProblemFields_PointToStoryProblem()
    {
        foreach (var prop in new[] { "ProblemType", "ConflictType", "Subject" })
        {
            var owners = GapWorkflowOwnership.WorkflowsFor(StoryItemType.Problem, prop);
            Assert.AreEqual(1, owners.Count, prop);
            Assert.AreEqual("StoryProblem", owners[0], prop);
        }

        Assert.AreEqual("Problem Type",
            GapWorkflowOwnership.DisplayLabel(StoryItemType.Problem, "ProblemType"));
        Assert.AreEqual("Conflict Type",
            GapWorkflowOwnership.DisplayLabel(StoryItemType.Problem, "ConflictType"));
        Assert.AreEqual("Subject",
            GapWorkflowOwnership.DisplayLabel(StoryItemType.Problem, "Subject"));
    }
}
