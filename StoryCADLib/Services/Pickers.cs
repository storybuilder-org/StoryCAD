// Pickers.cs
using Microsoft.UI.Xaml;

namespace StoryCAD.Services
{
    public static class Pickers
    {
#if WINDOWS
        public static void InitializeWithWindow(Windows.Storage.Pickers.FileOpenPicker picker, Window window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }
        public static void InitializeWithWindow(Windows.Storage.Pickers.FileSavePicker picker, Window window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }
        public static void InitializeWithWindow(Windows.Storage.Pickers.FolderPicker picker, Window window)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }
#else
        public static void InitializeWithWindow(object _, object __) { }
#endif
    }
}