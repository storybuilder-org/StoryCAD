# StoryCAD Debugging Configuration Guide

## Overview
This document provides debugging configuration settings for StoryCAD using UNO Platform with WinAppSDK and Desktop heads.

## Visual Studio 2022 Configuration

### Prerequisites
1. **Enable WinUI 3 Debugging Tools**
   - Navigate to: Tools → Options → Environment → Preview Features
   - Enable: "Enable UI Debugging Tooling for WinUI 3 Projects"
   - This enables Live Visual Tree, Live Property Explorer, and Hot Reload

### Target Framework Ordering
**Critical**: The order of target frameworks in the .csproj file affects debugging functionality.

```xml
<PropertyGroup>
  <!-- WinAppSDK must be first for Windows debugging -->
  <TargetFrameworks>net9.0-windows10.0.26100.0;net9.0-desktop</TargetFrameworks>
</PropertyGroup>
```

**Note**: If WebAssembly is added in the future, it must be placed last to prevent debugging issues.

## Launch Profiles

### launchSettings.json Configuration
Located at: `/StoryCAD/Properties/launchSettings.json`

```json
{
  "profiles": {
    "StoryCAD (WinAppSDK Unpackaged)": {
      "commandName": "Project",
      "compatibleTargetFramework": "windows"
    },
    "StoryCAD (WinAppSDK Packaged)": {
      "commandName": "MsixPackage",
      "compatibleTargetFramework": "windows"
    },
    "StoryCAD (Desktop)": {
      "commandName": "Project",
      "compatibleTargetFramework": "desktop"
    },
    "StoryCAD (Desktop WSL2)": {
      "commandName": "WSL2",
      "commandLineArgs": "{ProjectDir}/bin/Debug/net9.0-desktop/StoryCAD.dll",
      "distributionName": "",
      "compatibleTargetFramework": "desktop"
    }
  }
}
```

### Profile Selection Issues

#### WinAppSDK Unpackaged Profile
If the unpackaged profile doesn't appear in Visual Studio:
1. Comment out or remove the "StoryCAD (WinAppSDK Packaged)" entry from launchSettings.json
2. Reload the project
3. Select "StoryCAD (WinAppSDK Unpackaged)"

This is due to a known Visual Studio issue: https://aka.platform.uno/wasdk-maui-debug-profile-issue

## Platform-Specific Debugging

### Windows (WinAppSDK)

#### Recommended Settings
- **Configuration**: Debug
- **Platform**: x64
- **Profile**: "StoryCAD (WinAppSDK Unpackaged)" for development
- **Profile**: "StoryCAD (WinAppSDK Packaged)" for testing packaged scenarios

#### Environment Variables
```json
"environmentVariables": {
  "DOTNET_MODIFIABLE_ASSEMBLIES": "debug"  // Enables Hot Reload
}
```

#### Known Issues
- **Single Instance Debugging**: When debugging in Visual Studio, single instancing works but the window may not come to front
  - **Workaround**: Deploy the app, close it, then start from Start Menu to test single instancing

- **XAML Shutdown Exception**: 0xc000027b may occur when debugging (F5) but not in release (Ctrl+F5)
  - **Cause**: PrintManager event handlers accessing XAML resources during shutdown
  - **Status**: Fixed in current branch

### macOS (Desktop)

#### VSCode Configuration
Located at: `/.vscode/launch.json`

```json
{
  "name": "Uno Platform Desktop Debug",
  "type": "coreclr",
  "request": "launch",
  "preLaunchTask": "build-desktop",
  "program": "${workspaceFolder}/StoryCAD/bin/Debug/net9.0-desktop/StoryCAD.dll",
  "args": [],
  "launchSettingsProfile": "StoryCAD (Desktop)",
  "env": {
    "DOTNET_MODIFIABLE_ASSEMBLIES": "debug"
  },
  "cwd": "${workspaceFolder}/StoryCAD",
  "console": "internalConsole",
  "stopAtEntry": false
}
```

#### Rider Configuration
Located at: `/.run/StoryCAD.run.xml`

```xml
<configuration name="StoryCAD (Desktop)" type="LaunchSettings">
  <option name="LAUNCH_PROFILE_PROJECT_FILE_PATH" value="$PROJECT_DIR$/StoryCAD/StoryCAD.csproj" />
  <option name="LAUNCH_PROFILE_TFM" value="net9.0-desktop" />
  <option name="LAUNCH_PROFILE_NAME" value="StoryCAD (Desktop)" />
</configuration>
```

## Debugging Best Practices

### 1. Use Conditional Breakpoints
For platform-specific issues:
```csharp
#if HAS_UNO_WINUI
    // Set breakpoint here for Windows-specific debugging
#elif __MACOS__
    // Set breakpoint here for macOS-specific debugging
#endif
```

### 2. Diagnostic Logging
Enable verbose logging for debugging:
```csharp
// In App.xaml.cs or during startup
LogService.SetLogLevel(LogLevel.Debug);
```

### 3. Developer Menu
When debugging in Visual Studio, a developer menu appears with diagnostic tools:
- Memory usage
- Performance metrics
- Log viewer
- Feature flags

### 4. Hot Reload
Ensure Hot Reload is enabled:
- Visual Studio: Debug → Options → Hot Reload → Enable Hot Reload
- Add `DOTNET_MODIFIABLE_ASSEMBLIES=debug` environment variable

## Common Debugging Scenarios

### File I/O Issues
```csharp
// Use platform-agnostic paths
var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "test.stbx");

// Add debug output
Debug.WriteLine($"Attempting to access: {path}");
Debug.WriteLine($"File exists: {File.Exists(path)}");
```

### UI Thread Issues
```csharp
// Ensure UI operations on correct thread
DispatcherQueue.TryEnqueue(() =>
{
    // UI update code
    Debug.WriteLine($"Thread: {Thread.CurrentThread.ManagedThreadId}");
});
```

### Platform-Specific Behavior
```csharp
// Debug platform detection
Debug.WriteLine($"Platform: {RuntimeInformation.OSDescription}");
Debug.WriteLine($"Framework: {RuntimeInformation.FrameworkDescription}");
#if HAS_UNO_WINUI
Debug.WriteLine("Running on Windows (WinAppSDK)");
#elif __MACOS__
Debug.WriteLine("Running on macOS (Desktop)");
#endif
```

## Troubleshooting

### Debug Session Won't Start
1. Check target framework order in .csproj
2. Verify launch profile compatibility
3. Clean and rebuild solution
4. Check for conflicting launch profiles in launchSettings.json

### Breakpoints Not Hit
1. Ensure "Just My Code" is disabled for framework debugging
2. Check symbol loading (Debug → Windows → Modules)
3. Verify PDB files are generated and accessible
4. Clean bin/obj folders and rebuild

### Hot Reload Not Working
1. Verify `DOTNET_MODIFIABLE_ASSEMBLIES=debug` is set
2. Check Hot Reload is enabled in VS settings
3. Ensure compatible changes (see Hot Reload limitations)
4. Target framework must support Hot Reload

### Platform-Specific Code Not Executing
1. Verify conditional compilation symbols
2. Check partial class file naming conventions
3. Ensure platform-specific files are included in project
4. Review build output for compilation warnings

## Additional Resources

- [UNO Platform Debugging Documentation](https://platform.uno/docs/articles/uno-development/debugging-uno-ui.html)
- [Visual Studio Debugging Guide](https://docs.microsoft.com/en-us/visualstudio/debugger/)
- [WinUI 3 Debugging Tools](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/)
- [Platform-Specific Code in UNO](https://platform.uno/docs/articles/platform-specific-csharp.html)

## Quick Reference

### Build Configurations
- **Debug**: Full debugging symbols, no optimization
- **Release**: Optimized, minimal symbols
- **Profile**: Release with full symbols for profiling

### Command Line Debugging
```bash
# Windows (WinAppSDK)
dotnet run --project StoryCAD/StoryCAD.csproj --framework net9.0-windows10.0.26100.0

# macOS (Desktop)
dotnet run --project StoryCAD/StoryCAD.csproj --framework net9.0-desktop

# With verbose logging
dotnet run --project StoryCAD/StoryCAD.csproj --framework net9.0-desktop --verbosity detailed
```

### Debugging Shortcuts
- **F5**: Start Debugging
- **Ctrl+F5**: Start Without Debugging
- **Shift+F5**: Stop Debugging
- **F10**: Step Over
- **F11**: Step Into
- **Shift+F11**: Step Out
- **F9**: Toggle Breakpoint