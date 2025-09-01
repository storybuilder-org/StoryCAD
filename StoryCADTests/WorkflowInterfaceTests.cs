using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Collaborator.Contracts;
using StoryCAD.Services.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace StoryCADTests;

[TestClass]
public class WorkflowInterfaceTests
{
    /// <summary>
    /// Test that IWorkflowRunner interface can be implemented
    /// </summary>
    [TestMethod]
    public void IWorkflowRunner_CanBeImplemented()
    {
        // Arrange & Act
        IWorkflowRunner runner = new MockWorkflowRunner();
        
        // Assert
        Assert.IsNotNull(runner);
    }

    /// <summary>
    /// Test that RunAsync returns a WorkflowResult
    /// </summary>
    [TestMethod]
    public async Task IWorkflowRunner_RunAsync_ReturnsResult()
    {
        // Arrange
        IWorkflowRunner runner = new MockWorkflowRunner();
        var workflow = new WorkflowModel { Label = "Test" };
        var element = new StoryElement { Name = "Test Element" };
        object viewModel = new { };
        
        // Act
        var result = await runner.RunAsync(workflow, element, viewModel);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
    }

    /// <summary>
    /// Test that ValidateOutput returns a validation result
    /// </summary>
    [TestMethod]
    public void IWorkflowRunner_ValidateOutput_ReturnsValidation()
    {
        // Arrange
        IWorkflowRunner runner = new MockWorkflowRunner();
        var workflow = new WorkflowModel { Label = "Test" };
        string jsonOutput = "{}";
        
        // Act
        var validation = runner.ValidateOutput(jsonOutput, workflow);
        
        // Assert
        Assert.IsNotNull(validation);
        Assert.IsTrue(validation.IsValid);
    }

    /// <summary>
    /// Test that IStoryCADAPI interface can be implemented
    /// </summary>
    [TestMethod]
    public void IStoryCADAPI_CanBeImplemented()
    {
        // Arrange & Act
        IStoryCADAPI api = new MockStoryCADAPI();
        
        // Assert
        Assert.IsNotNull(api);
    }

    /// <summary>
    /// Test IStoryCADAPI model operations
    /// </summary>
    [TestMethod]
    public async Task IStoryCADAPI_ModelOperations_Work()
    {
        // Arrange
        IStoryCADAPI api = new MockStoryCADAPI();
        
        // Act & Assert - Test CreateEmptyOutline
        var createResult = await api.CreateEmptyOutline("Test Story", "Test Author", "0");
        Assert.IsNotNull(createResult);
        Assert.IsTrue(createResult.IsSuccess);
        
        // Test WriteOutline
        var writeResult = await api.WriteOutline("/test/path.stbx");
        Assert.IsNotNull(writeResult);
        Assert.IsTrue(writeResult.IsSuccess);
    }

    /// <summary>
    /// Test IStoryCADAPI element operations
    /// </summary>
    [TestMethod]
    public void IStoryCADAPI_ElementOperations_Work()
    {
        // Arrange
        IStoryCADAPI api = new MockStoryCADAPI();
        var testGuid = Guid.NewGuid();
        var testElement = new StoryElement { Uuid = testGuid, Name = "Test" };
        
        // Act & Assert - Test GetAllElements
        var elementsResult = api.GetAllElements();
        Assert.IsTrue(elementsResult.IsSuccess, "GetAllElements should succeed");
        Assert.IsNotNull(elementsResult.Payload);
        
        // Test UpdateStoryElement
        var updateResult = api.UpdateStoryElement(testElement, testGuid);
        Assert.IsTrue(updateResult.IsSuccess, "UpdateStoryElement should succeed");
        
        // Test GetStoryElement
        var retrieveResult = api.GetStoryElement(testGuid);
        Assert.IsTrue(retrieveResult.IsSuccess, "GetStoryElement should succeed");
        Assert.IsNotNull(retrieveResult.Payload, "Should return the element");
        
        // Test UpdateElementProperties
        var properties = new Dictionary<string, object> { { "Name", "Updated" } };
        var updatePropsResult = api.UpdateElementProperties(testGuid, properties);
        Assert.IsTrue(updatePropsResult.IsSuccess, "UpdateElementProperties should succeed");
        
        // Test UpdateElementProperty
        var result = api.UpdateElementProperty(testGuid, "Name", "New Name");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccess);
    }

    /// <summary>
    /// Mock implementation of IWorkflowRunner for testing
    /// </summary>
    private class MockWorkflowRunner : IWorkflowRunner
    {
        public async Task<WorkflowResult> RunAsync(WorkflowModel workflow, StoryElement element, object viewModel)
        {
            await Task.CompletedTask;
            return new WorkflowResult 
            { 
                Success = true, 
                Message = "Test completed",
                UpdatedElements = new List<StoryElement>(),
                NewElements = new List<StoryElement>()
            };
        }

        public WorkflowValidation ValidateOutput(string jsonOutput, WorkflowModel workflow)
        {
            return new WorkflowValidation { IsValid = true, ValidationMessage = "Valid" };
        }
    }

    /// <summary>
    /// Mock implementation of IStoryCADAPI for testing
    /// </summary>
    private class MockStoryCADAPI : IStoryCADAPI
    {
        private Dictionary<Guid, StoryElement> _elements = new();
        
        public StoryModel CurrentModel { get; set; } = new StoryModel();

        public async Task<OperationResult<List<Guid>>> CreateEmptyOutline(string name, string author, string templateIndex)
        {
            await Task.CompletedTask;
            return new OperationResult<List<Guid>>
            {
                IsSuccess = true,
                Payload = new List<Guid> { Guid.NewGuid() }
            };
        }

        public async Task<OperationResult<string>> WriteOutline(string filePath)
        {
            await Task.CompletedTask;
            return new OperationResult<string>
            {
                IsSuccess = true,
                Payload = filePath
            };
        }

        public OperationResult<ObservableCollection<StoryElement>> GetAllElements()
        {
            return OperationResult<ObservableCollection<StoryElement>>.Success(
                new ObservableCollection<StoryElement>(_elements.Values));
        }

        public OperationResult<bool> UpdateStoryElement(object newElement, Guid guid)
        {
            if (newElement is StoryElement element)
            {
                _elements[guid] = element;
                return OperationResult<bool>.Success(true);
            }
            return OperationResult<bool>.Failure("Invalid element type");
        }

        public OperationResult<bool> UpdateElementProperties(Guid elementGuid, Dictionary<string, object> properties)
        {
            if (_elements.TryGetValue(elementGuid, out var element))
            {
                // Update properties
                return OperationResult<bool>.Success(true);
            }
            return OperationResult<bool>.Failure("Element not found");
        }

        public OperationResult<object> UpdateElementProperty(Guid elementGuid, string propertyName, object newValue)
        {
            return new OperationResult<object>
            {
                IsSuccess = true,
                Payload = newValue
            };
        }

        public OperationResult<StoryElement> GetStoryElement(Guid guid)
        {
            if (_elements.TryGetValue(guid, out var element))
            {
                return OperationResult<StoryElement>.Success(element);
            }
            return OperationResult<StoryElement>.Failure("Element not found");
        }
    }
}