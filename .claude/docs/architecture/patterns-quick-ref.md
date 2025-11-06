# StoryCAD Architectural Patterns - Quick Reference

This document provides a concise overview of all architectural patterns used in StoryCAD. For detailed architecture information, see `/devdocs/StoryCAD_architecture_notes.md`.

---

## 1. MVVM Pattern (Model-View-ViewModel)

**Purpose**: Strict separation between UI and business logic

**When to Use**: All ViewModels and UI components

**Key Components**:
- **Models**: Data structures (StoryModel, StoryElement, etc.)
- **Views**: XAML pages and controls
- **ViewModels**: Presentation logic with data binding

**Implementation**:
```csharp
[ObservableObject]
public partial class CharacterViewModel : ISaveable
{
    [ObservableProperty]
    private string _name;

    [RelayCommand]
    private async Task SaveCharacter()
    {
        // Save logic
    }

    public void SaveModel()
    {
        _model.Name = Name;
    }
}
```

**See Also**: [Full MVVM details](/devdocs/StoryCAD_architecture_notes.md#1-mvvm-model-view-viewmodel)

---

## 2. Dependency Injection Pattern

**Purpose**: Centralized service registration and resolution

**When to Use**: All services and ViewModels

**Registration** (BootStrapper.cs):
```csharp
public static void Initialise(bool forUI = true)
{
    Ioc.Default.ConfigureServices(
        new ServiceCollection()
            .AddSingleton<AppState>()
            .AddSingleton<OutlineService>()
            .AddSingleton<NavigationService>()
            // ... other services
            .BuildServiceProvider()
    );
}
```

**Consumption** (Constructor Injection):
```csharp
public class CharacterViewModel
{
    private readonly OutlineService _outlineService;

    public CharacterViewModel()
    {
        _outlineService = Ioc.Default.GetService<OutlineService>();
    }
}
```

**Note**: ViewModels use `Ioc.Default.GetService<T>()` directly. Services use true constructor injection.

**See Also**: [DI details](/devdocs/StoryCAD_architecture_notes.md#2-dependency-injection)

---

## 3. SerializationLock Pattern (Thread Safety)

**Purpose**: Ensures serial reusability - one operation completes before another starts

**When to Use**: Wrap ALL state-modifying operations on StoryModel

**Implementation**:
```csharp
using (var serializationLock = new SerializationLock(autoSaveService, backupService, _logger))
{
    // Thread-safe operation
    // Auto-save and backup are paused during this block
    model.StoryElements.Add(newElement);
}
```

**Used In**:
- `OutlineService` - All state-modifying operations
- `SemanticKernelAPI` - All API operations
- `StoryIO` - File read/write operations

**Prevents**:
- Concurrent modifications to story data
- File corruption from simultaneous writes
- Race conditions between auto-save and manual operations

**See Also**: [SerializationLock details](/devdocs/StoryCAD_architecture_notes.md#3-thread-safety-pattern-serializationlock)

---

## 4. ISaveable Pattern

**Purpose**: Flush ViewModel changes to Model without type-specific knowledge

**When to Use**: Implement in ALL element ViewModels (CharacterViewModel, PlotViewModel, etc.)

**Interface**:
```csharp
public interface ISaveable
{
    void SaveModel(); // Flush ViewModel data to Model
}
```

**Implementation**:
```csharp
public class CharacterViewModel : ISaveable
{
    private CharacterModel _model;

    public void SaveModel()
    {
        // Transfer ViewModel properties to Model
        _model.Name = Name;
        _model.Age = Age;
        _model.Role = Role;
    }
}
```

**Registration** (in Pages):
```csharp
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    _appState.CurrentSaveable = ViewModel; // Register
}

protected override void OnNavigatedFrom(NavigationEventArgs e)
{
    _appState.CurrentSaveable = null; // Unregister
    base.OnNavigatedFrom(e);
}
```

**Benefits**:
- No ViewModel dependencies in services
- No type-specific switch statements
- ViewModels self-register in pages
- Clean separation of concerns

**See Also**: [ISaveable details](/devdocs/StoryCAD_architecture_notes.md#4-isaveable-pattern-issue-1100)

---

## 5. OperationResult Pattern

**Purpose**: Safe error handling for API operations

**When to Use**: All SemanticKernelAPI methods

**Implementation**:
```csharp
public class OperationResult<T>
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public T Payload { get; set; }
}
```

**Usage**:
```csharp
public OperationResult<Guid> AddElement(string parentGuid, StoryItemType type, string name)
{
    try
    {
        using (var serializationLock = new SerializationLock(...))
        {
            var newGuid = _outlineService.AddStoryElement(model, parentGuid, type, name);
            return new OperationResult<Guid>
            {
                IsSuccess = true,
                Payload = newGuid
            };
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "AddElement failed");
        return new OperationResult<Guid>
        {
            IsSuccess = false,
            ErrorMessage = ex.Message
        };
    }
}
```

**Why Use OperationResult?**
- External callers (LLMs, tools) need predictable interface
- No exceptions leak to external code
- Structured error information
- Clear success/failure indication

**See Also**: [OperationResult details](/devdocs/StoryCAD_architecture_notes.md#5-operationresult-pattern)

---

## 6. StoryDocument Pattern

**Purpose**: Encapsulate StoryModel and FilePath together atomically

**When to Use**: Always access model and path together via `AppState.CurrentDocument`

**Implementation**:
```csharp
public class StoryDocument
{
    public StoryModel StoryModel { get; set; }
    public string FilePath { get; set; }
}
```

**Usage**:
```csharp
// Correct - atomic access:
var document = AppState.CurrentDocument;
var model = document.StoryModel;
var path = document.FilePath;

// Wrong - separate access risks mismatch:
var model = SomeSource.StoryModel;
var path = SomeOtherSource.FilePath; // Could be out of sync!
```

**Benefits**:
- Prevents mismatched model/path states
- Atomic document operations
- Simplifies service interfaces
- Single source of truth

**See Also**: [StoryDocument details](/devdocs/StoryCAD_architecture_notes.md#6-storydocument-pattern-issue-1100)

---

## 7. Stateless Services Pattern

**Purpose**: Services don't maintain state between calls

**When to Use**: ALL service classes (OutlineService, NavigationService, etc.)

**Implementation**:
```csharp
public class OutlineService
{
    // NO stored StoryModel field!

    public Guid AddStoryElement(StoryModel model, Guid parentGuid, StoryItemType type, string name)
    {
        // Receives model as parameter
        var parent = model.StoryElements.StoryElementGuids[parentGuid];
        var newElement = new StoryElement { ... };
        model.StoryElements.Add(newElement);
        return newElement.Uuid;
    }
}
```

**Benefits**:
- Easier to test (no hidden state)
- Thread-safe by design (no shared state)
- Clearer dependencies (explicit parameters)
- Prevents stale state bugs

**See Also**: [Stateless Services details](/devdocs/StoryCAD_architecture_notes.md#7-stateless-services-pattern)

---

## 8. IMessenger Pattern (Loosely-Coupled Communication)

**Purpose**: Enable ViewModels and components to communicate without direct dependencies

**When to Use**: Cross-component notifications (status updates, name changes, document state)

**Core Implementation**:
```csharp
// Registering message handlers (usually in constructor):
Messenger.Register<StatusChangedMessage>(this,
    (_, m) => StatusMessageReceived(m));

// Sending messages (from any ViewModel or service):
Messenger.Send(new StatusChangedMessage(
    new StatusMessage("File saved", LogLevel.Info, true)));

// Request/Response pattern:
var isChanged = Messenger.Send(new IsChangedRequestMessage());
```

**Common Message Types**:

| Message Type | Purpose | Usage |
|-------------|---------|-------|
| `StatusChangedMessage` | Update status bar | User feedback, operation status |
| `IsChangedMessage` | Notify unsaved changes | Document dirty flag updates |
| `IsChangedRequestMessage` | Query changed state | Request current document state |
| `NameChangedMessage` | Sync element name | TreeView ↔ element name updates |
| `IsBackupStatusMessage` | Update backup indicator | Backup service status |

**Example - Sending Status Update**:
```csharp
Messenger.Send(new StatusChangedMessage(
    new StatusMessage("Character saved successfully", LogLevel.Info, true)));
```

**Example - Registering Handler (ShellViewModel)**:
```csharp
public ShellViewModel(...)
{
    Messenger.Register<StatusChangedMessage>(this,
        (_, m) => StatusMessageReceived(m));
}

private void StatusMessageReceived(StatusChangedMessage statusMessage)
{
    StatusMessage = statusMessage.Value.Status;
    // Update UI based on log level
}
```

**Benefits**:
- No direct ViewModel-to-ViewModel dependencies
- Publish-subscribe decoupling
- Easy to test (can verify messages sent)
- Prevents circular dependencies

**See Also**: [IMessenger details](/devdocs/StoryCAD_architecture_notes.md#8-mvvm-messenger-pattern)

---

## 9. Platform-Specific Code Pattern

**Purpose**: Handle Windows vs macOS platform differences

**When to Use**: Code that depends on platform-specific APIs

**Approaches**:

### Partial Classes (Preferred)
```csharp
// Windowing.cs (shared)
public partial class Windowing
{
    public partial void InitializePlatform();
}

// Windowing.WinAppSDK.cs (Windows)
#if HAS_UNO_WINUI
public partial class Windowing
{
    public partial void InitializePlatform()
    {
        // Windows-specific code
    }
}
#endif

// Windowing.desktop.cs (macOS)
#if __MACOS__
public partial class Windowing
{
    public partial void InitializePlatform()
    {
        // macOS-specific code
    }
}
#endif
```

### Conditional Compilation (For smaller differences)
```csharp
public void HandleKeyboardShortcut(KeyboardAccelerator accelerator)
{
#if HAS_UNO_WINUI
    // Ctrl+S on Windows
    if (accelerator.Modifiers == VirtualKeyModifiers.Control &&
        accelerator.Key == VirtualKey.S)
#elif __MACOS__
    // Cmd+S on macOS
    if (accelerator.Modifiers == VirtualKeyModifiers.Menu &&
        accelerator.Key == VirtualKey.S)
#endif
    {
        SaveFile();
    }
}
```

**Platform Symbols**:
- `HAS_UNO_WINUI` - Windows (WinAppSDK)
- `__MACOS__` - macOS
- `__IOS__` - iOS (future)
- `__ANDROID__` - Android (future)
- `__WASM__` - WebAssembly (future)

**See Also**: [Platform-specific code](/devdocs/StoryCAD_architecture_notes.md#platform-specific-code-patterns)

---

## Common Mistakes to Avoid

### 1. Forgetting SerializationLock
**Wrong**:
```csharp
public void AddCharacter(StoryModel model, string name)
{
    model.Characters.Add(new Character { Name = name }); // No lock!
}
```

**Right**:
```csharp
public void AddCharacter(StoryModel model, string name)
{
    using (var lock = new SerializationLock(_autoSave, _backup, _logger))
    {
        model.Characters.Add(new Character { Name = name });
    }
}
```

### 2. Not Implementing ISaveable
**Wrong**:
```csharp
public class CharacterViewModel : ObservableObject
{
    // Missing ISaveable interface
}
```

**Right**:
```csharp
public class CharacterViewModel : ObservableObject, ISaveable
{
    public void SaveModel()
    {
        _model.Name = Name;
        // ... flush all properties
    }
}
```

### 3. Storing State in Services
**Wrong**:
```csharp
public class OutlineService
{
    private StoryModel _currentModel; // State in service!

    public void AddElement(Guid parentGuid, ...)
    {
        _currentModel.Elements.Add(...); // Using stored state
    }
}
```

**Right**:
```csharp
public class OutlineService
{
    // No state stored!

    public void AddElement(StoryModel model, Guid parentGuid, ...)
    {
        model.Elements.Add(...); // Model passed as parameter
    }
}
```

### 4. Not Registering ISaveable in Pages
**Wrong**:
```csharp
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    // Forgot to register ViewModel!
}
```

**Right**:
```csharp
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    _appState.CurrentSaveable = ViewModel;
}

protected override void OnNavigatedFrom(NavigationEventArgs e)
{
    _appState.CurrentSaveable = null;
    base.OnNavigatedFrom(e);
}
```

---

## Pattern Decision Tree

**Adding a new ViewModel?**
1. Inherit from `ObservableObject`
2. Implement `ISaveable` interface
3. Use `[ObservableProperty]` for bindable properties
4. Use `[RelayCommand]` for commands
5. Register in `BootStrapper.cs`
6. Register/unregister `CurrentSaveable` in page navigation

**Adding a new Service?**
1. Keep it stateless (no fields for models/documents)
2. Accept models as parameters
3. Wrap state changes in `SerializationLock`
4. Register as singleton in `BootStrapper.cs`
5. Inject dependencies via constructor

**Working with StoryModel?**
1. Always access via `AppState.CurrentDocument.StoryModel`
2. Wrap modifications in `SerializationLock`
3. Flush ViewModels via `ISaveable.SaveModel()` before persisting
4. Use `OperationResult<T>` for API methods

**Cross-component Communication?**
1. Use `IMessenger` for notifications
2. Define message type (inherit from `ValueChangedMessage<T>`)
3. Register handler in recipient
4. Send from anywhere

---

## Quick Reference Links

- **Full Architecture**: `/devdocs/StoryCAD_architecture_notes.md`
- **Testing Guide**: `/devdocs/testing.md`
- **Coding Standards**: `/devdocs/coding.md`
- **UNO Platform Guide**: `.claude/docs/dependencies/uno-platform-guide.md`
- **MVVM Toolkit Guide**: `.claude/docs/dependencies/mvvm-toolkit-guide.md`
- **ViewModel Template**: `.claude/docs/examples/new-viewmodel-template.md`
- **Service Template**: `.claude/docs/examples/new-service-template.md`
