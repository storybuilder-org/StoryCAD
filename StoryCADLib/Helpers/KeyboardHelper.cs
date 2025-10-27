using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

namespace StoryCADLib.Helpers;

/// <summary>
/// Cross-platform keyboard state helper for UNO Platform.
/// Works on both Windows and macOS.
/// </summary>
public static class KeyboardHelper
{
    /// <summary>
    /// Checks if the Control key (or Cmd on macOS) is currently pressed.
    /// </summary>
    public static bool IsControlPressed()
    {
        var state = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        return state.HasFlag(CoreVirtualKeyStates.Down);
    }

    /// <summary>
    /// Checks if the Shift key is currently pressed.
    /// </summary>
    public static bool IsShiftPressed()
    {
        var state = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        return state.HasFlag(CoreVirtualKeyStates.Down);
    }

    /// <summary>
    /// Checks if the Alt key (or Option on macOS) is currently pressed.
    /// </summary>
    public static bool IsAltPressed()
    {
        var state = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
        return state.HasFlag(CoreVirtualKeyStates.Down);
    }

    /// <summary>
    /// Checks if a specific virtual key is currently pressed.
    /// </summary>
    /// <param name="key">The virtual key to check</param>
    public static bool IsKeyPressed(VirtualKey key)
    {
        var state = InputKeyboardSource.GetKeyStateForCurrentThread(key);
        return state.HasFlag(CoreVirtualKeyStates.Down);
    }
}
