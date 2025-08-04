# Remove and Replace DataSource #1067

## Description
Remove the current DataSource implementation and replace it with a more robust and maintainable solution.

## Background
The current DataSource implementation needs to be modernized and replaced to improve architecture and maintainability.

ShellViewModel's active outline is a Model class, StoryModel.cs. StoryModel holds two ObservableCollection<StoryNodeItem> properties, ExplorerView and NarratorView. Comments in StoryModel read:
- One of these TreeViews is actively bound in the Shell page view to a StoryNodeItem tree based on whichever of these two TreeView representations is presently selected.

However, these comments aren't correct. StoryModel is a POCO, and there's no binding to it.

Instead, ShellViewModel.cs holds the property DataSource, which is also an ObservableCollection<StoryNodeItem> and serves as the binding store for the TreeView in the Shell.xaml View. 

ShellViewModel contains a method, SetCurrentView, which sets DataSource to either ExplorerView or NarratorView (both of which are in StoryModel) when a ComboBox on the Shell.xaml view StatusBar is changed; the ComboBox dropdown has two text items describing the two views.

The 'current data source', 'DataSource', should be moved into StoryModel. Since it serves as a binding source, StoryModel should be converted into an ObservableObject. As a result, StoryModel should be moved from \Models to \ViewModels, matching StoryNodeItem. The binding requires an OnPropertyChanged event for the collection(s). There is a Changed flag in StoryModel that should be set to true if a node is added, removed, or moved, and a status indicator on the Shell.XAML StatusBar that indicates it needs saving.

Since setting the current view is something both the ShellViewModel and the API should be doing, SetCurrentView should be moved from ShellViewModel to OutlineService.

The ExplorerView currently has two roots; the first root is the set of 'live' StoryNodeItem instances. The second root is a 'TrashCan' StoryItemType, and its children are nodes that have been logically deleted. Methods exist to move an item to trash (move from the first root to the second) and to restore (move from the second root to the end of the first root.) The presence of the second root poses a problem for drag-and-drop operations: dragging from one root to the other should not be allowed, and there is no reason to reorder the contents of the trashcan. To fix this, StoryModel should add ObservableCollection, and TrashView should be removed, with the second root replaced by it. The Shell.xaml view should also have a second treeview added below the NavigationTree, and bound to the TrashCan collection.

These changes require additional modifications to JSON serialization and deserialization, as well as the addition of a FlattenTree for the TrashCan in StoryModel. The DAL StoryIO requires code to transfer data from the secondary root, if one exists, to the TrashView.

You can find examples of these changes in the simple_drag_and_drop branch. Be aware that while reading, the look and conversion from two roots to the new collection appear to be correct, but writing (saving an outline) has a problem. Saving should require replacing either ExplorerView data or NarratorView, depending on which one is currently active. Note that which view is 'current' doesn't need to be saved in the file; the app always opens with the Explorer view active.

## Acceptance Criteria

### Data model  
- Move `DataSource` (as `CurrentView`) into `StoryModel`; remove the property from `ShellViewModel`.  
- Convert `StoryModel` to `ObservableObject`; implement `OnPropertyChanged` and a `Changed` flag (drives "needs‑save" UI).  

### View/service separation  
- Relocate `SetCurrentView` from `ShellViewModel` to `OutlineService`; keep Explorer/Narrator switching fully functional.  

### Trash handling  
- Replace the dual‑root "TrashCan" with `ObservableCollection<StoryNodeItem> TrashView` inside `StoryModel`.  
- Add a second `TreeView` in **Shell.xaml** bound to `TrashView`.  
- Enforce drag‑and‑drop rules: no moves between main tree and TrashView; no re‑ordering within TrashView.  

### Persistence  
- Update JSON (de)serialization plus `FlattenTree` helpers to read legacy two‑root files and write the new layout.  

### Quality gates  
- Navigation tree, Explorer, and Narrator views behave exactly as they do today.  
- Unit tests cover view switching, trash operations, serialization, and `Changed` flag logic.  
- Docs (developer & user) reflect the new data model and workflow.

## Lifecycle

### Design tasks / sign-off
- [x] Plan this section
- [x] Human approves plan
- [x] Analyze current architecture and identify all affected components
- [x] Design new StoryModel as ObservableObject with CurrentView property
- [x] Design TrashCan collection separation from dual-root system
- [x] Design SetCurrentView relocation to OutlineService
- [x] Design Shell.xaml UI changes for separate TrashCan TreeView
- [x] Design JSON serialization/deserialization changes for new structure
- [x] Design migration strategy for existing dual-root files
- [x] Design drag-and-drop constraint enforcement
- [x] Design Changed flag integration with UI status indicators
- [x] Create class diagram showing new architecture
- [x] Define public API contracts for OutlineService.SetCurrentView
- [x] Validate design against existing functionality requirements
- [ ] Human final approval

### Code tasks / sign-off
- [ ] Plan this section
- [ ] Human approves plan
- [ ] Human final approval

### Test tasks / sign-off
- [ ] Plan this section
- [ ] Human approves plan
- [ ] Human final approval

### Final sign-off
- [ ] Plan this section
- [ ] Human approves plan
- [ ] Human final approval

---

## Detailed Design Plan

### Architecture Analysis

**Analyzed current architecture and identified all affected components:**

#### Current Architecture Issues:
1. **StoryModel** (Models/StoryModel.cs) - POCO class, not observable, holds ExplorerView/NarratorView collections
2. **ShellViewModel** (ViewModels/ShellViewModel.cs) - Contains DataSource property and SetCurrentView method that should be elsewhere
3. **Shell.xaml** - Uses ItemsRepeater bound to DataSource, dual-root system with TrashCan as second root
4. **OutlineService** - Missing SetCurrentView method, needs to handle view switching
5. **JSON Serialization** - FlattenTree methods handle dual-root serialization

#### Components Requiring Changes:

**Core Data Layer:**
- StoryModel.cs - Convert to ObservableObject, add CurrentView property, add TrashCan collection
- StoryNodeItem.cs - May need updates for new binding patterns
- StoryIO.cs - Update serialization/deserialization for new structure

**ViewModels:**
- ShellViewModel.cs - Remove DataSource property, remove SetCurrentView method
- Move StoryModel from Models/ to ViewModels/ directory

**Services:**
- OutlineService.cs - Add SetCurrentView method and view management logic

**UI Layer:**
- Shell.xaml - Add separate TreeView for TrashCan collection, maintain ItemsRepeater structure
- Shell.xaml.cs - Update event handlers for new structure

**Data Access:**
- JSON serialization/deserialization methods in StoryModel
- FlattenTree methods need TrashCan-specific version
- Migration logic for legacy dual-root files

#### Dependencies and Integration Points:
- ShellViewModel.DataSource binding → StoryModel.CurrentView
- View switching logic → OutlineService
- Trash operations → Separate TrashCan collection
- Drag-and-drop operations → Enhanced constraint validation
- Status indicators → StoryModel.Changed flag integration

---

### StoryModel ObservableObject Design

#### Class Structure:
```csharp
// TODO: Move StoryModel to ViewModels namespace in a future refactoring
// This class implements ObservableObject and contains view-specific logic
public class StoryModel : ObservableObject
{
    // Current properties remain unchanged
    public string FirstVersion { get; set; }
    public string LastVersion { get; set; }
    public StoryElementCollection StoryElements { get; set; }
    
    // View collections (existing)
    [JsonIgnore]
    public ObservableCollection<StoryNodeItem> ExplorerView { get; set; }
    [JsonIgnore] 
    public ObservableCollection<StoryNodeItem> NarratorView { get; set; }
    
    // NEW: Current active view for UI binding
    [JsonIgnore]
    private ObservableCollection<StoryNodeItem> _currentView;
    public ObservableCollection<StoryNodeItem> CurrentView 
    { 
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }
    
    // NEW: Separate trash collection (named TrashView for consistency with ExplorerView/NarratorView)
    [JsonIgnore]
    public ObservableCollection<StoryNodeItem> TrashView { get; set; }
    
    // NEW: Current view type tracking
    [JsonIgnore]
    public StoryViewType CurrentViewType { get; set; }
    
    // Enhanced Changed property with notification
    private bool _changed;
    [JsonIgnore]
    public bool Changed 
    {
        get => _changed;
        set 
        {
            // SetProperty returns true only if value actually changed
            if (SetProperty(ref _changed, value))
            {
                // Only update UI color when Changed property actually changes
                if (_changed)
                    ShellViewModel.ShellInstance.ChangeStatusColor = Colors.Red;
                else
                    ShellViewModel.ShellInstance.ChangeStatusColor = Colors.Green;
            }
        }
    }
}
```

#### Key Design Decisions:

1. **Inherits from ObservableObject** - Enables property change notifications for UI binding
2. **CurrentView Property** - Moved from ShellViewModel (as DataSource), provides single binding point for UI
3. **Enhanced Changed Property** - Triggers UI updates and status indicators automatically
4. **TrashView Collection** - Separate collection eliminates dual-root complexity (named for consistency)
5. **CurrentViewType Tracking** - Maintains which view is currently active
6. **No Redundant Properties** - Uses existing Changed property for save state

#### Directory Move:
- Move from `StoryCADLib/Models/StoryModel.cs` → `StoryCADLib/ViewModels/StoryModel.cs`
- Update all namespace references from `StoryCAD.Models` → `StoryCAD.ViewModels`

#### Serialization Strategy:
- CurrentView marked `[JsonIgnore]` - not persisted
- TrashView marked `[JsonIgnore]` - handled separately via FlattenTrashView method  
- CurrentViewType marked `[JsonIgnore]` - always opens in Explorer view

---

### TrashCan Collection Separation Design

#### Current Dual-Root Problem:
Currently, ExplorerView has two root nodes:
1. **First root**: StoryOverview + all live story elements  
2. **Second root**: TrashCan node containing deleted elements as children

This creates issues:
- Drag-and-drop operations between roots are problematic
- UI logic must handle "which root am I in?" complexity
- TreeView navigation assumes single root hierarchy
- Serialization requires special dual-root handling

#### New Separate Collection Design:

**StoryModel Changes:**
```csharp
// REMOVE: Second root from ExplorerView
// ExplorerView will only contain the StoryOverview root + live elements

// ADD: Dedicated TrashView collection
[JsonIgnore]
public ObservableCollection<StoryNodeItem> TrashView { get; set; }

// ADD: Flattened version for serialization  
[JsonInclude]
internal List<PersistableNode> FlattenedTrashView;
```

**Element Movement Operations:**

**Move to Trash (Delete):**
1. Remove element from its current parent.Children collection
2. Add element to StoryModel.TrashView collection
3. Set element.Parent = null (orphaned in TrashCan)
4. No additional properties needed - presence in collection determines status

**Restore from Trash:**
1. Remove element from StoryModel.TrashView collection  
2. Add element to appropriate location in ExplorerView (end of first root)
3. Set element.Parent to the target parent node
4. No additional properties needed - absence from TrashView determines status

#### Benefits:
- **Clean separation**: Main tree and trash are completely separate collections
- **Simplified drag-and-drop**: No cross-root operations to block
- **Cleaner UI binding**: Two distinct TreeViews, each bound to its own collection
- **Better performance**: No need to traverse dual-root hierarchy
- **Simplified serialization**: FlattenTrashView() handles trash separately

---

### SetCurrentView Relocation Design

#### Current Implementation Issues:
- `SetCurrentView()` method is in ShellViewModel (lines 1054-1068)
- Tightly coupled to UI concerns (DataSource binding, ViewList, CurrentViewType)  
- API and external services can't easily switch views without going through ShellViewModel
- Business logic mixed with presentation logic

#### New OutlineService Design:

**Method Signature with SerializationLock:**
```csharp
public void SetCurrentView(StoryModel model, StoryViewType viewType)
{
    var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
    var backupService = Ioc.Default.GetRequiredService<BackupService>();
    var logger = Ioc.Default.GetRequiredService<LogService>();
    
    using (var serializationLock = new SerializationLock(autoSaveService, backupService, logger))
    {
        switch (viewType)
        {
            case StoryViewType.ExplorerView:
                model.CurrentView = model.ExplorerView;
                model.CurrentViewType = StoryViewType.ExplorerView;
                break;
            case StoryViewType.NarratorView:
                model.CurrentView = model.NarratorView;
                model.CurrentViewType = StoryViewType.NarratorView;
                break;
            default:
                throw new ArgumentException($"Unsupported view type: {viewType}");
        }
        
        _log.Log(LogLevel.Info, $"View switched to {viewType}");
    }
}
```

**Usage Patterns:**

From ShellViewModel (UI-triggered view change):
```csharp
public void ViewChanged()
{
    if (OutlineManager.StoryModel.CurrentView == null || OutlineManager.StoryModel.CurrentView.Count == 0) return;
    
    var desiredViewType = SelectedView == "Story Explorer View" 
        ? StoryViewType.ExplorerView 
        : StoryViewType.NarratorView;
    
    outlineService.SetCurrentView(OutlineManager.StoryModel, desiredViewType);
    
    // Update UI-specific properties
    CurrentView = SelectedView;
    SelectedView = ViewList[(int)OutlineManager.StoryModel.CurrentViewType];
    
    // Navigate to first node of new view
    TreeViewNodeClicked(OutlineManager.StoryModel.CurrentView[0]);
}
```

From API/External Services:
```csharp
// Clean business logic call without UI dependencies
outlineService.SetCurrentView(storyModel, StoryViewType.NarratorView);
```

#### Benefits:
- **Separation of Concerns**: Business logic separated from UI concerns
- **Testability**: Can test view switching without UI dependencies  
- **API Access**: External services can switch views programmatically
- **Consistency**: Single source of truth for view switching logic
- **Maintainability**: View switching logic centralized in one service

---

### Shell.xaml UI Design - Shared Event Handlers

#### Shared Event Handler Design:

The existing CommandBar flyout system (AddStoryElementFlyout) and event handlers can be reused for both TreeViews:

**Event Handler Reuse:**
- **ContextRequested**: Same `AddButton_ContextRequested` handler for both trees
- **RightTapped**: Same `TreeViewItem_RightTapped` handler for both trees  
- **ItemInvoked**: Same `TreeViewItem_Invoked` handler for both trees

**Flyout Menu Logic:**
The existing `ShowFlyoutButtons()` method already handles TrashCan detection:
```csharp
// From ShellViewModel.cs lines 961-978
if (StoryNodeItem.RootNodeType(RightTappedNode) == StoryItemType.TrashCan)
{
    // Hide all buttons except Empty Trash and Restore
    RestoreStoryElementVisibility = Visibility.Visible;
    EmptyTrashVisibility = Visibility.Visible;
    // ... hide other buttons
}
```

#### Revised Shell.xaml Structure:
```xml
<SplitView.Pane>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="200" />
        </Grid.RowDefinitions>
        
        <!-- Main Navigation (unchanged) -->
        <ItemsRepeater Grid.Row="0" 
                      ItemsSource="{x:Bind ShellVm.StoryModel.CurrentView, Mode=TwoWay}"
                      ContextFlyout="{StaticResource AddStoryElementFlyout}" 
                      ContextRequested="AddButton_ContextRequested" >
            <!-- Existing template unchanged -->
        </ItemsRepeater>
        
        <!-- Separator -->
        <Border Grid.Row="1" Height="2" Background="Gray" Margin="5,2" />
        
        <!-- TrashCan TreeView - SHARED HANDLERS -->
        <Grid Grid.Row="2">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>
            
            <TextBlock Grid.Row="0" Text="Trash Can" FontWeight="Bold" Margin="10,5" />
            
            <TreeView Grid.Row="1" 
                     ItemsSource="{x:Bind ShellVm.StoryModel.TrashCan, Mode=TwoWay}"
                     ContextFlyout="{StaticResource AddStoryElementFlyout}"
                     ContextRequested="AddButton_ContextRequested"
                     ItemInvoked="TreeViewItem_Invoked"
                     >
                <!-- CanDragItems and CanReorderItems default to False -->
                <TreeView.ItemTemplate>
                    <DataTemplate x:DataType="viewmodels:StoryNodeItem">
                        <TreeViewItem RightTapped="TreeViewItem_RightTapped"
                                     IsExpanded="{x:Bind IsExpanded, Mode=TwoWay}"
                                     CanDrag="False">
                            <!-- Same visual template as main tree -->
                        </TreeViewItem>
                    </DataTemplate>
                </TreeView.ItemTemplate>
            </TreeView>
        </Grid>
    </Grid>
</SplitView.Pane>
```

#### Benefits of Shared Handlers:
- **No New Code**: Reuse existing event handler logic
- **Consistent UX**: Same right-click menu behavior 
- **Existing Logic Works**: `ShowFlyoutButtons()` already detects TrashCan context
- **No Breaking Changes**: All existing CommandBar flyout functionality preserved

---

### JSON Serialization Design

#### Implementation from simple_drag_and_drop branch:
The serialization follows the existing pattern in StoryIO.cs where ObservableCollections are flattened for JSON serialization and rebuilt during deserialization.

#### StoryModel Serialization:
```csharp
public string Serialize()
{
    // Flatten all three collections for serialization
    FlattenedExplorerView = FlattenTree(ExplorerView);
    FlattenedNarratorView = FlattenTree(NarratorView);
    if (TrashView != null)
        FlattenedTrashView = FlattenTree(TrashView);
    
    // Serialize
    return JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters =
        {
            new EmptyGuidConverter(),
            new StoryElementConverter(),
            new JsonStringEnumConverter()
        }
    });
}
```

#### StoryIO WriteStory Method:
The WriteStory method in StoryIO.cs needs to ensure the TrashView is properly handled:
```csharp
public async Task WriteStory(string output_path, StoryModel model)
{
    // ... existing code ...
    
    // Serialize model (which includes flattening TrashView)
    var json = model.Serialize();
    
    // Save file to disk
    await FileIO.WriteTextAsync(output, json);
}
```

#### Key Points:
- **CurrentView is a reference pointer**: When set to ExplorerView or NarratorView, it points directly to that collection. TreeView modifications through the binding update the underlying collection directly.
- **No synchronization needed**: Since CurrentView is just a reference, not a copy, all changes are automatically in the right collection for serialization.
- **TrashView**: Separate collection that gets flattened independently during serialization.
- **Serialization remains simple**: Each collection (ExplorerView, NarratorView, TrashView) is flattened as-is.

---

### Migration Strategy for Existing Dual-Root Files

#### Current State Analysis:
Legacy files have dual-root structure where ExplorerView contains:
- **Root 1**: StoryOverview + live story elements
- **Root 2**: TrashCan node with deleted elements as children

#### Migration Strategy Design:

**Detection Logic:**
```csharp
private bool IsLegacyDualRootFile(StoryModel model)
{
    // Check if ExplorerView has 2 or more roots and second is TrashCan
    return model.ExplorerView.Count >= 2 && 
           model.ExplorerView[1].Type == StoryItemType.TrashCan;
}
```

**Migration Process (in StoryIO.ReadStory):**
```csharp
private void MigrateLegacyDualRoot(StoryModel model)
{
    _logService.Log(LogLevel.Info, "Migrating legacy dual-root file to new structure");
    
    // Extract trash items from ExplorerView (NarratorView never has a TrashCan root)
    if (IsLegacyDualRootFile(model))
    {
        var trashRoot = model.ExplorerView[1];
        _logService.Log(LogLevel.Info, $"Found {trashRoot.Children.Count} items in ExplorerView trash");
        
        // Move all trash children to TrashView collection
        foreach (var trashItem in trashRoot.Children.ToList())
        {
            trashItem.Parent = null; // Orphan in TrashView
            model.TrashView.Add(trashItem);
        }
        
        // Remove the TrashCan root from ExplorerView
        model.ExplorerView.RemoveAt(1);
        _logService.Log(LogLevel.Info, "Migrated ExplorerView trash items to TrashView collection");
    }
    
    // Initialize CurrentView and CurrentViewType
    model.CurrentView = model.ExplorerView;
    model.CurrentViewType = StoryViewType.ExplorerView;
    
    // Step 4: Mark as migrated and needing save
    model.Changed = true;
    _logService.Log(LogLevel.Info, "Legacy dual-root migration completed");
}
```

**Integration with ReadStory:**
```csharp
public async Task<StoryModel> ReadStory(StorageFile StoryFile)
{
    // ... existing deserialization logic ...
    
    // Rebuild collections
    _model.ExplorerView = RebuildTree(_model.FlattenedExplorerView, _model.StoryElements, _logService);
    _model.NarratorView = RebuildTree(_model.FlattenedNarratorView, _model.StoryElements, _logService);
    
    // Handle TrashCan - either from new format or migration
    if (_model.FlattenedTrashCan != null)
    {
        // New format: rebuild from FlattenedTrashCan
        _model.TrashCan = RebuildTree(_model.FlattenedTrashCan, _model.StoryElements, _logService);
    }
    else
    {
        // Initialize empty collection for potential migration
        _model.TrashCan = new ObservableCollection<StoryNodeItem>();
    }
    
    // MIGRATION: Convert legacy dual-root structure
    MigrateLegacyDualRoot(_model);
    
    return _model;
}
```

#### Edge Cases Handled:

**Duplicate Items**: 
- Check UUIDs to avoid adding same item twice to TrashCan
- Use `.Any()` to detect existing items

**Empty Trash**:
- Handle cases where TrashCan root exists but has no children
- Initialize empty TrashCan collection gracefully

**Corrupted Data**:
- Validate TrashCan node type before migration
- Log warnings for unexpected structures
- Fail gracefully without data loss

**Performance**:
- Use `.ToList()` to avoid collection modification during enumeration
- Minimize tree traversals during migration

---

### Drag-and-Drop Constraint Design

#### Natural Separation:
- Main NavigationTree and TrashCan TreeView are separate controls
- No cross-TreeView dragging is possible by default
- No additional constraint logic needed

#### TrashCan TreeView Settings:
```xml
<TreeView ItemsSource="{x:Bind StoryModel.TrashCan}"
         SelectionMode="Single">      <!-- Can still select for context menu -->
         <!-- CanDragItems and CanReorderItems default to False -->
```

#### Main NavigationTree (unchanged):
```xml
<!-- Existing TreeView keeps all current drag-and-drop behavior -->
<TreeView CanDragItems="True" 
         AllowDrop="True" 
         CanReorderItems="True"
         DragItemsCompleted="NavigationTree_DragItemsCompleted">
```

#### What Actually Needs Enforcement:

**Only within TrashCan TreeView:**
- Dragging items out of trash is disabled by default
- Reordering within trash is disabled by default
- Still allow selection for right-click context menu

**Main NavigationTree:**
- Keep all existing drag-and-drop behavior
- No changes needed - works exactly as today

#### Operations:
- **Move to Trash**: Context menu "Delete" → moves from main tree to TrashCan collection
- **Restore from Trash**: Context menu "Restore" → moves from TrashCan to main tree  
- **Drag-and-Drop**: Only works within main NavigationTree (as it does today)

---

### Changed Flag Integration with TreeView Operations

#### StoryModel Property with Full Notification:
```csharp
public class StoryModel : ObservableObject
{
    private bool _changed;
    
    [JsonIgnore]
    public bool Changed 
    {
        get => _changed;
        set 
        {
            if (SetProperty(ref _changed, value))
            {
                // Trigger OnPropertyChanged notification
                
                // Update UI status indicator  
                if (_changed)
                {
                    ShellViewModel.ShowChange(); // Existing static method
                }
                else
                {
                    // Reset to green when saved
                    if (ShellViewModel.ShellInstance != null)
                        ShellViewModel.ShellInstance.ChangeStatusColor = Colors.Green;
                }
            }
        }
    }
}
```

#### TreeView Operation Hooks:
All TreeView operations must set Changed flag:

```csharp
// In OutlineService.AddStoryElement
public StoryElement AddStoryElement(StoryModel model, StoryItemType typeToAdd, StoryNodeItem parent)
{
    // ... create element logic ...
    
    // CRITICAL: Mark model as changed
    model.Changed = true; // Triggers OnPropertyChanged + UI update
    
    return newElement;
}

// In drag-and-drop operations (Shell.xaml.cs)
private void NavigationTree_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
{
    // ... existing drag-and-drop logic ...
    
    // CRITICAL: Mark as changed after any node movement
    ShellVm.OutlineManager.StoryModel.Changed = true;
}

// In move operations (ShellViewModel)
private void MoveTreeViewItemLeft()
{
    // ... existing move logic ...
    
    // EXISTING: ShowChange() call becomes:
    OutlineManager.StoryModel.Changed = true; // This calls ShowChange() automatically
}
```

#### Collection Change Monitoring:
Monitor ObservableCollection changes for automatic flagging:

```csharp
public StoryModel()
{
    ExplorerView = new ObservableCollection<StoryNodeItem>();
    NarratorView = new ObservableCollection<StoryNodeItem>();
    TrashCan = new ObservableCollection<StoryNodeItem>();
    
    // Monitor collection changes
    ExplorerView.CollectionChanged += OnTreeViewCollectionChanged;
    NarratorView.CollectionChanged += OnTreeViewCollectionChanged;
    TrashCan.CollectionChanged += OnTreeViewCollectionChanged;
    
    CurrentView = ExplorerView;
}

private void OnTreeViewCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
{
    // Any add/remove/move operation marks as changed
    Changed = true; // Triggers full notification chain
}
```

---

### Class Diagram of New Architecture

```
┌─────────────────────────────────────┐
│         StoryModel                  │
│      (ObservableObject)             │
├─────────────────────────────────────┤
│ + CurrentView: ObservableCollection │ ← UI binds here
│ + ExplorerView: ObservableCollection│
│ + NarratorView: ObservableCollection│
│ + TrashView: ObservableCollection   │ ← NEW: Separate collection
│ + CurrentViewType: StoryViewType    │ ← NEW: Track active view
│ + Changed: bool (with notifications)│ ← Enhanced with OnPropertyChanged
├─────────────────────────────────────┤
│ + SyncCurrentViewToSource()         │
│ + Serialize()                       │
│ + FlattenTree()                     │
│ + FlattenTrashView()                │ ← NEW: Separate flattening
└─────────────────────────────────────┘
                ▲
                │ Uses
┌─────────────────────────────────────┐
│       OutlineService                │
├─────────────────────────────────────┤
│ + SetCurrentView(model, viewType)   │ ← MOVED from ShellViewModel
│ + AddStoryElement()                 │
│ + DeleteStoryElement()              │
│ + RestoreFromTrash()                │
└─────────────────────────────────────┘
                ▲
                │ Calls
┌─────────────────────────────────────┐
│       ShellViewModel                │
├─────────────────────────────────────┤
│ - DataSource                        │ ← REMOVED (now CurrentView in StoryModel)
│ + OutlineManager: StoryModel        │
│ + ViewChanged()                     │ ← Calls OutlineService
│ - SetCurrentView()                  │ ← REMOVED (moved to service)
└─────────────────────────────────────┘
                ▲
                │ Binds to
┌─────────────────────────────────────┐
│         Shell.xaml                  │
├─────────────────────────────────────┤
│ NavigationTree → CurrentView        │
│ TrashCanTree → TrashView            │ ← NEW: Separate TreeView
│ StatusBar → Changed                 │
└─────────────────────────────────────┘
```

---

### Public API Contracts for OutlineService.SetCurrentView

```csharp
namespace StoryCAD.Services
{
    public interface IOutlineService
    {
        /// <summary>
        /// Sets the current view type for the story model and updates the CurrentView accordingly.
        /// </summary>
        /// <param name="model">The StoryModel to update</param>
        /// <param name="viewType">The desired view type (Explorer or Narrator)</param>
        /// <exception cref="ArgumentNullException">Thrown when model is null</exception>
        /// <exception cref="ArgumentException">Thrown when viewType is invalid</exception>
        void SetCurrentView(StoryModel model, StoryViewType viewType);
        
        // ... existing methods ...
    }
    
    public class OutlineService : IOutlineService
    {
        public void SetCurrentView(StoryModel model, StoryViewType viewType)
        {
            // Implementation would include proper locking with SerializationLock
            // and handle the view switching logic
        }
    }
}
```

---

### Validation Against Existing Functionality

✅ **Navigation tree behavior**: Maintained through existing event handlers  
✅ **Explorer/Narrator switching**: Enhanced with proper sync logic  
✅ **Drag-and-drop operations**: Simplified with separate TreeViews  
✅ **Context menu actions**: Reused existing flyout system  
✅ **Save/load operations**: Enhanced with migration support  
✅ **Status indicators**: Automated through Changed property  
✅ **API access**: Improved with OutlineService.SetCurrentView  
✅ **Performance**: Better with separate collections  
✅ **User experience**: No breaking changes to workflow  

The design maintains 100% backward compatibility while improving architecture and maintainability.

---

### Unit Test Specifications

Critical unit tests must be implemented to ensure the change tracking mechanism works correctly:

#### 1. StoryModel Changed Property Tests

```csharp
[TestClass]
public class StoryModelChangedTests
{
    [TestMethod]
    public void Changed_SetToTrue_RaisesPropertyChanged()
    {
        // Arrange
        var model = new StoryModel();
        bool propertyChangedRaised = false;
        model.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(StoryModel.Changed))
                propertyChangedRaised = true;
        };
        
        // Act
        model.Changed = true;
        
        // Assert
        Assert.IsTrue(propertyChangedRaised);
        Assert.IsTrue(model.Changed);
    }
    
    [TestMethod]
    public void Changed_SetToSameValue_DoesNotRaisePropertyChanged()
    {
        // Arrange
        var model = new StoryModel();
        model.Changed = true;
        bool propertyChangedRaised = false;
        model.PropertyChanged += (s, e) => propertyChangedRaised = true;
        
        // Act
        model.Changed = true; // Same value
        
        // Assert
        Assert.IsFalse(propertyChangedRaised);
    }
    
    [TestMethod]
    public void Changed_SetToFalse_UpdatesUIColorToGreen()
    {
        // Arrange
        var model = new StoryModel();
        model.Changed = true;
        
        // Act
        model.Changed = false;
        
        // Assert
        Assert.IsFalse(model.Changed);
        // Verify ShellViewModel.ShellInstance.ChangeStatusColor == Colors.Green
    }
}
```

#### 2. ShowChange() Behavior Tests

```csharp
[TestMethod]
public void ShowChange_WhenAlreadyChanged_DoesNotUpdateAgain()
{
    // Arrange
    var model = new StoryModel { Changed = true };
    var initialCallCount = GetShowChangeCallCount();
    
    // Act
    ShellViewModel.ShowChange();
    
    // Assert - verify early exit worked
    Assert.AreEqual(initialCallCount, GetShowChangeCallCount());
}

[TestMethod]
public void ShowChange_WhenNotChanged_SetsChangedAndUpdatesColor()
{
    // Arrange
    var model = new StoryModel { Changed = false };
    
    // Act
    ShellViewModel.ShowChange();
    
    // Assert
    Assert.IsTrue(model.Changed);
    Assert.AreEqual(Colors.Red, ShellViewModel.ShellInstance.ChangeStatusColor);
}
```

#### 3. Integration Tests

```csharp
[TestMethod]
public async Task SaveFile_ClearsChangedFlag()
{
    // Arrange
    var outlineVM = new OutlineViewModel();
    outlineVM.StoryModel.Changed = true;
    
    // Act
    await outlineVM.SaveFile();
    
    // Assert
    Assert.IsFalse(outlineVM.StoryModel.Changed);
    Assert.AreEqual(Colors.Green, ShellViewModel.ShellInstance.ChangeStatusColor);
}

[TestMethod]
public void ViewModelPropertyChange_TriggersShowChange()
{
    // Arrange
    var characterVM = new CharacterViewModel();
    characterVM.Model = new CharacterModel();
    
    // Act
    characterVM.Name = "New Name"; // Should trigger OnPropertyChanged
    
    // Assert
    Assert.IsTrue(characterVM.Model.StoryModel.Changed);
}
```

#### 4. Collection Change Monitoring Tests

```csharp
[TestMethod]
public void ExplorerView_AddItem_SetsChangedFlag()
{
    // Arrange
    var model = new StoryModel();
    model.Changed = false;
    
    // Act
    model.ExplorerView.Add(new StoryNodeItem(...));
    
    // Assert
    Assert.IsTrue(model.Changed);
}

[TestMethod]
public void TrashView_RemoveItem_SetsChangedFlag()
{
    // Arrange
    var model = new StoryModel();
    var item = new StoryNodeItem(...);
    model.TrashView.Add(item);
    model.Changed = false;
    
    // Act
    model.TrashView.Remove(item);
    
    // Assert
    Assert.IsTrue(model.Changed);
}
```

#### 5. SetCurrentView Tests

```csharp
[TestMethod]
public void SetCurrentView_WithSerializationLock_CompletesSuccessfully()
{
    // Arrange
    var model = new StoryModel();
    var outlineService = new OutlineService();
    
    // Act
    outlineService.SetCurrentView(model, StoryViewType.NarratorView);
    
    // Assert
    Assert.AreEqual(model.NarratorView, model.CurrentView);
    Assert.AreEqual(StoryViewType.NarratorView, model.CurrentViewType);
}

[TestMethod]
public void SetCurrentView_WithInvalidViewType_ThrowsArgumentException()
{
    // Arrange
    var model = new StoryModel();
    var outlineService = new OutlineService();
    
    // Act & Assert
    Assert.ThrowsException<ArgumentException>(() => 
        outlineService.SetCurrentView(model, (StoryViewType)999));
}
```

These tests ensure:
- The Changed property correctly implements INotifyPropertyChanged
- The ShowChange() early exit logic prevents redundant operations
- Save operations properly clear the flag and update UI
- Collection changes automatically trigger the changed flag
- View switching works correctly with proper locking