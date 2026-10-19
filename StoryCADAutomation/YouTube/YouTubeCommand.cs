using System.Diagnostics;
using System.Net;

namespace StoryCADAutomation.YouTube;

/// <summary>
///     `youtube auth` and `youtube upload &lt;video.mp4&gt; --meta &lt;meta.json&gt;` (#1626). The
///     client file and the saved token live in %USERPROFILE%\.storycad-youtube\. No client
///     secret, access token or refresh token is ever written to the console.
/// </summary>
internal static class YouTubeCommand
{
    // Exit codes as in Program: 0 done, 3 environment or network failure, 64 usage error.
    private const int Passed = 0;
    private const int EnvironmentFailure = 3;
    private const int UsageError = 64;

    internal const string ClientFileName = "client_secret.json";
    internal const string TokenFileName = "token.json";

    private static readonly TimeSpan ConsentTimeout = TimeSpan.FromMinutes(5);

    /// <summary>%USERPROFILE%\.storycad-youtube</summary>
    internal static string DefaultFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".storycad-youtube");

    /// <summary>`youtube auth`: the one-time consent in the browser; saves token.json.</summary>
    public static int Auth()
    {
        var folder = DefaultFolder;
        if (LoadClient(folder) is not { } client)
        {
            return EnvironmentFailure;
        }

        var redirectUri = $"http://127.0.0.1:{YouTubeOAuth.FreePort()}/";
        var verifier = YouTubeOAuth.NewVerifier();
        var state = YouTubeOAuth.NewVerifier();
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        try
        {
            listener.Start();
            var url = YouTubeOAuth.ConsentUrl(client, redirectUri, YouTubeOAuth.Challenge(verifier), state);
            // The address holds the client id and the PKCE challenge, neither of them secret.
            Console.WriteLine("youtube: sign in and choose the StoryCAD channel in the browser. If no browser opens, go to:");
            Console.WriteLine(url);
            OpenBrowser(url);
            var code = YouTubeOAuth.WaitForCodeAsync(listener, state, ConsentTimeout).GetAwaiter().GetResult();
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            return FinishAuthAsync(client, code, verifier, redirectUri, folder, http, Console.WriteLine)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is HttpListenerException or HttpRequestException or TaskCanceledException
                                       or TimeoutException or InvalidOperationException or IOException
                                       or System.Text.Json.JsonException or KeyNotFoundException)
        {
            Console.Error.WriteLine($"youtube auth failed: {ex.Message}");
            return EnvironmentFailure;
        }
    }

    /// <summary>
    ///     The part of `youtube auth` after the browser: exchanges the code, saves the refresh
    ///     token, and prints the channel so the user sees which channel they authorized.
    /// </summary>
    internal static async Task<int> FinishAuthAsync(YouTubeClient client, string code, string verifier,
        string redirectUri, string folder, HttpClient http, Action<string> log)
    {
        var (access, refresh) = await YouTubeOAuth.ExchangeCodeAsync(client, code, verifier, redirectUri, http);
        var tokenPath = Path.Combine(folder, TokenFileName);
        YouTubeOAuth.SaveRefreshToken(tokenPath, refresh);
        log($"youtube: token saved in {tokenPath}");
        // An OAuth consent screen left in "Testing" gives refresh tokens that expire after 7 days.
        var channel = await YouTubeApi.ChannelAsync(http, access);
        log(channel is { } c
            ? $"youtube: authorized channel \"{c.Title}\" ({c.Id})"
            : "youtube: this Google account has no YouTube channel; uploads will fail.");
        return Passed;
    }

    /// <summary>`youtube upload`: checks meta.json, then uploads the video with it.</summary>
    public static int Upload(string videoPath, string metaPath)
    {
        // Every check runs before any network call, and all errors print together.
        var meta = YouTubeMeta.Load(metaPath, out var errors);
        var problems = errors.ToList();
        if (!File.Exists(videoPath))
        {
            problems.Insert(0, $"video not found: {videoPath}");
        }

        if (problems.Count > 0)
        {
            foreach (var problem in problems)
            {
                Console.Error.WriteLine($"youtube: {problem}");
            }

            return UsageError;
        }

        var folder = DefaultFolder;
        if (LoadClient(folder) is not { } client)
        {
            return EnvironmentFailure;
        }

        var tokenPath = Path.Combine(folder, TokenFileName);
        if (!File.Exists(tokenPath))
        {
            Console.Error.WriteLine($"youtube: no {tokenPath}; run `StoryCADAutomation youtube auth` first.");
            return EnvironmentFailure;
        }

        try
        {
            var refreshToken = YouTubeOAuth.LoadRefreshToken(tokenPath);
            // The PUT carries the whole file, so the timeout allows for a slow uplink.
            using var http = new HttpClient { Timeout = TimeSpan.FromHours(2) };
            return UploadAsync(videoPath, meta!, client, refreshToken, http, Console.WriteLine, Console.Error.WriteLine)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException
                                       or InvalidDataException or System.Text.Json.JsonException
                                       or KeyNotFoundException or InvalidOperationException)
        {
            Console.Error.WriteLine($"youtube upload failed: {ex.Message}");
            return EnvironmentFailure;
        }
    }

    /// <summary>
    ///     Refreshes the access token, uploads, then runs the playlist, captions and thumbnail
    ///     steps. A failed later step is reported and gives exit 3, but the video id still prints,
    ///     because the video is on YouTube by then.
    /// </summary>
    internal static async Task<int> UploadAsync(string videoPath, YouTubeMeta meta, YouTubeClient client,
        string refreshToken, HttpClient http, Action<string> log, Action<string> error)
    {
        var access = await YouTubeOAuth.RefreshAsync(client, refreshToken, http);
        var megabytes = new FileInfo(videoPath).Length / (1024.0 * 1024.0);
        log($"youtube: uploading {Path.GetFileName(videoPath)} ({megabytes:0.0} MB) as {meta.Privacy}");
        var (videoId, privacy) = await YouTubeApi.UploadVideoAsync(http, access, videoPath, meta);
        if (privacy is not null && privacy != meta.Privacy)
        {
            // YouTube locks videos.insert uploads to private for API projects that have not passed its audit.
            error($"youtube: YouTube set the video to {privacy}, not {meta.Privacy}; change it in YouTube Studio.");
        }

        var result = Passed;
        var steps = new List<(string Name, Func<Task> Run)>();
        if (meta.PlaylistId is { } playlistId)
        {
            steps.Add(("playlist", () => YouTubeApi.AddToPlaylistAsync(http, access, playlistId, videoId)));
        }

        if (meta.CaptionsPath is { } captions)
        {
            steps.Add(("captions", () => YouTubeApi.InsertCaptionsAsync(http, access, videoId, captions)));
        }

        if (meta.ThumbnailPath is { } thumbnail)
        {
            steps.Add(("thumbnail", () => YouTubeApi.SetThumbnailAsync(http, access, videoId, thumbnail)));
        }

        foreach (var (name, run) in steps)
        {
            try
            {
                await run();
                log($"youtube: {name} done");
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                error($"youtube: {name} failed: {ex.Message}");
                result = EnvironmentFailure;
            }
        }

        log($"youtube: video id {videoId}");
        log($"youtube: https://youtu.be/{videoId}");
        return result;
    }

    /// <summary>The OAuth client from client_secret.json, or null after printing why not.</summary>
    private static YouTubeClient? LoadClient(string folder)
    {
        var path = Path.Combine(folder, ClientFileName);
        if (!File.Exists(path))
        {
            Console.Error.WriteLine(
                $"youtube: no {path}; download the Desktop OAuth client file from Google Cloud Console and save it there.");
            return null;
        }

        try
        {
            return YouTubeOAuth.LoadClient(path);
        }
        catch (Exception ex) when (ex is InvalidDataException or System.Text.Json.JsonException or IOException)
        {
            Console.Error.WriteLine($"youtube: {ex.Message}");
            return null;
        }
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // The address is already on the console for the user to open by hand.
        }
    }
}
