using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Collaborator.Contracts;
using System;
using System.Collections.Generic;

namespace StoryCADTests;

[TestClass]
public class WorkflowContextTests
{
    /// <summary>
    /// Test that WorkflowContext can be created and contains required properties
    /// </summary>
    [TestMethod]
    public void WorkflowContext_CanBeCreated()
    {
        // Arrange & Act
        var context = new WorkflowContext
        {
            CurrentElement = new StoryElement { Name = "Test Element" },
            StoryModel = new StoryModel(),
            WorkflowId = "test-workflow",
            InputParameters = new Dictionary<string, object>
            {
                { "param1", "value1" },
                { "param2", 42 }
            }
        };
        
        // Assert
        Assert.IsNotNull(context);
        Assert.IsNotNull(context.CurrentElement);
        Assert.AreEqual("Test Element", context.CurrentElement.Name);
        Assert.IsNotNull(context.StoryModel);
        Assert.AreEqual("test-workflow", context.WorkflowId);
        Assert.IsNotNull(context.InputParameters);
        Assert.AreEqual(2, context.InputParameters.Count);
    }

    /// <summary>
    /// Test that WorkflowStep can be created
    /// </summary>
    [TestMethod]
    public void WorkflowStep_CanBeCreated()
    {
        // Arrange & Act
        var step = new WorkflowStep
        {
            StepNumber = 1,
            Name = "Generate Content",
            Description = "Generate story content based on input",
            InputFields = new List<WorkflowInputField>
            {
                new WorkflowInputField
                {
                    Name = "premise",
                    Label = "Story Premise",
                    Type = "text",
                    Required = true
                }
            },
            OutputFields = new List<WorkflowOutputField>
            {
                new WorkflowOutputField
                {
                    Name = "generatedContent",
                    Label = "Generated Content",
                    Type = "text"
                }
            }
        };
        
        // Assert
        Assert.IsNotNull(step);
        Assert.AreEqual(1, step.StepNumber);
        Assert.AreEqual("Generate Content", step.Name);
        Assert.IsNotNull(step.InputFields);
        Assert.AreEqual(1, step.InputFields.Count);
        Assert.IsNotNull(step.OutputFields);
        Assert.AreEqual(1, step.OutputFields.Count);
    }

    /// <summary>
    /// Test that WorkflowConfiguration can be created
    /// </summary>
    [TestMethod]
    public void WorkflowConfiguration_CanBeCreated()
    {
        // Arrange & Act
        var config = new WorkflowConfiguration
        {
            WorkflowId = "character-development",
            Name = "Character Development Workflow",
            Description = "Develop character details through AI assistance",
            ElementType = StoryItemType.Character,
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { StepNumber = 1, Name = "Step 1" },
                new WorkflowStep { StepNumber = 2, Name = "Step 2" }
            }
        };
        
        // Assert
        Assert.IsNotNull(config);
        Assert.AreEqual("character-development", config.WorkflowId);
        Assert.AreEqual("Character Development Workflow", config.Name);
        Assert.AreEqual(StoryItemType.Character, config.ElementType);
        Assert.IsNotNull(config.Steps);
        Assert.AreEqual(2, config.Steps.Count);
    }
}