# Issue #1069: Achieve 100% API and OutlineService Test Coverage

## Overview
This folder contains the test coverage analysis and implementation plan for achieving 100% test coverage for the SemanticKernelAPI and OutlineService components, as well as addressing critical gaps in ShellViewModel.

## Status
- **Branch**: `issue-1069-test-coverage`
- **Date**: 2025-01-14
- **Current Coverage**: ~51% overall

## Documentation

### Coverage Analysis Reports
1. **[API_Coverage_Analysis.md](./API_Coverage_Analysis.md)**
   - Current: 87% (13/15 methods)
   - Gap: 2 methods

2. **[ShellViewModel_Coverage_Analysis.md](./ShellViewModel_Coverage_Analysis.md)**
   - Current: 6% (1/17 methods) - CRITICAL
   - Gap: 16 methods

3. **[OutlineService_Coverage_Analysis.md](./OutlineService_Coverage_Analysis.md)**
   - Current: 60% (15/25+ methods)
   - Gap: 10+ methods

4. **[Test_Coverage_Gap_Analysis.md](./Test_Coverage_Gap_Analysis.md)**
   - Executive summary
   - Risk assessment
   - Implementation strategy

## Work Completed

### Test File Renaming
- ✅ Renamed all test files to match source file names
- ✅ Updated test method names to follow convention
- ✅ Documented naming convention in CLAUDE.md

### Files Renamed
- `SemanticKernelApiTests.cs` → `SemanticKernelAPITests.cs`
- `ShellTests.cs` → `ShellViewModelTests.cs`
- `BackendTests.cs` → `BackendServiceTests.cs`
- `ProblemVMTests.cs` → `ProblemViewModelTests.cs`
- `LockTest.cs` → `LockTests.cs`
- Merged `ShellUITests.cs` into `StoryModelTests.cs`

## Next Steps

### Priority 0 (Immediate)
1. ShellViewModel.SaveModel - Data loss risk
2. ShellViewModel.TreeViewNodeClicked - Navigation failures
3. OutlineService.AddStoryElement - Core functionality

### Priority 1 (High)
1. ShellViewModel View Management
2. OutlineService Trash Operations
3. API.SetCurrentModel for Collaborator

### Priority 2 (Medium)
1. ShellViewModel Move Operations
2. OutlineService Tree Operations
3. Command Testing

## Test Naming Convention

### Files
- Pattern: `[SourceFileName]Tests.cs`
- Example: `SemanticKernelAPITests.cs`

### Methods
- Pattern: `MethodName_Scenario_ExpectedResult`
- Example: `SaveModel_WithValidPath_SucceedsSilently`

## Notes

- The DynamicSearch branch contains additional search methods that will need test coverage when merged
- ShellViewModel requires urgent attention due to 6% coverage
- Consider refactoring untestable code (static methods, tight coupling)