#!/usr/bin/env bash
# Publish StoryCADLib for the host platform, stage its runtime into
# storycad/runtime/, then build a platform wheel via `python -m build`.
#
# Users installing the resulting wheel must NOT need the .NET SDK. This
# script is the only place dotnet is invoked.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$HERE/../../.." && pwd)"
CSPROJ="$REPO_ROOT/StoryCADLib/StoryCADLib.csproj"
RUNTIME_DIR="$HERE/storycad/runtime"
FRAMEWORK="net10.0-desktop"

# ─── detect host RID ───────────────────────────────────────────────────────
uname_s="$(uname -s)"
uname_m="$(uname -m)"
case "$uname_s" in
  Darwin)
    case "$uname_m" in
      arm64)         RID="osx-arm64" ;;
      x86_64)        RID="osx-x64" ;;
      *) echo "Unsupported macOS arch: $uname_m" >&2; exit 1 ;;
    esac ;;
  Linux)
    case "$uname_m" in
      x86_64)        RID="linux-x64" ;;
      aarch64|arm64) RID="linux-arm64" ;;
      *) echo "Unsupported Linux arch: $uname_m" >&2; exit 1 ;;
    esac ;;
  MINGW*|MSYS*|CYGWIN*)
    case "$uname_m" in
      x86_64) RID="win-x64" ;;
      arm64)  RID="win-arm64" ;;
      *) echo "Unsupported Windows arch: $uname_m" >&2; exit 1 ;;
    esac ;;
  *) echo "Unsupported OS: $uname_s" >&2; exit 1 ;;
esac

echo "==> Host RID: $RID"
echo "==> Project:  $CSPROJ"
echo "==> Framework: $FRAMEWORK"

# ─── publish ───────────────────────────────────────────────────────────────
PUBLISH_OUT="$(mktemp -d)"
trap 'rm -rf "$PUBLISH_OUT"' EXIT

echo "==> dotnet publish -> $PUBLISH_OUT"
dotnet publish "$CSPROJ" \
  -c Release \
  -f "$FRAMEWORK" \
  -r "$RID" \
  --self-contained false \
  -o "$PUBLISH_OUT"

if [[ ! -f "$PUBLISH_OUT/StoryCADLib.dll" ]]; then
  echo "ERROR: StoryCADLib.dll not found in publish output ($PUBLISH_OUT)" >&2
  exit 1
fi

# ─── stage runtime ─────────────────────────────────────────────────────────
echo "==> Staging runtime into $RUNTIME_DIR"
rm -rf "$RUNTIME_DIR"
mkdir -p "$RUNTIME_DIR"

# Top-level binaries and config files.
find "$PUBLISH_OUT" -maxdepth 1 -type f \
  \( -name '*.dll' -o -name '*.dylib' -o -name '*.so' -o -name '*.json' \) \
  -exec cp -p {} "$RUNTIME_DIR/" \;

# Native sub-runtimes (RID-specific shared libs).
if [[ -d "$PUBLISH_OUT/runtimes" ]]; then
  cp -R "$PUBLISH_OUT/runtimes" "$RUNTIME_DIR/runtimes"
fi

STAGED_COUNT=$(find "$RUNTIME_DIR" -type f | wc -l | tr -d ' ')
echo "==> Staged $STAGED_COUNT runtime files"

# ─── build wheel ───────────────────────────────────────────────────────────
PYTHON_BIN="${PYTHON:-}"
if [[ -z "$PYTHON_BIN" ]]; then
  if command -v python3 >/dev/null 2>&1; then
    PYTHON_BIN="python3"
  elif command -v python >/dev/null 2>&1; then
    PYTHON_BIN="python"
  else
    echo "ERROR: no python interpreter found (set \$PYTHON to override)" >&2
    exit 1
  fi
fi
echo "==> $PYTHON_BIN -m build --wheel"
cd "$HERE"
"$PYTHON_BIN" -m build --wheel

echo "==> Done. Artifacts:"
ls -lh "$HERE/dist" | tail -n +2
