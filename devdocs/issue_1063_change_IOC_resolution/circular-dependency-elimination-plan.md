# Circular Dependency Elimination Plan

## Overview

This document identifies all circular dependencies discovered during the DI conversion (Issue #1063) and outlines a plan to eliminate them. Each circular dependency will be addressed as a separate sub-issue to ensure focused, incremental fixes.

## Status

**Plan Status**: DRAFT - In Development  
**Parent Issue**: #1063 (Change IOC Resolution)  
**Discovery Date**: 2025-01-21

---

## Identified Circular Dependencies (A→B→A Format)

### Priority 1: Critical Service-ViewModel Cycles

1. **ShellViewModel → AutoSaveService → ShellViewModel**
   - **Occurrences**: Multiple throughout batches
   - **Root Cause**: AutoSaveService calls ShellViewModel.ShowMessage for UI notifications
   - **Current Mitigation**: Partially using WeakReferenceMessenger for StatusChangedMessage
   - **Sub-Issue**: TBD

2. **ShellViewModel → BackupService → ShellViewModel**
   - **Occurrences**: Multiple throughout batches
   - **Root Cause**: BackupService directly sets ShellViewModel.BackupStatusColor (3 locations)
   - **Current Mitigation**: Partially using messaging, BackupStatusColor still uses Ioc.Default
   - **Sub-Issue**: TBD

3. **OutlineViewModel → AutoSaveService → OutlineViewModel**
   - **Occurrences**: Complex chain across batches
   - **Root Cause**: AutoSaveService needs OutlineViewModel's StoryModel and SaveFile() method
   - **Current Mitigation**: Lazy loading in OutlineViewModel (fragile)
   - **Sub-Issue**: TBD

4. **OutlineViewModel → BackupService → OutlineViewModel**
   - **Occurrences**: Complex chain across batches
   - **Root Cause**: BackupService needs OutlineViewModel's StoryModelFile path
   - **Current Mitigation**: Lazy loading in OutlineViewModel (fragile)
   - **Sub-Issue**: TBD

### Priority 2: Service-Service Cycles

5. **AutoSaveService → BackupService → AutoSaveService**
   - **Via**: SerializationLock
   - **Full Chain**: AutoSaveService → SerializationLock → BackupService → SerializationLock → AutoSaveService
   - **Root Cause**: SerializationLock needs to stop both services during serialization
   - **Sub-Issue**: TBD

### Priority 3: ViewModel-ViewModel Cycles

6. **ShellViewModel → OutlineViewModel → ShellViewModel**
   - **Type**: Indirect property access
   - **Root Cause**: Mutual dependency for navigation and state management
   - **Current Mitigation**: Both use constructor injection (same batch)
   - **Sub-Issue**: TBD

### Priority 4: Infrastructure Cycles

7. **Windowing → ShellViewModel → Windowing**
   - **Root Cause**: Infrastructure service depending on ViewModel
   - **Status**: Has existing TODOs in code
   - **Sub-Issue**: TBD

8. **Windowing → OutlineViewModel → Windowing**
   - **Root Cause**: Infrastructure service depending on ViewModel
   - **Status**: Has existing TODOs in code
   - **Sub-Issue**: TBD

### Priority 5: Indirect ViewModel Chains

9. **CharacterViewModel → ShellViewModel → CharacterViewModel**
   - **Type**: Indirect via ViewModel creation
   - **Root Cause**: ShellViewModel creates all ViewModels including CharacterViewModel
   - **Sub-Issue**: TBD

10. **CharacterViewModel → OutlineViewModel → CharacterViewModel**
    - **Type**: Indirect dependency
    - **Root Cause**: ViewModels depending on other ViewModels
    - **Sub-Issue**: TBD

---

## Services That Depend on ViewModels (Architecture Violations)

### Critical Violations

| Service | Depends On | Purpose | Location |
|---------|------------|---------|----------|
| **BackupService** | ShellViewModel | Sets BackupStatusColor directly | Lines 165, 246, 263 |
| **LogService** | ShellViewModel | Reads FilePathToLaunch | Line 305 |
| **CollaboratorService** | ShellViewModel | Sets CollabArgs | Line 238 |
| **AutoSaveService** | OutlineViewModel | Accesses StoryModel, SaveFile() | Multiple |
| **BackupService** | OutlineViewModel | Accesses StoryModelFile path | Multiple |
| **StoryIO** | OutlineViewModel | DAL layer violation | Multiple |
| **OutlineService** | OutlineViewModel | Service depends on its VM | Multiple |
| **Windowing** | ShellViewModel, OutlineViewModel | Infrastructure violation | Multiple |

---

## Root Architectural Problems

1. **StoryModel Location**
   - Currently: Accessed through OutlineViewModel
   - Should Be: In AppState
   - Impact: Causes most OutlineViewModel dependencies

2. **UI Notifications from Services**
   - Problem: Services send UI messages that fail in headless/API mode
   - Documented: MESSAGING_AND_HEADLESS_ISSUE.md
   - Solution: Domain events instead of UI events

3. **Direct Property Updates**
   - Problem: Services directly setting ViewModel properties
   - Example: BackupStatusColor, FilePathToLaunch, CollabArgs
   - Solution: Messaging or return values

4. **ViewModels as Service Locators**
   - Problem: ViewModels being used to access other services and data
   - Solution: Direct injection or AppState

---

## Recommended Solutions (from GPT5 Analysis)

1. **Extract Minimal Interfaces**
   - Create IReadOnlyAppState, INavigationContext, etc.
   - Each interface exposes only what's needed
   - Example: If only CurrentUser is needed, create IReadOnlySession

2. **Use Messaging for Decoupled Communication**
   - Replace direct ViewModel calls with WeakReferenceMessenger
   - Services send domain events, not UI commands
   - ViewModels translate domain events to UI updates

3. **Move Shared State to AppState**
   - StoryModel, StoryModelFile should be in AppState
   - Services depend on AppState, not ViewModels
   - ViewModels also get data from AppState

4. **Return Results Instead of UI Updates**
   - Services return status/results
   - ViewModels decide how to display
   - Eliminates service→ViewModel dependencies

5. **Apply Lazy<T> Sparingly**
   - Use only when truly needed for deferred resolution
   - Current usage in OutlineViewModel is fragile
   - Consider alternatives first

---

## Implementation Plan

### Phase 1: Create Sub-Issues
Each circular dependency gets its own GitHub issue with:
- Specific cycle to break (A→B→A)
- Current code locations
- Proposed solution approach
- Acceptance criteria
- Testing requirements

### Phase 2: Prioritized Execution
1. **First**: Move StoryModel to AppState (fixes multiple cycles)
2. **Second**: Implement messaging for UI notifications
3. **Third**: Extract minimal interfaces
4. **Fourth**: Fix remaining cycles

### Phase 3: Validation
- Each fix must pass all existing tests
- No new Ioc.Default calls introduced
- Grep verification for each fixed cycle
- Smoke test application functionality

---

## Sub-Issues Template

```markdown
Title: Fix Circular Dependency: [A] → [B] → [A]

## Description
Remove circular dependency between [A] and [B].

## Current State
- [A] depends on [B] because: [reason]
- [B] depends on [A] because: [reason]
- Code locations: [file:line references]

## Proposed Solution
[Specific approach: interface extraction, messaging, state relocation, etc.]

## Acceptance Criteria
- [ ] Circular dependency eliminated
- [ ] No Ioc.Default calls added
- [ ] All existing tests pass
- [ ] Grep verification shows no cycle
- [ ] Application smoke test passes

## Testing
- Unit tests for affected components
- Integration test for the specific flow
- Manual verification of [specific feature]
```

---

## Notes

- This plan emerged from the DI conversion work in Issue #1063
- Some cycles are already partially mitigated with TODOs in code
- The Windowing service has documented architectural issues
- Priority based on impact and frequency of occurrence
- Solutions derived from GPT5's architectural recommendations and the "squeeze" technique for interface extraction