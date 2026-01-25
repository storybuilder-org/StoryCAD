# Issue #25 - Responsive Layout Research

**Issue:** [StoryCAD #25 - Responsive Layout](https://github.com/storybuilder-org/StoryCAD/issues/25)
**Last Updated:** 2026-01-25

---

## Current Implementation

### Shell.xaml Structure

```
Page (Shell)
├── Grid
│   ├── Row 0: CommandBar (toolbar with hamburger button)
│   ├── Row 1: SplitView
│   │   ├── Pane: ScrollViewer → TreeView (navigation)
│   │   └── Content: Frame (story element pages)
│   └── Row 2: Status Bar
```

### Key Code Locations

| File | Purpose |
|------|---------|
| `StoryCAD/Views/Shell.xaml` | SplitView layout, hamburger button |
| `StoryCAD/Views/Shell.xaml.cs` | `ShellPage_SizeChanged`, `AdjustSplitViewPane` |
| `StoryCADLib/ViewModels/ShellViewModel.cs` | `IsPaneOpen`, `TogglePane()` |

### Current Size Handling

```csharp
// Shell.xaml.cs
private void ShellPage_SizeChanged(object sender, SizeChangedEventArgs e)
{
    AdjustSplitViewPane(e.NewSize.Width);
}

private void AdjustSplitViewPane(double width)
{
    if (ShellSplitView != null && ShellSplitView.IsPaneOpen)
    {
        var pane = Math.Max(200, width * 0.3);
        ShellSplitView.OpenPaneLength = pane;
    }
}
```

**Current behavior:** Pane width is 30% of window (minimum 200px). No orientation detection.

---

## Windows App SDK APIs

Source: [Microsoft Docs - Manage app windows](https://learn.microsoft.com/en-us/windows/apps/develop/ui/manage-app-windows)

### AppWindow Class

Primary abstraction for window management. Provides 1:1 mapping with HWND.

```csharp
// Get AppWindow from current window
AppWindow appWindow = AppWindow.GetFromWindowId(
    XamlRoot.ContentIslandEnvironment.AppWindowId);

// Read window size
var width = appWindow.Size.Width;
var height = appWindow.Size.Height;

// Detect orientation
bool isPortrait = height > width;
```

### AppWindow.Changed Event

```csharp
appWindow.Changed += AppWindow_Changed;

private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
{
    if (args.DidSizeChange)
    {
        bool isPortrait = sender.Size.Height > sender.Size.Width;
        // Update layout based on orientation
    }
}
```

### DisplayArea API

Get available screen workspace:

```csharp
RectInt32? workArea = DisplayArea.GetFromWindowId(
    appWindow.Id,
    DisplayAreaFallback.Nearest)?.WorkArea;
```

### Presenter Constraints

Set minimum/maximum window sizes:

```csharp
OverlappedPresenter presenter = OverlappedPresenter.Create();
presenter.PreferredMinimumWidth = 420;
presenter.PreferredMinimumHeight = 550;
AppWindow.SetPresenter(presenter);
```

---

## SplitView Display Modes

| Mode | Behavior |
|------|----------|
| `Inline` | Pane and content side-by-side (current) |
| `Overlay` | Pane overlays content when open |
| `CompactInline` | Narrow pane always visible, expands on open |
| `CompactOverlay` | Narrow pane always visible, overlays on open |

### Proposed Behavior by Orientation

| Orientation | DisplayMode | Hamburger Action |
|-------------|-------------|------------------|
| Landscape (wide) | `Inline` | Toggle `IsPaneOpen` |
| Portrait (narrow) | `Overlay` | Toggle `IsPaneOpen` (pane covers content) |

---

## Visual State Manager Approach

Define states based on window width:

```xml
<VisualStateManager.VisualStateGroups>
    <VisualStateGroup>
        <VisualState x:Name="WideState">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="800" />
            </VisualState.StateTriggers>
            <VisualState.Setters>
                <Setter Target="ShellSplitView.DisplayMode" Value="Inline" />
            </VisualState.Setters>
        </VisualState>
        <VisualState x:Name="NarrowState">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="0" />
            </VisualState.StateTriggers>
            <VisualState.Setters>
                <Setter Target="ShellSplitView.DisplayMode" Value="Overlay" />
                <Setter Target="ShellSplitView.IsPaneOpen" Value="False" />
            </VisualState.Setters>
        </VisualState>
    </VisualStateGroup>
</VisualStateManager.VisualStateGroups>
```

**Advantage:** Declarative, no code-behind needed for basic adaptation.

---

## UNO Platform Research

### AdaptiveTrigger Support

Source: [UNO Platform - AdaptiveTrigger](https://github.com/unoplatform/uno/blob/master/doc/articles/features/AdaptiveTrigger.md)

UNO Platform fully supports `AdaptiveTrigger` for responsive design. Key points:

- Triggers must be ordered from **largest to smallest** `MinWindowWidth` for correct evaluation
- Works cross-platform (Windows, macOS, iOS, Android, WebAssembly)

```xml
<!-- Correct ordering: largest to smallest -->
<VisualStateGroup>
    <VisualState x:Name="LargeScreen">
        <VisualState.StateTriggers>
            <AdaptiveTrigger MinWindowWidth="1000"/>
        </VisualState.StateTriggers>
        <VisualState.Setters>
            <Setter Target="Block1.FontSize" Value="40"/>
        </VisualState.Setters>
    </VisualState>

    <VisualState x:Name="MediumScreen">
        <VisualState.StateTriggers>
            <AdaptiveTrigger MinWindowWidth="720"/>
        </VisualState.StateTriggers>
        <VisualState.Setters>
            <Setter Target="Block1.FontSize" Value="30"/>
        </VisualState.Setters>
    </VisualState>

    <VisualState x:Name="SmallScreen">
        <VisualState.StateTriggers>
            <AdaptiveTrigger MinWindowWidth="200"/>
        </VisualState.StateTriggers>
        <VisualState.Setters>
            <Setter Target="Block1.FontSize" Value="20"/>
        </VisualState.Setters>
    </VisualState>
</VisualStateGroup>
```

### Device Orientation Detection (Mobile)

Source: [UNO Platform - Orientation Sensor](https://github.com/unoplatform/uno/blob/master/doc/articles/features/orientation-sensor.md)

For mobile platforms (iOS/Android), use `SimpleOrientationSensor`:

```csharp
var simpleOrientationSensor = SimpleOrientationSensor.GetDefault();
simpleOrientationSensor.OrientationChanged += SimpleOrientationSensor_OrientationChanged;

private async void SimpleOrientationSensor_OrientationChanged(
    object sender, SimpleOrientationSensorOrientationChangedEventArgs args)
{
    // Event handler runs on background thread - dispatch to UI thread
    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
    {
        switch (args.Orientation)
        {
            case SimpleOrientation.NotRotated:
                // Portrait
                break;
            case SimpleOrientation.Rotated90DegreesCounterclockwise:
                // Landscape left
                break;
            case SimpleOrientation.Rotated180DegreesCounterclockwise:
                // Portrait upside down
                break;
            case SimpleOrientation.Rotated270DegreesCounterclockwise:
                // Landscape right
                break;
            case SimpleOrientation.Faceup:
            case SimpleOrientation.Facedown:
                // Device flat
                break;
        }
    });
}
```

### iOS Orientation Control

Source: [UNO Platform - Controlling Orientation](https://github.com/unoplatform/uno/blob/master/doc/articles/features/orientation.md)

To control orientation on iOS, configure `info.plist`:

```xml
<!-- Required for DisplayInformation.AutoRotationPreferences to work -->
<!-- Opts out of iPad Multitasking/Split View -->
<key>UIRequiresFullScreen</key>
<true/>
```

### AppWindow Support

Source: [UNO Platform - Window](https://github.com/unoplatform/uno/blob/master/doc/articles/features/windows-ui-xaml-window.md)

UNO Platform supports `AppWindow` for window management:

```csharp
// Set full screen
myWindow.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

// Exit full screen
myWindow.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
```

### ApplicationView for Screen Bounds

Source: [UNO Platform - WindowingExplainer](https://github.com/unoplatform/uno/blob/master/src/Uno.UI/UI/Xaml/Window/WindowingExplainer.md)

The `ApplicationView` class retrieves "visible bounds" on mobile devices (accounting for status bars, navigation bars):

```csharp
// Note: ApplicationView.GetForCurrentView() throws in WinAppSDK
// Use ApplicationView.GetForWindowId() instead for Window-specific instance
```

### NavigationView with PaneDisplayMode

Source: [UNO Platform - Silverlight Migration Guide](https://github.com/unoplatform/uno/blob/master/doc/articles/guides/silverlight-migration/03-review-app-startup.md)

`NavigationView` supports automatic pane display mode:

```xml
<controls:NavigationView x:Name="NavView"
                         PaneDisplayMode="Auto"
                         IsBackButtonVisible="Collapsed">
    <!-- Auto mode adapts based on window width -->
</controls:NavigationView>
```

### Responsive Design Support Summary

Source: [UNO Platform - Supported Features](https://github.com/unoplatform/uno/blob/master/doc/articles/supported-features.md)

UNO Platform responsive design features:
- Layout constraints: Min/Max Width/Height
- StateTriggers for changing control states based on conditions
- DependencyProperty inheritance for property propagation

---

## Implementation Options

### Option A: Visual State Manager Only (Recommended for MVP)

**Pros:**
- Declarative, simple
- Works cross-platform with UNO
- No platform-specific code needed

**Cons:**
- Width-based only, not true orientation detection
- May not handle all edge cases on mobile

**Implementation:**
```xml
<!-- Shell.xaml - order largest to smallest -->
<VisualStateManager.VisualStateGroups>
    <VisualStateGroup>
        <VisualState x:Name="WideState">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="800" />
            </VisualState.StateTriggers>
            <VisualState.Setters>
                <Setter Target="ShellSplitView.DisplayMode" Value="Inline" />
            </VisualState.Setters>
        </VisualState>
        <VisualState x:Name="NarrowState">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="0" />
            </VisualState.StateTriggers>
            <VisualState.Setters>
                <Setter Target="ShellSplitView.DisplayMode" Value="Overlay" />
                <Setter Target="ShellSplitView.IsPaneOpen" Value="False" />
            </VisualState.Setters>
        </VisualState>
    </VisualStateGroup>
</VisualStateManager.VisualStateGroups>
```

### Option B: Code-Behind Orientation Detection

**Pros:**
- More precise control
- Can factor in aspect ratio, not just width
- Better for true mobile orientation handling

**Cons:**
- More code to maintain
- Platform-specific code for mobile sensors

### Option C: Hybrid Approach

Use Visual State Manager for basic width adaptation, add code-behind for:
- Fine-tuned orientation detection on mobile
- Platform-specific adjustments
- User preference override

---

## Related Files to Modify

| File | Changes |
|------|---------|
| `Shell.xaml` | Add VisualStateManager, update SplitView |
| `Shell.xaml.cs` | Orientation detection logic (if Option B/C) |
| `ShellViewModel.cs` | Add orientation-aware properties |
| `Preferences` | Optional: User preference for layout mode |

---

## References

### Microsoft Documentation
- [Windows App SDK - Manage app windows](https://learn.microsoft.com/en-us/windows/apps/develop/ui/manage-app-windows)
- [SplitView control](https://learn.microsoft.com/en-us/windows/apps/design/controls/split-view)
- [Responsive design techniques](https://learn.microsoft.com/en-us/windows/apps/design/layout/responsive-design)
- [Visual State triggers](https://learn.microsoft.com/en-us/windows/apps/design/layout/layouts-with-xaml#adaptive-triggers)

### UNO Platform Documentation
- [AdaptiveTrigger](https://github.com/unoplatform/uno/blob/master/doc/articles/features/AdaptiveTrigger.md)
- [Orientation Sensor](https://github.com/unoplatform/uno/blob/master/doc/articles/features/orientation-sensor.md)
- [Controlling Orientation](https://github.com/unoplatform/uno/blob/master/doc/articles/features/orientation.md)
- [Window Features](https://github.com/unoplatform/uno/blob/master/doc/articles/features/windows-ui-xaml-window.md)
- [Supported Features](https://github.com/unoplatform/uno/blob/master/doc/articles/supported-features.md)
