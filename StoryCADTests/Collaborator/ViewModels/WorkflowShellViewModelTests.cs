using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using StoryCADLib.Collaborator.ViewModels;

#nullable disable

namespace StoryCADTests.Collaborator.ViewModels;

/// <summary>
/// Unit tests for WorkflowShellViewModel
/// </summary>
[TestClass]
public class WorkflowShellViewModelTests
{
    private WorkflowShellViewModel _viewModel;

    [TestInitialize]
    public void Setup()
    {
        _viewModel = new WorkflowShellViewModel();
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WhenCalled_InitializesMenuItems()
    {
        // Assert
        Assert.IsNotNull(_viewModel.MenuItems);
        Assert.IsInstanceOfType(_viewModel.MenuItems, typeof(ObservableCollection<NavigationViewItem>));
        Assert.AreEqual(0, _viewModel.MenuItems.Count);
    }

    [TestMethod]
    public void Constructor_WhenCalled_InitializesExitCommand()
    {
        // Assert
        Assert.IsNotNull(_viewModel.ExitCommand);
        Assert.IsInstanceOfType(_viewModel.ExitCommand, typeof(RelayCommand));
    }

    [TestMethod]
    public void Constructor_WhenCalled_SetsTitleToDefault()
    {
        // Assert
        Assert.AreEqual("Story Collaborator", _viewModel.Title);
    }

    #endregion

    #region Property Tests

    [TestMethod]
    public void Title_WhenSet_ReturnsNewValue()
    {
        // Act
        _viewModel.Title = "Custom Title";

        // Assert
        Assert.AreEqual("Custom Title", _viewModel.Title);
    }

    [TestMethod]
    public void ContentFrame_Initially_IsNull()
    {
        // Assert
        Assert.IsNull(_viewModel.ContentFrame);
    }

    [TestMethod]
    public void NavView_Initially_IsNull()
    {
        // Assert
        Assert.IsNull(_viewModel.NavView);
    }

    [TestMethod]
    public void CurrentItem_Initially_IsNull()
    {
        // Assert
        Assert.IsNull(_viewModel.CurrentItem);
    }

    [TestMethod]
    public void OnWorkflowSelected_Initially_IsNull()
    {
        // Assert
        Assert.IsNull(_viewModel.OnWorkflowSelected);
    }

    [TestMethod]
    public void OnWorkflowSelected_WhenSet_ReturnsCallback()
    {
        // Arrange
        Func<object, Task> callback = async (obj) => await Task.CompletedTask;

        // Act
        _viewModel.OnWorkflowSelected = callback;

        // Assert
        Assert.IsNotNull(_viewModel.OnWorkflowSelected);
        Assert.AreEqual(callback, _viewModel.OnWorkflowSelected);
    }

    [TestMethod]
    [Ignore("Requires UI thread for NavigationViewItem")]
    public void CurrentItem_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(WorkflowShellViewModel.CurrentItem))
                propertyChangedRaised = true;
        };

        // Act
        _viewModel.CurrentItem = new NavigationViewItem();

        // Assert
        Assert.IsTrue(propertyChangedRaised);
    }

    #endregion

    #region LoadWorkflowMenuAsync Tests

    [TestMethod]
    [Ignore("Requires UI thread for NavigationViewItem")]
    public async Task LoadWorkflowMenuAsync_WhenCalled_ClearsExistingItems()
    {
        // Arrange
        _viewModel.MenuItems.Add(new NavigationViewItem { Content = "Existing" });
        Assert.AreEqual(1, _viewModel.MenuItems.Count);

        // Act
        await _viewModel.LoadWorkflowMenuAsync();

        // Assert - cleared and repopulated with 1 item
        Assert.AreEqual(1, _viewModel.MenuItems.Count);
    }

    [TestMethod]
    [Ignore("Requires UI thread for NavigationViewItem")]
    public async Task LoadWorkflowMenuAsync_WhenCalled_AddsWorkflowMenuItem()
    {
        // Act
        await _viewModel.LoadWorkflowMenuAsync();

        // Assert
        Assert.AreEqual(1, _viewModel.MenuItems.Count);
        Assert.AreEqual("Workflow", _viewModel.MenuItems[0].Content);
        Assert.AreEqual("Workflow", _viewModel.MenuItems[0].Tag);
    }

    [TestMethod]
    [Ignore("Requires UI thread for NavigationViewItem")]
    public async Task LoadWorkflowMenuAsync_WhenCalled_ReturnsCompletedTask()
    {
        // Act
        var task = _viewModel.LoadWorkflowMenuAsync();
        await task;

        // Assert
        Assert.IsTrue(task.IsCompletedSuccessfully);
    }

    #endregion

    #region ExitCommand Tests

    [TestMethod]
    public void ExitCommand_CanExecute_ReturnsTrue()
    {
        // Assert
        Assert.IsTrue(_viewModel.ExitCommand.CanExecute(null));
    }

    [TestMethod]
    [Ignore("Requires UI thread for NavigationViewItem")]
    public void ExitCommand_WhenExecuted_ClearsMenuItems()
    {
        // Arrange
        _viewModel.MenuItems.Add(new NavigationViewItem { Content = "Test" });
        Assert.AreEqual(1, _viewModel.MenuItems.Count);

        // Act
        _viewModel.ExitCommand.Execute(null);

        // Assert
        Assert.AreEqual(0, _viewModel.MenuItems.Count);
    }

    [TestMethod]
    public void ExitCommand_WithNullNavView_DoesNotThrow()
    {
        // Arrange
        _viewModel.NavView = null;

        // Act & Assert - should not throw
        _viewModel.ExitCommand.Execute(null);
    }

    #endregion

    #region Callback Tests

    [TestMethod]
    public async Task OnWorkflowSelected_WhenInvoked_ReceivesCorrectTag()
    {
        // Arrange
        object receivedTag = null;
        _viewModel.OnWorkflowSelected = async (tag) =>
        {
            receivedTag = tag;
            await Task.CompletedTask;
        };

        // Act
        await _viewModel.OnWorkflowSelected("TestWorkflow");

        // Assert
        Assert.AreEqual("TestWorkflow", receivedTag);
    }

    [TestMethod]
    public async Task OnWorkflowSelected_WithAsyncCallback_CompletesSuccessfully()
    {
        // Arrange
        var completed = false;
        _viewModel.OnWorkflowSelected = async (tag) =>
        {
            await Task.Delay(10);
            completed = true;
        };

        // Act
        await _viewModel.OnWorkflowSelected("Test");

        // Assert
        Assert.IsTrue(completed);
    }

    [TestMethod]
    public async Task OnWorkflowSelected_WhenReplaced_UsesNewCallback()
    {
        // Arrange
        var firstCalled = false;
        var secondCalled = false;

        _viewModel.OnWorkflowSelected = async (tag) =>
        {
            firstCalled = true;
            await Task.CompletedTask;
        };

        _viewModel.OnWorkflowSelected = async (tag) =>
        {
            secondCalled = true;
            await Task.CompletedTask;
        };

        // Act
        await _viewModel.OnWorkflowSelected("Test");

        // Assert
        Assert.IsFalse(firstCalled);
        Assert.IsTrue(secondCalled);
    }

    #endregion

    #region ObservableRecipient Tests

    [TestMethod]
    public void WorkflowShellViewModel_InheritsFrom_ObservableRecipient()
    {
        // Assert
        Assert.IsInstanceOfType(_viewModel, typeof(CommunityToolkit.Mvvm.ComponentModel.ObservableRecipient));
    }

    [TestMethod]
    public void WorkflowShellViewModel_CanBeActivated()
    {
        // Act
        _viewModel.IsActive = true;

        // Assert
        Assert.IsTrue(_viewModel.IsActive);
    }

    [TestMethod]
    public void WorkflowShellViewModel_CanBeDeactivated()
    {
        // Arrange
        _viewModel.IsActive = true;

        // Act
        _viewModel.IsActive = false;

        // Assert
        Assert.IsFalse(_viewModel.IsActive);
    }

    #endregion

    #region MenuItems Collection Tests

    [TestMethod]
    [Ignore("Requires UI thread for NavigationViewItem")]
    public void MenuItems_WhenCleared_HasZeroCount()
    {
        // Arrange
        _viewModel.MenuItems.Add(new NavigationViewItem());
        _viewModel.MenuItems.Add(new NavigationViewItem());

        // Act
        _viewModel.MenuItems.Clear();

        // Assert
        Assert.AreEqual(0, _viewModel.MenuItems.Count);
    }

    [TestMethod]
    [Ignore("Requires UI thread for NavigationViewItem")]
    public void MenuItems_CanBeReplaced()
    {
        // Arrange
        var newCollection = new ObservableCollection<NavigationViewItem>
        {
            new NavigationViewItem { Content = "Item 1" },
            new NavigationViewItem { Content = "Item 2" }
        };

        // Act
        _viewModel.MenuItems = newCollection;

        // Assert
        Assert.AreEqual(2, _viewModel.MenuItems.Count);
        Assert.AreSame(newCollection, _viewModel.MenuItems);
    }

    #endregion
}
