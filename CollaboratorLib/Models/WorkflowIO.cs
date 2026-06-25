using System.Collections.Generic;

namespace StoryCollaborator.Models
{
    /// <summary>
    /// Defines the input and output contract for a workflow.
    /// This replaces the StoryPropertyTemplate approach with a cleaner,
    /// element-based model that works directly with the StoryCAD API.
    /// </summary>
    public class WorkflowIO
    {
        /// <summary>
        /// Required input elements for the workflow to function
        /// </summary>
        public List<ElementRequirement> RequiredInputs { get; set; } = new();

        /// <summary>
        /// Optional inputs that can enhance the workflow if available
        /// </summary>
        public List<ElementRequirement> OptionalInputs { get; set; } = new();

        /// <summary>
        /// Elements and properties that the workflow will create or update
        /// </summary>
        public List<ElementOutput> Outputs { get; set; } = new();

        /// <summary>
        /// Base names of example-list placeholders this workflow requires (e.g., "ConflictType" for {{$ConflictType_examples}}).
        /// EnrichWithExamples reads this instead of scanning the template text.
        /// </summary>
        public List<string> ExampleLists { get; set; } = new();
    }
}
