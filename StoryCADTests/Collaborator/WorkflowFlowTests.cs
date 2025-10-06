using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Services.Collaborator;
using StoryCAD.Services.Collaborator.Contracts;

#nullable disable

namespace StoryCADTests.Collaborator;

[TestClass]
public class WorkflowFlowTests
{
    /// <summary>
    ///     Test the workflow loading sequence
    /// </summary>
    [TestMethod]
    public void Workflow_LoadingSequence_WorksCorrectly()
    {
        // Arrange
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        var mock = new TestWorkflowCollaborator();
        service.SetCollaborator(mock);

        var element = new StoryElement
        {
            Name = "Test Scene",
            ElementType = StoryItemType.Scene
        };

        // Act - Simulate workflow loading sequence
        // 1. Load workflow view model for element type
        service.LoadWorkflowViewModel(StoryItemType.Scene);

        // 2. Load wizard view model
        service.LoadWizardViewModel();

        // 3. Load specific workflow model
        service.LoadWorkflowModel(element, "scene-builder");

        // Assert - Verify the sequence was executed
        Assert.AreEqual(1, mock.LoadWorkflowViewModelCallCount);
        Assert.AreEqual(1, mock.LoadWizardViewModelCallCount);
        Assert.AreEqual(1, mock.LoadWorkflowModelCallCount);
        Assert.AreEqual("scene-builder", mock.LastWorkflowLoaded);
    }

    /// <summary>
    ///     Test workflow processing flow
    /// </summary>
    [TestMethod]
    public async Task Workflow_ProcessingFlow_ExecutesInOrder()
    {
        // Arrange
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        var mock = new TestWorkflowCollaborator();
        service.SetCollaborator(mock);

        var element = new StoryElement
        {
            Name = "Test Problem",
            ElementType = StoryItemType.Problem
        };

        // Act - Execute full workflow
        service.LoadWorkflowViewModel(StoryItemType.Problem);
        service.LoadWorkflowModel(element, "problem-solver");
        await service.ProcessWorkflowAsync();
        await service.SendButtonClickedAsync();
        service.SaveOutputs();

        // Assert - Verify execution order
        Assert.IsTrue(mock.ProcessWorkflowExecuted);
        Assert.IsTrue(mock.SendButtonClickedExecuted);
        Assert.IsTrue(mock.SaveOutputsExecuted);

        // Verify order
        Assert.IsTrue(mock.ProcessWorkflowTime < mock.SendButtonClickedTime);
        Assert.IsTrue(mock.SendButtonClickedTime < mock.SaveOutputsTime);
    }

    /// <summary>
    ///     Test workflow with multiple elements
    /// </summary>
    [TestMethod]
    public void Workflow_MultipleElements_HandledCorrectly()
    {
        // Arrange
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        var mock = new TestWorkflowCollaborator();
        service.SetCollaborator(mock);

        var elements = new List<StoryElement>
        {
            new() { Name = "Hero", ElementType = StoryItemType.Character },
            new() { Name = "Villain", ElementType = StoryItemType.Character },
            new() { Name = "Opening", ElementType = StoryItemType.Scene }
        };

        // Act - Process multiple elements
        foreach (var element in elements)
        {
            service.LoadWorkflowViewModel(element.ElementType);
            service.LoadWorkflowModel(element, $"{element.ElementType.ToString().ToLower()}-workflow");
        }

        // Assert
        Assert.AreEqual(3, mock.LoadWorkflowViewModelCallCount);
        Assert.AreEqual(3, mock.LoadWorkflowModelCallCount);
        Assert.AreEqual("scene-workflow", mock.LastWorkflowLoaded);
    }

    /// <summary>
    ///     Test workflow cancellation and recovery
    /// </summary>
    [TestMethod]
    public async Task Workflow_ErrorHandling_RecoversProperly()
    {
        // Arrange
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();
        var mock = new TestWorkflowCollaborator { ShouldFailProcessWorkflow = true };
        service.SetCollaborator(mock);

        var element = new StoryElement { Name = "Test", ElementType = StoryItemType.Character };

        // Act
        service.LoadWorkflowModel(element, "test-workflow");

        try
        {
            await service.ProcessWorkflowAsync();
        }
        catch
        {
            // Expected to fail
        }

        // Recovery - Try again without failure
        mock.ShouldFailProcessWorkflow = false;
        await service.ProcessWorkflowAsync();

        // Assert - Should recover and complete
        Assert.IsTrue(mock.ProcessWorkflowExecuted);
    }

    /// <summary>
    ///     Extended mock collaborator for workflow testing
    /// </summary>
    private class TestWorkflowCollaborator : ICollaborator
    {
        public int LoadWorkflowViewModelCallCount { get; private set; }
        public int LoadWizardViewModelCallCount { get; private set; }
        public int LoadWorkflowModelCallCount { get; private set; }
        public string LastWorkflowLoaded { get; private set; }

        public bool ProcessWorkflowExecuted { get; private set; }
        public bool SendButtonClickedExecuted { get; private set; }
        public bool SaveOutputsExecuted { get; private set; }

        public DateTime ProcessWorkflowTime { get; private set; }
        public DateTime SendButtonClickedTime { get; private set; }
        public DateTime SaveOutputsTime { get; private set; }

        public bool ShouldFailProcessWorkflow { get; set; }

        public Window CreateWindow(object context) => null;

        public void LoadWorkflowViewModel(StoryItemType elementType)
        {
            LoadWorkflowViewModelCallCount++;
        }

        public void LoadWizardViewModel()
        {
            LoadWizardViewModelCallCount++;
        }

        public void LoadWorkflowModel(StoryElement element, string workflow)
        {
            LoadWorkflowModelCallCount++;
            LastWorkflowLoaded = workflow;
        }

        public async Task ProcessWorkflowAsync()
        {
            if (ShouldFailProcessWorkflow)
            {
                throw new Exception("Test failure");
            }

            ProcessWorkflowExecuted = true;
            ProcessWorkflowTime = DateTime.Now;
            await Task.Delay(10); // Small delay to ensure time difference
        }

        public async Task SendButtonClickedAsync()
        {
            SendButtonClickedExecuted = true;
            SendButtonClickedTime = DateTime.Now;
            await Task.Delay(10);
        }

        public void SaveOutputs()
        {
            SaveOutputsExecuted = true;
            SaveOutputsTime = DateTime.Now;
        }
    }
}
