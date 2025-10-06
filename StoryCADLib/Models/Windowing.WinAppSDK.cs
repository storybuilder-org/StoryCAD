// Windowing.WinAppSDK.cs

using System.Runtime.InteropServices;
using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;

namespace StoryCAD.Models;

public partial class Windowing
{
    // Per-window DPI (pixels per DIP)
    private static double GetDpiScale(Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var dpi = GetDpiForWindow(hwnd);
        return dpi / 96.0;
    }

    [DllImport("User32.dll")]
    private static extern uint GetDpiForWindow(nint hWnd);

    private static AppWindow GetAppWindow(Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(id)!;
    }

    public void SetWindowSize(Window window, int widthDip, int heightDip)
    {
        var appWindow = GetAppWindow(window);
        var wa = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;

        var s = GetDpiScale(window);
        var w = (int)Math.Round(widthDip * s);
        var h = (int)Math.Round(heightDip * s);

        var minW = (int)Math.Round(1000 * s);
        var minH = (int)Math.Round(700 * s);

        w = Math.Clamp(w, minW, wa.Width);
        h = Math.Clamp(h, minH, wa.Height);

        appWindow.Resize(new SizeInt32(w, h));
    }

    public void CenterOnScreen(Window window, int baseWidthDip, int baseHeightDip)
    {
        var appWindow = GetAppWindow(window);
        var wa = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;

        var s = GetDpiScale(window);
        var w = (int)Math.Round(baseWidthDip * s);
        var h = (int)Math.Round(baseHeightDip * s);

        var minW = (int)Math.Round(1000 * s);
        var minH = (int)Math.Round(700 * s);

        w = Math.Clamp(w, minW, wa.Width);
        h = Math.Clamp(h, minH, wa.Height);

        var x = wa.X + (wa.Width - w) / 2;
        var y = wa.Y + (wa.Height - h) / 2;

        appWindow.MoveAndResize(new RectInt32(x, y, w, h));
    }

    public void Maximize(Window window)
    {
        if (GetAppWindow(window).Presenter is OverlappedPresenter p)
        {
            p.Maximize();
        }
    }

    public void Minimize(Window window)
    {
        if (GetAppWindow(window).Presenter is OverlappedPresenter p)
        {
            p.Minimize();
        }
    }

    public void Restore(Window window)
    {
        if (GetAppWindow(window).Presenter is OverlappedPresenter p)
        {
            p.Restore();
        }
    }

    public void SetMinimumSize(Window window, int minWidthDip = 1000, int minHeightDip = 700)
    {
        var appWindow = GetAppWindow(window);
        if (appWindow.Presenter is OverlappedPresenter p)
        {
            var s = GetDpiScale(window);
            p.IsResizable = true;
            p.IsMinimizable = true;
            p.IsMaximizable = true;
            p.PreferredMinimumWidth = (int)Math.Round(minWidthDip * s);
            p.PreferredMinimumHeight = (int)Math.Round(minHeightDip * s);
        }
    }
}
