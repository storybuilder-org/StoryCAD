using System.Text.Json.Serialization;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Models;

/// <summary>
///     Beatsheet model that is able to be saved/loaded from a file.
/// </summary>
public class SavedBeatsheet
{
    /// <summary>
    ///     Descritpion of beatsheet.
    /// </summary>
    [JsonInclude]
    public string Description { get; set; }

    /// <summary>
    ///     Story beats
    /// </summary>
    [JsonInclude]
    public List<StructureBeatViewModel> Beats { get; set; }
}
