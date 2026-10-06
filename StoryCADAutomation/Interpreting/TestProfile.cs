using StoryCADAutomation.Driver;

namespace StoryCADAutomation.Interpreting;

/// <summary>
///     The test-profile realization (design, Execution profiles): clicks use UIA patterns
///     where sufficient and a real pointer where the driver's per-control strategy says
///     patterns are known-insufficient; pause is ×0; assertions hard-fail; narrate is a log
///     line.
/// </summary>
public sealed class TestProfile : IExecutionProfile
{
    /// <inheritdoc />
    public string Name => "test";

    /// <inheritdoc />
    public bool ContinueOnAssertionFailure => false;

    /// <inheritdoc />
    public void Click(IUiDriver driver, ElementAddress target)
    {
        // The driver keeps the per-control strategy; the 2026-06-12 navigation case is the
        // founding example of a pattern-insufficient control (design, Execution profiles).
        switch (driver.ResolveClickStrategy(target))
        {
            case ClickStrategy.InvokePattern:
                driver.Invoke(target);
                break;
            case ClickStrategy.SelectionItemPattern:
                driver.Select(target);
                break;
            default:
                driver.ClickPointer(target);
                break;
        }
    }

    /// <inheritdoc />
    public void Approach(IUiDriver driver, ElementAddress target)
    {
        // No cursor travel in test mode: it only costs time.
    }

    /// <inheritdoc />
    public void ApproachInWindow(IUiDriver driver, string windowTitle, ElementAddress target)
    {
    }

    /// <inheritdoc />
    public void Type(IUiDriver driver, string text) => driver.TypeText(text);

    /// <inheritdoc />
    public void Set(IUiDriver driver, ElementAddress target, string value) => driver.SetText(target, value);

    /// <inheritdoc />
    public TimeSpan ScalePause(double seconds) => TimeSpan.Zero; // pause factor 0 in test mode

    /// <inheritdoc />
    public void Narrate(string text, Action<string> log) => log($"narrate: {text}");
}
