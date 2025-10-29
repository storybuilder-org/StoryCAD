// Windowing.desktop.cs
// Cross-head window sizing/centering:
// 1) Prefer Uno's WindowManagerHelper via reflection (if available)
// 2) Else: on Windows with a real HWND → Win32 APIs
// 3) Else: AppWindow fallback (no DisplayArea calls to avoid Uno stubs)

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using MUW = Microsoft.UI.Windowing; // alias to avoid ambiguity
using Windows.Graphics;

namespace StoryCADLib.Models;

public partial class Windowing
{
    // ---------- Reflection bridge to Uno WindowManagerHelper (no compile-time dependency) ----------
    private static object? _wmhInstance;
    private static bool _wmhChecked;

    private static object? EnsureWindowManagerHelper(Window window)
    {
        if (_wmhChecked) return _wmhInstance;
        _wmhChecked = true;

        // Common type locations in Uno heads
        Type? t =
            Type.GetType("Uno.WinUI.Runtime.Skia.WindowManagerHelper, Uno.WinUI.Runtime.Skia", throwOnError: false)
            ?? Type.GetType("Uno.UI.Runtime.Skia.WindowManagerHelper, Uno.UI.Runtime.Skia", throwOnError: false);
        if (t == null) return null;

        try
        {
            var instance = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var init = t.GetMethod("Initialize", new[] { typeof(Window) });
            init?.Invoke(instance, new object[] { window });
            _wmhInstance = instance;
        }
        catch
        {
            _wmhInstance = null;
        }
        return _wmhInstance;
    }

    private static bool TryWmhCall(Window window, string methodName, params object[] args)
    {
        var inst = EnsureWindowManagerHelper(window);
        if (inst == null) return false;
        try
        {
            var m = inst.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (m == null) return false;
            m.Invoke(inst, args);
            return true;
        }
        catch { return false; }
    }

    // ---------- Win32 interop (only used when a real HWND is available) ----------
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out RECT pvParam, uint fWinIni);
    [DllImport("user32.dll", SetLastError = true)] private static extern int GetSystemMetrics(int nIndex);

    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private const uint SPI_GETWORKAREA = 0x0030;
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private static bool IsValidWin32Handle(IntPtr h) => h != IntPtr.Zero && h.ToInt64() > 16;

    private static MUW.AppWindow GetAppWindow(Window window) => window.AppWindow; // WinAppSDK alias

    // ---------- Public API used by App.xaml.cs ----------
    public void SetWindowSize(Window window, int widthDip, int heightDip)
    {
        // 1) Uno helper (cross-platform) if present
        if (TryWmhCall(window, "Resize", widthDip, heightDip))
            return;

        // 2) macOS - skip resizing, use system default window size
        if (OperatingSystem.IsMacOS())
            return;

        // 3) Windows + real HWND → precise Win32 sizing
        if (OperatingSystem.IsWindows() && IsValidWin32Handle(WindowHandle))
        {
            SetWindowPos(WindowHandle, IntPtr.Zero, 0, 0, widthDip, heightDip, SWP_NOZORDER | SWP_SHOWWINDOW);
            return;
        }

        // 4) Cross-platform fallback using AppWindow
        var appWindow = GetAppWindow(window);
        appWindow.Resize(new SizeInt32 { Width = widthDip, Height = heightDip });
    }

    public void CenterOnScreen(Window window)
    {
        // 1) Uno helper (cross-platform) if present
        if (TryWmhCall(window, "Center"))
            return;

        // 2) Windows + real HWND → center using monitor work area
        if (OperatingSystem.IsWindows() && IsValidWin32Handle(WindowHandle))
        {
            var hMon = MonitorFromWindow(WindowHandle, MONITOR_DEFAULTTONEAREST);
            MONITORINFO mi = new() { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (hMon != IntPtr.Zero && GetMonitorInfo(hMon, ref mi) && GetWindowRect(WindowHandle, out var rect))
            {
                int winW = rect.Right - rect.Left;
                int winH = rect.Bottom - rect.Top;
                int workW = mi.rcWork.Right - mi.rcWork.Left;
                int workH = mi.rcWork.Bottom - mi.rcWork.Top;

                int x = mi.rcWork.Left + (workW - winW) / 2;
                int y = mi.rcWork.Top + (workH - winH) / 2;

                SetWindowPos(WindowHandle, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                return;
            }
            // If Win32 path fails, fall through to fallback.
        }

        // 3) Windows without a usable HWND → compute work area via SPI, move via AppWindow
        if (OperatingSystem.IsWindows())
        {
            RECT work;
            if (!SystemParametersInfo(SPI_GETWORKAREA, 0, out work, 0))
            {
                work = new RECT
                {
                    Left = 0,
                    Top = 0,
                    Right = GetSystemMetrics(SM_CXSCREEN),
                    Bottom = GetSystemMetrics(SM_CYSCREEN)
                };
            }

            var appWin = GetAppWindow(window);
            var size = appWin.Size;

            int workW = work.Right - work.Left;
            int workH = work.Bottom - work.Top;

            int newX = work.Left + (workW - size.Width) / 2;
            int newY = work.Top + (workH - size.Height) / 2;

            appWin.Move(new PointInt32 { X = newX, Y = newY });
            return;
        }

        // 4) Non-Windows heads without WMH: best-effort center (no crash, no DisplayArea)
        var aw = GetAppWindow(window);
        var sz = aw.Size;
        aw.Move(new PointInt32 { X = Math.Max(0, (1920 - sz.Width) / 2), Y = Math.Max(0, (1080 - sz.Height) / 2) });
    }

    public void SetMinimumSize(Window window, int minWidthDip = 1000, int minHeightDip = 700)
    {
        // Prefer Uno helper if present
        if (TryWmhCall(window, "SetMinimumSize", minWidthDip, minHeightDip))
            return;

        var presenter = GetAppWindow(window).Presenter as MUW.OverlappedPresenter;
        if (presenter is null) return;

        presenter.IsResizable = true;
        presenter.IsMinimizable = true;
        presenter.IsMaximizable = true;
        presenter.PreferredMinimumWidth = minWidthDip;
        presenter.PreferredMinimumHeight = minHeightDip;
    }
}
