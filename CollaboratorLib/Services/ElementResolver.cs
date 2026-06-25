using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCollaborator.Models;

namespace StoryCollaborator.Services
{
    /// <summary>
    /// Resolves <see cref="ElementRequirement"/> and <see cref="ElementOutput"/> specifications
    /// to actual <see cref="StoryElement"/> instances via the StoryCAD API.
    ///
    /// <para>
    /// <b>Design Philosophy:</b> ElementResolver follows a conservative auto-resolution strategy.
    /// Only singleton types (StoryOverview) are auto-resolved. All other element types return null,
    /// signaling to the caller that user interaction via ElementPicker is required.
    /// </para>
    ///
    /// <para>
    /// <b>Resolution Rules:</b>
    /// <list type="number">
    ///   <item>
    ///     <b>StoryOverview type:</b> Auto-resolves to the single instance. No picker needed
    ///     because exactly one StoryOverview exists per story.
    ///   </item>
    ///   <item>
    ///     <b>Other types with 0 matching elements + CreateIfMissing=true:</b> Returns null.
    ///     Caller invokes ElementPicker to create a new element of that type.
    ///   </item>
    ///   <item>
    ///     <b>Other types with exactly 1 matching element:</b> Returns null.
    ///     Caller shows ElementPicker with that element preselected, but user can create a new one.
    ///   </item>
    ///   <item>
    ///     <b>Other types with 2+ matching elements:</b> Returns null.
    ///     Caller shows ElementPicker for user selection (or create new).
    ///   </item>
    ///   <item>
    ///     <b>Referenced elements</b> (e.g., "Problem.Protagonist" where Problem has a
    ///     Protagonist GUID property pointing to a Character): Returns null.
    ///     Caller shows ElementPicker with the referenced element preselected,
    ///     but user can pick different or create new.
    ///   </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <b>Return Value Semantics:</b>
    /// <list type="bullet">
    ///   <item>Non-null StoryElement = auto-resolved (only for StoryOverview)</item>
    ///   <item>Null = needs picker (caller must invoke ElementPicker)</item>
    /// </list>
    /// </para>
    /// </summary>
    public class ElementResolver
    {
        private readonly IStoryCADAPI _api;
        private readonly ILogger<ElementResolver>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementResolver"/> class.
        /// </summary>
        /// <param name="api">The StoryCAD API for accessing story elements.</param>
        /// <param name="logger">Optional logger for diagnostic output.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="api"/> is null.</exception>
        public ElementResolver(IStoryCADAPI api, ILogger<ElementResolver>? logger = null)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _logger = logger;
        }

        /// <summary>
        /// Gets all story elements matching an <see cref="ElementRequirement"/>'s ElementType.
        /// </summary>
        /// <param name="requirement">The requirement specifying the ElementType to match.</param>
        /// <returns>
        /// An enumerable of matching StoryElements. Returns empty if no matches or API error.
        /// </returns>
        public IEnumerable<StoryElement> GetMatchingElements(ElementRequirement requirement)
        {
            _logger?.LogDebug("GetMatchingElements: ElementType={Type}, Label={Label}",
                requirement.ElementType, requirement.ElementLabel);

            var result = _api.GetElementsByType(requirement.ElementType);
            if (!result.IsSuccess || result.Payload == null)
            {
                _logger?.LogDebug("GetMatchingElements: API returned no results for {Type}", requirement.ElementType);
                return Enumerable.Empty<StoryElement>();
            }

            var elements = result.Payload.ToList();
            _logger?.LogDebug("GetMatchingElements: Found {Count} elements of type {Type}",
                elements.Count, requirement.ElementType);
            return elements;
        }

        /// <summary>
        /// Gets all story elements matching an <see cref="ElementOutput"/>'s ElementType.
        /// </summary>
        /// <param name="output">The output specifying the ElementType to match.</param>
        /// <returns>
        /// An enumerable of matching StoryElements. Returns empty if no matches or API error.
        /// </returns>
        public IEnumerable<StoryElement> GetMatchingElements(ElementOutput output)
        {
            var result = _api.GetElementsByType(output.ElementType);
            if (!result.IsSuccess || result.Payload == null)
                return Enumerable.Empty<StoryElement>();

            return result.Payload;
        }

        /// <summary>
        /// Attempts to auto-resolve an <see cref="ElementRequirement"/> to a <see cref="StoryElement"/>.
        /// </summary>
        /// <param name="requirement">The requirement to resolve.</param>
        /// <param name="alreadyResolved">
        /// Dictionary of elements already resolved in this workflow execution, keyed by ElementLabel.
        /// Used to resolve referenced elements (e.g., "Problem.Protagonist").
        /// </param>
        /// <returns>
        /// The auto-resolved StoryElement for singleton types (StoryOverview only), or null
        /// if user interaction via ElementPicker is required.
        /// </returns>
        public StoryElement? ResolveRequirement(
            ElementRequirement requirement,
            Dictionary<string, StoryElement>? alreadyResolved = null)
        {
            _logger?.LogDebug("ResolveRequirement: Label={Label}, Type={Type}, Reference={Ref}",
                requirement.ElementLabel, requirement.ElementType, requirement.ReferencedElementLabel ?? "(none)");

            // Handle referenced elements (e.g., "Problem.Protagonist")
            if (!string.IsNullOrEmpty(requirement.ReferencedElementLabel))
            {
                _logger?.LogDebug("ResolveRequirement: Has reference '{Ref}' - returning null (caller uses GetReferencedElement)",
                    requirement.ReferencedElementLabel);
                // Referenced elements always return null - picker shows with preselection
                return null;
            }

            // Only StoryOverview auto-resolves
            if (requirement.ElementType == StoryItemType.StoryOverview)
            {
                var overview = GetMatchingElements(requirement).FirstOrDefault();
                _logger?.LogDebug("ResolveRequirement: StoryOverview auto-resolve -> {Result}",
                    overview?.Name ?? "(null)");
                return overview;
            }

            _logger?.LogDebug("ResolveRequirement: Type {Type} is not singleton - returning null (needs picker)",
                requirement.ElementType);
            // All other types return null - caller must show picker
            return null;
        }

        /// <summary>
        /// Attempts to auto-resolve an <see cref="ElementOutput"/> to a <see cref="StoryElement"/>.
        /// </summary>
        /// <param name="output">The output specification to resolve.</param>
        /// <param name="alreadyResolved">
        /// Dictionary of elements already resolved in this workflow execution, keyed by ElementLabel.
        /// </param>
        /// <returns>
        /// The auto-resolved StoryElement for singleton types when CreateNew is false,
        /// or null if user interaction is required.
        /// </returns>
        public StoryElement? ResolveOutput(
            ElementOutput output,
            Dictionary<string, StoryElement>? alreadyResolved = null)
        {
            // Check if this label was already resolved as an input
            if (alreadyResolved != null && alreadyResolved.TryGetValue(output.ElementLabel, out var resolved))
            {
                return resolved;
            }

            // Only StoryOverview auto-resolves
            if (output.ElementType == StoryItemType.StoryOverview)
            {
                return GetMatchingElements(output).FirstOrDefault();
            }

            // All other types return null - picker needed
            return null;
        }

        /// <summary>
        /// Resolves all input requirements for a <see cref="WorkflowIO"/>, returning
        /// only the auto-resolvable elements keyed by their ElementLabel.
        /// </summary>
        /// <param name="workflowIO">The workflow I/O specification.</param>
        /// <returns>
        /// A dictionary mapping ElementLabel to auto-resolved StoryElement.
        /// Only contains entries for singleton types (StoryOverview).
        /// Elements requiring picker interaction are NOT included.
        /// </returns>
        public Dictionary<string, StoryElement> ResolveAllInputs(WorkflowIO workflowIO)
        {
            var result = new Dictionary<string, StoryElement>();

            // Process required inputs first
            foreach (var req in workflowIO.RequiredInputs)
            {
                var element = ResolveRequirement(req, result);
                if (element != null)
                {
                    result[req.ElementLabel] = element;
                }
            }

            // Then optional inputs
            foreach (var opt in workflowIO.OptionalInputs)
            {
                if (result.ContainsKey(opt.ElementLabel))
                    continue;

                var element = ResolveRequirement(opt, result);
                if (element != null)
                {
                    result[opt.ElementLabel] = element;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the preselection hint for a referenced element.
        /// </summary>
        /// <param name="requirement">The requirement with a ReferencedElementLabel.</param>
        /// <param name="alreadyResolved">Dictionary of already-resolved elements.</param>
        /// <returns>
        /// The referenced StoryElement if found (for preselection in picker), or null.
        /// </returns>
        public StoryElement? GetReferencedElement(
            ElementRequirement requirement,
            Dictionary<string, StoryElement>? alreadyResolved)
        {
            _logger?.LogDebug("GetReferencedElement: Label={Label}, Reference={Ref}",
                requirement.ElementLabel, requirement.ReferencedElementLabel ?? "(none)");

            if (string.IsNullOrEmpty(requirement.ReferencedElementLabel))
            {
                _logger?.LogDebug("GetReferencedElement: No reference - returning null");
                return null;
            }

            var result = ResolveReference(requirement.ReferencedElementLabel, alreadyResolved);
            _logger?.LogDebug("GetReferencedElement: Resolved '{Ref}' -> {Result}",
                requirement.ReferencedElementLabel, result?.Name ?? "(null)");
            return result;
        }

        /// <summary>
        /// Resolves a reference like "Problem.Protagonist" to the actual element.
        /// </summary>
        private StoryElement? ResolveReference(
            string reference,
            Dictionary<string, StoryElement>? resolved)
        {
            _logger?.LogDebug("ResolveReference: Parsing '{Reference}'", reference);

            // Parse "Problem.Protagonist" into ("Problem", "Protagonist")
            var parts = reference.Split('.');
            if (parts.Length != 2)
            {
                _logger?.LogDebug("ResolveReference: Invalid format (expected 'Parent.Property') - returning null");
                return null;
            }

            var parentLabel = parts[0];
            var propertyName = parts[1];
            _logger?.LogDebug("ResolveReference: Parent={Parent}, Property={Property}", parentLabel, propertyName);

            // Get parent element from already-resolved elements
            if (resolved == null || !resolved.TryGetValue(parentLabel, out var parent))
            {
                _logger?.LogDebug("ResolveReference: Parent '{Parent}' not found in resolved elements", parentLabel);
                return null;
            }

            _logger?.LogDebug("ResolveReference: Found parent '{Parent}' = {Name} ({Type})",
                parentLabel, parent.Name, parent.GetType().Name);

            // Read GUID property from parent element
            var prop = parent.GetType().GetProperty(propertyName);
            if (prop == null)
            {
                _logger?.LogDebug("ResolveReference: Property '{Property}' not found on {Type}",
                    propertyName, parent.GetType().Name);
                return null;
            }

            var value = prop.GetValue(parent);
            if (value is not Guid guid || guid == Guid.Empty)
            {
                _logger?.LogDebug("ResolveReference: Property '{Property}' is not a valid GUID (value={Value})",
                    propertyName, value);
                return null;
            }

            _logger?.LogDebug("ResolveReference: Property '{Property}' = {Guid}", propertyName, guid);

            // Look up the referenced element via API
            var result = _api.GetStoryElement(guid);
            if (result.IsSuccess && result.Payload != null)
            {
                _logger?.LogDebug("ResolveReference: API lookup success -> {Name}", result.Payload.Name);
                return result.Payload;
            }

            _logger?.LogDebug("ResolveReference: API lookup failed for GUID {Guid}", guid);
            return null;
        }
    }
}
