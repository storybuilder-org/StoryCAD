using System.Text.RegularExpressions;

namespace StoryCollaborator.Models;

/// <summary>What the interviewer decided about the answer it was just given (#119).</summary>
public enum InterviewVerdict
{
    /// <summary>Still on this field.</summary>
    KeepAsking,

    /// <summary>Enough to leave this field.</summary>
    GotIt,

    /// <summary>Refused, or there is not one. Leave the field.</summary>
    NotThis,

    /// <summary>Interview complete.</summary>
    Done
}

/// <summary>
/// One reply from the interviewer (#119). The verdict is all the model reports.
/// Field is owned by <see cref="Workflows.InterviewScript"/>, not by this parse.
/// </summary>
public sealed record InterviewReply(InterviewVerdict Verdict, string Question)
{
    private static readonly Regex HeaderPattern = new(
        @"^\s*VERDICT\s*:\s*(?<verdict>[A-Za-z-]+)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Parses a reply. Missing header, unknown token, and empty reply are Keep asking.
    /// Does not use Enum.TryParse as the product map.
    /// </summary>
    public static InterviewReply Parse(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return new InterviewReply(InterviewVerdict.KeepAsking, string.Empty);

        var lines = reply.Replace("\r\n", "\n").Split('\n');
        var match = HeaderPattern.Match(lines[0]);

        if (!match.Success)
            return new InterviewReply(InterviewVerdict.KeepAsking, reply.Trim());

        var token = match.Groups["verdict"].Value.Replace("-", string.Empty).ToUpperInvariant();
        var body = string.Join("\n", lines, 1, lines.Length - 1).Trim();

        var verdict = token switch
        {
            "KEEP" or "RETRY" or "FOLLOWUP" => InterviewVerdict.KeepAsking,
            "GOTIT" or "ANSWERED" => InterviewVerdict.GotIt,
            "NOTTHIS" => InterviewVerdict.NotThis,
            "DONE" => InterviewVerdict.Done,
            _ => InterviewVerdict.KeepAsking
        };

        return new InterviewReply(verdict, body);
    }

    /// <summary>Empty body never changes Field, including Got it.</summary>
    public static bool ShouldApply(bool opening, string question) =>
        !opening && !string.IsNullOrWhiteSpace(question);
}
