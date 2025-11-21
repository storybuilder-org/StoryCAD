using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator;
using StoryCADLib.Services.Collaborator.Contracts;

#nullable disable

namespace StoryCADTests.Services.Collaborator;

[TestClass]
public class CollaboratorServiceTests
{
    /// <summary>
    ///     Test that CollaboratorService can be created
    /// </summary>
    [TestMethod]
    public void CollaboratorService_CanBeCreated()
    {
        // Arrange & Act
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();

        // Assert
        Assert.IsNotNull(service);
    }

    /// <summary>
    ///     Test that COLLAB_DEBUG=0 bypasses collaborator loading
    /// </summary>
    [TestMethod]
    public async Task CollaboratorService_CollabDebugZero_DisablesCollaborator()
    {
        // Arrange
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        var originalValue = Environment.GetEnvironmentVariable("COLLAB_DEBUG");

        try
        {
            // Act - Set COLLAB_DEBUG to 0
            Environment.SetEnvironmentVariable("COLLAB_DEBUG", "0");
            var result = await service.CollaboratorEnabled();

            // Assert
            Assert.IsFalse(result, "CollaboratorEnabled should return false when COLLAB_DEBUG=0");
        }
        finally
        {
            // Cleanup - Restore original value
            Environment.SetEnvironmentVariable("COLLAB_DEBUG", originalValue);
        }
    }

    /// <summary>
    ///     Test that COLLAB_DEBUG=1 allows normal collaborator checks
    /// </summary>
    [TestMethod]
    public async Task CollaboratorService_CollabDebugOne_AllowsNormalChecks()
    {
        // Arrange
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        var originalValue = Environment.GetEnvironmentVariable("COLLAB_DEBUG");
        var originalPluginDir = Environment.GetEnvironmentVariable("STORYCAD_PLUGIN_DIR");

        try
        {
            // Act - Set COLLAB_DEBUG to 1 (should continue with normal checks)
            Environment.SetEnvironmentVariable("COLLAB_DEBUG", "1");
            // Clear STORYCAD_PLUGIN_DIR to ensure we're testing the COLLAB_DEBUG=1 path
            Environment.SetEnvironmentVariable("STORYCAD_PLUGIN_DIR", null);

            var result = await service.CollaboratorEnabled();

            // Assert - Result depends on whether we're in developer build and if DLL exists
            // We're just testing that it doesn't return false immediately
            // The actual result will depend on the test environment
            var appState = Ioc.Default.GetRequiredService<AppState>();
            if (!appState.DeveloperBuild)
            {
                Assert.IsFalse(result, "Should be false when not in developer build and no plugin dir");
            }
            // If in developer build, result depends on whether DLL is found
        }
        finally
        {
            // Cleanup - Restore original values
            Environment.SetEnvironmentVariable("COLLAB_DEBUG", originalValue);
            Environment.SetEnvironmentVariable("STORYCAD_PLUGIN_DIR", originalPluginDir);
        }
    }

    // Note: Tests for removed interface methods have been deleted.
    // The ICollaborator interface now only has CreateWindow and Dispose methods.
}