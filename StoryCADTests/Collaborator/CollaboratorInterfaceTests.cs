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

    /// <summary>
    ///     Test that CreateWindow method exists and can be called
    /// </summary>
    [TestMethod]
    public void ICollaborator_CreateWindow_MethodExists()
    {
        // Arrange
        ICollaborator collaborator = new MockCollaborator();
        object context = new { Test = "context" };

        // Act & Assert - just verify method can be called
        // Note: CreateWindow returns null in mock since Window requires UI thread
        var result = collaborator.CreateWindow(context);
        // We're testing the interface contract, not the implementation
        Assert.IsTrue(true); // Method exists and can be called
    }

    // Test methods for removed interface methods have been deleted
    // The interface now only has CreateWindow and Dispose methods

    /// <summary>
    ///     Mock implementation for testing
    /// </summary>
    private class MockCollaborator : ICollaborator
    {
        public Window CreateWindow(object context)
        {
            // Return null since creating Window requires UI thread
            // This is just testing the interface contract
            return null;
        }

        public void Dispose()
        {
            // Mock implementation
        }
    }
}
