# Publishing `StoryCADLib` to PyPI (maintainer guide)

This is the maintainer-facing companion to the user docs (`README.md`,
`GETTING_STARTED.md`, `docs/For Developers/Python_Bindings.md`). It covers how
the Python package is built, versioned, and published — and the gotchas you
won't want to re-derive at release time.

- **PyPI project:** [`StoryCADLib`](https://pypi.org/project/StoryCADLib/)
  (PyPI normalizes the name to `storycadlib`).
- **Import name:** `storycad` — intentionally different from the distribution
  name (like `pip install Pillow` → `import PIL`). `pip install StoryCADLib`,
  then `import storycad`.
- **What ships:** a platform wheel bundling a *framework-dependent* publish of
  `StoryCADLib.dll` + its managed/native runtime, plus the `pythonnet`
  dependency. End users need the **.NET 10 runtime** installed (not the SDK).

---

## Publishing mechanisms

Two paths exist. **Trusted Publishing is the one to use for real releases.**

### 1. Trusted Publishing / OIDC (primary — no token)

The `pypi-publish` job in `.github/workflows/store-publish.yml` authenticates to
PyPI with a short-lived GitHub OIDC token — there is no API token to store or
leak. It only runs inside GitHub Actions (GitHub mints the token), so it cannot
be used from a laptop.

One-time setup (already done; recorded here for posterity / re-creation):

- **PyPI → Account → Publishing → pending publisher** (creating it reserves the
  name immediately):
  - PyPI Project Name: `StoryCADLib`
  - Owner: `storybuilder-org`
  - Repository: `StoryCAD`
  - Workflow: `store-publish.yml`
  - Environment: `pypi`
- **GitHub repo → Settings → Environments → `pypi`** must exist (the job
  declares `environment: name: pypi`). Add release-only protection rules if
  desired.

### 2. Manual / local upload (fallback only)

Useful for claiming the name or a quick smoke test. **Covers only the platform
of the machine you build on.**

```bash
cd StoryCADLib/bindings/python
./build-wheel.sh                       # host-platform wheel only
pip install twine
python3 -m twine check dist/*.whl
python3 -m twine upload dist/*.whl     # prompts for token; do NOT pass -p
```

- Creating a brand-new project needs an **account-scoped** token
  (project-scoped tokens only exist once the project does).
- **Token hygiene:** never paste a token into a chat or onto a command line
  (`-p` leaks it into shell history and `ps`). Use the interactive prompt or
  `TWINE_PASSWORD` in your own shell, and **revoke immediately** if exposed.
  Trusted Publishing needs no token at all — prefer it.
- You can add the *other* platforms' wheels to the same version later (from CI
  or other machines) with `twine upload --skip-existing dist/*.whl`.

---

## Cutting a release (the automated path)

1. **Run `Build & Release`** (Actions → workflow_dispatch) with:
   - `version` = e.g. `4.1.1` (or `v4.1.1`)
   - **`skip_release = false`**  ⚠️ it defaults to `true` (build-only); you must
     flip it to actually create a release.
   - This runs `scripts/Set-Version.ps1`, which stamps the version into
     `Package.appxmanifest`, `StoryCADLib.csproj`
     (`Version`/`AssemblyVersion`/`FileVersion`), and the macOS `Info.plist`.
   - The `python-wheels` job builds wheels for **all four platforms**
     (`linux-x64`, `linux-arm64`, `osx-arm64`, `win-x64`), installs each wheel,
     and runs `pytest`. (Push/PR runs only smoke-test `linux-x64`.)
   - The `release` job creates a GitHub Release tagged with the version and
     attaches the installers, `*.nupkg`, and `storycad-pypi-wheel-*/*.whl`.
2. **The published Release triggers `Publish to Stores`** (`on: release`). Its
   `pypi-publish` job downloads the `*.whl` assets and pushes them to PyPI via
   OIDC (`skip-existing: true`, so re-runs are idempotent).
   - You can also run `Publish to Stores` manually (workflow_dispatch) with the
     release `tag` and the `publish_pypi` toggle.

---

## Versioning

The wheel version is **derived from `StoryCADLib.dll`'s `FileVersion`**, read at
build time by `_meta.py` (PE VERSION_INFO via `pefile`), then normalized
(`_normalise_version` drops trailing `.0` to ≤3 components, so `4.1.1.0` →
`4.1.1`). So the package version tracks the library version by default.

**Pre-release / test builds** — set `STORYCAD_WHEEL_VERSION` (used verbatim, not
normalized). A .NET `FileVersion` is numeric-only (`Major.Minor.Build.Revision`)
and can't carry a pre-release suffix, which is the whole reason this override
exists:

```bash
STORYCAD_WHEEL_VERSION=4.1.1rc1  ./build-wheel.sh   # release candidate
STORYCAD_WHEEL_VERSION=4.1.1.dev1 ./build-wheel.sh  # dev build
```

PyPI rules to keep in mind:

- **Versions are immutable** — you can never reuse or overwrite a version
  (delete/yank doesn't free it). Pick carefully.
- Pre-releases (`devN`/`aN`/`bN`/`rcN`) sort **before** the final release and
  `pip` skips them unless you pass `--pre` (or pin the exact/pre version), e.g.
  `pip install --pre StoryCADLib==4.1.1rc1`.

---

## Known caveats / follow-ups

- **Wheel tags are narrower than they need to be.** `hatch_build.py` sets
  `pure_python=False` + `infer_tag=True`, so wheels are tagged
  `cpXY-cpXY-<platform>` (e.g. `cp313-cp313-macosx_26_0_arm64`) — they install
  **only** on that exact CPython minor and that platform/macOS floor. But the
  package advertises `requires-python >=3.10` and has **no CPython C-extension
  of its own** (its native part is the .NET runtime loaded at runtime via
  `pythonnet`). Net effect today: a wheel built on Python 3.13 won't install on
  3.10–3.12, and the macOS floor follows the build runner's OS version.
  *Follow-up:* emit a broader tag (one wheel per platform serving all supported
  Pythons) instead of building per-(python, platform). Until then, a single
  release covers only the Python version CI builds with, per platform.
- **End users need the .NET 10 runtime** installed (the wheel is
  framework-dependent, not self-contained).
- **Distribution name ≠ import name** (`StoryCADLib` vs `storycad`) — keep both
  consistent if either is ever renamed (`pyproject.toml` `name`, the
  `store-publish.yml` env URL, install docs, and the `storycad/` package dir).
