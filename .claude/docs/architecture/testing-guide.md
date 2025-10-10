# StoryCAD Testing Guide

## Testing Philosophy

Follow Test-Driven Development (TDD):
1. Write a failing test for desired behavior
2. Run test and verify it fails with expected error
3. Write minimal code to make test pass
4. Run test and verify it passes
5. Refactor if needed while keeping tests green

### TDD Starting Approaches

There are two common ways to start a TDD cycle:

**Approach 1: Test-First (Compilation Errors)**
- Write test for non-existent method/property
- Get compilation error
- Add minimal implementation to compile
- Run test and see it fail properly
- Make it pass

**Approach 2: Stub-First (Runtime Failures)**
- Create empty method/property stub that compiles
- Write test that will fail at runtime
- Run test and see it fail with assertion error
- Implement to make it pass

Both approaches are valid TDD. StoryCAD uses both:
- Test-first for new properties/methods (compilation errors are the first "red")
- Stub-first when the interface already exists but behavior needs to change

## Test Organization

### File Structure
- One test class per production class
- Test class naming: `[ClassName]Tests`
- Test files mirror production structure
- Test data in `/TestInputs/` directory

### Test Method Naming
Use descriptive names that explain the scenario and expected outcome:
```csharp
[TestMethod]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Example: CurrentView_SetProperty_RaisesPropertyChanged
}
```

## Common Test Patterns

### Property Change Notification Testing
```csharp
[TestMethod]
public void PropertyName_WhenSet_RaisesPropertyChanged()
{
    // Arrange
    var model = new Model();
    var propertyChanged = false;
    model.PropertyChanged += (s, e) => {
        if (e.PropertyName == nameof(Model.Property))
            propertyChanged = true;
    };
    
    // Act
    model.Property = newValue;
    
    // Assert
    Assert.IsTrue(propertyChanged);
}
```

### Collection Change Monitoring
```csharp
[TestMethod]
public void Collection_AddItem_SetsModelChanged()
{
    // Arrange
    var model = new StoryModel();
    model.Changed = false;
    
    // Act
    model.TrashView.Add(new StoryNodeItem());
    
    // Assert
    Assert.IsTrue(model.Changed);
}
```

### UI Testing with MSTest
```csharp
[UITestMethod]
public async Task UIElement_Action_ExpectedResult()
{
    // Arrange
    await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
    {
        // UI setup code
    });
    
    // Act & Assert
    await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
    {
        // UI interaction and verification
    });
}
```

## Creating Test Data

### Method 1: Using SemanticKernelApi (Preferred)
```csharp
// Create API instance
var api = new SemanticKernelApi();

// Create outline with template
var result = await api.CreateEmptyOutline("Test Story", "Test Author", "0");
Assert.IsTrue(result.IsSuccess);

// Get and update elements
var elements = api.GetAllElements();
api.UpdateElementProperties(elementGuid, new Dictionary<string, object>
{
    { "Name", "Updated Name" }
});

// Save to file
var writeResult = await api.WriteOutline(testFilePath);
```

### Method 2: Using OutlineService
```csharp
var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

// Template 0: Blank (Overview + TrashCan)
var model = await outlineService.CreateModel("Test", "Author", 0);

// Template 1: With problem/characters
var model = await outlineService.CreateModel("Test", "Author", 1);

// Don't forget to reset Changed flag
model.Changed = false;
```

### Method 3: Direct Model Construction
```csharp
var model = new StoryModel();
var overview = new OverviewModel("Test", model, null);
model.ExplorerView.Add(overview.Node);

var trash = new TrashCanModel(model, null);
model.TrashView.Add(trash.Node);
```

## Test Data Management

### Test File Paths
```csharp
private readonly string TestFile = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "StoryCAD",
    "Test.stbx"
);
```

### Cleanup Pattern
```csharp
[TestCleanup]
public void Cleanup()
{
    if (File.Exists(TestFile))
        File.Delete(TestFile);
}
```

### Using TestInputs Directory
```csharp
string testDataPath = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "TestInputs",
    "SampleOutline.stbx"
);
```

## Debugging Test Failures

### Investigation Protocol
1. Read the test to understand expected behavior
2. Check if test assumptions are still valid
3. Add diagnostic output if needed:
   ```csharp
   Console.WriteLine($"Expected: {expected}, Actual: {actual}");
   ```
4. Debug the actual failure, not symptoms
5. Only modify test if requirements changed

### Common Issues

**"Transient" failures often aren't:**
- Check for uninitialized state
- Verify cleanup between tests
- Look for timing/async issues
- Examine collection counts and ordering

**Property binding failures:**
- Ensure using properties, not fields
- Check PropertyChanged implementation
- Verify binding mode (OneWay vs TwoWay)

**Collection issues:**
- Check for duplicate entries
- Verify parent-child relationships
- Ensure proper initialization

## Running Tests

### From WSL/Claude Code
See [Build Commands](./.claude/build_commands.md) for detailed commands.

Quick reference:
```bash
# Run all tests
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net8.0-windows10.0.19041.0/StoryCADTests.dll"

# Run specific test class
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net8.0-windows10.0.19041.0/StoryCADTests.dll" /Tests:FileTests
```

## Best Practices

1. **Test one thing per test** - Each test should verify a single behavior
2. **Use descriptive assertions** - Include messages that explain failures
3. **Keep tests independent** - No dependencies between tests
4. **Fast tests** - Mock external dependencies when possible
5. **Readable tests** - Tests are documentation of expected behavior

## Integration Testing

For features that require the full application context:
1. Build the application first
2. Test UI functionality manually
3. Document edge cases discovered
4. Add unit tests for specific issues found

Remember: "That coulda, woulda, shoulda stuff doesn't work in code" - always verify with actual tests!