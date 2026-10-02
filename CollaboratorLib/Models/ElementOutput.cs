using System.Collections.Generic;
using StoryCADLib.Models;

namespace StoryCollaborator.Models
{
    /// <summary>
    /// Describes an output that a workflow will produce
    /// </summary>
    public class ElementOutput
    {
        /// <summary>
        /// The type of story element to create or update
        /// </summary>
        public StoryItemType ElementType { get; set; }

        /// <summary>
        /// Semantic label for this element in the workflow context
        /// </summary>
        public required string ElementLabel { get; set; }

        /// <summary>
        /// Properties that will be created or updated
        /// </summary>
        public List<PropertySpec> PropertiesToUpdate { get; set; } = new();

    }
}
