using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StoryCADAutomation.YouTube;

/// <summary>
///     The YouTube Data API v3 calls for #1626: the signed-in channel, the resumable video
///     upload, and the playlist, captions and thumbnail steps after it.
/// </summary>
public static class YouTubeApi
{
    internal const string ApiBase = "https://www.googleapis.com/youtube/v3/";
    internal const string UploadBase = "https://www.googleapis.com/upload/youtube/v3/";

    internal const string VideoInsertUrl = UploadBase + "videos?uploadType=resumable&part=snippet,status";

    /// <summary>The channel the token belongs to (a Brand Account has its own), or null when none.</summary>
    public static async Task<(string Title, string Id)?> ChannelAsync(HttpClient http, string accessToken,
        CancellationToken cancellation = default)
    {
        using var request = Request(HttpMethod.Get, ApiBase + "channels?part=snippet&mine=true", accessToken);
        using var json = await SendAsync(http, request, "channels.list", cancellation);
        if (!json.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
        {
            return null;
        }

        var channel = items[0];
        return (channel.GetProperty("snippet").GetProperty("title").GetString()!, channel.GetProperty("id").GetString()!);
    }

    /// <summary>The JSON body of videos.insert: snippet and status from meta.json.</summary>
    internal static object VideoResource(YouTubeMeta meta) => new
    {
        snippet = new { title = meta.Title, description = meta.Description, tags = meta.Tags, categoryId = meta.CategoryId },
        status = new { privacyStatus = meta.Privacy, selfDeclaredMadeForKids = meta.MadeForKids },
    };

    /// <summary>The upload content type for a video file.</summary>
    internal static string VideoContentType(string path) =>
        Path.GetExtension(path).Equals(".mp4", StringComparison.OrdinalIgnoreCase) ? "video/mp4" : "application/octet-stream";

    /// <summary>
    ///     Uploads the video with the resumable protocol: one POST with the metadata opens the
    ///     session, one PUT streams the file to the session address. Returns the video id and
    ///     the privacy status YouTube gave it.
    /// </summary>
    public static async Task<(string Id, string? Privacy)> UploadVideoAsync(HttpClient http, string accessToken,
        string videoPath, YouTubeMeta meta, CancellationToken cancellation = default)
    {
        var length = new FileInfo(videoPath).Length;
        var contentType = VideoContentType(videoPath);

        Uri session;
        using (var start = Request(HttpMethod.Post, VideoInsertUrl, accessToken))
        {
            start.Headers.Add("X-Upload-Content-Type", contentType);
            start.Headers.Add("X-Upload-Content-Length", length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            start.Content = JsonContent.Create(VideoResource(meta));
            using var response = await http.SendAsync(start, cancellation);
            await ThrowIfFailedAsync(response, "videos.insert", cancellation);
            session = response.Headers.Location ??
                      throw new HttpRequestException("videos.insert returned no upload address.");
        }

        // Streamed from disk; a video of a few hundred MB never sits in memory.
        await using var file = new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var put = Request(HttpMethod.Put, session.ToString(), accessToken);
        put.Content = new StreamContent(file, 1 << 20);
        put.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        put.Content.Headers.ContentLength = length;
        JsonDocument json;
        try
        {
            json = await SendAsync(http, put, "video upload", cancellation);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException &&
                                   !cancellation.IsCancellationRequested)
        {
            // The command has no resume (#1626); a stopped upload is sent again from the first byte.
            throw new HttpRequestException(
                $"the video upload stopped: {ex.Message} There is no resume; a re-run starts the upload over.", ex,
                (ex as HttpRequestException)?.StatusCode);
        }

        using var _ = json;
        var id = json.RootElement.GetProperty("id").GetString()!;
        var privacy = json.RootElement.TryGetProperty("status", out var status) &&
                      status.TryGetProperty("privacyStatus", out var p)
            ? p.GetString()
            : null;
        return (id, privacy);
    }

    /// <summary>playlistItems.insert: adds the video to the end of a playlist.</summary>
    public static async Task AddToPlaylistAsync(HttpClient http, string accessToken, string playlistId, string videoId,
        CancellationToken cancellation = default)
    {
        using var request = Request(HttpMethod.Post, ApiBase + "playlistItems?part=snippet", accessToken);
        request.Content = JsonContent.Create(new
        {
            snippet = new { playlistId, resourceId = new { kind = "youtube#video", videoId } },
        });
        using var _ = await SendAsync(http, request, "playlistItems.insert", cancellation);
    }

    /// <summary>captions.insert as a multipart upload: an English track with an empty name.</summary>
    public static async Task InsertCaptionsAsync(HttpClient http, string accessToken, string videoId, string captionsPath,
        CancellationToken cancellation = default)
    {
        using var request = Request(HttpMethod.Post, UploadBase + "captions?uploadType=multipart&part=snippet", accessToken);
        var content = new MultipartContent("related");
        content.Add(JsonContent.Create(new { snippet = new { videoId, language = "en", name = "" } }));
        var track = new ByteArrayContent(await File.ReadAllBytesAsync(captionsPath, cancellation));
        track.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(track);
        request.Content = content;
        using var _ = await SendAsync(http, request, "captions.insert", cancellation);
    }

    /// <summary>thumbnails.set: the custom thumbnail.</summary>
    public static async Task SetThumbnailAsync(HttpClient http, string accessToken, string videoId, string thumbnailPath,
        CancellationToken cancellation = default)
    {
        // YouTube takes custom thumbnails only from a phone-verified channel; otherwise this returns 403.
        using var request = Request(HttpMethod.Post,
            UploadBase + "thumbnails/set?videoId=" + Uri.EscapeDataString(videoId), accessToken);
        request.Content = new ByteArrayContent(await File.ReadAllBytesAsync(thumbnailPath, cancellation));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(YouTubeMeta.ThumbnailContentType(thumbnailPath)!);
        using var _ = await SendAsync(http, request, "thumbnails.set", cancellation);
    }

    /// <summary>
    ///     The useful part of a Google error body: error.message for the Data API,
    ///     error and error_description for the token endpoint; else the start of the body.
    /// </summary>
    internal static string ErrorDetail(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var message))
                {
                    return message.GetString() ?? "";
                }

                if (error.ValueKind == JsonValueKind.String)
                {
                    return root.TryGetProperty("error_description", out var description)
                        ? $"{error.GetString()}: {description.GetString()}"
                        : error.GetString()!;
                }
            }
        }
        catch (JsonException)
        {
        }

        return body.Length > 300 ? body[..300] + "..." : body;
    }

    private static HttpRequestMessage Request(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static async Task<JsonDocument> SendAsync(HttpClient http, HttpRequestMessage request, string what,
        CancellationToken cancellation)
    {
        using var response = await http.SendAsync(request, cancellation);
        await ThrowIfFailedAsync(response, what, cancellation);
        var body = await response.Content.ReadAsStringAsync(cancellation);
        return JsonDocument.Parse(body.Length == 0 ? "{}" : body);
    }

    private static async Task ThrowIfFailedAsync(HttpResponseMessage response, string what, CancellationToken cancellation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellation);
        throw new HttpRequestException($"{what} returned {(int)response.StatusCode}: {ErrorDetail(body)}", null,
            response.StatusCode);
    }
}
