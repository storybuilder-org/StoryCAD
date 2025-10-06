using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Services.Collaborator;
using StoryCAD.Services.Logging;

namespace StoryCADTests.Collaborator;

[TestClass]
public class MockCollaboratorTests
{
    /// <summary>
    ///     Test that MockCollaborator can be created
    /// </summary>
    [TestMethod]
    public void MockCollaborator_CanBeCreated()
    {
        // Arrange & Act
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var mock = new MockCollaborator(logger);

        // Assert
        Assert.IsNotNull(mock);
    }

    /// <summary>
    ///     Test that MockCollaborator tracks state correctly
    /// </summary>
    [TestMethod]
    public void MockCollaborator_TracksState()
    {
        // Arrange
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var mock = new MockCollaborator(logger);
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
    ///     Test that MockCollaborator async methods work
    /// </summary>
    [TestMethod]
    public async Task MockCollaborator_AsyncMethodsWork()
    {
        // Arrange
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var mock = new MockCollaborator(logger);
        var element = new StoryElement { Name = "Test Scene" };
        mock.LoadWorkflowModel(element, "scene-builder");

        // Act & Assert - should not throw
        await mock.ProcessWorkflowAsync();
        await mock.SendButtonClickedAsync();
    }

    /// <summary>
    ///     Test that MockCollaborator can be used with CollaboratorService
    /// </summary>
    [TestMethod]
    public void MockCollaborator_WorksWithService()
    {
        // Arrange
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var mock = new MockCollaborator(logger);
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();

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
