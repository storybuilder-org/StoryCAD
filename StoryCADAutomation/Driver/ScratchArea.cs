using System.Text.Json;

namespace StoryCADAutomation.Driver;

/// <summary>
///     Per-run scratch directory: the app-data folder the launched app uses as its root (seeded
///     Preferences.json, logs), plus the outline and backup directories the seeded preferences
///     point at. The driver hands the app-data folder to StoryCAD through the STORYCAD_ROOT_DIR
///     environment variable (StoryCADLib/Models/AppState.cs, RootDirectoryOverrideVariable), so
///     the build output is launched in place and never copied (#1421).
/// </summary>
internal sealed class ScratchArea
{
    private ScratchArea(string root)
    {
        Root = root;
        OutlineDirectory = Path.Combine(root, "outlines");
        BackupDirectory = Path.Combine(root, "backups");
        AppDataDirectory = Path.Combine(root, "appdata");
    }

    /// <summary>Run root; the future {scratch} substitution in scripts resolves here.</summary>
    public string Root { get; }

    /// <summary>Seeded as OutlineDirectory so AutoSaveService writes here and nowhere else.</summary>
    public string OutlineDirectory { get; }

    /// <summary>Seeded as BackupDirectory so BackupService writes here and nowhere else.</summary>
    public string BackupDirectory { get; }

    /// <summary>The app's root for this run (STORYCAD_ROOT_DIR): Preferences.json and logs live here.</summary>
    public string AppDataDirectory { get; }

    /// <summary>Creates the per-run directory tree under <paramref name="scratchRoot" /> (default %TEMP%\StoryCADAutomation).</summary>
    public static ScratchArea Create(string? scratchRoot)
    {
        var baseDir = scratchRoot ?? Path.Combine(Path.GetTempPath(), "StoryCADAutomation");
        var root = Path.Combine(baseDir, $"run-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}");
        var area = new ScratchArea(root);
        Directory.CreateDirectory(area.OutlineDirectory);
        Directory.CreateDirectory(area.BackupDirectory);
        Directory.CreateDirectory(area.AppDataDirectory);
        return area;
    }

    /// <summary>
    ///     Writes the seeded Preferences.json into the app-data folder. The contents are part
    ///     of the design, not an implementation detail (devdocs/issue_1421_dsl_design.md
    ///     "Runner", Environment prep; key names are the JsonPropertyName values in
    ///     StoryCADLib/Models/Tools/PreferencesModel.cs). Keys omitted here keep the
    ///     PreferencesModel constructor defaults, because PreferencesIo deserializes over a
    ///     default-constructed model.
    /// </summary>
    /// <param name="storyCADLibVersion">
    ///     Assembly version of the launched StoryCADLib.dll. Must match what AppState.Version
    ///     reports at runtime or the app treats the launch as a version change and shows the
    ///     changelog dialog (probe precondition, devdocs/tools/uia_header_probe.ps1).
    /// </param>
    public void SeedPreferences(string storyCADLibVersion)
    {
        var preferences = new Dictionary<string, object>
        {
            // First-run/onboarding suppression, per the probe script's header keys: any open
            // ContentDialog blocks the file-open menu (WinUI allows one dialog at a time).
            ["Initialized"] = true,           // skip the PreferencesInitialization page
            ["Version"] = storyCADLibVersion, // suppress the changelog dialog
            ["ShowStartupDialog"] = false,    // suppress the help dialog
            ["HideKeyFileWarning"] = true,    // suppress the key-file warning dialog
            ["ShowFilePickerOnStartup"] = true, // the probe recipe drives the file-open menu at startup

            // Containment: AutoSaveService and BackupService write inside scratch and nowhere
            // else. Containment fails if these stay at user defaults (design, Runner section).
            ["OutlineDirectory"] = OutlineDirectory,
            ["BackupDirectory"] = BackupDirectory,

            // Autosave and timed backup OFF: ST-005 asserts the dirty indicator and the
            // Save-changes dialog; with autosave on that dialog may never appear and the smoke
            // script flakes on an unpinned preference (design, Runner section).
            ["Autosave"] = false,
            ["TimedBackup"] = false,
            ["BackupOnOpen"] = false,

            // Backend isolation: no consent flags set, nothing queued for posting. The .env
            // refusal already keeps the backend unreachable; these make intent explicit.
            ["ElmahConsent"] = false,
            ["NewsletterConsent"] = false,
            ["UsageStatsConsent"] = false,
            ["RecordPreferencesStatus"] = true, // marks backend sync as already done
            ["RecordVersionStatus"] = true,
            ["FirstName"] = string.Empty,
            ["LastName"] = string.Empty,
            ["Email"] = string.Empty,
            ["RecentFiles"] = Array.Empty<string>(),
        };

        File.WriteAllText(
            Path.Combine(AppDataDirectory, "Preferences.json"),
            JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>
    ///     Deletes the run directory. Never throws (teardown must be cancel-safe); returns
    ///     notes for anything left behind. Retries cover the window where the just-killed
    ///     app still holds file locks.
    /// </summary>
    public IReadOnlyList<string> Sweep()
    {
        var notes = new List<string>();
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }

                return notes;
            }
            catch (Exception ex)
            {
                if (attempt == 3)
                {
                    notes.Add($"Scratch sweep left '{Root}' behind: {ex.Message}");
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        return notes;
    }
}
