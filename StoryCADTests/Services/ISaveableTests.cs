using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Services;

namespace StoryCAD.Tests.Services;

[TestClass]
public class ISaveableTests
{
    private class TestSaveableViewModel : ISaveable
    {
        public bool SaveModelCalled { get; private set; }
        
        public void SaveModel()
        {
            SaveModelCalled = true;
        }
    }
    
    [TestMethod]
    public void SaveModel_WhenCalled_ExecutesWithoutParameters()
    {
        // Arrange
        var viewModel = new TestSaveableViewModel();
        
        // Act
        viewModel.SaveModel();
        
        // Assert
        Assert.IsTrue(viewModel.SaveModelCalled);
    }
    
    [TestMethod]
    public void ISaveable_CanBeCastFromNonSaveable_ReturnsNull()
    {
        // Arrange
        object nonSaveableObject = new object();
        
        // Act
        var saveable = nonSaveableObject as ISaveable;
        
        // Assert
        Assert.IsNull(saveable);
    }
    
    [TestMethod]
    public void ISaveable_CanBeCastFromSaveable_ReturnsInstance()
    {
        // Arrange
        var viewModel = new TestSaveableViewModel();
        
        // Act
        var saveable = viewModel as ISaveable;
        
        // Assert
        Assert.IsNotNull(saveable);
        Assert.AreSame(viewModel, saveable);
    }
}