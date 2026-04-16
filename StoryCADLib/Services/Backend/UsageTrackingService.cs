using System.Text.Json;
using StoryCADLib.DAL;

namespace StoryCADLib.Services.Backend;

/// <summary>
///     Accumulates usage data in memory during a session and flushes
///     to MySQL via IMySqlIo.RecordSessionData at session end.
///     All public methods are no-ops when consent is not given.
/// </summary>
public class UsageTrackingService : IUsageTrackingService
{
    private readonly PreferenceService _preferenceService;
    private readonly IMySqlIo _sqlIo;
    private readonly ILogService _logService;

    // Session state
    private DateTime _sessionStart;
    private readonly Dictionary<string, int> _featureCounts = new();
    private readonly List<OutlineSession> _outlineSessions = new();
    private OutlineSession _currentOutline;

    public UsageTrackingService(PreferenceService preferenceService, IMySqlIo sqlIo, ILogService logService)
    {
        _preferenceService = preferenceService;
        _sqlIo = sqlIo;
        _logService = logService;
    }

    private bool ConsentGiven => _preferenceService.Model.UsageStatsConsent;

    public void StartSession()
    {
        if (!ConsentGiven) return;
        _sessionStart = DateTime.UtcNow;
        _featureCounts.Clear();
        _outlineSessions.Clear();
        _currentOutline = null;
    }

    public async Task EndSession()
    {
        if (!ConsentGiven) return;
        if (!_sqlIo.IsConnectionConfigured) return;

        // Guard: StartSession never ran (consent was false at launch), or we
        // already flushed this session (menu-Exit → Application.Current.Exit
        // → OnApplicationClosing could call us twice).
        if (_sessionStart == default) return;

        var usageId = _preferenceService.Model.UsageId;
        if (string.IsNullOrEmpty(usageId))
        {
            _logService.Log(LogLevel.Warn, "Usage flush skipped: consent=true but UsageId is empty");
            return;
        }

        // Close any open outline
        if (_currentOutline != null)
        {
            _currentOutline.CloseTime = DateTime.UtcNow;
            _currentOutline = null;
        }

        var sessionEnd = DateTime.UtcNow;
        var clockTimeSeconds = (int)(sessionEnd - _sessionStart).TotalSeconds;

        try
        {
            // Build outline JSON
            var outlineData = _outlineSessions.Select(o => new
            {
                outline_guid = o.OutlineGuid.ToString(),
                open_time = o.OpenTime.ToString("yyyy-MM-dd HH:mm:ss"),
                close_time = (o.CloseTime ?? sessionEnd).ToString("yyyy-MM-dd HH:mm:ss"),
                elements_added = o.ElementsAdded,
                elements_deleted = o.ElementsDeleted,
                genre = o.Genre ?? "",
                story_form = o.StoryForm ?? "",
                element_count = o.ElementCount,
                last_updated = (o.CloseTime ?? sessionEnd).ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            // Build feature JSON
            var featureData = _featureCounts
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => new
                {
                    feature_name = kvp.Key,
                    use_count = kvp.Value
                }).ToList();

            var outlinesJson = JsonSerializer.Serialize(outlineData);
            var featuresJson = JsonSerializer.Serialize(featureData);

            await _sqlIo.RecordSessionData(
                usageId,
                _sessionStart,
                sessionEnd,
                clockTimeSeconds,
                outlinesJson,
                featuresJson);

            _logService.Log(LogLevel.Info,
                $"Usage data flushed: {_outlineSessions.Count} outlines, {_featureCounts.Count} features");
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Warn, ex, "Failed to flush usage data — data lost for this session");
        }
        finally
        {
            // Mark session as flushed so a second EndSession call is a no-op.
            _sessionStart = default;
        }
    }

    public void OutlineOpened(Guid outlineGuid, string genre, string storyForm, int elementCount)
    {
        if (!ConsentGiven) return;

        // Close previous outline if still open
        if (_currentOutline != null)
        {
            _currentOutline.CloseTime = DateTime.UtcNow;
        }

        _currentOutline = new OutlineSession
        {
            OutlineGuid = outlineGuid,
            OpenTime = DateTime.UtcNow,
            Genre = genre,
            StoryForm = storyForm,
            ElementCount = elementCount
        };
        _outlineSessions.Add(_currentOutline);
    }

    public void OutlineClosed(Guid outlineGuid, int elementCount)
    {
        if (!ConsentGiven) return;

        if (_currentOutline != null && _currentOutline.OutlineGuid == outlineGuid)
        {
            _currentOutline.CloseTime = DateTime.UtcNow;
            _currentOutline.ElementCount = elementCount;
            _currentOutline = null;
        }
    }

    public void ElementAdded()
    {
        if (!ConsentGiven || _currentOutline == null) return;
        _currentOutline.ElementsAdded++;
    }

    public void ElementRemoved()
    {
        if (!ConsentGiven || _currentOutline == null) return;
        _currentOutline.ElementsDeleted++;
    }

    public void ElementRestored()
    {
        if (!ConsentGiven || _currentOutline == null) return;
        // Restore is effectively an add (element comes back)
        _currentOutline.ElementsAdded++;
    }

    public void FeatureUsed(string featureName)
    {
        if (!ConsentGiven) return;

        if (!_featureCounts.TryAdd(featureName, 1))
            _featureCounts[featureName]++;
    }

    /// <summary>In-memory outline session data accumulated during editing.</summary>
    private class OutlineSession
    {
        public Guid OutlineGuid { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime? CloseTime { get; set; }
        public int ElementsAdded { get; set; }
        public int ElementsDeleted { get; set; }
        public string Genre { get; set; }
        public string StoryForm { get; set; }
        public int ElementCount { get; set; }
    }
}
