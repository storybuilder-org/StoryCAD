#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace StoryCAD.Views;

public class StableMenuFlyoutSubItem : MenuFlyoutSubItem
{
    private CancellationTokenSource? _closeCts;
    private CancellationTokenSource? _timeoutCts;
    private bool _isSubMenuOpen;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("SubMenuFlyout") is MenuFlyout subFlyout)
        {
            subFlyout.Opened += (s, e) =>
            {
                _isSubMenuOpen = true;
                StartTimeoutClose();
                WatchParent(subFlyout);
            };
            subFlyout.Closed += (s, e) =>
            {
                _isSubMenuOpen = false;
                _timeoutCts?.Cancel();
            };
        }
    }

    private void WatchParent(MenuFlyout subFlyout)
    {
        // Walk up to find the parent MenuFlyout and close submenu if it closes
        if (Parent is MenuFlyoutPresenter { Parent: Popup { Parent: FlyoutPresenter { } } } ||
            FindParentMenuFlyout() is MenuFlyout parentFlyout)
        {
            var pf = FindParentMenuFlyout();
            if (pf is not null)
                pf.Closed += (s, e) => subFlyout.Hide();
        }
    }

    private MenuFlyout? FindParentMenuFlyout()
    {
        DependencyObject? current = this;
        while (current is not null)
        {
            if (current is MenuFlyout mf) return mf;
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private async void StartTimeoutClose()
    {
        _timeoutCts?.Cancel();
        _timeoutCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(30_000, _timeoutCts.Token);
            if (_isSubMenuOpen)
                base.OnPointerExited(null!);
        }
        catch (TaskCanceledException) { }
    }

    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        _closeCts?.Cancel();
        base.OnPointerEntered(e);
    }

    protected override async void OnPointerExited(PointerRoutedEventArgs e)
    {
        _closeCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(350, _closeCts.Token);
            if (!_isSubMenuOpen)
                base.OnPointerExited(e);
        }
        catch (TaskCanceledException) { }
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        _closeCts?.Cancel();
        base.OnPointerPressed(e);
    }
}