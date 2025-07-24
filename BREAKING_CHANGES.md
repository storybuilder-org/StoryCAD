# Breaking Changes and Migration Guide

## Overview
This document outlines breaking changes made to improve thread safety and security in StoryCAD.

## Thread Safety Improvements

### Modified Classes
The following classes have been refactored to prevent deadlocks and improve thread safety:

1. `ToolsData` - Tools initialization
2. `ListData` - Lists initialization  
3. `ControlData` - Controls initialization

### Breaking Changes

#### Before (Dangerous - Could Cause Deadlocks)
```csharp
// Old synchronous initialization in constructor
public ToolsData() {
    Task.Run(async () => {
        // async work
    }).Wait(); // DEADLOCK RISK!
}

// Usage was immediate
var tools = Ioc.Default.GetService<ToolsData>();
var questions = tools.KeyQuestionsSource; // Data available immediately
```

#### After (Safe - Async Initialization)
```csharp
// New safe initialization pattern
public ToolsData() {
    // Initialize empty collections
    KeyQuestionsSource = new Dictionary<string, List<KeyQuestionModel>>();
    // ... other collections
    
    // Lazy async initialization
    _initializationTask = new Lazy<Task>(InitializeAsync);
}

// Usage requires explicit initialization
var tools = Ioc.Default.GetService<ToolsData>();
await tools.EnsureInitializedAsync(); // Call this before using data
var questions = tools.KeyQuestionsSource; // Now safe to use
```

### Migration Guide for Consumers

#### ViewModels Using These Services
Update any ViewModels that use `ToolsData`, `ListData`, or `ControlData`:

```csharp
// Old pattern (no longer works correctly)
public MyViewModel()
{
    var tools = Ioc.Default.GetService<ToolsData>();
    SomeProperty = tools.KeyQuestionsSource["SomeKey"]; // May be empty!
}

// New pattern (safe)
public MyViewModel()
{
    // Initialize in constructor
}

public async Task InitializeAsync()
{
    var tools = Ioc.Default.GetService<ToolsData>();
    await tools.EnsureInitializedAsync();
    SomeProperty = tools.KeyQuestionsSource["SomeKey"]; // Now guaranteed to be loaded
}
```

#### For View Code-Behind or Pages
```csharp
public sealed partial class MyPage : Page
{
    public MyPage()
    {
        this.InitializeComponent();
        // Don't access services data immediately
        Loaded += OnPageLoaded;
    }
    
    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Initialize data services
        await Ioc.Default.GetService<ToolsData>().EnsureInitializedAsync();
        await Ioc.Default.GetService<ListData>().EnsureInitializedAsync();
        await Ioc.Default.GetService<ControlData>().EnsureInitializedAsync();
        
        // Now safe to bind or use data
        UpdateUI();
    }
}
```

### Benefits
1. **Eliminates Deadlock Risk**: No more blocking async operations in constructors
2. **Better Performance**: Lazy loading means faster app startup
3. **Thread Safety**: Proper async patterns throughout
4. **Error Handling**: Better error reporting for initialization failures

## Security Improvements

### Temporary File Handling
The `FileOpenVM.OpenSample()` method now implements secure temporary file handling:

#### Improvements Made:
1. **Unique File Names**: Uses GUID to prevent naming conflicts and information disclosure
2. **File Attributes**: Marks files as temporary for OS cleanup
3. **Error Cleanup**: Automatically removes temporary files on errors
4. **Exception Safety**: Prevents resource leaks

#### Old (Insecure):
```csharp
var filePath = Path.Combine(Path.GetTempPath(), $"{SampleNames[SelectedSampleIndex]}.stbx");
await File.WriteAllTextAsync(filePath, content);
```

#### New (Secure):
```csharp
var tempFileName = $"{SampleNames[SelectedSampleIndex]}_{Guid.NewGuid():N}.stbx";
var filePath = Path.Combine(Path.GetTempPath(), tempFileName);
// + proper cleanup and error handling
```

## Testing Considerations

### Unit Tests
Update unit tests to call `EnsureInitializedAsync()` before using service data:

```csharp
[TestMethod]
public async Task TestSomething()
{
    var tools = Ioc.Default.GetService<ToolsData>();
    await tools.EnsureInitializedAsync();
    
    // Now test logic with fully initialized data
    Assert.IsTrue(tools.KeyQuestionsSource.Count > 0);
}
```

### Integration Tests
For integration tests, consider creating a test helper:

```csharp
public static class TestHelpers
{
    public static async Task InitializeAllServicesAsync()
    {
        await Ioc.Default.GetService<ToolsData>().EnsureInitializedAsync();
        await Ioc.Default.GetService<ListData>().EnsureInitializedAsync();
        await Ioc.Default.GetService<ControlData>().EnsureInitializedAsync();
    }
}
```

## Configuration Fix

### NLog Configuration
Fixed typo in `StoryCADLib/NLog.config`:
- **Before**: `iternalLogLevel="Debug"`
- **After**: `internalLogLevel="Debug"`

This ensures proper NLog internal logging configuration.