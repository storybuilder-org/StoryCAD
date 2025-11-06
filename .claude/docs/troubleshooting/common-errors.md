# Common Errors and Solutions

## Purpose
Quick troubleshooting guide for common StoryCAD development errors.

---

## Build and Compilation Errors

### Error: `Package.Current` throws PlatformNotSupportedException

**Problem**: Using Windows-specific API on non-Windows platform

**Error Message**:
```
System.PlatformNotSupportedException: 'Package.Current is not supported on this platform'
```

**Solution**:
```csharp
// Use conditional compilation
#if HAS_UNO_WINUI
var package = Package.Current;
#else
// Fallback for non-Windows
var version = Assembly.GetExecutingAssembly().GetName().Version;
#endif
```

---

### Error: Circular dependency between ViewModels and Services

**Problem**: ViewModel depends on Service, Service depends on ViewModel

**Error Message**:
```
CS0146: Circular dependency detected
```

**Solution**: Use ISaveable pattern instead of direct ViewModel references
```csharp
// ❌ WRONG - Direct ViewModel dependency
public class MyService
{
    public void SaveViewModel(CharacterViewModel vm) { }
}

// ✅ RIGHT - Use ISaveable interface
public class MyService
{
    public void SaveCurrent()
    {
        var saveable = _appState.CurrentSaveable;
        saveable?.SaveModel();
    }
}
```

**See Also**: `/devdocs/StoryCAD_architecture_notes.md` (Circular Dependencies section)

---

### Error: Missing ObservableProperty backing field

**Problem**: Used `[ObservableProperty]` on a property instead of field

**Error Message**:
```
CS0592: Attribute 'ObservableProperty' is not valid on this declaration type
```

**Solution**:
```csharp
// ❌ WRONG
[ObservableProperty]
public string Name { get; set; }

// ✅ RIGHT
[ObservableProperty]
private string _name;
```

---

## Runtime Errors

### Error: NullReferenceException in SaveModel()

**Problem**: Model not initialized before calling SaveModel()

**Error Message**:
```
System.NullReferenceException: Object reference not set to an instance of an object
at CharacterViewModel.SaveModel()
```

**Solution**: Always check for null
```csharp
public void SaveModel()
{
    if (_model == null)
    {
        _logger.LogWarn("SaveModel called but _model is null");
        return;
    }

    _model.Name = Name;
    // ... rest of saves
}
```

---

### Error: Data loss after navigation

**Problem**: Forgot to register ISaveable in page navigation

**Symptoms**: User edits data, navigates away, data is lost

**Solution**: Register/unregister in page navigation
```csharp
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    var appState = Ioc.Default.GetService<AppState>();
    appState.CurrentSaveable = ViewModel; // Register
}

protected override void OnNavigatedFrom(NavigationEventArgs e)
{
    var appState = Ioc.Default.GetService<AppState>();
    appState.CurrentSaveable = null; // Unregister
    base.OnNavigatedFrom(e);
}
```

---

### Error: Command CanExecute not updating

**Problem**: Command doesn't re-evaluate when property changes

**Symptoms**: Button stays disabled even when validation passes

**Solution**: Notify CanExecute changed
```csharp
// Option 1: Use attribute
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
private string _name;

// Option 2: Manual notification
partial void OnNameChanged(string value)
{
    SaveCommand.NotifyCanExecuteChanged();
}
```

---

### Error: SerializationLock timeout

**Problem**: Long-running operation inside SerializationLock

**Error Message**:
```
Operation timed out waiting for SerializationLock
```

**Solution**: Move long operations outside lock
```csharp
// ❌ WRONG - Long operation in lock
using (var lock = new SerializationLock(...))
{
    await DownloadLargeFile(); // Takes minutes!
    model.File = downloadedFile;
}

// ✅ RIGHT - Download first, then lock briefly
var downloadedFile = await DownloadLargeFile();
using (var lock = new SerializationLock(...))
{
    model.File = downloadedFile; // Quick operation
}
```

---

## Platform-Specific Errors

### Error: File path with wrong separator

**Problem**: Hardcoded `\` or `/` in file paths

**Symptoms**: Works on Windows, fails on macOS (or vice versa)

**Solution**: Use `Path.Combine()`
```csharp
// ❌ WRONG
var path = dataFolder + "\\" + filename;

// ✅ RIGHT
var path = Path.Combine(dataFolder, filename);
```

---

### Error: Cmd+S not working on macOS

**Problem**: Used `VirtualKeyModifiers.Control` on macOS (should be Menu)

**Solution**: Use conditional compilation
```csharp
#if HAS_UNO_WINUI
var modifier = VirtualKeyModifiers.Control; // Ctrl
#elif __MACOS__
var modifier = VirtualKeyModifiers.Menu;    // Cmd
#endif
```

---

### Error: XAML resource not found on macOS

**Problem**: Resource defined in Windows-specific file

**Solution**: Move to platform-agnostic location or use platform-specific resource dictionaries

---

## Messaging Errors

### Error: IMessenger messages not received

**Problem 1**: Not registered for message

**Solution**:
```csharp
// Register in constructor
Messenger.Register<StatusChangedMessage>(this, OnStatusChanged);
```

**Problem 2**: IsActive not set (for ObservableRecipient)

**Solution**:
```csharp
public class MyViewModel : ObservableRecipient
{
    public MyViewModel()
    {
        Messenger.Register<MyMessage>(this, OnMyMessage);
        IsActive = true; // IMPORTANT!
    }
}
```

---

### Error: Circular message loop

**Problem**: Message handler sends the same message type

**Symptoms**: Stack overflow, infinite loop

**Solution**: Add guard flag
```csharp
private bool _isUpdating = false;

private void OnNameChanged(NameChangedMessage message)
{
    if (_isUpdating) return;

    _isUpdating = true;
    try
    {
        // Handle message
    }
    finally
    {
        _isUpdating = false;
    }
}
```

---

## Test Failures

### Error: Test fails with "Service not registered"

**Problem**: Service not registered in test IoC container

**Solution**: Register services in test setup
```csharp
[TestInitialize]
public void Setup()
{
    Ioc.Default.ConfigureServices(
        new ServiceCollection()
            .AddSingleton<LogService>()
            .AddSingleton<AppState>()
            .BuildServiceProvider()
    );
}
```

---

### Error: "Collection was modified" during enumeration

**Problem**: Modifying collection while iterating

**Solution**: Use ToList() or iterate backwards
```csharp
// ❌ WRONG
foreach (var item in collection)
{
    collection.Remove(item); // Modifies during iteration
}

// ✅ RIGHT - Option 1: ToList()
foreach (var item in collection.ToList())
{
    collection.Remove(item);
}

// ✅ RIGHT - Option 2: Backwards iteration
for (int i = collection.Count - 1; i >= 0; i--)
{
    collection.RemoveAt(i);
}
```

---

## Dependency Injection Errors

### Error: Service has dependencies but no constructor

**Problem**: Service registered in IoC but constructor parameters not provided

**Error Message**:
```
InvalidOperationException: Unable to resolve service for type 'LogService'
```

**Solution**: Register all dependencies
```csharp
// In BootStrapper.cs
services
    .AddSingleton<LogService>()           // Register dependency first
    .AddSingleton<AppState>()             // Also a dependency
    .AddSingleton<MyService>();           // Then register service
```

---

### Error: Using Ioc.Default in service constructor

**Problem**: Service using IoC container directly instead of constructor injection

**Solution**: Use proper constructor injection
```csharp
// ❌ WRONG
public class MyService
{
    private readonly LogService _logger;

    public MyService()
    {
        _logger = Ioc.Default.GetService<LogService>();
    }
}

// ✅ RIGHT
public class MyService
{
    private readonly LogService _logger;

    public MyService(LogService logger)
    {
        _logger = logger;
    }
}
```

---

## Performance Issues

### Error: UI freezing during operations

**Problem**: Long-running operation on UI thread

**Solution**: Use async/await
```csharp
// ❌ WRONG
[RelayCommand]
private void LoadData()
{
    var data = LoadLargeDataset(); // Blocks UI
    DataItems = data;
}

// ✅ RIGHT
[RelayCommand]
private async Task LoadData()
{
    var data = await Task.Run(() => LoadLargeDataset());
    DataItems = data;
}
```

---

### Error: Memory leak from event handlers

**Problem**: Event handlers not unsubscribed

**Solution**: Unsubscribe in cleanup
```csharp
public class MyViewModel
{
    public MyViewModel()
    {
        Messenger.Register<MyMessage>(this, OnMyMessage);
    }

    public void Cleanup()
    {
        Messenger.Unregister<MyMessage>(this);
        // or use: Messenger.UnregisterAll(this);
    }
}
```

---

## API and External Service Errors

### Error: Semantic Kernel API key not found

**Problem**: OpenAI/Azure OpenAI API key not configured

**Error Message**:
```
InvalidOperationException: API key not configured
```

**Solution**: Set environment variable
```bash
# Windows (PowerShell)
$env:OPENAI_API_KEY="sk-..."

# macOS/Linux (bash/zsh)
export OPENAI_API_KEY="sk-..."

# Or use Doppler
doppler secrets set OPENAI_API_KEY="sk-..."
```

---

## Quick Diagnostic Checklist

When encountering an error:

1. **Check the error message**
   - [ ] Read the full stack trace
   - [ ] Identify the exact file and line number

2. **Check patterns**
   - [ ] Is ISaveable implemented?
   - [ ] Is SerializationLock used for state changes?
   - [ ] Are services stateless?
   - [ ] Is the model passed as a parameter?

3. **Check platform**
   - [ ] Does it use platform-specific APIs?
   - [ ] Is conditional compilation used correctly?
   - [ ] Does it work on both Windows and macOS?

4. **Check IoC/DI**
   - [ ] Is the service registered in BootStrapper?
   - [ ] Are all dependencies registered?
   - [ ] Is constructor injection used (not Ioc.Default)?

5. **Check logs**
   - [ ] Review application logs
   - [ ] Check for warnings before error
   - [ ] Enable trace-level logging if needed

---

## Getting Help

### Check Documentation
1. **Architecture Notes**: `/devdocs/StoryCAD_architecture_notes.md`
2. **Patterns Quick Reference**: `.claude/docs/architecture/patterns-quick-ref.md`
3. **Dependency Guides**: `.claude/docs/dependencies/`

### Check Examples
1. **Existing Code**: Look for similar implementations in `/StoryCADLib/`
2. **Templates**: `.claude/docs/examples/`
3. **Tests**: `/StoryCADTests/` for working examples

### Ask Claude Code
- Provide full error message and stack trace
- Include relevant code snippet
- Mention which pattern you're trying to follow

---

## See Also

- **Patterns Quick Reference**: `.claude/docs/architecture/patterns-quick-ref.md`
- **Architecture Notes**: `/devdocs/StoryCAD_architecture_notes.md`
- **Testing Guide**: `/devdocs/testing.md`
- **Build Commands**: `/devdocs/build_commands.md`
