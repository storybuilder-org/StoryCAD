# Status Bar Buttons Plan for StoryWorld

**Created:** 2026-01-20
**Status:** Pending review

## Problem Statement

StoryWorldPage has 5 list-based tabs (Physical World, Species, Cultures, Governments, Religions) that allow multiple entries. Users need to Add and Remove entries from these lists.

### Current Implementation (problematic)
- Each list tab had a centered "+ Add World" button at the top
- When the tab is empty, this creates a lot of wasted vertical space
- Delete buttons are trash icons on each entry card
- We tried moving the Add button to the page header (next to title) but it overlaps with the title text

### User's Preferred Solution
- Move Add/Remove buttons to the Shell status bar (bottom of window, near "Story Explorer View" dropdown)
- Buttons should only appear when StoryWorldPage is active
- Button labels should be context-sensitive to the selected tab (e.g., "+ Add World", "- Remove World")
- Remove button should remove the currently selected entry in the list

## Proposed Architecture

### 1. New Message Classes (in Services/Messages/)

```csharp
// Sent when navigating to/from StoryWorldPage
public class StoryWorldPageActiveMessage
{
    public bool IsActive { get; set; }
    public int TabIndex { get; set; }
    public string TabName { get; set; }
    public bool HasEntries { get; set; }
    public bool HasSelection { get; set; }
}

// Sent when tab selection changes within StoryWorldPage
public class StoryWorldTabChangedMessage
{
    public int TabIndex { get; set; }
    public string TabName { get; set; }
    public bool HasEntries { get; set; }
    public bool HasSelection { get; set; }
}

// Sent when list selection changes
public class StoryWorldSelectionChangedMessage
{
    public bool HasSelection { get; set; }
}

// Sent by Shell when Add button clicked
public class AddStoryWorldEntryMessage { }

// Sent by Shell when Remove button clicked
public class RemoveStoryWorldEntryMessage { }
```

### 2. ShellViewModel Changes

Add properties:
- `StoryWorldAddButtonVisibility` - Visibility, bound to Add button
- `StoryWorldRemoveButtonVisibility` - Visibility, bound to Remove button
- `StoryWorldAddButtonLabel` - string, e.g., "+ Add World"
- `StoryWorldRemoveButtonLabel` - string, e.g., "- Remove World"

Add commands:
- `AddStoryWorldEntryCommand` - RelayCommand that sends `AddStoryWorldEntryMessage`
- `RemoveStoryWorldEntryCommand` - RelayCommand that sends `RemoveStoryWorldEntryMessage`

Add message handlers:
- Handle `StoryWorldPageActiveMessage` - show/hide buttons, update labels
- Handle `StoryWorldTabChangedMessage` - update labels and Remove visibility
- Handle `StoryWorldSelectionChangedMessage` - update Remove button enabled state

### 3. Shell.xaml Changes

Add to status bar (right of edit icon):
```xml
<Button Content="{x:Bind ShellVm.StoryWorldAddButtonLabel, Mode=OneWay}"
        Command="{x:Bind ShellVm.AddStoryWorldEntryCommand}"
        Visibility="{x:Bind ShellVm.StoryWorldAddButtonVisibility, Mode=OneWay}"/>

<Button Content="{x:Bind ShellVm.StoryWorldRemoveButtonLabel, Mode=OneWay}"
        Command="{x:Bind ShellVm.RemoveStoryWorldEntryCommand}"
        Visibility="{x:Bind ShellVm.StoryWorldRemoveButtonVisibility, Mode=OneWay}"/>
```

### 4. StoryWorldViewModel Changes

Add selected item properties:
- `SelectedPhysicalWorld` - PhysicalWorldEntry
- `SelectedSpecies` - SpeciesEntry
- `SelectedCulture` - CultureEntry
- `SelectedGovernment` - GovernmentEntry
- `SelectedReligion` - ReligionEntry

Send messages:
- On page activate/deactivate: `StoryWorldPageActiveMessage`
- On tab change: `StoryWorldTabChangedMessage`
- On selection change: `StoryWorldSelectionChangedMessage`

Handle incoming messages:
- `AddStoryWorldEntryMessage` - call appropriate Add method based on SelectedTabIndex
- `RemoveStoryWorldEntryMessage` - remove the selected entry from appropriate list

### 5. StoryWorldPage.xaml Changes

- Switch from `ItemsRepeater` to `ListView` for selection support
- Bind `SelectedItem` to ViewModel selected properties
- Remove the header Add button that was overlapping title
- Keep card-style DataTemplate (just inside ListView)

### 6. StoryWorldPage.xaml.cs Changes

- Send `StoryWorldPageActiveMessage(true, ...)` in `OnNavigatedTo`
- Send `StoryWorldPageActiveMessage(false, ...)` in `OnNavigatedFrom`

## Message Flow

```
StoryWorldPage                     ShellViewModel                StoryWorldViewModel
     |                                   |                              |
     |--[Navigate To]------------------>|                              |
     |                                   |                              |
     |--[PageActiveMessage(true)]------->| Show buttons                 |
     |                                   | Set labels                   |
     |                                   |                              |
     |--[TabChangedMessage]------------->| Update labels                |
     |                                   | Update Remove visibility     |
     |                                   |                              |
     |--[SelectionChangedMessage]------->| Enable/disable Remove        |
     |                                   |                              |
     |                                   |--[User clicks Add]           |
     |                                   |--[AddEntryMessage]---------->| Add to list
     |                                   |                              |
     |                                   |--[User clicks Remove]        |
     |                                   |--[RemoveEntryMessage]------->| Remove selected
     |                                   |                              |
     |--[Navigate Away]---------------->|                              |
     |--[PageActiveMessage(false)]------>| Hide buttons                 |
```

## Complexity Concerns

This approach involves:
- 5 new message classes
- ~6 new properties in ShellViewModel
- ~2 new commands in ShellViewModel
- ~3 message handlers in ShellViewModel
- ~5 new selected item properties in StoryWorldViewModel
- ~2 message handlers in StoryWorldViewModel
- Switching 5 tabs from ItemsRepeater to ListView
- Coordinating state across 3 components (Page, ShellViewModel, StoryWorldViewModel)

## Questions for Review

1. Is there a simpler way to achieve the same UX goal?
2. Could we avoid the cross-component messaging complexity?
3. Are there existing patterns in the codebase we should follow?
4. Would a different UI approach (not status bar) be simpler while still solving the wasted space problem?
