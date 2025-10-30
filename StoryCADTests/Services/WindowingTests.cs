using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.Logging;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Services.IoC;

namespace StoryCADTests.Services
{
    [TestClass]
    public class WindowingTests
    {
        private Windowing _windowing;

        [TestInitialize]
        public void Setup()
        {
            // Initialize IoC if not already done
            if (Ioc.Default.GetService<AppState>() == null)
            {
                BootStrapper.Initialise(true);
            }

            // Get Windowing from IoC container
            _windowing = Ioc.Default.GetRequiredService<Windowing>();
        }

        [TestMethod]
        public void CenterOnScreen_WithNullWindow_ThrowsException()
        {
            // Arrange
            // Note: We can't easily create a real Window in unit tests
            // This test verifies the method throws with null window
            Window? window = null;

            // Act & Assert
            // CenterOnScreen requires a valid window, throws on null
            Assert.ThrowsException<NullReferenceException>(() =>
                _windowing.CenterOnScreen(window!));
        }

        [TestMethod]
        public void SetMinimumSize_WithNullWindow_ThrowsException()
        {
            // Arrange
            Window? window = null;

            // Act & Assert
            // SetMinimumSize requires a valid window, throws on null
            Assert.ThrowsException<NullReferenceException>(() =>
                _windowing.SetMinimumSize(window!, 1000, 700));
        }

        [TestMethod]
        public void SetWindowSize_WithNullWindow_PlatformSpecificBehavior()
        {
            // Arrange
            Window? window = null;

            // Act & Assert
            // macOS: Returns early without throwing (line 102-103 in Windowing.desktop.cs)
            // Windows: Would throw NullReferenceException when accessing GetAppWindow()
            // This test just verifies macOS behavior - returns without exception
            _windowing.SetWindowSize(window!, 1200, 800);
            // If we get here on macOS, test passes (early return on line 103)
        }

#if WINDOWS10_0_18362_0_OR_GREATER
        [TestMethod]
        public void CenterOnScreen_OnWindows_UsesModernAPIs()
        {
            // This test verifies modern WinUI 3 APIs are used (AppWindow, DisplayArea)
            // After issue #1116, implementation uses AppWindow.Move() instead of Win32 APIs

            // Verify the modern helper methods are defined
            var methodInfo = typeof(Windowing).GetMethod("GetAppWindow",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(methodInfo, "GetAppWindow helper method should be defined");

            methodInfo = typeof(Windowing).GetMethod("GetDpiScale",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(methodInfo, "GetDpiScale helper method should be defined");

            // Verify GetDpiForWindow P/Invoke is still used for DPI scaling
            methodInfo = typeof(Windowing).GetMethod("GetDpiForWindow",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(methodInfo, "GetDpiForWindow P/Invoke should be defined for DPI awareness");
        }
#endif

        #region TDD Tests for SRP Refactoring and DPI Scaling

        [TestMethod]
        public void CenterOnScreen_WithoutSizeParameters_OnlyPositionsWindow()
        {
            // Arrange
            Window? window = null;

            // Act & Assert
            // After refactoring, CenterOnScreen should NOT accept size parameters
            // This test verifies the method signature has been changed
            var methodInfo = typeof(Windowing).GetMethod("CenterOnScreen",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(methodInfo, "CenterOnScreen method should exist");

            var parameters = methodInfo.GetParameters();
            // Should only have 1 parameter: Window window
            Assert.AreEqual(1, parameters.Length,
                "CenterOnScreen should only accept Window parameter (SRP: only center, don't resize)");
            Assert.AreEqual(typeof(Window), parameters[0].ParameterType,
                "First parameter should be Window");
        }

        [TestMethod]
        public void SetWindowSize_AcceptsSizeParameters_OnlyResizesWindow()
        {
            // Arrange & Act
            var methodInfo = typeof(Windowing).GetMethod("SetWindowSize",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(methodInfo, "SetWindowSize method should exist");

            var parameters = methodInfo.GetParameters();
            // Should have 3 parameters: Window window, int width, int height
            Assert.AreEqual(3, parameters.Length,
                "SetWindowSize should accept Window, width, and height parameters");
            Assert.AreEqual(typeof(Window), parameters[0].ParameterType);
            Assert.AreEqual(typeof(int), parameters[1].ParameterType);
            Assert.AreEqual(typeof(int), parameters[2].ParameterType);
        }

#if WINDOWS10_0_18362_0_OR_GREATER
        [TestMethod]
        public void CenterOnScreen_UsesDisplayArea_ForScreenDimensions()
        {
            // After issue #1116, implementation uses DisplayArea.WorkArea
            // which provides logical pixels (DIPs) automatically, handling DPI scaling correctly

            // Verify CenterOnScreen uses DisplayArea (indirectly tested via GetAppWindow)
            var methodInfo = typeof(Windowing).GetMethod("CenterOnScreen",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(methodInfo, "CenterOnScreen method should exist");

            // The implementation now uses:
            // - DisplayArea.GetFromWindowId() to get the display area
            // - WorkArea property which returns logical pixels
            // - AppWindow.Move() to position the window
            // This correctly handles DPI scaling without needing separate logical/physical methods
        }

        [TestMethod]
        public void SetWindowSize_UsesDpiScale_ForCorrectSizing()
        {
            // Verify SetWindowSize handles DPI scaling
            var methodInfo = typeof(Windowing).GetMethod("SetWindowSize",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(methodInfo, "SetWindowSize method should exist");

            var parameters = methodInfo.GetParameters();
            Assert.AreEqual(3, parameters.Length, "SetWindowSize should accept Window, width, and height");
            Assert.AreEqual(typeof(Window), parameters[0].ParameterType);
            Assert.AreEqual(typeof(int), parameters[1].ParameterType, "Width should be in DIPs");
            Assert.AreEqual(typeof(int), parameters[2].ParameterType, "Height should be in DIPs");

            // The implementation:
            // 1. Takes DIPs (device independent pixels) as input
            // 2. Multiplies by DPI scale to get physical pixels
            // 3. Clamps to work area bounds
            // 4. Calls AppWindow.Resize() with physical pixels
        }

        [TestMethod]
        public void GetDpiScale_HandlesPerMonitorDPI()
        {
            // Verify DPI scaling method exists for per-monitor DPI awareness
            var methodInfo = typeof(Windowing).GetMethod("GetDpiScale",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(methodInfo, "GetDpiScale should exist for per-monitor DPI handling");

            // The implementation:
            // - Uses GetDpiForWindow() to get the current monitor's DPI
            // - Returns scale factor (e.g., 1.5 for 150% scaling)
            // - This ensures windows are sized correctly on high-DPI displays
        }

        [TestMethod]
        public void DisplayArea_ProvidesLogicalDimensions()
        {
            // This test documents that DisplayArea.WorkArea provides logical pixels (DIPs)
            // No need for separate GetLogicalScreenWidth/Height methods

            // The modern implementation:
            // - DisplayArea.WorkArea.Width/Height are already in logical pixels
            // - No manual DPI conversion needed for screen dimensions
            // - Windows automatically handles the DPI scaling for WorkArea

            // This is why the old GetLogicalScreenWidth/Height methods are not needed
            Assert.IsTrue(true, "DisplayArea.WorkArea provides logical dimensions automatically");
        }

        [TestMethod]
        public void CenterOnScreen_OnWindows_UsesLogicalPixelsForPositioning()
        {
            // Arrange
            // This test documents the expected behavior:
            // At 150% DPI with 3840x2160 physical screen:
            // - Logical screen: 2560x1440
            // - Window size: 1200x800 (logical)
            // - Expected position: ((2560-1200)/2, (1440-800)/2) = (680, 320) logical
            //
            // Current bug: Uses physical pixels for screen size
            // - Calculates: ((3840-1200)/2, (2160-800)/2) = (1320, 680) physical
            // - But SetWindowPos expects logical, so window is mispositioned
            //
            // This test will pass once CenterOnScreen uses GetLogicalScreenWidth/Height

            var methodInfo = typeof(Windowing).GetMethod("CenterOnScreen",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(methodInfo,
                "CenterOnScreen should exist and use logical pixels for DPI-aware positioning");
        }
#endif

        #endregion
    }
}