using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace StoryCADAutomation.Video;

/// <summary>
///     Runs the ffmpeg program named by STORYCAD_FFMPEG (#1421 Milestone 5, video design
///     section 10). The runner never searches PATH for it.
/// </summary>
public sealed partial class Ffmpeg(string exePath)
{
    public const string PathVariable = "STORYCAD_FFMPEG";

    public string ExePath { get; } = exePath;

    /// <summary>The ffmpeg in STORYCAD_FFMPEG, or null with a reason when it is not set or not a file.</summary>
    public static Ffmpeg? FromEnvironment(out string? problem)
    {
        var path = Environment.GetEnvironmentVariable(PathVariable);
        problem = string.IsNullOrWhiteSpace(path) ? $"{PathVariable} is not set."
            : !File.Exists(path) ? $"{PathVariable} names '{path}', which is not a file."
            : null;
        return problem is null ? new Ffmpeg(path!) : null;
    }

    /// <summary>A process for ffmpeg with these arguments, in this working folder.</summary>
    public ProcessStartInfo StartInfo(IEnumerable<string> arguments, string workingDirectory)
    {
        var info = new ProcessStartInfo(ExePath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        return info;
    }

    /// <summary>Runs ffmpeg to the end. A non-zero exit throws with the last lines ffmpeg wrote.</summary>
    public void Run(IEnumerable<string> arguments, string workingDirectory)
    {
        using var process = Process.Start(StartInfo(arguments, workingDirectory))!;
        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEnd();
        process.WaitForExit();
        output.Wait();
        if (process.ExitCode != 0)
        {
            var tail = string.Join(Environment.NewLine, errors.Split('\n').TakeLast(15));
            throw new InvalidOperationException($"ffmpeg exited with code {process.ExitCode}:{Environment.NewLine}{tail}");
        }
    }

    /// <summary>The length of a media file, from the Duration line that ffmpeg prints for an input.</summary>
    public TimeSpan? Duration(string mediaPath)
    {
        using var process = Process.Start(StartInfo(new[] { "-hide_banner", "-i", mediaPath }, Path.GetTempPath()))!;
        process.StandardInput.Close();
        var errors = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return ParseDuration(errors);
    }

    internal static TimeSpan? ParseDuration(string ffmpegOutput)
    {
        var match = DurationLine().Match(ffmpegOutput);
        return match.Success
            ? TimeSpan.ParseExact(match.Groups[1].Value, @"hh\:mm\:ss\.FF", CultureInfo.InvariantCulture)
            : null;
    }

    [GeneratedRegex(@"Duration: (\d\d:\d\d:\d\d\.\d+)")]
    private static partial Regex DurationLine();
}
