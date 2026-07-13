namespace StoryCADAutomation.Driver;

/// <summary>
///     Primary-desktop facts asserted at launch (devdocs/issue_1421_dsl_design.md "CI hosting":
///     hosted runners provide an interactive desktop at a fixed resolution with 100% DPI; the
///     driver asserts the actual values and the runner maps a mismatch to exit 3 rather than
///     clicking blind).
/// </summary>
/// <param name="WidthPixels">Primary desktop width, physical pixels.</param>
/// <param name="HeightPixels">Primary desktop height, physical pixels.</param>
/// <param name="Dpi">System DPI; 96 means 100% scale.</param>
public sealed record DisplayFacts(int WidthPixels, int HeightPixels, int Dpi)
{
    /// <summary>DPI expressed as the familiar Windows scale percentage.</summary>
    public int ScalePercent => (int)Math.Round(Dpi * 100.0 / 96.0);

    /// <summary>
    ///     Reads the primary display. Forces per-monitor-v2 DPI awareness first: an unaware
    ///     process gets virtualized metrics and the assertion would lie.
    /// </summary>
    public static DisplayFacts ReadPrimary()
    {
        NativeMethods.EnsureDpiAwareness();
        return new DisplayFacts(
            NativeMethods.PrimaryScreenWidth(),
            NativeMethods.PrimaryScreenHeight(),
            NativeMethods.SystemDpi());
    }

    public override string ToString() => $"{WidthPixels}x{HeightPixels} at {ScalePercent}% scale ({Dpi} DPI)";
}
