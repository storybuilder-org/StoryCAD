using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Collaborator.ViewModels;

namespace StoryCADTests.Collaborator.ViewModels;

/// <summary>
/// Collaborator #119: the interview page state.
///
/// Replaces the section-picker tests. Terry removed the picker along with the life-history
/// script: the interview opens on its first question, and the only control it owns is Save.
/// </summary>
[TestClass]
public class InterviewSessionViewModelTests
{
    [TestMethod]
    public void InterviewControls_AreAbsentOutsideAnInterview()
    {
        // Collapsed, not disabled. A dead button under Accept all on every other workflow
        // page shortens the Property Updates list above it to make room.
        var viewModel = new WorkflowViewModel();

        Assert.AreEqual(Visibility.Collapsed, viewModel.InterviewControlsVisibility);
    }

    [TestMethod]
    public void BeginInterviewSession_ShowsControlsWithNothingToSaveYet()
    {
        var viewModel = new WorkflowViewModel();

        viewModel.BeginInterviewSession();

        Assert.AreEqual(Visibility.Visible, viewModel.InterviewControlsVisibility);
        Assert.IsFalse(viewModel.CanSaveInterview);
        Assert.IsFalse(viewModel.CanPressSave);
    }

    [TestMethod]
    public void Save_UnlocksOnTheFirstAnswer_NotOnlyAtTheEnd()
    {
        // It rescues a session the writer abandons partway.
        var viewModel = new WorkflowViewModel();
        viewModel.BeginInterviewSession();

        viewModel.CanSaveInterview = true;

        Assert.IsTrue(viewModel.CanPressSave);
    }

    [TestMethod]
    public void Save_IsBlockedWhileATurnIsInFlight()
    {
        // A save landing mid-turn writes a transcript missing the answer being recorded.
        var viewModel = new WorkflowViewModel();
        viewModel.BeginInterviewSession();
        viewModel.CanSaveInterview = true;

        viewModel.IsInterviewTurnRunning = true;

        Assert.IsFalse(viewModel.CanPressSave);
    }

    [TestMethod]
    public async Task SaveInterviewCommand_DoesNothingOverAnEmptyTranscript()
    {
        var viewModel = new WorkflowViewModel();
        viewModel.BeginInterviewSession();
        var called = false;
        viewModel.OnSaveInterview = () => { called = true; return Task.CompletedTask; };

        viewModel.SaveInterviewCommand.Execute(null);
        await Task.Yield();

        Assert.IsFalse(called, "Save ran with nothing recorded; it can only print a refusal.");
    }

    [TestMethod]
    public void CanPressSave_RaisesChangeWhenEitherInputMoves()
    {
        // The command is a plain RelayCommand over an async void handler, so the button
        // only greys correctly if both inputs notify.
        var viewModel = new WorkflowViewModel();
        var raised = 0;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkflowViewModel.CanPressSave)) raised++;
        };

        viewModel.CanSaveInterview = true;
        viewModel.IsInterviewTurnRunning = true;

        Assert.AreEqual(2, raised);
    }
}
