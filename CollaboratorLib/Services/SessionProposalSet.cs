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
    public void ReplaceFromPending(
        IEnumerable<PendingUpdate> pending,
        Func<Guid, string?>? resolveElementName = null)
    {
        _entries.Clear();
        foreach (var u in pending)
        {
            // Keep full outline value at classify time — needed after Skip when the writer
            // asks "what is this field now?" (outline text, not a truncated proposal).
            var outline = u.CurrentDisplay ?? string.Empty;
            _entries[u.Key] = new Entry(
                u, ProposalSessionStatus.Open, FormatValue(u.Value, resolveElementName), outline);
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

    /// <summary>
    /// Open rows for Accept. Keep typed <see cref="PendingUpdate.Value"/> (e.g. List&lt;string&gt; for
    /// SimpleList). Do not replace with ProposedText — that is display/chat only; List.ToString() is garbage.
    /// Chat patches already store the new text on <see cref="PendingUpdate.Value"/> via TryApplyPatch.
    /// </summary>
    public IEnumerable<PendingUpdate> OpenAsPendingUpdates() =>
        _entries.Values
            .Where(e => e.Status == ProposalSessionStatus.Open)
            .Select(e => e.Update);

    /// <summary>
    /// Snapshot for chat history: full proposed text and full outline value at capture time.
    /// After Skip, "as it is now" is OutlineText (what stayed on the story element).
    /// </summary>
    public string BuildSnapshotText(int maxValueChars = 0)
    {
        if (_entries.Count == 0)
            return "(No proposals in this session.)";

        var sb = new StringBuilder();
        sb.AppendLine("Property proposals for this workflow run.");
        sb.AppendLine("For each key: status, Collaborator proposed text, and outline value when the run classified the field.");
        sb.AppendLine("If status is skipped, the writer kept the outline value (use Outline for \"as it is now\").");
        sb.AppendLine("If status is accepted, the outline should match what was accepted (use Proposed if Outline empty).");
        sb.AppendLine();
        foreach (var e in _entries.Values.OrderBy(x => x.Update.Key, StringComparer.OrdinalIgnoreCase))
        {
            var status = e.Status switch
            {
                ProposalSessionStatus.Open => "open",
                ProposalSessionStatus.Accepted => "accepted",
                ProposalSessionStatus.Skipped => "skipped",
                _ => "?"
            };
            sb.AppendLine($"### {e.Update.Key} [{status}]");
            sb.AppendLine("Proposed (Collaborator):");
            sb.AppendLine(Cap(e.ProposedText, maxValueChars));
            sb.AppendLine("Outline (value on the story element when classified):");
            sb.AppendLine(string.IsNullOrEmpty(e.OutlineText)
                ? "(empty)"
                : Cap(e.OutlineText, maxValueChars));
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private static string Cap(string? text, int maxValueChars)
    {
        var val = text ?? string.Empty;
        if (maxValueChars > 0 && val.Length > maxValueChars)
            return val.Substring(0, maxValueChars) + "…";
        return val;
    }

    /// <summary>Open count (still need Accept/Skip on the left list).</summary>
    public int OpenCount =>
        _entries.Values.Count(e => e.Status == ProposalSessionStatus.Open);

    public static string BuildSystemInstructions(string workflowTitle) =>
        "You help revise property proposals from the '" + workflowTitle + "' workflow run. " +
        "This is not general story chat and you do not have the full outline loaded beyond the snapshot.\n\n" +
        "Rules:\n" +
        "- Answer the writer in plain text only (no Markdown).\n" +
        "- The snapshot gives, for each key: status, Proposed (Collaborator), and Outline (story element when classified).\n" +
        "- If the writer asks what a field is \"now\" and status is skipped, quote the full Outline text from the snapshot.\n" +
        "- If status is open, quote Proposed (and Outline if they ask what is already on the element).\n" +
        "- If status is accepted, prefer Proposed as what was written.\n" +
        "- You may only change keys listed in the property proposals snapshot.\n" +
        "- If the writer wants a field changed, include a JSON block (optionally fenced) of the form: " +
        "{\"patches\":[{\"key\":\"ElementLabel.Property\",\"value\":\"new text\"}]}\n" +
        "- If the request is not about those proposals, say clearly it is out of scope for this chat. " +
        "Do not invent patches. Suggest Accept, Try Again, another workflow, or editing the outline.\n" +
        "- Do not invent element IDs or properties outside the proposal list. Do not claim text is missing if Outline or Proposed is present in the snapshot.";

    /// <summary>
    /// Human-readable proposal text. Must not use object.ToString() on lists
    /// (leaks "System.Collections.Generic.List`1[System.String]").
    /// </summary>
    private static string FormatValue(object? value, Func<Guid, string?>? resolveElementName = null) =>
        ValueDisplay.Format(value, resolveElementName);

    public sealed record Entry(
        PendingUpdate Update,
        ProposalSessionStatus Status,
        string ProposedText,
        string OutlineText);
}
