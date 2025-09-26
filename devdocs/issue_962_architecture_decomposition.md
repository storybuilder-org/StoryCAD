# Issue #962: Architecture Decomposition

## 1. Report Generation Pipeline Analysis

### Core Pipeline Flow
```
User Selection → Report Generation → Format Conversion → Output
                        ↓                     ↓              ↓
              PrintReportsDialog    PrintReports.Generate   Platform
                                          (Shared)          Specific
```

### Shared Components (Platform-Independent)

#### 1. Report Content Generation
**Class**: `PrintReports`
**Method**: `Generate()`
**Responsibilities**:
- Traverse story model
- Format text content
- Apply report templates
- Insert page breaks
- Return formatted string

**Key Characteristics**:
- Pure business logic
- No UI dependencies
- No platform APIs
- Stateless operation
- Returns platform-agnostic string

#### 2. Report Selection UI
**Dialog**: `PrintReportsDialog.xaml`
**ViewModel Properties**: Selection flags in `PrintReportDialogVM`
- CreateOverview, CreateSummary, CreateStructure
- SelectAllProblems, SelectAllCharacters, etc.
- Element lists (ProblemNodes, CharacterNodes, etc.)

**Key Characteristics**:
- XAML-based UI (works on all UNO platforms)
- Data binding to ViewModel
- No platform-specific code

#### 3. Data Models
- StoryModel
- StoryNodeItem
- StoryElement derivatives
- All platform-independent

### Platform-Specific Components

#### Windows (PrintManager Path)
```csharp
// Platform-specific workflow
GeneratePrintDocumentReport() 
    → PrintDocument creation
    → Event handlers (Paginate, GetPreviewPage, AddPages)
    → StackPanel/TextBlock rendering
    → PrintManager.ShowPrintUIAsync()
```

**Dependencies**:
- Windows.Graphics.Printing.*
- Microsoft.UI.Xaml.Printing.*
- HWND (Window handle)
- Win32 interop

#### All Platforms (PDF Path)
```csharp
// Cross-platform workflow  
ExportReportsToPdfAsync()
    → SKDocument.CreatePdf()
    → SKCanvas drawing
    → Memory stream
    → File save picker
```

**Dependencies**:
- SkiaSharp
- Platform file pickers
- Memory streams
- File I/O

## 2. PrintReportDialogVM Dependencies

### Current Responsibilities (Too Many)

1. **UI State Management**
   - Dialog properties
   - Selection state
   - Data binding

2. **Report Generation Orchestration**
   - Calling PrintReports.Generate()
   - Managing report cache

3. **Windows Printing**
   - PrintDocument lifecycle
   - Print event handling
   - PrintManager interaction

4. **PDF Export**
   - SkiaSharp document creation
   - Page layout calculations
   - File saving

5. **Platform Detection**
   - Conditional compilation
   - Platform-specific messaging

### Service Dependencies
```csharp
public PrintReportDialogVM(
    AppState appState,
    Windowing window, 
    EditFlushService editFlushService,
    ILogService logService)
```

- **AppState**: Access to current document/model
- **Windowing**: Window handle, file pickers, dialogs
- **EditFlushService**: Save pending edits
- **ILogService**: Logging

### Problematic Coupling
- ViewModel knows about platform-specific APIs
- Mixing UI concerns with output generation
- Direct PrintManager manipulation
- Platform detection in business logic

## 3. Natural Split Points

### Clean Architecture Approach

#### 1. Core Layer (Shared)
```csharp
// Report generation interface
interface IReportGenerator {
    Task<string> GenerateReportAsync(ReportOptions options);
}

// Report options
class ReportOptions {
    bool CreateOverview { get; set; }
    List<StoryNodeItem> SelectedNodes { get; set; }
    // ... other options
}
```

#### 2. Output Layer (Platform-Specific)
```csharp
// Output interface
interface IReportOutput {
    Task<bool> OutputReportAsync(string reportContent);
    bool IsAvailable { get; }
    string DisplayName { get; }
}

// Implementations
class PrintManagerOutput : IReportOutput  // Windows only
class PdfOutput : IReportOutput          // All platforms
```

#### 3. ViewModel Layer (Simplified)
```csharp
class PrintReportDialogVM {
    // UI state only
    ReportOptions Options { get; }
    
    // Delegated to services
    IReportGenerator Generator { get; }
    IList<IReportOutput> AvailableOutputs { get; }
}
```

## 4. Proposed Partial Class Structure

### Option A: Minimal Change (Current Architecture)
```
PrintReportDialogVM.cs              # Shared code + PDF
PrintReportDialogVM.WinAppSDK.cs    # PrintManager code only
PrintReportDialogVM.Desktop.cs      # Desktop-specific (if any)
```

**Pros**: Least disruptive
**Cons**: Maintains architectural issues

### Option B: Clean Separation (Recommended)
```
# ViewModels
PrintReportDialogVM.cs              # UI state only

# Services  
ReportGenerator.cs                  # Shared generation
ReportOutputService.cs              # Output orchestration
├── PrintManagerOutput.WinAppSDK.cs
├── PdfOutput.cs                    # Shared 
└── ReportOutputService.Desktop.cs  # Desktop overrides
```

**Pros**: Clean architecture, testable
**Cons**: More refactoring needed

## 5. Shared vs Platform-Specific Mapping

### Definitely Shared
- Report content generation (PrintReports)
- Report selection UI (PrintReportsDialog)
- Data models (StoryModel, etc.)
- Report options/configuration
- Text formatting logic
- Page break calculations

### Definitely Platform-Specific
- PrintManager interaction
- PrintDocument creation
- HWND usage
- Print preview rendering
- Print event handlers

### Currently Shared but Could Be Platform-Specific
- File picker integration (slight differences)
- Error messaging (platform context)
- Default save locations
- Progress reporting UI

### Currently Platform-Specific but Should Be Shared
- PDF generation (works everywhere)
- Basic file I/O
- Report caching logic
- Pagination algorithms

## 6. Command and Menu Structure

### Current Implementation
```csharp
// ShellViewModel
PrintReportsCommand → OpenPrintMenu() → PrintReportDialogVM.OpenPrintReportDialog()
ExportReportsToPdfCommand → OpenExportPdfMenu() → PrintReportDialogVM.OpenPrintReportDialog(PDF)
```

### Platform-Specific Menu Visibility
- Need platform detection in ShellViewModel
- Or use partial classes for command registration
- Or use configuration-based approach

## 7. State Management Analysis

### Current State Locations
1. **Selection State**: PrintReportDialogVM properties
2. **Document State**: AppState.CurrentDocument
3. **Print State**: PrintDocument, preview cache
4. **Generation State**: In-memory during operation

### Threading Concerns
- Report generation: Can be async
- PrintManager: Must be on UI thread
- PDF generation: Can be background
- File I/O: Should be async

## 8. Key Architectural Decisions Needed

1. **Abstraction Level**
   - Keep PrintReportDialogVM monolithic with partials?
   - Or refactor to service-based architecture?

2. **Platform Detection**
   - Compile-time (#if) as current?
   - Runtime with interfaces?
   - Dependency injection?

3. **Menu Visibility**
   - Hide unavailable options?
   - Disable with explanation?
   - Platform-specific menus?

4. **Error Handling**
   - Platform-specific messages?
   - Generic fallbacks?
   - User guidance?

## 9. Recommendations

### Short Term (Minimal Disruption)
1. Use partial classes to separate platform code
2. Remove RunOnUIAsync wrapper
3. Keep current architecture
4. Focus on working implementation

### Long Term (Clean Architecture)
1. Extract report output to services
2. Use dependency injection
3. Separate concerns properly
4. Enable easy testing

### Critical Path
1. Fix printing (remove RunOnUIAsync)
2. Implement partial classes
3. Ensure both platforms work
4. Refactor architecture later