using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Collaborator.Models;
using StoryCADLib.Collaborator.ViewModels;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #261: the list of missing pieces on the Outline gaps page.
/// </summary>
public partial class GapWorkflowViewModelTests
{
    [TestMethod]
    public void Load_TwoMissingPieces_FillsMissingPieces()
    {
        var vm = new GapWorkflowViewModel();

        vm.Load(new GapWorkflowPayload { MissingPieces = new[] { Piece("Crown"), Piece("Banquo") } });

        Assert.AreEqual(2, vm.MissingPieces.Count);
        Assert.IsTrue(vm.HasMissingPieces);
    }

    [TestMethod]
    public void Load_NoMissingPiece_HasMissingPiecesIsFalse()
    {
        var vm = new GapWorkflowViewModel();
        vm.Load(new GapWorkflowPayload { MissingPieces = new[] { Piece("Crown") } });

        vm.Load(new GapWorkflowPayload());

        Assert.IsFalse(vm.HasMissingPieces);
    }

    [TestMethod]
    public void Load_MissingPieceNoGroup_HidesEmptyMessage()
    {
        var vm = new GapWorkflowViewModel();

        vm.Load(new GapWorkflowPayload { MissingPieces = new[] { Piece("Crown") } });

        Assert.IsFalse(vm.ShowEmptyMessage);
    }

    [TestMethod]
    public void Load_EmptyPayload_ShowsEmptyMessage()
    {
        var vm = new GapWorkflowViewModel();

        vm.Load(new GapWorkflowPayload());

        Assert.IsTrue(vm.ShowEmptyMessage);
    }

    [TestMethod]
    public void Load_MissingPieces_RaisesPropertyChanged()
    {
        var vm = new GapWorkflowViewModel();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => { if (e.PropertyName != null) changed.Add(e.PropertyName); };

        vm.Load(new GapWorkflowPayload { MissingPieces = new[] { Piece("Crown") } });

        CollectionAssert.Contains(changed, nameof(GapWorkflowViewModel.HasMissingPieces));
        CollectionAssert.Contains(changed, nameof(GapWorkflowViewModel.ShowEmptyMessage));
    }

    [TestMethod]
    public async Task OpenMissingPieceCommand_MissingPiece_SendsWorkflowLabel()
    {
        var vm = new GapWorkflowViewModel();
        string? opened = null;
        vm.OnOpenWorkflow = label => { opened = label; return Task.CompletedTask; };

        await vm.OpenMissingPieceCommand.ExecuteAsync(Piece("Crown"));

        Assert.AreEqual("ProblemBeats", opened);
    }

    private static GapMissingPieceLink Piece(string name) => new()
    {
        ElementGuid = Guid.NewGuid(),
        ElementName = name,
        Text = "No Scene is on the beats of this Problem.",
        WorkflowLabel = "ProblemBeats",
        WorkflowTitle = "Problem Beats"
    };
}
