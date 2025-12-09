using System.Text.Json;
using System.Text.Json.Serialization;

namespace StoryCADLib.DAL;

/// <summary>
///     Custom converter to handle empty GUID strings.
/// </summary>
public class EmptyGuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) { return Guid.Empty; }

        var guidString = reader.GetString();
        if (string.IsNullOrWhiteSpace(guidString)) { return Guid.Empty; }

        return Guid.TryParse(guidString, out var guid) 
            ? guid 
            : throw new JsonException($"Unable to convert \"{guidString}\" to Guid.");
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value == Guid.Empty ? "" : value.ToString());
    }
}
