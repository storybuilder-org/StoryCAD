using System.Text.Json.Serialization;

namespace StoryCADLib.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StoryItemType
{
    StoryOverview,
    Problem,
    Character,
    Setting,
    Scene,
    Folder,
    Section,
    Web,
    Notes,
    TrashCan,
    Unknown
}
