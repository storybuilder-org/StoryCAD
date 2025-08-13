using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Collaborator;
using System.Threading.Tasks;

namespace StoryCADTests;

[TestClass]
public class MockCollaboratorTests
{
    /// <summary>
    /// Test that MockCollaborator can be created
    /// </summary>
    [TestMethod]
    public void MockCollaborator_CanBeCreated()
    {
        // Arrange & Act
        var mock = new MockCollaborator();
        
        // Assert
        Assert.IsNotNull(mock);
    }

    /// <summary>
    /// Test that MockCollaborator tracks state correctly
    /// </summary>
    [TestMethod]
    public void MockCollaborator_TracksState()
    {
        // Arrange
        var mock = new MockCollaborator();
        var element = new StoryElement { Name = "Test Character", ElementType = StoryItemType.Character };
        
        // Act
        mock.LoadWorkflowViewModel(StoryItemType.Character);
        mock.LoadWorkflowModel(element, "character-development");
        
        var state = mock.GetCurrentState();
        
        // Assert
        Assert.AreEqual(element, state.element);
        Assert.AreEqual("character-development", state.workflow);
        Assert.AreEqual(StoryItemType.Character, state.elementType);
    }

    /// <summary>
    /// Test that MockCollaborator async methods work
    /// </summary>
    [TestMethod]
    public async Task MockCollaborator_AsyncMethodsWork()
    {
        // Arrange
        var mock = new MockCollaborator();
        var element = new StoryElement { Name = "Test Scene" };
        mock.LoadWorkflowModel(element, "scene-builder");
        
        // Act & Assert - should not throw
        await mock.ProcessWorkflowAsync();
        await mock.SendButtonClickedAsync();
    }

    /// <summary>
    /// Test that MockCollaborator can be used with CollaboratorService
    /// </summary>
    [TestMethod]
    public void MockCollaborator_WorksWithService()
    {
        // Arrange
        var mock = new MockCollaborator();
        var service = new CollaboratorService();
        
        // Act
        service.SetCollaborator(mock);
        var element = new StoryElement { Name = "Test Problem" };
        
        service.LoadWorkflowViewModel(StoryItemType.Problem);
        service.LoadWorkflowModel(element, "problem-solver");
        service.SaveOutputs();
        
        // Assert
        var state = mock.GetCurrentState();
        Assert.AreEqual(element, state.element);
        Assert.AreEqual("problem-solver", state.workflow);
        Assert.AreEqual(StoryItemType.Problem, state.elementType);
    }
}