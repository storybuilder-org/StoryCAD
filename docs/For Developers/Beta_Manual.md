---
title: Beta Manual
layout: default
nav_enabled: true
nav_order: 119
parent: For Developers
has_toc: false
---
## Beta Manual

StoryCAD has two versions of its user manual:

- **Production Manual** — [storybuilder-org.github.io/StoryCAD](https://storybuilder-org.github.io/StoryCAD/)
- **Beta Manual** — [storybuilder-org.github.io/BetaManual](https://storybuilder-org.github.io/BetaManual/)

### What's the difference?

The **production manual** is published from the `main` branch of the StoryCAD repository. It documents the current stable release and is what most users should read.

The **beta manual** is a separate copy that automatically stays in sync with the `dev` branch. Whenever documentation changes are pushed to `dev`, a GitHub Actions workflow copies the updated files to the [BetaManual](https://github.com/storybuilder-org/BetaManual) repository, where they are published to GitHub Pages. This means the beta manual always reflects the latest in-development documentation, including pages for features that haven't been released yet.

### When to use each

| Manual | Audience | Content |
|--------|----------|---------|
| Production | All users | Matches the current stable release |
| Beta | Beta testers and developers | Matches the `dev` branch; may include incomplete or upcoming features |

### How to tell them apart

The beta manual has two visual indicators so readers always know which version they're looking at:

- **Title**: The site header reads "StoryCAD Beta Manual" instead of "StoryCAD Manual".
- **Footer banner**: Every page displays a notice at the bottom saying "You are viewing the beta version of this manual!" with a link back to the main repository.

These are configured in the BetaManual repository's `_config.yml` and are preserved automatically during syncs — only the documentation content is copied, not the Jekyll configuration.

### How the sync works

A workflow in `.github/workflows/sync-beta-manual.yml` runs whenever a push to `dev` touches files in the `docs/` directory. It copies the documentation content (markdown files, images, etc.) into the BetaManual repository while preserving that repository's own Jekyll configuration, home page, and Gemfile.

The sync can also be triggered manually from the Actions tab if needed.
