using StoryCollaborator.Models;

namespace StoryCollaborator.Services;

/// <summary>
/// Collaborator #140 (#116 rev): Protect overwrites require confirmation.
/// Free updates apply without a dialog. Protect Accepts are staged during Review Each
/// / per-row Accept; one confirm runs when the pending queue is fully decided.
/// </summary>
public sealed class OverwriteAcceptanceSession
{
    private readonly List<PendingUpdate> _stagedProtect = new();

    public IReadOnlyList<PendingUpdate> StagedProtect => _stagedProtect;

    public int StagedCount => _stagedProtect.Count;

    public bool HasStaged => _stagedProtect.Count > 0;

    /// <summary>Stage a Protect update for later confirm+apply (idempotent by key).</summary>
    public void StageProtect(PendingUpdate update)
    {
        if (update.Kind != UpdateKind.Protect)
            throw new ArgumentException("Only Protect updates may be staged.", nameof(update));

        if (_stagedProtect.Any(u =>
                string.Equals(u.Key, update.Key, StringComparison.OrdinalIgnoreCase)))
            return;

        _stagedProtect.Add(update);
    }

    public void ClearStage() => _stagedProtect.Clear();

    /// <summary>
    /// True when every pending row has been decided (list empty) and Protect accepts are waiting.
    /// </summary>
    public bool ShouldConfirmStaged(int remainingPendingCount) =>
        remainingPendingCount == 0 && _stagedProtect.Count > 0;

    /// <summary>Partition pending into free (Accept All may apply) vs Protect.</summary>
    public static (List<PendingUpdate> Free, List<PendingUpdate> Protect) Partition(
        IEnumerable<PendingUpdate> pending)
    {
        var free = new List<PendingUpdate>();
        var protect = new List<PendingUpdate>();
        foreach (var u in pending)
        {
            if (u.Kind == UpdateKind.Protect)
                protect.Add(u);
            else if (u.AcceptAllMayApply)
                free.Add(u);
            else
                free.Add(u); // Unclassified treated as free
        }
        return (free, protect);
    }

    /// <summary>Dialog body for Confirm overwrite of Protect fields.</summary>
    public static string BuildConfirmMessage(IReadOnlyList<PendingUpdate> protect)
    {
        if (protect == null || protect.Count == 0)
            return "No fields need overwrite confirmation.";

        var sb = new System.Text.StringBuilder();
        sb.Append(protect.Count == 1
            ? "This field already has your content. Replace it with Collaborator's proposal?"
            : $"These {protect.Count} fields already have your content. Replace them with Collaborator's proposals?");
        sb.AppendLine();
        sb.AppendLine();
        foreach (var u in protect)
            sb.AppendLine($"• {u.Key}");
        return sb.ToString().TrimEnd();
    }
}
