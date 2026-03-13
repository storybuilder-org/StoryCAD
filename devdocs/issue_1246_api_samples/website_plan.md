# StoryCADAPI Documentation Website Plan

## Overview

Create a documentation website for StoryCADLib API, hosted on GitHub Pages at the StoryCADAPI repository. Follows the [pollydocs.org](https://www.pollydocs.org/) model: docfx-generated documentation with sidebar navigation, progressive disclosure, and professional developer experience.

**Target URL:** `https://storybuilder-org.github.io/StoryCADAPI/`
**Repository:** `storybuilder-org/StoryCADAPI` (currently private, renamed from API-Samples)

---

## Phase 1: Infrastructure Setup

### 1.1 Repository Structure

```
StoryCADAPI/
├── .github/
│   └── workflows/
│       └── deploy-docs.yml          # GitHub Actions workflow
├── .gitattributes                    # *.js binary (for future Blazor)
├── docs/                             # docfx source
│   ├── docfx.json                    # docfx configuration
│   ├── toc.yml                       # Top-level table of contents
│   ├── index.md                      # Homepage
│   ├── getting-started/
│   │   ├── toc.yml
│   │   ├── index.md                  # Installation & first API call
│   │   ├── quick-start.md            # 5-minute tutorial
│   │   └── hello-world.md            # Minimal working example
│   ├── concepts/
│   │   ├── toc.yml
│   │   ├── operation-result.md       # OperationResult<T> pattern
│   │   ├── story-model.md            # StoryModel structure
│   │   ├── element-types.md          # Character, Scene, Problem, etc.
│   │   └── headless-mode.md          # Using API without UI
│   ├── operations/
│   │   ├── toc.yml
│   │   ├── outline-operations.md     # Create, open, save outlines
│   │   ├── element-operations.md     # CRUD for story elements
│   │   ├── navigation.md             # Tree navigation
│   │   ├── search.md                 # Search operations
│   │   └── beat-sheets.md            # Beat sheet API
│   ├── samples/
│   │   ├── toc.yml
│   │   ├── index.md                  # Samples overview
│   │   └── [sample docs]             # Per-sample documentation
│   ├── advanced/
│   │   ├── toc.yml
│   │   ├── error-handling.md         # Working with OperationResult
│   │   ├── semantic-kernel.md        # SK integration
│   │   ├── testing.md                # Testing with the API
│   │   └── migration-3x-to-4x.md     # Migration guide
│   ├── api/                          # Auto-generated API reference
│   │   └── .gitkeep
│   └── templates/                    # Custom docfx templates (optional)
├── samples/                          # Sample projects (runnable code)
│   ├── Directory.Packages.props      # Central package management
│   ├── getting-started/
│   │   └── HelloStoryCAD/
│   ├── core/
│   │   ├── StoryGraphBasics/
│   │   ├── StoryMetrics/
│   │   └── ConsistencyValidation/
│   └── semantic-kernel/
│       ├── StoryDiagnosticAgent/
│       └── AutomatedCritique/
├── README.md                         # Repository overview
├── CONTRIBUTING.md                   # Contribution guidelines
└── LICENSE
```

### 1.2 docfx.json Configuration

```json
{
  "$schema": "https://raw.githubusercontent.com/dotnet/docfx/main/schemas/docfx.schema.json",
  "metadata": [
    {
      "src": [
        {
          "files": ["**/StoryCADLib.csproj"],
          "src": "../StoryCAD"
        }
      ],
      "dest": "api",
      "includePrivateMembers": false,
      "disableGitFeatures": false,
      "disableDefaultFilter": false,
      "properties": {
        "TargetFramework": "net10.0-windows10.0.22621"
      },
      "filter": "filterConfig.yml"
    }
  ],
  "build": {
    "content": [
      { "files": ["**/*.{md,yml}"], "src": "." }
    ],
    "resource": [
      { "files": ["images/**"] }
    ],
    "output": "_site",
    "template": ["default", "modern"],
    "globalMetadata": {
      "_appTitle": "StoryCAD API",
      "_appName": "StoryCAD API",
      "_appFooter": "StoryBuilder Foundation - GNU GPL v3",
      "_enableSearch": true,
      "_enableNewTab": true,
      "_disableContribution": false,
      "_gitContribute": {
        "repo": "https://github.com/storybuilder-org/StoryCADAPI",
        "branch": "main",
        "path": "docs"
      }
    },
    "fileMetadata": {
      "_layout": { "api/**": "Reference" }
    },
    "markdownEngineName": "markdig",
    "noLangKeyword": false,
    "keepFileLink": false,
    "cleanupCacheHistory": false,
    "disableGitFeatures": false
  }
}
```

### 1.3 GitHub Actions Workflow

Create `.github/workflows/deploy-docs.yml`:

```yaml
name: Deploy Documentation

on:
  push:
    branches: [main]
    paths:
      - 'docs/**'
      - 'samples/**'
      - '.github/workflows/deploy-docs.yml'
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: "pages"
  cancel-in-progress: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout StoryCADAPI
        uses: actions/checkout@v4

      - name: Checkout StoryCAD (for API metadata)
        uses: actions/checkout@v4
        with:
          repository: storybuilder-org/StoryCAD
          path: StoryCAD
          ref: main

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install docfx
        run: dotnet tool install -g docfx

      - name: Build documentation
        run: docfx docs/docfx.json

      - name: Upload artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: docs/_site

  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    needs: build
    steps:
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
```

### 1.4 Required Static Files

**docs/.nojekyll** (empty file):
```
# Create empty file - prevents Jekyll processing
```

**docs/404.md**:
```markdown
---
_layout: landing
---
# Page Not Found

The page you requested could not be found.

[Return to documentation home](/)
```

---

## Phase 2: Content - Getting Started

### 2.1 Homepage (docs/index.md)

```markdown
---
_layout: landing
---
# StoryCAD API

Build story outlines programmatically with the StoryCADLib API.

[!INCLUDE[Quick Example](getting-started/quick-example.md)]

## Features

- **Outline Management** - Create, open, save story outlines (.stbx files)
- **Element CRUD** - Add characters, scenes, problems, settings
- **Structure Tools** - Beat sheets, master plots, dramatic situations
- **Headless Mode** - Use in console apps, web APIs, batch processing
- **Semantic Kernel** - Ready-made functions for AI agents

## Getting Started

- [Installation](getting-started/index.md)
- [Quick Start Tutorial](getting-started/quick-start.md)
- [Hello World Sample](getting-started/hello-world.md)

## Learn More

- [Concepts](concepts/index.md) - Core data structures and patterns
- [Operations](operations/index.md) - API method reference by category
- [Samples](samples/index.md) - Complete working examples
- [API Reference](api/index.md) - Full technical documentation
```

### 2.2 Installation (docs/getting-started/index.md)

Content covering:
- NuGet installation (`dotnet add package StoryCADLib`)
- Target framework requirements (net10.0)
- Headless initialization (`BootStrapper.Initialise(headless: true)`)
- Getting the API instance via DI

### 2.3 Quick Start (docs/getting-started/quick-start.md)

5-minute tutorial covering:
1. Create empty outline
2. Add a character
3. Add a scene
4. Save to file
5. Verify with StoryCAD application

---

## Phase 3: API Reference (XML Documentation)

### 3.1 Enable XML Documentation in StoryCADLib

Update `StoryCADLib.csproj`:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn> <!-- Suppress missing XML doc warnings initially -->
</PropertyGroup>
```

### 3.2 Priority XML Documentation Targets

Add XML documentation to these files first:

| Priority | File | Methods |
|----------|------|---------|
| 1 | StoryCADAPI.cs | All public methods (~80) |
| 2 | OperationResult.cs | IsSuccess, Payload, ErrorMessage |
| 3 | BootStrapper.cs | Initialise() |
| 4 | Key Models | StoryModel, StoryNodeItem, element types |

### 3.3 XML Documentation Template

```csharp
/// <summary>
/// Creates a new empty story outline with the specified metadata.
/// </summary>
/// <param name="title">The title of the story.</param>
/// <param name="author">The author's name.</param>
/// <param name="templateIndex">Template index: "0" for blank, "1" for basic template.</param>
/// <returns>
/// An <see cref="OperationResult{T}"/> containing a list of created element GUIDs on success,
/// or an error message on failure.
/// </returns>
/// <example>
/// <code>
/// var result = await api.CreateEmptyOutline("My Story", "Jane Doe", "0");
/// if (result.IsSuccess)
/// {
///     Console.WriteLine($"Created {result.Payload.Count} elements");
/// }
/// </code>
/// </example>
public async Task<OperationResult<List<Guid>>> CreateEmptyOutline(string title, string author, string templateIndex)
```

---

## Phase 4: Sample Projects

### 4.1 Central Package Management

Create `samples/Directory.Packages.props`:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="StoryCADLib" Version="4.0.0" />
    <PackageVersion Include="Microsoft.SemanticKernel" Version="1.61.0" />
  </ItemGroup>
</Project>
```

### 4.2 V1 Sample Set (5 Samples)

| # | Sample | Type | Purpose |
|---|--------|------|---------|
| 1 | HelloStoryCAD | Core | Minimal example - create outline, add character, save |
| 2 | StoryGraphBasics | Core | Full CRUD, parent-child relationships, tree navigation |
| 3 | StoryMetrics | Core | Query outline for statistics (scene count, POV distribution) |
| 4 | ConsistencyValidation | Core | Detect story problems (scenes without conflict, etc.) |
| 5 | StoryDiagnosticAgent | SK | LLM-powered story analysis and recommendations |

### 4.3 Sample Project Template

Each sample folder contains:
```
SampleName/
├── SampleName.csproj
├── Program.cs
├── README.md           # What it demonstrates, how to run
└── sample-output.txt   # Expected output (for verification)
```

### 4.4 Migrate Existing Samples

Update existing samples to 4.0:

| Current | Action |
|---------|--------|
| StoryCADChat | Update to net10.0, StoryCADLib 4.0, fix namespace |
| Blank Project | Rename to HelloStoryCAD, update references |

---

## Phase 5: Advanced Topics

### 5.1 Migration Guide (3.x → 4.0)

Content from research:
- Namespace change: `StoryCAD.Services.API` → `StoryCADLib.Services.API`
- Class rename: `StoryCADAPI` → `StoryCADApi`
- TFM update: net8.0/net9.0 → net10.0
- New features available in 4.0

### 5.2 Semantic Kernel Integration

Document using StoryCADLib with SK agents:
- Available KernelFunctions
- Creating a kernel with StoryCAD plugins
- Example agent prompts

### 5.3 Error Handling

Deep dive on OperationResult pattern:
- Checking success/failure
- Accessing payloads
- Common error messages and resolutions

---

## Implementation Checklist

### Phase 1: Infrastructure (Week 1)
- [ ] Create branch `issue-1246-api-docs`
- [ ] Set up docfx.json in StoryCADAPI repo
- [ ] Create GitHub Actions workflow
- [ ] Add .nojekyll and 404.md
- [ ] Configure GitHub Pages (Settings → Pages → GitHub Actions)
- [ ] Verify empty site deploys successfully

### Phase 2: Getting Started (Week 2)
- [ ] Write index.md (homepage)
- [ ] Write getting-started/index.md (installation)
- [ ] Write getting-started/quick-start.md (5-min tutorial)
- [ ] Write getting-started/hello-world.md (minimal example)
- [ ] Create HelloStoryCAD sample project

### Phase 3: API Reference (Week 3)
- [ ] Enable XML documentation in StoryCADLib.csproj
- [ ] Add XML docs to StoryCADAPI.cs (priority methods)
- [ ] Add XML docs to OperationResult.cs
- [ ] Add XML docs to BootStrapper.cs
- [ ] Verify API reference generates in docfx

### Phase 4: Samples (Week 4)
- [ ] Set up Directory.Packages.props
- [ ] Migrate StoryCADChat to 4.0
- [ ] Create StoryGraphBasics sample
- [ ] Create StoryMetrics sample
- [ ] Create ConsistencyValidation sample
- [ ] Document each sample in docs/samples/

### Phase 5: Advanced & Polish (Week 5)
- [ ] Write migration-3x-to-4x.md
- [ ] Write semantic-kernel.md
- [ ] Write error-handling.md
- [ ] Add cross-links between docs and samples
- [ ] Review and polish all content
- [ ] Make repository public

### Phase 6: Release
- [ ] Publish StoryCADLib 4.0.0 to NuGet
- [ ] Update README with links to docs site
- [ ] Add cross-links from StoryCAD main repo
- [ ] Announce availability

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| StoryCADLib 4.0 codebase | Ready | In dev branch |
| StoryCADLib 4.0 NuGet | Not published | Needed for samples to reference |
| StoryCADAPI repo public | Private | Make public when ready |
| GitHub Pages enabled | Not configured | Configure after first push |

---

## Success Criteria

- [ ] Documentation site accessible at GitHub Pages URL
- [ ] API reference auto-generated from XML docs
- [ ] All 5 v1 samples compile and run
- [ ] Getting started guide enables new users in <5 minutes
- [ ] "Edit this page" links work for community contributions
- [ ] Search functionality works
- [ ] Mobile-responsive layout

---

## References

- [docfx Documentation](https://dotnet.github.io/docfx/)
- [pollydocs.org](https://www.pollydocs.org/) - Model site
- [GitHub Pages Documentation](https://docs.github.com/en/pages)
- [Blazor WASM Deployment Guide](file:///mnt/d/dev/src/Collaborator/devdocs/issue_61_prompt_generator/blazor_github_pages_deployment.md) - For future Prompt Generator integration
