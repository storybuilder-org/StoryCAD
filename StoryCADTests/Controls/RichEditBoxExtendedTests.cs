using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services.IoC;

namespace StoryCADTests.Controls
{
    [TestClass]
    public class RichEditBoxExtendedTests
    {
        [TestInitialize]
        public void Setup()
        {
            // Initialize IoC if not already done
            if (Ioc.Default.GetService<AppState>() == null)
            {
                BootStrapper.Initialise(true);
            }
        }

#if WINDOWS10_0_18362_0_OR_GREATER
        // These tests require UI dispatcher initialization which is only available on Windows with WinAppSDK head
        // They are excluded from desktop head (macOS/Linux) builds

        [TestMethod]
        [Ignore("Requires UI thread - cannot run in headless CI.")]
        public void Constructor_WhenCreated_SetsAcceptsReturnTrue()
        {
            // Arrange & Act
            var richEditBox = new RichEditBoxExtended();

            // Assert
            Assert.IsTrue(richEditBox.AcceptsReturn,
                "AcceptsReturn should be true to enable multi-line text entry");
        }

        [TestMethod]
        [Ignore("Requires UI thread - cannot run in headless CI.")]
        public void Constructor_WhenCreated_SetsTextWrappingToWrap()
        {
            // Arrange & Act
            var richEditBox = new RichEditBoxExtended();

            // Assert
            Assert.AreEqual(TextWrapping.Wrap, richEditBox.TextWrapping,
                "TextWrapping should be set to Wrap for proper text wrapping behavior");
        }

        [TestMethod]
        [Ignore("Requires UI thread - cannot run in headless CI.")]
        public void RtfText_WhenSet_UpdatesCorrectly()
        {
            // Arrange
            var richEditBox = new RichEditBoxExtended();
            var rtfContent = @"{\rtf1 This is plain text content}";

            // Act
            richEditBox.RtfText = rtfContent;

            // Assert
            Assert.IsNotNull(richEditBox.RtfText, "RtfText should not be null after setting");
        }
#endif
    }
}
