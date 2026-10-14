using System.Globalization;

namespace StoryCADAutomation.Video;

/// <summary>A card that covers the recording from Start to End: text, or an image file.</summary>
public sealed record VideoCard(TimeSpan Start, TimeSpan End, string? Text, string? ImagePath);

/// <summary>One voice clip and where it starts in the recording.</summary>
public sealed record ClipPlacement(string Path, TimeSpan Start);

/// <summary>Everything assembly needs; all times count from the first frame of the recording.</summary>
public sealed class VideoPlan
{
    public required string RecordingPath { get; init; }
    public required TimeSpan RecordingLength { get; init; }
    public string? Title { get; init; }
    public string? Closing { get; init; }
    public IReadOnlyList<VideoCard> Cards { get; init; } = Array.Empty<VideoCard>();
    public IReadOnlyList<ClipPlacement> Clips { get; init; } = Array.Empty<ClipPlacement>();
    public string? LogoPath { get; init; }
    public string? MusicPath { get; init; }
    public TimeSpan? MusicLength { get; init; }
}

/// <summary>
///     Joins the title card, the recording with its cards and clips, and the closing card into
///     one .mp4 file (#1421 Milestone 5, video design section 9). Each part is encoded with the
///     same settings, so the parts join without a second encode.
/// </summary>
public static class VideoAssembler
{
    public const int Width = 1920;
    public const int Height = 1080;
    private const string Background = "0x1E1E1E";
    private static readonly TimeSpan TitleWithoutMusic = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ClosingLength = TimeSpan.FromSeconds(10);

    // Inside a filter, a colon separates options, so the drive colon is escaped.
    private const string FontFile = @"C\:/Windows/Fonts/segoeui.ttf";

    /// <summary>Makes <paramref name="outputPath" /> from the plan. Work files go in <paramref name="workDirectory" />.</summary>
    public static void Assemble(Ffmpeg ffmpeg, VideoPlan plan, string workDirectory, string outputPath)
    {
        Directory.CreateDirectory(workDirectory);
        var parts = new List<string>();
        if (plan.Title is not null)
        {
            File.WriteAllText(Path.Combine(workDirectory, "title.txt"), plan.Title);
            ffmpeg.Run(TitleArguments(plan, "title.mp4"), workDirectory);
            parts.Add("title.mp4");
        }

        for (var i = 0; i < plan.Cards.Count; i++)
        {
            if (plan.Cards[i].Text is { } text)
            {
                File.WriteAllText(Path.Combine(workDirectory, $"card{i}.txt"), text);
            }
        }

        ffmpeg.Run(BodyArguments(plan, "body.mp4"), workDirectory);
        parts.Add("body.mp4");
        if (plan.Closing is not null)
        {
            File.WriteAllText(Path.Combine(workDirectory, "closing.txt"), plan.Closing);
            ffmpeg.Run(ClosingArguments("closing.mp4"), workDirectory);
            parts.Add("closing.mp4");
        }

        File.WriteAllLines(Path.Combine(workDirectory, "parts.txt"), parts.Select(p => $"file '{p}'"));
        ffmpeg.Run(new[]
        {
            "-hide_banner", "-loglevel", "error", "-y", "-f", "concat", "-safe", "0", "-i", "parts.txt",
            "-c", "copy", "-movflags", "+faststart", Path.GetFullPath(outputPath),
        }, workDirectory);
    }

    /// <summary>The title card: the logo above the title, the intro music under it.</summary>
    internal static IReadOnlyList<string> TitleArguments(VideoPlan plan, string output)
    {
        var length = plan.MusicPath is not null && plan.MusicLength is { } music ? music : TitleWithoutMusic;
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-y" };
        args.AddRange(ColorInput(length));
        var filter = new List<string>();
        var video = "[0:v]";
        var next = 1;
        if (plan.LogoPath is not null)
        {
            args.AddRange(new[] { "-i", plan.LogoPath });
            filter.Add($"[{next}:v]scale=w=1000:h=400:force_original_aspect_ratio=decrease[logo]");
            filter.Add($"{video}[logo]overlay=x=(W-w)/2:y=(H-h)/2-140[bg]");
            video = "[bg]";
            next++;
        }

        var textY = plan.LogoPath is null ? "(h-text_h)/2" : "(h-text_h)/2+260";
        filter.Add($"{video}{DrawText("title.txt", textY)}[v]");
        if (plan.MusicPath is not null)
        {
            args.AddRange(new[] { "-i", plan.MusicPath });
            filter.Add($"[{next}:a]{AudioFormat}[a]");
        }
        else
        {
            args.AddRange(SilenceInput(length));
            filter.Add($"[{next}:a]{AudioFormat}[a]");
        }

        args.AddRange(new[] { "-filter_complex", string.Join(";", filter), "-map", "[v]", "-map", "[a]" });
        args.AddRange(Encode(length, output));
        return args;
    }

    /// <summary>The recording, scaled to 1920x1080, with each card over its step and each clip at its time.</summary>
    internal static IReadOnlyList<string> BodyArguments(VideoPlan plan, string output)
    {
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-y", "-i", plan.RecordingPath };
        var filter = new List<string> { $"[0:v]scale={Width}:{Height}:flags=lanczos,fps=30,setsar=1[v0]" };
        var input = 1;
        for (var i = 0; i < plan.Cards.Count; i++)
        {
            var card = plan.Cards[i];
            if (card.ImagePath is not null)
            {
                args.AddRange(new[] { "-loop", "1", "-framerate", "30", "-i", card.ImagePath });
                filter.Add($"[{input}:v]scale={Width}:{Height}:force_original_aspect_ratio=decrease," +
                           $"pad={Width}:{Height}:(ow-iw)/2:(oh-ih)/2:color={Background},setsar=1,fps=30[c{i}]");
            }
            else
            {
                args.AddRange(new[] { "-f", "lavfi", "-i", $"color=c={Background}:s={Width}x{Height}:r=30" });
                filter.Add($"[{input}:v]{DrawText($"card{i}.txt", "(h-text_h)/2")}[c{i}]");
            }

            filter.Add($"[v{i}][c{i}]overlay=0:0:shortest=1:enable='between(t,{Seconds(card.Start)},{Seconds(card.End)})'[v{i + 1}]");
            input++;
        }

        var audio = new List<string>();
        foreach (var clip in plan.Clips)
        {
            args.AddRange(new[] { "-i", clip.Path });
            var delay = (long)Math.Max(0, clip.Start.TotalMilliseconds);
            filter.Add($"[{input}:a]{AudioFormat},adelay={delay}:all=1[a{audio.Count}]");
            audio.Add($"[a{audio.Count}]");
            input++;
        }

        if (audio.Count == 0)
        {
            args.AddRange(SilenceInput(plan.RecordingLength));
            filter.Add($"[{input}:a]{AudioFormat}[aout]");
        }
        else
        {
            filter.Add($"{string.Concat(audio)}amix=inputs={audio.Count}:normalize=0:duration=longest,apad[aout]");
        }

        args.AddRange(new[] { "-filter_complex", string.Join(";", filter), "-map", $"[v{plan.Cards.Count}]", "-map", "[aout]" });
        args.AddRange(Encode(plan.RecordingLength, output));
        return args;
    }

    /// <summary>The closing card: the closing text for 10 s, no sound.</summary>
    internal static IReadOnlyList<string> ClosingArguments(string output)
    {
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-y" };
        args.AddRange(ColorInput(ClosingLength));
        args.AddRange(SilenceInput(ClosingLength));
        args.AddRange(new[]
        {
            "-filter_complex", $"[0:v]{DrawText("closing.txt", "(h-text_h)/2")}[v];[1:a]{AudioFormat}[a]",
            "-map", "[v]", "-map", "[a]",
        });
        args.AddRange(Encode(ClosingLength, output));
        return args;
    }

    private const string AudioFormat = "aformat=sample_rates=48000:channel_layouts=stereo";

    // White Segoe UI, 64 px, centered on both axes (design section 9). The text comes from a
    // file and expansion is off, so no character in it needs escaping.
    private static string DrawText(string textFile, string y)
        => $"drawtext=fontfile='{FontFile}':textfile={textFile}:expansion=none:text_align=C:" +
           $"fontsize=64:fontcolor=white:x=(w-text_w)/2:y={y}";

    private static IEnumerable<string> ColorInput(TimeSpan length)
        => new[] { "-f", "lavfi", "-i", $"color=c={Background}:s={Width}x{Height}:r=30:d={Seconds(length)}" };

    private static IEnumerable<string> SilenceInput(TimeSpan length)
        => new[] { "-f", "lavfi", "-i", $"anullsrc=r=48000:cl=stereo:d={Seconds(length)}" };

    private static IEnumerable<string> Encode(TimeSpan length, string output) => new[]
    {
        "-c:v", "libx264", "-preset", "medium", "-crf", "18", "-pix_fmt", "yuv420p", "-r", "30",
        "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2",
        "-t", Seconds(length), output,
    };

    internal static string Seconds(TimeSpan t) => t.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
}
