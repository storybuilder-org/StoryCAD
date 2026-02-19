#!/usr/bin/env bash
#
# review-1124-acceptance.sh
# Automated review of #1124 acceptance criteria for StoryCAD Release 4.0
#
# Usage: bash devdocs/review-1124-acceptance.sh [--skip-build] [--skip-tests]
#
# Runs from WSL against the StoryCAD repo at /mnt/d/dev/src/StoryCAD

set -uo pipefail

# --- Configuration ---
REPO="/mnt/d/dev/src/StoryCAD"
MSBUILD="/mnt/c/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe"
VSTEST="/mnt/c/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe"
TEST_DLL="StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll"

SKIP_BUILD=false
SKIP_TESTS=false
for arg in "$@"; do
    case "$arg" in
        --skip-build) SKIP_BUILD=true ;;
        --skip-tests) SKIP_TESTS=true ;;
    esac
done

# --- Counters ---
PASS=0; FAIL=0; SKIP=0; MANUAL=0

# --- Output helpers ---
pass()   { echo "  [PASS] $1"; PASS=$((PASS + 1)); }
fail()   { echo "  [FAIL] $1"; FAIL=$((FAIL + 1)); }
skip()   { echo "  [SKIP] $1"; SKIP=$((SKIP + 1)); }
manual() { echo "  [MANUAL] $1"; MANUAL=$((MANUAL + 1)); }

echo "=== #1124 Acceptance Criteria Automated Review ==="
echo "Date: $(date '+%Y-%m-%d %H:%M')"
echo "Repo: $REPO"
echo ""

# =====================================================================
# BUILD & DEPLOYMENT
# =====================================================================
echo "BUILD & DEPLOYMENT"

# Windows WinAppSDK build
if [ "$SKIP_BUILD" = true ]; then
    skip "Windows WinAppSDK build (--skip-build)"
else
    echo "  ... building Windows WinAppSDK (this takes a while) ..."
    WIN_SLN=$(wslpath -w "$REPO/StoryCAD.sln")
    if "$MSBUILD" "$WIN_SLN" -t:Build -p:Configuration=Debug -p:Platform=x64 \
        -nologo -verbosity:quiet -consoleloggerparameters:ErrorsOnly 2>&1 | tail -5; then
        pass "Windows WinAppSDK build succeeds"
    else
        fail "Windows WinAppSDK build failed"
    fi
fi

# macOS desktop build (can only run on macOS)
if [[ "$(uname)" == "Darwin" ]]; then
    if [ "$SKIP_BUILD" = true ]; then
        skip "macOS desktop build (--skip-build)"
    else
        echo "  ... building macOS desktop head ..."
        if dotnet build "$REPO/StoryCAD.sln" -f net10.0-desktop -c Debug --nologo -v q 2>&1 | tail -5; then
            pass "macOS desktop head builds"
        else
            fail "macOS desktop head build failed"
        fi
    fi
else
    skip "macOS desktop build (requires macOS)"
fi

# Version numbers
MANIFEST_VERSION=$(grep -oP 'Version="\K[^"]+' "$REPO/StoryCAD/Package.appxmanifest" | head -1 || echo "NOT FOUND")
LIB_VERSION=$(grep -oP '<Version>\K[^<]+' "$REPO/StoryCADLib/StoryCADLib.csproj" || echo "NOT FOUND")

if [[ "$MANIFEST_VERSION" == "4.0"* ]] && [[ "$LIB_VERSION" == "4.0"* ]]; then
    pass "Version 4.0 in Package.appxmanifest ($MANIFEST_VERSION) and StoryCADLib.csproj ($LIB_VERSION)"
else
    fail "Version mismatch: Package.appxmanifest=$MANIFEST_VERSION, StoryCADLib.csproj=$LIB_VERSION (expected 4.0.x)"
fi

# Distribution packages
manual "Windows MSIX package created for Microsoft Store"
manual "macOS application bundle (.app) created"
manual "macOS installer package (.pkg or .dmg) created"

echo ""

# =====================================================================
# TESTING & QUALITY
# =====================================================================
echo "TESTING & QUALITY"

# Windows test suite
if [ "$SKIP_TESTS" = true ]; then
    skip "Windows test suite (--skip-tests)"
else
    echo "  ... running test suite (this takes a while) ..."
    WIN_TEST_DLL=$(wslpath -w "$REPO/$TEST_DLL")
    TEST_OUTPUT=$("$VSTEST" "$WIN_TEST_DLL" 2>&1) || true
    TESTS_PASSED=$(echo "$TEST_OUTPUT" | grep -oP 'Passed:\s*\K\d+' || echo "0")
    TESTS_FAILED=$(echo "$TEST_OUTPUT" | grep -oP 'Failed:\s*\K\d+' || echo "0")
    TESTS_SKIPPED=$(echo "$TEST_OUTPUT" | grep -oP 'Skipped:\s*\K\d+' || echo "0")
    TESTS_TOTAL=$(echo "$TEST_OUTPUT" | grep -oP 'Total tests:\s*\K\d+' || echo "0")

    if [ "$TESTS_TOTAL" = "0" ]; then
        fail "Windows tests: could not determine test results"
    elif [ "$TESTS_FAILED" != "0" ]; then
        fail "Windows tests: $TESTS_FAILED/$TESTS_TOTAL failed ($TESTS_PASSED passed, $TESTS_SKIPPED skipped)"
    elif [ "$((TESTS_PASSED + TESTS_SKIPPED))" != "$TESTS_TOTAL" ]; then
        DIFF=$((TESTS_TOTAL - TESTS_PASSED - TESTS_SKIPPED))
        fail "Windows tests: $DIFF/$TESTS_TOTAL unaccounted ($TESTS_PASSED passed, $TESTS_SKIPPED skipped)"
    else
        pass "Windows tests: $TESTS_PASSED passed, $TESTS_SKIPPED skipped ($TESTS_TOTAL total)"
    fi
fi

# macOS tests
if [[ "$(uname)" == "Darwin" ]]; then
    skip "macOS test suite (not implemented yet)"
else
    skip "macOS test suite (requires macOS)"
fi

# Manual testing status (check #1284)
ISSUE_1284_STATE=$(gh issue view 1284 --repo storybuilder-org/StoryCAD --json state --jq '.state' 2>/dev/null || echo "UNKNOWN")
ISSUE_1284_BODY=$(gh issue view 1284 --repo storybuilder-org/StoryCAD --json body --jq '.body' 2>/dev/null || echo "")
TOTAL_BOXES=$(echo "$ISSUE_1284_BODY" | grep -c '\- \[.\]' 2>/dev/null || true)
TOTAL_BOXES=${TOTAL_BOXES:-0}
CHECKED_BOXES=$(echo "$ISSUE_1284_BODY" | grep -c '\- \[x\]' 2>/dev/null || true)
CHECKED_BOXES=${CHECKED_BOXES:-0}
if [ "$ISSUE_1284_STATE" = "CLOSED" ]; then
    pass "Manual testing (#1284) completed (issue closed)"
elif [ "$TOTAL_BOXES" -gt 0 ]; then
    fail "Manual testing (#1284) incomplete: $CHECKED_BOXES/$TOTAL_BOXES checkboxes done (issue $ISSUE_1284_STATE)"
else
    fail "Manual testing (#1284) status unknown"
fi

manual "Performance acceptable on both platforms"

echo ""

# =====================================================================
# DOCUMENTATION
# =====================================================================
echo "DOCUMENTATION"

# Changelog
if [ -f "$REPO/docs/For Developers/Changelog.md" ]; then
    if grep -qi '4\.0\|release 4' "$REPO/docs/For Developers/Changelog.md" 2>/dev/null; then
        pass "Changelog.md exists and references 4.0"
    else
        fail "Changelog.md exists but does NOT reference Release 4.0"
    fi
else
    fail "Changelog.md not found at docs/For Developers/Changelog.md"
fi

# User manual platform content
MAC_REFS=$(grep -ril 'macos\|mac os\|macintosh' "$REPO/docs" --include="*.md" 2>/dev/null | grep -v '_site' | wc -l)
if [ "$MAC_REFS" -gt 0 ]; then
    pass "User manual has platform-specific content ($MAC_REFS files with macOS references)"
else
    fail "No macOS references found in user manual (docs/)"
fi

# Developer docs
DEV_ARCH=$(find "$REPO/.claude/docs/architecture" -name "*.md" 2>/dev/null | wc -l)
if [ "$DEV_ARCH" -gt 0 ]; then
    pass "Developer documentation exists ($DEV_ARCH files in .claude/docs/architecture/)"
else
    fail "No developer architecture docs found"
fi

# Release notes
RELEASE_NOTES=$(find "$REPO/devdocs" -iname "*release*notes*" ! -iname "*comments*" ! -name "*.sh" 2>/dev/null | head -5)
if [ -z "$RELEASE_NOTES" ]; then
    RELEASE_NOTES=$(find "$REPO/docs" -iname "*release*notes*" ! -iname "*comments*" 2>/dev/null | head -5)
fi
if [ -n "$RELEASE_NOTES" ]; then
    pass "Release notes found: $(echo "$RELEASE_NOTES" | xargs -I{} basename {})"
else
    fail "No release notes found in devdocs/ or docs/"
fi

echo ""

# =====================================================================
# DISTRIBUTION
# =====================================================================
echo "DISTRIBUTION"

manual "Windows: Microsoft Store update from 3.4 to 4.0"
manual "macOS: Installation works, notarization/signing complete"

echo ""

# =====================================================================
# SUMMARY
# =====================================================================
echo "============================================"
echo "SUMMARY: $PASS PASS, $FAIL FAIL, $SKIP SKIP, $MANUAL MANUAL"
TOTAL=$((PASS + FAIL + SKIP + MANUAL))
echo "Total checks: $TOTAL"
if [ "$FAIL" -gt 0 ]; then
    echo "STATUS: *** FAILURES DETECTED — review FAIL items above ***"
    exit 1
else
    echo "STATUS: All automated checks passed"
    exit 0
fi
