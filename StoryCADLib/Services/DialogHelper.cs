// Dialogs.cs
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement; // Uno projects this API cross-platform

namespace StoryCAD.Services
{
    public static class DialogHelper
    {
        // Same entry point name/signature you've been using
        public static async Task<ContentDialogResult> ShowAsync(ContentDialog dialog, XamlRoot root, double baseMaxWidthDip = 540)
        {
            if (dialog is null) throw new ArgumentNullException(nameof(dialog));
            if (root   is null) throw new ArgumentNullException(nameof(root));

            dialog.XamlRoot = root;

            // Always honor system text scaling (Windows and Uno heads).
            double t = new UISettings().TextScaleFactor;

            // Prevent overflow on small viewports (phones/tablets).
            double viewport = root.Size.Width > 0 ? root.Size.Width : baseMaxWidthDip;
            dialog.MaxWidth = Math.Min(Math.Round(baseMaxWidthDip * Math.Max(1.0, t)), Math.Max(320, viewport - 32));

            return await dialog.ShowAsync();
        }
    }
}