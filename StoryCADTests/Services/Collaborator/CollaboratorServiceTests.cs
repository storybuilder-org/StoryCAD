using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Collaborator;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Logging;

#nullable disable

namespace StoryCADTests.Services.Collaborator;

[TestClass]
public class CollaboratorServiceTests
{
    private static CollaboratorService CreateService(Func<ICollaborator> factory = null) =>
        new(
            Ioc.Default.GetRequiredService<AppState>(),
            Ioc.Default.GetRequiredService<ILogService>(),
            Ioc.Default.GetRequiredService<PreferenceService>(),
            Ioc.Default.GetRequiredService<AutoSaveService>(),
            Ioc.Default.GetRequiredService<BackupService>(),
            Ioc.Default.GetRequiredService<StoryCADApi>(),
            factory);

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
    ///     With no factory registered (public/free build, CollaboratorLib not compiled in),
    ///     HasCollaborator is false and the Collaborator entry point stays hidden.
    /// </summary>
    [TestMethod]
    public void HasCollaborator_NoFactory_ReturnsFalse()
    {
        var service = CreateService(factory: null);

        Assert.IsFalse(service.HasCollaborator,
            "HasCollaborator should be false when no Collaborator factory is registered");
    }

    /// <summary>
    ///     When a factory is registered at the composition root (CollaboratorLib compiled in),
    ///     HasCollaborator is true. The factory itself is the seam #30's license gate will wrap.
    /// </summary>
    [TestMethod]
    public void HasCollaborator_WithFactory_ReturnsTrue()
    {
        var service = CreateService(factory: () => null);

        Assert.IsTrue(service.HasCollaborator,
            "HasCollaborator should be true when a Collaborator factory is registered");
    }
}
