// Windowing.WinAppSDK.cs
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace StoryCAD.Models;

public partial class Windowing
{
    // Per-window DPI (pixels per DIP)
    private static double GetDpiScale(Window window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        uint dpi = GetDpiForWindow(hwnd);
        return dpi / 96.0;
    }

    [DllImport("User32.dll")]
    private static extern uint GetDpiForWindow(nint hWnd);

    private static AppWindow GetAppWindow(Window window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(id)!;
    }

    public void SetWindowSize(Window window, int widthDip, int heightDip)
    {
        var appWindow = GetAppWindow(window);
        var wa = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;

        double s = GetDpiScale(window);
        int w = (int)System.Math.Round(widthDip * s);
        int h = (int)System.Math.Round(heightDip * s);

        int minW = (int)System.Math.Round(1000 * s);
        int minH = (int)System.Math.Round(700 * s);

        w = System.Math.Clamp(w, minW, wa.Width);
        h = System.Math.Clamp(h, minH, wa.Height);

        appWindow.Resize(new SizeInt32(w, h));
    }

    public void CenterOnScreen(Window window, int baseWidthDip, int baseHeightDip)
    {
        var appWindow = GetAppWindow(window);
        var wa = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;

        double s = GetDpiScale(window);
        int w = (int)System.Math.Round(baseWidthDip * s);
        int h = (int)System.Math.Round(baseHeightDip * s);

        int minW = (int)System.Math.Round(1000 * s);
        int minH = (int)System.Math.Round(700 * s);

        w = System.Math.Clamp(w, minW, wa.Width);
        h = System.Math.Clamp(h, minH, wa.Height);

        int x = wa.X + (wa.Width  - w) / 2;
        int y = wa.Y + (wa.Height - h) / 2;

        appWindow.MoveAndResize(new RectInt32(x, y, w, h));
    }

    public void Maximize(Window window)
    {
        if (GetAppWindow(window).Presenter is OverlappedPresenter p) p.Maximize();
    }
    public void Minimize(Window window)
    {
        if (GetAppWindow(window).Presenter is OverlappedPresenter p) p.Minimize();
    }
    public void Restore(Window window)
    {
        if (GetAppWindow(window).Presenter is OverlappedPresenter p) p.Restore();
    }

    public void SetMinimumSize(Window window, int minWidthDip = 1000, int minHeightDip = 700)
    {
        var appWindow = GetAppWindow(window);
        if (appWindow.Presenter is OverlappedPresenter p)
        {
            double s = GetDpiScale(window);
            p.IsResizable = true;
            p.IsMinimizable = true;
            p.IsMaximizable = true;
            p.PreferredMinimumWidth  = (int)System.Math.Round(minWidthDip  * s);
            p.PreferredMinimumHeight = (int)System.Math.Round(minHeightDip * s);
        }
    }
}
