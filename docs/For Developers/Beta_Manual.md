---
title: Manual Workflow
layout: default
nav_enabled: true
nav_order: 119
parent: For Developers
has_toc: false
---
## Manual Workflow

StoryCAD has two versions of its user manual, each published from a different branch:

| Manual | URL | Branch | Audience |
|--------|-----|--------|----------|
| **Production** | [manual.storybuilder.org](https://manual.storybuilder.org/) | `main` | All users — matches the current stable release |
| **Beta** | [beta.manual.storybuilder.org](https://beta.manual.storybuilder.org/) | `dev` | Beta testers and developers — may include incomplete or upcoming features |

Both are Jekyll sites using the Just the Docs theme, hosted on GitHub Pages with custom domains.

### How publishing works

**Production manual** is published directly from the `main` branch via GitHub Pages.

**Beta manual** syncs automatically. A GitHub Actions workflow (`.github/workflows/sync-beta-manual.yml`) runs whenever a push to `dev` touches files in `docs/`. It copies the documentation content into the [BetaManual](https://github.com/storybuilder-org/BetaManual) repository, which publishes to GitHub Pages. Only doc content is synced — the BetaManual repo keeps its own `_config.yml`, home page, and Gemfile.

The sync can also be triggered manually from the Actions tab.

### Making changes

**Beta/dev changes** (new features, upcoming docs):

1. Work in the `dev` branch (or a feature branch merged to `dev`)
2. Edit files in `docs/`
3. Commit and push to `dev`
4. Changes appear on [beta.manual.storybuilder.org](https://beta.manual.storybuilder.org/) automatically

**Production hotfixes** (typos, urgent corrections to the live manual):

1. Branch off `main`
2. Make the fix and open a PR to `main`
3. After merge, rebase `dev` on `main` to pick up the change

### How to tell them apart

The beta manual has two visual indicators:

- **Title**: The site header reads "StoryCAD Beta Manual" instead of "StoryCAD Manual".
- **Footer banner**: Every page shows a notice saying "You are viewing the beta version of this manual!" with a link to the production manual.

These are configured in the BetaManual repository's `_config.yml` and preserved during syncs.

### Local preview

Preview documentation changes locally before pushing:

```
pwsh serve-docs.ps1
```

Or directly with Jekyll:

```
bundle exec jekyll serve --livereload
```

The site will be available at `http://localhost:4000`. The script checks for Ruby and Bundler and installs dependencies automatically.

### Beta builds and the manual

StoryCAD automatically points users to the correct manual based on the build type. The CI workflow (`build-release.yml`) passes `-p:IsBetaBuild=true` for every build **except** a full production release (a `workflow_dispatch` with `skip_release` unchecked). This defines the `BETA_BUILD` compile constant, which sets the default for the **"Use beta documentation"** preference in Settings.

| Build trigger | Beta flag | Default manual |
| ------------- | --------- | -------------- |
| Push to `dev` or `main` | Yes | Beta |
| Pull request | Yes | Beta |
| `workflow_dispatch` (test run / `skip_release: true`) | Yes | Beta |
| `workflow_dispatch` (full release / `skip_release: false`) | No | Production |

Users can always override this in **Preferences > Use beta documentation**, regardless of the build type.

### Note on deprecated repos

The **ManualTest** and **StoryBuilder-Manual** repositories are deprecated. All manual work now happens in the main StoryCAD repository's `docs/` folder.
