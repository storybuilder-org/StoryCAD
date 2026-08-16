using System;
using System.Collections.Generic;

namespace StoryCADLib.Collaborator.Models;

/// <summary>
/// Payload passed to the Outline gaps page (issue #107 phase 6).
/// Built in CollaboratorLib; displayed in StoryCADLib UI.
/// </summary>
public sealed class GapWorkflowPayload
{
    public IReadOnlyList<GapElementGroup> Groups { get; set; } = Array.Empty<GapElementGroup>();

    public string GuessSentence { get; set; } = string.Empty;
}

public sealed class GapElementGroup
{
    public Guid ElementGuid { get; set; }
    public string ElementName { get; set; } = string.Empty;
    public string ElementTypeLabel { get; set; } = string.Empty;

    /// <summary>One link per missing required field (option 2: field → workflow or element).</summary>
    public IReadOnlyList<GapFieldLink> MissingFields { get; set; } = Array.Empty<GapFieldLink>();
}

/// <summary>
/// A single empty required field. Click opens its helper workflow, or the host element if none.
/// </summary>
public sealed class GapFieldLink
{
    public string DisplayLabel { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public Guid ElementGuid { get; set; }

    /// <summary>Collaborator workflow label, or empty when only StoryCAD edit applies.</summary>
    public string WorkflowLabel { get; set; } = string.Empty;

    /// <summary>Workflow display title when <see cref="WorkflowLabel"/> is set.</summary>
    public string WorkflowTitle { get; set; } = string.Empty;

    public bool OpensElementOnly => string.IsNullOrEmpty(WorkflowLabel);

    /// <summary>Shown under the field name: "via GMC" or "edit in StoryCAD".</summary>
    public string ActionHint
    {
        get
        {
            if (OpensElementOnly)
                return "edit in StoryCAD";
            return string.IsNullOrEmpty(WorkflowTitle) ? WorkflowLabel : $"via {WorkflowTitle}";
        }
    }
}
