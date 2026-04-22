---
title: CI/CD Pipeline
layout: default
nav_enabled: true
nav_order: 119
parent: For Developers
has_toc: true
---

# CI/CD Pipeline

StoryCAD uses GitHub Actions for continuous integration, documentation deployment, and release distribution. This page documents all workflows, what they produce, and how to use them.

---

## Workflows Overview

| Workflow | File | Purpose |
|----------|------|---------|
| Build & Release | `build-release.yml` | Build, test, sign, and release for Windows and macOS |
| Deploy Jekyll site to Pages | `pages.yml` | Build and deploy the user manual to GitHub Pages |
| Sync Beta Manual | `sync-beta-manual.yml` | Sync docs from dev branch to the beta manual site |
| Publish to Stores | `store-publish.yml` | Submit Windows packages to the Microsoft Store and macOS packages to the Apple App Store |

---

## Build & Release

**File:** `.github/workflows/build-release.yml`

### Triggers

- **Push** to `main` or `dev` (ignores markdown, docs, and images)
- **Pull request** to `main` or `dev`
- **Manual** via `workflow_dispatch` with:
  - `version` (required): Tag/version string, e.g. `v4.0.0`
  - `skip_release` (optional, default: true): Build only, skip creating a GitHub release

### Platform Matrix

| Runner | RID | Architecture | Output |
|--------|-----|-------------|--------|
| `windows-latest` | win-x64 | x64 | MSIX package |
| `windows-latest` | win-x86 | x86 | MSIX package |
| `windows-11-arm` | win-arm64 | ARM64 | MSIX package |
| `macos-latest` | osx-arm64 | arm64 | PKG installer |

### What It Does

1. **Changelog generation** (release builds only): Fetches raw release notes from the GitHub API, then transforms them into a user-friendly format via OpenAI. Falls back to raw notes if the API key is unavailable.

2. **Build and test**: Restores packages, updates version numbers in `Package.appxmanifest` and `StoryCADLib.csproj`, runs tests on all platforms, then packages.

3. **Code signing**:
   - **Windows**: Decodes a base64-encoded PFX certificate from secrets and signs the MSIX package during build.
   - **macOS**: Creates a temporary keychain, imports Apple Distribution certificates, signs the app bundle and dylibs, then builds a signed PKG installer.

4. **Artifact upload**: Each platform/architecture uploads its package as a GitHub Actions artifact.

5. **Release creation** (when `skip_release` is false): Downloads all artifacts and creates a GitHub release with the generated changelog.

### Versioning

- **Release builds** (`workflow_dispatch` with `skip_release=false`): Uses the provided version string verbatim (e.g. `4.0.0`).
- **Draft/CI builds**: Uses `major.minor.run_number.65534` format, where `65534` indicates a non-release build.

### Artifacts

After a successful build, artifacts are available on the Actions run page:

- `storycad-msix-win-x64.zip` — Windows x64 MSIX package
- `storycad-msix-win-x86.zip` — Windows x86 MSIX package
- `storycad-msix-win-arm64.zip` — Windows ARM64 MSIX package
- `storycad-osx-arm64.pkg` — macOS PKG installer

### Required Secrets

| Secret | Purpose |
|--------|---------|
| `BASE64_ENCODED_PFX` | Windows code signing certificate (base64) |
| `PFX_PASSWORD` | Password for the PFX certificate |
| `ENV` | Environment variables file for the app |
| `OPENAI_API_KEY` | Optional: AI-enhanced changelog generation |
| `APPLE_CERTIFICATES_BASE64` | macOS signing certificates (base64) |
| `APPLE_CERT_PASSWORD` | Password for macOS certificates |
| `MACOS_PROVISIONING_PROFILE_BASE64` | macOS provisioning profile (base64) |

---

## Deploy Jekyll Site to Pages

**File:** `.github/workflows/pages.yml`

### Triggers

- **Push** to `main` or `gh-pages`
- **Pull request** (build only — deploy step is skipped)
- **Manual** via `workflow_dispatch`

### What It Does

Builds the Jekyll user manual site and deploys it to GitHub Pages at [manual.storybuilder.org](https://manual.storybuilder.org/). On pull requests the `build` job still runs as a sanity check, but the `deploy` job is gated on `github.event_name != 'pull_request'` so nothing publishes until the PR lands.

---

## Sync Beta Manual

**File:** `.github/workflows/sync-beta-manual.yml`

### Triggers

- **Push** to `dev` when files under `docs/` change
- **Manual** via `workflow_dispatch`

### What It Does

Syncs the `docs/` directory from the `dev` branch to the external [BetaManual repository](https://github.com/storybuilder-org/BetaManual) using `rsync`. This keeps the beta documentation site at [beta.manual.storybuilder.org](https://beta.manual.storybuilder.org/) up to date with the latest development changes.

### Required Secrets

| Secret | Purpose |
|--------|---------|
| `BETA_MANUAL_PAT` | Personal access token for the BetaManual repository |

---

## Publish to Stores

**File:** `.github/workflows/store-publish.yml`

### Triggers

- **Release published**: Automatically submits when a GitHub release is created
- **Manual** via `workflow_dispatch` with a release tag

### What It Does

Runs two independent jobs in parallel on release publication:

- **`microsoft-build` + `microsoft-publish`** — builds a Store-ready MSIX (`StoreUpload` mode with signing disabled, since the Store re-signs packages), then uses the Microsoft Store Submission REST API (`manage.devcenter.microsoft.com`) to create a draft submission, replace release notes on every listing, and upload the package. The submission is left as a draft in Partner Center for manual review and commit — the workflow does not publish.
- **`apple-publish`** — downloads the signed `storycad-osx-arm64.pkg` asset already attached to the release (produced by `build-release.yml` with Mac App Store certs), uploads it to App Store Connect via `xcrun altool`, then uses the App Store Connect API to patch `whatsNew` on the en-US draft version. The draft is left unsubmitted for manual review.

Each job is gated by its `runs-on` runner (`windows-latest` / `macos-latest`). Failures in one store do not block the other.

### Microsoft Store — Required Secrets

| Secret | Purpose |
|--------|---------|
| `PARTNER_CENTER_TENANT_ID` | Azure AD tenant ID |
| `PARTNER_CENTER_CLIENT_ID` | Azure AD app client ID |
| `PARTNER_CENTER_CLIENT_SECRET` | Azure AD app client secret |

The Microsoft Store app ID (`9PLBNHZV1XM2`) is hard-coded in the workflow as the `MS_APP_ID` environment variable; it is not a secret.

### Apple App Store — Required Secrets

| Secret | Purpose |
|--------|---------|
| `APP_STORE_CONNECT_KEY_ID` | 10-character key identifier |
| `APP_STORE_CONNECT_ISSUER_ID` | UUID shown above the keys list in App Store Connect |
| `APP_STORE_CONNECT_API_KEY_B64` | Base64-encoded `.p8` private key file |
| `APPLE_APP_ID` | Numeric Apple ID of the app record (App Information → Apple ID) |

---

## Finding Artifacts

### From GitHub Actions

1. Go to the [Actions tab](https://github.com/storybuilder-org/StoryCAD/actions)
2. Click on a workflow run
3. Scroll to the **Artifacts** section at the bottom
4. Download the package for your platform

### From GitHub Releases

1. Go to the [Releases page](https://github.com/storybuilder-org/StoryCAD/releases)
2. Find the release version
3. Download from the **Assets** section
