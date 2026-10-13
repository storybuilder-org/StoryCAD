using System.Diagnostics;
using System.Drawing;
using System.Globalization;

namespace StoryCADAutomation.Video;

/// <summary>
///     Records one area of the screen with ffmpeg's gdigrab while the script runs (#1421
///     Milestone 5, video design section 8.2). It also finds where the runner's clock sits in
///     the recording, so assembly can put each clip and card at the right place.
/// </summary>
public sealed class ScreenRecorder : IDisposable
{
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(15);

    private readonly Process _process;
    private readonly Task<string> _errors;
    private readonly TaskCompletionSource<TimeSpan> _offset = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private ScreenRecorder(Process process, Stopwatch clock)
    {
        _process = process;
        _errors = process.StandardError.ReadToEndAsync();
        _ = Task.Run(() => ReadProgress(clock));
    }

    /// <summary>The recording file.</summary>
    public required string Path { get; init; }

    /// <summary>
    ///     Runner clock time at the first frame of the recording. A time t on the runner's
    ///     clock is at t - ClockOffset in the recording.
    /// </summary>
    public TimeSpan ClockOffset { get; private set; }

    /// <summary>Starts recording the area and waits until the first frames are written.</summary>
    public static ScreenRecorder Start(Ffmpeg ffmpeg, Rectangle area, string path, Stopwatch clock)
    {
        var arguments = new[]
        {
            "-hide_banner", "-loglevel", "error", "-y",
            "-f", "gdigrab", "-framerate", "30",
            "-offset_x", area.X.ToString(CultureInfo.InvariantCulture),
            "-offset_y", area.Y.ToString(CultureInfo.InvariantCulture),
            "-video_size", $"{area.Width}x{area.Height}",
            "-i", "desktop",
            // zerolatency keeps frames out of the encoder's queue, so the progress times below
            // match the frames that are on screen now.
            "-c:v", "libx264", "-preset", "ultrafast", "-tune", "zerolatency", "-crf", "15", "-pix_fmt", "yuv420p",
            "-progress", "pipe:1", "-stats_period", "0.1",
            path,
        };
        var process = Process.Start(ffmpeg.StartInfo(arguments, System.IO.Path.GetDirectoryName(path)!))!;
        var recorder = new ScreenRecorder(process, clock) { Path = path };
        bool started;
        try
        {
            started = recorder._offset.Task.Wait(StartTimeout);
        }
        catch (AggregateException)
        {
            started = false;
        }

        if (!started)
        {
            var detail = recorder._errors.IsCompleted ? recorder._errors.Result.Trim() : "no frame after 15 s";
            recorder.Dispose();
            throw new InvalidOperationException($"ffmpeg did not start recording: {detail}");
        }

        recorder.ClockOffset = recorder._offset.Task.Result;
        return recorder;
    }

    /// <summary>Stops the recording and lets ffmpeg finish the file.</summary>
    public void Stop()
    {
        if (_process.HasExited)
        {
            return;
        }

        // "q" is ffmpeg's own stop key; it writes the end of the file before it exits.
        _process.StandardInput.Write('q');
        _process.StandardInput.Flush();
        if (!_process.WaitForExit(TimeSpan.FromSeconds(30)))
        {
            _process.Kill();
            throw new InvalidOperationException("ffmpeg did not stop recording within 30 s.");
        }

        _errors.Wait();
        if (_process.ExitCode != 0)
        {
            throw new InvalidOperationException($"ffmpeg recording ended with code {_process.ExitCode}: {_errors.Result.Trim()}");
        }
    }

    public void Dispose()
    {
        if (!_process.HasExited)
        {
            _process.Kill();
            // Wait, so the recording file is free to delete when Dispose returns.
            _process.WaitForExit(TimeSpan.FromSeconds(10));
        }

        _process.Dispose();
    }

    private void ReadProgress(Stopwatch clock)
    {
        string? line;
        while ((line = _process.StandardOutput.ReadLine()) is not null)
        {
            var now = clock.Elapsed;
            if (!_offset.Task.IsCompleted && ParseOutTime(line) is { } outTime && outTime > TimeSpan.Zero)
            {
                _offset.TrySetResult(now - outTime);
            }
        }

        _offset.TrySetException(new InvalidOperationException("ffmpeg stopped before it recorded a frame."));
    }

    /// <summary>The out_time_us value of an ffmpeg -progress line, or null for any other line.</summary>
    internal static TimeSpan? ParseOutTime(string line)
    {
        const string key = "out_time_us=";
        return line.StartsWith(key, StringComparison.Ordinal)
               && long.TryParse(line.AsSpan(key.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var us)
            ? TimeSpan.FromMicroseconds(us)
            : null;
    }
}
