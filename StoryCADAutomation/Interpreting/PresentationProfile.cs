using System.Diagnostics;
using System.Globalization;
using System.Text;
using StoryCADAutomation.Driver;

namespace StoryCADAutomation.Interpreting;

/// <summary>
///     The presentation realization for recording a demo (#1421 Milestone 4): the cursor glides
///     to each target before the verb acts, pauses run at full length times the pacing factor,
///     text is typed one character at a time, and narrate lines become subtitle cues that
///     hold the run for their reading time. Failed assertions abort as in test mode; a failed
///     demo is recorded again (Terry, 2026-10-05).
/// </summary>
public sealed class PresentationProfile : IExecutionProfile
{
    // Typing speed at pacing 1.0. About 15 characters a second reads as a quick typist.
    private static readonly TimeSpan BaseKeystrokeDelay = TimeSpan.FromMilliseconds(65);

    // Narrate holds the run for reading time, about 150 words a minute, at least 2 seconds.
    private static readonly TimeSpan PerWord = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan MinimumCue = TimeSpan.FromSeconds(2);

    private static readonly TestProfile Test = new();

    private readonly Stopwatch _clock;
    private readonly List<SubtitleCue> _cues = new();
    private readonly double _pacing;

    /// <param name="pacing">Multiplier for pauses and typing delay; 1.0 is the script's own timing.</param>
    /// <param name="clock">
    ///     Cue times count from this clock. The runner starts it when the run starts, so the
    ///     .srt lines up with a recording started just before the runner.
    /// </param>
    public PresentationProfile(double pacing, Stopwatch clock)
    {
        if (!double.IsFinite(pacing) || pacing <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pacing), "pacing must be greater than 0.");
        }

        _pacing = pacing;
        _clock = clock;
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
        // The run waits while the caption is read, so the next action starts after it.
        var reading = TimeSpan.FromTicks((long)(ReadingTime(text).Ticks * _pacing));
        var start = _clock.Elapsed;
        _cues.Add(new SubtitleCue(start, start + reading, text));
        log($"narrate: {text}");
        Thread.Sleep(reading);
    }

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
