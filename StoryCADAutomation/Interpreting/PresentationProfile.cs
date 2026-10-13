using System.Diagnostics;
using System.Globalization;
using System.Text;
using StoryCADAutomation.Driver;

namespace StoryCADAutomation.Interpreting;

/// <summary>
///     The presentation realization for recording a demo (#1421 Milestone 4): the cursor glides
///     to each target before the verb acts, pauses run at full length times the pacing factor,
///     and text is typed one character at a time. Failed assertions abort as in test mode; a
///     failed demo is recorded again (Terry, 2026-10-05).
///     Step timing (#1421 Milestone 5, video design section 7): a step's clips play one after
///     the other from the step start while its actions run; the step ends when both are
///     complete, then waits for its hold. The runner does not play the clips; it records when
///     each one starts.
/// </summary>
public sealed class PresentationProfile : IExecutionProfile
{
    // Typing speed at pacing 1.0. About 15 characters a second reads as a quick typist.
    private static readonly TimeSpan BaseKeystrokeDelay = TimeSpan.FromMilliseconds(65);

    // Stand-in clip length when no clip exists: about 150 words a minute, at least 2 seconds.
    private static readonly TimeSpan PerWord = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan MinimumCue = TimeSpan.FromSeconds(2);

    private static readonly TestProfile Test = new();

    private readonly Func<TimeSpan> _elapsed;
    private readonly Action<TimeSpan> _sleep;
    private readonly Func<string, TimeSpan?> _clipLength;
    private readonly List<SubtitleCue> _cues = new();
    private readonly double _pacing;

    // The open step: when its last clip stops, and its hold. Null when no step is open.
    private TimeSpan? _narrationEnd;
    private TimeSpan _hold;

    /// <param name="pacing">Multiplier for pauses and typing delay; 1.0 is the script's own timing.</param>
    /// <param name="clock">
    ///     Clip times count from this clock. The runner starts it when the run starts, so the
    ///     times line up with a recording started just before the runner.
    /// </param>
    /// <param name="clipLength">
    ///     The length of the clip for a narrate text, or null when no clip exists; then the
    ///     reading time stands in. Null means no clips at all.
    /// </param>
    public PresentationProfile(double pacing, Stopwatch clock, Func<string, TimeSpan?>? clipLength = null)
        : this(pacing, () => clock.Elapsed, Thread.Sleep, clipLength)
    {
    }

    /// <summary>For tests: a clock and a sleep that need no real time.</summary>
    internal PresentationProfile(double pacing, Func<TimeSpan> elapsed, Action<TimeSpan> sleep,
        Func<string, TimeSpan?>? clipLength)
    {
        if (!double.IsFinite(pacing) || pacing <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pacing), "pacing must be greater than 0.");
        }

        _pacing = pacing;
        _elapsed = elapsed;
        _sleep = sleep;
        _clipLength = clipLength ?? (static _ => null);
    }

    /// <inheritdoc />
    public string Name => "presentation";

    /// <inheritdoc />
    public bool ContinueOnAssertionFailure => false;

    /// <summary>Narrate lines in run order, with their start times.</summary>
    public IReadOnlyList<SubtitleCue> Cues => _cues;

    /// <inheritdoc />
    public void Click(IUiDriver driver, ElementAddress target)
    {
        Approach(driver, target);
        Test.Click(driver, target);
    }

    /// <inheritdoc />
    public void Approach(IUiDriver driver, ElementAddress target) => driver.GlideTo(target);

    /// <inheritdoc />
    public void ApproachInWindow(IUiDriver driver, string windowTitle, ElementAddress target)
        => driver.GlideToInWindow(windowTitle, target);

    /// <inheritdoc />
    public void Type(IUiDriver driver, string text)
    {
        var delay = TimeSpan.FromTicks((long)(BaseKeystrokeDelay.Ticks * _pacing));
        foreach (var character in text)
        {
            driver.TypeText(character.ToString());
            Thread.Sleep(delay);
        }
    }

    /// <inheritdoc />
    public void Set(IUiDriver driver, ElementAddress target, string value)
    {
        // Clearing through the test path first opens a collapsed Expander around the field
        // (the #1420 expand-before-type fact) and empties it. The value is then typed, so it
        // appears on screen as a user would enter it.
        driver.SetText(target, string.Empty);
        Approach(driver, target);
        driver.Focus(target);
        Type(driver, value);
    }

    /// <inheritdoc />
    public TimeSpan ScalePause(double seconds) => TimeSpan.FromSeconds(seconds * _pacing);

    /// <inheritdoc />
    public void Narrate(string text, Action<string> log)
    {
        // A clip is real audio, so pacing does not change its length. The reading time stands
        // in for a clip, so pacing does not change it either (Milestone 4 scaled it).
        var length = _clipLength(text) ?? ReadingTime(text);
        var now = _elapsed();
        log($"narrate: {text}");
        if (_narrationEnd is { } end)
        {
            // In a step the clip follows the step's earlier clips, and the actions do not wait.
            var start = end > now ? end : now;
            _cues.Add(new SubtitleCue(start, start + length, text));
            _narrationEnd = start + length;
            return;
        }

        // Outside a step there is no step end to wait at, so the run waits for the clip here.
        _cues.Add(new SubtitleCue(now, now + length, text));
        _sleep(length);
    }

    /// <inheritdoc />
    public void BeginStep(double holdSeconds)
    {
        EndStep();
        _narrationEnd = _elapsed();
        // Hold is always the seconds written; pacing does not change it (Terry, 2026-10-13).
        _hold = TimeSpan.FromSeconds(holdSeconds);
    }

    /// <inheritdoc />
    public void EndStep()
    {
        if (_narrationEnd is not { } end)
        {
            return;
        }

        _narrationEnd = null;
        var wait = StepEndWait(_elapsed(), end, _hold);
        if (wait > TimeSpan.Zero)
        {
            _sleep(wait);
        }
    }

    /// <summary>
    ///     How long a step waits after its actions: until its narration stops, then its hold
    ///     (video design, section 7, rules 3 and 4).
    /// </summary>
    internal static TimeSpan StepEndWait(TimeSpan now, TimeSpan narrationEnd, TimeSpan hold)
        => (narrationEnd > now ? narrationEnd - now : TimeSpan.Zero) + hold;

    internal static TimeSpan ReadingTime(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var time = TimeSpan.FromTicks(PerWord.Ticks * words);
        return time > MinimumCue ? time : MinimumCue;
    }

    /// <summary>The cues as a SubRip (.srt) file. A cue ends early if the next one starts first.</summary>
    public static string ToSrt(IReadOnlyList<SubtitleCue> cues)
    {
        var srt = new StringBuilder();
        for (var i = 0; i < cues.Count; i++)
        {
            var start = cues[i].Start;
            var end = cues[i].End;
            if (i + 1 < cues.Count && cues[i + 1].Start < end)
            {
                end = cues[i + 1].Start;
            }

            srt.Append(i + 1).Append("\r\n");
            srt.Append(SrtTime(start)).Append(" --> ").Append(SrtTime(end)).Append("\r\n");
            srt.Append(cues[i].Text).Append("\r\n\r\n");
        }

        return srt.ToString();
    }

    private static string SrtTime(TimeSpan t)
        => string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00},{3:000}",
            (int)t.TotalHours, t.Minutes, t.Seconds, t.Milliseconds);
}

/// <summary>One narrate line and when it shows, counted from the start of the run.</summary>
public sealed record SubtitleCue(TimeSpan Start, TimeSpan End, string Text);
