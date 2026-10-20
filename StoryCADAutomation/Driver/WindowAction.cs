namespace StoryCADAutomation.Driver;

/// <summary>What the window verb does to the app's main window (#1422 Window_Management).</summary>
public enum WindowAction
{
    /// <summary>Sets the visible frame to a size in 100%-scale pixels at the display's top-left.</summary>
    Size,

    /// <summary>Minimizes to the taskbar.</summary>
    Minimize,

    /// <summary>Fills the screen.</summary>
    Maximize,

    /// <summary>Returns to the frame the window had before the first minimize or maximize.</summary>
    Restore,
}
