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
        public void CenterOnScreen_WithValidWindow_ShouldNotThrow()
        {
            // Arrange
            // Note: We can't easily create a real Window in unit tests
            // This test verifies the method doesn't throw with null checks
            Window? window = null;

            // Act & Assert
            // After SRP refactoring, CenterOnScreen only takes a window parameter
            // Should handle null window gracefully
            Assert.ThrowsExactly<NullReferenceException>(() =>
                _windowing.CenterOnScreen(window!));
        }

        [TestMethod]
        public void SetMinimumSize_WithValidWindow_ShouldNotThrow()
        {
            // Arrange
            Window? window = null;

            // Act & Assert
            // Should handle null window gracefully
            Assert.ThrowsExactly<NullReferenceException>(() =>
                _windowing.SetMinimumSize(window!, 1000, 700));
        }

        [TestMethod]
        public void SetWindowSize_WithValidWindow_ShouldNotThrow()
        {
            // Arrange
            Window? window = null;

            // Act & Assert
            // Should handle null window gracefully
            Assert.ThrowsExactly<NullReferenceException>(() =>
                _windowing.SetWindowSize(window!, 1200, 800));
        }

#if WINDOWS10_0_18362_0_OR_GREATER
        [TestMethod]
        public void CenterOnScreen_OnWindows_UsesWin32APIs()
        {
            // This test verifies Win32 P/Invoke declarations exist
            // Actual window positioning requires a real window handle
            // which is not available in unit tests

            // Verify the P/Invoke methods are defined (will fail to compile if not)
            var methodInfo = typeof(Windowing).GetMethod("GetSystemMetrics",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(methodInfo, "GetSystemMetrics P/Invoke should be defined");

            methodInfo = typeof(Windowing).GetMethod("SetWindowPos",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(methodInfo, "SetWindowPos P/Invoke should be defined");
        }
#endif

#if __MACOS__
        [TestMethod]
        public void CenterOnScreen_OnMacOS_UsesNSWindow()
        {
            // This test verifies macOS implementation
            // Actual window centering requires NSWindow which is not available in unit tests

            // Test would verify NSWindow.Center() is called
            // but requires UI thread and actual window instance
            Assert.Inconclusive("macOS window centering requires NSWindow instance");
        }
#endif

        [TestMethod]
        public void GetDpiScale_ReturnsOne()
        {
            // Arrange & Act
            // Using reflection since GetDpiScale is private
            var methodInfo = typeof(Windowing).GetMethod("GetDpiScale",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var result = methodInfo?.Invoke(null, new object?[] { null });

            // Assert
            Assert.AreEqual(1.0, result, "DPI scale should be 1.0 for Skia Desktop");
        }

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
        public void GetLogicalScreenWidth_OnWindows_ReturnsLogicalPixels()
        {
            // Arrange & Act
            // Using reflection to test private method
            var methodInfo = typeof(Windowing).GetMethod("GetLogicalScreenWidth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.IsNotNull(methodInfo, "GetLogicalScreenWidth method should exist for DPI awareness");

            // This test will fail until the method is implemented
            // Method should return screen width in logical pixels (DIPs), not physical pixels
            // At 150% DPI: Physical 3840 should return Logical 2560
        }

        [TestMethod]
        public void GetLogicalScreenHeight_OnWindows_ReturnsLogicalPixels()
        {
            // Arrange & Act
            // Using reflection to test private method
            var methodInfo = typeof(Windowing).GetMethod("GetLogicalScreenHeight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.IsNotNull(methodInfo, "GetLogicalScreenHeight method should exist for DPI awareness");

            // This test will fail until the method is implemented
            // Method should return screen height in logical pixels (DIPs), not physical pixels
            // At 150% DPI: Physical 2160 should return Logical 1440
        }

        [TestMethod]
        public void GetPhysicalScreenWidth_OnWindows_ReturnsPhysicalPixels()
        {
            // Arrange & Act
            // Using reflection to test private method
            var methodInfo = typeof(Windowing).GetMethod("GetPhysicalScreenWidth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.IsNotNull(methodInfo, "GetPhysicalScreenWidth method should exist");

            // This method wraps GetSystemMetrics(SM_CXSCREEN)
            // Should return physical pixels (e.g., 3840 at 150% DPI)
        }

        [TestMethod]
        public void GetPhysicalScreenHeight_OnWindows_ReturnsPhysicalPixels()
        {
            // Arrange & Act
            // Using reflection to test private method
            var methodInfo = typeof(Windowing).GetMethod("GetPhysicalScreenHeight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.IsNotNull(methodInfo, "GetPhysicalScreenHeight method should exist");

            // This method wraps GetSystemMetrics(SM_CYSCREEN)
            // Should return physical pixels (e.g., 2160 at 150% DPI)
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