using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.Services.Dialogs;

/// <summary>
///     Tests for ToolValidationService which validates prerequisites for tool usage.
///     These tests verify validation logic for explorer view requirements, outline state,
///     and node selection requirements. The service reads state from AppState.
/// </summary>
[TestClass]
public class ToolValidationServiceTests
{
    private ToolValidationService _toolValidationService;
    private AppState _appState;
    private StoryModel _model;
    private StoryNodeItem _testNode;

    [TestInitialize]
    public void TestInitialize()
    {
        _toolValidationService = Ioc.Default.GetRequiredService<ToolValidationService>();
        _appState = Ioc.Default.GetRequiredService<AppState>();

        // Create a minimal story model for testing
        _model = new StoryModel();
        _testNode = new StoryNodeItem(new OverviewModel("Test Story", _model, null), null);
        _model.ExplorerView.Add(_testNode);
        _model.NarratorView.Add(new StoryNodeItem(new FolderModel("Narrative View", _model, StoryItemType.Folder, null), null));

        // Set up AppState with default test state
        _appState.CurrentDocument = new StoryDocument(_model, "test.stbx");
        _appState.CurrentViewType = StoryViewType.ExplorerView;
        _appState.CurrentNode = _testNode;
        _appState.RightTappedNode = null;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Reset AppState after each test
        _appState.CurrentDocument = null;
        _appState.CurrentViewType = default;
        _appState.CurrentNode = null;
        _appState.RightTappedNode = null;
    }

    #region Basic Validation Tests (6)

    [TestMethod]
    public void VerifyToolUse_WithExplorerViewOnly_InExplorerView_ReturnsTrue()
    {
        // Arrange
        _appState.CurrentViewType = StoryViewType.ExplorerView;
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: true, nodeRequired: false);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifyToolUse_WithExplorerViewOnly_InNarratorView_ReturnsFalse()
    {
        // Arrange
        _appState.CurrentViewType = StoryViewType.NarratorView;
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: true, nodeRequired: false);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyToolUse_WithoutExplorerViewOnly_InNarratorView_ReturnsTrue()
    {
        // Arrange
        _appState.CurrentViewType = StoryViewType.NarratorView;
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: false);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifyToolUse_WithNodeRequired_WhenRightTappedNodeExists_ReturnsTrue()
    {
        // Arrange
        _appState.CurrentNode = null;
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: true);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifyToolUse_WithNodeRequired_WhenOnlyCurrentNodeExists_ReturnsTrue()
    {
        // Arrange
        _appState.CurrentNode = _testNode;
        _appState.RightTappedNode = null;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: true);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifyToolUse_WithoutNodeRequired_WhenBothNodesNull_ReturnsTrue()
    {
        // Arrange
        _appState.CurrentNode = null;
        _appState.RightTappedNode = null;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: false);

        // Assert
        Assert.IsTrue(result);
    }

    #endregion

    #region Outline Open Validation Tests (5)

    [TestMethod]
    public void VerifyToolUse_WithCheckOutlineIsOpen_WhenModelIsNull_ReturnsFalse()
    {
        // Arrange
        _appState.CurrentDocument = null;
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: false, checkOutlineIsOpen: true);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyToolUse_WithCheckOutlineIsOpen_WhenExplorerViewEmpty_ReturnsFalse()
    {
        // Arrange
        var emptyModel = new StoryModel();
        emptyModel.ExplorerView.Clear();
        _appState.CurrentDocument = new StoryDocument(emptyModel, "test.stbx");
        _appState.CurrentViewType = StoryViewType.ExplorerView;
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: false, checkOutlineIsOpen: true);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyToolUse_WithCheckOutlineIsOpen_WhenNarratorViewEmpty_ReturnsFalse()
    {
        // Arrange
        var model = new StoryModel();
        model.ExplorerView.Add(_testNode);
        model.NarratorView.Clear();
        _appState.CurrentDocument = new StoryDocument(model, "test.stbx");
        _appState.CurrentViewType = StoryViewType.NarratorView;
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: false, checkOutlineIsOpen: true);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyToolUse_WithCheckOutlineIsOpen_WhenModelPopulated_ReturnsTrue()
    {
        // Arrange
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: false, checkOutlineIsOpen: true);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifyToolUse_WithoutCheckOutlineIsOpen_WhenModelIsNull_ReturnsTrue()
    {
        // Arrange
        _appState.CurrentDocument = null;
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: false, checkOutlineIsOpen: false);

        // Assert
        Assert.IsTrue(result);
    }

    #endregion

    #region Side Effect Tests (4) - Critical for issue #1146

    [TestMethod]
    public void VerifyToolUse_WithNodeRequired_WhenBothNodesNull_ReturnsFalse()
    {
        // Arrange
        _appState.CurrentNode = null;
        _appState.RightTappedNode = null;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: true);

        // Assert
        Assert.IsFalse(result, "Should return false when node is required but both nodes are null");
        Assert.IsNull(_appState.RightTappedNode, "Should not set RightTappedNode when both are null");
    }

    [TestMethod]
    public void VerifyToolUse_WithNodeRequired_WhenRightTappedNull_SetsRightTappedToCurrentNode()
    {
        // Arrange - RightTappedNode is null, but CurrentNode exists
        _appState.CurrentNode = _testNode;
        _appState.RightTappedNode = null;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: true);

        // Assert
        Assert.IsTrue(result, "Should return true when CurrentNode exists");
        Assert.AreSame(_testNode, _appState.RightTappedNode, "Should set RightTappedNode to CurrentNode (side effect)");
    }

    [TestMethod]
    public void VerifyToolUse_WithNodeRequired_WhenRightTappedExists_DoesNotChangeRightTapped()
    {
        // Arrange - Both nodes exist, RightTappedNode should NOT be changed
        var rightTappedNode = new StoryNodeItem(new FolderModel("Test Folder", _model, StoryItemType.Folder, null), null);
        _appState.CurrentNode = _testNode;
        _appState.RightTappedNode = rightTappedNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: true);

        // Assert
        Assert.IsTrue(result, "Should return true when RightTappedNode exists");
        Assert.AreSame(rightTappedNode, _appState.RightTappedNode, "Should NOT change RightTappedNode when it already exists");
    }

    [TestMethod]
    public void VerifyToolUse_WithoutNodeRequired_NoSideEffect()
    {
        // Arrange - nodeRequired=false, so no side effect should occur
        _appState.CurrentNode = _testNode;
        _appState.RightTappedNode = null;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: false);

        // Assert
        Assert.IsTrue(result, "Should return true when nodeRequired is false");
        Assert.IsNull(_appState.RightTappedNode, "Should NOT set RightTappedNode when nodeRequired is false");
    }

    #endregion

    #region Edge Case Tests (3)

    [TestMethod]
    public void VerifyToolUse_WithDefaultViewType_AndExplorerViewOnly_ReturnsTrue()
    {
        // Arrange - default StoryViewType is ExplorerView (enum value 0)
        _appState.CurrentViewType = default;
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: true, nodeRequired: false);

        // Assert
        Assert.IsTrue(result, "Default StoryViewType is ExplorerView, should pass explorerViewOnly check");
    }

    [TestMethod]
    public void VerifyToolUse_WithAllValidationsPassed_ReturnsTrue()
    {
        // Arrange - all conditions met
        _appState.CurrentViewType = StoryViewType.ExplorerView;
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: true, nodeRequired: true, checkOutlineIsOpen: true);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifyToolUse_WithMultipleValidationFailures_ReturnsFalseForFirst()
    {
        // Arrange - multiple failures: wrong view, no node, no model
        _appState.CurrentViewType = StoryViewType.NarratorView;
        _appState.CurrentNode = null;
        _appState.RightTappedNode = null;
        _appState.CurrentDocument = null;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: true, nodeRequired: true, checkOutlineIsOpen: true);

        // Assert
        Assert.IsFalse(result, "Should return false on first validation failure");
    }

    #endregion

    #region Status Message Tests (3)

    [TestMethod]
    public void VerifyToolUse_ExplorerViewOnlyFailure_ReturnsFalse()
    {
        // Arrange
        _appState.CurrentViewType = StoryViewType.NarratorView;
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: true, nodeRequired: false);

        // Assert
        Assert.IsFalse(result, "Should return false and send status message for explorer view failure");
    }

    [TestMethod]
    public void VerifyToolUse_NodeRequiredFailure_ReturnsFalse()
    {
        // Arrange
        _appState.CurrentNode = null;
        _appState.RightTappedNode = null;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: true);

        // Assert
        Assert.IsFalse(result, "Should return false and send status message for node required failure");
    }

    [TestMethod]
    public void VerifyToolUse_OutlineOpenFailure_ReturnsFalse()
    {
        // Arrange
        _appState.CurrentDocument = null;
        _appState.RightTappedNode = _testNode;

        // Act
        var result = _toolValidationService.VerifyToolUse(
            explorerViewOnly: false, nodeRequired: false, checkOutlineIsOpen: true);

        // Assert
        Assert.IsFalse(result, "Should return false and send status message for outline open failure");
    }

    #endregion
}
