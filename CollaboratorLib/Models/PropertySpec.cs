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
    /// Carries one extracted output value between ExtractOutputs and ApplyUpdates.
    /// Value type by WriteVia: Scalar=string, SimpleList=List&lt;string&gt;,
    /// BeatSheet=List&lt;BeatInfo&gt;, CastMembers=List&lt;Guid&gt;,
    /// Relationships=List&lt;RelationshipInfo&gt;, TypedList=null.
    /// </summary>
    public sealed record PendingUpdate(
        string ElementLabel,
        Guid ElementUuid,
        PropertySpec Spec,
        object? Value);

    /// <summary>
    /// One beat in a BeatSheet output.
    /// </summary>
    public sealed record BeatInfo(
        string Title,
        string Description,
        Guid? AssignedElement = null);

    /// <summary>
    /// One relationship entry in a Relationships output.
    /// </summary>
    public sealed record RelationshipInfo(
        Guid RecipientGuid,
        string Description,
        bool Mirror = false);
}
