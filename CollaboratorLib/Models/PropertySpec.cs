using System.Collections.Generic;
using System;

namespace StoryCollaborator.Models
{
    /// <summary>
    /// Describes how a workflow output property is written via the StoryCAD API.
    /// </summary>
    public enum WriteVia
    {
        Scalar,       // UpdateElementProperty — string or simple scalar
        SimpleList,   // List<string> via inline clear-then-AddCollectionEntry
        BeatSheet,    // ObservableCollection<StructureBeat> via the beat API
        TypedList,    // declared-only this issue; runner emits "not yet implemented" diagnostic
        CastMembers,  // List<Guid> via AddCastMember; recipient GUID chosen from injected CharacterChoices
        Relationships // List<RelationshipModel> via AddRelationship; recipient GUID from injected CharacterChoices
    }

    /// <summary>
    /// Typed descriptor for a single workflow input or output property.
    /// One construction path (no bare-string shortcut): use the primary constructor.
    /// When used in an input declaration, only <see cref="Property"/> is read;
    /// the output-only fields (<see cref="WriteVia"/>, <see cref="JsonKey"/>,
    /// <see cref="ScalarType"/>, <see cref="ListEntryType"/>) are ignored.
    /// </summary>
    public sealed record PropertySpec(
        string Property,
        WriteVia WriteVia = WriteVia.Scalar,
        string? JsonKey = null,
        Type? ScalarType = null,
        Type? ListEntryType = null);

    /// <summary>
    /// How a scalar pending update relates to the live outline field (issue #116).
    /// Non-scalar updates stay <see cref="Unclassified"/> and Accept All may apply them.
    /// </summary>
    public enum UpdateKind
    {
        /// <summary>Not classified (non-scalar or pre-classify).</summary>
        Unclassified = 0,
        /// <summary>Target empty — Accept All may fill.</summary>
        Fill,
        /// <summary>Collaborator wrote this field earlier this session — Accept All may refresh.</summary>
        Refresh,
        /// <summary>User-owned non-empty differs — Review Each only; Accept All skips.</summary>
        Protect,
        /// <summary>Proposed equals current — dropped from pending.</summary>
        NoOp
    }

    /// <summary>
    /// Carries one extracted output value between ExtractOutputs and ApplyUpdates.
    /// Value type by WriteVia: Scalar=string, SimpleList=List&lt;string&gt;,
    /// BeatSheet=List&lt;BeatInfo&gt;, CastMembers=List&lt;Guid&gt;,
    /// Relationships=List&lt;RelationshipInfo&gt;, TypedList=null.
    /// Optional classification fields are set by <c>ClassifyScalarUpdates</c> (#116).
    /// </summary>
    public sealed record PendingUpdate(
        string ElementLabel,
        Guid ElementUuid,
        PropertySpec Spec,
        object? Value,
        UpdateKind Kind = UpdateKind.Unclassified,
        string? CurrentDisplay = null,
        string? CraftExplanation = null)
    {
        public string Key => $"{ElementLabel}.{Spec.Property}";

        /// <summary>Stable session key: element UUID + property (survives label renames).</summary>
        public string SessionTouchKey => $"{ElementUuid:N}.{Spec.Property}";

        public bool AcceptAllMayApply =>
            Kind is UpdateKind.Fill or UpdateKind.Refresh or UpdateKind.Unclassified;
    }

    /// <summary>
    /// One beat in a BeatSheet output.
    /// SceneName (#150 BeatScenes): when set on an empty beat, create a Scene under the
    /// problem and assign it. Structure and other workflows leave SceneName null.
    /// </summary>
    public sealed record BeatInfo(
        string Title,
        string Description,
        Guid? AssignedElement = null,
        string? SceneName = null,
        string? SceneDescription = null,
        string? SceneNotes = null,
        string? SceneType = null,
        IReadOnlyList<Guid>? SceneCast = null);

    /// <summary>
    /// One relationship entry in a Relationships output.
    /// </summary>
    public sealed record RelationshipInfo(
        Guid RecipientGuid,
        string RelationType,
        bool Mirror = false,
        string Trait = "",
        string Attitude = "",
        string Notes = "");
}
