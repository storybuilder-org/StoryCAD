# StoryCAD 4.0 Testing Strategy - UNO Platform Migration

## Executive Summary

The migration from WinUI 3 to UNO Platform for StoryCAD 4.0 fundamentally changes our testing approach. With Windows and macOS as initial targets, we need a cross-platform testing strategy that scales to future platforms (Linux, iOS, Android, WebAssembly).

## Key Changes from WinUI 3 to UNO

### Testing Implications
1. **Multiple Platform Targets**: Each platform may have unique behaviors
2. **Different UI Renderers**: Windows (WinUI 3), macOS (AppKit/Skia)
3. **Platform-Specific Features**: File dialogs, menus, keyboard shortcuts
4. **New Bug Categories**: Platform-specific issues, renderer differences
5. **Increased Test Matrix**: Tests × Platforms = exponential growth

## Recommended Testing Strategy for 4.0

### 1. Platform-Agnostic Core Testing (Priority 1)
**What**: Business logic and data layer tests
**How**: MSTest/NUnit in shared project
**Coverage**: 80% of functionality

```csharp
[TestClass]
public class StoryModelTests  // Runs on all platforms
{
    [TestMethod]
    public void CreateCharacter_AddsToModel()
    {
        // Pure logic test - no UI
        var model = new StoryModel();
        var character = model.CreateCharacter("Jane");
        Assert.IsNotNull(character);
    }
}
```

### 2. UI Abstraction Layer Testing (Priority 2)
**What**: Test UI behavior through ViewModels
**How**: Mock the platform-specific services
**Coverage**: 15% of functionality

```csharp
[TestClass]
public class CharacterViewModelTests
{
    [TestMethod]
    public void SaveCommand_CallsCorrectService()
    {
        var mockFileService = new Mock<IFileService>();
        var vm = new CharacterViewModel(mockFileService.Object);
        
        vm.SaveCommand.Execute(null);
        
        mockFileService.Verify(x => x.Save(It.IsAny<string>()), Times.Once);
    }
}
```

### 3. Platform-Specific UI Testing (Priority 3)
**What**: Actual UI automation per platform
**How**: Different tools per platform
**Coverage**: 5% critical paths only

#### Windows Testing
- **Tool**: Appium (as previously planned)
- **Scope**: Existing smoke tests

#### macOS Testing  
- **Tool**: XCUITest or Appium for Mac
- **Scope**: Same smoke tests, macOS-specific

```swift
// macOS UI Test
func testFileMenuOnMac() {
    let app = XCUIApplication()
    app.launch()
    
    // macOS has menu bar at top
    app.menuBars.menuBarItems["File"].click()
    app.menuItems["New Story"].click()
    
    XCTAssertTrue(app.windows["Untitled"].exists)
}
```

## Cross-Platform Test Matrix

### Smoke Test Coverage

| Test Case | Windows | macOS | Linux* | Web* | Mobile* |
|-----------|---------|-------|--------|------|---------|
| App Launch | Auto | Auto | Manual | Manual | N/A |
| File New/Open/Save | Auto | Auto | Manual | Manual | Limited |
| Add Story Elements | Auto | Auto | Manual | Manual | View Only |
| Keyboard Shortcuts | Auto | Manual | Manual | N/A | N/A |
| Native Menus | Auto | Auto | Manual | N/A | N/A |

*Future platforms

### Platform-Specific Test Areas

#### Windows-Specific
- [ ] Windows file dialogs
- [ ] Windows keyboard shortcuts (Ctrl+S)
- [ ] Windows context menus
- [ ] Multiple window support

#### macOS-Specific
- [ ] macOS menu bar
- [ ] macOS keyboard shortcuts (Cmd+S)
- [ ] macOS file dialogs (.stbx association)
- [ ] Trackpad gestures
- [ ] Retina display rendering

## Automation Strategy for UNO

### Option 1: Uno.UITest (Recommended)
**Pros**: 
- Built for UNO Platform
- Single test runs on multiple platforms
- Maintained by UNO team

**Cons**:
- Learning curve
- Limited compared to native tools

```csharp
[Test]
public class CrossPlatformSmokeTest : TestBase
{
    [Test]
    public void CreateNewStory()
    {
        App.WaitForElement("FileMenu");
        App.Tap("FileMenu");
        App.Tap("NewStoryMenuItem");
        
        App.WaitForElement("StoryOverview");
        Assert.IsTrue(App.Query("StoryOverview").Any());
    }
}
```

### Option 2: Platform-Specific Tools
- **Windows**: Appium
- **macOS**: XCUITest
- **Linux**: Dogtail/LDTP
- **Web**: Selenium/Playwright

**Pros**: Best tool for each platform
**Cons**: Maintain multiple test suites

### Option 3: AI-Powered Universal Testing
- **AskUI**: Works across all platforms
- **Testim**: Learning-based cross-platform

**Pros**: One tool, all platforms
**Cons**: Cost, less precise

## Migration Path

### Phase 1: Pre-Migration (Now - 3.3 Release)
1. Create comprehensive unit test suite (platform-agnostic)
2. Document all UI behaviors for migration validation
3. Build manual test plans that work across platforms

### Phase 2: Early Migration (4.0 Alpha)
1. Set up Uno.UITest infrastructure
2. Port smoke tests to Uno.UITest
3. Create platform-specific test projects
4. Establish CI/CD for Windows + macOS

### Phase 3: Dual Platform (4.0 Beta)
1. Run parallel testing on Windows/macOS
2. Create platform comparison reports
3. Document platform-specific behaviors
4. Automate critical paths on both platforms

### Phase 4: Production (4.0 Release)
1. Full test suite on primary platform (Windows)
2. Smoke + critical paths on secondary (macOS)
3. Community testing for edge cases

## CI/CD Pipeline for Multi-Platform

```yaml
# azure-pipelines.yml
trigger:
- main
- release/4.0

strategy:
  matrix:
    Windows:
      imageName: 'windows-latest'
      platform: 'win'
    macOS:
      imageName: 'macos-latest'
      platform: 'mac'

pool:
  vmImage: $(imageName)

steps:
- task: DotNetCoreCLI@2
  displayName: 'Run Core Tests'
  inputs:
    command: 'test'
    projects: '**/StoryCAD.Core.Tests.csproj'

- task: DotNetCoreCLI@2
  displayName: 'Run Platform Tests'
  inputs:
    command: 'test'
    projects: '**/StoryCAD.$(platform).Tests.csproj'
```

## Test Data Strategy

### Shared Test Files
Create platform-agnostic test files:
- Store in cloud (GitHub repo)
- Test file I/O on each platform
- Verify cross-platform compatibility

### Platform-Specific Test Cases
```csharp
[TestClass]
public class PlatformFileTests
{
    [TestMethod]
    [DataRow("Windows", @"C:\Users\Test\story.stbx")]
    [DataRow("macOS", "/Users/Test/story.stbx")]
    [DataRow("Linux", "/home/test/story.stbx")]
    public void ValidateFilePath(string platform, string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Create(platform)))
        {
            // Platform-specific validation
        }
    }
}
```

## Complexity Management

### 1. Shared Code Maximization
- 90% shared code across platforms
- Platform-specific code in interfaces
- Dependency injection for platform services

### 2. Test Prioritization
| Priority | Test Type | Platforms | Frequency |
|----------|-----------|-----------|-----------|
| P0 | Unit Tests | All | Every commit |
| P1 | Integration Tests | All | Every PR |
| P2 | UI Smoke Tests | Win/Mac | Daily |
| P3 | Full UI Tests | Windows | Weekly |
| P4 | Full UI Tests | macOS | Release |

### 3. Platform Feature Matrix
Document which features work on which platforms:

| Feature | Windows | macOS | Notes |
|---------|---------|-------|-------|
| File Operations | ✅ | ✅ | Different dialogs |
| Drag & Drop | ✅ | ✅ | Different implementations |
| Keyboard Shortcuts | ✅ | ⚠️ | Cmd vs Ctrl |
| Touch Gestures | ✅ | ⚠️ | Trackpad only |
| Printing | ✅ | 🔄 | Platform-specific |

## Risk Mitigation

### Platform-Specific Bugs
- **Risk**: Bug only appears on one platform
- **Mitigation**: Community beta testing per platform

### Test Maintenance Burden
- **Risk**: 2x platforms = 2x test maintenance
- **Mitigation**: Shared tests where possible, platform tests only for critical paths

### Performance Differences
- **Risk**: Slow on one platform
- **Mitigation**: Platform-specific performance benchmarks

## Recommendations

### For 3.3 Release (Current)
1. **Don't invest heavily in WinUI 3 UI automation** - it won't transfer
2. **Do invest in unit tests** - they will transfer
3. **Document current behavior thoroughly** - for comparison after migration

### For 4.0 Migration
1. **Start with Uno.UITest** for cross-platform basics
2. **Add platform-specific tests only for critical differences**
3. **Leverage community for platform-specific testing**
4. **Consider AI tools for visual regression testing**

### Long-term (4.1+)
1. **Evaluate WebAssembly** for easy testing
2. **Consider Linux** as third platform
3. **Mobile as read-only** companion apps

## Conclusion

The UNO Platform migration requires rethinking our testing strategy from "test the Windows app" to "test the core + verify per platform". By focusing on shared code testing and minimal platform-specific UI tests, we can maintain quality without exponential test growth.

The key insight: **Test the logic once, verify the UI per platform.**