using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Collaborator.Models;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCollaborator;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #237 item 11. Try Again means "give me something different". The runner
///     puts the rejected proposals on the wire as the RejectedProposals arg, and the pane's
///     rows render to that text one "DisplayName: value" line each. A first run sends nothing.
/// </summary>
[TestClass]
public class TryAgainRejectedProposalsTests
{
    private static WorkflowRunner CreateRunner()
    {
        var model = new StoryModel();
        var api = new StoryCADApi(
            Ioc.Default.GetRequiredService<OutlineService>(),
            Ioc.Default.GetRequiredService<ListData>(),
            Ioc.Default.GetRequiredService<ControlData>(),
            Ioc.Default.GetRequiredService<ToolsData>());
        api.CurrentModel = model;
        var workflow = new Workflow("Premise", "Ideation (Story idea => Concept => Premise)", "test", StoryItemType.StoryOverview);
        return new WorkflowRunner(model, workflow, api);
    }

    [TestMethod]
    public void ApplyRejectedProposals_FirstRun_AddsNoArg()
    {
        var runner = CreateRunner();
        var args = new Dictionary<string, string>();

        runner.ApplyRejectedProposals(args);

        Assert.IsFalse(args.ContainsKey("RejectedProposals"), "a first run has nothing to depart from");
    }

    [TestMethod]
    public void ApplyRejectedProposals_Blank_AddsNoArg()
    {
        var runner = CreateRunner();
        runner.RejectedProposals = "   ";
        var args = new Dictionary<string, string>();

        runner.ApplyRejectedProposals(args);

        Assert.IsFalse(args.ContainsKey("RejectedProposals"));
    }

    [TestMethod]
    public void ApplyRejectedProposals_TryAgain_SendsTheTextAsIs()
    {
        var runner = CreateRunner();
        runner.RejectedProposals = "Concept: What if a parent chases a lost child?\nPremise: A parent chases a lost child.";
        var args = new Dictionary<string, string>();

        runner.ApplyRejectedProposals(args);

        Assert.AreEqual(runner.RejectedProposals, args["RejectedProposals"]);
    }

    [TestMethod]
    public void FormatRejectedProposals_OneLinePerRow_DisplayNameThenValue()
    {
        var rows = new[]
        {
            new PendingUpdateItem { Key = "Overview.Concept", PropertyDisplayName = "Concept", ProposedDisplay = "What if a parent chases a lost child?" },
            new PendingUpdateItem { Key = "Overview.Premise", PropertyDisplayName = "Premise", ProposedDisplay = " A parent chases a lost child. " }
        };

        var text = WorkflowRunner.FormatRejectedProposals(rows);

        Assert.AreEqual(
            "Concept: What if a parent chases a lost child?\nPremise: A parent chases a lost child.",
            text);
    }

    [TestMethod]
    public void FormatRejectedProposals_SkipsRowsWithNoProposedText()
    {
        var rows = new[]
        {
            new PendingUpdateItem { Key = "Overview.Description", PropertyDisplayName = "Story Idea", ProposedDisplay = "" },
            new PendingUpdateItem { Key = "Overview.Premise", PropertyDisplayName = "Premise", ProposedDisplay = "A parent chases a lost child." }
        };

        var text = WorkflowRunner.FormatRejectedProposals(rows);

        Assert.AreEqual("Premise: A parent chases a lost child.", text);
    }

    [TestMethod]
    public void FormatRejectedProposals_NoRows_IsEmpty()
    {
        Assert.AreEqual(string.Empty, WorkflowRunner.FormatRejectedProposals(Array.Empty<PendingUpdateItem>()));
    }

    [TestMethod]
    public void FormatRejectedProposals_CutsAValueAtFourThousandCharacters()
    {
        var rows = new[]
        {
            new PendingUpdateItem { Key = "Problem.BeatSheet", PropertyDisplayName = "Beats", ProposedDisplay = new string('x', 5000) }
        };

        var text = WorkflowRunner.FormatRejectedProposals(rows);

        Assert.AreEqual("Beats: ".Length + 4000, text.Length);
    }
}
