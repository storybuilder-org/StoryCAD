// Pickers.cs

using Windows.Storage.Pickers;
using WinRT.Interop;

namespace StoryCAD.Services;

public static class Pickers
{
#if WINDOWS
    public static void InitializeWithWindow(FileOpenPicker picker, Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
    }

    public static void InitializeWithWindow(FileSavePicker picker, Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
    }

    public static void InitializeWithWindow(FolderPicker picker, Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
    }
#else
        public static void InitializeWithWindow(object _, object __) { }
#endif
}
