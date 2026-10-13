using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace StoryCADAutomation.Voice;

/// <summary>
///     The narrator clips for a script (#1421 Milestone 5, video design section 8.1): one
///     ElevenLabs clip for each narrate text, in &lt;out&gt;/voice/. The clip name comes from the
///     voice and the text, so a clip is made once and a changed line gets a new clip.
/// </summary>
public static class VoiceClips
{
    public const string ApiKeyVariable = "ELEVENLABS_API_KEY";
    public const string VoiceIdVariable = "STORYCAD_VOICE_ID";

    // WAV, because its header gives the clip length without ffprobe. 24 kHz, because
    // ElevenLabs keeps 44.1 kHz PCM and WAV for the Pro tier and above.
    internal const string OutputFormat = "wav_24000";
    internal const string ModelId = "eleven_multilingual_v2";

    private static readonly Uri Api = new("https://api.elevenlabs.io/v1/text-to-speech/");

    /// <summary>The clip folder under the runner's --out folder.</summary>
    public static string Folder(string outputDirectory) => Path.Combine(Path.GetFullPath(outputDirectory), "voice");

    /// <summary>The clip file name for a voice and a narrate text.</summary>
    public static string ClipName(string voiceId, string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(voiceId + "\n" + text));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant() + ".wav";
    }

    /// <summary>
    ///     The clip length for a narrate text, or null when the clip is missing or is not a WAV
    ///     file; then the presentation profile uses the reading time.
    /// </summary>
    public static Func<string, TimeSpan?> Lengths(string folder, string voiceId) => text =>
    {
        var path = Path.Combine(folder, ClipName(voiceId, text));
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            return WavLength(stream);
        }
        catch (IOException)
        {
            // A clip open in another program must not stop a recording; the reading time stands in.
            return null;
        }
    };

    /// <summary>
    ///     Makes each missing clip and keeps each clip that exists. Returns the counts. A failed
    ///     request throws <see cref="HttpRequestException" />; the clips made before it stay.
    /// </summary>
    public static async Task<(int Made, int Kept)> MakeAsync(
        IEnumerable<string> texts, string folder, string voiceId, string apiKey, HttpClient http,
        Action<string> log, CancellationToken cancellation = default)
    {
        Directory.CreateDirectory(folder);
        int made = 0, kept = 0;
        foreach (var text in texts.Distinct(StringComparer.Ordinal))
        {
            var path = Path.Combine(folder, ClipName(voiceId, text));
            if (File.Exists(path))
            {
                kept++;
                continue;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post,
                new Uri(Api, Uri.EscapeDataString(voiceId) + "?output_format=" + OutputFormat));
            request.Headers.Add("xi-api-key", apiKey);
            request.Content = JsonContent.Create(new { text, model_id = ModelId });
            using var response = await http.SendAsync(request, cancellation);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellation);
                throw new HttpRequestException(
                    $"ElevenLabs returned {(int)response.StatusCode} for \"{text}\": {detail}", null, response.StatusCode);
            }

            var audio = await response.Content.ReadAsByteArrayAsync(cancellation);
            if (WavLength(new MemoryStream(audio)) is not { } length)
            {
                throw new HttpRequestException($"ElevenLabs did not return a WAV file for \"{text}\".");
            }

            // Written under a temporary name first, so a stopped run leaves no half clip that
            // the next run would keep.
            var partial = path + ".part";
            await File.WriteAllBytesAsync(partial, audio, cancellation);
            File.Move(partial, path, overwrite: true);
            made++;
            log($"voice: {length.TotalSeconds:0.0}s {Path.GetFileName(path)} \"{text}\"");
        }

        return (made, kept);
    }

    /// <summary>The audio length of a WAV file from its fmt and data chunks; null if not WAV.</summary>
    internal static TimeSpan? WavLength(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        if (stream.Length < 12 || Tag(reader) != "RIFF")
        {
            return null;
        }

        reader.ReadUInt32();
        if (Tag(reader) != "WAVE")
        {
            return null;
        }

        uint byteRate = 0;
        while (stream.Length - stream.Position >= 8)
        {
            var id = Tag(reader);
            long size = reader.ReadUInt32();
            var remaining = stream.Length - stream.Position;
            if (id == "fmt " && size >= 16 && remaining >= 16)
            {
                var start = stream.Position;
                stream.Position += 8; // format, channels, sample rate
                byteRate = reader.ReadUInt32();
                stream.Position = start;
            }
            else if (id == "data")
            {
                // A streamed WAV can carry a placeholder size; the bytes present are the clip.
                var bytes = size == 0 || size > remaining ? remaining : size;
                return byteRate == 0 ? null : TimeSpan.FromSeconds((double)bytes / byteRate);
            }

            stream.Position += Math.Min(size + (size & 1), remaining);
        }

        return null;
    }

    private static string Tag(BinaryReader reader) => Encoding.ASCII.GetString(reader.ReadBytes(4));
}
