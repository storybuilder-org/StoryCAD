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

    // Collaborator #237 item 3: the run wrote its properties in this order, each from the
    // ones before it. The snapshot and the chat rules follow it.
    private readonly List<string> _order = new();

    public int Count => _entries.Count;

    public bool ContainsKey(string key) => _entries.ContainsKey(key);

    public Entry? Get(string key) =>
        _entries.TryGetValue(key, out var e) ? e : null;

    public IEnumerable<Entry> All => _order.Select(k => _entries[k]);

    /// <summary>Keys in the order the run wrote them.</summary>
    public IReadOnlyList<string> OrderedKeys => _order;

    /// <summary>Replace session set from a new workflow extract (open rows).</summary>
    public void ReplaceFromPending(
        IEnumerable<PendingUpdate> pending,
        Func<Guid, string?>? resolveElementName = null)
    {
        _entries.Clear();
        _order.Clear();
        foreach (var u in pending)
        {
            // Keep full outline value at classify time — needed after Skip when the writer
            // asks "what is this field now?" (outline text, not a truncated proposal).
            var outline = u.CurrentDisplay ?? string.Empty;
            if (!_entries.ContainsKey(u.Key))
                _order.Add(u.Key);
            _entries[u.Key] = new Entry(
                u, ProposalSessionStatus.Open, FormatValue(u.Value, resolveElementName), outline);
        }
    }

    /// <summary>
    /// Collaborator #237 item 5: the model does not always spell a patch key as
    /// "ElementLabel.Property". Accept the exact key, a bare property name that matches one
    /// entry, or a "Label.Property" that matches one entry on its property part. Null when
    /// nothing matches or the bare name is ambiguous.
    /// </summary>
    public string? ResolveKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var wanted = key.Trim();
        if (_entries.TryGetValue(wanted, out var exact))
            return exact.Update.Key;

        var property = wanted.Contains('.') ? wanted[(wanted.LastIndexOf('.') + 1)..] : wanted;
        var matches = _order
            .Where(k => string.Equals(_entries[k].Update.Spec.Property, property, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(_entries[k].DisplayName, property, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return matches.Count == 1 ? matches[0] : null;
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
    public bool TryApplyPatch(string key, string newValue, out bool reopened) =>
        TryApplyPatch(key, newValue, out reopened, out _);

    /// <summary>
    /// Apply a chat patch and say whether the text differs from what the list already shows.
    /// Collaborator #237: the Scorecard chat of 2026-09-05 re-sent the text already in the
    /// list, and the app answered "Changed Concept, Premise." over a list that looked the same.
    /// <paramref name="changed"/> is false when the patch text equals the current proposed
    /// text after line-ending and edge-whitespace normalization.
    /// </summary>
    public bool TryApplyPatch(string key, string newValue, out bool reopened, out bool changed)
    {
        reopened = false;
        changed = false;
        var resolved = ResolveKey(key);
        if (resolved == null || !_entries.TryGetValue(resolved, out var e))
            return false;
        key = resolved;

        reopened = e.Status is ProposalSessionStatus.Accepted or ProposalSessionStatus.Skipped;
        object? patched = newValue;
        var proposedText = newValue ?? string.Empty;
        if (e.Update.Value is BeatRowValue row)
        {
            // Collaborator #217 section 5.7: a beat row is typed. Chat may rename the stub a
            // Create row makes; a Bind row names an outline element and takes no free text.
            if (row.BindGuid.HasValue)
                return false;
            patched = row with { Row = row.Row with { SceneName = newValue } };
            proposedText = FormatValue(patched);
        }
        changed = !string.Equals(Normalize(proposedText), Normalize(e.ProposedText), StringComparison.Ordinal);
        var updatedUpdate = e.Update with { Value = patched };
        _entries[key] = e with
        {
            Update = updatedUpdate,
            ProposedText = proposedText,
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
        sb.AppendLine("Property proposals for this workflow run, in the order the run wrote them.");
        sb.AppendLine("For each proposal: its name, status, patch key, Collaborator proposed text, and outline value when the run classified the field.");
        sb.AppendLine("If status is skipped, the writer kept the outline value (use Outline for \"as it is now\").");
        sb.AppendLine("If status is accepted, the outline should match what was accepted (use Proposed if Outline empty).");
        sb.AppendLine();
        foreach (var e in All)
        {
            var status = e.Status switch
            {
                ProposalSessionStatus.Open => "open",
                ProposalSessionStatus.Accepted => "accepted",
                ProposalSessionStatus.Skipped => "skipped",
                _ => "?"
            };
            sb.AppendLine($"### {e.DisplayName} [{status}] (patch key: {e.Update.Key})");
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

    private static string Normalize(string? text) =>
        (text ?? string.Empty).Replace("\r\n", "\n").Trim();

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
        BuildSystemInstructions(workflowTitle, Array.Empty<string>());

    /// <summary>
    /// Chat rules. <paramref name="orderedNames"/> lists the proposals in the order the run
    /// wrote them (Collaborator #237 item 3): a change to one re-derives every proposal after
    /// it. One name or none adds no order rule.
    /// Collaborator #237: the Scorecard chat of 2026-09-05 told the writer "I can't see or
    /// control your patch system" and asked the writer to apply the patches. The model was
    /// never told that the app applies its patches JSON to the list. The mechanism is stated
    /// first, and the rules forbid handing that work back to the writer.
    /// </summary>
    public static string BuildSystemInstructions(string workflowTitle, IReadOnlyList<string> orderedNames)
    {
        var text =
            "You help revise property proposals from the '" + workflowTitle + "' workflow run. " +
            "This is not general story chat and you do not have the full outline loaded beyond the snapshot.\n\n" +
            "How this chat works:\n" +
            "- The snapshot is the proposal list the writer sees on screen. For each proposal it gives: its name, status, patch key, Proposed (Collaborator), and Outline (story element when classified).\n" +
            "- The app reads the patches JSON out of your reply, replaces the proposal text in the list with each patch value, and hides the JSON from the writer. Your patch is the change. Nothing else changes the list.\n" +
            "- After the app applies a patch it adds an \"Updated property proposals\" snapshot to this conversation. The latest snapshot is what the writer sees now.\n\n" +
            "Rules:\n" +
            "- Answer the writer in plain text only (no Markdown).\n" +
            "- When you talk to the writer, call a proposal by its name (for example Concept). Do not show the writer a patch key or a status word. The patch key belongs only inside the patches JSON.\n" +
            "- If the writer wants a field changed, include a JSON block (optionally fenced) of the form: " +
            "{\"patches\":[{\"key\":\"ElementLabel.Property\",\"value\":\"new text\"}]}. " +
            "The value is the complete new text for that proposal, ready to write to the outline: not a summary of the change, not a diff. " +
            "Keep line breaks as \\n inside the JSON string. A reply without patches JSON changes nothing.\n" +
            "- Tell the writer in one or two sentences what you changed and why. " +
            "Never tell the writer to apply, paste, re-run, or accept a patch; the app already applied it. " +
            "Never say you cannot see or change the list; the latest snapshot is the list.\n" +
            "- If the writer says a change is not there, compare the request with the latest snapshot. " +
            "If the snapshot already has the change, say which proposal has it and quote the changed sentence. " +
            "If it does not, send the patch again with the complete new text.\n" +
            "- If the writer asks what a field is \"now\" and status is skipped, quote the full Outline text from the snapshot.\n" +
            "- If status is open, quote Proposed (and Outline if they ask what is already on the element).\n" +
            "- If status is accepted, prefer Proposed as what was written.\n" +
            "- You may only change proposals listed in the snapshot.\n";
        // Collaborator #237: the Scorecard chat of 2026-09-05 was asked to make the mother the
        // protagonist. The run's Description proposal named a stadium employee, the order rule
        // said later proposals follow earlier ones, and the model kept the employee. The
        // writer's request outranks the proposals, and a role change starts where the role is set.
        text +=
            "- The writer's request outranks every proposal in the snapshot. " +
            "If the request contradicts an earlier proposal (for example who the protagonist is, what they want, or what happens), " +
            "rewrite that earlier proposal too, starting at the first proposal that states it, and then every proposal after it. " +
            "Do not keep a role, a name, or an event from an earlier proposal that the writer asked you to change.\n";
        if (orderedNames.Count > 1)
        {
            text +=
                "- The run wrote the proposals in this order, each from the ones before it: " +
                string.Join(", ", orderedNames) + ". " +
                "When you change one, also rewrite every proposal after it in that order so it follows from the change, " +
                "and include a patch for each proposal you rewrote.\n";
        }
        text +=
            "- If the request is not about those proposals, say clearly it is out of scope for this chat. " +
            "Do not invent patches. Suggest Accept, Try Again, another workflow, or editing the outline.\n" +
            "- Do not invent element IDs or properties outside the proposal list. Do not claim text is missing if Outline or Proposed is present in the snapshot.";
        return text;
    }

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
        string OutlineText)
    {
        /// <summary>The name the pane shows for this proposal.</summary>
        public string DisplayName => Update.DisplayNameOverride ?? Update.Spec.Property;
    }
}
