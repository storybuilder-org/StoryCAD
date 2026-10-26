using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace StoryCADAutomation.YouTube;

/// <summary>
///     `youtube upload &lt;video.mp4&gt; --meta &lt;meta.json&gt;` and
///     `youtube update &lt;video id&gt; --meta &lt;meta.json&gt;` (#1626). Each run signs in through
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

    /// <summary>The revoke gets this long, so a stalled network cannot hold the exit.</summary>
    internal static readonly TimeSpan RevokeTimeout = TimeSpan.FromSeconds(15);

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

        // The PUT carries the whole file, so the timeout allows for a slow uplink.
        return RunSignedIn("upload", TimeSpan.FromHours(2), (access, http, stop) =>
            UploadAsync(videoPath, meta!, access, http, Console.WriteLine, Console.Error.WriteLine, stop));
    }

    /// <summary>
    ///     `youtube update`: checks the video id and meta.json, signs in through the browser,
    ///     then replaces the video's title, description, tags and category with meta.json's.
    /// </summary>
    public static int Update(string videoId, string metaPath)
    {
        var meta = YouTubeMeta.Load(metaPath, out var errors);
        var problems = errors.ToList();
        if (!IsVideoId(videoId))
        {
            problems.Insert(0, $"'{videoId}' is not a video id; give the 11 characters after youtu.be/");
        }

        if (problems.Count > 0)
        {
            foreach (var problem in problems)
            {
                Console.Error.WriteLine($"youtube: {problem}");
            }

            return UsageError;
        }

        return RunSignedIn("update", TimeSpan.FromMinutes(2), (access, http, stop) =>
            UpdateAsync(videoId, meta!, access, http, Console.WriteLine, Console.Error.WriteLine, stop));
    }

    /// <summary>A YouTube video id: 11 letters, digits, '-' or '_'.</summary>
    internal static bool IsVideoId(string value) =>
        value.Length == 11 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    /// <summary>
    ///     Signs in through the browser, then runs <paramref name="work" /> with the access token.
    ///     Ctrl+C cancels the run instead of ending the process, so the revoke inside the work
    ///     still runs.
    /// </summary>
    private static int RunSignedIn(string verb, TimeSpan httpTimeout,
        Func<string, HttpClient, CancellationToken, Task<int>> work)
    {
        if (LoadClient(DefaultFolder) is not { } client)
        {
            return EnvironmentFailure;
        }

        var verifier = YouTubeOAuth.NewVerifier();
        var state = YouTubeOAuth.NewVerifier();
        HttpListener? listener = null;
        // Ctrl+C cancels the run instead of ending the process, so the revoke in UploadAsync still runs.
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            e.Cancel = true;
            stop.Cancel();
        };
        Console.CancelKeyPress += onCancel;
        try
        {
            listener = StartListener(YouTubeOAuth.FreePort(), out var redirectUri);
            var url = YouTubeOAuth.ConsentUrl(client, redirectUri, YouTubeOAuth.Challenge(verifier), state);
            // The address holds the client id and the PKCE challenge, neither of them secret.
            Console.WriteLine("youtube: sign in and choose the StoryCAD channel in the browser. If no browser opens, go to:");
            Console.WriteLine(url);
            OpenBrowser(url);
            var code = YouTubeOAuth.WaitForCodeAsync(listener, state, ConsentTimeout, stop.Token).GetAwaiter().GetResult();
            listener.Close();
            using var http = new HttpClient { Timeout = httpTimeout };
            var access = YouTubeOAuth.ExchangeCodeAsync(client, code, verifier, redirectUri, http, stop.Token)
                .GetAwaiter().GetResult();
            return work(access, http, stop.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            Console.Error.WriteLine($"youtube {verb} stopped by Ctrl+C; steps not yet done were not run.");
            return EnvironmentFailure;
        }
        catch (Exception ex) when (ex is HttpListenerException or HttpRequestException or TaskCanceledException
                                       or TimeoutException or InvalidOperationException or IOException
                                       or InvalidDataException or JsonException or KeyNotFoundException)
        {
            Console.Error.WriteLine($"youtube {verb} failed: {ex.Message}");
            return EnvironmentFailure;
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
            listener?.Close();
        }
    }

    /// <summary>Starts the redirect listener on 127.0.0.1, or on localhost when that is refused.</summary>
    internal static HttpListener StartListener(int port, out string redirectUri)
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
    ///     by then. The token is revoked at the end, also when a step throws or the run is
    ///     cancelled; the revoke has its own <see cref="RevokeTimeout" />.
    /// </summary>
    internal static Task<int> UploadAsync(string videoPath, YouTubeMeta meta, string access, HttpClient http,
        Action<string> log, Action<string> error, CancellationToken cancellation = default, TimeSpan? revokeTimeout = null) =>
        WithRevokeAsync(access, http, log, error, revokeTimeout,
            () => UploadWithTokenAsync(videoPath, meta, access, http, log, error, cancellation));

    /// <summary>
    ///     With the signed-in access token: prints the channel, replaces the video's snippet, and
    ///     revokes the token at the end, as <see cref="UploadAsync" /> does.
    /// </summary>
    internal static Task<int> UpdateAsync(string videoId, YouTubeMeta meta, string access, HttpClient http,
        Action<string> log, Action<string> error, CancellationToken cancellation = default, TimeSpan? revokeTimeout = null) =>
        WithRevokeAsync(access, http, log, error, revokeTimeout, async () =>
        {
            await LogChannelAsync(http, access, log, error, cancellation);
            cancellation.ThrowIfCancellationRequested();
            await YouTubeApi.UpdateSnippetAsync(http, access, videoId, meta, cancellation);
            log($"youtube: updated title, description, tags and category of https://youtu.be/{videoId}");
            log("youtube: privacy, playlist, captions and thumbnail are not changed by update");
            return Passed;
        });

    /// <summary>Runs <paramref name="work" />, then revokes the token, also when the work throws or is cancelled.</summary>
    private static async Task<int> WithRevokeAsync(string access, HttpClient http, Action<string> log,
        Action<string> error, TimeSpan? revokeTimeout, Func<Task<int>> work)
    {
        try
        {
            return await work();
        }
        finally
        {
            // Not linked to the run's token: after Ctrl+C that token is already cancelled.
            var limit = revokeTimeout ?? RevokeTimeout;
            using var timer = new CancellationTokenSource(limit);
            try
            {
                await YouTubeOAuth.RevokeAsync(access, http, timer.Token);
                log("youtube: sign-in revoked");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // The upload result stands; an access token stops working by itself within an hour.
                var why = ex is TaskCanceledException ? $"no answer in {limit.TotalSeconds:0.#} s" : ex.Message;
                error($"youtube: warning: the revoke failed, so the sign-in stays valid for up to an hour: {why}");
            }
        }
    }

    private static async Task<int> UploadWithTokenAsync(string videoPath, YouTubeMeta meta, string access,
        HttpClient http, Action<string> log, Action<string> error, CancellationToken cancellation)
    {
        await LogChannelAsync(http, access, log, error, cancellation);
        cancellation.ThrowIfCancellationRequested();
        var megabytes = new FileInfo(videoPath).Length / (1024.0 * 1024.0);
        log($"youtube: uploading {Path.GetFileName(videoPath)} ({megabytes:0.0} MB) as {meta.Privacy}");
        var (videoId, privacy) = await YouTubeApi.UploadVideoAsync(http, access, videoPath, meta, cancellation);
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
            steps.Add(("playlist", () => YouTubeApi.AddToPlaylistAsync(http, access, playlistId, videoId, cancellation)));
        }

        if (meta.CaptionsPath is { } captions)
        {
            steps.Add(("captions", () => YouTubeApi.InsertCaptionsAsync(http, access, videoId, captions, cancellation)));
        }

        if (meta.ThumbnailPath is { } thumbnail)
        {
            steps.Add(("thumbnail", () => YouTubeApi.SetThumbnailAsync(http, access, videoId, thumbnail, cancellation)));
        }

        var result = Passed;
        foreach (var (name, run) in steps)
        {
            cancellation.ThrowIfCancellationRequested();
            try
            {
                await run();
                log($"youtube: {name} done");
            }
            catch (Exception ex) when ((ex is HttpRequestException or IOException or TaskCanceledException
                                            or JsonException) && !cancellation.IsCancellationRequested)
            {
                error($"youtube: {name} failed: {ex.Message}");
                result = EnvironmentFailure;
            }
        }

        return result;
    }

    /// <summary>Prints the channel the user picked in the browser, so a wrong choice shows before any change.</summary>
    private static async Task LogChannelAsync(HttpClient http, string access, Action<string> log, Action<string> error,
        CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        try
        {
            var channel = await YouTubeApi.ChannelAsync(http, access, cancellation);
            log(channel is { } c
                ? $"youtube: signed in to channel \"{c.Title}\" ({c.Id})"
                : "youtube: this Google account has no YouTube channel; the next step will fail.");
        }
        catch (Exception ex) when ((ex is HttpRequestException or JsonException or KeyNotFoundException
                                        or InvalidOperationException) && !cancellation.IsCancellationRequested)
        {
            error($"youtube: warning: the channel lookup failed: {ex.Message}");
        }
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
