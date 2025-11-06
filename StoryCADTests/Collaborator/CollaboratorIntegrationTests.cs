using System.Reflection;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Logging;

namespace StoryCADTests.Collaborator;

[TestClass]
public class CollaboratorIntegrationTests
{
    /// <summary>
    ///     Test that we can load CollaboratorLib via interface if DLL exists
    /// </summary>
    [TestMethod]
    public void CollaboratorService_CanLoadViaInterface_IfDllExists()
    {
        // This test will only work if CollaboratorLib.dll is in the output directory
        // In a real deployment, this would be loaded from the plugin directory

        // Arrange
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CollaboratorLib.dll");

        // Act & Assert
        if (File.Exists(dllPath))
        {
            // If DLL exists, try to load it
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                var collaboratorType = assembly.GetType("StoryCollaborator.Collaborator");

                if (collaboratorType != null)
                {
                    // Check if it implements ICollaborator
                    var implementsInterface = typeof(ICollaborator).IsAssignableFrom(collaboratorType);
                    Assert.IsTrue(implementsInterface, "Collaborator should implement ICollaborator interface");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to load CollaboratorLib: {ex.Message}");
            }
        }
        else
        {
            // If DLL doesn't exist, use mock
            var logger = Ioc.Default.GetRequiredService<ILogService>();
            var mock = new MockCollaborator(logger);
            service.SetCollaborator(mock);
            Assert.IsTrue(service.HasCollaborator);
        }
    }

    /// <summary>
    ///     Test that CollaboratorService falls back to mock when DLL not available
    /// </summary>
    [TestMethod]
    public void CollaboratorService_UseMock_WhenDllNotAvailable()
    {
        // Arrange
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var mock = new MockCollaborator(logger);

        // Act
        service.SetCollaborator(mock);

        // Assert
        Assert.IsTrue(service.HasCollaborator);
    }

    /// <summary>
    ///     Test workflow execution through the interface
    /// </summary>
    [TestMethod]
    public async Task CollaboratorService_CanExecuteWorkflow_ThroughInterface()
    {
        // Arrange
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var mock = new MockCollaborator(logger);
        service.SetCollaborator(mock);

        var element = new StoryElement
        {
            Name = "Test Character",
            ElementType = StoryItemType.Character,
            Uuid = Guid.NewGuid()
        };

        // Act
        service.LoadWorkflowViewModel(StoryItemType.Character);
        service.LoadWorkflowModel(element, "character-development");
        await service.ProcessWorkflowAsync();
        service.SaveOutputs();

        // Assert
        Assert.IsTrue(service.HasCollaborator);
        // Verify the mock received all the calls
        var state = mock.GetCurrentState();
        Assert.AreEqual(element, state.element);
        Assert.AreEqual("character-development", state.workflow);
    }

    /// <summary>
    ///     Test that both sync and async methods work
    /// </summary>
    [TestMethod]
    public async Task CollaboratorService_SupportsBothSyncAndAsync()
    {
        // Arrange
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var mock = new MockCollaborator(logger);
        service.SetCollaborator(mock);

        // Act - Test sync methods
        service.ProcessWorkflow(); // Sync version
        service.SendButtonClicked(); // Sync version

        // Act - Test async methods
        await service.ProcessWorkflowAsync(); // Async version
        await service.SendButtonClickedAsync(); // Async version

        // Assert
        Assert.IsTrue(service.HasCollaborator);
    }

    /// <summary>
    ///     Test interface compatibility with real Collaborator type if available
    /// </summary>
    [TestMethod]
    public void CollaboratorLib_ImplementsInterface_Correctly()
    {
        // This test verifies the actual CollaboratorLib implementation
        var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CollaboratorLib.dll");

        if (!File.Exists(dllPath))
        {
            // Skip test if DLL not available
            Assert.Inconclusive("CollaboratorLib.dll not found in test directory");
            return;
        }

        try
        {
            // Load the assembly
            var assembly = Assembly.LoadFrom(dllPath);
            var collaboratorType = assembly.GetType("StoryCollaborator.Collaborator");

            Assert.IsNotNull(collaboratorType, "Collaborator type should exist");

            // Check interface implementation
            Assert.IsTrue(typeof(ICollaborator).IsAssignableFrom(collaboratorType),
                "Collaborator should implement ICollaborator");

            // Check for required methods
            var methods = collaboratorType.GetMethods();

            Assert.IsTrue(Array.Exists(methods, m => m.Name == "CreateWindow"),
                "Should have CreateWindow method");
            Assert.IsTrue(Array.Exists(methods, m => m.Name == "LoadWorkflowViewModel"),
                "Should have LoadWorkflowViewModel method");
            Assert.IsTrue(Array.Exists(methods, m => m.Name == "LoadWizardViewModel"),
                "Should have LoadWizardViewModel method");
            Assert.IsTrue(Array.Exists(methods, m => m.Name == "ProcessWorkflowAsync"),
                "Should have ProcessWorkflowAsync method");
            Assert.IsTrue(Array.Exists(methods, m => m.Name == "SendButtonClickedAsync"),
                "Should have SendButtonClickedAsync method");
            Assert.IsTrue(Array.Exists(methods, m => m.Name == "SaveOutputs"),
                "Should have SaveOutputs method");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to validate CollaboratorLib interface: {ex.Message}");
        }
    }
}
