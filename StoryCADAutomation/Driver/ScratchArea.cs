using System.Text.Json;

namespace StoryCADAutomation.Driver;

/// <summary>
///     Per-run scratch directory: a mirrored app install with a seeded Preferences.json, plus
///     the outline and backup directories the seeded preferences point at.
///     <para>
///         Why a mirror: when StoryCAD runs unpackaged, AppState.RootDirectory falls back to
///         AppDomain.CurrentDomain.BaseDirectory (StoryCADLib/Models/AppState.cs), so
///         PreferencesIo reads Preferences.json from the exe directory and there is no path or
///         environment override. Seeding in place would overwrite the developer's own dev-run
///         preferences in bin; mirroring the install into scratch and launching the mirror keeps
///         the run fully contained. Hard links make the mirror cheap on the same volume;
///         cross-volume falls back to copying (~800 MB of Debug output). Hard links share
///         inodes with the bin output, so rebuilding while a run is live mutates the mirror's
///         loose data files (loaded PE images stay locked).
///     </para>
/// </summary>
internal sealed class ScratchArea
{
    /// <summary>Root-relative entries never mirrored from the source install.</summary>
    private static readonly string[] ExcludedRootFiles =
    {
        "Preferences.json", // developer's dev-run preferences stay put; the run gets its own seed
        ".env",             // backend isolation; launch refuses on .env before mirroring anyway
        "evergreenbootstrapper.exe", // runtime File.Create under RootDirectory (StoryCADLib/ViewModels/WebViewModel.cs:343);
                                     // a hard-linked stale copy would be truncated through the shared inode
    };

    private const string ExcludedRootDirectory = "logs"; // fresh NLog output in the mirror belongs to this run

    private ScratchArea(string root)
    {
        Root = root;
        OutlineDirectory = Path.Combine(root, "outlines");
        BackupDirectory = Path.Combine(root, "backups");
        AppDirectory = Path.Combine(root, "app");
    }

    /// <summary>Run root; the future {scratch} substitution in scripts resolves here.</summary>
    public string Root { get; }

    /// <summary>Seeded as OutlineDirectory so AutoSaveService writes here and nowhere else.</summary>
    public string OutlineDirectory { get; }

    /// <summary>Seeded as BackupDirectory so BackupService writes here and nowhere else.</summary>
    public string BackupDirectory { get; }

    /// <summary>The mirrored install; the exe the driver actually launches lives here.</summary>
    public string AppDirectory { get; }

    /// <summary>"hardlink" or "copy"; recorded for diagnostics.</summary>
    public string MirrorMode { get; private set; } = "none";

    /// <summary>Creates the per-run directory tree under <paramref name="scratchRoot" /> (default %TEMP%\StoryCADAutomation).</summary>
    public static ScratchArea Create(string? scratchRoot)
    {
        var baseDir = scratchRoot ?? Path.Combine(Path.GetTempPath(), "StoryCADAutomation");
        var root = Path.Combine(baseDir, $"run-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}");
        var area = new ScratchArea(root);
        Directory.CreateDirectory(area.OutlineDirectory);
        Directory.CreateDirectory(area.BackupDirectory);
        Directory.CreateDirectory(area.AppDirectory);
        return area;
    }

    /// <summary>
    ///     Mirrors the app install into <see cref="AppDirectory" />. Hard links when source and
    ///     scratch share a volume, per-file copy otherwise (or when a link fails).
    /// </summary>
    public void MirrorApp(string sourceDir)
    {
        var sameVolume = string.Equals(
            Path.GetPathRoot(Path.GetFullPath(sourceDir)),
            Path.GetPathRoot(Path.GetFullPath(Root)),
            StringComparison.OrdinalIgnoreCase);
        MirrorMode = sameVolume ? "hardlink" : "copy";

        foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, dir);
            if (IsExcluded(rel))
            {
                continue;
            }

            Directory.CreateDirectory(Path.Combine(AppDirectory, rel));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            if (IsExcluded(rel))
            {
                continue;
            }

            var dest = Path.Combine(AppDirectory, rel);
            if (!sameVolume || !NativeMethods.TryCreateHardLink(dest, file))
            {
                File.Copy(file, dest, overwrite: true);
            }
        }
    }

    /// <summary>
    ///     Writes the seeded Preferences.json into the mirrored install. The contents are part
    ///     of the design, not an implementation detail (devdocs/issue_1421_dsl_design.md
    ///     "Runner", Environment prep; key names are the JsonPropertyName values in
    ///     StoryCADLib/Models/Tools/PreferencesModel.cs). Keys omitted here keep the
    ///     PreferencesModel constructor defaults, because PreferencesIo deserializes over a
    ///     default-constructed model.
    /// </summary>
    /// <param name="storyCADLibVersion">
    ///     Assembly version of the mirrored StoryCADLib.dll. Must match what AppState.Version
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
            Path.Combine(AppDirectory, "Preferences.json"),
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

    private static bool IsExcluded(string relativePath)
    {
        foreach (var file in ExcludedRootFiles)
        {
            if (string.Equals(relativePath, file, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return string.Equals(relativePath, ExcludedRootDirectory, StringComparison.OrdinalIgnoreCase)
               || relativePath.StartsWith(ExcludedRootDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
