using System.Globalization;

namespace StoryCADAutomation.Video;

/// <summary>A span of the recording that the video leaves out, in recording time.</summary>
public sealed record Cut(TimeSpan Start, TimeSpan End);

/// <summary>
///     The cuts for `wait &lt;target&gt; condense` (#1626, Terry, 2026-10-24): a Collaborator run
///     can take minutes, so the video keeps only the start and the end of each condensed wait.
/// </summary>
public static class Cuts
{
    /// <summary>The part of a condensed wait that stays in the video at each end, so the viewer sees the run start and finish.</summary>
    internal static readonly TimeSpan Keep = TimeSpan.FromSeconds(1.5);

    /// <summary>
    ///     The cuts for the condensed waits, all in recording time. Each cut is the wait less
    ///     <see cref="Keep" /> at each end. No clip is cut: a cut starts after a clip that plays at
    ///     its start and ends before a clip that starts in it. Nothing after
    ///     <paramref name="bodyEnd" /> is cut, and a cut of no length is left out.
    /// </summary>
    internal static IReadOnlyList<Cut> FromWaits(IEnumerable<(TimeSpan Start, TimeSpan End)> waits,
        IEnumerable<(TimeSpan Start, TimeSpan End)> clips, TimeSpan bodyEnd)
    {
        var sortedClips = clips.OrderBy(c => c.Start).ToList();
        var cuts = new List<Cut>();
        foreach (var wait in waits.OrderBy(w => w.Start))
        {
            var start = wait.Start + Keep;
            var end = wait.End - Keep < bodyEnd ? wait.End - Keep : bodyEnd;
            foreach (var clip in sortedClips)
            {
                if (clip.End <= start || clip.Start >= end)
                {
                    continue;
                }

                if (clip.Start <= start)
                {
                    start = clip.End;
                }
                else
                {
                    end = clip.Start;
                }
            }

            if (end > start)
            {
                cuts.Add(new Cut(start, end));
            }
        }

        return cuts;
    }

    /// <summary>
    ///     A recording time as a time in the video body: less the length of each cut before it.
    ///     A time inside a cut becomes the time where the cut is.
    /// </summary>
    internal static TimeSpan Map(TimeSpan t, IReadOnlyList<Cut> cuts)
    {
        var removed = TimeSpan.Zero;
        foreach (var cut in cuts)
        {
            if (t >= cut.End)
            {
                removed += cut.End - cut.Start;
            }
            else
            {
                return (t > cut.Start ? cut.Start : t) - removed;
            }
        }

        return t - removed;
    }

    /// <summary>
    ///     The video filters that drop the cut frames and close the gaps, for after fps=30, or ""
    ///     without cuts. The clips are placed in body time, so the audio needs no cut.
    /// </summary>
    internal static string SelectFilter(IReadOnlyList<Cut> cuts)
    {
        if (cuts.Count == 0)
        {
            return string.Empty;
        }

        var spans = string.Join("+", cuts.Select(c => $"between(t,{Seconds(c.Start)},{Seconds(c.End)})"));
        return $",select='not({spans})',setpts=N/FRAME_RATE/TB";
    }

    private static string Seconds(TimeSpan t) => t.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
}
