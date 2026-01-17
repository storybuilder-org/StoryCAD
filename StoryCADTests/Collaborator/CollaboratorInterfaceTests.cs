using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;
#if WINDOWS10_0_22621_0_OR_GREATER
#endif

#nullable disable

namespace StoryCADTests.Collaborator;

[TestClass]
public class CollaboratorInterfaceTests
{
    /// <summary>
    ///     Test that ICollaborator interface can be implemented with required methods
    /// </summary>
    [TestMethod]
    public void ICollaborator_CanBeImplemented()
    {
        // Arrange & Act
        ICollaborator collaborator = new MockCollaborator();

        // Assert
        Assert.IsNotNull(collaborator);
    }

    // Test methods for removed interface methods have been deleted
    // The interface now only has CreateWindow and Dispose methods

    /// <summary>
    ///     Mock implementation for testing
    /// </summary>
    private class MockCollaborator : ICollaborator
    {
        public Task<Window> OpenAsync(IStoryCADAPI api, StoryModel model, Window hostWindow, Frame hostFrame, string filePath)
        {
            return Task.FromResult<Window>(null);
        }

        public CollaboratorResult Close()
        {
            return new CollaboratorResult { Completed = true, Summary = "mock" };
        }

        public void SetSettings(CollaboratorSettings settings)
        {
        }

        public CollaboratorSettings GetSettings()
        {
            return new CollaboratorSettings();
        }

        public void Dispose()
        {
        }
    }
}
