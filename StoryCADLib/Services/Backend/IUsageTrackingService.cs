namespace StoryCADLib.Services.Backend;

/// <summary>
///     Accumulates usage data in memory during a session and flushes
///     to the backend at session end. All methods are no-ops when
///     UsageStatsConsent is false.
/// </summary>
public interface IUsageTrackingService
{
    /// <summary>Records session start timestamp.</summary>
    void StartSession();

    /// <summary>Flushes all accumulated data to MySQL via BackendService, then discards.</summary>
    Task EndSession();

    /// <summary>Records an outline being opened with its current metadata.</summary>
    void OutlineOpened(Guid outlineGuid, string genre, string storyForm, int elementCount);

    /// <summary>Records an outline being closed with its final element count.</summary>
    void OutlineClosed(Guid outlineGuid, int elementCount);

    /// <summary>Records a story element being added.</summary>
    void ElementAdded();

    /// <summary>Records a story element being removed (moved to trash).</summary>
    void ElementRemoved();

    /// <summary>Records a story element being restored from trash.</summary>
    void ElementRestored();

    /// <summary>Records a feature/tool being used (e.g., "Collaborator", "MasterPlots").</summary>
    void FeatureUsed(string featureName);
}
