using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.Input;
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
        Assert.IsTrue(_viewModel.ConversationList[0].StartsWith("User:"));
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
        Assert.IsTrue(_viewModel.ConversationList[1].Contains("not connected"));
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
        Assert.IsTrue(_viewModel.ConversationList[1].Contains("Test response"));
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
        Assert.IsTrue(_viewModel.ConversationList[1].StartsWith("Error:"));
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
