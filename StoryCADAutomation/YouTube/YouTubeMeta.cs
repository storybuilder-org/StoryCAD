using System.Text;
using System.Text.Json;

namespace StoryCADAutomation.YouTube;

/// <summary>
///     The meta.json file for `youtube upload` (#1626): title, description, tags, category,
///     privacy, made-for-kids, and the optional playlist, captions and thumbnail. Paths in the
///     file resolve against the file's own folder.
/// </summary>
public sealed record YouTubeMeta
{
    public const int MaxTitleLength = 100;
    public const int MaxDescriptionBytes = 5000;

    // YouTube counts the commas between tags, and 2 more for the quotes around a tag with a space.
    public const int MaxTagsLength = 500;

    // thumbnails.set accepts JPEG or PNG up to 2 MB.
    public const long MaxThumbnailBytes = 2 * 1024 * 1024;

    public const string DefaultCategoryId = "28"; // Science & Technology

    // Unlisted by default: the #1626 checklist uploads Unlisted, checks Content > Claims, then publishes.
    public const string DefaultPrivacy = "unlisted";

    internal static readonly string[] Privacies = { "private", "unlisted", "public" };

    private static readonly string[] Fields =
        { "title", "description", "tags", "categoryId", "privacy", "madeForKids", "playlistId", "captions", "thumbnail" };

    public required string Title { get; init; }
    public string Description { get; init; } = "";
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public string CategoryId { get; init; } = DefaultCategoryId;
    public string Privacy { get; init; } = DefaultPrivacy;
    public bool MadeForKids { get; init; }
    public string? PlaylistId { get; init; }

    /// <summary>Full path of the .srt or .vtt file, or null.</summary>
    public string? CaptionsPath { get; init; }

    /// <summary>Full path of the thumbnail image, or null.</summary>
    public string? ThumbnailPath { get; init; }

    /// <summary>
    ///     Reads and checks a meta.json file. Returns null and every error found when the file
    ///     is not valid, so the user fixes them all in one pass.
    /// </summary>
    public static YouTubeMeta? Load(string metaPath, out IReadOnlyList<string> errors)
    {
        var list = new List<string>();
        errors = list;
        if (!File.Exists(metaPath))
        {
            list.Add($"meta file not found: {metaPath}");
            return null;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(metaPath),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (JsonException ex)
        {
            list.Add($"{metaPath} is not valid JSON: {ex.Message}");
            return null;
        }

        using (document)
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(metaPath))!;
            return Parse(document.RootElement, folder, list);
        }
    }

    /// <summary>Checks the JSON object; relative paths resolve against <paramref name="folder" />.</summary>
    internal static YouTubeMeta? Parse(JsonElement root, string folder, List<string> errors)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            errors.Add("meta.json must be a JSON object.");
            return null;
        }

        // A misspelled field (say "privacyStatus") would otherwise fall back to its default unseen.
        foreach (var property in root.EnumerateObject().Where(p => !Fields.Contains(p.Name, StringComparer.Ordinal)))
        {
            errors.Add($"unknown field '{property.Name}'; known fields: {string.Join(", ", Fields)}.");
        }

        var title = Text(root, "title", errors);
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add("title is required.");
        }
        else
        {
            if (title.Length > MaxTitleLength)
            {
                errors.Add($"title is {title.Length} characters; the limit is {MaxTitleLength}.");
            }

            if (title.IndexOfAny(new[] { '<', '>' }) >= 0)
            {
                errors.Add("title must not contain '<' or '>'.");
            }
        }

        var description = Text(root, "description", errors) ?? "";
        var descriptionBytes = Encoding.UTF8.GetByteCount(description);
        if (descriptionBytes > MaxDescriptionBytes)
        {
            errors.Add($"description is {descriptionBytes} bytes in UTF-8; the limit is {MaxDescriptionBytes}.");
        }

        if (description.IndexOfAny(new[] { '<', '>' }) >= 0)
        {
            errors.Add("description must not contain '<' or '>'.");
        }

        var tags = new List<string>();
        if (root.TryGetProperty("tags", out var tagsElement))
        {
            if (tagsElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add("tags must be an array of strings.");
            }
            else
            {
                foreach (var tag in tagsElement.EnumerateArray())
                {
                    if (tag.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(tag.GetString()))
                    {
                        errors.Add("tags must be an array of non-empty strings.");
                        break;
                    }

                    tags.Add(tag.GetString()!);
                }

                if (TagsLength(tags) > MaxTagsLength)
                {
                    errors.Add($"tags come to {TagsLength(tags)} characters as YouTube counts them; the limit is {MaxTagsLength}.");
                }
            }
        }

        var categoryId = Text(root, "categoryId", errors) ?? DefaultCategoryId;
        if (categoryId.Length == 0 || !categoryId.All(char.IsAsciiDigit))
        {
            errors.Add($"categoryId must be a number in a string, such as \"{DefaultCategoryId}\".");
        }

        var privacy = Text(root, "privacy", errors) ?? DefaultPrivacy;
        if (!Privacies.Contains(privacy, StringComparer.Ordinal))
        {
            errors.Add($"privacy '{privacy}' is not one of {string.Join(", ", Privacies)}.");
        }

        var madeForKids = false;
        if (root.TryGetProperty("madeForKids", out var kids))
        {
            if (kids.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                madeForKids = kids.GetBoolean();
            }
            else
            {
                errors.Add("madeForKids must be true or false.");
            }
        }

        var playlistId = Text(root, "playlistId", errors);
        if (playlistId is not null && string.IsNullOrWhiteSpace(playlistId))
        {
            errors.Add("playlistId must not be empty; leave it out for no playlist.");
        }

        var captions = FilePath(root, "captions", folder, errors);
        if (captions is not null &&
            Path.GetExtension(captions).ToLowerInvariant() is not (".srt" or ".vtt"))
        {
            errors.Add($"captions must be an .srt or .vtt file: {captions}");
        }

        var thumbnail = FilePath(root, "thumbnail", folder, errors);
        if (thumbnail is not null)
        {
            if (ThumbnailContentType(thumbnail) is null)
            {
                errors.Add($"thumbnail must be a .jpg, .jpeg or .png file: {thumbnail}");
            }
            else if (File.Exists(thumbnail) && new FileInfo(thumbnail).Length > MaxThumbnailBytes)
            {
                errors.Add($"thumbnail is larger than 2 MB: {thumbnail}");
            }
        }

        if (errors.Count > 0)
        {
            return null;
        }

        return new YouTubeMeta
        {
            Title = title!,
            Description = description,
            Tags = tags,
            CategoryId = categoryId,
            Privacy = privacy,
            MadeForKids = madeForKids,
            PlaylistId = playlistId,
            CaptionsPath = captions,
            ThumbnailPath = thumbnail,
        };
    }

    /// <summary>The tags length as YouTube counts it: commas between tags, quotes around a tag with a space.</summary>
    internal static int TagsLength(IReadOnlyCollection<string> tags) =>
        tags.Sum(t => t.Length + (t.Contains(' ') ? 2 : 0)) + Math.Max(0, tags.Count - 1);

    /// <summary>The upload content type for a thumbnail file, or null when YouTube does not take it.</summary>
    internal static string? ThumbnailContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        _ => null,
    };

    /// <summary>The string value of a field, null when absent; an error when it is not a string.</summary>
    private static string? Text(JsonElement root, string name, List<string> errors)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add($"{name} must be a string.");
            return null;
        }

        return value.GetString();
    }

    /// <summary>A path field resolved against the meta.json folder; an error when the file is missing.</summary>
    private static string? FilePath(JsonElement root, string name, string folder, List<string> errors)
    {
        var value = Text(root, name, errors);
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{name} must not be empty; leave it out for none.");
            return null;
        }

        var path = Path.GetFullPath(Path.Combine(folder, value));
        if (!File.Exists(path))
        {
            errors.Add($"{name} file not found: {path}");
        }

        return path;
    }
}
