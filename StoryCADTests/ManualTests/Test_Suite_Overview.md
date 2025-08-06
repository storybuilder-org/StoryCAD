# StoryCAD Test Suite Overview

## Test Plan Hierarchy

### Level 1: Smoke Test (5 minutes)
**Purpose**: Verify build is stable enough for testing  
**When**: After every build, before any other testing  
**File**: `Smoke_Test.md`

### Level 2: Core Functionality Tests (30 minutes)
**Purpose**: Validate essential features work  
**When**: Daily builds, before release  
**Files**: 
- `Core_File_Operations.md` (10 min)
- `Core_Story_Elements.md` (10 min)  
- `Core_Navigation.md` (10 min)

### Level 3: Feature Area Tests (15-20 minutes each)
**Purpose**: Deep dive into specific features  
**When**: After feature changes, rotating schedule  
**Files**:
- `Tools_Test_Plan.md`
- `Reports_Test_Plan.md`
- `Drag_Drop_Test_Plan.md`
- `Keyboard_Shortcuts_Test_Plan.md`
- `Preferences_Test_Plan.md`

### Level 4: Full Regression (3-4 hours)
**Purpose**: Complete validation before release  
**When**: Release candidates  
**File**: `Full_Manual_Test_Plan.md`

---

## Test Assignment Strategy

### For 2 Testers:
- **Tester A**: Smoke + Core File Ops + Tools
- **Tester B**: Core Story Elements + Navigation + Reports

### Rotation Schedule:
- Week 1: Tools & Reports focus
- Week 2: Drag/Drop & Preferences focus
- Week 3: Full regression

---

## Quick Test Selection Guide

| Time Available | Run These Tests |
|---------------|-----------------|
| 5 minutes | Smoke Test only |
| 30 minutes | Smoke + Core tests |
| 1 hour | Smoke + Core + 2 Feature areas |
| 2 hours | Smoke + Core + All Features |
| 4 hours | Full Regression |