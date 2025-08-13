using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using StoryCAD.Models;
using StoryCAD.Services.Collaborator.Contracts;
using System;
using System.Threading.Tasks;

namespace StoryCADTests;

[TestClass]
public class CollaboratorInterfaceTests
{
    /// <summary>
    /// Test that ICollaborator interface can be implemented with required methods
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
    /// Test that CreateWindow returns a Window
    /// </summary>
    [UITestMethod] 
    public void ICollaborator_CreateWindow_ReturnsWindow()
    {
        // Arrange
        ICollaborator collaborator = new MockCollaborator();
        object context = new { Test = "context" };
        
        // Act
        Window window = collaborator.CreateWindow(context);
        
        // Assert
        Assert.IsNotNull(window);
    }

    /// <summary>
    /// Test workflow loading methods exist
    /// </summary>
    [TestMethod]
    public void ICollaborator_WorkflowMethods_Exist()
    {
        // Arrange
        ICollaborator collaborator = new MockCollaborator();
        var element = new StoryElement { Name = "Test" };
        
        // Act & Assert - just verify methods can be called
        collaborator.LoadWorkflowViewModel(StoryItemType.Character);
        collaborator.LoadWizardViewModel();
        collaborator.LoadWorkflowModel(element, "test-workflow");
    }

    /// <summary>
    /// Test async methods
    /// </summary>
    [TestMethod]
    public async Task ICollaborator_AsyncMethods_CanExecute()
    {
        // Arrange
        ICollaborator collaborator = new MockCollaborator();
        
        // Act & Assert - verify async methods can be called
        await collaborator.ProcessWorkflowAsync();
        await collaborator.SendButtonClickedAsync();
    }

    /// <summary>
    /// Test SaveOutputs method
    /// </summary>
    [TestMethod]
    public void ICollaborator_SaveOutputs_CanExecute()
    {
        // Arrange
        ICollaborator collaborator = new MockCollaborator();
        
        // Act & Assert - verify method can be called
        collaborator.SaveOutputs();
    }

    /// <summary>
    /// Mock implementation for testing
    /// </summary>
    private class MockCollaborator : ICollaborator
    {
        public Window CreateWindow(object context)
        {
            return new Window();
        }

        public void LoadWorkflowViewModel(StoryItemType elementType)
        {
            // Mock implementation
        }

        public void LoadWizardViewModel()
        {
            // Mock implementation
        }

        public void LoadWorkflowModel(StoryElement element, string workflow)
        {
            // Mock implementation
        }

        public async Task ProcessWorkflowAsync()
        {
            await Task.CompletedTask;
        }

        public async Task SendButtonClickedAsync()
        {
            await Task.CompletedTask;
        }

        public void SaveOutputs()
        {
            // Mock implementation
        }
    }
}