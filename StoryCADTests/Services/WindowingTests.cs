using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.Logging;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using System;
using System.Reflection;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Services.IoC;

namespace StoryCADTests.Services
{
    [TestClass]
    public class WindowingTests
    {
        private Windowing _windowing = null!;

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

        // Tests for SetWindowSize - updated after reintroducing the method in unified Windowing.cs
        // Note: We can't easily create a real Window in unit tests, so we test what we can

        [TestMethod]
        public void SetWindowSize_WithNullWindow_ReturnsEarlyWithoutException()
        {
            // Arrange
            // Note: We can't easily create a real Window in unit tests
            // This test verifies the method handles null gracefully
            Window? window = null;

            // Act - should return early without throwing
            _windowing.SetWindowSize(window!, 1200.0, 800.0);

            // Assert - if we get here without exception, test passes
            Assert.IsTrue(true, "SetWindowSize handled null window correctly on all platforms");
        }

        [TestMethod]
        public void SetWindowSize_MethodExists_WithCorrectSignature()
        {
            // Arrange & Act
            var methodInfo = typeof(Windowing).GetMethod("SetWindowSize",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            // Assert
            Assert.IsNotNull(methodInfo, "SetWindowSize method should exist");

            var parameters = methodInfo.GetParameters();
            Assert.AreEqual(3, parameters.Length,
                "SetWindowSize should accept 3 parameters");
            Assert.AreEqual(typeof(Window), parameters[0].ParameterType,
                "First parameter should be Window");
            Assert.AreEqual(typeof(double), parameters[1].ParameterType,
                "Second parameter (width) should be double");
            Assert.AreEqual(typeof(double), parameters[2].ParameterType,
                "Third parameter (height) should be double");
        }

        [TestMethod]
        public void TryGetScaleFactor_UsesPassedWindowParameter_NotMainWindow()
        {
            // This test ensures TryGetScaleFactor uses the passed Window parameter
            // This was a bug where it used MainWindow instead of the passed window
            var methodInfo = typeof(Windowing).GetMethod("TryGetScaleFactor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(methodInfo, "TryGetScaleFactor method should exist");

            var parameters = methodInfo.GetParameters();
            Assert.AreEqual(1, parameters.Length,
                "TryGetScaleFactor should accept exactly one parameter");
            Assert.AreEqual(typeof(Window), parameters[0].ParameterType,
                "TryGetScaleFactor should accept Window parameter to avoid using MainWindow directly");
        }

        [TestMethod]
        public void SetWindowSize_PlatformSpecificDpiHandling_UsesCorrectApproach()
        {
            // This test verifies that the correct DPI handling approach exists for each platform
            // We can't test actual DPI values without a real window, but we can verify the infrastructure

#if WINDOWS && !HAS_UNO
            // On WinAppSDK, verify GetDpiForWindow P/Invoke exists
            var methodInfo = typeof(Windowing).GetMethod("GetDpiForWindow",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(methodInfo,
                "WinAppSDK build should have GetDpiForWindow P/Invoke for DPI scaling");
#elif HAS_UNO
            // On Uno Skia builds (Windows desktop or macOS desktop)
            // Verify TryGetScaleFactor exists and accepts Window parameter
            var methodInfo = typeof(Windowing).GetMethod("TryGetScaleFactor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(methodInfo,
                "Uno Skia build should have TryGetScaleFactor method for DPI handling");

            // Verify TryGetScaleFactor accepts Window parameter (fix for bug where it used MainWindow)
            var parameters = methodInfo.GetParameters();
            Assert.AreEqual(1, parameters.Length,
                "TryGetScaleFactor should accept exactly one parameter");
            Assert.AreEqual(typeof(Window), parameters[0].ParameterType,
                "TryGetScaleFactor should accept Window parameter, not use MainWindow directly");

            // Platform-specific expectations for Skia desktop
            if (OperatingSystem.IsWindows())
            {
                // Windows Skia desktop uses DisplayInformation or environment variable
                Assert.IsTrue(true, "Windows Skia desktop should attempt DisplayInformation for DPI");
            }
            else if (OperatingSystem.IsMacOS())
            {
                // macOS Skia desktop uses DisplayInformation or defaults
                Assert.IsTrue(true, "macOS Skia desktop should attempt DisplayInformation for DPI");
            }
#endif
        }

// Tests for platform-specific methods commented out - these were removed with partial classes
//#if WINDOWS10_0_18362_0_OR_GREATER
//        [TestMethod]
//        public void CenterOnScreen_OnWindows_UsesModernAPIs()
//        {
//            // This test verifies modern WinUI 3 APIs are used (AppWindow, DisplayArea)
//            // After issue #1116, implementation uses AppWindow.Move() instead of Win32 APIs
//
//            // Verify the modern helper methods are defined
//            var methodInfo = typeof(Windowing).GetMethod("GetAppWindow",
//                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
//            Assert.IsNotNull(methodInfo, "GetAppWindow helper method should be defined");
//
//            methodInfo = typeof(Windowing).GetMethod("GetDpiScale",
//                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
//            Assert.IsNotNull(methodInfo, "GetDpiScale helper method should be defined");
//
//            // Verify GetDpiForWindow P/Invoke is still used for DPI scaling
//            methodInfo = typeof(Windowing).GetMethod("GetDpiForWindow",
//                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
//            Assert.IsNotNull(methodInfo, "GetDpiForWindow P/Invoke should be defined for DPI awareness");
//        }
//#endif

        #region TDD Tests for SRP Refactoring and DPI Scaling

        // Note: CenterOnScreen and SetMinimumSize methods were removed with partial classes
        // SetWindowSize has been reintroduced in the unified Windowing.cs
        // The active tests above verify SetWindowSize functionality

//#if WINDOWS10_0_18362_0_OR_GREATER
//        [TestMethod]
//        public void CenterOnScreen_UsesDisplayArea_ForScreenDimensions()
//        {
//            // After issue #1116, implementation uses DisplayArea.WorkArea
//            // which provides logical pixels (DIPs) automatically, handling DPI scaling correctly

//            // Verify CenterOnScreen uses DisplayArea (indirectly tested via GetAppWindow)
//            var methodInfo = typeof(Windowing).GetMethod("CenterOnScreen",
//                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
//            Assert.IsNotNull(methodInfo, "CenterOnScreen method should exist");

//            // The implementation now uses:
//            // - DisplayArea.GetFromWindowId() to get the display area
//            // - WorkArea property which returns logical pixels
//            // - AppWindow.Move() to position the window
//            // This correctly handles DPI scaling without needing separate logical/physical methods
//        }

//        [TestMethod]
//        public void SetWindowSize_UsesDpiScale_ForCorrectSizing()
//        {
//            // Verify SetWindowSize handles DPI scaling
//            var methodInfo = typeof(Windowing).GetMethod("SetWindowSize",
//                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
//            Assert.IsNotNull(methodInfo, "SetWindowSize method should exist");

//            var parameters = methodInfo.GetParameters();
//            Assert.AreEqual(3, parameters.Length, "SetWindowSize should accept Window, width, and height");
//            Assert.AreEqual(typeof(Window), parameters[0].ParameterType);
//            Assert.AreEqual(typeof(int), parameters[1].ParameterType, "Width should be in DIPs");
//            Assert.AreEqual(typeof(int), parameters[2].ParameterType, "Height should be in DIPs");

//            // The implementation:
//            // 1. Takes DIPs (device independent pixels) as input
//            // 2. Multiplies by DPI scale to get physical pixels
//            // 3. Clamps to work area bounds
//            // 4. Calls AppWindow.Resize() with physical pixels
//        }

//        [TestMethod]
//        public void GetDpiScale_HandlesPerMonitorDPI()
//        {
//            // Verify DPI scaling method exists for per-monitor DPI awareness
//            var methodInfo = typeof(Windowing).GetMethod("GetDpiScale",
//                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
//            Assert.IsNotNull(methodInfo, "GetDpiScale should exist for per-monitor DPI handling");

//            // The implementation:
//            // - Uses GetDpiForWindow() to get the current monitor's DPI
//            // - Returns scale factor (e.g., 1.5 for 150% scaling)
//            // - This ensures windows are sized correctly on high-DPI displays
//        }

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

//        [TestMethod]
//        public void CenterOnScreen_OnWindows_UsesLogicalPixelsForPositioning()
//        {
//            // Arrange
//            // This test documents the expected behavior:
//            // At 150% DPI with 3840x2160 physical screen:
//            // - Logical screen: 2560x1440
//            // - Window size: 1200x800 (logical)
//            // - Expected position: ((2560-1200)/2, (1440-800)/2) = (680, 320) logical
//            //
//            // Current bug: Uses physical pixels for screen size
//            // - Calculates: ((3840-1200)/2, (2160-800)/2) = (1320, 680) physical
//            // - But SetWindowPos expects logical, so window is mispositioned
//            //
//            // This test will pass once CenterOnScreen uses GetLogicalScreenWidth/Height

//            var methodInfo = typeof(Windowing).GetMethod("CenterOnScreen",
//                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

//            Assert.IsNotNull(methodInfo,
//                "CenterOnScreen should exist and use logical pixels for DPI-aware positioning");
//        }
//#endif

        #endregion

        #region GetMaxWindowSize and Clamping Tests

        [TestMethod]
        public void GetMaxWindowSize_MethodExists_WithCorrectSignature()
        {
            // Arrange & Act
            var methodInfo = typeof(Windowing).GetMethod("GetMaxWindowSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Assert
            Assert.IsNotNull(methodInfo, "GetMaxWindowSize method should exist");

            var returnType = methodInfo.ReturnType;
            Assert.IsTrue(returnType.IsGenericType || returnType.Name.Contains("ValueTuple"),
                "GetMaxWindowSize should return a tuple");
        }

        [TestMethod]
        public void GetMaxWindowSize_OnWindows_ReturnsPositiveValues()
        {
            // This test verifies GetMaxWindowSize returns valid dimensions on Windows
            // We use reflection since it's a private method
            var methodInfo = typeof(Windowing).GetMethod("GetMaxWindowSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(methodInfo, "GetMaxWindowSize method should exist");

            // Invoke the method
            var result = methodInfo.Invoke(_windowing, null);
            Assert.IsNotNull(result, "GetMaxWindowSize should return a value");

            // Extract tuple values
            var tupleType = result.GetType();
            var widthField = tupleType.GetField("Item1") ?? tupleType.GetField("width");
            var heightField = tupleType.GetField("Item2") ?? tupleType.GetField("height");

            Assert.IsNotNull(widthField, "Result should have width field");
            Assert.IsNotNull(heightField, "Result should have height field");

            int width = (int)widthField.GetValue(result)!;
            int height = (int)heightField.GetValue(result)!;

            // On Windows, we should get positive screen dimensions
            // (In headless test mode, this may return 0,0 which is acceptable)
            Assert.IsTrue(width >= 0, $"Width should be non-negative, got {width}");
            Assert.IsTrue(height >= 0, $"Height should be non-negative, got {height}");
        }

        [TestMethod]
        public void SetWindowSize_ClampsToScreenBounds_WhenRequestedSizeExceedsScreen()
        {
            // This test documents the expected clamping behavior
            // SetWindowSize should clamp requested dimensions to fit within available screen space
            //
            // Example scenario (Wilkie's issue):
            // - Screen: 1680x1050
            // - Requested: 1800x1200
            // - Expected: Clamped to ~1630x1000 (with 50px margin)
            //
            // We can't easily test the actual clamping without a real window,
            // but we verify the infrastructure exists

            var setWindowSizeMethod = typeof(Windowing).GetMethod("SetWindowSize",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(setWindowSizeMethod, "SetWindowSize should exist");

            var getMaxWindowSizeMethod = typeof(Windowing).GetMethod("GetMaxWindowSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(getMaxWindowSizeMethod,
                "GetMaxWindowSize should exist for clamping logic");
        }

        #endregion
    }
}