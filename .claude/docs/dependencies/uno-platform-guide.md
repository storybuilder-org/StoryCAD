# UNO Platform Guide for StoryCAD

## Package Information

- **SDK**: Uno.Sdk
- **Version**: 6.2.36
- **Purpose**: Cross-platform UI framework enabling Windows and macOS support from single codebase
- **Documentation**: Use Context7 MCP for latest docs: `@context7 Uno.Platform` or visit [https://platform.uno/docs/](https://platform.uno/docs/)

---

## What is UNO Platform?

UNO Platform enables StoryCAD to run on multiple operating systems from a single codebase. It provides:

1. **WinUI 3 Compatibility**: Native WinUI 3 on Windows, emulated on other platforms
2. **Platform Heads**: Different execution models for different platforms
3. **Skia Rendering**: Cross-platform 2D graphics
4. **Native Performance**: Platform-specific rendering where available
5. **Conditional Compilation**: Platform-specific code separated via preprocessor directives

---

## StoryCAD's Platform Targets

### Windows (WinAppSDK Head)
- **Target**: `net9.0-windows10.0.22621`
- **Runtime**: WinAppSDK (Windows App SDK 1.6)
- **Packaging**: MSIX
- **Status**: ✅ Primary development platform

### macOS (Desktop Head)
- **Target**: `net9.0-desktop`
- **Runtime**: Skia + native macOS
- **Packaging**: .app bundle
- **Status**: ✅ Active development (UNOTestBranch)

### Future Platforms
- **Linux**: Desktop head with Skia
- **WebAssembly**: Browser-based execution
- **iOS/Android**: Mobile platforms

**Current Code Reuse**: ~95% shared between Windows and macOS

---

## Platform-Specific Code Patterns

### 1. Conditional Compilation (Recommended)

Use preprocessor directives for platform-specific code:

```csharp
#if HAS_UNO_WINUI
// Windows-specific code (WinAppSDK head)
var package = Package.Current;
var version = package.Id.Version;
#elif __MACOS__
// macOS-specific code
var version = NSBundle.MainBundle.InfoDictionary["CFBundleVersion"];
#else
// Other platforms or shared fallback
var version = "Unknown";
#endif
```

**Platform Symbols**:
| Symbol | Platform |
|--------|----------|
| `HAS_UNO_WINUI` | Windows (WinAppSDK head) |
| `__MACOS__` | macOS |
| `__IOS__` | iOS |
| `__ANDROID__` | Android |
| `__WASM__` | WebAssembly |
| `__SKIA__` | Any Skia-based platform (macOS, Linux, etc.) |

### 2. Partial Classes (For Large Platform Differences)

Separate platform-specific implementations into different files:

**Windowing.cs** (shared):
```csharp
namespace StoryCAD.Services;

public partial class Windowing
{
    public partial void InitializeWindow();

    public void ConfigureWindow()
    {
        InitializeWindow(); // Calls platform-specific implementation
    }
}
```

**Windowing.WinAppSDK.cs** (Windows-only):
```csharp
#if HAS_UNO_WINUI
namespace StoryCAD.Services;

public partial class Windowing
{
    public partial void InitializeWindow()
    {
        // Windows-specific window setup
        var window = App.MainWindow;
        window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
    }
}
#endif
```

**Windowing.desktop.cs** (macOS/Linux):
```csharp
#if __MACOS__
namespace StoryCAD.Services;

public partial class Windowing
{
    public partial void InitializeWindow()
    {
        // macOS-specific window setup
        var window = App.MainWindow;
        // macOS configuration
    }
}
#endif
```

**File Naming Convention**:
- `{ClassName}.cs` - Shared code
- `{ClassName}.WinAppSDK.cs` - Windows-specific
- `{ClassName}.desktop.cs` - macOS/Linux-specific
- `{ClassName}.mobile.cs` - iOS/Android-specific
- `{ClassName}.wasm.cs` - WebAssembly-specific

### 3. Runtime Platform Detection

Use when platform-specific APIs might throw on other platforms:

```csharp
public bool IsPackagedApp()
{
    try
    {
        var package = Package.Current; // Windows-specific API
        return package != null;
    }
    catch
    {
        // Non-Windows platform or unpackaged app
        return false;
    }
}
```

### 4. Platform-Specific File Exclusion

Exclude files from compilation on certain platforms via `.csproj`:

```xml
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) != 'windows'">
    <!-- Remove Windows-specific files on non-Windows platforms -->
    <Compile Remove="ViewModels\Tools\PrintReportDialogVM.WinAppSDK.cs" />
</ItemGroup>
```

---

## Common UNO Platform Scenarios in StoryCAD

### Keyboard Shortcuts (Platform Modifiers)

Windows uses `Ctrl` while macOS uses `Cmd` (Menu key):

```csharp
public void ConfigureKeyboardShortcuts()
{
    var saveAccelerator = new KeyboardAccelerator
    {
#if HAS_UNO_WINUI
        Modifiers = VirtualKeyModifiers.Control, // Ctrl on Windows
#elif __MACOS__
        Modifiers = VirtualKeyModifiers.Menu,    // Cmd on macOS
#endif
        Key = VirtualKey.S
    };

    saveAccelerator.Invoked += SaveCommand_Invoked;
    KeyboardAccelerators.Add(saveAccelerator);
}
```

### File Paths

Use platform-agnostic path handling:

```csharp
// Wrong - hardcoded Windows path
var path = @"C:\Users\User\Documents\StoryCAD\story.stbx";

// Right - platform-agnostic
var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
var storycadFolder = Path.Combine(documentsFolder, "StoryCAD");
var storyPath = Path.Combine(storycadFolder, "story.stbx");
```

### Logging Configuration

Different logging providers per platform:

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
#endif
}
```

### Package Information

Windows-specific packaging APIs:

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
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
    }
#else
    // Non-Windows platforms
    return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
#endif
}
```

---

## Testing on Multiple Platforms

### Build for Windows
```bash
dotnet build StoryCAD.csproj -c Debug -f net9.0-windows10.0.22621
```

### Build for macOS
```bash
dotnet build StoryCAD.csproj -c Debug -f net9.0-desktop
```

### Run Tests (Multi-Target)
```bash
dotnet test StoryCADTests/StoryCADTests.csproj
```

---

## Common Issues and Solutions

### Issue: `Package.Current` throws on macOS

**Problem**:
```csharp
var package = Package.Current; // PlatformNotSupportedException on macOS
```

**Solution**:
```csharp
#if HAS_UNO_WINUI
var package = Package.Current;
#else
// Fallback for non-Windows
var version = Assembly.GetExecutingAssembly().GetName().Version;
#endif
```

### Issue: File path separator differences

**Problem**: Hardcoded `\` or `/` in paths

**Solution**: Use `Path.Combine()` which handles platform-specific separators:
```csharp
// Wrong
var path = dataFolder + "\\" + filename;

// Right
var path = Path.Combine(dataFolder, filename);
```

### Issue: Menu key accelerators not working on macOS

**Problem**: Using `VirtualKeyModifiers.Control` on macOS (should be `VirtualKeyModifiers.Menu`)

**Solution**: Use conditional compilation for modifiers (see Keyboard Shortcuts example above)

### Issue: XAML resources not found on non-Windows

**Problem**: Resources defined in Windows-specific files

**Solution**: Move shared resources to platform-agnostic files, or use platform-specific resource dictionaries

---

## UNO Platform Resources

### Official Documentation
- **Main Docs**: [https://platform.uno/docs/](https://platform.uno/docs/)
- **API Reference**: Use Context7 MCP: `@context7 Uno.Platform`
- **Platform-Specific C#**: [https://platform.uno/docs/articles/platform-specific-csharp.html](https://platform.uno/docs/articles/platform-specific-csharp.html)
- **Migration Guide**: [https://platform.uno/docs/articles/howto-migrate-existing-code.html](https://platform.uno/docs/articles/howto-migrate-existing-code.html)

### StoryCAD-Specific Resources
- **Full Architecture**: `/devdocs/StoryCAD_architecture_notes.md#uno-platform-architecture`
- **Migration Project**: [GitHub Project Board](https://github.com/orgs/storybuilder-org/projects/6)
- **Testing Strategy**: `/StoryCADTests/ManualTests/UNO_Platform_Testing_Strategy.md`
- **Platform-Specific Code Examples**: `.claude/docs/examples/platform-specific-code.md`

---

## Best Practices for StoryCAD

### ✅ Do:
1. **Test on both Windows and macOS** before committing to UNOTestBranch
2. **Use `Path.Combine()`** for all file paths
3. **Use partial classes** for large platform-specific implementations
4. **Use `#if` directives** for small platform-specific code blocks
5. **Write platform-agnostic code first**, then add platform-specific overrides
6. **Document platform differences** in code comments

### ❌ Don't:
1. **Hardcode Windows paths** (`C:\`, `\` separators)
2. **Assume Windows-only APIs** are available (Package.Current, etc.)
3. **Forget platform-specific keyboard modifiers** (Ctrl vs Cmd)
4. **Use reflection or dynamic code** when platform-specific code is better
5. **Skip testing on macOS** if changes affect UI or file operations

---

## Quick Decision Tree

**Need platform-specific code?**

→ **Small difference (< 10 lines)?**
   - Use `#if` preprocessor directives

→ **Large difference (> 10 lines)?**
   - Use partial classes with separate files

→ **API might not exist on all platforms?**
   - Use runtime try/catch with fallback

→ **File needs to be platform-specific?**
   - Use file exclusion in `.csproj`

**Unsure if code is cross-platform?**
1. Check if it uses Windows-specific APIs (Package.*, Windows.*, etc.)
2. Build for `net9.0-desktop` target and verify no errors
3. Test on macOS if available

---

## See Also

- **Platform-Specific Code Examples**: `.claude/docs/examples/platform-specific-code.md`
- **Architecture Overview**: `/devdocs/StoryCAD_architecture_notes.md`
- **Patterns Quick Reference**: `.claude/docs/architecture/patterns-quick-ref.md`
- **Context7 for API docs**: Use `@context7 Uno.Platform` in Claude Code
