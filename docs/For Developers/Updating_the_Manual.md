---
title: Updating the Manual
layout: default
nav_enabled: true
nav_order: 109
parent: For Developers
has_toc: false
---

## Updating the Manual

The StoryCAD user manual is built using [Jekyll](https://jekyllrb.com/) with the [Just the Docs](https://just-the-docs.github.io/just-the-docs/) theme. Documentation files are written in Markdown and located in the `docs` folder.

## Serving Documentation Locally

You can preview the manual locally before publishing. Changes to markdown files will automatically refresh in the browser.

### Prerequisites

- **Ruby** - Install via [RubyInstaller](https://rubyinstaller.org/) (Windows) or `brew install ruby` (macOS)
- **PowerShell Core** - Comes with Windows; install via `brew install powershell` on macOS

### Running the Documentation Server

From the repository root, run:

```powershell
pwsh serve-docs.ps1
```

Or via MSBuild:

```bash
dotnet msbuild -t:ServeDocs
```

The documentation will be served at [http://localhost:4000](http://localhost:4000) with live reload enabled.

### Custom Port

To use a different port:

```powershell
pwsh serve-docs.ps1 -Port 8080
```

## Building Static Documentation

To build the documentation without serving (outputs to `_site` folder):

```bash
dotnet msbuild -t:BuildDocs
```

## Adding New Pages

1. Create a new `.md` file in the appropriate `docs` subfolder
2. Add front matter at the top of the file:

```yaml
---
title: Your Page Title
layout: default
nav_enabled: true
nav_order: 110
parent: Parent Section Name
has_toc: false
---
```

3. Write your content in Markdown
4. Preview locally with `pwsh serve-docs.ps1`

## Publishing

Documentation is automatically published to GitHub Pages when changes are merged to the main branch.