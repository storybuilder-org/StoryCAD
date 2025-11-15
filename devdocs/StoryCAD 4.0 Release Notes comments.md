# StoryCAD 4.0 Release Notes - Draft Comments

**Release Date:** TBD
**Major Version:** 4.0
**Platforms:** Windows (v3.4+), macOS (v4.0)

---

## macOS Platform Support

StoryCAD 4.0 introduces macOS support, bringing our fiction outlining tools to macOS users for the first time. The macOS version is built on UNO Platform, enabling cross-platform development while maintaining native performance on both Windows and macOS.

### What's New for macOS Users

**Full Outlining Capabilities:**
- All story elements: Story Overview, Characters, Settings, Scenes, Problems, Folders, Notes
- Complete toolset: Master Plots, Dramatic Situations, Stock Scenes, Conflict Builder
- File compatibility: .stbx files work seamlessly between Windows and macOS
- Scrivener integration: Export outlines to Scrivener projects
- Reports: Print reports and character descriptions

**macOS-Native Experience:**
- Launch from Applications folder, Dock, or Launchpad
- Native macOS window management
- Standard macOS keyboard shortcuts
- Optimized for both Intel and Apple Silicon (M1/M2/M3) processors

### Platform-Specific Differences

**Text Editing on macOS:**

StoryCAD 4.0 on macOS uses plain text editing for all text fields (Character descriptions, Scene summaries, Notes, etc.). This is due to platform constraints with the UNO Platform's RichEditBox implementation on macOS.

**What this means:**
- **Windows (v3.4+)**: Full rich text formatting (bold, italic, fonts, colors, alignment, lists) with formatting toolbar and spell checking
- **macOS (v4.0)**: Plain text editing only - no formatting toolbar, no spell checking

**Cross-Platform File Compatibility:**

.stbx outline files are fully compatible between Windows and macOS. You can create an outline on Windows and open it on macOS, or vice versa. All story content, structure, and story elements transfer seamlessly.

**Important**: Text formatting applied on Windows will be permanently removed when a file is opened and saved on macOS. The text content itself is preserved, but formatting (bold, italic, fonts, colors) is stripped.

**Recommendations for cross-platform workflows:**
- Keep text formatting minimal if regularly moving files between platforms
- Maintain separate Windows and macOS copies if formatting is important
- Use Windows exclusively for outlines requiring rich text formatting
- Use macOS for plain text workflows

### Technical Decision: RTF Implementation (Issue #1130)

**Decision**: Option 2 (Hybrid Approach) selected for v4.0 initial macOS release.

**Rationale:**
- Zero current macOS user base (new platform launch)
- Unknown macOS compatibility landscape - other issues needed resolution first
- Working implementation already exists and tested
- Risk management - RTF cross-platform compatibility unproven
- Ship-and-iterate strategy - launch v4.0, gather feedback, upgrade in v4.1 if demanded

**Current Implementation:**
- macOS uses `TextBox` control for text entry
- RTF formatting stripped on load via `ReportFormatter.GetText()`
- Plain text saved to `RtfText` property for file persistence
- Full API compatibility with Windows version maintained
- File format compatibility preserved (Windows ↔ macOS)

**Future Consideration:**

Option 1 (NSTextView native control for full RTF support on macOS) remains available for v4.1+ if:
- macOS user base grows (≥100 users)
- Users request formatting capabilities (≥5 requests)
- Other macOS compatibility issues resolved
- Market validates macOS investment

Estimated effort: 15-25 hours (1-week POC to determine RTF compatibility viability)

**Related Documentation:**
- Full research: Previously at `/devdocs/issue_1130_rtf_implementation.md` (now archived)
- Implementation: `/StoryCADLib/Controls/RichEditBoxExtended.desktop.cs`
- User documentation: User manual updated with platform differences and cross-platform guidance
- UNO Platform tracking: [Issue #3848](https://github.com/unoplatform/uno/issues/3848)

---

## Windows Updates (v3.4)

**UNO Platform Migration:**

StoryCAD 3.4 completes the migration from WinUI 3 to UNO Platform on Windows, preparing the codebase for cross-platform support while maintaining full Windows feature compatibility.

**Windows users should see no functional changes** - this update maintains all existing features while modernizing the underlying platform.

**Technical Changes:**
- Migration to UNO Platform SDK 6.4.13
- .NET 9.0 SDK 9.0.306
- Dual-targeting: `net9.0-windows10.0.22621` (WinAppSDK) and `net9.0-desktop` (Desktop)
- Build system updates for multi-platform support

**Benefits for Windows Users:**
- Improved stability and performance
- Foundation for future cross-platform features
- Continued full RTF support with formatting toolbar
- All existing functionality preserved

---

## Breaking Changes

**None for existing Windows users.** All Windows functionality remains identical.

**For new macOS users:** Text formatting not available (see Platform-Specific Differences above).

---

## Known Issues

**macOS:**
- No text formatting capabilities (by design - see Issue #1130 decision)
- No spell checking in text fields
- No right-click formatting context menu

**Cross-Platform:**
- Opening Windows files with formatting on macOS permanently strips formatting when saved

---

## Installation

**Windows (v3.4+):**
- Download from [Microsoft Apps Store](https://apps.microsoft.com/detail/9plbnhzv1xm2?hl=en-US&gl=US)
- System requirements: Windows 10 (19041.0+) or Windows 11

**macOS (v4.0):**
- Download from [Apple App Store](https://www.apple.com/app-store/)
- System requirements: macOS 10.15 (Catalina) or later

---

## Upgrade Path

**From StoryCAD 3.x (Windows):**
- Install v3.4 from Microsoft Apps Store
- Your existing .stbx files work without modification
- All features and formatting preserved

**New macOS Installation:**
- Install v4.0 from Apple App Store
- Open existing .stbx files from Windows (formatting will be stripped on save)
- Create new plain text outlines

---

## Documentation

**User Manual Updated:**
- Platform-specific installation instructions
- Cross-platform file compatibility guidance
- Platform differences clearly documented
- Troubleshooting for cloud storage cross-platform scenarios

**Staging Site:** https://storybuilder-org.github.io/StoryBuilder-Manual/

---

## Credits

**Development:**
- Windows UNO Platform migration: [Development team]
- macOS platform support: [Development team]
- Cross-platform testing: [Testing team]

**Documentation:**
- User manual platform updates: AI-assisted (Claude Code)
- Technical research (Issue #1130): [Primary researcher]

---

## Looking Forward (v4.1+)

**Potential Future Enhancements:**
- Enhanced macOS text editing (NSTextView implementation) if user demand warrants
- Additional platform-specific features
- Expanded cross-platform capabilities

**Feedback Welcome:**
- Discord: https://discord.gg/storybuilder
- GitHub Issues: https://github.com/storybuilder-org/StoryCAD/issues

---

**Note**: This is a draft for release notes comments. Finalize before v4.0 public release.
