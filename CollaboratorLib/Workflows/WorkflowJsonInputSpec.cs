using System;
using System.Collections.Generic;

namespace StoryCollaborator.Workflows;

/// <summary>
/// Issue #260: the property lists a migrated workflow's "input" object writes. Set on
/// <see cref="Workflow.JsonInput"/>; null means the workflow sends no "input" field yet and
/// Elements and Args stay its only carriers until it migrates.
///
/// A label-to-property-list map, not fixed fields: each workflow's own shape method in
/// <see cref="StoryCollaborator.WorkflowInputBuilder"/> looks its lists up by a key it chooses
/// (for example "Target" for the workflow's own gathered element, "ResolvedCharacter" for a
/// two-property character reference). A key is scoped to one workflow's spec -- the same key
/// name in two different <see cref="WorkflowJsonInputSpec"/> instances may hold two different
/// property lists. Replaces four fixed fields (TargetProperties, RelatedProblemProperties,
/// ResolvedCharacterProperties, RelatedSceneProperties) because SceneBuilder needs two
/// different character projections and StoryProblem needs a seven-property one.
/// </summary>
public sealed class WorkflowJsonInputSpec
{
    /// <summary>Property lists keyed by the name a workflow's shape method assigns them.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> PropertyLists { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

    /// <summary>The named property list, or an empty list when this spec declares none under that key.</summary>
    public IReadOnlyList<string> this[string key] =>
        PropertyLists.TryGetValue(key, out var list) ? list : Array.Empty<string>();
}
