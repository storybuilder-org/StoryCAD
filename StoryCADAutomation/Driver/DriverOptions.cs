namespace StoryCADAutomation.Driver;

/// <summary>
///     Launch configuration for <see cref="StoryCADDriver.Launch" />. Defaults follow
///     devdocs/issue_1421_dsl_design.md: 5 s implicit readiness wait, 1 s expect-no settle
///     window, display expectations asserted at launch (the runner maps a mismatch to exit 3).
/// </summary>
public sealed class DriverOptions
{
    /// <summary>
    ///     Full path to StoryCAD.exe (WinAppSDK head build output). The runner owns defaulting
    ///     this to the sibling Debug build; the driver requires it explicitly.
    /// </summary>
    public required string AppPath { get; init; }

    /// <summary>
    ///     Root under which per-run scratch directories are created. Default:
    ///     %TEMP%\StoryCADAutomation.
    /// </summary>
    public string? ScratchRoot { get; init; }

    /// <summary>Implicit readiness wait for every locating operation (design default 5 s).</summary>
    public TimeSpan ReadinessTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Settle-then-check window for absence assertions (design default 1 s). Deliberately
    ///     not the readiness timeout: inheriting the implicit wait would invert expect-no
    ///     semantics into wait-for-the-error.
    /// </summary>
    public TimeSpan AbsenceSettle { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     How long launch waits for the app's main window. 45 s matches the manual probe
    ///     (devdocs/tools/uia_header_probe.ps1) which this launch path turns into code.
    /// </summary>
    public TimeSpan MainWindowTimeout { get; init; } = TimeSpan.FromSeconds(45);

    /// <summary>
    ///     How long launch waits, after the native main window exists, for the process's
    ///     windows to appear in the UIA tree (the UIA provider connects asynchronously).
    /// </summary>
    public TimeSpan UiaWindowTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Minimum primary-desktop size asserted at launch. The design's CI hosting section
    ///     records that pointer math depends on the hosted runner's fixed resolution; the
    ///     runner batch pins the actual CI values, these are the working defaults.
    /// </summary>
    public int MinDesktopWidth { get; init; } = 1920;

    /// <inheritdoc cref="MinDesktopWidth" />
    public int MinDesktopHeight { get; init; } = 1080;

    /// <summary>
    ///     Exact DPI scale the desktop must report at launch (100 = 96 DPI), or null to skip
    ///     the scale assertion. Default 100 per the design's CI hosting section: fail with the
    ///     launch class rather than click blind.
    /// </summary>
    public int? RequiredDpiScalePercent { get; init; } = 100;

    /// <summary>
    ///     Sweep the scratch directory during teardown (design: "Scratch is swept on exit").
    ///     The runner turns this off only while it copies the app's NLog files into the report.
    /// </summary>
    public bool SweepScratchOnTeardown { get; init; } = true;

    /// <summary>
    ///     Let Windows Error Reporting handle an app crash, so it writes a dump (runner --ci,
    ///     #1421 D-CI-CRASH). Off by default: locally a WER dialog could outlive the run.
    /// </summary>
    public bool AllowCrashReporting { get; init; }
}
