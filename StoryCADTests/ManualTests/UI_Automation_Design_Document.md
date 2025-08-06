# UI Automation Testing Design Document for StoryCAD

## Executive Summary

This document proposes implementing automated UI testing for StoryCAD to reduce manual testing burden while maintaining quality. Given that WinAppDriver is no longer actively maintained (last release 2020), we recommend modern alternatives that work with WinUI 3.

## Current State

### Manual Testing Challenges
- Full regression takes 3-4 hours
- Smoke testing requires 5+ minutes per build
- Testing burden increases with each feature
- Risk of human error in repetitive tests

### WinAppDriver Status
- **Development paused** since 2020
- 1000+ open issues
- Microsoft focusing on Windows 11 platforms
- Not recommended for new projects

## Proposed Solution

### Phase 1: Minimal Automation (Q1 2025)
Automate only the smoke test using **Appium with Windows Driver**

#### Technology Stack
```json
{
  "framework": "MSTest (existing)",
  "driver": "Appium.WebDriver",
  "windows_driver": "Appium-windows-driver (actively maintained)",
  "pattern": "Page Object Model"
}
```

#### Why Appium Instead of WinAppDriver
- Actively maintained (weekly updates)
- Bundles WinAppDriver functionality
- Future-proof with plugin architecture
- Works with WinUI 3 applications

### Phase 2: AI-Enhanced Testing (Q2 2025)
Evaluate AI-powered tools to reduce brittleness

#### Candidates
1. **AskUI** - Visual AI recognition, no AutomationIds needed
2. **Testim.io** - Self-healing tests with ML
3. **Microsoft Power Automate** - Low-code option for non-developers

## Implementation Plan

### Step 1: Prepare Codebase (2 weeks)
Add AutomationProperties to critical UI elements:

```xml
<!-- Shell.xaml -->
<Button x:Name="FileMenuButton"
        AutomationProperties.AutomationId="FileMenu"
        AutomationProperties.Name="File menu"
        Content="File"/>

<TreeView x:Name="StoryOutlineTree"
          AutomationProperties.AutomationId="NavigationTree"
          AutomationProperties.Name="Story outline navigation"/>
```

### Step 2: Proof of Concept (1 week)
Implement automated smoke test:

```csharp
[TestClass]
public class SmokeTests
{
    private WindowsDriver<WindowsElement> driver;
    private StoryCADApp app;
    
    [TestInitialize]
    public void Setup()
    {
        var options = new AppiumOptions();
        options.AddAdditionalCapability("app", @"C:\Program Files\StoryCAD\StoryCAD.exe");
        options.AddAdditionalCapability("deviceName", "WindowsPC");
        
        driver = new WindowsDriver<WindowsElement>(
            new Uri("http://127.0.0.1:4723"), options);
        app = new StoryCADApp(driver);
    }
    
    [TestMethod]
    [TestCategory("Smoke")]
    public void ST001_ApplicationLaunch()
    {
        // Verify main window
        Assert.IsNotNull(app.MainWindow);
        Assert.IsTrue(app.NavigationPane.IsDisplayed);
        Assert.IsTrue(app.ContentPane.IsDisplayed);
    }
    
    [TestMethod]
    [TestCategory("Smoke")]
    public void ST002_CreateNewStory()
    {
        // Create new story
        app.FileMenu.Click();
        app.NewStoryMenuItem.Click();
        
        // Verify creation
        Assert.AreEqual("Untitled", app.StoryOverviewNode.Text);
        
        // Enter story name
        app.StoryNameField.SendKeys("Automated Test Story");
        
        // Verify update
        Assert.AreEqual("Automated Test Story", app.StoryOverviewNode.Text);
    }
    
    [TestMethod]
    [TestCategory("Smoke")]
    public void ST003_AddBasicElements()
    {
        // Add character
        app.NavigationPane.RightClick(app.StoryOverviewNode);
        app.ContextMenu.SelectAddCharacter();
        
        // Verify character added
        var characterNode = app.NavigationPane.FindNode("New Character");
        Assert.IsNotNull(characterNode);
        
        // Name character
        app.CharacterNameField.Clear();
        app.CharacterNameField.SendKeys("Test Character");
        
        // Verify name updated
        characterNode = app.NavigationPane.FindNode("Test Character");
        Assert.IsNotNull(characterNode);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        driver?.Quit();
    }
}
```

### Step 3: Page Object Model (1 week)
Create maintainable test structure:

```csharp
public class StoryCADApp
{
    private readonly WindowsDriver<WindowsElement> driver;
    
    public MainWindow MainWindow => new MainWindow(driver);
    public NavigationPane NavigationPane => new NavigationPane(driver);
    public ContentPane ContentPane => new ContentPane(driver);
    public FileMenu FileMenu => new FileMenu(driver);
}

public class NavigationPane
{
    private readonly WindowsDriver<WindowsElement> driver;
    
    public WindowsElement TreeView => 
        driver.FindElementByAccessibilityId("NavigationTree");
    
    public WindowsElement StoryOverviewNode => 
        TreeView.FindElementByName("Story Overview");
    
    public WindowsElement FindNode(string name) =>
        TreeView.FindElementByName(name);
    
    public void RightClick(WindowsElement element)
    {
        var actions = new Actions(driver);
        actions.ContextClick(element).Perform();
    }
}
```

### Step 4: CI Integration (1 week)
Add to build pipeline:

```yaml
# azure-pipelines.yml
- task: VSTest@2
  displayName: 'Run Smoke Tests'
  inputs:
    testSelector: 'testAssemblies'
    testAssemblyVer2: |
      **\*Tests.dll
    searchFolder: '$(System.DefaultWorkingDirectory)'
    testFiltercriteria: 'TestCategory=Smoke'
    runInParallel: false
    timeoutInMinutes: 5
```

## Success Metrics

### Phase 1 Goals
- Smoke test runs in < 2 minutes
- 90% pass rate (allowing for timing issues)
- Saves 25 hours/year (5 min × 3 builds/week × 50 weeks)

### ROI Calculation
- **Investment**: 5 weeks developer time (200 hours)
- **Savings**: 25 hours/year testing + reduced bug escapes
- **Break-even**: Year 2 if maintenance < 25 hours/year

## Risk Mitigation

### Brittleness Risk
- **Mitigation**: Use AutomationIds, not visual properties
- **Fallback**: Keep manual tests as backup

### Maintenance Risk
- **Mitigation**: Start small (5 tests), expand if successful
- **Fallback**: Can abandon if maintenance exceeds savings

### Technology Risk
- **Mitigation**: Use Appium (active) vs WinAppDriver (dead)
- **Fallback**: Evaluate AI tools if Appium fails

## Alternatives Considered

1. **Keep Manual Only**
   - Pro: No development cost
   - Con: Testing burden grows with features

2. **Full AI Solution (AskUI)**
   - Pro: No AutomationIds needed
   - Con: $500+/month licensing

3. **Custom UI Automation Library**
   - Pro: Full control
   - Con: High development cost

## Recommendation

1. **Immediate**: Continue manual testing with new focused test plans
2. **Q1 2025**: Implement Phase 1 (Appium smoke tests)
3. **Q2 2025**: Evaluate Phase 2 (AI tools) based on Phase 1 results

## Appendix: Test Automation Readiness Checklist

- [ ] AutomationIds added to Shell.xaml
- [ ] AutomationIds added to story element forms
- [ ] Appium server installed on build machine
- [ ] Test project created with Appium.WebDriver
- [ ] Page objects implemented for main UI
- [ ] Smoke tests passing locally
- [ ] CI pipeline configured
- [ ] Documentation updated
- [ ] Team trained on maintenance