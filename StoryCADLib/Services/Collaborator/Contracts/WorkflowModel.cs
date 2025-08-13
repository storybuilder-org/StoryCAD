using System.Collections.Generic;

namespace StoryCAD.Services.Collaborator.Contracts;

/// <summary>
/// Workflow definition (no implementation details)
/// </summary>
public class WorkflowModel
{
    /// <summary>
    /// Internal identifier for the workflow
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Display title for the workflow
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Description of what the workflow does
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// List of required inputs for the workflow
    /// </summary>
    public List<WorkflowInput> RequiredInputs { get; set; } = new();

    /// <summary>
    /// List of expected outputs from the workflow
    /// </summary>
    public List<WorkflowOutput> ExpectedOutputs { get; set; } = new();

    // No prompt templates or IP here - those stay in CollaboratorLib
}

/// <summary>
/// Defines an input required by a workflow
/// </summary>
public class WorkflowInput
{
    /// <summary>
    /// Name of the input
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Type of the input (e.g., "text", "element", "number")
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Whether this input is required
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Description of the input for user guidance
    /// </summary>
    public string Description { get; set; }
}

/// <summary>
/// Defines an output produced by a workflow
/// </summary>
public class WorkflowOutput
{
    /// <summary>
    /// Name of the output
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Type of the output
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Description of the output
    /// </summary>
    public string Description { get; set; }
}