namespace StoryCADLib.Services.Collaborator.Contracts;

/// <summary>
///     Context for workflow execution
/// </summary>
public class WorkflowContext
{
    /// <summary>
    ///     The current story element being worked on
    /// </summary>
    public StoryElement CurrentElement { get; set; }

    /// <summary>
    ///     The entire story model for context
    /// </summary>
    public StoryModel StoryModel { get; set; }

    /// <summary>
    ///     Identifier of the workflow being executed
    /// </summary>
    public string WorkflowId { get; set; }

    /// <summary>
    ///     Input parameters for the workflow
    /// </summary>
    public Dictionary<string, object> InputParameters { get; set; } = new();

    /// <summary>
    ///     Output results from the workflow
    /// </summary>
    public Dictionary<string, object> OutputResults { get; set; } = new();
}

/// <summary>
///     Represents a single step in a workflow
/// </summary>
public class WorkflowStep
{
    /// <summary>
    ///     Step number in the workflow sequence
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    ///     Name of the step
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Description of what this step does
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///     Input fields required for this step
    /// </summary>
    public List<WorkflowInputField> InputFields { get; set; } = new();

    /// <summary>
    ///     Output fields produced by this step
    /// </summary>
    public List<WorkflowOutputField> OutputFields { get; set; } = new();
}

/// <summary>
///     Represents an input field in a workflow step
/// </summary>
public class WorkflowInputField
{
    /// <summary>
    ///     Internal name of the field
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Display label for the field
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    ///     Type of the field (text, number, element, etc.)
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    ///     Whether this field is required
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    ///     Default value for the field
    /// </summary>
    public object DefaultValue { get; set; }

    /// <summary>
    ///     Help text for the field
    /// </summary>
    public string HelpText { get; set; }
}

/// <summary>
///     Represents an output field from a workflow step
/// </summary>
public class WorkflowOutputField
{
    /// <summary>
    ///     Internal name of the field
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Display label for the field
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    ///     Type of the field
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    ///     Description of the output
    /// </summary>
    public string Description { get; set; }
}

/// <summary>
///     Configuration for a complete workflow
/// </summary>
public class WorkflowConfiguration
{
    /// <summary>
    ///     Unique identifier for the workflow
    /// </summary>
    public string WorkflowId { get; set; }

    /// <summary>
    ///     Display name of the workflow
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Description of the workflow
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///     Type of story element this workflow applies to
    /// </summary>
    public StoryItemType ElementType { get; set; }

    /// <summary>
    ///     Steps in the workflow
    /// </summary>
    public List<WorkflowStep> Steps { get; set; } = new();

    /// <summary>
    ///     Whether this workflow is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
