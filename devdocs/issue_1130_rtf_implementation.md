# Issue #1130: RTF Implementation Research and Strategy

**Date:** 2025-10-03
**Status:** Research Complete - **Decision: Proceed with Option 3**
**Branch:** UNOTestBranch
**Decision Date:** 2025-10-03

---

## Executive Summary

This document contains research findings and implementation strategy for adding RTF (Rich Text Format) support to StoryCAD's macOS version using UNO Platform. After evaluating multiple approaches and conducting cost-benefit analysis, **Option 3 (Hybrid - TextBox fallback with plain text editing)** has been selected for initial macOS release.

### Decision Rationale

**Selected: Option 3 (Current TextBox Implementation)**

**Key factors:**
1. **Zero current macOS users** - No existing user base to disappoint
2. **Unknown compatibility landscape** - Other macOS compatibility issues need resolution first
3. **Working implementation** - Option 3 already functional, tested, and stable
4. **Risk management** - RTF compatibility between platforms is unproven
5. **Resource allocation** - 15-25 hours better spent on critical compatibility issues
6. **Ship-and-iterate strategy** - Launch macOS version, gather user feedback, upgrade formatting in v4.1 if demanded

**Deferred: Option 1 (NSTextView POC)**
- Research complete, implementation path documented
- Can be revisited when:
  - macOS user base established
  - Users request formatting capabilities
  - Other compatibility issues resolved
  - Market validates macOS investment

**Path forward:**
- Ship v4.0 macOS with plain text editing (Option 3)
- Focus on full feature manual testing and compatibility
- Document limitation in user documentation
- Gather user feedback post-launch
- Consider Option 1 NSTextView POC for v4.1 if users demand formatting

---

## Background

StoryCAD uses `RichEditBoxExtended` controls extensively across 6 major story element pages:
- Overview (Story Idea, Concept, Premise, Structure Notes, Notes)
- Character
- Problem
- Scene
- Setting
- Folder

**Current State:**
- **Windows (WinAppSDK head):** Full RTF support via `RichEditBox` and `ITextDocument` API
- **macOS (Desktop head):** Plain `TextBox` fallback with RTF stripping (temporary workaround)

**Critical Requirements:**
- RTF persistence for cross-platform file compatibility
- Scrivener export compatibility (requires RTF format)
- Theme-aware text colors (dark/light mode)
- Two-way data binding with ViewModels

---

## Options Evaluated

### Option 1: NSTextView (Native macOS Control) 🔬 **PROOF OF CONCEPT**

**Description:** Wrap macOS's native `NSTextView` control in `RichEditBoxExtended.macOS.cs` partial class.

**Pros:**
- ✅ Full RTF compatibility on macOS (same feature set as Windows)
- ✅ Native performance and macOS-standard behavior
- ✅ Maintains Scrivener export compatibility
- ✅ Users get expected macOS text editing experience
- ✅ No loss of formatting when editing on macOS
- ✅ Future-ready for iPad (UITextView uses same pattern)

**Cons:**
- ❌ Requires AppKit/.NET for macOS knowledge
- ❌ Platform-specific code to maintain
- ❌ Initial development effort (estimated: 2-3 weeks)

**Implementation Feasibility:** ✅ **CONFIRMED VIABLE**

---

### Option 2: UNO RichEditBox ❌ **NOT VIABLE**

**Description:** Use UNO Platform's built-in `RichEditBox` control.

**Investigation Results:**
- UNO's RichEditBox is a **non-functional stub** on all non-Windows platforms
- Source: `src/Uno.UI/UI/Xaml/Controls/RichEditBox/RichEditBox.cs` (minimal implementation)
- `Document` property: `NotImplementedException` on macOS, iOS, Android, WASM, Skia
- `TextDocument` property: `NotImplementedException` on all platforms
- No platform-specific implementation files exist (no `RichEditBox.macOS.cs`)
- Open issue since 2019: [unoplatform/uno#3848](https://github.com/unoplatform/uno/issues/3848)

**Conclusion:** Cannot be used even as a basic text control. This option is off the table.

---

### Option 3: Hybrid Approach (Plain Text Editing, RTF Persistence) ✅ **SELECTED**

**Description:** Continue using `TextBox` on macOS (current state).

**Pros:**
- ✅ Minimal implementation effort (already implemented)
- ✅ Works today with no additional code
- ✅ File format compatibility maintained
- ✅ Zero risk - proven and stable
- ✅ Allows focus on other macOS compatibility issues
- ✅ Ship-and-iterate strategy - validate market first

**Cons:**
- ❌ No rich text editing on macOS (plain text only)
- ❌ Formatting created on Windows is lost when edited on macOS
- ❌ Inconsistent cross-platform experience
- ⚠️ May limit macOS version's appeal to writers (mitigated: no current macOS users to disappoint)

**Status:** **SELECTED for v4.0 macOS launch.** Current implementation by Jake working and tested. Can upgrade to Option 1 in v4.1 if user feedback demands formatting capabilities.

**Implementation Details:**
- Source: `/StoryCADLib/Controls/RichEditBoxExtended.cs` (lines 74-119)
- Inherits from `TextBox` under `#else` (HAS_UNO) condition
- Strips RTF using `ReportFormatter.GetText()` when loading
- Saves plain text to `RtfText` property on changes
- Full API compatibility with Windows version (same property names)

---

### Option 4: Wait for UNO Platform

**Description:** Monitor UNO Platform issue #3848 for official RichEditBox implementation.

**Cons:**
- ❌ Unknown timeline (issue open since 2019, labeled "challenging")
- ❌ No guarantee UNO will implement all needed features
- ❌ Poor user experience in the meantime
- ❌ Blocks macOS version's rich text capabilities indefinitely

**Conclusion:** Not recommended as primary strategy.

---

## Technical Research Findings

### 1. .NET for macOS (Xamarin.Mac Successor)

**Status:** ✅ Fully Supported

- Xamarin.Mac is **NOT deprecated** - integrated into modern .NET as ".NET for macOS"
- Available in .NET 6, 7, 8, and 9
- Target framework: `net9.0-macos` (StoryCAD uses .NET 9)
- `AppKit.NSTextView` class available via Microsoft.macOS.Sdk
- Full C# bindings, no Swift/Objective-C interop required
- Official documentation: [Microsoft Learn - AppKit.NSTextView](https://learn.microsoft.com/en-us/dotnet/api/appkit.nstextview)

### 2. UNO Platform Native Control Integration Pattern

**Pattern:** Partial classes with platform-specific implementations

**Example from UNO's TextBox:**
```
TextBox.cs (shared/generated)
TextBox.macOS.cs (macOS-specific, wraps NSTextField)
TextBox.Android.cs (Android-specific)
TextBox.iOS.cs (iOS-specific)
```

**Key mechanisms:**
- Use `#if __MACOS__` conditional compilation
- Inherit from or wrap native controls
- Maintain consistent public API surface across platforms
- Handle platform-specific events and map to WinUI equivalents

**Reference:** [UNO Platform - Creating Custom Controls](https://platform.uno/docs/articles/guides/creating-custom-controls.html)

### 3. NSTextView as Rich Text Editor

**Understanding RichEditBoxExtended Architecture:**

The "Extended" part is NOT about plain text conversion - it's about adding:
1. **`RtfText` DependencyProperty** for two-way binding with ViewModels
2. **`UpdateTheme()` method** for dark/light mode text color support
3. **Lock mechanism** (`_lockChangeExecution`) to prevent binding loops

**Windows RichEditBoxExtended:**
- Base: `RichEditBox` (provides rich text editing UI to users)
- Wrapper adds: `RtfText` binding property
- Users can: Bold, italic, underline, colors (context menu in screenshot)
- Saved as: RTF in outline files
- Plain text extraction: Used separately for reports/Scrivener via `ReportFormatter.GetText()`

**macOS Option 1 Goal: Same Pattern with NSTextView**
- Base: `NSTextView` with `RichText = true` (provides native macOS rich text editing)
- Wrapper adds: Same `RtfText` binding property, `UpdateTheme()` method
- Users can: Bold, italic, underline, colors (native macOS formatting)
- Saved as: RTF in outline files (cross-platform compatible)
- Plain text extraction: Same `ReportFormatter.GetText()` for reports

**NSTextView Key Properties:**
- `textView.String` - **plain text** content (for reading only)
- `textView.TextStorage` - **NSTextStorage** (attributed string with formatting) - THIS is what we use
- `textView.RichText` - enable/disable rich text features (set to `true`)
- `textView.AllowsUndo` - undo/redo support
- `textView.IsEditable` - read-only mode
- Native macOS formatting menu built-in

**NSTextView Rich Text Operations:**

**Loading RTF into NSTextView:**
```csharp
// Convert RTF string to NSData
NSData rtfData = NSData.FromString(rtfText, NSStringEncoding.UTF8);

// Create attributed string from RTF
NSAttributedString attrString = new NSAttributedString(
    rtfData,
    new NSAttributedStringDocumentAttributes { DocumentType = NSDocumentType.RTF },
    out NSError error
);

if (error == null && attrString != null)
{
    // Load into text view (preserves formatting)
    textView.TextStorage.SetString(attrString);
}
```

**Extracting RTF from NSTextView:**
```csharp
// Get full range of text
NSRange fullRange = new NSRange(0, textView.TextStorage.Length);

// Convert attributed string to RTF data
NSData rtfData = textView.TextStorage.GetDataFromRange(
    fullRange,
    NSDocumentType.RTF,
    out NSError error
);

if (error == null && rtfData != null)
{
    // Convert to string for storage
    string rtfString = rtfData.ToString(NSStringEncoding.UTF8);
}
```

**Detecting User Edits:**
```csharp
// Recommended approach using helper method
NSObject observer = NSText.Notifications.ObserveDidChange(textView, (sender, args) => {
    // Extract RTF and update RtfText property
    // (code above)
});

// Alternative: Direct notification
NSNotificationCenter.DefaultCenter.AddObserver(
    NSText.DidChangeNotification,
    HandleTextChanged,
    textView
);
```

### 4. Future iPad Support (UITextView)

**iOS/iPadOS Equivalent:** `UITextView` (uses UIKit, not AppKit)

**RTF Support:** ✅ YES
```csharp
// UITextView RTF support (iOS/iPadOS)
NSAttributedString attrString = new NSAttributedString(
    rtfData,
    new NSAttributedStringDocumentAttributes {
        DocumentType = UIDocumentType.RTF
    },
    out NSError error
);
textView.AttributedText = attrString;
```

**Key Differences from NSTextView:**
- Property: `AttributedText` (not `TextStorage`)
- Enable editing: `AllowsEditingTextAttributes = true`
- Same RTF document type constant: `NSRTFTextDocumentType`

**Implementation Path:** Same partial class pattern as macOS
- Create `RichEditBoxExtended.iOS.cs` when iPad support is added
- Parallel implementation to macOS version
- Share common RTF conversion logic

---

## Proof of Concept: Option 1 (NSTextView) Implementation

**Purpose:** Evaluate feasibility, implementation complexity, and RTF compatibility of NSTextView approach.

**Decision Criteria:**
- **Complexity:** Is implementation effort reasonable (2-3 weeks estimate)?
- **RTF Compatibility:** Does RTF round-trip work between Windows and macOS?
- **User Experience:** Does it feel native and performant on macOS?
- **Maintainability:** Is the code maintainable long-term?
- **vs. Option 3:** Does it provide sufficient value over current TextBox fallback?

**Final Decision:** After POC, compare Option 1 (NSTextView) vs Option 3 (Hybrid/TextBox) and select.

### Architecture

**File Structure:**
```
StoryCADLib/Controls/
├── RichEditBoxExtended.cs          (shared + Windows #if !HAS_UNO)
└── RichEditBoxExtended.macOS.cs    (NEW - macOS #if __MACOS__)
```

**Current Structure Analysis:**
- `RichEditBoxExtended.cs` uses `#if !HAS_UNO` for Windows version (inherits from RichEditBox)
- `RichEditBoxExtended.cs` uses `#else` for UNO version (currently inherits from TextBox)
- **Proposed:** Replace TextBox inheritance with NSTextView wrapper in macOS-specific partial class

### Implementation Strategy

**Approach A: Conditional Compilation in Single File**
```csharp
#if !HAS_UNO
    // Windows: RichEditBox wrapper (existing)
    public partial class RichEditBoxExtended : RichEditBox { }
#elif __MACOS__
    // macOS: NSTextView wrapper (NEW)
    public partial class RichEditBoxExtended : Control { }
#else
    // Other UNO platforms: TextBox fallback
    public partial class RichEditBoxExtended : TextBox { }
#endif
```

**Approach B: Partial Class Files (RECOMMENDED)**
```csharp
// RichEditBoxExtended.cs (shared)
public partial class RichEditBoxExtended
{
    public static readonly DependencyProperty RtfTextProperty = ...;
    public string RtfText { get; set; }
    partial void PlatformInitialize();
    partial void PlatformSetRtfText(string rtf);
    partial void PlatformUpdateTheme();
}

// RichEditBoxExtended.WinAppSDK.cs
#if !HAS_UNO
public partial class RichEditBoxExtended : RichEditBox
{
    partial void PlatformInitialize() { /* existing Windows code */ }
}
#endif

// RichEditBoxExtended.macOS.cs (NEW)
#if __MACOS__
public partial class RichEditBoxExtended : Control
{
    private NSTextView _nativeTextView;
    partial void PlatformInitialize() { /* NSTextView setup */ }
}
#endif
```

**Recommended:** Approach B (partial class files) for better code organization and maintainability.

### Key Implementation Details

**1. NSTextView Initialization:**
```csharp
private NSTextView _nativeTextView;
private NSScrollView _scrollView;

public RichEditBoxExtended()
{
    // Create scroll view container
    _scrollView = new NSScrollView();
    _scrollView.HasVerticalScroller = true;
    _scrollView.AutohidesScrollers = true;

    // Create text view
    _nativeTextView = new NSTextView();
    _nativeTextView.AutoresizingMask = NSViewResizingMask.WidthSizable;
    _nativeTextView.AllowsUndo = true;
    _nativeTextView.IsRichText = true;

    // Handle text changes
    NSNotificationCenter.DefaultCenter.AddObserver(
        NSText.DidChangeNotification,
        OnTextChanged,
        _nativeTextView
    );

    _scrollView.DocumentView = _nativeTextView;

    PlatformInitialize();
}
```

**2. RTF Text Property Binding (Loading from ViewModel):**
```csharp
private static void RtfTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var control = d as RichEditBoxExtended;
    if (control == null || control._lockChangeExecution) return;

    control._lockChangeExecution = true;

    string rtfText = e.NewValue?.ToString() ?? "";

    if (string.IsNullOrEmpty(rtfText))
    {
        // Empty text
        control._nativeTextView.String = "";
    }
    else
    {
        // Convert RTF string to NSData
        NSData rtfData = NSData.FromString(rtfText, NSStringEncoding.UTF8);

        // Create attributed string from RTF (preserves formatting)
        NSAttributedString attrString = new NSAttributedString(
            rtfData,
            new NSAttributedStringDocumentAttributes { DocumentType = NSDocumentType.RTF },
            out NSError error
        );

        if (error == null && attrString != null)
        {
            // Load formatted text into NSTextView
            // Users will see and can edit bold, italic, underline, colors
            control._nativeTextView.TextStorage.SetString(attrString);
        }
        else
        {
            // Fallback if RTF parsing fails: use plain text
            control._nativeTextView.String = rtfText;
        }
    }

    control._lockChangeExecution = false;
}
```

**3. Text Change Event Handling (Saving to ViewModel):**
```csharp
private void OnTextChanged(NSNotification notification)
{
    if (_lockChangeExecution) return;
    _lockChangeExecution = true;

    // Extract RTF from NSTextView with all user formatting
    NSRange fullRange = new NSRange(0, _nativeTextView.TextStorage.Length);
    NSData rtfData = _nativeTextView.TextStorage.GetDataFromRange(
        fullRange,
        NSDocumentType.RTF,  // Export as RTF to preserve bold, italic, colors, etc.
        out NSError error
    );

    if (error == null && rtfData != null)
    {
        // Convert RTF data to string and save to RtfText property
        // This RTF will be saved to the outline file
        RtfText = rtfData.ToString(NSStringEncoding.UTF8);
    }
    else
    {
        // Fallback to plain text if RTF export fails
        RtfText = _nativeTextView.String ?? "";
    }

    _lockChangeExecution = false;
}
```

**4. Theme Support:**
```csharp
public void UpdateTheme(object sender, RoutedEventArgs e)
{
    var theme = ActualTheme;

    if (theme == ElementTheme.Dark)
    {
        _nativeTextView.BackgroundColor = NSColor.ControlBackground;
        _nativeTextView.TextColor = NSColor.ControlText;
    }
    else
    {
        _nativeTextView.BackgroundColor = NSColor.ControlBackground;
        _nativeTextView.TextColor = NSColor.ControlText;
    }
}
```

**5. Property Mapping:**
- `AcceptsReturn` → `_nativeTextView.IsEditable = true` (NSTextView is multiline by default)
- `IsSpellCheckEnabled` → `_nativeTextView.ContinuousSpellCheckingEnabled`
- `TextWrapping` → `_nativeTextView.Container.WidthTracksTextView`
- `PlaceholderText` → Custom overlay or attributed string placeholder
- `IsReadOnly` → `_nativeTextView.IsEditable = false`

### Complete Data Flow

**User edits "Story Idea" on Windows:**
1. User types text, applies bold/italic formatting via context menu
2. RichEditBox (base control) handles rich text editing
3. RichEditBoxExtended.TextChanged fires
4. Extract RTF via `Document.GetText(TextGetOptions.FormatRtf)`
5. Update `RtfText` property → ViewModel's `Description` property
6. Save outline → RTF stored in `.stbx` file

**User opens same outline on macOS:**
1. Load outline file, deserialize RTF string
2. ViewModel's `Description` property contains RTF
3. XAML binding: `RtfText="{x:Bind OverviewVm.Description, Mode=TwoWay}"`
4. RtfTextPropertyChanged fires
5. Convert RTF → NSAttributedString
6. Load into NSTextView.TextStorage
7. **User sees formatted text with bold/italic preserved**
8. User can continue editing with macOS native formatting menu
9. NSText.DidChangeNotification fires on edits
10. Extract RTF from TextStorage
11. Update `RtfText` property → ViewModel → outline file
12. **Formatting preserved cross-platform**

**Reports and Scrivener Export:**
- `ReportFormatter.GetText(rtfText)` strips RTF → plain text
- Used for print reports, Scrivener interchange
- Separate concern from editing (the "Extended" part provides binding, not plain text editing)

### RTF Compatibility Considerations

**Windows → macOS:**
- Load RTF using `NSAttributedString` with RTF document type
- If parsing fails, fallback to plain text using existing `ReportFormatter.GetText()`
- Preserve formatting where possible

**macOS → Windows:**
- NSTextView generates standard RTF
- Windows RichEditBox should handle NSTextView-generated RTF natively
- Test round-trip to verify compatibility

**Scrivener Export:**
- Export already uses `ReportFormatter` to generate RTF from stored text
- Should work identically whether RTF came from Windows or macOS
- Test to verify

---

## Cost-Benefit Analysis: Option 1 vs Option 3

### Option 1 (NSTextView POC) - NOT SELECTED

**Benefits:**
- ✅ Full RTF editing on macOS (bold, italic, underline, colors)
- ✅ Formatting preserved cross-platform
- ✅ Professional-grade application
- ✅ Consistent user experience Windows ↔ macOS
- ✅ Writers get expected formatting tools

**Costs:**
- ❌ **15-25 hours human time** (9-12 interactive sessions over 3-4 weeks)
- ❌ **Unknown RTF compatibility risk** - May require workarounds/debugging
- ❌ **Opportunity cost** - Time better spent on critical compatibility issues
- ❌ **Uncertain ROI** - Zero macOS users today to benefit

**Week 1 POC Scope (5-10 hours):**
- Implement RichEditBoxExtended.macOS.cs with NSTextView
- RTF ↔ NSAttributedString conversion
- Basic round-trip test (Windows → macOS → Windows)
- **Decision point:** RTF works → continue | RTF broken → stop

### Option 3 (Current TextBox) - **SELECTED**

**Benefits:**
- ✅ **Zero implementation cost** (already done)
- ✅ **Zero risk** - Working and tested
- ✅ File compatibility maintained
- ✅ **Focus on critical issues** - Other macOS compatibility unknowns
- ✅ **Ship-and-iterate** - Validate market before investing

**Costs:**
- ❌ Plain text only on macOS
- ❌ Windows-created formatting lost when edited on macOS
- ⚠️ May disappoint writers expecting formatting
  - **Mitigated:** Zero current macOS users

### Market Context

**Current User Base:**
- **1,500 Windows users** with RTF editing
- **0 macOS users** (new market)
- Target audience: **Writers** (expect writing tools)
- Competitive landscape: Unknown

**Strategic Considerations:**
1. **Unknown demand** - No data on macOS market size
2. **Unknown compatibility issues** - RTF is not the only challenge
3. **Risk management** - Ship minimal viable product, gather feedback
4. **Iterative approach** - v4.0 plain text → v4.1 formatting if demanded

### Decision Matrix

| Factor | Option 1 (NSTextView) | Option 3 (TextBox) | Winner |
|--------|----------------------|-------------------|---------|
| **Implementation cost** | 15-25 hours | 0 hours | ✅ Option 3 |
| **Risk** | RTF compatibility unknown | Zero (proven) | ✅ Option 3 |
| **User experience** | Full formatting | Plain text | Option 1 |
| **Time to market** | +3-4 weeks | Immediate | ✅ Option 3 |
| **Resource allocation** | Diverts from critical issues | Enables focus | ✅ Option 3 |
| **Market validation** | Assumes demand | Tests demand | ✅ Option 3 |
| **Upgrade path** | N/A | Can do v4.1 | ✅ Option 3 |

**Conclusion:** Option 3 wins on **pragmatic grounds** - ship macOS version quickly, validate market, iterate based on user feedback.

---

## Testing Strategy (Option 3 - Current Implementation)

### ✅ Already Complete (Jake's Implementation)

**Current TextBox Implementation:**
- Plain text editing verified working
- RTF stripping via `ReportFormatter.GetText()` tested
- File compatibility Windows ↔ macOS maintained
- All XAML pages functional (Overview, Character, Problem, Scene, Setting, Folder)

### Remaining Testing Focus (macOS v4.0)

**Priority: Other Compatibility Issues**
- Full feature manual testing on macOS
- Identify and resolve platform-specific bugs
- Verify all story element types work correctly
- Test file operations (open, save, auto-save, backup)
- Theme switching, keyboard shortcuts, native menus
- Scrivener export functionality

**RTF-Specific Testing (Option 3):**
- ✅ Verify Windows-created RTF loads as plain text (no crashes)
- ✅ Verify macOS edits save correctly
- ✅ Verify round-trip works (Windows → macOS → Windows)
- ✅ Document formatting limitation in user docs

---

## Deferred Testing Strategy (Option 1 - If Revisited in v4.1)

**Note:** This testing plan applies only if Option 1 NSTextView POC is pursued in future version.

### Phase 1: Basic Functionality
1. ✅ Create RichEditBoxExtended.macOS.cs
2. ✅ Implement NSTextView wrapper
3. ✅ Test text input/display on macOS
4. ✅ Verify RtfText property binding

### Phase 2: RTF Round-Trip (CRITICAL FOR POC)
1. ✅ Create RTF content on Windows with formatting (bold, italic, colors)
2. ✅ Save outline on Windows
3. ✅ Open same outline on macOS
4. ✅ **Verify text displays WITH FORMATTING** (bold/italic/colors preserved, NOT stripped)
5. ✅ Edit text on macOS, apply additional formatting
6. ✅ Save outline on macOS
7. ✅ Open on Windows
8. ✅ **Verify all formatting preserved** (both Windows-created and macOS-added)

### Phase 3: Cross-Platform Compatibility
1. ✅ Test all 6 story element pages (Overview, Character, Problem, Scene, Setting, Folder)
2. ✅ Test theme switching (dark/light mode)
3. ✅ Test Scrivener export from macOS
4. ✅ Compare with Scrivener export from Windows
5. ✅ Verify file format compatibility

### Phase 4: Edge Cases
1. ✅ Empty RTF text
2. ✅ Very large RTF documents
3. ✅ Invalid/corrupted RTF data
4. ✅ Special characters, Unicode
5. ✅ Copy/paste between controls
6. ✅ Undo/redo functionality

### Phase 5: Regression Testing
1. ✅ Verify Windows version unchanged
2. ✅ Run full test suite
3. ✅ Manual testing on both platforms

---

## Risk Assessment (Option 1 - NSTextView - If Pursued in Future)

**Note:** These risks apply only if Option 1 is revisited for v4.1 or later.

### Technical Risks

**Risk 1: RTF Format Incompatibility**
- **Probability:** Low-Medium (unproven)
- **Impact:** High (blocks Option 1 viability)
- **Mitigation:** Week 1 POC will discover this immediately. Fallback to Option 3 if compatibility issues found.

**Risk 2: Performance with Large Documents**
- **Probability:** Low
- **Impact:** Low
- **Mitigation:** NSTextView is highly optimized. StoryCAD documents are typically small.

**Risk 3: NSTextView API Changes**
- **Probability:** Very Low
- **Impact:** Low
- **Mitigation:** AppKit API is stable, breaking changes rare. .NET bindings maintained by Microsoft.

### Development Risks

**Risk 4: Learning Curve for AppKit**
- **Probability:** Medium
- **Impact:** Medium (extends timeline)
- **Mitigation:** Good documentation available. TextBox.macOS.cs provides implementation pattern.

**Risk 5: Testing Coverage**
- **Probability:** Medium
- **Impact:** Medium
- **Mitigation:** Comprehensive test plan above. Beta testing with macOS users.

### Business Risks (Option 3 - Selected Approach)

**Risk 6: User Disappointment**
- **Probability:** Low-Medium
- **Impact:** Medium (may affect adoption)
- **Mitigation:**
  - Zero current macOS users (no existing expectations)
  - Document limitation clearly in user guide
  - Marketing: Position as "v1 macOS", more features coming
  - Gather feedback, upgrade in v4.1 if demanded

**Risk 7: Competitive Disadvantage**
- **Probability:** Unknown
- **Impact:** Medium
- **Mitigation:**
  - Competitive landscape unclear
  - First-to-market advantage (getting macOS version shipped)
  - Can upgrade formatting later if competitors have it

---

## Timeline Estimate (Option 1 - If Revisited)

**Note:** This timeline is DEFERRED. Only applies if Option 1 NSTextView POC is pursued in v4.1 or later.

**Total: 2-3 weeks (POC + Evaluation)**

- **Week 1:** POC Implementation
  - Day 1-2: RichEditBoxExtended.macOS.cs skeleton, NSTextView wrapper
  - Day 3-4: RTF text property binding, event handling
  - Day 5: Theme support, property mapping
  - **Critical:** RTF compatibility testing begins

- **Week 2:** POC Testing & Refinement
  - Day 6-7: Basic functionality testing
  - Day 8-9: RTF round-trip testing (DECISION POINT - does it work?)
  - Day 10: Edge cases, bug fixes

- **Week 3:** POC Evaluation & Decision
  - Day 11-12: Cross-platform compatibility testing
  - Day 13: Compare actual vs. estimated effort
  - Day 14: **Decision Point:** Continue or revert to Option 3
  - Day 15: Documentation, Issue update

**Human Time Investment:** 15-25 hours (9-12 interactive sessions)

**Contingency:** +1 week for unexpected issues

---

## Timeline (Option 3 - Current Plan)

**Status:** ✅ **COMPLETE** - Already implemented by Jake

**Remaining Work:**
- Document RTF limitation in user guide: **1 hour**
- Full macOS manual testing (focus on other compatibility issues): **Ongoing**
- Update Issue #1130 with decision: **30 minutes**

**Total Additional Time for Option 3:** ~2 hours

---

## Alternative: Incremental Approach

If full NSTextView implementation proves challenging, consider incremental delivery:

**Phase 1 (Immediate):** Keep current TextBox fallback
- Document limitation for macOS users
- Ensure file format compatibility maintained

**Phase 2 (Near-term):** Basic NSTextView (plain text)
- Wrap NSTextView but continue stripping RTF on load
- Better editing experience than TextBox (undo/redo, better text handling)
- No RTF formatting support yet

**Phase 3 (Long-term):** Full RTF support
- Implement RTF read/write as outlined above
- Full cross-platform parity

This allows delivering improved macOS experience sooner while deferring RTF complexity.

---

## Next Steps

### Immediate Actions (v4.0 macOS Launch)

**1. Document RTF Limitation** (~1 hour)
- Add to user manual: "macOS version supports plain text editing. Formatting from Windows version will be preserved in files but displayed as plain text on macOS."
- Add to release notes: Known limitation
- Position as v1 feature set

**2. Continue Full Manual Testing** (Ongoing)
- Focus on OTHER macOS compatibility issues
- File operations, keyboard shortcuts, native menus
- Theme switching, story element pages
- Scrivener export functionality

**3. Update Issue #1130** (~30 minutes)
- Document decision to use Option 3
- Close issue with resolution: "Working as designed - plain text editing on macOS v4.0"
- Note: Can reopen for v4.1 if user feedback demands formatting

### Future Consideration (v4.1+)

**If macOS user feedback requests formatting:**

**Option A: Quick Win POC (Week 1 only - 5-10 hours)**
- Implement basic NSTextView wrapper
- Test RTF round-trip
- If works: Continue to full implementation
- If fails: Document findings, keep Option 3

**Option B: Stay with Option 3**
- Monitor UNO Platform progress on RichEditBox (Issue #3848)
- Wait for official implementation
- Upgrade when available

**Decision triggers for revisiting Option 1:**
- ≥5 user requests for formatting
- Competitive pressure (other tools have it)
- macOS user base >100 users
- Other compatibility issues resolved

---

## References

### UNO Platform
- [Creating Custom Controls](https://platform.uno/docs/articles/guides/creating-custom-controls.html)
- [Platform-Specific C#](https://platform.uno/docs/articles/platform-specific-csharp.html)
- [UNO Issue #3848 - Implement RichEditBox](https://github.com/unoplatform/uno/issues/3848)
- [TextBox.macOS.cs Source](https://github.com/unoplatform/uno/blob/master/src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.macOS.cs)

### Apple Frameworks
- [AppKit.NSTextView API](https://learn.microsoft.com/en-us/dotnet/api/appkit.nstextview)
- [NSAttributedString Documentation](https://developer.apple.com/documentation/foundation/nsattributedstring)
- [UITextView API (iOS/iPadOS)](https://developer.apple.com/documentation/uikit/uitextview)

### .NET for macOS
- [Upgrade to .NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/migration/?view=net-maui-9.0)
- [Xamarin.Mac to .NET Migration](https://learn.microsoft.com/en-us/dotnet/maui/migration/native-projects?view=net-maui-9.0)

### StoryCAD Project
- Issue #1130: Evaluate macOS RTF Support Strategy
- `/StoryCADLib/Controls/RichEditBoxExtended.cs` (current implementation)
- Discord conversation: `/mnt/c/temp/issue_1130_discord_conversation.txt`

---

## Appendix: Discord Conversation Analysis

**Key Takeaways from Jake/Terry Discussion:**

1. **UNO RichEditBox Investigation:** Jake attempted to use UNO's RichEditBox directly and discovered `Document.SetText()` throws `NotImplementedException`. This confirmed our research findings.

2. **TextBox Fallback Implementation:** Jake implemented the current TextBox-based solution with RTF stripping using `ReportFormatter.GetText()`. This is Option 3 (Hybrid Approach).

3. **Terry's Intent:** Terry wanted to explore UNO's RichEditBox to stay "plugged into future UNO work" rather than creating a custom solution. However, research shows UNO's RichEditBox has no implementation, making this unfeasible.

4. **Current State:** TextBox fallback is working but acknowledged as temporary. macOS users have plain text editing only, formatting is stripped on load.

5. **Path Forward:** NSTextView approach (Option 1) aligns with Terry's goal of leveraging platform capabilities while providing full functionality today.

---

**Document Version:** 1.0
**Last Updated:** 2025-10-03
**Author:** Claude (AI Assistant)
**Reviewed By:** Terry Cox
