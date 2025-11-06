# MVVM Toolkit Guide for StoryCAD

## Package Information

- **Package**: CommunityToolkit.Mvvm
- **Version**: 8.4.0
- **Purpose**: Modern MVVM framework with source generators, IMessenger, and Ioc container
- **Documentation**: Use Context7 MCP for latest docs: `@context7 CommunityToolkit.Mvvm` or visit [https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)

---

## What StoryCAD Uses From MVVM Toolkit

StoryCAD uses four primary features:

1. **ObservableObject** - Base class for ViewModels with INotifyPropertyChanged
2. **Source Generators** - `[ObservableProperty]` and `[RelayCommand]` attributes
3. **IMessenger** - Loosely-coupled messaging between components
4. **Ioc.Default** - Dependency injection container

---

## 1. ObservableObject and Source Generators

### Basic ViewModel Pattern

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

[ObservableObject]
public partial class CharacterViewModel : ISaveable
{
    // Source generator creates Name property with INotifyPropertyChanged
    [ObservableProperty]
    private string _name;

    // Source generator creates Age property
    [ObservableProperty]
    private int _age;

    // Source generator creates SaveCharacterCommand
    [RelayCommand]
    private async Task SaveCharacter()
    {
        // Save logic here
    }

    // ISaveable implementation (not generated)
    public void SaveModel()
    {
        _model.Name = Name;
        _model.Age = Age;
    }
}
```

### What the Source Generator Creates

For `[ObservableProperty] private string _name;`, the generator creates:

```csharp
public string Name
{
    get => _name;
    set
    {
        if (!EqualityComparer<string>.Default.Equals(_name, value))
        {
            OnNameChanging(value);
            OnPropertyChanging(nameof(Name));
            _name = value;
            OnNameChanged(value);
            OnPropertyChanged(nameof(Name));
        }
    }
}
```

For `[RelayCommand] private async Task SaveCharacter()`, the generator creates:

```csharp
public IAsyncRelayCommand SaveCharacterCommand { get; }

private void InitializeCommands()
{
    SaveCharacterCommand = new AsyncRelayCommand(SaveCharacter);
}
```

### Property Change Notifications

**Automatic notifications**:
```csharp
[ObservableProperty]
private string _name;

// Setting Name automatically raises PropertyChanged
Name = "New Value"; // UI updates automatically
```

**Manual notifications** (when needed):
```csharp
public void UpdateComputedProperty()
{
    // Manually notify for properties without backing fields
    OnPropertyChanged(nameof(FullName));
}

public string FullName => $"{FirstName} {LastName}";
```

### Property Change Callbacks

```csharp
[ObservableProperty]
private string _name;

// Generated method you can implement
partial void OnNameChanged(string value)
{
    // Called after Name is set
    if (!string.IsNullOrEmpty(value))
    {
        Messenger.Send(new NameChangedMessage(new NameChangeMessage(oldName, value)));
    }
}

// Also available: OnNameChanging(string value) - called before
```

### Command Patterns

**Simple synchronous command**:
```csharp
[RelayCommand]
private void DeleteCharacter()
{
    // Synchronous operation
    _characters.Remove(CurrentCharacter);
}
```

**Async command**:
```csharp
[RelayCommand]
private async Task SaveCharacterAsync()
{
    await _outlineService.SaveStoryAsync(model);
}
```

**Command with CanExecute**:
```csharp
[RelayCommand(CanExecute = nameof(CanSaveCharacter))]
private async Task SaveCharacter()
{
    // Save logic
}

private bool CanSaveCharacter()
{
    return !string.IsNullOrEmpty(Name) && Age > 0;
}

// Notify when CanExecute changes
private void UpdateValidation()
{
    SaveCharacterCommand.NotifyCanExecuteChanged();
}
```

**Command with parameter**:
```csharp
[RelayCommand]
private void SelectTab(string tabName)
{
    CurrentTab = tabName;
}

// XAML binding:
// <Button Command="{x:Bind ViewModel.SelectTabCommand}"
//         CommandParameter="Overview" />
```

---

## 2. IMessenger (Loosely-Coupled Communication)

### Core Concepts

IMessenger uses a publish-subscribe pattern:
- **Register**: Components subscribe to message types
- **Send**: Components publish messages
- **Request**: Components request information (request/response pattern)

### StoryCAD's Message Types

| Message Type | Purpose | Usage |
|-------------|---------|-------|
| `StatusChangedMessage` | Status bar updates | User feedback, operation status |
| `IsChangedMessage` | Document dirty flag | Unsaved changes notification |
| `IsChangedRequestMessage` | Query dirty flag | Request current document state |
| `NameChangedMessage` | Element name sync | TreeView ↔ element name updates |
| `IsBackupStatusMessage` | Backup indicator | Backup service status |

### Registering Message Handlers

**In constructor or initialization**:
```csharp
using CommunityToolkit.Mvvm.Messaging;

public class ShellViewModel : ObservableRecipient
{
    public ShellViewModel()
    {
        // Register for StatusChangedMessage
        Messenger.Register<StatusChangedMessage>(this,
            (recipient, message) => StatusMessageReceived(message));

        // Alternative syntax with static method
        Messenger.Register<ShellViewModel, IsChangedMessage>(this,
            static (r, m) => r.IsChangedMessageReceived(m));
    }

    private void StatusMessageReceived(StatusChangedMessage message)
    {
        StatusText = message.Value.Status;
        StatusLevel = message.Value.Level;
    }

    private void IsChangedMessageReceived(IsChangedMessage message)
    {
        IsDocumentDirty = message.Value;
    }
}
```

### Sending Messages

**Value message** (one-way notification):
```csharp
// From any ViewModel or service
Messenger.Send(new StatusChangedMessage(
    new StatusMessage("File saved successfully", LogLevel.Info, true)));

Messenger.Send(new IsChangedMessage(true));
```

**Request message** (request/response pattern):
```csharp
// Define request message
public class IsChangedRequestMessage : RequestMessage<bool> { }

// Register handler (in ShellViewModel)
Messenger.Register<IsChangedRequestMessage>(this,
    (_, m) => m.Reply(State.CurrentDocument?.Model?.Changed ?? false));

// Send request (from any component)
var isChanged = Messenger.Send(new IsChangedRequestMessage());
```

### Creating Custom Messages

**Value message**:
```csharp
using CommunityToolkit.Mvvm.Messaging.Messages;

public class NameChangedMessage : ValueChangedMessage<NameChangeMessage>
{
    public NameChangedMessage(NameChangeMessage value) : base(value) { }
}

public record NameChangeMessage(string OldName, string NewName);
```

**Request message**:
```csharp
public class CurrentDocumentRequestMessage : RequestMessage<StoryDocument> { }
```

### Unregistering Messages

**Unregister specific message**:
```csharp
Messenger.Unregister<StatusChangedMessage>(this);
```

**Unregister all messages**:
```csharp
Messenger.UnregisterAll(this);
```

**Automatic unregistration** (using ObservableRecipient):
```csharp
public class MyViewModel : ObservableRecipient
{
    // Set IsActive to false to auto-unregister
    public void Cleanup()
    {
        IsActive = false; // Unregisters all messages
    }
}
```

### ObservableRecipient Pattern

`ObservableRecipient` extends `ObservableObject` with automatic message management:

```csharp
public class ShellViewModel : ObservableRecipient
{
    public ShellViewModel()
    {
        // Register messages
        Messenger.Register<StatusChangedMessage>(this, OnStatusChanged);

        // Activate (enables messages)
        IsActive = true;
    }

    private void OnStatusChanged(StatusChangedMessage message)
    {
        // Handle message
    }

    // When IsActive = false, all messages are automatically unregistered
}
```

---

## 3. Dependency Injection (Ioc.Default)

### Service Registration

**In BootStrapper.cs**:
```csharp
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

public static class BootStrapper
{
    public static void Initialise(bool forUI = true)
    {
        Ioc.Default.ConfigureServices(
            new ServiceCollection()
                // Core services
                .AddSingleton<AppState>()
                .AddSingleton<OutlineService>()
                .AddSingleton<NavigationService>()
                .AddSingleton<AutoSaveService>()
                .AddSingleton<BackupService>()
                .AddSingleton<LogService>()

                // ViewModels (if needed)
                .AddTransient<CharacterViewModel>()
                .AddTransient<PlotViewModel>()

                .BuildServiceProvider()
        );
    }
}
```

### Service Resolution

**In ViewModels** (StoryCAD pattern):
```csharp
public class CharacterViewModel : ObservableObject
{
    private readonly OutlineService _outlineService;
    private readonly LogService _logger;

    public CharacterViewModel()
    {
        // Get services from Ioc container
        _outlineService = Ioc.Default.GetService<OutlineService>();
        _logger = Ioc.Default.GetService<LogService>();
    }
}
```

**In Services** (true constructor injection):
```csharp
public class OutlineService
{
    private readonly LogService _logger;
    private readonly AutoSaveService _autoSave;

    public OutlineService(LogService logger, AutoSaveService autoSave)
    {
        _logger = logger;
        _autoSave = autoSave;
    }
}
```

**Required vs Optional**:
```csharp
// Throws if service not found
var service = Ioc.Default.GetRequiredService<OutlineService>();

// Returns null if not found
var service = Ioc.Default.GetService<OptionalService>();
```

---

## Common Usage Patterns in StoryCAD

### Pattern 1: Element ViewModel

```csharp
[ObservableObject]
public partial class CharacterViewModel : ISaveable
{
    private readonly CharacterModel _model;
    private readonly OutlineService _outlineService;
    private readonly LogService _logger;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private int _age;

    [ObservableProperty]
    private string _role;

    public CharacterViewModel(CharacterModel model)
    {
        _model = model;
        _outlineService = Ioc.Default.GetService<OutlineService>();
        _logger = Ioc.Default.GetService<LogService>();

        // Load from model
        _name = model.Name;
        _age = model.Age;
        _role = model.Role;
    }

    public void SaveModel()
    {
        _model.Name = Name;
        _model.Age = Age;
        _model.Role = Role;
    }

    [RelayCommand]
    private async Task SaveCharacter()
    {
        SaveModel();
        await _outlineService.SaveStoryAsync();
        Messenger.Send(new StatusChangedMessage(
            new StatusMessage("Character saved", LogLevel.Info, true)));
    }
}
```

### Pattern 2: Shell/Main ViewModel with Messaging

```csharp
public class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string _statusMessage;

    [ObservableProperty]
    private Color _statusColor;

    public ShellViewModel()
    {
        // Register message handlers
        Messenger.Register<StatusChangedMessage>(this, OnStatusChanged);
        Messenger.Register<IsChangedMessage>(this, OnDocumentChanged);

        IsActive = true;
    }

    private void OnStatusChanged(StatusChangedMessage message)
    {
        StatusMessage = message.Value.Status;
        StatusColor = message.Value.Level switch
        {
            LogLevel.Info => Colors.Blue,
            LogLevel.Warn => Colors.Goldenrod,
            LogLevel.Error => Colors.Red,
            _ => Colors.Gray
        };
    }

    private void OnDocumentChanged(IsChangedMessage message)
    {
        // Update save button, etc.
        SaveCommand.NotifyCanExecuteChanged();
    }
}
```

### Pattern 3: Service with Messaging

```csharp
public class OutlineService
{
    private readonly LogService _logger;

    public OutlineService(LogService logger)
    {
        _logger = logger;
    }

    public Guid AddStoryElement(StoryModel model, Guid parentGuid, StoryItemType type, string name)
    {
        using (var lock = new SerializationLock(...))
        {
            var newElement = new StoryElement { Name = name, Type = type };
            model.StoryElements.Add(newElement);

            // Notify via messenger
            WeakReferenceMessenger.Default.Send(new StatusChangedMessage(
                new StatusMessage($"Added {type}: {name}", LogLevel.Info, false)));

            return newElement.Uuid;
        }
    }
}
```

---

## Common Issues and Solutions

### Issue: Property change not raising notifications

**Problem**:
```csharp
private string _name; // Missing [ObservableProperty]

public string Name
{
    get => _name;
    set => _name = value; // No notification!
}
```

**Solution**:
```csharp
[ObservableProperty]
private string _name; // Source generator handles notifications
```

### Issue: Command not updating CanExecute

**Problem**:
```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { }

private bool CanSave() => !string.IsNullOrEmpty(Name);

[ObservableProperty]
private string _name; // CanSave doesn't re-evaluate!
```

**Solution**:
```csharp
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
private string _name; // Automatically notifies SaveCommand
```

Or manually:
```csharp
[ObservableProperty]
private string _name;

partial void OnNameChanged(string value)
{
    SaveCommand.NotifyCanExecuteChanged();
}
```

### Issue: Messages not being received

**Problem**: Not registered or IsActive = false

**Solution**:
```csharp
public class MyViewModel : ObservableRecipient
{
    public MyViewModel()
    {
        Messenger.Register<MyMessage>(this, OnMyMessage);
        IsActive = true; // Important!
    }
}
```

### Issue: Circular messaging

**Problem**: Message handler sends same message type

**Solution**: Use different message types or add guard:
```csharp
private bool _isUpdating = false;

private void OnNameChanged(NameChangedMessage message)
{
    if (_isUpdating) return;

    _isUpdating = true;
    // Handle message
    _isUpdating = false;
}
```

---

## Best Practices for StoryCAD

### ✅ Do:
1. **Use `[ObservableProperty]`** for all bindable properties
2. **Use `[RelayCommand]`** for all commands
3. **Implement ISaveable** in element ViewModels
4. **Use IMessenger** for cross-component communication
5. **Register services in BootStrapper**
6. **Set IsActive = true** when using ObservableRecipient
7. **Unregister or set IsActive = false** when ViewModel is disposed

### ❌ Don't:
1. **Manually implement INotifyPropertyChanged** (use source generators)
2. **Create ICommand instances manually** (use `[RelayCommand]`)
3. **Couple ViewModels directly** (use IMessenger)
4. **Forget to call NotifyCanExecuteChanged** when CanExecute conditions change
5. **Register messages without setting IsActive = true** (for ObservableRecipient)

---

## See Also

- **Context7 for API docs**: `@context7 CommunityToolkit.Mvvm`
- **Official Docs**: [https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- **Patterns Quick Reference**: `.claude/docs/architecture/patterns-quick-ref.md`
- **ViewModel Template**: `.claude/docs/examples/new-viewmodel-template.md`
- **Architecture Notes**: `/devdocs/StoryCAD_architecture_notes.md`
