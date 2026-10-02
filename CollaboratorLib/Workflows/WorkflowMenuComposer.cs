using System;
using System.Collections.Generic;
using System.Linq;
using StoryCADLib.Models;

namespace StoryCollaborator.Workflows
{
    /// <summary>
    /// One workflow row in the navigation pane.
    /// </summary>
    public sealed class WorkflowMenuItem
    {
        public WorkflowMenuItem(Workflow workflow, bool isStarred)
        {
            Workflow = workflow;
            IsStarred = isStarred;
        }

        public Workflow Workflow { get; }

        /// <summary>Drives the star glyph; starred rows live in the starred band.</summary>
        public bool IsStarred { get; }

        public string Title => Workflow.Title;

        public string Label => Workflow.Label;
    }

    /// <summary>
    /// One expandable group in the navigation pane.
    /// </summary>
    public sealed class WorkflowMenuBand
    {
        public WorkflowMenuBand(string title, bool isExpanded, IReadOnlyList<WorkflowMenuItem> items)
        {
            Title = title;
            IsExpanded = isExpanded;
            Items = items;
        }

        public string Title { get; }

        /// <summary>Starred band opens; catalog groups start closed so the pane stays short.</summary>
        public bool IsExpanded { get; }

        public IReadOnlyList<WorkflowMenuItem> Items { get; }
    }

    /// <summary>
    /// Builds the ordered bands of the Collaborator navigation pane from the registry and the
    /// user's starred labels. Pure — no UI types — so the ordering rules can be tested without
    /// a XamlRoot. The caller renders these bands as NavigationViewItems and prepends the
    /// outline-gaps entry.
    /// </summary>
    public static class WorkflowMenuComposer
    {
        /// <summary>Title of the band holding the user's starred workflows.</summary>
        public const string StarredBandTitle = "Starred";

        /// <summary>
        /// Returns the starred band (when any workflow is starred) followed by element-type
        /// groups holding everything else.
        /// </summary>
        /// <param name="workflows">Registry workflows, in registry order.</param>
        /// <param name="starredLabels">
        /// Labels the user has starred. Labels that match no workflow are ignored — a withdrawn
        /// workflow keeps its star in preferences without putting a dead row in the pane.
        /// </param>
        /// <param name="groupTitle">Maps an element type to its group header.</param>
        public static IReadOnlyList<WorkflowMenuBand> Compose(
            IEnumerable<Workflow> workflows,
            IEnumerable<string> starredLabels,
            Func<StoryItemType, string> groupTitle)
        {
            var all = workflows?.ToList() ?? new List<Workflow>();
            var starred = new HashSet<string>(
                starredLabels?.Where(l => !string.IsNullOrWhiteSpace(l)) ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);

            var bands = new List<WorkflowMenuBand>();

            // Starred first, in registry order rather than the order the user starred them: the
            // registry order is the craft order (idea, problem, structure, cast, scenes), which
            // is more use as a next-action list than a click history.
            var starredItems = all
                .Where(w => starred.Contains(w.Label))
                .Select(w => new WorkflowMenuItem(w, true))
                .ToList();

            if (starredItems.Count > 0)
            {
                bands.Add(new WorkflowMenuBand(StarredBandTitle, true, starredItems));
            }

            // The rest, grouped by element type. A starred workflow is not repeated here: two
            // rows would share one registry instance as their Tag, and RestoreSelection matches
            // by tag, so it would highlight whichever copy it reached first.
            var currentType = (StoryItemType?)null;
            var currentItems = new List<WorkflowMenuItem>();

            void FlushGroup()
            {
                if (currentType.HasValue && currentItems.Count > 0)
                {
                    bands.Add(new WorkflowMenuBand(groupTitle(currentType.Value), false, currentItems));
                }

                currentItems = new List<WorkflowMenuItem>();
            }

            foreach (var workflow in all)
            {
                if (currentType != workflow.PrimaryElementType)
                {
                    FlushGroup();
                    currentType = workflow.PrimaryElementType;
                }

                if (!starred.Contains(workflow.Label))
                {
                    currentItems.Add(new WorkflowMenuItem(workflow, false));
                }
            }

            FlushGroup();

            return bands;
        }
    }
}
