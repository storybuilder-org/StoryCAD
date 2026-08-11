using StoryCADLib.DAL;
using StoryCADLib.Models.Tools;

namespace StoryCADLib.Services.Collaborator;

/// <summary>
///     Owns the set of starred Collaborator workflows: which workflows appear in the short band
///     at the top of the Collaborator navigation pane, ahead of the collapsed element-type groups.
///     Stars are stored as registry labels in <see cref="PreferencesModel.StarredCollaboratorWorkflows" />
///     and persisted to Preferences.json, so they survive a restart. Collaborator settings do not —
///     they are session state — which is why stars do not live there.
/// </summary>
public class WorkflowStarService
{
    private readonly ILogService _log;
    private readonly PreferenceService _preferenceService;
    private readonly Func<PreferencesModel, Task> _writePreferences;

    public WorkflowStarService(PreferenceService preferenceService, ILogService log)
        : this(preferenceService, log, null)
    {
    }

    /// <summary>
    ///     Test seam: <paramref name="writePreferences" /> defaults to
    ///     <see cref="PreferencesIo.WritePreferences" />, so tests can observe persistence without
    ///     a second disk-writing path. Mirrors
    ///     <see cref="PreferenceService.EnsureUserGuidProvisionedAsync" />.
    /// </summary>
    public WorkflowStarService(
        PreferenceService preferenceService,
        ILogService log,
        Func<PreferencesModel, Task> writePreferences)
    {
        _preferenceService = preferenceService;
        _log = log;
        _writePreferences = writePreferences;
    }

    /// <summary>
    ///     Returns the user's starred workflow labels, seeding the defaults the first time.
    ///     The seed runs once per user, gated by
    ///     <see cref="PreferencesModel.CollaboratorStarDefaultsApplied" />; afterwards the stored
    ///     list is returned as-is, including when it is empty.
    /// </summary>
    /// <param name="defaultLabels">
    ///     The default starred set, supplied by the caller because the workflow registry lives in
    ///     CollaboratorLib and StoryCADLib does not reference it.
    /// </param>
    public async Task<IReadOnlyList<string>> GetStarredAsync(IEnumerable<string> defaultLabels)
    {
        var model = _preferenceService.Model;
        model.StarredCollaboratorWorkflows ??= new List<string>();

        if (model.CollaboratorStarDefaultsApplied)
        {
            return model.StarredCollaboratorWorkflows.ToList();
        }

        model.StarredCollaboratorWorkflows = Normalize(defaultLabels);
        model.CollaboratorStarDefaultsApplied = true;
        await PersistAsync(model);
        _log?.Log(LogLevel.Info,
            $"Seeded {model.StarredCollaboratorWorkflows.Count} default starred Collaborator workflows.");

        return model.StarredCollaboratorWorkflows.ToList();
    }

    /// <summary>
    ///     Replaces the starred set and persists it. An empty set is a legitimate choice and is
    ///     stored as such — the defaults are never re-seeded over it.
    /// </summary>
    public async Task SetStarredAsync(IEnumerable<string> labels)
    {
        var model = _preferenceService.Model;
        model.StarredCollaboratorWorkflows = Normalize(labels);
        // A user who curated the set has by definition been through the seed, even if they got
        // here before GetStarredAsync ran. Without this the next read would overwrite their pick.
        model.CollaboratorStarDefaultsApplied = true;
        // Logged as intent, not outcome: PreferencesIo.WritePreferences catches and logs its own
        // failures, so this method cannot tell a successful write from a failed one. Claiming
        // "saved" here would contradict the error PreferencesIo just logged.
        _log?.Log(LogLevel.Info,
            $"Persisting {model.StarredCollaboratorWorkflows.Count} starred Collaborator workflows.");
        await PersistAsync(model);
    }

    /// <summary>Drops blanks and duplicates while keeping the caller's order.</summary>
    private static List<string> Normalize(IEnumerable<string> labels)
    {
        var normalized = new List<string>();
        if (labels == null)
        {
            return normalized;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var label in labels)
        {
            if (!string.IsNullOrWhiteSpace(label) && seen.Add(label))
            {
                normalized.Add(label);
            }
        }

        return normalized;
    }

    /// <summary>
    ///     Persists preferences. A failed write costs the user their star edits at next launch,
    ///     which is not worth tearing down the Collaborator session over, so it is swallowed —
    ///     the in-memory set stays correct for this session. <see cref="PreferencesIo" /> logs
    ///     its own write failures; the catch here covers the injected delegate and the case where
    ///     constructing the writer throws.
    /// </summary>
    private async Task PersistAsync(PreferencesModel model)
    {
        try
        {
            var write = _writePreferences ?? new PreferencesIo().WritePreferences;
            await write(model);
        }
        catch (Exception ex)
        {
            _log?.LogException(LogLevel.Warn, ex, "Failed to persist starred Collaborator workflows.");
        }
    }
}
