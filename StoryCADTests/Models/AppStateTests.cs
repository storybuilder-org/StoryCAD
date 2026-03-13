using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.ViewModels;

namespace StoryCADTests.Models;

[TestClass]
public class AppStateTests
{
    [TestMethod]
    public void CurrentDocument_WhenSet_StoresValue()
    {
        // Arrange
        var appState = new AppState();
        var model = new StoryModel();
        var document = new StoryDocument(model, Path.Combine(Path.GetTempPath(), "test.stbx"));

        // Act
        appState.CurrentDocument = document;

        // Assert
        Assert.AreSame(document, appState.CurrentDocument);
    }

    [TestMethod]
    public void CurrentDocument_InitiallyNull()
    {
        // Arrange
        var appState = new AppState();

        // Act & Assert
        Assert.IsNull(appState.CurrentDocument);
    }

    [TestMethod]
    public void CurrentSaveable_WhenSet_StoresValue()
    {
        // Arrange
        var appState = new AppState();
        var saveable = new TestSaveable();

        // Act
        appState.CurrentSaveable = saveable;

        // Assert
        Assert.AreSame(saveable, appState.CurrentSaveable);
    }

    [TestMethod]
    public void CurrentSaveable_InitiallyNull()
    {
        // Arrange
        var appState = new AppState();

        // Act & Assert
        Assert.IsNull(appState.CurrentSaveable);
    }

    [TestMethod]
    public void CurrentSaveable_CanBeSetToNull()
    {
        // Arrange
        var appState = new AppState();
        appState.CurrentSaveable = new TestSaveable();

        // Act
        appState.CurrentSaveable = null;

        // Assert
        Assert.IsNull(appState.CurrentSaveable);
    }

    private class TestSaveable : ISaveable
    {
        public void SaveModel()
        {
        }
    }

    #region CurrentViewType Tests (Issue #1146)

    [TestMethod]
    public void CurrentViewType_WhenSet_StoresValue()
    {
        // Arrange
        var appState = new AppState();

        // Act
        appState.CurrentViewType = StoryViewType.NarratorView;

        // Assert
        Assert.AreEqual(StoryViewType.NarratorView, appState.CurrentViewType);
    }

    [TestMethod]
    public void CurrentViewType_InitiallyDefault()
    {
        // Arrange
        var appState = new AppState();

        // Act & Assert - enum defaults to first value (0 = ExplorerView)
        Assert.AreEqual(default(StoryViewType), appState.CurrentViewType);
    }

    #endregion

    #region CurrentNode Tests (Issue #1146)

    [TestMethod]
    public void CurrentNode_WhenSet_StoresValue()
    {
        // Arrange
        var appState = new AppState();
        var model = new StoryModel();
        var node = new StoryNodeItem(new OverviewModel("Test", model, null), null);

        // Act
        appState.CurrentNode = node;

        // Assert
        Assert.AreSame(node, appState.CurrentNode);
    }

    [TestMethod]
    public void CurrentNode_InitiallyNull()
    {
        // Arrange
        var appState = new AppState();

        // Act & Assert
        Assert.IsNull(appState.CurrentNode);
    }

    [TestMethod]
    public void CurrentNode_CanBeSetToNull()
    {
        // Arrange
        var appState = new AppState();
        var model = new StoryModel();
        var node = new StoryNodeItem(new OverviewModel("Test", model, null), null);
        appState.CurrentNode = node;

        // Act
        appState.CurrentNode = null;

        // Assert
        Assert.IsNull(appState.CurrentNode);
    }

    #endregion

    #region RightTappedNode Tests (Issue #1146)

    [TestMethod]
    public void RightTappedNode_WhenSet_StoresValue()
    {
        // Arrange
        var appState = new AppState();
        var model = new StoryModel();
        var node = new StoryNodeItem(new OverviewModel("Test", model, null), null);

        // Act
        appState.RightTappedNode = node;

        // Assert
        Assert.AreSame(node, appState.RightTappedNode);
    }

    [TestMethod]
    public void RightTappedNode_InitiallyNull()
    {
        // Arrange
        var appState = new AppState();

        // Act & Assert
        Assert.IsNull(appState.RightTappedNode);
    }

    [TestMethod]
    public void RightTappedNode_CanBeSetToNull()
    {
        // Arrange
        var appState = new AppState();
        var model = new StoryModel();
        var node = new StoryNodeItem(new OverviewModel("Test", model, null), null);
        appState.RightTappedNode = node;

        // Act
        appState.RightTappedNode = null;

        // Assert
        Assert.IsNull(appState.RightTappedNode);
    }

    #endregion

    #region IsClosing Tests (Issue #1293)

    [TestMethod]
    public void IsClosing_InitiallyFalse()
    {
        // Arrange
        var appState = new AppState();

        // Act & Assert - should default to false
        Assert.IsFalse(appState.IsClosing);
    }

    [TestMethod]
    public void IsClosing_WhenSetTrue_StoresValue()
    {
        // Arrange
        var appState = new AppState();

        // Act
        appState.IsClosing = true;

        // Assert
        Assert.IsTrue(appState.IsClosing);
    }

    [TestMethod]
    public void IsClosing_CanBeResetToFalse()
    {
        // Arrange
        var appState = new AppState();
        appState.IsClosing = true;

        // Act
        appState.IsClosing = false;

        // Assert
        Assert.IsFalse(appState.IsClosing);
    }

    #endregion
}
