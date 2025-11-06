# Platform-Specific Code Examples

## Purpose
Copy-paste examples for handling Windows vs macOS platform differences in StoryCAD.

---

## Example 1: Keyboard Shortcuts (Ctrl vs Cmd)

### Problem
Windows uses Ctrl while macOS uses Cmd (Menu key) for shortcuts.

### Solution: Conditional Compilation
```csharp
public void ConfigureKeyboardShortcuts()
{
    // Save shortcut: Ctrl+S (Windows) or Cmd+S (macOS)
    var saveAccelerator = new KeyboardAccelerator
    {
#if HAS_UNO_WINUI
        Modifiers = VirtualKeyModifiers.Control, // Ctrl on Windows
#elif __MACOS__
        Modifiers = VirtualKeyModifiers.Menu,    // Cmd on macOS
#else
        Modifiers = VirtualKeyModifiers.Control, // Default to Ctrl
#endif
        Key = VirtualKey.S
    };

    saveAccelerator.Invoked += (sender, args) => SaveCommand.Execute(null);
    KeyboardAccelerators.Add(saveAccelerator);
}
```

---

## Example 2: Partial Classes for Platform-Specific Implementation

### Shared Code (`Windowing.cs`)
```csharp
namespace StoryCAD.Services;

public partial class Windowing
{
    // Shared properties and methods
    public Window MainWindow { get; set; }

    // Partial method declaration (implemented in platform-specific files)
    public partial void InitializeWindow();
    public partial void SetWindowTitle(string title);

    // Shared logic
    public void ConfigureWindow(Window window)
    {
        MainWindow = window;
        InitializeWindow(); // Calls platform-specific implementation
    }
}
```

### Windows-Specific (`Windowing.WinAppSDK.cs`)
```csharp
#if HAS_UNO_WINUI
namespace StoryCAD.Services;

public partial class Windowing
{
    public partial void InitializeWindow()
    {
        var appWindow = MainWindow.AppWindow;

        // Windows-specific title bar customization
        appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
    }

    public partial void SetWindowTitle(string title)
    {
        MainWindow.Title = title;
    }
}
#endif
```

### macOS-Specific (`Windowing.desktop.cs`)
```csharp
#if __MACOS__
namespace StoryCAD.Services;

public partial class Windowing
{
    public partial void InitializeWindow()
    {
        // macOS-specific window setup
        var appWindow = MainWindow.AppWindow;
        // macOS doesn't support extended title bar same way
        // Use native macOS window configuration
    }

    public partial void SetWindowTitle(string title)
    {
        MainWindow.Title = title;
    }
}
#endif
```

---

## Example 3: File Paths (Platform-Agnostic)

### Problem
Windows uses `\` and macOS uses `/` as path separators.

### Solution: Use Path.Combine
```csharp
// ❌ WRONG - Hardcoded Windows path
var wrongPath = @"C:\Users\User\Documents\StoryCAD\story.stbx";

// ✅ RIGHT - Platform-agnostic
var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
var storycadFolder = Path.Combine(documentsFolder, "StoryCAD");
var storyPath = Path.Combine(storycadFolder, "story.stbx");

// Create directory if needed (works on all platforms)
if (!Directory.Exists(storycadFolder))
{
    Directory.CreateDirectory(storycadFolder);
}
```

---

## Example 4: Package APIs (Windows-Only)

### Problem
`Package.Current` only available on Windows.

### Solution: Runtime Try/Catch with Fallback
```csharp
public string GetAppVersion()
{
#if HAS_UNO_WINUI
    try
    {
        var package = Package.Current;
        var version = package.Id.Version;
        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
    catch
    {
        // Fall back to assembly version if Package.Current fails
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
    }
#else
    // Non-Windows platforms
    return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
#endif
}
```

### Alternative: Boolean Check
```csharp
public bool IsPackagedApp()
{
#if HAS_UNO_WINUI
    try
    {
        var package = Package.Current;
        return package != null;
    }
    catch
    {
        return false;
    }
#else
    return false;  // Non-Windows is never packaged
#endif
}
```

---

## Example 5: Logging Configuration

### Problem
Different platforms use different logging providers.

### Solution: Conditional Compilation for Logging Setup
```csharp
public void ConfigureLogging(ILoggingBuilder builder)
{
#if __WASM__
    builder.AddProvider(new WebAssemblyConsoleLoggerProvider());
#elif __IOS__ || __MACCATALYST__
    builder.AddProvider(new OSLogLoggerProvider());
#elif __MACOS__
    builder.AddConsole();
    builder.AddDebug();
#elif HAS_UNO_WINUI
    builder.AddConsole();
    builder.AddDebug();
    builder.AddEventLog(); // Windows-specific
#else
    builder.AddConsole();
#endif

    builder.SetMinimumLevel(LogLevel.Information);
}
```

---

## Example 6: File Picker (Platform-Specific)

### Windows (`FilePicker.WinAppSDK.cs`)
```csharp
#if HAS_UNO_WINUI
using Windows.Storage.Pickers;

public partial class FilePicker
{
    public partial async Task<string> PickFileAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".stbx");

        // Windows-specific window handle setup
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}
#endif
```

### macOS (`FilePicker.desktop.cs`)
```csharp
#if __MACOS__
using AppKit;

public partial class FilePicker
{
    public partial async Task<string> PickFileAsync()
    {
        await Task.CompletedTask; // Make it async

        var panel = NSOpenPanel.OpenPanel;
        panel.AllowedFileTypes = new[] { "stbx" };
        panel.CanChooseFiles = true;
        panel.CanChooseDirectories = false;

        if (panel.RunModal() == 1) // NSModalResponseOK
        {
            return panel.Url?.Path;
        }

        return null;
    }
}
#endif
```

---

## Example 7: Platform Detection at Runtime

### Utility Class
```csharp
public static class PlatformInfo
{
    public static bool IsWindows =>
#if HAS_UNO_WINUI
        true;
#else
        false;
#endif

    public static bool IsMacOS =>
#if __MACOS__
        true;
#else
        false;
#endif

    public static bool IsLinux =>
#if __LINUX__
        true;
#else
        false;
#endif

    public static bool IsWebAssembly =>
#if __WASM__
        true;
#else
        false;
#endif

    public static string PlatformName
    {
        get
        {
#if HAS_UNO_WINUI
            return "Windows";
#elif __MACOS__
            return "macOS";
#elif __LINUX__
            return "Linux";
#elif __WASM__
            return "WebAssembly";
#else
            return "Unknown";
#endif
        }
    }
}
```

### Usage
```csharp
public void ShowPlatformSpecificMessage()
{
    if (PlatformInfo.IsWindows)
    {
        MessageDialog.Show("Welcome to StoryCAD on Windows!");
    }
    else if (PlatformInfo.IsMacOS)
    {
        MessageDialog.Show("Welcome to StoryCAD on macOS!");
    }
}
```

---

## Example 8: Menu Accelerators

### Problem
Different keyboard layouts and conventions per platform.

### Solution: Platform-Specific Accelerator Keys
```csharp
public void ConfigureMenuAccelerators()
{
    var menuItems = new[]
    {
        ("New", VirtualKey.N),
        ("Open", VirtualKey.O),
        ("Save", VirtualKey.S),
        ("Quit", VirtualKey.Q)
    };

    foreach (var (text, key) in menuItems)
    {
#if HAS_UNO_WINUI
        var modifier = VirtualKeyModifiers.Control;
#elif __MACOS__
        var modifier = VirtualKeyModifiers.Menu; // Cmd key
#else
        var modifier = VirtualKeyModifiers.Control;
#endif

        var menuItem = new MenuFlyoutItem
        {
            Text = text,
            KeyboardAccelerators =
            {
                new KeyboardAccelerator
                {
                    Key = key,
                    Modifiers = modifier
                }
            }
        };

        MenuItems.Add(menuItem);
    }
}
```

---

## Example 9: File System Watcher

### Problem
Different file system notification mechanisms.

### Solution: Conditional Implementation
```csharp
public partial class FileWatcherService
{
    private FileSystemWatcher _watcher;

    public void StartWatching(string folderPath)
    {
#if HAS_UNO_WINUI || __MACOS__ || __LINUX__
        _watcher = new FileSystemWatcher(folderPath)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            Filter = "*.stbx"
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileCreated;
        _watcher.Deleted += OnFileDeleted;

        _watcher.EnableRaisingEvents = true;
#elif __WASM__
        // WebAssembly doesn't support file system watching
        _logger.LogWarning("File watching not supported on WebAssembly");
#endif
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInfo($"File changed: {e.FullPath}");
    }
}
```

---

## Example 10: Multi-Platform Build Matrix

### Understanding What Gets Built Where

This example demonstrates how platform-specific code behaves in different build scenarios.

### Scenario: Cross-Platform Feature with Platform Enhancements

```csharp
// FilePickerService.cs (shared)
public partial class FilePickerService
{
    // Shared logic
    public partial Task<string> PickFileAsync();

    public async Task<List<string>> PickMultipleFilesAsync()
    {
        // Shared implementation that calls platform-specific code
        var files = new List<string>();
        var firstFile = await PickFileAsync();
        if (firstFile != null)
        {
            files.Add(firstFile);
        }
        return files;
    }
}

// FilePickerService.WinAppSDK.cs (Windows-specific)
#if HAS_UNO_WINUI
using Windows.Storage.Pickers;

public partial class FilePickerService
{
    public partial async Task<string> PickFileAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".stbx");

        // Windows-specific initialization
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}
#endif

// FilePickerService.desktop.cs (macOS-specific)
#if __MACOS__
using AppKit;

public partial class FilePickerService
{
    public partial async Task<string> PickFileAsync()
    {
        await Task.CompletedTask;

        var panel = NSOpenPanel.OpenPanel;
        panel.AllowedFileTypes = new[] { "stbx" };
        panel.CanChooseFiles = true;

        // Mac-specific native dialog
        if (panel.RunModal() == 1)
        {
            return panel.Url?.Path;
        }

        return null;
    }
}
#endif
```

### Build Matrix: What Gets Included

| Build Platform | Target Framework | Files Compiled | Result |
|----------------|------------------|----------------|--------|
| **Windows** | net9.0-windows10.0.22621 | FilePickerService.cs<br>FilePickerService.WinAppSDK.cs | Windows-optimized binary with WinAppSDK picker |
| **Windows** | net9.0-desktop | FilePickerService.cs<br>~~FilePickerService.desktop.cs~~ ❌ | Runs on Mac but WITHOUT native Mac picker* |
| **macOS** | net9.0-desktop | FilePickerService.cs<br>FilePickerService.desktop.cs | Mac-optimized binary with AppKit picker |

*The `#if __MACOS__` symbol is NOT defined when building on Windows, so FilePickerService.desktop.cs code is excluded even though you're building the desktop target.

### Testing Matrix

| Built On | Tested On | File Picker Behavior |
|----------|-----------|---------------------|
| Windows (WinAppSDK) | Windows | ✅ Native Windows picker |
| Windows (Desktop) | Windows | ✅ Native Windows picker |
| Windows (Desktop) | macOS | ⚠️ Missing Mac picker (fallback behavior) |
| macOS (Desktop) | macOS | ✅ Native Mac picker |
| macOS (Desktop) | Windows | ⚠️ Missing Windows picker (fallback behavior) |

### Production Distribution Strategy

**Option 1: Cross-Platform Binary (Limited Features)**
- Build on Windows for desktop target
- Ships same binary for Windows and Mac
- Mac users get fallback file picker (not native)

**Option 2: Platform-Optimized Binaries (Recommended)**
- Build on Windows → Windows distribution
- Build on Mac → macOS distribution
- Each platform gets native file picker
- Best user experience

### Key Takeaway

The platform you BUILD on determines which platform-specific code is COMPILED, not which platforms the binary CAN run on.

```
Desktop target runs on: Windows, Mac, Linux
But includes platform code from: Build platform only
```

---

## Quick Reference: Preprocessor Symbols

| Symbol | Platform | Use Case |
|--------|----------|----------|
| `HAS_UNO_WINUI` | Windows (WinAppSDK) | Windows-specific APIs |
| `__MACOS__` | macOS | macOS-specific code |
| `__IOS__` | iOS | iOS-specific code |
| `__ANDROID__` | Android | Android-specific code |
| `__WASM__` | WebAssembly | Browser-based code |
| `__LINUX__` | Linux | Linux-specific code |
| `__SKIA__` | Skia-based platforms | macOS, Linux, WebAssembly |

---

## File Naming Conventions

- `{ClassName}.cs` - Shared code
- `{ClassName}.WinAppSDK.cs` - Windows-specific (use `#if HAS_UNO_WINUI`)
- `{ClassName}.desktop.cs` - macOS/Linux (use `#if __MACOS__`)
- `{ClassName}.mobile.cs` - iOS/Android (use `#if __IOS__ || __ANDROID__`)
- `{ClassName}.wasm.cs` - WebAssembly (use `#if __WASM__`)

---

## See Also

- **UNO Platform Guide**: `.claude/docs/dependencies/uno-platform-guide.md`
- **Architecture Notes**: `/devdocs/StoryCAD_architecture_notes.md#platform-specific-code-patterns`
- **Patterns Quick Reference**: `.claude/docs/architecture/patterns-quick-ref.md`
