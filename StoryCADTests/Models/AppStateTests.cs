using StoryCAD.Models;
using StoryCAD.Services;

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
        var document = new StoryDocument(model, @"C:\test.stbx");

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
}
