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
        if (reader.TokenType == JsonTokenType.String)
        {
            var guidString = reader.GetString();
            if (string.IsNullOrWhiteSpace(guidString))
            {
                return Guid.Empty;
            }

            if (Guid.TryParse(guidString, out var guid))
            {
                return guid;
            }
        }
        else if (reader.TokenType == JsonTokenType.Null)
        {
            return Guid.Empty;
        }

        throw new JsonException($"Unable to convert \"{reader.GetString()}\" to {typeof(Guid)}.");
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        if (value == Guid.Empty)
        {
            writer.WriteStringValue(string.Empty);
        }
        else
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
