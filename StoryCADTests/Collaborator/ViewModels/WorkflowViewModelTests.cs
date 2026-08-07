using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.Input;
using StoryCADLib.Collaborator.Models;
using StoryCADLib.Collaborator.ViewModels;

#nullable disable

namespace StoryCADTests.Collaborator.ViewModels;

/// <summary>
/// Unit tests for WorkflowViewModel
/// </summary>
[TestClass]
public class WorkflowViewModelTests
{
    private WorkflowViewModel _viewModel;

    [TestInitialize]
    public void Setup()
    {
        _viewModel = new WorkflowViewModel();
        // #145: production leaves Send locked until proposals seed; unit tests of send
        // path enable chat explicitly.
        _viewModel.IsChatEnabled = true;
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WhenCalled_InitializesConversationList()
    {
        // Assert
        Assert.IsNotNull(_viewModel.ConversationList);
        Assert.AreEqual(0, _viewModel.ConversationList.Count);
    }

    [TestMethod]
    public void Constructor_WhenCalled_InitializesAcceptCommand()
    {
        // Assert
        Assert.IsNotNull(_viewModel.AcceptCommand);
        Assert.IsInstanceOfType(_viewModel.AcceptCommand, typeof(RelayCommand));
    }

    [TestMethod]
    public void Constructor_WhenCalled_InitializesSendCommand()
    {
        // Assert
        Assert.IsNotNull(_viewModel.SendCommand);
        Assert.IsInstanceOfType(_viewModel.SendCommand, typeof(RelayCommand));
    }

    [TestMethod]
    public void Constructor_WhenCalled_SetsAcceptVisibilityToVisible()
    {
        // Assert
        Assert.AreEqual(Visibility.Visible, _viewModel.AcceptVisibility);
    }

    [TestMethod]
    public void Constructor_WhenCalled_SetsProgressVisibilityToCollapsed()
    {
        // Assert
        Assert.AreEqual(Visibility.Collapsed, _viewModel.ProgressVisibility);
    }

    #endregion

    #region Property Tests

    [TestMethod]
    public void InputText_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(WorkflowViewModel.InputText))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.InputText = "Test input";

        // Assert
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual("Test input", _viewModel.InputText);
    }

    [TestMethod]
    public void PromptOutput_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(WorkflowViewModel.PromptOutput))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.PromptOutput = "Test output";

        // Assert
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual("Test output", _viewModel.PromptOutput);
    }

    [TestMethod]
    public void SelectedElementsSummary_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(WorkflowViewModel.SelectedElementsSummary))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.SelectedElementsSummary = "Problem: Main conflict";

        // Assert
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual("Problem: Main conflict", _viewModel.SelectedElementsSummary);
    }

    [TestMethod]
    public void AcceptVisibility_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(WorkflowViewModel.AcceptVisibility))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.AcceptVisibility = Visibility.Collapsed;

        // Assert
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual(Visibility.Collapsed, _viewModel.AcceptVisibility);
    }

    [TestMethod]
    public void ProgressVisibility_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(WorkflowViewModel.ProgressVisibility))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.ProgressVisibility = Visibility.Visible;

        // Assert
        Assert.IsTrue(propertyChangedRaised);
        Assert.AreEqual(Visibility.Visible, _viewModel.ProgressVisibility);
    }

    [TestMethod]
    public void Title_WhenSet_ReturnsValue()
    {
        // Act
        _viewModel.Title = "Test Workflow";

        // Assert
        Assert.AreEqual("Test Workflow", _viewModel.Title);
    }

    [TestMethod]
    public void Description_WhenSet_ReturnsValue()
    {
        // Act
        _viewModel.Description = "Test Description";

        // Assert
        Assert.AreEqual("Test Description", _viewModel.Description);
    }

    [TestMethod]
    public void Explanation_WhenSet_ReturnsValue()
    {
        // Act
        _viewModel.Explanation = "Test Explanation";

        // Assert
        Assert.AreEqual("Test Explanation", _viewModel.Explanation);
    }

    #endregion

    #region InitializeAsync Tests

    [TestMethod]
    public async Task InitializeAsync_WithNullWorkflow_ReturnsEarly()
    {
        // Act
        await _viewModel.InitializeAsync(null);

        // Assert - Title should remain null
        Assert.IsNull(_viewModel.Title);
    }

    [TestMethod]
    public async Task InitializeAsync_WithWorkflow_SetsTitleFromToString()
    {
        // Arrange
        var workflow = new TestWorkflow { Name = "Premise" };

        // Act
        await _viewModel.InitializeAsync(workflow);

        // Assert
        Assert.AreEqual("Premise", _viewModel.Title);
    }

    [TestMethod]
    public async Task InitializeAsync_WithWorkflow_SetsDescriptionToEmpty()
    {
        // Arrange
        var workflow = new TestWorkflow { Name = "GMC" };

        // Act
        await _viewModel.InitializeAsync(workflow);

        // Assert
        Assert.AreEqual(string.Empty, _viewModel.Description);
    }

    [TestMethod]
    public async Task InitializeAsync_WithWorkflow_SetsExplanationToEmpty()
    {
        // Arrange
        var workflow = new TestWorkflow { Name = "Test" };

        // Act
        await _viewModel.InitializeAsync(workflow);

        // Assert
        Assert.AreEqual(string.Empty, _viewModel.Explanation);
    }

    #endregion

    #region SendButtonClicked Tests

    [TestMethod]
    public async Task SendButtonClicked_WhenChatDisabled_DoesNotInvokeCallback()
    {
        _viewModel.IsChatEnabled = false;
        _viewModel.InputText = "Hello";
        var invoked = false;
        _viewModel.OnSendMessage = _ =>
        {
            invoked = true;
            return Task.FromResult("x");
        };

        await _viewModel.SendButtonClicked();

        Assert.IsFalse(invoked);
        Assert.AreEqual(1, _viewModel.ConversationList.Count);
        Assert.IsFalse(_viewModel.ConversationList[0].IsUser);
        StringAssert.Contains(_viewModel.ConversationList[0].Text, "unlocks");
    }

    [TestMethod]
    public async Task SendButtonClicked_WithEmptyInput_DoesNotAddToConversation()
    {
        // Arrange
        _viewModel.InputText = "";

        // Act
        await _viewModel.SendButtonClicked();

        // Assert
        Assert.AreEqual(0, _viewModel.ConversationList.Count);
    }

    [TestMethod]
    public async Task SendButtonClicked_WithWhitespaceInput_DoesNotAddToConversation()
    {
        // Arrange
        _viewModel.InputText = "   ";

        // Act
        await _viewModel.SendButtonClicked();

        // Assert
        Assert.AreEqual(0, _viewModel.ConversationList.Count);
    }

    [TestMethod]
    public async Task SendButtonClicked_WithValidInput_ClearsInputText()
    {
        // Arrange
        _viewModel.InputText = "Hello";

        // Act
        await _viewModel.SendButtonClicked();

        // Assert
        Assert.AreEqual(string.Empty, _viewModel.InputText);
    }

    [TestMethod]
    public async Task SendButtonClicked_WithValidInput_AddsUserMessageToConversation()
    {
        // Arrange
        _viewModel.InputText = "Hello";

        // Act
        await _viewModel.SendButtonClicked();

        // Assert
        Assert.IsTrue(_viewModel.ConversationList[0].IsUser);
    }

    [TestMethod]
    public async Task SendButtonClicked_WithoutCallback_AddsNotConnectedMessage()
    {
        // Arrange
        _viewModel.InputText = "Hello";
        _viewModel.OnSendMessage = null;

        // Act
        await _viewModel.SendButtonClicked();

        // Assert
        Assert.AreEqual(2, _viewModel.ConversationList.Count);
        Assert.IsTrue(_viewModel.ConversationList[1].Text.Contains("not connected"));
    }

    [TestMethod]
    public async Task SendButtonClicked_WithCallback_InvokesCallback()
    {
        // Arrange
        var callbackInvoked = false;
        _viewModel.InputText = "Hello";
        _viewModel.OnSendMessage = async (msg) =>
        {
            callbackInvoked = true;
            return "Response";
        };

        // Act
        await _viewModel.SendButtonClicked();

        // Assert
        Assert.IsTrue(callbackInvoked);
    }

    [TestMethod]
    public async Task SendButtonClicked_WithCallback_AddsResponseToConversation()
    {
        // Arrange
        _viewModel.InputText = "Hello";
        _viewModel.OnSendMessage = async (msg) => "Test response";

        // Act
        await _viewModel.SendButtonClicked();

        // Assert
        Assert.AreEqual(2, _viewModel.ConversationList.Count);
        Assert.IsTrue(_viewModel.ConversationList[1].Text.Contains("Test response"));
    }

    [TestMethod]
    public async Task SendButtonClicked_WhenCallbackThrows_AddsErrorMessage()
    {
        // Arrange
        _viewModel.InputText = "Hello";
        _viewModel.OnSendMessage = async (msg) => throw new Exception("Test error");

        // Act
        await _viewModel.SendButtonClicked();

        // Assert
        Assert.IsTrue(_viewModel.ConversationList[1].Text.StartsWith("Error:"));
    }

    [TestMethod]
    public async Task SendButtonClicked_AfterCompletion_SetsProgressVisibilityToCollapsed()
    {
        // Arrange
        _viewModel.InputText = "Hello";

        // Act
        await _viewModel.SendButtonClicked();

        // Assert
        Assert.AreEqual(Visibility.Collapsed, _viewModel.ProgressVisibility);
    }

    #endregion

    #region Command Tests

    [TestMethod]
    public void AcceptCommand_CanExecute_ReturnsTrue()
    {
        // Assert
        Assert.IsTrue(_viewModel.AcceptCommand.CanExecute(null));
    }

    [TestMethod]
    public void AcceptCommand_WhenExecuted_DoesNotThrow()
    {
        // Act & Assert - should not throw
        _viewModel.AcceptCommand.Execute(null);
    }

    [TestMethod]
    public void SendCommand_CanExecute_ReturnsTrue()
    {
        // Assert
        Assert.IsTrue(_viewModel.SendCommand.CanExecute(null));
    }

    #endregion

    #region Review Each (issue #115 / #116)

    private static PendingUpdateItem Item(string key, string proposed = "v", bool isProtected = false) =>
        new()
        {
            Key = key,
            ProposedDisplay = proposed,
            CurrentDisplay = isProtected ? "existing" : "",
            KindLabel = isProtected ? "Has your text" : "New",
            IsProtected = isProtected,
            SummaryLine = isProtected ? "Has your text" : "New"
        };

    /// <summary>
    /// Simulates Collaborator: accept/skip removes the key then SetPendingUpdates (#116 item list).
    /// </summary>
    private void WireRemoveOnAcceptOrSkip(List<PendingUpdateItem> store)
    {
        _viewModel.OnAcceptProperty = key =>
        {
            store.RemoveAll(i => i.Key == key);
            _viewModel.SetPendingUpdates(store);
            return Task.CompletedTask;
        };
        _viewModel.OnSkipProperty = key =>
        {
            store.RemoveAll(i => i.Key == key);
            _viewModel.SetPendingUpdates(store);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// #145: skip/accept keeps the row and marks KindLabel settled (session proposal set).
    /// </summary>
    private void WireMarkSettledOnAcceptOrSkip(List<PendingUpdateItem> store)
    {
        void Mark(string key, string kind)
        {
            var item = store.Find(i => i.Key == key);
            if (item == null) return;
            item.KindLabel = kind;
            item.SummaryLine = kind;
            item.IsProtected = false;
            _viewModel.SetPendingUpdates(store);
        }

        _viewModel.OnAcceptProperty = key =>
        {
            Mark(key, "Accepted");
            return Task.CompletedTask;
        };
        _viewModel.OnSkipProperty = key =>
        {
            Mark(key, "Skipped");
            return Task.CompletedTask;
        };
    }

    [TestMethod]
    public void PendingUpdatesHeader_ReflectsFreeAndProtectedCounts()
    {
        _viewModel.SetPendingUpdates(new List<PendingUpdateItem>
        {
            Item("A", isProtected: false),
            Item("B", isProtected: true),
            Item("C", isProtected: true)
        });

        Assert.AreEqual("Property Updates (3: 1 free, 2 need review)", _viewModel.PendingUpdatesHeader);

        _viewModel.ClearPendingUpdates();
        Assert.AreEqual("Property Updates", _viewModel.PendingUpdatesHeader);
    }

    [TestMethod]
    public void RefreshTopicalExplanation_IncludesSelectedAndPendingCounts()
    {
        _viewModel.SelectedElementsSummary = "Overview: Test Story";
        _viewModel.SetPendingUpdates(new List<PendingUpdateItem>
        {
            Item("A", isProtected: false),
            Item("B", isProtected: true)
        });

        StringAssert.Contains(_viewModel.Explanation, "Selected: Overview: Test Story");
        StringAssert.Contains(_viewModel.Explanation, "2 property update(s)");
        StringAssert.Contains(_viewModel.Explanation, "1 free");
        StringAssert.Contains(_viewModel.Explanation, "1 need review");
    }

    [TestMethod]
    public void ReviewEach_AcceptCurrent_DoesNotSkipMiddleProperty()
    {
        // Arrange — three updates (Ideation: Description, Concept, Premise)
        var store = new List<PendingUpdateItem>
        {
            Item("Overview.Description", "idea"),
            Item("Overview.Concept", "concept"),
            Item("Overview.Premise", "premise")
        };
        _viewModel.SetPendingUpdates(store);
        WireRemoveOnAcceptOrSkip(store);
        _viewModel.ReviewEachCommand.Execute(null);

        Assert.AreEqual("Overview.Description", _viewModel.CurrentReviewKey);

        // Act — accept first, then whatever is shown next
        _viewModel.AcceptCurrentCommand.Execute(null);
        var secondKey = _viewModel.CurrentReviewKey;
        _viewModel.AcceptCurrentCommand.Execute(null);
        var thirdKey = _viewModel.CurrentReviewKey;

        // Assert — middle must be offered; nothing silently dropped
        Assert.AreEqual("Overview.Concept", secondKey, "After first accept, next should be Concept (not Premise).");
        Assert.AreEqual("Overview.Premise", thirdKey, "After second accept, next should be Premise.");
        Assert.IsTrue(_viewModel.IsInReviewMode);
        Assert.AreEqual(1, store.Count);

        _viewModel.AcceptCurrentCommand.Execute(null);
        Assert.IsFalse(_viewModel.IsInReviewMode);
        Assert.AreEqual(0, store.Count, "All three accepts must leave nothing discarded.");
    }

    /// <summary>
    /// Protect Accept must mark KindLabel Accepted (session set). Leaving
    /// "Has your text" makes AdvancePastSettledRows stick on the same row.
    /// </summary>
    [TestMethod]
    public void ReviewEach_AcceptProtect_AdvancesWhenMarkedAccepted()
    {
        var store = new List<PendingUpdateItem>
        {
            Item("Problem.Name", "old name", isProtected: true),
            Item("Problem.AntagConflict", "old conflict", isProtected: true),
            Item("Problem.Theme", "old theme", isProtected: true),
        };
        _viewModel.SetPendingUpdates(store);
        WireMarkSettledOnAcceptOrSkip(store);
        _viewModel.ReviewEachCommand.Execute(null);

        Assert.AreEqual("Problem.Name", _viewModel.CurrentReviewKey);

        _viewModel.SkipCurrentCommand.Execute(null);
        Assert.AreEqual("Problem.AntagConflict", _viewModel.CurrentReviewKey);

        _viewModel.AcceptCurrentCommand.Execute(null);
        Assert.AreEqual("Problem.Theme", _viewModel.CurrentReviewKey,
            "After Accept on Protect AntagConflict, Review Each must advance (not stick).");
        Assert.IsTrue(_viewModel.IsInReviewMode);
    }

    /// <summary>
    /// Documents the stuck-bug shape: Accept that does not settle leaves the index frozen.
    /// </summary>
    [TestMethod]
    public void ReviewEach_AcceptProtect_WithoutMarkingSettled_StaysOnRow()
    {
        var store = new List<PendingUpdateItem>
        {
            Item("Problem.AntagConflict", "old", isProtected: true),
            Item("Problem.Theme", "t", isProtected: true),
        };
        _viewModel.SetPendingUpdates(store);
        // Bug shape: stage-only Accept (no KindLabel change)
        _viewModel.OnAcceptProperty = _ => Task.CompletedTask;
        _viewModel.ReviewEachCommand.Execute(null);

        Assert.AreEqual("Problem.AntagConflict", _viewModel.CurrentReviewKey);
        _viewModel.AcceptCurrentCommand.Execute(null);
        Assert.AreEqual("Problem.AntagConflict", _viewModel.CurrentReviewKey,
            "Without settling, Review Each must stay (regression detector for production Protect path).");
    }

    [TestMethod]
    public void ReviewEach_SkipCurrent_DoesNotDropRemaining()
    {
        var store = new List<PendingUpdateItem>
        {
            Item("Overview.Description", "idea"),
            Item("Overview.Concept", "concept"),
            Item("Overview.Premise", "premise")
        };
        _viewModel.SetPendingUpdates(store);
        WireRemoveOnAcceptOrSkip(store);
        _viewModel.ReviewEachCommand.Execute(null);

        _viewModel.SkipCurrentCommand.Execute(null); // skip Description
        Assert.AreEqual("Overview.Concept", _viewModel.CurrentReviewKey);
        Assert.AreEqual(2, store.Count);

        _viewModel.AcceptCurrentCommand.Execute(null); // accept Concept
        Assert.AreEqual("Overview.Premise", _viewModel.CurrentReviewKey);
        Assert.AreEqual(1, store.Count);

        _viewModel.AcceptCurrentCommand.Execute(null);
        Assert.AreEqual(0, store.Count);
        Assert.IsFalse(_viewModel.IsInReviewMode);
    }

    [TestMethod]
    public void ReviewEach_SkipWhenRowsStayOnList_AdvancesPastSkipped()
    {
        // Regression #145: session set keeps Skipped rows; Review must not stick forever.
        var store = new List<PendingUpdateItem>
        {
            Item("Antagonist.Name", "Ewan"),
            Item("Problem.Premise", "p"),
            Item("Problem.Theme", "t")
        };
        _viewModel.SetPendingUpdates(store);
        WireMarkSettledOnAcceptOrSkip(store);
        _viewModel.ReviewEachCommand.Execute(null);

        Assert.AreEqual("Antagonist.Name", _viewModel.CurrentReviewKey);

        _viewModel.SkipCurrentCommand.Execute(null);
        Assert.AreEqual("Problem.Premise", _viewModel.CurrentReviewKey,
            "After skip, review must move to next open row (not stay on Skipped Name).");
        Assert.AreEqual(3, store.Count, "All rows remain on the session list");
        Assert.AreEqual("Skipped", store.Find(i => i.Key == "Antagonist.Name")!.KindLabel);

        _viewModel.SkipCurrentCommand.Execute(null);
        Assert.AreEqual("Problem.Theme", _viewModel.CurrentReviewKey);

        _viewModel.SkipCurrentCommand.Execute(null);
        Assert.IsFalse(_viewModel.IsInReviewMode, "Review ends when no open rows remain");
        Assert.AreEqual(3, _viewModel.PendingUpdateItems.Count, "Settled rows stay visible");
    }

    [TestMethod]
    public void ReviewEach_AcceptLastOfTwo_DoesNotClearUnreviewed()
    {
        // Secondary bug: accept first of two remaining while index logic treated "done" early
        var store = new List<PendingUpdateItem>
        {
            Item("A", "1"),
            Item("B", "2")
        };
        _viewModel.SetPendingUpdates(store);
        WireRemoveOnAcceptOrSkip(store);
        _viewModel.ReviewEachCommand.Execute(null);

        _viewModel.AcceptCurrentCommand.Execute(null);
        Assert.AreEqual("B", _viewModel.CurrentReviewKey);
        Assert.AreEqual(1, store.Count);
        Assert.IsTrue(store.Exists(i => i.Key == "B"));

        _viewModel.AcceptCurrentCommand.Execute(null);
        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void CurrentReviewDisplayName_WithDisplayFields_FormatsFriendlyName()
    {
        var item = Item("Problem.StructureTitle", "v");
        item.ElementName = "Problem";
        item.PropertyDisplayName = "Structure Title";
        _viewModel.SetPendingUpdates(new List<PendingUpdateItem> { item });
        _viewModel.ReviewEachCommand.Execute(null);

        Assert.AreEqual("Structure Title (Problem)", _viewModel.CurrentReviewDisplayName);
        Assert.AreEqual("Problem.StructureTitle", _viewModel.CurrentReviewKey);
    }

    [TestMethod]
    public void CurrentReviewDisplayName_WithoutDisplayFields_FallsBackToKey()
    {
        _viewModel.SetPendingUpdates(new List<PendingUpdateItem> { Item("Problem.StructureTitle", "v") });
        _viewModel.ReviewEachCommand.Execute(null);

        Assert.AreEqual("Problem.StructureTitle", _viewModel.CurrentReviewDisplayName);
    }

    [TestMethod]
    public void AcceptItem_WithPendingUpdate_InvokesCallbackAndRemovesRow()
    {
        var store = new List<PendingUpdateItem> { Item("Overview.Premise", "v"), Item("Overview.Concept", "v") };
        _viewModel.SetPendingUpdates(store);
        WireRemoveOnAcceptOrSkip(store);

        _viewModel.AcceptItem("Overview.Concept");

        Assert.AreEqual(1, store.Count);
        Assert.AreEqual("Overview.Premise", store[0].Key);
        Assert.AreEqual(1, _viewModel.PendingUpdateItems.Count);
    }

    [TestMethod]
    public void SkipItem_DuringReviewMode_ClampsIndexAndExitsWhenEmpty()
    {
        var store = new List<PendingUpdateItem> { Item("A", "1"), Item("B", "2") };
        _viewModel.SetPendingUpdates(store);
        WireRemoveOnAcceptOrSkip(store);
        _viewModel.ReviewEachCommand.Execute(null);

        _viewModel.SkipItem("B");
        Assert.IsTrue(_viewModel.IsInReviewMode);
        Assert.AreEqual("A", _viewModel.CurrentReviewKey);

        _viewModel.SkipItem("A");
        Assert.IsFalse(_viewModel.IsInReviewMode, "Review mode must end when inline ticks empty the list.");
    }

    [TestMethod]
    public void AcceptItem_WithNoPendingUpdates_DoesNotInvokeCallback()
    {
        var invoked = false;
        _viewModel.OnAcceptProperty = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        _viewModel.AcceptItem("Overview.Premise");

        Assert.IsFalse(invoked);
    }

    [TestMethod]
    public void AddStatusMessage_ConsecutiveLines_RollUpIntoOneGroup()
    {
        _viewModel.AddStatusMessage("Starting workflow: Problem Structure");
        _viewModel.AddStatusMessage("Built request body from 1 elements");
        _viewModel.AddStatusMessage("Received AI response");

        Assert.AreEqual(1, _viewModel.ConversationList.Count);
        var group = _viewModel.ConversationList[0];
        Assert.IsTrue(group.IsStatusGroup);
        Assert.AreEqual(3, group.Steps.Count);
        StringAssert.Contains(group.StatusHeader, "3 steps");
        StringAssert.Contains(group.StatusHeader, "Received AI response");
    }

    [TestMethod]
    public void AddStatusMessage_AfterBubble_StartsNewGroup()
    {
        _viewModel.AddStatusMessage("Running Problem Structure...");
        _viewModel.ConversationList.Add(ChatMessage.FromCollaborator("Here's my explanation."));
        _viewModel.AddStatusMessage("Applied Problem.StructureTitle");

        Assert.AreEqual(3, _viewModel.ConversationList.Count);
        Assert.IsTrue(_viewModel.ConversationList[0].IsStatusGroup);
        Assert.IsFalse(_viewModel.ConversationList[1].IsStatusGroup);
        Assert.IsTrue(_viewModel.ConversationList[2].IsStatusGroup);
    }

    [TestMethod]
    public void AddStatusMessage_StripsDashHeaders()
    {
        _viewModel.AddStatusMessage("--- Gathering input elements ---");

        Assert.AreEqual("Gathering input elements", _viewModel.ConversationList[0].Steps[0]);
    }

    [TestMethod]
    public void ConversationList_SenderLabel_ShownOncePerRun()
    {
        _viewModel.ConversationList.Add(ChatMessage.FromCollaborator("first"));
        _viewModel.ConversationList.Add(ChatMessage.FromCollaborator("second"));
        _viewModel.ConversationList.Add(ChatMessage.FromUser("mine"));
        _viewModel.ConversationList.Add(ChatMessage.FromCollaborator("reply"));

        Assert.IsTrue(_viewModel.ConversationList[0].ShowSender);
        Assert.IsFalse(_viewModel.ConversationList[1].ShowSender, "Second Collaborator bubble in a run repeats no label.");
        Assert.IsTrue(_viewModel.ConversationList[2].ShowSender);
        Assert.IsTrue(_viewModel.ConversationList[3].ShowSender);
    }

    [TestMethod]
    public void ConversationList_StatusGroup_NeverShowsSender()
    {
        _viewModel.AddStatusMessage("Running...");

        Assert.IsFalse(_viewModel.ConversationList[0].ShowSender);
    }

    [TestMethod]
    public void PendingUpdateItem_DisplayName_FallsBackToKey()
    {
        var bare = Item("Overview.Premise", "v");
        Assert.AreEqual("Overview.Premise", bare.DisplayName);
        Assert.AreEqual(bare.SummaryLine, bare.ElementAndKind);

        bare.ElementName = "Overview";
        bare.PropertyDisplayName = "Premise";
        Assert.AreEqual("Premise", bare.DisplayName);
        Assert.AreEqual("Overview · New", bare.ElementAndKind);
    }

    #endregion

    #region ObservableRecipient Tests

    [TestMethod]
    public void WorkflowViewModel_InheritsFrom_ObservableRecipient()
    {
        // Assert
        Assert.IsInstanceOfType(_viewModel, typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableRecipient));
    }

    [TestMethod]
    public void WorkflowViewModel_CanBeActivated()
    {
        // Act
        _viewModel.IsActive = true;

        // Assert
        Assert.IsTrue(_viewModel.IsActive);
    }

    #endregion

    #region Helper Classes

    private class TestWorkflow
    {
        public string Name { get; set; }
        public override string ToString() => Name;
    }

    #endregion
}
