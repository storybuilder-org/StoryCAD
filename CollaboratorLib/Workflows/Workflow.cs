using System.Collections.Generic;
using StoryCADLib.Models;
using StoryCollaborator.Models;

namespace StoryCollaborator.Workflows
{
    /// <summary>
    /// Standard workflow class for all AI-assisted story development workflows.
    /// Provides two constructors:
    /// - Simple: For basic workflows with single input/output element type
    /// - Full: For complex workflows with custom WorkflowIO configuration
    /// </summary>
    public sealed class Workflow
    {
        // Core properties
        public string Label { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string PluginsFolder => Plugins;

        // Additional properties
        public StoryModel? Model { get; set; }
        public string Plugins { get; set; } = string.Empty;

        private WorkflowIO _workflowIO;

        /// <summary>
        /// Creates an empty workflow (for serialization or testing).
        /// </summary>
        public Workflow()
        {
            _workflowIO = new WorkflowIO();
        }

        /// <summary>
        /// Creates a workflow with simple input/output based on a single element type.
        /// Use this for straightforward workflows that operate on one element type.
        /// </summary>
        /// <param name="label">Short identifier used for plugin folder lookup (e.g., "StoryForm")</param>
        /// <param name="title">Display title (e.g., "Story Form")</param>
        /// <param name="description">Brief description of what the workflow does</param>
        /// <param name="primaryElementType">The main element type this workflow operates on</param>
        /// <param name="explanation">Optional detailed explanation</param>
        /// <param name="outputProperties">Optional list of property descriptors this workflow will update</param>
        public Workflow(
            string label,
            string title,
            string description,
            StoryItemType primaryElementType,
            string? explanation = null,
            List<PropertySpec>? outputProperties = null,
            List<string>? exampleLists = null)
        {
            Label = label;
            Title = title;
            Description = description;
            Explanation = explanation ?? string.Empty;
            Plugins = label;

            _workflowIO = CreateSimpleWorkflowIO(primaryElementType, outputProperties, exampleLists);
        }

        /// <summary>
        /// Creates a workflow with full WorkflowIO configuration.
        /// Use this for complex workflows with multiple inputs, optional inputs, or complex outputs.
        /// </summary>
        /// <param name="label">Short identifier used for plugin folder lookup</param>
        /// <param name="title">Display title</param>
        /// <param name="description">Brief description</param>
        /// <param name="explanation">Detailed explanation</param>
        /// <param name="workflowIO">Full input/output specification</param>
        public Workflow(
            string label,
            string title,
            string description,
            string explanation,
            WorkflowIO workflowIO)
        {
            Label = label;
            Title = title;
            Description = description;
            Explanation = explanation;
            Plugins = label;

            _workflowIO = workflowIO;
        }

        /// <summary>
        /// Gets the input/output specification for this workflow
        /// </summary>
        public WorkflowIO GetIO()
        {
            return _workflowIO;
        }

        /// <summary>
        /// Sets the input/output specification for this workflow
        /// </summary>
        public void SetIO(WorkflowIO io)
        {
            _workflowIO = io;
        }

        /// <summary>
        /// Initializes the workflow with a StoryModel
        /// </summary>
        public void Initialize(StoryModel model)
        {
            Model = model;
        }

        private static WorkflowIO CreateSimpleWorkflowIO(StoryItemType elementType, List<PropertySpec>? outputProperties, List<string>? exampleLists = null)
        {
            var elementLabel = GetDefaultLabel(elementType);

            return new WorkflowIO
            {
                RequiredInputs = new List<ElementRequirement>
                {
                    new ElementRequirement
                    {
                        ElementType = elementType,
                        ElementLabel = elementLabel,
                        RequiredProperties = new List<PropertySpec>(),
                        CreateIfMissing = false
                    }
                },

                OptionalInputs = new List<ElementRequirement>(),

                Outputs = new List<ElementOutput>
                {
                    new ElementOutput
                    {
                        ElementType = elementType,
                        ElementLabel = elementLabel,
                        PropertiesToUpdate = outputProperties ?? new List<PropertySpec>()
                    }
                },

                ExampleLists = exampleLists ?? new List<string>()
            };
        }

        /// <summary>
        /// Gets the default label for an element type (used in WorkflowIO).
        /// </summary>
        public static string GetDefaultLabel(StoryItemType elementType)
        {
            return elementType switch
            {
                StoryItemType.StoryOverview => "Overview",
                StoryItemType.Problem => "Problem",
                StoryItemType.Character => "Character",
                StoryItemType.Setting => "Setting",
                StoryItemType.Scene => "Scene",
                StoryItemType.Folder => "Folder",
                StoryItemType.Section => "Section",
                StoryItemType.Web => "Web",
                StoryItemType.Notes => "Notes",
                StoryItemType.TrashCan => "Trash",
                _ => elementType.ToString()
            };
        }
    }
}
