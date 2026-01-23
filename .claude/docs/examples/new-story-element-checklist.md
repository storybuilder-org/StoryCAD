# Adding a New Story Element Type - Complete Checklist

## Purpose

This checklist ensures all integration points are covered when adding a new story element type to StoryCAD. Missing any item can cause crashes, missing features, or data loss.

**Source:** Derived from Issue #782 (StoryWorld) implementation which identified several missed integration points during code review.

---

## Quick Reference

| Category | Items | Typical Files |
|----------|-------|---------------|
| Core | 5 | Enum, Model, ViewModel, Page XAML, Page code-behind |
| Serialization | 2 | StoryElementConverter.cs |
| TreeView/Navigation | 5 | StoryNodeItem.cs, ShellViewModel.cs, App.xaml.cs |
| Commands/Menu | 5 | ShellViewModel.cs, Shell.xaml |
| OutlineService | 2 | OutlineService.cs |
| DI/Registration | 2 | BootStrapper.cs, Lists.json |
| Reports | 7 | Multiple files |
| API | 3 | SemanticKernelAPI.cs |
| Tests | 5+ | Test project |
| Documentation | 4+ | User manual |

---

## Detailed Checklist

### 1. Core Model/View/ViewModel

- [ ] **StoryItemType enum** - Add new type value
  - File: `StoryCADLib/Enums/StoryItemType.cs`

- [ ] **Model class** - Create `[ElementName]Model.cs`
  - File: `StoryCADLib/Models/[ElementName]Model.cs`
  - Inherit from `StoryElement`
  - Set `ElementType` in constructor

- [ ] **ViewModel class** - Create `[ElementName]ViewModel.cs`
  - File: `StoryCADLib/ViewModels/[ElementName]ViewModel.cs`
  - Implement `ISaveable`, `INavigable`, `IReloadable` as needed
  - See: `new-viewmodel-template.md`

- [ ] **Page XAML** - Create `[ElementName]Page.xaml`
  - File: `StoryCAD/Views/[ElementName]Page.xaml`

- [ ] **Page code-behind** - Create `[ElementName]Page.xaml.cs`
  - File: `StoryCAD/Views/[ElementName]Page.xaml.cs`
  - Register ISaveable in OnNavigatedTo/OnNavigatedFrom

### 2. JSON Serialization

- [ ] **StoryElementConverter Read** - Add case to type discriminator switch
  - File: `StoryCADLib/DAL/StoryElementConverter.cs`
  - Location: `Read()` method, `targetType` switch (~line 30-44)
  ```csharp
  "[ElementName]" => typeof([ElementName]Model),
  ```

- [ ] **StoryElementConverter Write** - Add case to type discriminator switch
  - File: `StoryCADLib/DAL/StoryElementConverter.cs`
  - Location: `Write()` method, `typeDiscriminator` switch (~line 95-109)
  ```csharp
  [ElementName]Model _ => "[ElementName]",
  ```

### 3. TreeView Icon & Navigation

- [ ] **StoryNodeItem Symbol - Constructor 1** - Add icon case
  - File: `StoryCADLib/ViewModels/StoryNodeItem.cs`
  - Location: First constructor switch (~line 524-556)
  ```csharp
  case StoryItemType.[ElementName]:
      Symbol = Symbol.[IconName];
      break;
  ```

- [ ] **StoryNodeItem Symbol - Constructor 2** - Add icon case (DUPLICATE CODE!)
  - File: `StoryCADLib/ViewModels/StoryNodeItem.cs`
  - Location: Second constructor switch (~line 592-624)
  - **CRITICAL:** Both constructors have identical switch statements - update BOTH!

- [ ] **ShellViewModel Page Constant** - Add page identifier
  - File: `StoryCADLib/ViewModels/ShellViewModel.cs`
  - Add: `public const string [ElementName]Page = "[ElementName]Page";`

- [ ] **App.xaml.cs Navigation Config** - Register page
  - File: `StoryCAD/App.xaml.cs`
  - Add: `nav.Configure([ElementName]Page, typeof([ElementName]Page));`

- [ ] **ShellViewModel TreeViewNodeClicked** - Add navigation case
  - File: `StoryCADLib/ViewModels/ShellViewModel.cs`
  - Location: `TreeViewNodeClicked()` switch statement

- [ ] **ShellViewModel SaveModel** - Add save case
  - File: `StoryCADLib/ViewModels/ShellViewModel.cs`
  - Location: `SaveModel()` switch statement
  ```csharp
  case [ElementName]Page:
      var vm = Ioc.Default.GetRequiredService<[ElementName]ViewModel>();
      vm.SaveModel();
      break;
  ```

### 4. Commands & Menu UI

- [ ] **ShellViewModel Command Property** - Add command
  - File: `StoryCADLib/ViewModels/ShellViewModel.cs`
  - Add: `public IRelayCommand Add[ElementName]Command { get; }`

- [ ] **ShellViewModel CanAdd Method** - Add validation
  - File: `StoryCADLib/ViewModels/ShellViewModel.cs`
  - Add method checking `SerializationLock` and any constraints (singleton, etc.)

- [ ] **Shell.xaml Flyout Menu** - Add menu item with icon
  - File: `StoryCAD/Views/Shell.xaml`
  - Location: Add flyout `<MenuFlyout>`
  ```xaml
  <MenuFlyoutItem Command="{x:Bind ShellVm.Add[ElementName]Command}"
                  Text="Add [ElementName]">
      <MenuFlyoutItem.Icon>
          <SymbolIcon Symbol="[IconName]" />
      </MenuFlyoutItem.Icon>
  </MenuFlyoutItem>
  ```

- [ ] **Shell.xaml Context Menu** - Add right-click menu item
  - File: `StoryCAD/Views/Shell.xaml`
  - Location: TreeView context menu

- [ ] **Shell.xaml Keyboard Shortcut** - Add KeyboardAccelerator
  - File: `StoryCAD/Views/Shell.xaml`
  - **NOTE:** Check available shortcuts on BOTH Windows (Alt+key) and macOS (Cmd+key)
  - Document the shortcut in user manual

### 5. OutlineService

- [ ] **OutlineService.AddStoryElement** - Add creation case
  - File: `StoryCADLib/Services/Outline/OutlineService.cs`
  - Location: `AddStoryElement()` switch expression

- [ ] **Singleton Check** (if applicable) - Add validation
  - File: `StoryCADLib/Services/Outline/OutlineService.cs`
  - Add helper method like `[ElementName]Exists()` if element is singleton

### 6. DI Registration

- [ ] **BootStrapper.cs** - Register ViewModel
  - File: `StoryCADLib/Services/BootStrapper.cs`
  ```csharp
  services.AddTransient<[ElementName]ViewModel>();
  ```

- [ ] **Lists.json** - Add any list data (if applicable)
  - File: `StoryCADLib/Assets/Install/Lists.json`

### 7. Reports

- [ ] **Report Template** - Create template file
  - File: `StoryCADLib/Assets/Install/reports/[Element Name].txt`

- [ ] **StoryCADLib.csproj** - Add as EmbeddedResource
  - File: `StoryCADLib/StoryCADLib.csproj`
  ```xml
  <EmbeddedResource Include="Assets\Install\reports\[Element Name].txt" />
  ```

- [ ] **PrintReportDialogVM** - Add checkbox property
  - File: `StoryCADLib/ViewModels/Tools/PrintReportDialogVM.cs`
  - Add: `public bool Create[ElementName] { get; set; }`

- [ ] **ReportFormatter** - Add format method
  - File: `StoryCADLib/Services/Reports/ReportFormatter.cs`
  - Add: `Format[ElementName]Report()` method

- [ ] **PrintReports** - Add generation case
  - File: `StoryCADLib/Services/Reports/PrintReports.cs`
  - Location: `Generate()` method

- [ ] **ScrivenerReports** - Add generation case and method
  - File: `StoryCADLib/Services/Reports/ScrivenerReports.cs`
  - Add case in main method + `Generate[ElementName]Report()` method

- [ ] **PrintReportsDialog.xaml** - Add checkbox to UI
  - File: `StoryCADLib/Services/Dialogs/Tools/PrintReportsDialog.xaml`

### 8. API Integration

- [ ] **SemanticKernelAPI GetXxx** (if singleton) - Add convenience method
  - File: `StoryCADLib/Services/API/SemanticKernelAPI.cs`
  - Add: `Get[ElementName]()` method returning `OperationResult<StoryElement>`

- [ ] **SemanticKernelAPI GetElementsByType** - Update to handle new type
  - File: `StoryCADLib/Services/API/SemanticKernelAPI.cs`
  - Update switch or filtering logic

- [ ] **API Description Attributes** - Update documentation
  - File: `StoryCADLib/Services/API/SemanticKernelAPI.cs`
  - Update `[Description]` attributes to mention new element type

### 9. Tests (TDD)

- [ ] **Model Tests** - Test model creation and properties
- [ ] **ViewModel Tests** - Test LoadModel, SaveModel, commands
- [ ] **TreeView Icon Test** - Verify correct Symbol is assigned
  ```csharp
  [TestMethod]
  public void [ElementName]_Node_Has[Icon]Symbol()
  {
      // Arrange
      StoryModel model = new();
      OverviewModel overview = new("Overview", model, null);
      model.ExplorerView.Add(overview.Node);

      // Act
      [ElementName]Model element = new("Test", model, overview.Node);

      // Assert
      Assert.AreEqual(Symbol.[Icon], element.Node.Symbol);
  }
  ```
- [ ] **Report Tests** - Test report generation
- [ ] **API Tests** - Test API methods

### 10. User Documentation

#### Required: Reference Documentation

- [ ] **Main Form Page** - Create `[ElementName]_Form.md`
  - Location: `/docs/Story Elements/`
  - Include: Purpose, how to add, overview of tabs

- [ ] **Tab Pages** (if applicable) - Create page per tab
  - Location: `/docs/Story Elements/`
  - Include: Screenshot, field descriptions, examples

- [ ] **Reports Documentation** - Update reports pages
  - Files: `/docs/Reports/Print_Reports.md`, `Scrivener_Reports.md`

- [ ] **Keyboard Shortcuts** - Document new shortcut
  - File: `/docs/Quick Start/Keyboard_Shortcuts.md`

#### Optional: Educational Content

- [ ] **Conceptual Guide** (if new concepts involved) - Create or update guide
  - Location: `/docs/Writing with StoryCAD/`
  - Include: Explanation of concepts, how to use the feature effectively, examples
  - **Note:** If the new story element introduces concepts that need explanation beyond field descriptions (e.g., StoryWorld's World Type classification, worldbuilding taxonomies), add educational content to help writers understand *why* and *how* to use the feature, not just *what* the fields are.

---

## Common Mistakes

### Missing TreeView Icon
**Symptom:** Element appears in TreeView with no icon
**Cause:** `StoryNodeItem` constructors not updated
**Fix:** Add case to BOTH constructors (they have duplicate switch statements)

### JSON Deserialization Crash
**Symptom:** `JsonException: Unsupported Type discriminator`
**Cause:** `StoryElementConverter.Read()` missing case
**Fix:** Add case to Read() switch expression

### Data Loss on Navigation
**Symptom:** Changes lost when clicking another element
**Cause:** `ShellViewModel.SaveModel()` missing case
**Fix:** Add case to SaveModel() switch statement

### Element Not Appearing in Reports
**Symptom:** Element missing from print/export
**Cause:** Missing report template or Generate() case
**Fix:** Add template file, csproj reference, and Generate() case

### Keyboard Shortcut Conflict
**Symptom:** Shortcut doesn't work or triggers wrong action
**Cause:** Shortcut already used by another command
**Fix:** Check existing shortcuts in Shell.xaml before assigning; test on both Windows and macOS

---

## See Also

- **ViewModel Template:** `new-viewmodel-template.md`
- **Patterns Quick Reference:** `patterns-quick-ref.md`
- **Issue #782 Log:** `/devdocs/worldbuilding/issue_782_log.md` (real-world example)
