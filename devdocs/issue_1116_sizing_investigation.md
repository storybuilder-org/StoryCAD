# Issue 1116 - RichEditBoxExtended Horizontal Sizing Problem

## Problem Description
The `RichEditBoxExtended` control (TextBox-based on desktop head) in the Story Idea tab does NOT stretch horizontally to fill its container on initial app load.

**Observed Behavior:**
- On app open: Control too narrow, text cut off on right, right border not visible
- After manually widening window: Control expands correctly to fill width
- Switching tabs and back: No change (problem persists)

**Expected Behavior:**
- Control should stretch to fill container width on initial load
- Control should show right border immediately

## What We Know
1. Other TextBoxes on same page (Author, Date Created, Last Changed) stretch correctly
2. Problem is specific to RichEditBoxExtended in Grid.Row="2" 
3. Control CAN resize correctly (proves after window resize)
4. Not a content issue - bounds should be independent of content
5. Only testing Story Idea tab (other tabs broken - no RichEditBox impl in UNO)

## Current Implementation

**Control:** `StoryCADLib/Controls/RichEditBoxExtended.desktop.cs`
```csharp
public partial class RichEditBoxExtended : TextBox
{
    public RichEditBoxExtended()
    {
        TextWrapping = TextWrapping.Wrap;
        AcceptsReturn = true;
        ScrollViewer.SetHorizontalScrollBarVisibility(this, ScrollBarVisibility.Disabled);
        ScrollViewer.SetHorizontalScrollMode(this, ScrollMode.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(this, ScrollBarVisibility.Auto);
        ScrollViewer.SetZoomMode(this, ZoomMode.Disabled);
        TextChanged += RichEditBoxExtended_TextChanged;
    }
}
```

**XAML:** `StoryCAD/Views/OverviewPage.xaml` (lines 43-56)
```xaml
<usercontrols:RichEditBoxExtended
    Grid.Row="2"
    x:Name="StoryIdeaBox"
    Header="Story Idea"
    TextWrapping="Wrap"
    HorizontalAlignment="Stretch"
    VerticalAlignment="Stretch"
    AcceptsReturn="True"
    IsSpellCheckEnabled="True"
    RtfText="{x:Bind OverviewVm.Description, Mode=TwoWay}"
    ScrollViewer.HorizontalScrollBarVisibility="Disabled"
    ScrollViewer.HorizontalScrollMode="Disabled"
    ScrollViewer.VerticalScrollBarVisibility="Auto"
    ScrollViewer.ZoomMode="Disabled"/>
```

**Parent Layout:**
- Grid with one column: `<ColumnDefinition Width="*"/>`
- Control in Grid.Row="2" with `<RowDefinition Height="*"/>`
- Parent Grid has `HorizontalAlignment="Stretch"`

## What We've Tried (All Failed)

1. ❌ Added `HorizontalAlignment = HorizontalAlignment.Stretch;` in constructor
2. ❌ Added `HorizontalContentAlignment = HorizontalAlignment.Stretch;` in constructor
3. ❌ Added explicit `Grid.Column="0"` in XAML
4. ❌ Removed all ScrollViewer.Set* calls from constructor
5. ❌ Wrapped control in `<Border>` element

## Analysis
- Problem appears to be during initial layout measurement pass
- Grid should provide available width during measure
- TextBox with HorizontalAlignment="Stretch" should measure to fill it
- Control works correctly on resize (proves it CAN measure correctly)
- Likely a UNO Platform TextBox measurement bug on desktop head, or timing issue with Pivot layout

## Next Steps to Investigate
1. Override `MeasureOverride` with diagnostic logging to see what `availableSize.Width` is passed
2. Compare initial load vs resize to see if width differs
3. Check if this is a known UNO Platform issue with TextBox on desktop head
4. Try programmatically forcing InvalidateMeasure after Loaded event

## Backup Location
`/mnt/c/temp/issue_1116_current_file_backups/`

## Build Status
✅ Desktop head compiles successfully (0 errors, 18 warnings)
