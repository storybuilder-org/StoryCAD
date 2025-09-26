# Issue #962: Test Specifications for PrintReportDialogVM

## TDD Approach
Write these tests BEFORE implementing the partial class refactoring to ensure functionality is preserved.

## Test Categories

### 1. Report Selection State Tests

```csharp
[TestClass]
public class PrintReportDialogVMReportSelectionTests
{
    [TestMethod]
    public void DefaultState_AllSelectionsAreFalse()
    {
        // Verify initial state has no reports selected
    }

    [TestMethod]
    public void SelectAllProblems_WhenSet_UpdatesProperty()
    {
        // Test property change notification
    }

    [TestMethod]
    public void MultipleSelections_CanBeSetIndependently()
    {
        // Verify multiple report types can be selected
    }
}
```

### 2. Report Generation Tests

```csharp
[TestClass]  
public class PrintReportDialogVMReportGenerationTests
{
    [TestMethod]
    public async Task GenerateReport_WithNoSelections_ReturnsEmptyReport()
    {
        // Arrange
        var vm = CreateTestVM();
        // All selections false by default
        
        // Act
        var report = await vm.GenerateReportContentAsync();
        
        // Assert
        Assert.IsTrue(string.IsNullOrWhiteSpace(report));
    }

    [TestMethod]
    public async Task GenerateReport_WithOverviewSelected_IncludesOverviewContent()
    {
        // Arrange
        var vm = CreateTestVM();
        vm.CreateOverview = true;
        SetupTestStoryModel();
        
        // Act
        var report = await vm.GenerateReportContentAsync();
        
        // Assert
        Assert.IsTrue(report.Contains("STORY OVERVIEW"));
        Assert.IsTrue(report.Contains(_testModel.StoryName));
    }

    [TestMethod]
    public async Task GenerateReport_WithCharacterListSelected_IncludesCharacterNames()
    {
        // Test character list generation
    }

    [TestMethod]
    public async Task GenerateReport_WithMultipleSelections_IncludesAllSelectedContent()
    {
        // Test combining multiple report types
    }
}
```

### 3. Page Breaking Tests

```csharp
[TestClass]
public class PrintReportDialogVMPaginationTests
{
    [TestMethod]
    public void BuildReportPages_WithShortContent_CreatesSinglePage()
    {
        // Arrange
        var content = GenerateLines(50); // Less than 70 lines
        
        // Act
        var pages = PrintReportDialogVM.BuildReportPages(content, 70);
        
        // Assert
        Assert.AreEqual(1, pages.Count);
        Assert.AreEqual(50, pages[0].Count);
    }

    [TestMethod]
    public void BuildReportPages_WithLongContent_CreatesMultiplePages()
    {
        // Arrange
        var content = GenerateLines(150); // More than 70 lines
        
        // Act
        var pages = PrintReportDialogVM.BuildReportPages(content, 70);
        
        // Assert
        Assert.AreEqual(3, pages.Count); // 70 + 70 + 10
        Assert.AreEqual(70, pages[0].Count);
        Assert.AreEqual(70, pages[1].Count);
        Assert.AreEqual(10, pages[2].Count);
    }

    [TestMethod]
    public void BuildReportPages_RespectsPageBreakMarkers()
    {
        // Test \PageBreak handling
    }
}
```

### 4. Platform-Specific Behavior Tests

```csharp
[TestClass]
public class PrintReportDialogVMPlatformTests
{
    [TestMethod]
    public async Task OpenPrintReportDialog_WithPrintMode_CallsStartPrintMenu()
    {
        // Arrange
        var vm = CreateTestVM();
        var startPrintMenuCalled = false;
        // Mock or override StartPrintMenu
        
        // Act
        await vm.OpenPrintReportDialog(ReportOutputMode.Print);
        
        // Assert
        Assert.IsTrue(startPrintMenuCalled);
    }

    [TestMethod]
    public async Task OpenPrintReportDialog_WithPdfMode_CallsExportPdf()
    {
        // Test PDF mode routing
    }
}
```

### 5. Node Traversal Tests

```csharp
[TestClass]
public class PrintReportDialogVMNodeTraversalTests
{
    [TestMethod]
    public void TraverseNode_WithValidNode_AddsToReportElements()
    {
        // Arrange
        var vm = CreateTestVM();
        var testNode = CreateTestCharacterNode();
        vm.SelectAllCharacters = true;
        
        // Act
        vm.TraverseNode(testNode);
        
        // Assert
        Assert.IsTrue(vm.ReportElements.Contains(testNode));
    }

    [TestMethod]
    public void TraverseNode_WithUnselectedType_SkipsNode()
    {
        // Test filtering based on selections
    }

    [TestMethod]
    public void TraverseNode_RecursivelyProcessesChildren()
    {
        // Test tree traversal
    }
}
```

### 6. Integration Tests (After Refactoring)

```csharp
[TestClass]
public class PrintReportDialogVMIntegrationTests
{
    [TestMethod]
    public async Task FullReportGeneration_FromUISelections_ProducesExpectedOutput()
    {
        // End-to-end test simulating user selections
    }

    [TestMethod]
    public void PlatformSpecificMethods_AreProperlyIsolated()
    {
        // Verify partial class separation
    }
}
```

## Test Helpers

```csharp
public static class PrintReportTestHelpers
{
    public static PrintReportDialogVM CreateTestVM()
    {
        var appState = new AppState();
        var windowing = new MockWindowing();
        var editFlush = new EditFlushService(...);
        var logger = new MockLogService();
        
        return new PrintReportDialogVM(appState, windowing, editFlush, logger);
    }
    
    public static StoryModel CreateTestStoryModel()
    {
        // Create test story with various elements
    }
    
    public static string GenerateLines(int count)
    {
        return string.Join(Environment.NewLine, 
            Enumerable.Range(1, count).Select(i => $"Line {i}"));
    }
}
```

## Mocking Requirements

1. **MockWindowing** - Returns test window handles
2. **MockLogService** - Captures log messages for verification  
3. **File operations** - Mock file picker dialogs
4. **PrintManager** - Not needed for VM tests (UI concern)

## Coverage Goals

- All public methods on PrintReportDialogVM
- All report selection combinations
- Edge cases (empty story, very large reports)
- Platform routing logic
- Error conditions

## Notes

1. These tests should work regardless of platform
2. No UI dependencies (PrintDocument, StackPanel) in tests
3. Focus on data flow and business logic
4. Use async test methods where appropriate
5. Follow AAA pattern (Arrange, Act, Assert)