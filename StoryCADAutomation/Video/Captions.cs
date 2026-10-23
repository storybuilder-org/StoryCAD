using System.Globalization;
using System.Text;

namespace StoryCADAutomation.Video;

/// <summary>One caption: its text (one or two lines, split by '\n') and when it shows in the finished video.</summary>
public sealed record Caption(TimeSpan Start, TimeSpan End, string Text);

/// <summary>
///     The SubRip caption file for a video (#1626, Terry, 2026-10-23). YouTube's automatic
///     captions misspell names and terms, but the narration is spoken from the script's narrate
///     lines, so the captions come from those lines and the clip times that assembly uses.
///     Caption shape follows the DCMP Captioning Key: at most 2 lines of 32 characters, each
///     caption on screen from 1.33 s to 6 s.
/// </summary>
public static class Captions
{
    internal const int MaxLineLength = 32;
    internal static readonly TimeSpan MinDuration = TimeSpan.FromSeconds(1.33);
    internal static readonly TimeSpan MaxDuration = TimeSpan.FromSeconds(6);

    /// <summary>The caption over the title card when the intro music plays.</summary>
    internal const string Music = "[music]";

    // A break at a sentence or clause end must leave at least this much text before it, so a
    // short opening ("Yes,") does not become a caption too brief to read. About 1.33 s of speech.
    private const int MinBreakLength = 20;

    /// <summary>
    ///     The captions for the assembled video: each clip's caption text across that clip, in
    ///     the time of the joined video, and [music] over a title card with music.
    /// </summary>
    public static IReadOnlyList<Caption> ForPlan(VideoPlan plan)
    {
        // The body starts where the title card starts to crossfade into it (VideoAssembler.JoinArguments).
        var titleLength = VideoAssembler.TitleLength(plan);
        var bodyStart = plan.Title is null ? TimeSpan.Zero : titleLength - VideoAssembler.PartCrossfade;
        var bodyEnd = bodyStart + plan.RecordingLength;
        var captions = new List<Caption>();
        foreach (var clip in plan.Clips.Where(c => !string.IsNullOrWhiteSpace(c.Caption)))
        {
            // Assembly starts a clip with a cue before the recording at 0 (adelay).
            var start = bodyStart + (clip.Start > TimeSpan.Zero ? clip.Start : TimeSpan.Zero);
            captions.AddRange(Time(clip.Caption!, start, clip.Length)
                .Where(c => c.Start < bodyEnd)
                .Select(c => c.End > bodyEnd ? c with { End = bodyEnd } : c));
        }

        captions = Limit(captions.OrderBy(c => c.Start).ToList());
        if (plan.Title is not null && plan.MusicPath is not null && plan.MusicLength is not null)
        {
            var end = captions.Count > 0 && captions[0].Start < titleLength ? captions[0].Start : titleLength;
            captions.Insert(0, new Caption(TimeSpan.Zero, end, Music));
        }

        return captions;
    }

    /// <summary>
    ///     The captions for one clip: each caption gets a share of the clip's time in proportion
    ///     to its share of the clip's characters.
    /// </summary>
    internal static IReadOnlyList<Caption> Time(string text, TimeSpan start, TimeSpan length)
    {
        var parts = Split(text);
        double total = parts.Sum(p => p.Length);
        var captions = new List<Caption>();
        var done = 0;
        foreach (var part in parts)
        {
            var from = start + length * (done / total);
            done += part.Length;
            captions.Add(new Caption(from, start + length * (done / total), part));
        }

        return captions;
    }

    /// <summary>
    ///     Keeps each caption from 1.33 s to 6 s on screen. A caption over 6 s ends at 6 s. A
    ///     caption under 1.33 s stays up longer, but not past the start of the next caption.
    /// </summary>
    internal static List<Caption> Limit(List<Caption> captions)
    {
        for (var i = 0; i < captions.Count; i++)
        {
            var c = captions[i];
            if (c.End - c.Start > MaxDuration)
            {
                captions[i] = c with { End = c.Start + MaxDuration };
            }
            else if (c.End - c.Start < MinDuration)
            {
                var end = c.Start + MinDuration;
                if (i + 1 < captions.Count && captions[i + 1].Start < end)
                {
                    end = captions[i + 1].Start > c.End ? captions[i + 1].Start : c.End;
                }

                captions[i] = c with { End = end };
            }
        }

        return captions;
    }

    /// <summary>
    ///     Splits a narration into caption texts of at most 2 lines of 32 characters, breaking
    ///     only between words. When the next word does not fit, the caption ends at its last
    ///     sentence end, else at its last clause end, else before that word.
    /// </summary>
    internal static IReadOnlyList<string> Split(string text)
    {
        var captions = new List<string>();
        var current = new List<string>();
        foreach (var unit in Units(text))
        {
            if (current.Count > 0 && Wrap(current.Append(unit).ToList()) is null)
            {
                var cut = BreakPoint(current);
                captions.Add(Wrap(current.Take(cut).ToList())!);
                current = current.Skip(cut).ToList();
                if (current.Count > 0 && Wrap(current.Append(unit).ToList()) is null)
                {
                    captions.Add(Wrap(current)!);
                    current.Clear();
                }
            }

            current.Add(unit);
        }

        if (current.Count > 0)
        {
            captions.Add(Wrap(current)!);
        }

        return captions;
    }

    /// <summary>
    ///     The words of the text, with a run of capitalized words kept as one unit when the run
    ///     fits on a line, so a name such as "The Old Man" stays together.
    /// </summary>
    internal static List<string> Units(string text)
    {
        var units = new List<string>();
        foreach (var word in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (units.Count > 0 && StartsUpper(word) && StartsUpper(LastWord(units[^1])) && !EndsSentence(units[^1])
                && !EndsClause(units[^1]) && units[^1].Length + 1 + word.Length <= MaxLineLength)
            {
                units[^1] += " " + word;
            }
            else
            {
                units.Add(word);
            }
        }

        return units;
    }

    /// <summary>
    ///     The units as one or two lines of at most 32 characters, or null when they do not fit.
    ///     Of the two-line breaks, the one with the shorter longer line wins; a break after a
    ///     clause or sentence end counts as 8 characters shorter. One unit always fits.
    /// </summary>
    internal static string? Wrap(IReadOnlyList<string> units)
    {
        var whole = string.Join(" ", units);
        if (whole.Length <= MaxLineLength || units.Count == 1)
        {
            return whole;
        }

        string? best = null;
        var bestScore = int.MaxValue;
        for (var k = 1; k < units.Count; k++)
        {
            var first = string.Join(" ", units.Take(k));
            var second = string.Join(" ", units.Skip(k));
            if (first.Length > MaxLineLength || second.Length > MaxLineLength)
            {
                continue;
            }

            var score = Math.Max(first.Length, second.Length) - (EndsClause(first) || EndsSentence(first) ? 8 : 0);
            if (score < bestScore)
            {
                (best, bestScore) = (first + "\n" + second, score);
            }
        }

        return best;
    }

    /// <summary>Where a full caption ends: after its last sentence end, else its last clause end, else after all its units.</summary>
    private static int BreakPoint(List<string> units)
    {
        foreach (var ends in new Func<string, bool>[] { EndsSentence, EndsClause })
        {
            for (var i = units.Count - 1; i >= 1; i--)
            {
                if (ends(units[i - 1]) && string.Join(" ", units.Take(i)).Length >= MinBreakLength)
                {
                    return i;
                }
            }
        }

        return units.Count;
    }

    private static bool StartsUpper(string word) => word.Length > 0 && char.IsUpper(word[0]);

    private static string LastWord(string unit) => unit[(unit.LastIndexOf(' ') + 1)..];

    private static bool EndsSentence(string text) => text.TrimEnd('"', '\'', ')').EndsWith('.') ||
                                                     text.TrimEnd('"', '\'', ')').EndsWith('!') ||
                                                     text.TrimEnd('"', '\'', ')').EndsWith('?');

    private static bool EndsClause(string text) => text.EndsWith(',') || text.EndsWith(';') || text.EndsWith(':');

    /// <summary>The captions as SubRip text: index, times, text, and a blank line after each caption.</summary>
    public static string ToSrt(IReadOnlyList<Caption> captions)
    {
        var srt = new StringBuilder();
        for (var i = 0; i < captions.Count; i++)
        {
            srt.Append(i + 1).Append('\n');
            srt.Append(SrtTime(captions[i].Start)).Append(" --> ").Append(SrtTime(captions[i].End)).Append('\n');
            srt.Append(captions[i].Text).Append("\n\n");
        }

        return srt.ToString();
    }

    /// <summary>HH:MM:SS,mmm, rounded to the millisecond.</summary>
    internal static string SrtTime(TimeSpan t)
    {
        var ms = (long)Math.Round(Math.Max(0, t.TotalMilliseconds));
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00},{3:000}",
            ms / 3_600_000, ms / 60_000 % 60, ms / 1000 % 60, ms % 1000);
    }

    /// <summary>Writes the captions as a UTF-8 .srt file.</summary>
    public static void Write(VideoPlan plan, string path)
        => File.WriteAllText(path, ToSrt(ForPlan(plan)), new UTF8Encoding(false));
}
