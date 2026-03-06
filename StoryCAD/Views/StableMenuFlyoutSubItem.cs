using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace StoryCAD.Views;

/// <summary>
/// MenuFlyoutSubItem that suppresses OnPointerExited to prevent the submenu
/// closing while the cursor crosses the gap between parent item and child popup.
/// Fixes Uno Platform Skia/macOS cascading popup dismissal bug (issue #1323).
/// Light-dismiss on click-outside still works correctly.
/// </summary>
public class StableMenuFlyoutSubItem : MenuFlyoutSubItem
{
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        // Intentionally suppress base call.
    }
}
