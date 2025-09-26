# Issue #962: Platform Feature Matrix & Requirements

## 1. Complete Platform Feature Matrix

### Core Functionality Matrix

| Feature | Windows (WinAppSDK) | macOS (Desktop) | Linux (Desktop) |
|---------|---------------------|-----------------|-----------------|
| **Menu Items** |
| File → Print Reports | ✅ Visible | ❌ Hidden | ❌ Hidden |
| File → Export Reports to PDF | ✅ Visible | ✅ Visible (as "Export Reports") | ✅ Visible (as "Export Reports") |
| **Keyboard Shortcuts** |
| Print (Ctrl/Cmd+P) | ✅ Active | ❌ Not bound | ❌ Not bound |
| Export PDF (Ctrl/Cmd+E) | ✅ Active | ✅ Active | ✅ Active |
| **Output Methods** |
| Print to Physical Printer | ✅ Full support | ❌ N/A | ❌ N/A |
| Print Preview | ✅ Native dialog | ❌ N/A | ❌ N/A |
| Export to PDF | ✅ File picker | ✅ File picker | ✅ File picker |
| **Report Features** |
| Story Overview | ✅ | ✅ | ✅ |
| Story Synopsis | ✅ | ✅ | ✅ |
| Problem Structure | ✅ | ✅ | ✅ |
| Element Lists | ✅ | ✅ | ✅ |
| Individual Elements | ✅ | ✅ | ✅ |
| **UI Components** |
| Report Selection Dialog | ✅ Standard | ✅ Standard | ✅ Standard |
| Print Settings Dialog | ✅ Native Windows | ❌ N/A | ❌ N/A |
| File Save Dialog | ✅ Native | ✅ Native | ✅ Native |
| Progress Indication | ✅ Print queue | ⚠️ Need custom | ⚠️ Need custom |
| **Error Handling** |
| Printer Errors | ✅ Native messages | N/A | N/A |
| PDF Errors | ✅ Custom messages | ✅ Custom messages | ✅ Custom messages |
| Permissions Errors | ✅ Windows UAC | ✅ macOS perms | ✅ Linux perms |

### Technical Capability Matrix

| Capability | Windows (WinAppSDK) | macOS (Desktop) | Linux (Desktop) |
|-----------|---------------------|-----------------|-----------------|
| **APIs** |
| PrintManager | ✅ Full | ❌ Not available | ❌ Not available |
| PrintDocument | ✅ Full | ❌ Not available | ❌ Not available |
| Window.WindowHandle | ✅ HWND | ⚠️ Platform handle | ⚠️ Platform handle |
| File Pickers | ✅ Native | ✅ Native | ✅ GTK/Qt |
| **Rendering** |
| Native Print Pipeline | ✅ GDI/Direct2D | ❌ | ❌ |
| SkiaSharp | ✅ | ✅ | ✅ |
| Font Rendering | ✅ DirectWrite | ✅ CoreText | ✅ FreeType |
| **File System** |
| Documents Folder | ✅ %USERPROFILE%\Documents | ✅ ~/Documents | ✅ ~/Documents |
| Temp Files | ✅ %TEMP% | ✅ /tmp | ✅ /tmp |
| Path Separators | ✅ Backslash | ✅ Forward slash | ✅ Forward slash |

## 2. UI/UX Specifications by Platform

### Windows (WinAppSDK)

#### Menu Structure
```
File
├── New Story                  Ctrl+N
├── Open Story                 Ctrl+O
├── Save Story                 Ctrl+S
├── ─────────────────────────
├── Print Reports...           Ctrl+P    ← Traditional printing
├── Export Reports to PDF...   Ctrl+E    ← PDF option
├── ─────────────────────────
└── Exit                       Alt+F4
```

#### Dialog Flow - Print Path
1. User selects "Print Reports..." (Ctrl+P)
2. Report Selection Dialog appears
3. User clicks "Print" button
4. Windows Print Dialog appears
5. User selects printer and settings
6. Print job queued

#### Dialog Flow - PDF Path  
1. User selects "Export Reports to PDF..." (Ctrl+E)
2. Report Selection Dialog appears
3. User clicks "Export PDF" button
4. File Save Dialog appears
5. PDF generated and saved

### macOS (Desktop)

#### Menu Structure
```
File
├── New Story                  ⌘N
├── Open Story                 ⌘O
├── Save Story                 ⌘S
├── ─────────────────────────
├── Export Reports...          ⌘E        ← PDF only
├── ─────────────────────────
└── Quit StoryCAD              ⌘Q
```

#### Dialog Flow
1. User selects "Export Reports..." (⌘E)
2. Report Selection Dialog appears
3. User clicks "Export" button
4. macOS Save Dialog appears
5. PDF generated and saved

#### Platform-Specific Considerations
- Use native macOS file dialog
- Support iCloud Drive locations
- Follow macOS HIG for button placement
- Use SF Symbols for icons where appropriate

### Linux (Desktop)

#### Menu Structure
```
File
├── New Story                  Ctrl+N
├── Open Story                 Ctrl+O  
├── Save Story                 Ctrl+S
├── ─────────────────────────
├── Export Reports...          Ctrl+E    ← PDF only
├── ─────────────────────────
└── Quit                       Ctrl+Q
```

#### Dialog Flow
1. User selects "Export Reports..." (Ctrl+E)
2. Report Selection Dialog appears
3. User clicks "Export" button
4. GTK/Qt File Dialog appears (depending on desktop environment)
5. PDF generated and saved

## 3. Non-Functional Requirements

### Performance Requirements

#### Report Generation Time Limits
| Report Size | Pages | Max Time (Print) | Max Time (PDF) | Status Updates |
|-------------|-------|------------------|----------------|----------------|
| Small | 1-10 | 2 sec | 1 sec | Not required |
| Medium | 11-50 | 5 sec | 3 sec | Every 10 pages |
| Large | 51-100 | 10 sec | 5 sec | Every 10 pages |
| Very Large | 100+ | 30 sec | 15 sec | Every 5 pages |

#### Memory Usage Limits
- Maximum memory per operation: 200 MB
- Peak memory for large reports: 500 MB
- Memory must be released after completion
- No memory leaks permitted

#### Threading Requirements
- UI must remain responsive during generation
- Cancel must respond within 500ms
- Progress updates every 1 second minimum
- Background thread for PDF generation
- UI thread for PrintManager operations

### Reliability Requirements

#### Error Recovery
1. **Transient Errors**: Retry up to 3 times
2. **File System Errors**: Provide alternate location
3. **Memory Errors**: Gracefully degrade (smaller chunks)
4. **Printer Errors**: Fall back to PDF option
5. **PDF Errors**: Provide detailed diagnostics

#### Resource Management
- All file handles must be closed
- Temporary files must be deleted
- Memory streams must be disposed
- Event handlers must be unregistered
- No resource leaks permitted

#### Cancellation Support
- User can cancel at any time
- Cancellation completes within 1 second
- Partial files are cleaned up
- UI returns to ready state
- No corruption of application state

### Operational Requirements

#### File Size Limits
- Maximum report size: 1000 pages
- Maximum PDF file size: 100 MB
- Warning at 500 pages
- Chunk large reports if needed

#### Security Requirements
- No elevation required for normal operation
- Respect OS file permissions
- No writing to system directories
- Sanitize file paths
- Validate all user input

#### Logging Requirements
```csharp
// Required log points
LogLevel.Info: "Report generation started"
LogLevel.Info: "Report generation completed: {pages} pages"
LogLevel.Warning: "Large report warning: {pages} pages"
LogLevel.Error: "Report generation failed: {error}"
LogLevel.Debug: "Page {n} generated"
```

#### Telemetry Requirements
- Report type selected
- Output method (print/PDF)
- Page count
- Generation time
- Error types (anonymous)
- Platform identifier

### Accessibility Requirements

#### Screen Reader Support
- All controls properly labeled
- Status updates announced
- Error messages announced
- Progress announced at milestones
- Keyboard navigation complete

#### Visual Requirements  
- High contrast mode support
- Focus indicators visible
- Text scalable to 200%
- Color not sole indicator
- WCAG AA compliance

## 4. Quality Assurance Strategy

### Test Data Requirements

#### Golden Report Samples
1. **Minimal Report**: 1 page, overview only
2. **Basic Report**: 5 pages, all sections
3. **Unicode Report**: Special characters, emojis
4. **Large Report**: 100+ pages
5. **Edge Cases**: Empty sections, missing data

### Visual Diff Methodology
```
1. Generate PDF on each platform
2. Convert to images (300 DPI)
3. Compare with ImageMagick
4. Tolerance: 2% pixel difference
5. Manual review of differences
```

### Platform Test Matrix
| Test Case | Windows | macOS | Linux |
|-----------|---------|-------|-------|
| Print Preview | ✅ | N/A | N/A |
| PDF Generation | ✅ | ✅ | ✅ |
| File Permissions | ✅ | ✅ | ✅ |
| Large Reports | ✅ | ✅ | ✅ |
| Cancellation | ✅ | ✅ | ✅ |
| Memory Limits | ✅ | ✅ | ✅ |

### Regression Test Suite
1. All golden samples must pass
2. Platform differences documented
3. Performance within limits
4. No memory leaks
5. Accessibility verified

## 5. Success Metrics

### Functional Success
- [ ] Windows printing works identically to current app
- [ ] PDF export works on all platforms
- [ ] Report content identical across output methods
- [ ] All platforms meet performance requirements

### Quality Success  
- [ ] Zero crashes in normal operation
- [ ] Memory usage within limits
- [ ] All errors handled gracefully
- [ ] Accessibility standards met

### User Success
- [ ] Intuitive platform-appropriate UI
- [ ] Clear progress indication
- [ ] Helpful error messages
- [ ] Successful output every time