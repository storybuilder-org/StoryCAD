namespace StoryCADAutomation.Driver;

/// <summary>
///     Snapshot of a located element, taken at the moment a wait completed. This is what the
///     driver hands back instead of a UIA element (macOS-seam rule: UIA types stay inside the
///     driver). Feeds the future expect verbs: enabled/disabled, text, exists.
/// </summary>
/// <param name="IsEnabled">UIA IsEnabled at snapshot time.</param>
/// <param name="IsOffscreen">UIA IsOffscreen at snapshot time.</param>
/// <param name="IsKeyboardFocusable">UIA IsKeyboardFocusable at snapshot time.</param>
/// <param name="Name">UIA Name, or null when unreadable.</param>
/// <param name="Text">Value-pattern or Text-pattern content, or null when the element has neither.</param>
/// <param name="ControlType">UIA control type name, for diagnostics only.</param>
public sealed record ElementState(
    bool IsEnabled,
    bool IsOffscreen,
    bool IsKeyboardFocusable,
    string? Name,
    string? Text,
    string ControlType);
