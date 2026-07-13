using System.Text.Json.Serialization;

namespace StoryCADLib.Models;

/// <summary>
///     A picture attached to a story element (Character, Setting, or Scene).
///     The image is embedded in the .stbx outline as Base64, so the outline
///     stays self-contained and portable. Imports are capped at a viewing
///     resolution by <see cref="Services.ImageService"/> (large images are
///     downscaled and re-encoded as WebP) to keep outlines small.
///     Modeled on <see cref="RelationshipModel"/>: a plain serializable POCO
///     held in a List on the owning element and serialized inline.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class StoryImage
{
    #region Properties

    /// <summary>Stable identifier for this image within its element.</summary>
    [JsonInclude]
    [JsonPropertyName("Id")]
    public Guid Id { get; set; }

    /// <summary>
    ///     Base64 of the stored image bytes (capped at viewing resolution on
    ///     import; see <see cref="Services.ImageService"/>).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("ImageData")]
    public string ImageData { get; set; }

    /// <summary>MIME content type of the stored image, e.g. "image/png", "image/jpeg".</summary>
    [JsonInclude]
    [JsonPropertyName("ContentType")]
    public string ContentType { get; set; }

    /// <summary>Original file name of the picked image (informational).</summary>
    [JsonInclude]
    [JsonPropertyName("FileName")]
    public string FileName { get; set; }

    /// <summary>User-supplied caption (e.g. the actor's name for a character).</summary>
    [JsonInclude]
    [JsonPropertyName("Caption")]
    public string Caption { get; set; }

    #endregion

    #region Constructors

    public StoryImage(Guid id, string imageData, string contentType, string fileName)
    {
        Id = id;
        ImageData = imageData;
        ContentType = contentType;
        FileName = fileName;
        Caption = string.Empty;
    }

    // Required for deserialization. Don't remove.
    public StoryImage() { }

    #endregion
}
