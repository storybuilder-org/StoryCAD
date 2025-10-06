using System.Text.Json.Serialization;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Models;

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
