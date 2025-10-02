using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services;

namespace StoryCADTests.Services;

[TestClass]
public class EditFlushServiceTests
{
    private AppState _appState;
    private EditFlushService _service;
    
    [TestInitialize]
    public void Initialize()
    {
        _appState = new AppState();
        _service = new EditFlushService(_appState);
    }
    
    [TestMethod]
    public void FlushCurrentEdits_WithNoCurrentSaveable_ReturnsSuccessfully()
    {
        // Arrange
        _appState.CurrentSaveable = null;
        
        // Act & Assert - should not throw
        _service.FlushCurrentEdits();
    }
    
    [TestMethod]
    public void FlushCurrentEdits_WithCurrentSaveable_CallsSaveModel()
    {
        // Arrange
        var saveable = new TestSaveable();
        _appState.CurrentSaveable = saveable;
        
        // Act
        _service.FlushCurrentEdits();
        
        // Assert
        Assert.IsTrue(saveable.SaveModelCalled);
    }
    
    [TestMethod]
    public void FlushCurrentEdits_WhenSaveModelThrows_LogsErrorAndContinues()
    {
        // Arrange
        var saveable = new ThrowingSaveable();
        _appState.CurrentSaveable = saveable;
        
        // Act & Assert - should not throw even though SaveModel throws
        _service.FlushCurrentEdits();
    }
    
    private class TestSaveable : ISaveable
    {
        public bool SaveModelCalled { get; private set; }
        
        public void SaveModel()
        {
            SaveModelCalled = true;
        }
    }
    
    private class ThrowingSaveable : ISaveable
    {
        public void SaveModel()
        {
            throw new System.InvalidOperationException("Test exception");
        }
    }
}