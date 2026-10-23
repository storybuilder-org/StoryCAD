using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace StoryCADAutomation.YouTube;

/// <summary>
///     `youtube upload &lt;video.mp4&gt; --meta &lt;meta.json&gt;` (#1626). Each run signs in through
///     the browser, keeps the access token in memory only, and revokes it when the run ends,
///     after a failure too. No token is saved; only the client file lives in
///     %USERPROFILE%\.storycad-youtube\ (Terry, 2026-10-23). No client secret or access token is
///     ever written to the console.
/// </summary>
internal static class YouTubeCommand
{
    // Exit codes as in Program: 0 done, 3 environment or network failure, 64 usage error.
    private const int Passed = 0;
    private const int EnvironmentFailure = 3;
    private const int UsageError = 64;

    internal const string ClientFileName = "client_secret.json";

    private static readonly TimeSpan ConsentTimeout = TimeSpan.FromMinutes(5);

    /// <summary>%USERPROFILE%\.storycad-youtube</summary>
    internal static string DefaultFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".storycad-youtube");

    /// <summary>`youtube upload`: checks meta.json, signs in through the browser, then uploads the video with it.</summary>
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

        if (LoadClient(DefaultFolder) is not { } client)
        {
            return EnvironmentFailure;
        }

        var verifier = YouTubeOAuth.NewVerifier();
        var state = YouTubeOAuth.NewVerifier();
        HttpListener? listener = null;
        try
        {
            listener = StartListener(YouTubeOAuth.FreePort(), out var redirectUri);
            var url = YouTubeOAuth.ConsentUrl(client, redirectUri, YouTubeOAuth.Challenge(verifier), state);
            // The address holds the client id and the PKCE challenge, neither of them secret.
            Console.WriteLine("youtube: sign in and choose the StoryCAD channel in the browser. If no browser opens, go to:");
            Console.WriteLine(url);
            OpenBrowser(url);
            var code = YouTubeOAuth.WaitForCodeAsync(listener, state, ConsentTimeout).GetAwaiter().GetResult();
            listener.Close();
            // The PUT carries the whole file, so the timeout allows for a slow uplink.
            using var http = new HttpClient { Timeout = TimeSpan.FromHours(2) };
            var access = YouTubeOAuth.ExchangeCodeAsync(client, code, verifier, redirectUri, http).GetAwaiter().GetResult();
            return UploadAsync(videoPath, meta!, access, http, Console.WriteLine, Console.Error.WriteLine)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is HttpListenerException or HttpRequestException or TaskCanceledException
                                       or TimeoutException or InvalidOperationException or IOException
                                       or InvalidDataException or JsonException or KeyNotFoundException)
        {
            Console.Error.WriteLine($"youtube upload failed: {ex.Message}");
            return EnvironmentFailure;
        }
        finally
        {
            listener?.Close();
        }
    }

    /// <summary>Starts the redirect listener on 127.0.0.1, or on localhost when that is refused.</summary>
    private static HttpListener StartListener(int port, out string redirectUri)
    {
        redirectUri = $"http://127.0.0.1:{port}/";
        var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        try
        {
            listener.Start();
            return listener;
        }
        catch (HttpListenerException)
        {
            // HTTP.sys can refuse a 127.0.0.1 prefix to a user without a URL reservation (netsh http add urlacl);
            // it lets any user listen on localhost. Google accepts both as a Desktop client redirect.
            listener.Close();
        }

        redirectUri = $"http://localhost:{port}/";
        listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        try
        {
            listener.Start();
            return listener;
        }
        catch
        {
            listener.Close();
            throw;
        }
    }

    /// <summary>
    ///     With the signed-in access token: prints the channel, uploads, prints the video id and
    ///     link, then runs the playlist, captions and thumbnail steps. A failed later step is
    ///     reported and gives exit 3; the id is already printed, because the video is on YouTube
    ///     by then. The token is revoked at the end, also when a step throws.
    /// </summary>
    internal static async Task<int> UploadAsync(string videoPath, YouTubeMeta meta, string access, HttpClient http,
        Action<string> log, Action<string> error)
    {
        try
        {
            return await UploadWithTokenAsync(videoPath, meta, access, http, log, error);
        }
        finally
        {
            try
            {
                await YouTubeOAuth.RevokeAsync(access, http);
                log("youtube: sign-in revoked");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // The upload result stands; an access token stops working by itself within an hour.
                error($"youtube: warning: the revoke failed, so the sign-in stays valid for up to an hour: {ex.Message}");
            }
        }
    }

    private static async Task<int> UploadWithTokenAsync(string videoPath, YouTubeMeta meta, string access,
        HttpClient http, Action<string> log, Action<string> error)
    {
        // The channel the user picked in the browser, so a wrong choice shows before the upload.
        try
        {
            var channel = await YouTubeApi.ChannelAsync(http, access);
            log(channel is { } c
                ? $"youtube: signed in to channel \"{c.Title}\" ({c.Id})"
                : "youtube: this Google account has no YouTube channel; the upload will fail.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException
                                       or InvalidOperationException)
        {
            error($"youtube: warning: the channel lookup failed: {ex.Message}");
        }

        var megabytes = new FileInfo(videoPath).Length / (1024.0 * 1024.0);
        log($"youtube: uploading {Path.GetFileName(videoPath)} ({megabytes:0.0} MB) as {meta.Privacy}");
        var (videoId, privacy) = await YouTubeApi.UploadVideoAsync(http, access, videoPath, meta);
        log($"youtube: video id {videoId}");
        log($"youtube: https://youtu.be/{videoId}");
        if (privacy is not null && privacy != meta.Privacy)
        {
            // YouTube locks videos.insert uploads to private for API projects that have not passed its audit.
            error($"youtube: YouTube set the video to {privacy}, not {meta.Privacy}; change it in YouTube Studio.");
        }

        // An access token lasts about an hour and there is no refresh token, so a step after an
        // upload longer than that fails with 401 and is reported like any failed step.
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

        var result = Passed;
        foreach (var (name, run) in steps)
        {
            try
            {
                await run();
                log($"youtube: {name} done");
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException
                                           or JsonException)
            {
                error($"youtube: {name} failed: {ex.Message}");
                result = EnvironmentFailure;
            }
        }

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
        catch (Exception ex) when (ex is InvalidDataException or JsonException or IOException)
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
