using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Collaborator.Models;
using StoryCADLib.Collaborator.ViewModels;

namespace StoryCADTests.Collaborator.ViewModels;

/// <summary>Collaborator #119: section picker state.</summary>
[TestClass]
public class InterviewPickerViewModelTests
{
    private static WorkflowViewModel WithSections()
    {
        var vm = new WorkflowViewModel();
        vm.SetInterviewSections(new List<InterviewSectionItem>
        {
            new() { Id = "PresentWork", Title = "Present role and work", Blurb = "b" },
            new() { Id = "Origin", Title = "Origin and family", Blurb = "b" },
            new() { Id = "LowPoint", Title = "The low point", Blurb = "b" }
        });
        return vm;
    }

    [TestMethod]
    public void SettingSections_ShowsThePickerAndHidesSummarize()
    {
        var vm = WithSections();

        Assert.AreEqual(3, vm.InterviewSections.Count);
        Assert.IsTrue(vm.IsInterviewPickerVisible);
        Assert.IsFalse(vm.CanSummarize);
    }

    [TestMethod]
    public void PickerAndPropertyUpdates_NeverShareThePanel()
    {
        // They occupy the same three grid rows. Overlaying them drew both headers on
        // top of each other, because every candidate theme background is translucent.
        var vm = WithSections();

        Assert.AreEqual(Microsoft.UI.Xaml.Visibility.Visible, vm.InterviewPickerVisibility);
        Assert.AreEqual(Microsoft.UI.Xaml.Visibility.Collapsed, vm.PropertyUpdatesVisibility);

        vm.MarkInterviewStarted();

        Assert.AreEqual(Microsoft.UI.Xaml.Visibility.Collapsed, vm.InterviewPickerVisibility);
        Assert.AreEqual(Microsoft.UI.Xaml.Visibility.Visible, vm.PropertyUpdatesVisibility);
    }

    [TestMethod]
    public void PropertyUpdatesShow_ForEveryNonInterviewWorkflow()
    {
        // No sections set means an ordinary workflow; the panel must be its usual self.
        var vm = new WorkflowViewModel();

        Assert.AreEqual(Microsoft.UI.Xaml.Visibility.Visible, vm.PropertyUpdatesVisibility);
        Assert.AreEqual(Microsoft.UI.Xaml.Visibility.Collapsed, vm.InterviewPickerVisibility);
    }

    [TestMethod]
    public void NothingTicked_MeansNothingToStart()
    {
        var vm = WithSections();

        Assert.IsFalse(vm.HasChosenSections);
    }

    [TestMethod]
    public void TickingASection_EnablesStart()
    {
        var vm = WithSections();

        vm.InterviewSections[1].IsSelected = true;

        Assert.IsTrue(vm.HasChosenSections);
    }

    [TestMethod]
    public async Task Start_PassesTheTickedIdsInCatalogOrder()
    {
        var vm = WithSections();
        vm.InterviewSections[2].IsSelected = true;
        vm.InterviewSections[0].IsSelected = true;

        IReadOnlyList<string> received = null;
        vm.OnStartInterview = ids => { received = ids; return Task.CompletedTask; };

        await vm.StartInterviewAsync();

        CollectionAssert.AreEqual(new[] { "PresentWork", "LowPoint" }, received.ToArray());
    }

    [TestMethod]
    public async Task Start_HidesThePicker_ButSummarizeWaitsForAReply()
    {
        // Summarize over an empty transcript can only print a refusal, so leaving the
        // picker does not unlock it. Collaborator sets CanSummarize when a reply lands.
        var vm = WithSections();
        vm.InterviewSections[0].IsSelected = true;
        vm.OnStartInterview = _ => Task.CompletedTask;

        await vm.StartInterviewAsync();
        vm.MarkInterviewStarted();

        Assert.IsFalse(vm.IsInterviewPickerVisible);
        Assert.IsFalse(vm.CanSummarize);

        vm.CanSummarize = true;
        Assert.IsTrue(vm.CanSummarize);
    }

    [TestMethod]
    public async Task Skip_WorksWithNothingTicked()
    {
        // Skipping the script must not require choosing part of the script first.
        var vm = WithSections();
        var called = false;
        vm.OnSkipToQuestions = () => { called = true; return Task.CompletedTask; };

        await vm.SkipToQuestionsAsync();

        Assert.IsTrue(called);
        Assert.IsFalse(vm.HasChosenSections);
    }

    [TestMethod]
    public async Task Skip_LeavesNoQueuedQuestions()
    {
        var vm = WithSections();
        vm.OnSkipToQuestions = () => Task.CompletedTask;

        await vm.SkipToQuestionsAsync();
        vm.MarkInterviewStarted();
        vm.SetUpcomingSection(string.Empty, 0);

        Assert.IsFalse(vm.IsInterviewPickerVisible);
        Assert.IsFalse(vm.HasMoreSections);
        Assert.IsFalse(vm.CanSummarize);
    }

    [TestMethod]
    public async Task Start_WithNothingTicked_DoesNothing()
    {
        var vm = WithSections();
        var called = false;
        vm.OnStartInterview = _ => { called = true; return Task.CompletedTask; };

        await vm.StartInterviewAsync();

        Assert.IsFalse(called);
    }

    [TestMethod]
    public void RemainingCount_DrivesTheNextControl()
    {
        var vm = WithSections();

        vm.SetUpcomingSection("The low point", 2);
        Assert.IsTrue(vm.HasMoreSections);

        vm.SetUpcomingSection(string.Empty, 0);
        Assert.IsFalse(vm.HasMoreSections);
    }

    [TestMethod]
    public void NextLabel_NamesTheQuestionComingUp()
    {
        var vm = WithSections();

        vm.SetUpcomingSection("The low point", 2);
        Assert.AreEqual("Next question - The low point", vm.NextSectionLabel);
    }

    [TestMethod]
    public void NextLabel_FallsBackWhenNothingIsQueued()
    {
        var vm = WithSections();

        vm.SetUpcomingSection(string.Empty, 0);
        Assert.AreEqual("Next question", vm.NextSectionLabel);
    }

    [TestMethod]
    public async Task Next_IsIgnoredWhenTheQueueIsEmpty()
    {
        var vm = WithSections();
        vm.SetUpcomingSection(string.Empty, 0);
        var called = false;
        vm.OnNextSection = () => { called = true; return Task.CompletedTask; };

        await vm.NextSectionAsync();

        Assert.IsFalse(called);
    }

    [TestMethod]
    public async Task Next_AdvancesWhenSectionsRemain()
    {
        var vm = WithSections();
        vm.SetUpcomingSection("Origin and family", 1);
        var called = false;
        vm.OnNextSection = () => { called = true; return Task.CompletedTask; };

        await vm.NextSectionAsync();

        Assert.IsTrue(called);
    }

    [TestMethod]
    public void InterviewControls_AreAbsentForEveryOtherWorkflow()
    {
        // Disabled is not absent: a dead Next/Summarize pair under Accept all would also
        // shorten the Property Updates list on all 23 non-interview pages.
        var vm = new WorkflowViewModel();

        Assert.IsFalse(vm.IsInterviewSession);
        Assert.AreEqual(Microsoft.UI.Xaml.Visibility.Collapsed, vm.InterviewControlsVisibility);
    }

    [TestMethod]
    public void InterviewControls_StayVisibleForTheWholeSession()
    {
        var vm = WithSections();

        Assert.AreEqual(Microsoft.UI.Xaml.Visibility.Visible, vm.InterviewControlsVisibility);

        vm.MarkInterviewStarted();

        Assert.AreEqual(Microsoft.UI.Xaml.Visibility.Visible, vm.InterviewControlsVisibility);
    }

    [TestMethod]
    public async Task Next_IsBlockedWhileATurnIsRunning()
    {
        // The commands are RelayCommands over async void handlers, so nothing self-disables
        // during a slow call. Two presses would dequeue two sections and overlap two posts.
        var vm = WithSections();
        vm.SetUpcomingSection("Origin and family", 2);
        var calls = 0;
        vm.OnNextSection = () => { calls++; return Task.CompletedTask; };

        vm.IsInterviewTurnRunning = true;
        Assert.IsFalse(vm.CanAskNextSection);
        await vm.NextSectionAsync();

        vm.IsInterviewTurnRunning = false;
        Assert.IsTrue(vm.CanAskNextSection);
        await vm.NextSectionAsync();

        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public async Task Start_IsBlockedWhileATurnIsRunning()
    {
        var vm = WithSections();
        vm.InterviewSections[0].IsSelected = true;
        var calls = 0;
        vm.OnStartInterview = _ => { calls++; return Task.CompletedTask; };

        vm.IsInterviewTurnRunning = true;
        await vm.StartInterviewAsync();

        Assert.AreEqual(0, calls);
    }
}
