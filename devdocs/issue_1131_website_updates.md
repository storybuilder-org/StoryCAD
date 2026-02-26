# Issue #1131: Website Updates for Cross-Platform Support

Working document for updating storybuilder.org to reflect macOS support in Release 4.0.

## Research Summary

### How Others Announced Cross-Platform Support

**UNO Platform 2.4 macOS Announcement** ([source](https://platform.uno/blog/announcing-uno-platform-2-4-macos-support-and-windows-calculator-on-macos/)):
- Combined feature announcement with concrete demo (Windows Calculator on macOS)
- Emphasized code reuse: "your C# and XAML code can run seamlessly on Windows, iOS, Android, Web, and now macOS"
- Used "preview" language for initial release
- Included visual banner announcing the version

**Microsoft Windows App Launch** ([source](https://techcommunity.microsoft.com/blog/windows-itpro-blog/windows-app-now-available-on-all-major-platforms/4246939)):
- Clear platform-specific sections for each OS
- In-place update strategy for existing users
- Customer testimonial highlighting benefits
- Emphasized "no disruption to business continuity"

**Best Practices for Product Announcements** ([source](https://userpilot.com/blog/product-launch-guide-for-saas/)):
- Build anticipation with teaser content before launch
- Keep messaging brief and focused
- Single clear CTA per section
- Include visuals/screenshots prominently
- Personalize messaging for different audience segments

---

## Part A: Website Sitemap

```
storybuilder.org/
├── / (Homepage)
├── /about/
├── /why-storycad/
├── /volunteer/
├── /storycad (Product Page)
├── /blog
│   ├── /category/release-notes/
│   ├── /category/blog/
│   └── /category/tips-and-tricks/
├── /learn/
├── /resources/
├── /events/
├── /faq/
├── /contact-us/
└── /donate/

External Links:
├── Microsoft Store: apps.microsoft.com/store/detail/9PLBNHZV1XM2
├── User Manual: storybuilder-org.github.io/StoryCAD/
├── GitHub: github.com/storybuilder-org/StoryCAD
├── Discord: discord.com/invite/CHGu4Fh9r8
├── Facebook: facebook.com/StoryCAD
└── Twitter: twitter.com/StoryCAD
```

---

## Part B: Requirements Mapping

| Page | Updates Required | Priority |
|------|------------------|----------|
| **/** (Homepage) | Platform badges, update tagline, update "Windows desktop" text | HIGH |
| **/storycad** | Dual platform download buttons, system requirements, platform features | HIGH |
| **/category/release-notes/** | New Release 4.0 announcement post | HIGH |
| **/faq/** | Add macOS FAQs, update platform questions | MEDIUM |
| **/learn/** | Note cross-platform compatibility | MEDIUM |
| **/resources/** | Update any platform-specific references | MEDIUM |
| **/why-storycad/** | Fix "cloud-based" claim, update platform info | MEDIUM |
| **/about/** | Update organization description if needed | LOW |
| **/volunteer/** | No changes needed | NONE |
| **/contact-us/** | No changes needed | NONE |
| **/donate/** | No changes needed | NONE |
| **/events/** | No changes needed | NONE |

### External Updates
| Resource | Updates Required | Priority |
|----------|------------------|----------|
| **User Manual** | Platform-specific notes, keyboard shortcuts, file paths | HIGH |
| **Microsoft Store listing** | Already Windows-specific, no change needed | NONE |
| **Mac App Store listing** | NEW - create when distribution ready (#1126) | HIGH |

---

## Part C: Draft Updates by Page

### 1. Homepage (/)

**Current State:**
- Hero: "EVERY STORY DESERVES TO BE TOLD"
- CTA links to Microsoft Store only
- Describes as "FREE Windows desktop story outliner"

**Required Changes:**

#### Hero Section
Add platform badges below the main tagline:

```
[Windows Logo] Windows  |  [Apple Logo] macOS
```

Or use a combined badge:
```
Available for Windows and macOS
```

#### Marketing Copy Updates

**FIND:** "FREE Windows desktop story outliner"
**REPLACE WITH:** "FREE story outliner for Windows and macOS"

**FIND:** Any instance of "Windows desktop application"
**REPLACE WITH:** "desktop application for Windows and macOS"

#### Download Button
Change single Microsoft Store button to two options:

```
[Download for Windows]     [Download for macOS]
   Microsoft Store            Direct Download
```

Or use a smart download section:
```
Download StoryCAD
[Windows]  [macOS]
```

---

### 2. Product Page (/storycad)

**Current State:**
- Hero: "WORDS ARE NOT ENOUGH"
- Features list focused on functionality
- Two download buttons → Microsoft Store
- Specifies "Windows desktop application"

**Required Changes:**

#### Platform Section (NEW)
Add a dedicated platform requirements section:

```markdown
## System Requirements

### Windows
- Windows 10 or Windows 11
- Version 10.0.19041.0 or later
- x64 architecture
- Available on Microsoft Store

### macOS
- macOS 10.15 Catalina or later
- Intel (x64) or Apple Silicon (ARM64)
- Download DMG installer

### Cross-Platform Features
- .stbx story files work on both platforms
- Sync via cloud storage (OneDrive, iCloud, Dropbox)
- Same features on both platforms*

*Note: Print Reports available on Windows. macOS uses Export to PDF.
```

#### Download Section
Replace single-platform download with:

```markdown
## Download StoryCAD

### Windows
[Get it from Microsoft Store]
- Automatic updates
- Windows 10/11 required

### macOS
[Download DMG]
- macOS 10.15+ required
- Supports Intel and Apple Silicon

Both versions are free and open source.
```

#### Feature Notes
Add platform-specific callouts where relevant:

```markdown
**Print Reports**
Generate formatted reports of your story outline.
- Windows: Print directly or export to PDF
- macOS: Export to PDF
```

---

### 3. Release Notes - New Post (/category/release-notes/)

**Create new release notes post for 4.0:**

```markdown
# Release 4.0 - StoryCAD Now on macOS!

**Release Date:** [TBD]

We're thrilled to announce StoryCAD 4.0, our biggest release ever. StoryCAD is now available on macOS, bringing the same powerful story outlining tools to Mac users.

## What's New

### macOS Support
StoryCAD now runs natively on macOS 10.15 Catalina and later, supporting both Intel and Apple Silicon Macs. This means you can:
- Outline stories on your Mac with the same features Windows users love
- Share .stbx files between Windows and Mac seamlessly
- Use familiar Mac keyboard shortcuts (Cmd instead of Ctrl)

### Cross-Platform Compatibility
Your story files (.stbx) work on both platforms. Start a story on Windows, continue on Mac, or vice versa. Use your preferred cloud storage (iCloud, OneDrive, Dropbox) to keep files synced.

### Platform-Specific Notes

**Windows users:** No changes to your workflow. Automatic updates via Microsoft Store.

**macOS users:**
- Download the DMG installer from our website
- Export to PDF is available (Print Reports coming in a future update)
- First launch requires allowing the app in System Preferences > Security

## Technical Details

StoryCAD 4.0 is built on UNO Platform, enabling us to maintain a single codebase while delivering native experiences on both platforms. This foundation also opens the door for future platforms (Linux, web, mobile).

## Download

- **Windows:** [Microsoft Store](https://apps.microsoft.com/store/detail/9PLBNHZV1XM2)
- **macOS:** [Download DMG](#) (link to be added)

## Thank You

This release represents months of work to bring StoryCAD to more writers. Special thanks to our testers and the UNO Platform community.

Questions? Join us on [Discord](https://discord.com/invite/CHGu4Fh9r8) or [contact us](/contact-us/).
```

---

### 4. FAQ Page (/faq/)

**Add new FAQ entries:**

```markdown
## Platform & Compatibility

### What platforms does StoryCAD run on?
StoryCAD runs on Windows 10/11 and macOS 10.15 (Catalina) or later. The macOS version supports both Intel and Apple Silicon Macs.

### Can I share story files between Windows and Mac?
Yes! StoryCAD's .stbx files are fully cross-platform. You can start a story on Windows and continue on Mac, or vice versa. Use cloud storage like iCloud, OneDrive, or Dropbox to keep your files synced across devices.

### Are there any feature differences between Windows and macOS?
The features are nearly identical. The main difference is:
- **Windows:** Print Reports and Export to PDF
- **macOS:** Export to PDF only (direct printing coming in a future update)

### Which version should I download?
- **Windows 10 or 11:** Download from the Microsoft Store
- **macOS 10.15+:** Download the DMG installer from our website

### I'm on Linux/ChromeOS/mobile. Is StoryCAD available?
Not yet, but we're exploring additional platforms for future releases. StoryCAD 4.0's cross-platform foundation makes this possible.

### I have both a Windows PC and a Mac. Do I need separate licenses?
No! StoryCAD is free and open source. Install it on as many devices as you like.
```

**Update existing FAQ if present:**

Any existing FAQ mentioning "Windows only" or "Windows desktop" should be updated to reflect cross-platform availability.

---

### 5. Why StoryCAD Page (/why-storycad/)

**Current Issue:**
Page mentions "cloud-based accessibility" and "work on your story anytime, anywhere" which is misleading for a desktop app.

**Required Changes:**

**FIND:** "cloud-based accessibility"
**REPLACE WITH:** "cross-platform availability"

**FIND:** "work on your story anytime, anywhere"
**REPLACE WITH:** "work on your story on Windows or Mac, with files that sync via your preferred cloud storage"

**ADD platform info to feature sections:**

```markdown
### Available Where You Write
StoryCAD runs on both Windows and macOS, so you can use whichever computer suits your writing style. Story files are fully compatible between platforms—start on your desktop, continue on your laptop.
```

---

### 6. Learn Page (/learn/)

**Add cross-platform note:**

```markdown
### Getting Started
StoryCAD is available for Windows and macOS. The tutorials and documentation apply to both platforms—the interface is identical. Where keyboard shortcuts differ, we show both:
- **Save:** Ctrl+S (Windows) / Cmd+S (Mac)
- **New:** Ctrl+N (Windows) / Cmd+N (Mac)
```

---

### 7. Resources Page (/resources/)

**Minor update to Product Knowledge section:**

**FIND:** References to "Windows" specifically
**REPLACE WITH:** "Windows and macOS" or just "StoryCAD"

---

## Part D: SEO & Metadata Updates

### Page Titles

| Page | Current (assumed) | Recommended |
|------|-------------------|-------------|
| Homepage | StoryCAD - Story Outliner | StoryCAD - Free Story Outliner for Windows & Mac |
| /storycad | StoryCAD Product | StoryCAD - Free Outlining Software for Windows & macOS |
| /faq | FAQ | StoryCAD FAQ - Windows & Mac Writing Software |

### Meta Descriptions

**Homepage:**
```
StoryCAD is a free, open-source story outliner for Windows and macOS. Structure your novel with character development, plot tools, and scene planning. Download free from Microsoft Store or as Mac DMG.
```

**Product Page:**
```
Download StoryCAD free for Windows or macOS. Outline your story with character tools, plot aids, and scene planning. Works on Windows 10/11 and macOS 10.15+.
```

### Keywords to Include
- StoryCAD Mac
- StoryCAD macOS
- story outliner Mac
- novel outlining software Mac
- free writing software macOS
- cross-platform story outliner

### Open Graph Tags
Update og:description to match meta descriptions. Consider platform-specific og:image showing both Windows and Mac screenshots.

---

## Part E: Visual Assets Needed

1. **Platform badges** - Windows logo + macOS logo for hero section
2. **Mac screenshot** - StoryCAD running on macOS for product page
3. **Side-by-side screenshot** - Windows and Mac versions together
4. **Download icons** - Microsoft Store badge, Mac download badge
5. **Release 4.0 banner** - For release notes post and potential homepage feature

---

## Implementation Checklist

### Phase 1: Pre-Launch (Before 4.0 Release)
- [ ] Prepare all draft content
- [ ] Create visual assets
- [ ] Set up Mac download infrastructure (#1126)
- [ ] Draft Release 4.0 blog post

### Phase 2: Launch Day
- [ ] Update homepage (platform badges, tagline, download buttons)
- [ ] Update /storycad product page
- [ ] Publish Release 4.0 announcement
- [ ] Update /faq with new entries
- [ ] Update meta descriptions and titles

### Phase 3: Post-Launch
- [ ] Update /why-storycad (fix cloud claims)
- [ ] Update /learn (add platform note)
- [ ] Update /resources (minor text updates)
- [ ] Monitor for issues and user feedback
- [ ] Update FAQ based on common questions

---

## References

- Issue #1131: https://github.com/storybuilder-org/StoryCAD/issues/1131
- Issue #1124: Release 4.0 for UNO desktop head for macOS
- Issue #1126: Package and distribute StoryCAD for macOS
- Issue #962: Printing isn't implemented on UNO
- UNO Platform macOS announcement: https://platform.uno/blog/announcing-uno-platform-2-4-macos-support-and-windows-calculator-on-macos/
- Microsoft Windows App launch: https://techcommunity.microsoft.com/blog/windows-itpro-blog/windows-app-now-available-on-all-major-platforms/4246939
