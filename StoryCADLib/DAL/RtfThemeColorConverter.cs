using System.Text.Json;
using System.Text.Json.Serialization;
using StoryCADLib.Services.Rtf;

namespace StoryCADLib.DAL;

/// <summary>
///     Removes theme text colors from each string that an outline reads or writes,
///     so that a theme color is never kept in an outline file (#1675).
/// </summary>
public class RtfThemeColorConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => RtfThemeColor.RemoveThemeColors(reader.GetString());

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(RtfThemeColor.RemoveThemeColors(value));
}
