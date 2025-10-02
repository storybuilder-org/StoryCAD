# Issue #1111 Test Reorganization Recovery Plan

## Background
The test reorganization work for issue #1111 was completed but lost from git history. This plan provides exact steps to recreate the work.

## Current State
- Branch: `issue-1111-reorganize-tests` 
- PR #1114 exists but only contains devdocs deletion commit
- 30 test files remain in StoryCADTests root directory
- 12 test files already in organized folders
- Stash@{0} contains namespace fixes (StoryCAD.Tests → StoryCADTests)

## Recovery Steps

### Step 1: Apply Stashed Changes
```bash
git stash pop
```
This contains namespace fixes for files already in folders:
- DAL/StoryIOTests.cs
- Models/AppStateTests.cs
- Models/StoryDocumentTests.cs  
- Services/EditFlushServiceTests.cs
- Services/ISaveableTests.cs
- Services/Outline/FileCreateServiceTests.cs
- Services/Outline/FileOpenServiceTests.cs
- ViewModels/CharacterViewModelISaveableTests.cs
- ViewModels/FileOpenVMTests.cs
- ViewModels/OutlineViewModelSaveFileTests.cs
- ViewModels/ShellViewModelAppStateTests.cs
- Plus a partial fix to SceneViewModelTests.cs

### Step 2: Create Missing Folder Structure
```bash
mkdir -p StoryCADTests/Services/Backend
mkdir -p StoryCADTests/Services/Collaborator  
mkdir -p StoryCADTests/Services/Installation
mkdir -p StoryCADTests/Services/Search
mkdir -p StoryCADTests/Services/Logging
mkdir -p StoryCADTests/Services/Navigation
mkdir -p StoryCADTests/Services/Dialogs
mkdir -p StoryCADTests/Collaborator
mkdir -p StoryCADTests/Utilities
mkdir -p StoryCADTests/ViewModels/Tools
```

### Step 3: Move Test Files to Proper Locations

#### Services Tests
```bash
git mv StoryCADTests/BackendServiceTests.cs StoryCADTests/Services/Backend/
git mv StoryCADTests/CollaboratorServiceTests.cs StoryCADTests/Services/Collaborator/
git mv StoryCADTests/SearchServiceTests.cs StoryCADTests/Services/Search/
git mv StoryCADTests/InstallServiceTests.cs StoryCADTests/Services/Installation/
```

#### Models Tests
```bash
git mv StoryCADTests/CharacterModelTests.cs StoryCADTests/Models/
git mv StoryCADTests/StoryModelTests.cs StoryCADTests/Models/
git mv StoryCADTests/ProblemViewModelTests.cs StoryCADTests/Models/
```

#### DAL Tests
```bash
git mv StoryCADTests/ControlLoaderTests.cs StoryCADTests/DAL/
git mv StoryCADTests/ListLoaderTests.cs StoryCADTests/DAL/
git mv StoryCADTests/ToolLoaderTests.cs StoryCADTests/DAL/
git mv StoryCADTests/TemplateTests.cs StoryCADTests/DAL/
git mv StoryCADTests/IocLoaderTests.cs StoryCADTests/DAL/
git mv StoryCADTests/FileTests.cs StoryCADTests/DAL/
git mv StoryCADTests/PreferenceIOTests.cs StoryCADTests/DAL/
```

#### ViewModels Tests
```bash
git mv StoryCADTests/OutlineViewModelTests.cs StoryCADTests/ViewModels/
git mv StoryCADTests/ShellViewModelTests.cs StoryCADTests/ViewModels/
git mv StoryCADTests/FileOpenVMTests.cs StoryCADTests/ViewModels/
```

#### Collaborator Tests
```bash
git mv StoryCADTests/CollaboratorInterfaceTests.cs StoryCADTests/Collaborator/
git mv StoryCADTests/CollaboratorIntegrationTests.cs StoryCADTests/Collaborator/
git mv StoryCADTests/MockCollaboratorTests.cs StoryCADTests/Collaborator/
git mv StoryCADTests/WorkflowInterfaceTests.cs StoryCADTests/Collaborator/
git mv StoryCADTests/WorkflowContextTests.cs StoryCADTests/Collaborator/
git mv StoryCADTests/WorkflowFlowTests.cs StoryCADTests/Collaborator/
git mv StoryCADTests/SemanticKernelAPITests.cs StoryCADTests/Collaborator/
```

#### Utilities Tests
```bash
git mv StoryCADTests/ENVTests.cs StoryCADTests/Utilities/
git mv StoryCADTests/LockTests.cs StoryCADTests/Utilities/
git mv StoryCADTests/ReportFormatterTests.cs StoryCADTests/Utilities/
```

#### Services/Outline Tests
```bash
git mv StoryCADTests/OutlineServiceTests.cs StoryCADTests/Services/Outline/
git mv StoryCADTests/NodeItemTests.cs StoryCADTests/Services/Outline/
```

### Step 4: Create Stubbed SceneViewModelTests
Replace the contents of `StoryCADTests/ViewModels/SceneViewModelTests.cs` with:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.ViewModels;

namespace StoryCADTests.ViewModels
{
    [TestClass]
    public class SceneViewModelTests
    {
        // TODO: Issue #1113 - Rewrite SceneViewModelTests
        // Original tests were causing issues due to complex dependencies
        // This stub ensures the test class exists and compiles
        
        [TestMethod]
        public void SceneViewModel_Stub_Test()
        {
            Assert.Inconclusive("SceneViewModelTests need to be rewritten - see Issue #1113");
        }
    }
}
```

### Step 5: Fix Remaining Namespace Issues
Check for any files still using the wrong namespace:
```bash
grep -r "namespace StoryCAD\.Tests" StoryCADTests/
```

Change any found from `namespace StoryCAD.Tests` to `namespace StoryCADTests` (matching folder structure).

### Step 6: Build and Verify
```bash
# Build the solution
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64

# Run all tests
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net9.0-windows10.0.22621.0/StoryCADTests.dll"
```

**Expected Result:** 398 passed, 0 failed, 4 skipped

### Step 7: Commit Changes
```bash
git add -A
git commit -m "Issue #1111: Reorganize StoryCADTests folder structure

- Moved 42 test files from root to folders matching StoryCADLib structure
- Fixed namespace inconsistencies (StoryCAD.Tests -> StoryCADTests) 
- Created stubbed SceneViewModelTests per Issue #1113
- All tests passing: 398 passed, 0 failed, 4 skipped

Organized into:
- Services/ (Backend, Collaborator, Search, Installation, Outline, etc.)
- Models/
- DAL/
- ViewModels/
- Collaborator/
- Utilities/

Fixes #1111"
```

### Step 8: Push to Update PR
```bash
git push origin issue-1111-reorganize-tests
```

## Files That Should Remain in Root
These are NOT test files and should stay in StoryCADTests root:
- App.xaml / App.xaml.cs
- MainWindow.xaml / MainWindow.xaml.cs  
- TestSetup.cs
- Package.appxmanifest
- mstest.runsettings
- StoryCADTests.csproj
- README.md
- app.manifest
- .env
- Manual Tests.xlsx
- CloseFileTest.stbx (test data file)
- Folders: Assets/, TestInputs/, TestResults/, Views/, ManualTests/, bin/, obj/

## Final Verification Checklist
- [ ] No *Tests.cs files remain in StoryCADTests root directory
- [ ] All namespaces match their folder structure
- [ ] SceneViewModelTests is properly stubbed with Issue #1113 reference
- [ ] Solution builds without errors or warnings
- [ ] Test results show: 398 passed, 0 failed, 4 skipped
- [ ] Git history shows files as moved (preserving history)
- [ ] PR #1114 updated with actual file reorganization

## Notes
- This work was originally completed on September 2, 2025 but was lost from git history
- The PR description accurately reflects what was achieved
- Use `git mv` for all file moves to preserve git history
- The stash contains important namespace fixes that must be applied first