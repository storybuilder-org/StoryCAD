# Issue 1246: Research & Information Gathering

## NuGet Package: StoryCADLib

**URL**: https://www.nuget.org/packages/StoryCADLib

| Property | Published | Codebase |
|----------|-----------|----------|
| Version | 3.3.0 | 4.0.0 |
| Target | net9.0-windows10.0.22621 | net10.0-desktop + net10.0-windows10.0.22621 |
| Platform | Windows-only | UNO (cross-platform) |

### Version History
| Version | Downloads | Date |
|---------|-----------|------|
| 3.3.0 | 296 | Sep 19, 2025 |
| 3.2.1 | 285 | Aug 7, 2025 |
| 3.2.0 | 204 | Jul 7, 2025 |
| 3.1.0 | 193 | Apr 25, 2025 |
| 3.0.8 | 271 | Apr 14, 2025 |
| 3.0.0 | 255 | Apr 10, 2025 |

**Total Downloads**: ~1.9K (~5/day average)
**Owner**: StoryBuilderFoundation

### Current Consumers

**NuGet page states**: "Not used by any NuGet packages or popular GitHub repositories"

**Known consumers**:
- **Outliner** (internal project) - references 3.3.0
- StoryCAD team CI/CD builds

**Likely sources of ~1.9K downloads**:
- Internal development and CI/CD
- Outliner project
- Occasional curious developers
- NuGet indexing/bots

**Implication**: Low external adoption gives freedom for breaking changes in 4.0 (namespace, class naming) without significant impact.

### Dependencies (3.3.0)
- CommunityToolkit.Mvvm >= 8.4.0
- Microsoft.SemanticKernel >= 1.61.0
- Microsoft.WindowsAppSDK >= 1.7.250606001
- MySql.Data >= 9.4.0
- NLog >= 6.0.3
- Octokit >= 14.0.0

---

## API-Samples Repository

**Location**: `/mnt/d/dev/src/API-Samples/`
**Status**: Private (never released)
**Source**: repos.md line 47-49

### Contents
1. **StoryCADChat/** - Console app using SK + LLM for natural language interaction
   - `Program.cs` - 49 lines
   - `StoryCADChat.csproj` - References StoryCADLib 3.0.8, net8.0

2. **Blank Project/StoryCAD-API-Starter/** - Minimal starter template
   - `Program.cs` - 12 lines (just BootStrapper.Initialise())
   - References StoryCADLib 3.0.8, net8.0

### README Sample Code
```csharp
using StoryCAD.Services.API;

var api = new StoryCADApi();
var outlineResult = await api.CreateEmptyOutline("Title", "Author", "0");
var updateResult = api.UpdateElementProperty(guid, "Name", "New Name");
var writeResult = await api.WriteOutline("path.stbx");
```

---

## Outliner Directory

**Location**: `/mnt/d/dev/src/Outliner/`
**Git Repo**: No

### Variants (by modification date)

| Folder | Modified | Structure | StoryCADLib |
|--------|----------|-----------|-------------|
| Outliner/Outliner/ | Sep 20 | Single project | - |
| Outliner shell/ProseToOutline/ | Sep 20 | Single project | 3.3.0 |
| **OutlinerAlt/** | **Sep 21** | Full solution | **3.3.0** |

### OutlinerAlt (Latest)
**Purpose**: Prose-to-outline converter using LLM

**Projects**:
- `Outliner/` - WinUI executable
- `OutlinerLib/` - Core library
- `OutlinerTests/` - Tests

**Architecture** (from Documentation/Architecture.md):
1. **Phase 1 (LLM Analysis)**: Single-pass extraction via Semantic Kernel
2. **Phase 2 (API Construction)**: Build outline via StoryCADLib API

**Processing Order**:
1. Story Overview (root)
2. Characters (referenced by others)
3. Settings (scenes occur in them)
4. Scenes (reference chars/settings)
5. Problems (reference all types)

**Known Issue** (from csproj):
```xml
<!-- Workaround for StoryCADLib content files being in wrong location in package -->
<Target Name="RemoveStoryCADLibContentFiles" ...>
```

---

## Key Source Files

| File | Location | Purpose |
|------|----------|---------|
| StoryCADAPI.cs | StoryCADLib/Services/API/ | Main API (77KB) |
| OperationResult.cs | StoryCADLib/Services/API/ | Result pattern |
| BootStrapper.cs | StoryCADLib/Services/IoC/ | DI initialization |

---

## Gap Analysis

### Version Gaps
- Published NuGet: 3.3.0 → Codebase: 4.0.0
- Samples target: net8.0 → Current: net10.0
- Semantic Kernel in samples: 1.41.0 → Current: 1.61.0+

### Platform Gap
- Published: Windows-only (WinAppSDK)
- Codebase: UNO Platform (cross-platform)

### Documentation Gaps
- No public API documentation
- No migration guide (3.x → 4.0)
- No UNO-specific guidance for API consumers

### Packaging Issue
- StoryCADLib content files in wrong location
- Documented workaround exists in Outliner projects

---

## Open Research Questions

### Can non-UNO apps consume StoryCADLib 4.0?

**Status**: ✅ RESEARCHED

**Answer**: **Yes, with minor caveats.**

#### Dependencies Analysis

| Package | UNO-Specific? |
|---------|---------------|
| CommunityToolkit.Mvvm | No |
| Microsoft.SemanticKernel* | No |
| MySql.Data, NLog, Octokit, etc. | No |
| **Uno.Fonts.Fluent** | **Yes (only one)** |

**Critical Finding**: Only ONE UNO-specific package exists (Uno.Fonts.Fluent, for UI fonts).

#### Initialization

```csharp
// Minimal initialization for non-UI app
BootStrapper.Initialise(headless: true);
var api = Ioc.Default.GetRequiredService<StoryCADApi>();
```

- Default `headless = true` requires no UI context
- All services register successfully without UI
- Test suite runs in headless mode

#### API Compatibility

| Aspect | Status |
|--------|--------|
| Core API (StoryCADAPI) | ✅ Fully Compatible |
| OutlineService | ✅ Fully Compatible |
| Data Models | ✅ Fully Compatible |
| Semantic Kernel Integration | ✅ Fully Compatible |
| File Operations | ✅ Fully Compatible |
| Windowing Service | ⚠️ Non-functional in headless |
| UI Dialogs | ⚠️ Returns Primary result |
| File Picker UI | ⚠️ Requires own implementation |

#### Recommendations

1. **Works now**: Console apps, ASP.NET, web APIs can consume StoryCADLib
2. **Workarounds**: Implement own file picker (pass paths to API)
3. **Optional improvements**:
   - Extract Uno.Fonts.Fluent as conditional
   - Create headless-specific NuGet package
   - Add headless mode documentation

**Sources**:
- StoryCADLib.csproj: `/mnt/d/dev/src/StoryCAD/StoryCADLib/StoryCADLib.csproj`
- ServiceLocator.cs: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Services/IoC/ServiceLocator.cs`
- StoryCADAPI.cs: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Services/API/StoryCADAPI.cs`

#### Architectural Options for Headless Support

**Current State**: Single package with UI and headless code mixed. Uno.Fonts.Fluent only used in XAML for icons.

**Option A: Extract StoryCADLib.Core**

```
StoryCADLib.Core (new)
├── Models/ (StoryModel, elements)
├── DAL/ (StoryIO, JSONResourceLoader)
├── Services/Outline/ (OutlineService)
└── Services/API/ (IStoryCADApi - core operations, no SK attributes)

StoryCADLib.SemanticKernel (new, optional)
├── StoryCADApi (wraps IStoryCADApi)
├── [KernelFunction] attributes
└── References: Core + Microsoft.SemanticKernel

StoryCADLib (refactored)
├── UI, ViewModels, XAML, Windowing
└── References: Core
```

**Benefits**:
- Core has minimal dependencies (no UI, no SK)
- SK integration is opt-in
- Clearer naming for non-SK users
- .NET console/ASP.NET apps get lean package

**Trade-offs**:

| Approach | Effort | Maintenance | Benefit |
|----------|--------|-------------|---------|
| Extract Core + SK packages | High | 3 packages to publish | Clean separation |
| Conditional compilation | Medium | Complex builds | Single source |
| Just document headless | Low | None | Unused deps acceptable |

**Recommendation**: Start with documentation (low effort). Consider extraction if demand grows or during major refactor.

**Note**: Python access strategy (R3) is unaffected - Python uses .stbx JSON directly, not .NET packages.

---

### Exemplary API Documentation Models

**Status**: ✅ RESEARCHED

#### Top Models Identified

| Library | Key Pattern | Adopt For |
|---------|-------------|-----------|
| **UNO Platform Samples** | Feature-based organization, progressive complexity | Sample structure |
| **Semantic Kernel** | Test-based samples, multi-language (C#/Python/Java) | Sample implementation |
| **Azure SDK Guidelines** | Naming conventions, OperationResult pattern, README-first | API design |
| **FluentValidation** | Progressive learning path (8 sections) | Doc structure |
| **Polly** | Plain language before technical details | Accessibility |
| **Serilog** | Minimal 5-line examples, copy-paste ready | Quick start |
| **CommunityToolkit.Mvvm** | Before/after comparisons, platform-specific samples | Comparison docs |

#### Key Recommendations

| Priority | Recommendation | Source |
|----------|----------------|--------|
| 1 | XML docs with `<summary>`, `<param>`, `<returns>`, `<example>` | Microsoft |
| 2 | Progressive disclosure (quick start → basic → advanced) | FluentValidation |
| 3 | Test-based sample projects that always compile | Semantic Kernel |
| 4 | Plain language before technical details | Polly |
| 5 | Standardize naming (Create, Get, Update, Delete) | Azure SDK |
| 6 | Copy-paste ready examples for every method | Serilog |
| 7 | "When to use this" guidance | MediatR |
| 8 | Side-by-side comparisons (API vs. direct service) | CommunityToolkit |

#### Recommended Doc Structure

```
/docs/api/
├── README.md                    # Overview and quick start
├── getting-started.md           # Installation and first API call
├── concepts/                    # OperationResult, StoryModel, element types
├── operations/                  # Outline, element, navigation, file ops
├── samples/                     # HelloWorld, CreateStory, AIAgent, Batch
├── advanced/                    # Error handling, testing, extensibility
└── reference/                   # Auto-generated from XML docs
```

#### Progressive Disclosure Levels

- **Level 1 (30 sec)**: Install, create instance, CreateEmptyOutline()
- **Level 2 (5 min)**: Create elements, navigate, modify, save
- **Level 3 (15+ min)**: Templates, batch ops, error handling, SK integration
- **Level 4 (reference)**: Complete API reference, edge cases

#### References

1. UNO Platform Samples. https://github.com/unoplatform/Uno.Samples
2. Semantic Kernel Documentation. https://learn.microsoft.com/en-us/semantic-kernel/
3. Azure SDK .NET Design Guidelines. https://azure.github.io/azure-sdk/dotnet_introduction.html
4. FluentValidation Documentation. https://docs.fluentvalidation.net/
5. Polly Documentation. https://www.pollydocs.org/
6. Serilog Getting Started. https://github.com/serilog/serilog/wiki/Getting-Started
7. CommunityToolkit.Mvvm. https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/
8. Microsoft XML Documentation. https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/

---

### Python Access Strategy

**Status**: ✅ RESEARCHED

**Recommendation**: **Direct .stbx JSON** (Option 4) as primary approach.

#### Options Evaluated

| Option | Complexity | Educational Value | Recommendation |
|--------|------------|-------------------|----------------|
| REST Wrapper | Medium-High | Medium | Future enhancement |
| gRPC Service | High | Low-Medium | Not recommended |
| Python.NET | Medium | Medium | Not recommended |
| **Direct .stbx JSON** | **Low-Medium** | **High** | **Primary approach** |

#### Why Direct .stbx JSON?

1. **Educational Alignment**: Aligns with StoryBuilder Foundation's teaching mission
2. **Zero Dependencies**: No .NET runtime or servers required
3. **Format Already Exists**: Well-structured JSON with discriminator-based types
4. **Pythonic**: Can design truly native Python API

#### .stbx Format Structure

```json
{
  "CreatedVersion": "...",
  "LastVersion": "...",
  "FlattenedExplorerView": [{"Uuid": "...", "ParentUuid": "..."}],
  "Elements": [
    {"Type": "Character|Scene|Problem|...", "GUID": "...", "Name": "...", ...}
  ]
}
```

**Element Types**: StoryOverview, Problem, Character, Setting, Scene, Folder, Notes, Web, TrashCan, Section, StoryWorld

#### Proposed Python API

```python
from storycad import Outline, Character

outline = Outline.load("my_story.stbx")
for char in outline.characters:
    print(f"{char.name}: {char.role}")
outline.save("my_story.stbx")
```

#### Implementation Roadmap

1. **Phase 1**: Document JSON Schema (2-3 weeks)
2. **Phase 2**: Create `storycad` Python package (4-6 weeks)
3. **Phase 3**: Educational materials (2-4 weeks)
4. **Phase 4** (Future): REST API if demand warrants

#### References

1. Python.NET Official Site. http://pythonnet.github.io/
2. jsonschema Python Library. https://python-jsonschema.readthedocs.io/
3. Pydantic JSON Schema. https://docs.pydantic.dev/latest/concepts/json_schema/

---

### Migration Path (3.x → 4.0)

**Status**: ✅ RESEARCHED

#### Breaking Changes

| Aspect | 3.x | 4.0 | Action |
|--------|-----|-----|--------|
| **Namespace** | `StoryCAD.Services.API` | `StoryCADLib.Services.API` | Update imports |
| **Class Name** | `StoryCADAPI` | `StoryCADApi` | Rename (casing) |
| **.NET Target** | `net8.0-windows10.0.22621` | `net10.0-*` | Update TFM |
| **Platform** | Windows only | Cross-platform (UNO) | No code change |

#### Migration Steps

1. **Update project file**:
```xml
<TargetFramework>net10.0-windows10.0.22621</TargetFramework>
<PackageReference Include="StoryCADLib" Version="4.0.0" />
```

2. **Update using statements**:
```csharp
// Old
using StoryCAD.Services.API;
// New
using StoryCADLib.Services.API;
```

3. **Update class references**:
```csharp
var api = Ioc.Default.GetRequiredService<StoryCADApi>(); // lowercase 'i'
```

#### New Features in 4.0

- **Beat Sheets API**: 19 new methods for beat sheet management
- **Enhanced Search**: `SearchForText()`, `SearchForReferences()`, `SearchInSubtree()`
- **Trash/Restore**: `RestoreFromTrash()`, `EmptyTrash()`
- **Resource APIs**: Full access to ControlData, ListData, ToolsData

#### Backward Compatibility

- Core CRUD operations unchanged
- OperationResult<T> pattern identical
- BootStrapper.Initialise() unchanged

**Sources**:
- StoryCADAPI.cs: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Services/API/StoryCADAPI.cs`
- IStoryCADAPI.cs: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Services/Collaborator/Contracts/IStoryCADAPI.cs`
- API-Samples: `/mnt/d/dev/src/API-Samples/`

---

### Sample Distribution Strategy

**Status**: ✅ RESEARCHED

**Recommendation**: **Separate public repository** with strong cross-linking.

#### Options Evaluated

| Option | Pros | Cons | Used By |
|--------|------|------|---------|
| **Separate public repo** | Clean separation, independent releases, beginner-friendly | Version sync burden, discoverability | Microsoft, UNO Platform |
| Part of main repo | Always synced, single repo | Increases size, mixes concerns | Semantic Kernel |
| NuGet samples folder | Auto-delivered, version-matched | Increases package size, not standard | Not common |
| Combination | Maximum discoverability | Highest maintenance, confusing | - |

#### Recommended Structure

```
storybuilder-org/API-Samples (public)
├── README.md                      # Links to main repo, NuGet, docs
├── Directory.Packages.props       # Central package management
├── /getting-started/
│   └── HelloStoryCAD/             # Minimal sample
├── /scenarios/
│   ├── StoryCADChat/              # Natural language
│   └── ProseToOutline/            # Document conversion
└── /advanced/
    └── CustomPlugins/             # Extensions
```

#### Cross-Linking Strategy

- **NuGet README**: Link to API-Samples repo
- **Main StoryCAD README**: Link to API-Samples
- **API-Samples README**: Link back to NuGet and main repo
- **User Manual**: "Samples" section linking to repo

#### Version Synchronization

Use Central Package Management (`Directory.Packages.props`):
```xml
<PackageVersion Include="StoryCADLib" Version="4.0.0" />
```

#### Action Items

1. Make API-Samples repository public
2. Add cross-links to all READMEs
3. Implement Central Package Management
4. Add governance docs (CONTRIBUTING.md, CODE_OF_CONDUCT.md)

#### References

1. dotnet/samples. https://github.com/dotnet/samples
2. unoplatform/Uno.Samples. https://github.com/unoplatform/Uno.Samples
3. NuGet Central Package Management. https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management

---

### Packaging Issue Status

**Status**: ✅ RESEARCHED

**Answer**: **Fixed in 4.0.** Workaround only needed for 3.3.0 consumers.

#### Root Cause (3.3.0)

In 3.3.0, Assets/Install files were configured as `Content` with `PackagePath`:
```xml
<Content Update="Assets\*.*" PackagePath="contentFiles\any\net8.0-windows10.0-22621\$(PackageId)\Assets\..." />
```

This caused files to copy to wrong locations in consuming projects (`StoryCADLib\Assets\Install`).

#### Fix (4.0)

Commit `7e4bf92c` (2025-11-04) changed all Assets/Install from `Content` to `EmbeddedResource`:
```xml
<Content Remove="Assets\Install\*" />
<EmbeddedResource Include="Assets\Install\Bibliog.txt" />
<EmbeddedResource Include="Assets\Install\Controls.json" />
<!-- ... all Install files ... -->
```

This is correct because `JSONResourceLoader` uses `GetManifestResourceStream()` which requires embedded resources.

#### Impact

| Version | Status | Workaround Needed? |
|---------|--------|-------------------|
| 3.3.0 (NuGet) | Bug present | Yes |
| 4.0.0 (codebase) | Fixed | No |

#### Recommendations

1. Publish 4.0 from commit `7e4bf92c` or later
2. Consuming projects on 3.3.0 should upgrade to 4.0+
3. Outliner workaround can be removed after upgrade

**Sources**:
- StoryCADLib.csproj: `/mnt/d/dev/src/StoryCAD/StoryCADLib/StoryCADLib.csproj` (lines 200-252)
- JSONResourceLoader.cs: `/mnt/d/dev/src/StoryCAD/StoryCADLib/DAL/JSONResourceLoader.cs`
- Fix commit: `7e4bf92c` (2025-11-04)

---

### Snippet Style Strategy

**Decision**: Both standalone and cohesive samples.

- **Standalone snippets**: Per-method, referenced/inlined in API guide
- **Cohesive samples**: Build complete outline step-by-step, show workflow

---

### API Documentation Hosting Strategy

**Question**: Should we have a dedicated website for the API, or add to existing infrastructure?

**Status**: Open - collecting options

**Options**:
| Option | Example | Pros | Cons |
|--------|---------|------|------|
| Add to storybuilder.org | - | Single brand, no new infra | Different audience |
| Add to user manual | storybuilder-org.github.io/StoryCAD/ | Already exists, Jekyll | Mixes audiences |
| Dedicated docs site | pollydocs.org | Clean separation, polished | More to maintain |
| README-driven in repo | GitHub markdown | Simplest, co-located | Less polished |

**Examples to study**:
- Polly: https://www.pollydocs.org/ (dedicated site)
- Semantic Kernel: Microsoft Learn (integrated)
- UNO Platform: platform.uno/docs (dedicated)

**Considerations**:
- Educational mission (accessibility for learners)
- Small team (maintenance burden)
- Auto-generated API reference (DocFX, etc.)
- Cross-linking with samples repo

---

## Sample Project Ideas

### A. Core API Samples (No LLM)

| # | Sample | Description | Value |
|---|--------|-------------|-------|
| 1 | **Story Graph Basics** | Create story, add elements, link them, save/load | Canonical "hello world" |
| 2 | **Scene-Centric Workflow** | Create scenes, assign POV/goal/conflict/outcome, reorder, validate | Scene-first philosophy |
| 3 | **Master Plot + Beat Sheet** | Define master plot, attach beat sheet, map beats → scenes | Structure from data |
| 4 | **Subplots & Sequences** | Create subplot problems, associate scenes, query by subplot | Multi-thread narratives |
| 5 | **Consistency & Validation** | Detect: scenes without conflict, characters before intro, unresolved problems | Story integrity engine |
| 6 | **Story Metrics** | Scenes per act, POV distribution, character frequency, conflict density | Dashboards/analytics |
| 7 | **Import / Export** | Export to JSON, import from JSON, Scrivener-like import | Interop and tooling |
| 8 | **Project Tracker** | Scan folder for .stbx files, summarize: titles, word counts, completeness | Portfolio management |
| 9 | **Prompt Generator** | Generate prompts from Master Plots, Dramatic Situations, Stock Scenes | Writing exercises |

### B. Semantic Kernel / LLM Samples

| # | Sample | Description | Value |
|---|--------|-------------|-------|
| 10 | **Scene Expansion** | Input goal/conflict/outcome → prose draft (not auto-stored) | Assistive, not generative-first |
| 11 | **Beat-to-Scene Generator** | Beat description → scene skeleton, user approves before insert | LLM as structured data generator |
| 12 | **Story Diagnostic Agent** | Analyze structure: sagging middle, missing reversals, passive protagonist | Coaching use case |
| 13 | **Character Voice Check** | Profile + dialogue → inconsistencies in tone, diction, motivation | Strong differentiator |
| 14 | **Subplot Balance Analyzer** | Detect dominance/neglect, recommend redistribution | Structural intelligence |
| 15 | **"What If" Branching** | Clone scene, ask LLM for alternate outcome, compare impact | Non-destructive experimentation |
| 16 | **Multi-Level Summary** | Scene → sequence → subplot → full story (uses API structure) | Shows why model matters |
| 17 | **Automated Critique** | Analyze against craft principles, checklist feedback | Educational feedback |
| 18 | **Prose to Outline** | Convert txt/docx/pdf → StoryCAD outline | Content pipeline |

### C. Packaging Recommendations

- **One folder per sample** with minimal console app
- **Single README**: "What this demonstrates"
- **Paired samples** where applicable:
  - `SceneValidation.Basic`
  - `SceneValidation.SemanticKernel`
- **API-first** - avoid UI samples initially
- **Central Package Management** for version sync

### D. Suggested v1 Minimum Set

Core (no LLM):
1. Story Graph Basics (hello world)
2. Story Metrics (analytics)
3. Consistency & Validation (integrity)

With SK/LLM:
4. Story Diagnostic Agent (coaching)
5. Automated Critique (educational)

This gives a balanced introduction without overwhelming maintenance.
