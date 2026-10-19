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
///     one .mp4 file (#1421 Milestone 5, video design section 9). The parts crossfade into each
///     other and each card fades in and out (#1626, Terry, 2026-10-19).
/// </summary>
public static class VideoAssembler
{
    public const int Width = 1920;
    public const int Height = 1080;
    private const string Background = "0x1E1E1E";
    private static readonly TimeSpan TitleWithoutMusic = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ClosingLength = TimeSpan.FromSeconds(10);

    // Terry may change these two after he sees a preview (#1626, 2026-10-19).
    /// <summary>The crossfade from the title card to the body and from the body to the closing card.</summary>
    internal static readonly TimeSpan PartCrossfade = TimeSpan.FromSeconds(0.5);

    /// <summary>How long a card over the recording takes to fade in, and again to fade out.</summary>
    internal static readonly TimeSpan CardFade = TimeSpan.FromSeconds(0.3);

    // Inside a filter, a colon separates options, so the drive colon is escaped.
    private const string FontFile = @"C\:/Windows/Fonts/segoeui.ttf";

    /// <summary>Makes <paramref name="outputPath" /> from the plan. Work files go in <paramref name="workDirectory" />.</summary>
    public static void Assemble(Ffmpeg ffmpeg, VideoPlan plan, string workDirectory, string outputPath)
    {
        Directory.CreateDirectory(workDirectory);
        var parts = new List<(string Path, TimeSpan Length)>();
        if (plan.Title is not null)
        {
            File.WriteAllText(Path.Combine(workDirectory, "title.txt"), plan.Title);
            ffmpeg.Run(TitleArguments(plan, "title.mp4"), workDirectory);
            parts.Add(("title.mp4", TitleLength(plan)));
        }

        for (var i = 0; i < plan.Cards.Count; i++)
        {
            if (plan.Cards[i].Text is { } text)
            {
                File.WriteAllText(Path.Combine(workDirectory, $"card{i}.txt"), text);
            }
        }

        ffmpeg.Run(BodyArguments(plan, "body.mp4"), workDirectory);
        parts.Add(("body.mp4", plan.RecordingLength));
        if (plan.Closing is not null)
        {
            File.WriteAllText(Path.Combine(workDirectory, "closing.txt"), plan.Closing);
            ffmpeg.Run(ClosingArguments("closing.mp4"), workDirectory);
            parts.Add(("closing.mp4", ClosingLength));
        }

        ffmpeg.Run(JoinArguments(parts, Path.GetFullPath(outputPath)), workDirectory);
    }

    /// <summary>
    ///     Joins the parts in order, each crossfading into the next over <see cref="PartCrossfade" />,
    ///     video and sound alike. The crossfades re-encode, with the same settings as each part.
    /// </summary>
    internal static IReadOnlyList<string> JoinArguments(IReadOnlyList<(string Path, TimeSpan Length)> parts, string output)
    {
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-y" };
        foreach (var part in parts)
        {
            args.AddRange(new[] { "-i", part.Path });
        }

        if (parts.Count == 1)
        {
            args.AddRange(new[] { "-c", "copy", "-movflags", "+faststart", output });
            return args;
        }

        var filter = new List<string>();
        string video = "[0:v]", audio = "[0:a]";
        var joined = parts[0].Length;
        for (var i = 1; i < parts.Count; i++)
        {
            // An xfade offset counts on the video joined so far, which each earlier crossfade made shorter.
            var offset = joined - PartCrossfade;
            filter.Add($"{video}[{i}:v]xfade=transition=fade:duration={Seconds(PartCrossfade)}:offset={Seconds(offset)}[v{i}]");
            filter.Add($"{audio}[{i}:a]acrossfade=d={Seconds(PartCrossfade)}[a{i}]");
            (video, audio) = ($"[v{i}]", $"[a{i}]");
            joined = offset + parts[i].Length;
        }

        args.AddRange(new[] { "-filter_complex", string.Join(";", filter), "-map", video, "-map", audio });
        args.AddRange(EncodeSettings);
        args.AddRange(new[] { "-movflags", "+faststart", "-t", Seconds(joined), output });
        return args;
    }

    /// <summary>The title card's length: the intro music's, or 5 s without music.</summary>
    internal static TimeSpan TitleLength(VideoPlan plan)
        => plan.MusicPath is not null && plan.MusicLength is { } music ? music : TitleWithoutMusic;

    /// <summary>The title card: the title above the logo, the intro music under it.</summary>
    internal static IReadOnlyList<string> TitleArguments(VideoPlan plan, string output)
    {
        var length = TitleLength(plan);
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-y" };
        args.AddRange(ColorInput(length));
        var filter = new List<string>();
        var video = "[0:v]";
        var next = 1;
        if (plan.LogoPath is not null)
        {
            args.AddRange(new[] { "-i", plan.LogoPath });
            filter.Add($"[{next}:v]scale=w=1000:h=400:force_original_aspect_ratio=decrease[logo]");
            filter.Add($"{video}[logo]overlay=x=(W-w)/2:y=(H-h)/2+140[bg]");
            video = "[bg]";
            next++;
        }

        var textY = plan.LogoPath is null ? "(h-text_h)/2" : "(h-text_h)/2-260";
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

    /// <summary>
    ///     The recording, scaled to 1920x1080, with each card over its step and each clip at its
    ///     time. Each card fades in and out over <see cref="CardFade" />, or over half its length
    ///     when it is shorter than two fades; a card that starts at 0 shows at once.
    /// </summary>
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
                           $"pad={Width}:{Height}:(ow-iw)/2:(oh-ih)/2:color={Background},setsar=1,fps=30{CardFades(card)}[c{i}]");
            }
            else
            {
                args.AddRange(new[] { "-f", "lavfi", "-i", $"color=c={Background}:s={Width}x{Height}:r=30" });
                filter.Add($"[{input}:v]{DrawText($"card{i}.txt", "(h-text_h)/2")}{CardFades(card)}[c{i}]");
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

    /// <summary>The alpha fades for one card's stream, which counts time from the recording's first frame.</summary>
    internal static string CardFades(VideoCard card)
    {
        var length = card.End - card.Start;
        var fade = length < CardFade * 2 ? length / 2 : CardFade;
        if (fade <= TimeSpan.Zero)
        {
            return string.Empty;
        }

        var fades = ",format=yuva420p";
        if (card.Start > TimeSpan.Zero)
        {
            fades += $",fade=t=in:st={Seconds(card.Start)}:d={Seconds(fade)}:alpha=1";
        }

        return fades + $",fade=t=out:st={Seconds(card.End - fade)}:d={Seconds(fade)}:alpha=1";
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

    private static readonly string[] EncodeSettings =
    {
        "-c:v", "libx264", "-preset", "medium", "-crf", "18", "-pix_fmt", "yuv420p", "-r", "30",
        "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2",
    };

    private static IEnumerable<string> Encode(TimeSpan length, string output)
        => EncodeSettings.Concat(new[] { "-t", Seconds(length), output });

    internal static string Seconds(TimeSpan t) => t.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
}
