# Issue 1246: Update and Release New Samples and API Guidance

## Current Status
- **Phase**: Phase 1-6 Complete (Infrastructure + Getting Started + API Reference + Samples + Concepts/Operations/Advanced). Website content complete.
- **Date**: 2026-02-06
- **Branch**: `issue-1246-api-docs` (StoryCADAPI repo)
- **Branch**: `issue-1246-xml-docs` (StoryCAD repo - XML doc generation + comment fixes)
- **Local folder renamed**: `/mnt/d/dev/src/API-Samples` → `/mnt/d/dev/src/StoryCADAPI`
- **Website nav**: Home, Getting Started, Concepts, Operations, Samples, Advanced, API Reference (7 sections)

### Cross-Reference: Issue #61 (Prompt Generator)

Issue #61 (Collaborator repo) researched deployment options and arrived at a solution that applies to both issues:

**Selected approach:** Blazor WASM + GitHub Pages on public StoryCADAPI repo
- docfx for API documentation
- Blazor WASM for interactive samples (Prompt Generator)
- GitHub Pages hosting (free)
- Follows [pollydocs.org](https://www.pollydocs.org/) model

**Repo renamed:** `API-Samples` → `StoryCADAPI` (2026-02-04)

**Deployment guide:** See `/mnt/d/dev/src/Collaborator/devdocs/issue_61_prompt_generator/blazor_github_pages_deployment.md`

This resolves **D2 (Documentation Hosting)** - use GitHub Pages with docfx + Blazor WASM.

## Session Log

### 2026-01-24
- Set up PIE template on GitHub issue
- Created devdocs/api_samples folder
- Investigated existing resources:
  - NuGet package (StoryCADLib 3.3.0 published, 4.0.0 in codebase)
  - API-Samples repo (private, outdated samples)
  - Outliner directory (3 variants, OutlinerAlt is latest)
- Documented findings in issue_1246_research.md
- Discussed guidance strategy with user:
  - Three tracks: external examples, internal XML docs, sample projects
  - Educational mission framing
  - StoryCAD API vs Semantic Kernel as consumer (naming clarification)
- Defined 7 research items (R1-R7)
- **Starting research phase** - launching parallel agents for R1, R2, R6
- **Research phase complete** - all 7 items researched
- Added open question: API documentation hosting strategy (website options)
- Discussed headless package architecture:
  - Uno.Fonts.Fluent only used in XAML (UI icons)
  - Options: Extract Core package vs conditional compilation vs just document
  - Could separate: StoryCADLib.Core, StoryCADLib.SemanticKernel, StoryCADLib (UI)
  - Recommendation: Start with documentation, consider extraction later
- Brainstormed sample project ideas:
  - Core samples (9): Story Graph Basics, Scene Workflow, Beat Sheets, Validation, Metrics, etc.
  - SK/LLM samples (9): Scene Expansion, Diagnostic Agent, Voice Check, Critique, etc.
  - Our additions: Project Tracker, Prompt Generator, Automated Critique
  - Suggested v1 minimum: 5 samples (3 core + 2 SK)
- Built decision framework (D1-D5) with dependency chain
- Documented current NuGet consumers (~1.9K downloads, mostly internal/Outliner)
- Created issue #1277 for publicity/outreach (depends on #1246)
- **Session paused** - next step: decisions on D1 (package architecture) and D2 (doc hosting)

### 2026-02-04 - Repo Cleanup & Rename
- **Repo renamed:** `API-Samples` → `StoryCADAPI` on GitHub
- Updated local git remote to new URL
- **Security cleanup for public release:**
  - Removed hardcoded API key placeholder from `StoryCADChat/Program.cs`
  - Replaced with `Environment.GetEnvironmentVariable("OPENAI_API_KEY")`
  - Added `.env` patterns to `.gitignore`
  - Created `.env.example` documenting required environment variables
  - Updated `README.md` with environment setup instructions
- **Repo is now ready to make public** - no secrets, clear setup docs
- Cross-reference: Work done during Collaborator #61 session

### 2026-02-04 - Phase 1: Infrastructure Setup
- Created website plan document (`website_plan.md`)
- Established branching model for StoryCADAPI (matching StoryCAD: main + dev)
- Created branches: `dev` from `main`, `issue-1246-api-docs` from `dev`
- Committed security cleanup changes
- **Created docfx infrastructure:**
  - `docs/docfx.json` - configuration for API reference generation
  - `docs/filterConfig.yml` - API filter rules
  - `docs/toc.yml` - top-level navigation
  - `docs/index.md` - homepage with quick example
  - `docs/getting-started/` - installation, quick-start, hello-world docs
  - `docs/.nojekyll` - disable Jekyll processing
  - `docs/404.md` - not found page
- **Created GitHub Actions workflow:**
  - `.github/workflows/deploy-docs.yml` - build-only (deploy commented out)
  - Workflow can access org-private NuGet packages
  - Uploads build artifact for inspection without deploying
- Added `.gitattributes` for future Blazor WASM support
- **Phase 1 complete** - infrastructure ready, not yet deployed

### 2026-02-04 - API Reference & Documentation Polish
- **Attempted auto-generated API docs** - blocked by build issues:
  - Initially tried `dotnet build StoryCADLib.csproj` directly (wrong approach)
  - Correct approach is MSBuild via solution, but discovered stale `StoryCADLib/global.json`
  - StoryCADLib has its own global.json with `Uno.Sdk: 6.0.67`
  - Solution root has `Uno.Sdk: 6.4.53`
  - This causes version mismatch when building project directly
- **Pivoted to manual API documentation** (like pollydocs.org):
  - `docs/api/index.md` - API overview with quick reference tables
  - `docs/api/semantic-kernel-api.md` - Full method documentation with examples
  - `docs/api/operation-result.md` - OperationResult<T> pattern documentation
  - `docs/api/toc.yml` - Section navigation
  - Removed unused `docs/filterConfig.yml`
- **Updated README.md**:
  - Correct namespace (`StoryCADLib.Services.API` not `StoryCAD.Services.API`)
  - Proper DI initialization (`ServiceLocator.Initialize(headless: true)`)
  - Added NuGet and license badges
  - Documented OperationResult pattern and headless mode
  - Cross-links to related projects
- **Local preview tested**:
  - `docfx docs/docfx.json --serve` runs successfully
  - Site renders at http://localhost:8080
  - Navigation pane, search, modern template all working
- **Renamed local folder**: `API-Samples` → `StoryCADAPI`

### 2026-02-04 - XML Documentation Generation (continued)
- **Resolved stale global.json issue:**
  - Deleted `/mnt/d/dev/src/StoryCAD/StoryCADLib/global.json` (had Uno.Sdk 6.0.67)
  - Solution root global.json (Uno.Sdk 6.4.53) now applies correctly
  - Builds now work without version conflicts
- **Enabled XML documentation in StoryCADLib.csproj:**
  - Added `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
  - Added `<NoWarn>$(NoWarn);CS1591</NoWarn>` to suppress missing-doc warnings
- **Build successful:**
  - `StoryCADLib.xml` generated (217KB)
  - Contains documentation for Collaborator, Services, ViewModels, Models
  - StoryCADApi methods are documented
- **Fixed all malformed XML comments:**
  - `StoryCADAPI.cs` - Added `<summary>` tags, escaped `<T>` as `&lt;T&gt;`, removed orphaned comment
  - `INavigationService.cs` - Fixed `</Param>` to `</param>` (case mismatch)
  - `ShellViewModel.cs` - Fixed missing `>` in `</summary>`
  - `OutlineService.cs` - Added missing param tags for Model, Parent, Index parameters
  - `ElementPicker.xaml.cs` - Removed orphaned XML comment at end of class
- **Rebuild verified** - No XML documentation warnings, clean build

### 2026-02-05 - HeadlessTest Console App (Verification)
- **Created HeadlessTest project** in `/mnt/d/dev/src/StoryCADAPI/HeadlessTest/`
  - `global.json` - Uno.Sdk 6.4.53, .NET 10.0.102
  - `HeadlessTest.csproj` - Uno.Sdk console app, `net10.0-desktop`, ProjectReference to StoryCADLib
  - `Program.cs` - 7-step verification of headless API usage
- **All 7 checks passed:**
  1. Headless initialization (`BootStrapper.Initialise()`)
  2. DI resolution of `StoryCADApi`
  3. `CreateEmptyOutline` - 3 element GUIDs returned
  4. Element inspection - StoryOverview, TrashCan, Narrative View Folder
  5. `WriteOutline` - 1407-byte `.stbx` file written to disk
  6. `OpenOutline` - read back successfully
  7. Round-trip verification - element count matches
- **Key finding:** `UnoSingleProject=true` required in csproj for `net10.0-desktop` to resolve correctly on Windows
- **Build/run command:** `dotnet run -f net10.0-desktop` (via Windows dotnet.exe from WSL)
- **Empirically proves** non-UI consumers can use StoryCADLib 4.0 via ProjectReference

### 2026-02-05 - Release Roadmap Clarification
- **Resolved D1 (Package Architecture):** Single package (StoryCADLib), publish to NuGet.org at launch. No Core/SK split needed.
- **Defined two-phase release roadmap:**
  - **Phase 1 (NOW):** All development uses ProjectReference. StoryCADAPI repo stays private. No NuGet package needed.
  - **Phase 2 (at 4.0 store release):** Publish NuGet, switch to PackageReference, make repo public, deploy website.
- **Key insight:** NuGet publication is gated on 4.0 shipping to Windows Store + Apple Store, not on architecture decisions. This unblocks all Phase 1 work immediately.
- Updated status log with roadmap, resolved decisions, and updated dependency chain.

### 2026-02-05 - v1 Sample Projects (5 samples)
- **Built 5 educational console app samples** in `/mnt/d/dev/src/StoryCADAPI/`:
  1. **StoryGraphBasics** - Create, populate, link, save & reload an outline (10 API methods)
  2. **StoryMetrics** - Analytics dashboard: element counts, character appearances, setting usage
  3. **ConsistencyValidation** - 6 validation checks detecting 7 intentional issues (orphan chars, unused settings, etc.)
  4. **StoryDiagnosticAgent** - SK + LLM diagnoses pacing, passive protagonist, plot holes
  5. **AutomatedCritique** - SK + LLM scores outline against 5 craft criteria with rubric
- **Each sample is self-contained** (4 files: global.json, .csproj, Program.cs, README.md)
- **All 5 build with 0 errors, 0 warnings** (Windows dotnet.exe, `net10.0-desktop`)
- **Samples 1-3 run successfully** with verified output
- **Samples 4-5 build-only** (require OPENAI_API_KEY at runtime)
- **SK samples use** `Environment.GetEnvironmentVariable("OPENAI_API_KEY")` — no hardcoded keys
- **API coverage across samples:**
  - Core: CreateEmptyOutline, AddElement, UpdateElementProperties, AddCastMember, AddRelationship, GetAllElements, GetElementsByType, GetElement, GetStoryElement, WriteOutline, OpenOutline, SearchForReferences, GetKeyQuestionElements, GetKeyQuestions
  - SK: Kernel.CreateBuilder().AddOpenAIChatCompletion(), IChatCompletionService, ChatHistory

### 2026-02-06 - Sample Documentation for docfx Website
- **Created `docs/samples/` section** in StoryCADAPI docfx site (7 new files):
  - `toc.yml` — Section navigation (6 entries)
  - `index.md` — Overview with sample table, prerequisites, run instructions, API coverage matrix
  - `story-graph-basics.md` — Create/link/query/persist workflow
  - `story-metrics.md` — Analytics and query APIs
  - `consistency-validation.md` — Validation checks and orphan detection
  - `story-diagnostic-agent.md` — SK + LLM structural diagnosis
  - `automated-critique.md` — SK + LLM scored craft evaluation
- **Updated 2 existing files:**
  - `docs/toc.yml` — Added Samples to top-level nav (between Getting Started and API Reference)
  - `docs/index.md` — Fixed broken Samples link (was GitHub URL, now `samples/index.md`)
- **docfx build verified** — 0 errors, all 7 HTML pages generated in `_site/samples/`
- **Local preview confirmed** — pages render correctly, sidebar nav works, TIP/NOTE callouts display
- **Known:** "View source on GitHub" links return 404 while repo is private; will resolve at Phase 2

### 2026-02-06 - Concepts, Operations, and Advanced Sections
- **Created `docs/concepts/` section** (4 new files):
  - `toc.yml` — Section navigation (3 entries)
  - `story-model.md` — StoryModel structure, StoryElementCollection, GUID-based cross-referencing, ExplorerView/NarratorView/TrashView trees, singleton elements, file format
  - `element-types.md` — All 11 StoryItemType values with model class, key properties organized by category, value types (plain string vs GUID reference vs GetExamples enum), containment rules, quick-reference tables for UpdateElementProperty
  - `resource-data.md` — Overview of 6 resource categories (example lists, conflict builder, key questions, master plots, stock scenes, beat sheets) with access patterns and cross-references to Operations pages
- **Created `docs/operations/` section** (4 new files):
  - `toc.yml` — Section navigation (3 entries)
  - `search.md` — SearchForText, SearchForReferences, SearchInSubtree, RemoveReferences with full examples, return structures, and practical patterns (orphan detection, dependency graphs)
  - `resource-workflows.md` — Step-by-step code examples for conflict builder (4-step), key questions (2-step), master plots (3-step), stock scenes (2-step), plus combined workflow example
  - `beat-sheets.md` — Complete beat sheet workflow: browse templates, preview, apply to Problem, view structure, assign elements, customize (create/update/delete/move), save/load .stbeat files, complete end-to-end example
- **Created `docs/advanced/` section** (3 new files):
  - `toc.yml` — Section navigation (2 entries)
  - `semantic-kernel.md` — [KernelFunction] attribute pattern, plugin registration, automatic function calling, complete agent example with system prompt, multi-step agent patterns, system prompt tips, NuGet dependencies
  - `migration-3x-to-4x.md` — Breaking changes (namespace, class rename, TFM, StoryWorld), before/after code for each, new API methods summary, file format compatibility, migration checklist
- **Updated `docs/toc.yml`** — Reordered to 7 sections following developer learning path: Home → Getting Started → Concepts → Operations → Samples → Advanced → API Reference
- **Updated `docs/index.md`** — Added links to Concepts, Operations, and Advanced in Documentation area
- **Updated `README.md`** — Added `taskkill.exe /F /IM docfx.exe` instruction for stopping docfx server from WSL
- **docfx build verified** — 0 errors, all 7 top-level nav entries render, all section sidebars work
- **Committed and pushed** to `issue-1246-api-docs` branch (14 files, 1,708 lines)
- **Content sourced from**: StoryCADApi.cs (method signatures, XML docs, [KernelFunction] attributes), all element model classes (CharacterModel, SceneModel, ProblemModel, SettingModel, OverviewModel, StoryWorldModel, FolderModel, WebModel, TrashCanModel), StoryModel.cs, StoryElement.cs, StoryItemType.cs, OperationResult.cs, existing samples

### Future Enhancements
- Add logo/branding
- Diagrams (StoryModel structure, GUID reference graph)
- Custom CSS styling

## Release Roadmap

### Phase 1: Development (NOW — pre-launch)

**Context:** StoryCADAPI repo is private. API website is not published. All work uses ProjectReference.

- Samples reference StoryCADLib via `ProjectReference` to `../../StoryCAD/StoryCADLib/StoryCADLib.csproj`
- CI in StoryCADAPI checks out both repos (pattern already in `deploy-docs.yml`)
- No NuGet package needed during this phase

**Work items:**
- [x] Build and test sample projects (v1 set: 3 core + 2 SK)
- [x] Build API website (docfx) — infrastructure, getting started, API reference, samples sections done
- [ ] Add Blazor WASM interactive samples (Prompt Generator — see Collaborator #61)
- [ ] Write advanced topics / guides (migration, SK integration, error handling)
- [ ] Configure docfx to use auto-generated XML docs (optional enhancement)
- [ ] Add build/run instructions for StoryCADAPI users (website content + repo README)

### Phase 2: Public Launch (when 4.0 ships to Windows Store + Apple Store)

- Add NuGet metadata to `StoryCADLib.csproj` (PackageId, Authors, License, etc.)
- Publish StoryCADLib 4.0.0 to NuGet.org (public)
- Switch all samples from `ProjectReference` to `<PackageReference Include="StoryCADLib" Version="4.0.0" />`
- Make StoryCADAPI repo public
- Enable GitHub Pages deployment for API website
- Announce via issue #1277 (publicity/outreach)

### Completed Milestones
- [x] Determine how StoryCADLib 4.0 (UNO) project creation works (R1 - headless works)
- [x] Decide on documentation hosting strategy → **GitHub Pages + docfx + Blazor WASM** (resolved via #61)
- [x] **Phase 1: Infrastructure** - docfx config, GitHub Actions, docs skeleton
- [x] **Phase 2: Getting Started content** - installation, quick-start, hello-world
- [x] **Phase 3: API Reference** - manual markdown docs + XML generation now working
- [x] Resolve stale `StoryCADLib/global.json` - **DELETED**, builds work now
- [x] Enable XML documentation generation in StoryCADLib
- [x] Publish StoryCADAPI repo publicly (ready - secrets removed, repo renamed)
- [x] Decide on package architecture - **Single package (StoryCADLib), publish at launch** (D1 resolved 2026-02-05)
- [x] **Sample documentation** - 5 sample pages + overview in docfx website (2026-02-06)

## Open Issues
1. ~~StoryCADLib 4.0.0 not yet published to NuGet (still 3.3.0)~~ — **Phase 2**: NuGet.org publish gated on 4.0 Windows Store + Apple Store release
2. ~~Samples reference outdated versions (3.0.8, net8.0)~~ — **Phase 1**: samples use ProjectReference during development; switch to PackageReference at launch
3. ~~UNO platform changes may affect API consumers~~ (R1: headless works fine)
4. ~~Packaging issue documented in Outliner workaround~~ (R6: fixed in 4.0)
5. ~~Stale `StoryCADLib/global.json`~~ - **DELETED** (2026-02-04)
6. ~~Malformed XML doc comments~~ - **FIXED** (2026-02-04)
7. NuGet.org publish gated on 4.0 Windows Store + Apple Store release (see Phase 2 roadmap)

## Decision Framework

### Tier 1: Foundational — RESOLVED

| ID | Decision | Resolution | Date |
|----|----------|-----------|------|
| D1 | **Package Architecture** | **Single package (StoryCADLib)**, publish to NuGet.org at launch. No Core/SK split needed now. | 2026-02-05 |
| D2 | **Documentation Hosting** | **GitHub Pages + docfx + Blazor WASM** (resolved via #61) | 2026-02-04 |
| D4 | **Samples in separate public repo?** | **Yes — StoryCADAPI repo.** Made public at Phase 2 (store release). | 2026-02-05 |
| D5 | **v1 = 5 samples (3 core + 2 SK)?** | **Yes.** See v1 sample list in research doc. | 2026-02-05 |

### Tier 2: Strategy Confirmation (research has recommendations, need sign-off)

| ID | Decision | Recommendation | If Yes, Then... |
|----|----------|----------------|-----------------|
| D3 | **Python via .stbx JSON?** | Yes - pure Python library | Document JSON schema first |

### Tier 3: Execution (after Tier 1-2 resolved)

| Action | Depends On |
|--------|------------|
| Publish NuGet 4.0.0 | 4.0 store release (Windows + Apple) |
| Build v1 samples | D4, D5 (use ProjectReference during Phase 1) |
| Write API documentation | D2 (resolved) |
| Document .stbx JSON schema | D3 |
| Add XML doc comments to API | Done (2026-02-04) |

### Dependency Chain

```
4.0 Store Release (Windows Store + Apple Store)
 └─► NuGet 4.0.0 publication
 └─► Samples switch from ProjectReference to PackageReference
 └─► StoryCADAPI repo made public
 └─► GitHub Pages deployment enabled
 └─► Announce via #1277 (publicity/outreach)

D3 + D4 + D5 confirmed
 └─► Can start building samples and docs now (Phase 1, ProjectReference)
```

**Current state**: D1 and D2 resolved. NuGet publish gated on 4.0 store release, not on architecture decisions. Phase 1 work can proceed immediately.

## Notes
- OutlinerAlt is best reference for real-world API usage
- ~~API-Samples repo is private - needs to be released~~ → Renamed to **StoryCADAPI**, ready to make public

---

## Research

See `issue_1246_research.md` for completed research items (R1-R7).
