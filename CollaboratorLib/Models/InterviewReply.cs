using System;
using System.Text.RegularExpressions;

namespace StoryCollaborator.Models;

/// <summary>What the interviewer decided about the answer it was just given (#119).</summary>
public enum InterviewVerdict
{
    /// <summary>The writer answered. Move to the next line the client handed over.</summary>
    Answered,

    /// <summary>A non-answer. Ask the same line again with its premise intact.</summary>
    Retry,

    /// <summary>The answer offered a concrete detail worth one question. Position holds.</summary>
    FollowUp,

    /// <summary>Every block is answered.</summary>
    Done
}

/// <summary>
/// One reply from the interviewer (#119), split into its verdict and the question the
/// writer actually sees.
///
/// The verdict is all the model reports, because it is the only thing the client cannot
/// work out for itself. Position is arithmetic and lives in <see cref="Workflows.InterviewScript"/>:
/// an earlier build had the model report a field and line with each question and it drifted
/// out of step with the question it was actually asking.
/// </summary>
public sealed record InterviewReply(InterviewVerdict Verdict, string Question)
{
    // Tolerant of spacing and case. A header that arrives slightly malformed should cost a
    // verdict, not leave "VERDICT: ANSWERED" on screen in front of the writer.
    private static readonly Regex HeaderPattern = new(
        @"^\s*VERDICT\s*:\s*(?<verdict>[A-Za-z-]+)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Parses a reply. A missing or unreadable header is read as Answered: the interview
    /// walks on rather than stalling on the same question, which is the failure a writer
    /// cannot get out of.
    /// </summary>
    public static InterviewReply Parse(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return new InterviewReply(InterviewVerdict.Answered, string.Empty);

        var lines = reply.Replace("\r\n", "\n").Split('\n');
        var match = HeaderPattern.Match(lines[0]);

        if (!match.Success)
            return new InterviewReply(InterviewVerdict.Answered, reply.Trim());

        var body = string.Join("\n", lines, 1, lines.Length - 1).Trim();
        var verdict = match.Groups["verdict"].Value.Replace("-", string.Empty);

        return new InterviewReply(
            Enum.TryParse<InterviewVerdict>(verdict, ignoreCase: true, out var parsed)
                ? parsed
                : InterviewVerdict.Answered,
            body);
    }
}
