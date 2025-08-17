# Name Synchronization Between StoryElement and StoryNodeItem

## Issue Context
- **Discovered During**: Issue #1069 test coverage work
- **Date**: 2025-01-17
- **Affects**: Tests that modify StoryElement.Name after creation

## Problem Description
During test failure investigation, we discovered that StoryElement.Name and StoryNodeItem.Name are only synchronized at creation time, not when subsequently modified.

### Current Behavior

#### At Creation Time
When a StoryElement is created via `AddStoryElement()`:
1. StoryElement constructor sets its `_name` field
2. StoryElement creates its StoryNodeItem: `_node = new(this, parentNode, type)`
3. StoryNodeItem constructor copies the name: `Name = node.Name` (line 282 of StoryNodeItem.cs)
4. **Result**: Names are synchronized at this point

#### After Creation
When StoryElement.Name is modified:
```csharp
// StoryElement.cs, lines 29-32
public string Name
{
    get => _name;
    set => _name = value;  // Just sets field, no notification to Node
}
```
**Result**: StoryElement name changes, but StoryNodeItem name remains unchanged

### How This Works in the UI
In the actual application:
- User changes name in TreeView (bound to StoryNodeItem.Name)
- UI data binding propagates change to StoryElement.Name
- Both stay synchronized through UI binding mechanisms

### How This Fails in API/Tests
In headless scenarios (API calls, unit tests):
- No UI binding exists
- Setting `storyElement.Name = "New Name"` doesn't update `storyElement.Node.Name`
- Setting `storyNodeItem.Name = "New Name"` doesn't update the associated StoryElement's name

## Affected Tests
Tests that fail due to this issue:
1. `ConvertProblemToScene_WithChildren_MovesChildren` - Sets child.Name, checks node.Children names
2. `ConvertSceneToProblem_WithChildren_MovesChildren` - Same pattern

Example of the problem:
```csharp
// Test sets StoryElement name
child1.Name = "Child Character 1";

// Later checks StoryNodeItem name - FAILS
Assert.IsTrue(scene.Node.Children.Any(c => c.Name == "Child Character 1"));
// c.Name is still "New Character" (the default)
```

## Design Considerations

### Option 1: Accept Current Behavior
- Document that names only sync at creation
- API users must set names before adding elements, or update both if needed
- Fix tests to work with this limitation

### Option 2: Add Bidirectional Sync
- Make StoryElement.Name setter also update Node.Name
- Make StoryNodeItem.Name setter also update associated StoryElement.Name
- Ensures consistency in all scenarios

### Option 3: One-Way Sync from StoryElement
- Since StoryElement is the data model (primary in new architecture)
- Make StoryElement.Name setter update Node.Name
- StoryNodeItem.Name becomes read-only or updates StoryElement

## Implications

### For API Users
- Currently must be aware that changing StoryElement.Name after creation doesn't update the tree
- May cause confusion if they expect the tree to reflect name changes

### For Testing
- Tests must either:
  - Set names before/during creation
  - Update both StoryElement.Name and Node.Name
  - Check the correct property based on what was set

### For UI Consistency
- Current UI works fine due to data binding
- Any fix should maintain this working behavior

## Solution Implemented
After discussion, we implemented **Option 3: One-Way Sync from StoryElement**.

The StoryElement.Name setter now updates the Node's name:
```csharp
public string Name
{
    get => _name;
    set
    {
        _name = value;
        // Keep the node synchronized when name changes from API
        if (_node != null)
            _node.Name = value;
    }
}
```

This ensures:
- **API operations**: When StoryElement.Name changes, Node.Name automatically updates
- **UI operations**: UI binding keeps both in sync as before
- **Consistency**: The data model (StoryElement) is authoritative and pushes changes to the view model (StoryNodeItem)

## Tests Fixed
With this change, the following tests now pass without manual synchronization:
- `ConvertProblemToScene_WithChildren_MovesChildren`
- `ConvertSceneToProblem_WithChildren_MovesChildren`

## Related Issues
- Similar to the redundant reference removal methods, this reveals an architectural boundary between UI-driven and API-driven operations
- Part of the larger transition from node-centric to element-centric architecture
- This solution aligns with the new architecture where StoryElement is primary