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
    ///     Test that CollaboratorService can be created with an ICollaborator
    /// </summary>
    [TestMethod]
    public void CollaboratorService_CanUseInterface()
    {
        // Arrange
        ICollaborator mockCollaborator = new MockCollaboratorImplementation();
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();

        // Act
        service.SetCollaborator(mockCollaborator);

        // Assert
        Assert.IsNotNull(service);
        Assert.IsTrue(service.HasCollaborator);
    }

    /// <summary>
    ///     Test that LoadWorkflowViewModel delegates to interface
    /// </summary>
    [TestMethod]
    public void CollaboratorService_LoadWorkflowViewModel_UsesInterface()
    {
        // Arrange
        var mockCollaborator = new MockCollaboratorImplementation();
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        service.SetCollaborator(mockCollaborator);

        // Act
        service.LoadWorkflowViewModel(StoryItemType.Character);

        // Assert
        Assert.IsTrue(mockCollaborator.LoadWorkflowViewModelCalled);
        Assert.AreEqual(StoryItemType.Character, mockCollaborator.LastElementType);
    }

    /// <summary>
    ///     Test that LoadWizardViewModel delegates to interface
    /// </summary>
    [TestMethod]
    public void CollaboratorService_LoadWizardViewModel_UsesInterface()
    {
        // Arrange
        var mockCollaborator = new MockCollaboratorImplementation();
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        service.SetCollaborator(mockCollaborator);

        // Act
        service.LoadWizardViewModel();

        // Assert
        Assert.IsTrue(mockCollaborator.LoadWizardViewModelCalled);
    }

    /// <summary>
    ///     Test that LoadWorkflowModel delegates to interface
    /// </summary>
    [TestMethod]
    public void CollaboratorService_LoadWorkflowModel_UsesInterface()
    {
        // Arrange
        var mockCollaborator = new MockCollaboratorImplementation();
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        service.SetCollaborator(mockCollaborator);
        var element = new StoryElement { Name = "Test" };

        // Act
        service.LoadWorkflowModel(element, "test-workflow");

        // Assert
        Assert.IsTrue(mockCollaborator.LoadWorkflowModelCalled);
        Assert.AreEqual(element, mockCollaborator.LastElement);
        Assert.AreEqual("test-workflow", mockCollaborator.LastWorkflow);
    }

    /// <summary>
    ///     Test that async methods work through interface
    /// </summary>
    [TestMethod]
    public async Task CollaboratorService_AsyncMethods_UseInterface()
    {
        // Arrange
        var mockCollaborator = new MockCollaboratorImplementation();
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        service.SetCollaborator(mockCollaborator);

        // Act
        await service.ProcessWorkflowAsync();
        await service.SendButtonClickedAsync();

        // Assert
        Assert.IsTrue(mockCollaborator.ProcessWorkflowCalled);
        Assert.IsTrue(mockCollaborator.SendButtonClickedCalled);
    }

    /// <summary>
    ///     Test that SaveOutputs delegates to interface
    /// </summary>
    [TestMethod]
    public void CollaboratorService_SaveOutputs_UsesInterface()
    {
        // Arrange
        var mockCollaborator = new MockCollaboratorImplementation();
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        service.SetCollaborator(mockCollaborator);

        // Act
        service.SaveOutputs();

        // Assert
        Assert.IsTrue(mockCollaborator.SaveOutputsCalled);
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

    /// <summary>
    ///     Mock implementation for testing
    /// </summary>
    private class MockCollaboratorImplementation : ICollaborator
    {
        public bool LoadWorkflowViewModelCalled { get; private set; }
        public bool LoadWizardViewModelCalled { get; private set; }
        public bool LoadWorkflowModelCalled { get; private set; }
        public bool ProcessWorkflowCalled { get; private set; }
        public bool SendButtonClickedCalled { get; private set; }
        public bool SaveOutputsCalled { get; private set; }

        public StoryItemType LastElementType { get; private set; }
        public StoryElement LastElement { get; private set; }
        public string LastWorkflow { get; private set; }

        public Window CreateWindow(object context)
        {
            // Return null since we can't create windows in tests
            return null;
        }

        public void LoadWorkflowViewModel(StoryItemType elementType)
        {
            LoadWorkflowViewModelCalled = true;
            LastElementType = elementType;
        }

        public void LoadWizardViewModel()
        {
            LoadWizardViewModelCalled = true;
        }

        public void LoadWorkflowModel(StoryElement element, string workflow)
        {
            LoadWorkflowModelCalled = true;
            LastElement = element;
            LastWorkflow = workflow;
        }

        public async Task ProcessWorkflowAsync()
        {
            ProcessWorkflowCalled = true;
            await Task.CompletedTask;
        }

        public async Task SendButtonClickedAsync()
        {
            SendButtonClickedCalled = true;
            await Task.CompletedTask;
        }

        public void SaveOutputs()
        {
            SaveOutputsCalled = true;
        }
    }
}
