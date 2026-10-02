using System.Collections.Generic;
using StoryCADLib.Models;

namespace StoryCollaborator.Models
{
    /// <summary>
    /// Describes an input element requirement for a workflow
    /// </summary>
    public class ElementRequirement
    {
        /// <summary>
        /// The type of story element required
        /// </summary>
        public StoryItemType ElementType { get; set; }

        /// <summary>
        /// Semantic label for this element in the workflow context
        /// (e.g., "Overview", "StoryProblem", "Protagonist")
        /// </summary>
        public required string ElementLabel { get; set; }

        /// <summary>
        /// Properties that must have values for this requirement to be satisfied.
        /// Only PropertySpec.Property is read for input requirements.
        /// </summary>
        public List<PropertySpec> RequiredProperties { get; set; } = new();

        /// <summary>
        /// Whether to create the element if it doesn't exist
        /// </summary>
        public bool CreateIfMissing { get; set; }

        /// <summary>
        /// If this references another element (like StoryProblem on Overview),
        /// this describes that relationship
        /// </summary>
        public string? ReferencedElementLabel { get; set; }
    }
}
