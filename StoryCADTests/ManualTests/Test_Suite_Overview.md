# StoryCAD Test Suite Overview

**Updated for 4.0 release** — macOS-first testing with Windows follow-up.

All test plans include **Platform Notes** for macOS/Windows differences.

## Test Plan Hierarchy (Tiered)

### Tier 1: Must Test — Blocks Release (~1.5 hours, macOS first)
**Purpose**: Core functionality that must work on both platforms before release
**When**: Before any release candidate

| File | Time | Coverage |
|------|------|----------|
| `Smoke_Test.md` | ~5 min | Launch, create, save, open, exit |
| `Core_File_Operations.md` | ~10 min | New, save, open, save-as, samples |
| `Core_Story_Elements.md` | ~10 min | Character, Problem, Scene, Setting, Notes |
| `Core_Navigation.md` | ~10 min | Tree, tabs, views, search, drag-drop |
| `StoryWorld_Test_Plan.md` | ~15 min | All 9 worldbuilding tabs, list entries |
| `Cross_Platform_File_Interchange.md` | ~10 min | .stbx roundtrip Mac-Windows |

### Tier 2: Should Test (~1 hour)
**Purpose**: Important features and tools
**When**: Before release, after Tier 1 passes

| File | Time | Coverage |
|------|------|----------|
| `Copy_Elements_Test_Plan.md` | ~10 min | Copy elements between outlines |
| `Reports_Test_Plan.md` | ~15 min | PDF export, Print (Windows), Scrivener |
| `Tools_Test_Plan.md` | ~15 min | Master Plots, Dramatic Situations, Stock Scenes, etc. |
| `Services_Test_Plan.md` | ~15 min | AutoSave, Backup, Search, Logging |
| `Preferences_Test_Plan.md` | ~10 min | Settings, theme, directories |

### Tier 3: Nice to Test (~1 hour)
**Purpose**: Platform-specific behaviors and edge cases
**When**: If time permits after Tiers 1-2

| File | Time | Coverage |
|------|------|----------|
| `Window_Management.md` | ~5 min | Resize, min/max, full-screen, overflow |
| `macOS_Specific.md` | ~15 min | Install, menu bar, Cmd shortcuts, permissions |
| `Full_Manual_Test_Plan.md` | ~30 min | Edit ops, error handling, performance |

---

## Test Assignment Strategy (4.0)

### For 2 Testers (Terry + Jake) — macOS First:
- **Tester A**: Smoke + Core File Ops + Core Navigation + StoryWorld + Reports
- **Tester B**: Core Story Elements + Copy Elements + Tools + Services + Preferences
- **Both**: Cross-Platform File Interchange (requires both platforms)

### Windows Follow-Up (as time permits):
Run the same tiers on Windows, prioritizing:
- StoryWorld (new feature, untested on production Windows)
- Copy Elements (new feature)
- Reports — Print to physical printer (Windows-only feature)
- Anything that worked in 3.4 but may have regressed

---

## Quick Test Selection Guide

| Time Available | Run These Tests |
|---------------|-----------------|
| 5 minutes | Smoke Test only |
| 30 minutes | Smoke + Core tests (File Ops, Elements, Navigation) |
| 1 hour | Tier 1 (all 6 plans) |
| 2 hours | Tier 1 + Tier 2 |
| 3.5 hours | All tiers |