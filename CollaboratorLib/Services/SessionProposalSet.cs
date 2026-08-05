using System.Text;
using StoryCollaborator.Models;

namespace StoryCollaborator.Services;

/// <summary>Status of one proposal in the chat session set (#145).</summary>
public enum ProposalSessionStatus
{
    Open,
    Accepted,
    Skipped
}

/// <summary>
/// Full proposal set from the last workflow run for the chat session (#145).
/// Keys survive Accept/Skip; chat may patch any key; unknown keys are rejected.
/// </summary>
public sealed class SessionProposalSet
{
    private readonly Dictionary<string, Entry> _entries =
        new(StringComparer.OrdinalIgnoreCase);

    public int Count => _entries.Count;

    public bool ContainsKey(string key) => _entries.ContainsKey(key);

    public Entry? Get(string key) =>
        _entries.TryGetValue(key, out var e) ? e : null;

    public IEnumerable<Entry> All => _entries.Values;

    /// <summary>Replace session set from a new workflow extract (open rows).</summary>
    public void ReplaceFromPending(IEnumerable<PendingUpdate> pending)
    {
        _entries.Clear();
        foreach (var u in pending)
        {
            _entries[u.Key] = new Entry(u, ProposalSessionStatus.Open, FormatValue(u.Value));
        }
    }

    public void MarkAccepted(string key)
    {
        if (_entries.TryGetValue(key, out var e))
            _entries[key] = e with { Status = ProposalSessionStatus.Accepted };
    }

    public void MarkSkipped(string key)
    {
        if (_entries.TryGetValue(key, out var e))
            _entries[key] = e with { Status = ProposalSessionStatus.Skipped };
    }

    /// <summary>
    /// Apply a chat patch. Re-opens Accepted/Skipped as Open.
    /// Returns false if key is not in the session set.
    /// </summary>
    public bool TryApplyPatch(string key, string newValue, out bool reopened)
    {
        reopened = false;
        if (!_entries.TryGetValue(key, out var e))
            return false;

        reopened = e.Status is ProposalSessionStatus.Accepted or ProposalSessionStatus.Skipped;
        var updatedUpdate = e.Update with { Value = newValue };
        _entries[key] = e with
        {
            Update = updatedUpdate,
            ProposedText = newValue ?? string.Empty,
            Status = ProposalSessionStatus.Open
        };
        return true;
    }

    public IEnumerable<PendingUpdate> OpenAsPendingUpdates() =>
        _entries.Values
            .Where(e => e.Status == ProposalSessionStatus.Open)
            .Select(e => e.Update with { Value = e.ProposedText });

    /// <summary>
    /// Plain-text snapshot for chat history seed / refresh.
    /// Full proposal text (high cap) so "show me Description" works after Skip (#145 UX).
    /// </summary>
    public string BuildSnapshotText(int maxValueChars = 8000)
    {
        if (_entries.Count == 0)
            return "(No proposals in this session.)";

        var sb = new StringBuilder();
        sb.AppendLine("Property proposals for this workflow run (full text; status open/accepted/skipped):");
        foreach (var e in _entries.Values.OrderBy(x => x.Update.Key, StringComparer.OrdinalIgnoreCase))
        {
            var status = e.Status switch
            {
                ProposalSessionStatus.Open => "open",
                ProposalSessionStatus.Accepted => "accepted",
                ProposalSessionStatus.Skipped => "skipped",
                _ => "?"
            };
            var val = e.ProposedText ?? string.Empty;
            if (maxValueChars > 0 && val.Length > maxValueChars)
                val = val.Substring(0, maxValueChars) + "…";
            // Keep newlines for long sketches; model needs readable prose
            sb.AppendLine($"- {e.Update.Key} [{status}]:");
            sb.AppendLine(val);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Open count (still need Accept/Skip on the left list).</summary>
    public int OpenCount =>
        _entries.Values.Count(e => e.Status == ProposalSessionStatus.Open);

    public static string BuildSystemInstructions(string workflowTitle) =>
        "You help revise property proposals from the '" + workflowTitle + "' workflow run. " +
        "This is not general story chat and you do not have the full outline loaded.\n\n" +
        "Rules:\n" +
        "- Answer the writer in plain text only (no Markdown).\n" +
        "- You may only change keys listed in the property proposals snapshot.\n" +
        "- If the writer wants a field changed, include a JSON block (optionally fenced) of the form: " +
        "{\"patches\":[{\"key\":\"ElementLabel.Property\",\"value\":\"new text\"}]}\n" +
        "- If the request is not about those proposals, say clearly it is out of scope for this chat. " +
        "Do not invent patches. Suggest Accept, Try Again, another workflow, or editing the outline.\n" +
        "- Do not invent element IDs or properties outside the proposal list.";

    private static string FormatValue(object? value) =>
        value switch
        {
            null => string.Empty,
            string s => s,
            _ => value.ToString() ?? string.Empty
        };

    public sealed record Entry(
        PendingUpdate Update,
        ProposalSessionStatus Status,
        string ProposedText);
}
