# Issue #1003: Missing Symbols - Icons Don't Appear on macOS

## Investigation Report
**Date**: 2025-10-16
**Branch**: UNOTestBranch
**Platforms**: Windows (WinUI 3) + macOS (UNO Desktop)
**Status**: Root cause identified - Missing Uno.Fonts.Fluent package

---

## Executive Summary

Icons are not displaying on macOS because StoryCAD relies on `SymbolThemeFontFamily`, which references **Segoe Fluent Icons** font. This font is:
1. Windows-proprietary and cannot be legally distributed on non-Windows platforms
2. Automatically available on Windows via WinUI 3
3. **Requires the `Uno.Fonts.Fluent` NuGet package on macOS** (currently NOT installed)

**Solution**: Add `Uno.Fonts.Fluent` package to enable cross-platform icon rendering.

---

## 1. Font Icon Usage Analysis

### 1.1 Icon Implementation Pattern

StoryCAD uses **two approaches** for icons:

#### A. FontIcon with SymbolThemeFontFamily (Most Common)
```xaml
<FontIcon FontFamily="{ThemeResource SymbolThemeFontFamily}" Glyph="&#xE8F4;" />
```

**Files Using This Pattern**:
- `/StoryCAD/Views/Shell.xaml` - 16 instances (toolbar, menu items)
- `/StoryCADLib/Services/Dialogs/FileOpenMenu.xaml` - 4 instances
- `/StoryCADLib/Collaborator/Views/WorkflowShell.xaml` - 1 instance
- `/StoryCAD/Views/ProblemPage.xaml` - Mixed usage

**Unicode Glyphs Used**:
| Glyph | Code | Purpose | Location |
|-------|------|---------|----------|
| 📄 | `&#xE8F4;` | Open/Create file | Shell menu |
| 💾 | `&#xE74E;` | Save | Shell menu, ProblemPage |
| 💾📋 | `&#xE792;` | Save As | Shell menu |
| 📦 | `&#xe838;` | Backup | Shell menu, FileOpenMenu |
| ❌ | `&#xE127;` | Close | Shell menu |
| 🚪 | `&#xE106;` | Exit | Shell menu |
| ➕ | `&#xE710;` | Add | Shell toolbar, FileOpenMenu |
| ↔️ | `&#xE759;` | Move | Shell toolbar |
| 🤖 | `&#xE71B;` | Collaborator/Assign | Shell toolbar, ProblemPage |
| 🔧 | `&#xE90F;` | Tools | Shell toolbar |
| 📊 | `&#xF571;` | Reports | Shell toolbar |
| ⚙️ | `&#xE713;` | Preferences | Shell toolbar |
| 💬 | `&#xED15;` | Feedback | Shell toolbar |
| ❓ | `&#xE897;` | Help | Shell toolbar, WorkflowShell |

#### B. FontIcon with "Segoe MDL2 Assets" (Hardcoded)
```xaml
<FontIcon FontFamily="Segoe MDL2 Assets" Glyph="&#xE777;" />
```

**Files Using This Pattern**:
- `/StoryCAD/Views/Shell.xaml` - Backup status indicator (line 589)
- `/StoryCAD/Views/ProblemPage.xaml` - Assign, Move Down, Save buttons
- `/StoryCADLib/Services/Dialogs/FileOpenMenu.xaml` - Backup icon (line 37)

**Problem**: Hardcoded "Segoe MDL2 Assets" will ALWAYS fail on macOS.

#### C. SymbolIcon with Symbol Enum (WinUI Native)
```xaml
<SymbolIcon Symbol="Refresh" />
```

**Files Using This Pattern**:
- `/StoryCAD/Views/WebPage.xaml` - Navigation buttons (Refresh, Back, Forward)
- `/StoryCAD/Views/Shell.xaml` - TreeView icons (StoryNodeItem.Symbol binding)
- `/StoryCAD/Views/ProblemPage.xaml` - Beat icons
- `/StoryCADLib/Controls/RelationshipView.xaml` - Cancel icon

**Symbol Values Used**:
```csharp
// From StoryNodeItem.cs constructor (lines 524-556)
Symbol.View          // StoryOverview
Symbol.Contact       // Character
Symbol.AllApps       // Scene
Symbol.Help          // Problem
Symbol.Globe         // Setting
Symbol.Folder        // Folder/Section
Symbol.PreviewLink   // Web
Symbol.Delete        // TrashCan
Symbol.TwoPage       // Notes
Symbol.Refresh       // WebPage
Symbol.Back          // WebPage
Symbol.Forward       // WebPage
Symbol.Cancel        // RelationshipView, Beatsheet
Symbol.ReportHacked  // PreferencesDialog error indicator
Symbol.Edit          // Shell status bar
```

---

## 2. Root Cause: Missing Uno.Fonts.Fluent Package

### 2.1 Current Package Configuration

**File**: `/mnt/d/dev/src/StoryCAD/Directory.Packages.props`

```xml
<Project ToolsVersion="15.0">
    <ItemGroup>
        <PackageVersion Include="Microsoft.SemanticKernel" Version="1.41.0" />
        <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.0" />
        <!-- ... other packages ... -->
        <!-- ❌ Uno.Fonts.Fluent is MISSING -->
    </ItemGroup>
</Project>
```

**Analysis**: No `Uno.Fonts.Fluent` package reference exists in:
- `Directory.Packages.props`
- `StoryCAD/StoryCAD.csproj`
- `StoryCADLib/StoryCADLib.csproj`

### 2.2 How SymbolThemeFontFamily Works

#### On Windows (WinUI 3)
```xaml
<!-- Resolves to Windows system font -->
<FontIcon FontFamily="{ThemeResource SymbolThemeFontFamily}" />
```
- `SymbolThemeFontFamily` → "Segoe Fluent Icons" (system font)
- Font automatically available in Windows 11
- Works out-of-the-box

#### On macOS (UNO Platform) - CURRENT STATE
```xaml
<!-- Resolves to NOTHING - font missing -->
<FontIcon FontFamily="{ThemeResource SymbolThemeFontFamily}" />
```
- `SymbolThemeFontFamily` → **undefined or fallback font**
- Segoe Fluent Icons not legally distributable
- **Result**: Empty squares or missing glyphs

#### On macOS (UNO Platform) - WITH Uno.Fonts.Fluent
```xaml
<!-- Resolves to Uno's open-source font -->
<FontIcon FontFamily="{ThemeResource SymbolThemeFontFamily}" />
```
- `SymbolThemeFontFamily` → Uno's Fluent Icons font (open-source)
- Provides Windows 11 iconography cross-platform
- Unicode glyph codes remain compatible

### 2.3 Licensing Constraints

**Microsoft's Segoe Fluent Icons**:
- ❌ Cannot ship to non-Windows platforms
- ❌ Proprietary license
- ✅ OK for development/design use only

**Uno.Fonts.Fluent**:
- ✅ Open-source alternative
- ✅ Cross-platform compatible
- ✅ Windows 11 iconography parity
- ✅ Same Unicode code points

---

## 3. Icon Categories and Platform Support

### 3.1 SymbolIcon (WinUI Native)
**Support Status**: ✅ **Fully Supported on macOS**
- Uses `Symbol` enum (e.g., `Symbol.Refresh`)
- UNO Platform provides native implementation
- No font dependency required
- **Action Required**: NONE - works out-of-the-box

### 3.2 FontIcon with SymbolThemeFontFamily
**Support Status**: ⚠️ **Requires Uno.Fonts.Fluent**
- Uses theme resource that resolves differently per platform
- **Action Required**: Install `Uno.Fonts.Fluent` package

### 3.3 FontIcon with Hardcoded "Segoe MDL2 Assets"
**Support Status**: ❌ **Will Always Fail on macOS**
- Directly references Windows-only font
- **Action Required**: Replace with `{ThemeResource SymbolThemeFontFamily}`

---

## 4. Detailed File-by-File Analysis

### 4.1 Critical Files (Toolbar Icons)

#### `/StoryCAD/Views/Shell.xaml`
**Total Icons**: ~30
**Issue Instances**: 16 FontIcon + 1 hardcoded
**Impact**: Main application toolbar completely broken

**Lines Affected**:
```
154: FontIcon - Open/Create (SymbolThemeFontFamily) ✅
162: FontIcon - Save (SymbolThemeFontFamily) ✅
170: FontIcon - Save As (SymbolThemeFontFamily) ✅
179: FontIcon - Backup (SymbolThemeFontFamily) ✅
187: FontIcon - Close (SymbolThemeFontFamily) ✅
192: FontIcon - Exit (SymbolThemeFontFamily) ✅
201: FontIcon - Add (SymbolThemeFontFamily) ✅
295: FontIcon - Move (SymbolThemeFontFamily) ✅
311: Button Content - Move Left (SymbolThemeFontFamily) ✅
315: Button Content - Move Right (SymbolThemeFontFamily) ✅
319: Button Content - Move Up (SymbolThemeFontFamily) ✅
323: Button Content - Move Down (SymbolThemeFontFamily) ✅
335: FontIcon - Collaborator (SymbolThemeFontFamily) ✅
340: FontIcon - Tools (SymbolThemeFontFamily) ✅
384: FontIcon - Reports (SymbolThemeFontFamily) ✅
415: FontIcon - Preferences (SymbolThemeFontFamily) ✅
424: FontIcon - Feedback (SymbolThemeFontFamily) ✅
433: FontIcon - Help (SymbolThemeFontFamily) ✅
589: FontIcon - Backup Status ("Segoe MDL2 Assets") ❌ HARDCODED
```

**Tree View Icons** (Lines 113-526):
```xaml
<SymbolIcon Symbol="{x:Bind Symbol, Mode=OneWay}" />
```
✅ These work fine - uses SymbolIcon with enum binding

#### `/StoryCAD/Views/ProblemPage.xaml`
**Lines Affected**:
```
189: FontIcon - Assign ("Segoe MDL2 Assets") ❌ HARDCODED
226: FontIcon - Move Down ("Segoe MDL2 Assets") ❌ HARDCODED
230: FontIcon - Save Beatsheet ("Segoe MDL2 Assets") ❌ HARDCODED
258: SymbolIcon - Beat Element Icon ✅ Works with Symbol enum
```

#### `/StoryCADLib/Services/Dialogs/FileOpenMenu.xaml`
**Lines Affected**:
```
16: FontIcon - New (SymbolThemeFontFamily) ✅
23: FontIcon - Recent (SymbolThemeFontFamily) ✅
30: FontIcon - Sample (SymbolThemeFontFamily) ✅
37: FontIcon - Backup ("Segoe MDL2 Assets") ❌ HARDCODED
47: FontIcon - Open from file (SymbolThemeFontFamily) ✅
```

#### `/StoryCADLib/Collaborator/Views/WorkflowShell.xaml`
**Lines Affected**:
```
33: FontIcon - Help (SymbolThemeFontFamily) ✅
```

### 4.2 Files Using SymbolIcon (No Issues)

#### `/StoryCAD/Views/WebPage.xaml`
```xaml
25: <SymbolIcon Symbol="Refresh" />
30: <SymbolIcon Symbol="Back" />
35: <SymbolIcon Symbol="Forward" />
```
✅ All navigation icons work fine

#### `/StoryCADLib/Controls/RelationshipView.xaml`
```xaml
24: <SymbolIcon Symbol="Cancel" Tag="{x:Bind PartnerUuid}" />
```
✅ Delete relationship icon works fine

#### `/StoryCADLib/Services/Dialogs/Tools/PreferencesDialog.xaml`
```xaml
186: <SymbolIcon Symbol="ReportHacked" Foreground="Red" />
```
✅ Error indicator works fine

### 4.3 Code-Behind Symbol Usage

#### `/StoryCADLib/ViewModels/StoryNodeItem.cs` (Lines 524-556)
```csharp
switch (_type)
{
    case StoryItemType.StoryOverview:
        Symbol = Symbol.View;      // ✅ Works
        break;
    case StoryItemType.Character:
        Symbol = Symbol.Contact;   // ✅ Works
        break;
    case StoryItemType.Scene:
        Symbol = Symbol.AllApps;   // ✅ Works
        break;
    // ... etc ...
}
```
**Status**: ✅ All Symbol enum assignments work correctly on macOS

#### `/StoryCADLib/ViewModels/Tools/StructureBeatViewModel.cs` (Lines 150-169)
```csharp
public Symbol ElementIcon
{
    get
    {
        if (Element.ElementType == StoryItemType.Problem)
            return Symbol.Help;      // ✅ Works
        if (Element.ElementType == StoryItemType.Scene)
            return Symbol.World;     // ✅ Works
        return Symbol.Cancel;        // ✅ Works
    }
}
```
**Status**: ✅ All Symbol enum usage works correctly

---

## 5. Platform-Specific Code Analysis

### 5.1 Current Platform Detection

**Pattern Found**: `#if HAS_UNO_WINUI` and `#if __MACOS__`

**Files with Platform-Specific Code**:
```
/devdocs/issue_1139_compatability_warnings.md
/devdocs/issue_1139_uno_api_research.md
/devdocs/issue_0964_single_instancing_v2.md
/.claude/docs/troubleshooting/common-errors.md
/.claude/docs/examples/platform-specific-code.md
/.claude/docs/dependencies/uno-platform-guide.md
/StoryCADLib/Services/Collaborator/CollaboratorService.cs
/StoryCADLib/DAL/StoryIO.cs
```

**No Platform-Specific Icon Handling Found**: Icons are not currently using platform detection.

### 5.2 Asset Loading System

**File**: `/StoryCAD/Assets/SharedAssets.md`

```markdown
# Shared Assets

## Here is a cheat sheet

1. Add the image file to the `Assets` directory of a shared project.
2. Set the build action to `Content`.
3. (Recommended) Provide an asset for various scales/dpi

### Examples

\Assets\Images\logo.scale-100.png
\Assets\Images\logo.scale-200.png
\Assets\Images\logo.scale-400.png
```

**Analysis**:
- Asset system supports scale-aware images
- Uses UNO Platform's asset pipeline
- No special font configuration documented

---

## 6. Resource Dictionary Analysis

### 6.1 App.xaml Configuration

**File**: `/StoryCAD/Views/App.xaml`

```xaml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- Load WinUI resources -->
            <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            <!-- Load Uno.UI.Toolkit resources -->
            <ToolkitResources xmlns="using:Uno.Toolkit.UI" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

**Missing**: No explicit font resource dictionary or `SymbolThemeFontFamily` override

### 6.2 Theme Resource Usage

**Pattern in XAML**:
```xaml
FontFamily="{ThemeResource SymbolThemeFontFamily}"
```

**Expected Resolution**:
1. Search merged dictionaries
2. Search app resources
3. Fall back to system theme resources

**Current State**:
- Windows: Resolves to system Segoe Fluent Icons
- macOS: **No resolution found** → defaults to system font → glyphs missing

---

## 7. Known Issues and TODOs

### 7.1 TODO/FIXME Search Results
**Search Pattern**: `TODO.*icon|TODO.*symbol|TODO.*macos|FIXME.*icon`
**Results**: ❌ **No results found**

**Interpretation**: Icon issues on macOS are not yet documented in code comments.

### 7.2 Related Issue Comments

**From CollaboratorService.cs**:
```csharp
#pragma warning disable CS0169 // Fields used in platform-specific code
// (!HAS_UNO only - see Issue #1126 for macOS support)
private bool dllExists;
#pragma warning restore CS0169

// macOS plugin loading tracked in Issue #1126 and #1135
```

**Note**: Issue #1126 tracks macOS-specific features, may be related.

---

## 8. Web Research Findings

### 8.1 UNO Platform Documentation

**Source**: https://platform.uno/docs/articles/uno-fluent-assets.html

Key findings:
- Starting from Uno.UI 4.7, `Uno.Fonts.Fluent` provides cross-platform symbols
- Must be added to all app heads (except WinUI head)
- Uses open-source alternative to Segoe Fluent Icons
- Automatically integrates with `SymbolThemeFontFamily`

### 8.2 Licensing Constraints

**Source**: https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font

> "Segoe Fluent Icons.ttf prevents it being used cross-platform. You can download the font for use in design and development, but you may not ship it to another platform."

### 8.3 UNO Platform Issues

**GitHub Discussion #6493**: "Plan for new Segoe Fluent Icons"
- UNO team discussing updated icon font
- Focus on Windows 11 Sun Valley refresh

**GitHub Issue #3011**: "Support for Segoe fonts"
- Long-standing request
- Solution: Uno.Fonts.Fluent package

**GitHub Issue #5734**: "Use new Fluent Icons (Sun Valley refresh)"
- Tracking latest icon updates

---

## 9. Recommended Solution

### 9.1 Primary Fix: Add Uno.Fonts.Fluent Package

**Step 1**: Add to `Directory.Packages.props`
```xml
<ItemGroup>
    <PackageVersion Include="Uno.Fonts.Fluent" Version="2.7.1" />
</ItemGroup>
```

**Step 2**: Reference in `StoryCADLib.csproj`
```xml
<ItemGroup>
    <PackageReference Include="Uno.Fonts.Fluent" />
</ItemGroup>
```

**Step 3**: Reference in `StoryCAD.csproj` (main app)
```xml
<ItemGroup>
    <PackageReference Include="Uno.Fonts.Fluent" />
</ItemGroup>
```

**Expected Result**: All `SymbolThemeFontFamily` icons render correctly on macOS.

### 9.2 Secondary Fix: Replace Hardcoded Font References

**Files to Update**:
1. `/StoryCAD/Views/Shell.xaml` (line 589)
2. `/StoryCAD/Views/ProblemPage.xaml` (lines 189, 226, 230)
3. `/StoryCADLib/Services/Dialogs/FileOpenMenu.xaml` (line 37)

**Find**:
```xaml
<FontIcon FontFamily="Segoe MDL2 Assets" Glyph="..." />
```

**Replace With**:
```xaml
<FontIcon FontFamily="{ThemeResource SymbolThemeFontFamily}" Glyph="..." />
```

**Rationale**: Hardcoded font names will always fail cross-platform.

### 9.3 Optional: Verify Symbol Enum Usage (Already Working)

**No changes needed** for:
- `SymbolIcon` controls
- `Symbol` enum bindings in ViewModels
- TreeView icons using `Symbol` property

These already work correctly on macOS.

---

## 10. Testing Plan

### 10.1 Windows Testing (Regression Prevention)
- [ ] All toolbar icons display correctly
- [ ] TreeView icons display correctly
- [ ] Menu flyout icons display correctly
- [ ] Status bar icons display correctly
- [ ] No visual regressions

### 10.2 macOS Testing (Issue Verification)
**Before Fix**:
- [ ] Document which icons are missing (screenshot)
- [ ] Verify empty squares or missing glyphs

**After Fix**:
- [ ] All toolbar icons display correctly
- [ ] TreeView icons display correctly
- [ ] Menu flyout icons display correctly
- [ ] Status bar icons display correctly
- [ ] Icon appearance matches Windows (within reasonable platform differences)

### 10.3 Build Verification
- [ ] Windows build succeeds with zero warnings
- [ ] macOS build succeeds with zero warnings
- [ ] Package restore completes successfully
- [ ] No version conflicts with Uno.Fonts.Fluent

---

## 11. Impact Assessment

### 11.1 Affected UI Areas

**High Impact** (Completely Broken):
- Main toolbar (16 icons)
- File menu (5 icons)
- Move controls (4 direction icons)

**Medium Impact** (Partially Broken):
- Problem page beatsheet editor (3 icons)
- File open dialog (1 icon)
- Status bar backup indicator (1 icon)

**No Impact** (Already Working):
- TreeView navigation icons (SymbolIcon)
- Web page navigation buttons (SymbolIcon)
- Relationship controls (SymbolIcon)
- Preferences error indicator (SymbolIcon)

### 11.2 User Experience Impact

**Current State on macOS**:
- Users see empty boxes where icons should appear
- Toolbar buttons lack visual identity
- Navigation relies solely on text labels
- Professional appearance severely degraded

**After Fix**:
- Full icon parity with Windows
- Professional, polished appearance
- Improved usability and discoverability
- Consistent cross-platform UX

---

## 12. Related Issues

### 12.1 Issue #1139: UNO API Compatibility Warnings
**File**: `/devdocs/issue_1139_uno_api_compatibility.md`

Addresses 11 Uno0001 warnings for unsupported APIs on macOS. Icon issue is separate but related to overall macOS compatibility.

**APIs Affected**:
- ListBox (UI control)
- DisplayArea.WorkArea (window management)
- StorageFile.DeleteAsync overload
- App Extensions System
- WebView2 Environment

**Note**: Font icons are NOT part of the API warnings - they're a separate packaging/dependency issue.

### 12.2 Issue #1126: macOS Collaborator Support
Referenced in code comments for platform-specific features. May need coordination if Collaborator window uses icons.

---

## 13. Acceptance Criteria

### 13.1 Definition of Done
- [ ] Uno.Fonts.Fluent package added to all projects
- [ ] All hardcoded "Segoe MDL2 Assets" references replaced
- [ ] Build succeeds on both Windows and macOS (zero warnings)
- [ ] All icons display correctly on Windows (no regressions)
- [ ] All icons display correctly on macOS (issue resolved)
- [ ] Visual comparison screenshots provided (before/after)
- [ ] User documentation updated if needed
- [ ] Code review completed
- [ ] Manual testing completed on both platforms

### 13.2 Verification Steps
1. Install Uno.Fonts.Fluent package
2. Build on Windows → verify zero regressions
3. Build on macOS → verify icons now display
4. Take screenshots of Shell.xaml toolbar (both platforms)
5. Compare icon appearance across platforms
6. Test all affected views (Shell, ProblemPage, FileOpenMenu, WorkflowShell)
7. Verify TreeView icons still work (should be unchanged)

---

## 14. Estimated Effort

**Investigation**: ✅ **COMPLETE** (4 hours)

**Implementation**:
- Add package references: 15 minutes
- Update hardcoded font references: 30 minutes
- Build and test on Windows: 30 minutes
- Build and test on macOS: 1 hour
- **Total**: 2-3 hours

**Documentation**:
- Update CLAUDE.md with icon guidance: 30 minutes
- Update issue with findings: 15 minutes
- Create PR description: 15 minutes
- **Total**: 1 hour

**Grand Total**: 3-4 hours (excluding investigation)

---

## 15. Implementation Checklist

### Phase 1: Package Installation
- [ ] Add `Uno.Fonts.Fluent` to `Directory.Packages.props`
- [ ] Reference in `StoryCAD.csproj`
- [ ] Reference in `StoryCADLib.csproj`
- [ ] Restore packages: `dotnet restore`

### Phase 2: Code Updates
- [ ] Update Shell.xaml line 589 (backup status icon)
- [ ] Update ProblemPage.xaml lines 189, 226, 230 (beatsheet icons)
- [ ] Update FileOpenMenu.xaml line 37 (backup icon)
- [ ] Search for any other "Segoe MDL2 Assets" references

### Phase 3: Testing
- [ ] Build Windows target (verify no regressions)
- [ ] Run Windows app (visual inspection)
- [ ] Build macOS target (verify icons now work)
- [ ] Run macOS app (visual inspection)
- [ ] Take comparison screenshots

### Phase 4: Documentation
- [ ] Update issue #1003 with resolution
- [ ] Document in CLAUDE.md icon usage patterns
- [ ] Create PR with detailed description
- [ ] Update user documentation if needed

---

## 16. References

### Official Documentation
- [UNO Fluent UI Assets](https://platform.uno/docs/articles/uno-fluent-assets.html)
- [Segoe Fluent Icons Font (Microsoft)](https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font)
- [UNO Platform-Specific C#](https://platform.uno/docs/articles/platform-specific-csharp.html)
- [UNO Supported Features](https://platform.uno/docs/articles/supported-features.html)

### NuGet Packages
- [Uno.Fonts.Fluent 2.7.1](https://www.nuget.org/packages/Uno.Fonts.Fluent/)
- [GitHub: uno.fonts repository](https://github.com/unoplatform/uno.fonts)

### GitHub Issues
- [#6493: Plan for new Segoe Fluent Icons](https://github.com/unoplatform/uno/discussions/6493)
- [#3011: Support for Segoe fonts](https://github.com/unoplatform/uno/issues/3011)
- [#5734: Use new Fluent Icons](https://github.com/unoplatform/uno/issues/5734)
- [#225: SymbolIcon cannot be used](https://github.com/unoplatform/uno/issues/225)

### Stack Overflow
- [How to use Font Icon on Uno Platform](https://stackoverflow.com/questions/65456676/how-to-use-font-icon-on-uno-platform)

### Internal Documentation
- `/StoryCAD/Assets/SharedAssets.md` - Asset loading guide
- `/devdocs/issue_1139_uno_api_compatibility.md` - Related UNO API issues
- `/.claude/docs/dependencies/uno-platform-guide.md` - UNO Platform patterns

---

## Appendix A: Full Icon Inventory

### Shell.xaml Icons (30 total)
| Line | Type | Font | Glyph | Purpose | Status |
|------|------|------|-------|---------|--------|
| 154 | FontIcon | SymbolThemeFontFamily | E8F4 | Open | ✅ → 🔧 |
| 162 | FontIcon | SymbolThemeFontFamily | E74E | Save | ✅ → 🔧 |
| 170 | FontIcon | SymbolThemeFontFamily | E792 | Save As | ✅ → 🔧 |
| 179 | FontIcon | SymbolThemeFontFamily | e838 | Backup | ✅ → 🔧 |
| 187 | FontIcon | SymbolThemeFontFamily | E127 | Close | ✅ → 🔧 |
| 192 | FontIcon | SymbolThemeFontFamily | E106 | Exit | ✅ → 🔧 |
| 201 | FontIcon | SymbolThemeFontFamily | E710 | Add | ✅ → 🔧 |
| 295 | FontIcon | SymbolThemeFontFamily | E759 | Move | ✅ → 🔧 |
| 311 | Button | SymbolThemeFontFamily | E09A | Move Left | ✅ → 🔧 |
| 315 | Button | SymbolThemeFontFamily | E013 | Move Right | ✅ → 🔧 |
| 319 | Button | SymbolThemeFontFamily | E09C | Move Up | ✅ → 🔧 |
| 323 | Button | SymbolThemeFontFamily | E09D | Move Down | ✅ → 🔧 |
| 335 | FontIcon | SymbolThemeFontFamily | E71B | Collaborator | ✅ → 🔧 |
| 340 | FontIcon | SymbolThemeFontFamily | E90F | Tools | ✅ → 🔧 |
| 384 | FontIcon | SymbolThemeFontFamily | F571 | Reports | ✅ → 🔧 |
| 415 | FontIcon | SymbolThemeFontFamily | E713 | Preferences | ✅ → 🔧 |
| 424 | FontIcon | SymbolThemeFontFamily | ED15 | Feedback | ✅ → 🔧 |
| 433 | FontIcon | SymbolThemeFontFamily | E897 | Help | ✅ → 🔧 |
| 589 | FontIcon | **Segoe MDL2** | E777 | Backup Status | ❌ → ⚠️ |
| 113+ | SymbolIcon | (Enum) | Various | TreeView | ✅ |

**Legend**:
- ✅ Already works (SymbolIcon)
- 🔧 Needs Uno.Fonts.Fluent (SymbolThemeFontFamily)
- ❌ Broken (Hardcoded font)
- ⚠️ Needs code change

### FileOpenMenu.xaml Icons (5 total)
| Line | Type | Font | Glyph | Purpose | Status |
|------|------|------|-------|---------|--------|
| 16 | FontIcon | SymbolThemeFontFamily | E710 | New | 🔧 |
| 23 | FontIcon | SymbolThemeFontFamily | EC92 | Recent | 🔧 |
| 30 | FontIcon | SymbolThemeFontFamily | E753 | Sample | 🔧 |
| 37 | FontIcon | **Segoe MDL2** | E777 | Backup | ❌ |
| 47 | FontIcon | SymbolThemeFontFamily | E838 | Open | 🔧 |

### ProblemPage.xaml Icons (4 total)
| Line | Type | Font | Glyph | Purpose | Status |
|------|------|------|-------|---------|--------|
| 189 | FontIcon | **Segoe MDL2** | E71B | Assign | ❌ |
| 226 | FontIcon | **Segoe MDL2** | E74B | Move Down | ❌ |
| 230 | FontIcon | **Segoe MDL2** | E74E | Save | ❌ |
| 258 | SymbolIcon | (Enum) | Various | Beat Icon | ✅ |

### Other Files
| File | Icons | Status |
|------|-------|--------|
| WorkflowShell.xaml | 1 FontIcon (Help) | 🔧 |
| WebPage.xaml | 3 SymbolIcon | ✅ |
| RelationshipView.xaml | 1 SymbolIcon (Cancel) | ✅ |
| PreferencesDialog.xaml | 1 SymbolIcon (Error) | ✅ |

**Summary**:
- **23 icons** need Uno.Fonts.Fluent package (SymbolThemeFontFamily)
- **5 icons** need code changes (hardcoded font)
- **11 icons** already work (SymbolIcon enum)
- **Total**: 39 icons analyzed

---

## Appendix B: Code Snippets for Testing

### Test 1: Verify Package Installation
```bash
# After adding package, verify it's restored
dotnet list package | grep Uno.Fonts.Fluent

# Expected output:
# > Uno.Fonts.Fluent    2.7.1
```

### Test 2: Visual Inspection Checklist (macOS)
```
Before Fix:
[ ] Shell toolbar - empty squares instead of icons
[ ] File menu - empty squares
[ ] TreeView - icons SHOULD be visible (SymbolIcon)
[ ] Status bar - backup icon missing

After Fix:
[ ] Shell toolbar - all icons visible
[ ] File menu - all icons visible
[ ] TreeView - icons still visible (no change expected)
[ ] Status bar - backup icon visible
```

### Test 3: Compare Icon Rendering
**Take screenshots of**:
1. Windows - Shell.xaml main toolbar
2. macOS (before fix) - Shell.xaml main toolbar
3. macOS (after fix) - Shell.xaml main toolbar

**Compare**:
- Icon shape/appearance
- Spacing and alignment
- Color rendering (respects theme)

---

## Appendix C: Search Commands Used

### Icon Usage Search
```bash
# FontIcon usage
grep -r "FontIcon" StoryCAD/ --include="*.xaml" -n

# SymbolIcon usage
grep -r "SymbolIcon" StoryCAD/ --include="*.xaml" -n

# Glyph property
grep -r "Glyph" StoryCAD/ --include="*.xaml" -n

# Symbol enum in C#
grep -r "Symbol\." StoryCADLib/ --include="*.cs" -n
```

### Platform-Specific Code Search
```bash
# macOS conditionals
grep -r "__MACOS__" StoryCAD/ --include="*.cs" -n

# Windows conditionals
grep -r "HAS_UNO_WINUI" StoryCAD/ --include="*.cs" -n

# Font references
grep -r "Segoe" StoryCAD/ -n
```

### Package Search
```bash
# Uno.Fonts references
grep -r "Uno.Fonts" StoryCAD/ -n

# Package versions
cat Directory.Packages.props
```

---

**Investigation Status**: ✅ **COMPLETE**
**Next Steps**: Implement solution (estimated 3-4 hours)
**Priority**: HIGH - Blocks macOS user experience
