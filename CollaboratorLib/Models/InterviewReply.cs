using System;
using System.Collections.Generic;
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
/// One reply from the interviewer (#119). The verdict is all the model reports on an
/// ordinary turn. On a "you choose" opening it also reports the targets it picked
/// (design section 25.5); the client validates those against the ids it knows. Field
/// is owned by the client's plan, not by this parse.
/// </summary>
public sealed record InterviewReply(
    InterviewVerdict Verdict,
    string Question,
    IReadOnlyList<string> Targets)
{
    public InterviewReply(InterviewVerdict verdict, string question)
        : this(verdict, question, Array.Empty<string>())
    {
    }

    // The token may carry spaces or hyphens ("GOT IT", "NOT-THIS"); both are stripped
    // before the table lookup. Anything else on the header line fails the match.
    private static readonly Regex HeaderPattern = new(
        @"^\s*VERDICT\s*:\s*(?<verdict>[A-Za-z][A-Za-z -]*?)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // The stance puts the TARGETS line second, but wherever it lands it is bookkeeping
    // the writer must never see (review, 2026-09-07: one left in the body would have
    // been posted to the chat and saved into the Note). Lifted from any line.
    private static readonly Regex TargetsPattern = new(
        @"^\s*TARGETS\s*:\s*(?<ids>.*?)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Commas and semicolons only: a spaced header ("Back story") is one token, and
    // InterviewScript.Canonical resolves it. Splitting on spaces shredded it.
    private static readonly char[] IdSeparators = { ',', ';' };

    /// <summary>
    /// Parses a reply. Missing header, unknown token, and empty reply are Keep asking.
    /// Does not use Enum.TryParse as the product map. Leading blank lines are ignored:
    /// the header must be the first line with text on it, not the first line. A TARGETS
    /// line directly after the header is lifted out of the question.
    /// </summary>
    public static InterviewReply Parse(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return new InterviewReply(InterviewVerdict.KeepAsking, string.Empty);

        var lines = reply.TrimStart().Replace("\r\n", "\n").Split('\n');
        var match = HeaderPattern.Match(lines[0]);

        if (!match.Success)
            return new InterviewReply(InterviewVerdict.KeepAsking, reply.Trim());

        var token = match.Groups["verdict"].Value
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToUpperInvariant();

        var rest = new List<string>(lines.Length - 1);
        for (var i = 1; i < lines.Length; i++)
            rest.Add(lines[i]);

        IReadOnlyList<string> targets = Array.Empty<string>();
        var targetsAt = rest.FindIndex(line => TargetsPattern.IsMatch(line));
        if (targetsAt >= 0)
        {
            targets = TargetsPattern.Match(rest[targetsAt]).Groups["ids"].Value
                .Split(IdSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            rest.RemoveAt(targetsAt);
        }

        var body = string.Join("\n", rest).Trim();

        var verdict = token switch
        {
            "KEEP" or "RETRY" or "FOLLOWUP" => InterviewVerdict.KeepAsking,
            "GOTIT" or "ANSWERED" => InterviewVerdict.GotIt,
            "NOTTHIS" => InterviewVerdict.NotThis,
            "DONE" => InterviewVerdict.Done,
            _ => InterviewVerdict.KeepAsking
        };

        return new InterviewReply(verdict, body, targets);
    }

    /// <summary>Empty body never changes Field, including Got it.</summary>
    public static bool ShouldApply(bool opening, string question) =>
        !opening && !string.IsNullOrWhiteSpace(question);

    /// <summary>
    /// True when a reply body that should have been a closing line is a question. The
    /// sample-outline runs of 2026-09-07 got one on the last field twice in two runs.
    /// The stance now forbids it; this is the client's guard, so the writer is never
    /// shown a question they cannot answer with the chat already closed.
    /// </summary>
    public static bool LooksLikeAQuestion(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.TrimEnd().EndsWith('?');

    /// <summary>
    /// True when a reply body is the closing line. On a turn with a next field still to
    /// go that is the interviewer leaving the interview instead of the field (the retest
    /// of 2026-09-07 saw it on a second refusal), and the writer must not be told to save.
    /// A question is never a close, however it is worded: "do you still believe you can
    /// save it?" is a question about a marriage.
    /// </summary>
    public static bool LooksLikeAClose(string? text) =>
        !string.IsNullOrWhiteSpace(text)
        && !LooksLikeAQuestion(text)
        && (text.Contains("end of the interview", StringComparison.OrdinalIgnoreCase)
            || text.Contains("you can save it", StringComparison.OrdinalIgnoreCase));
}
