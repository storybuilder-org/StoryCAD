#!/usr/bin/env bash
#
# review-1124-acceptance.sh
# Automated review of #1124 acceptance criteria for StoryCAD Release 4.0
#
# Usage: bash /tmp/review-1124-acceptance.sh [--skip-build] [--skip-tests]
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

# 1. Windows WinAppSDK build
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

# 2. macOS desktop build (can only run on macOS)
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

# 3. Version numbers
MANIFEST_VERSION=$(grep -oP 'Version="\K[^"]+' "$REPO/StoryCAD/Package.appxmanifest" | head -1 || echo "NOT FOUND")
LIB_VERSION=$(grep -oP '<Version>\K[^<]+' "$REPO/StoryCADLib/StoryCADLib.csproj" || echo "NOT FOUND")
LIB_ASM_VERSION=$(grep -oP '<AssemblyVersion>\K[^<]+' "$REPO/StoryCADLib/StoryCADLib.csproj" || echo "NOT FOUND")

if [[ "$MANIFEST_VERSION" == "4.0"* ]] && [[ "$LIB_VERSION" == "4.0"* ]]; then
    pass "Version 4.0 in Package.appxmanifest ($MANIFEST_VERSION) and StoryCADLib.csproj ($LIB_VERSION)"
else
    fail "Version mismatch: Package.appxmanifest=$MANIFEST_VERSION, StoryCADLib.csproj=$LIB_VERSION (expected 4.0.x)"
fi

# 4. Dual target frameworks in StoryCAD.csproj
if grep -q 'net10.0-windows10.0.22621' "$REPO/StoryCAD/StoryCAD.csproj" && \
   grep -q 'net10.0-desktop' "$REPO/StoryCAD/StoryCAD.csproj"; then
    pass "Dual target frameworks in StoryCAD.csproj (windows + desktop)"
else
    fail "Missing target framework(s) in StoryCAD.csproj"
fi

# 5. Dual target frameworks in StoryCADLib.csproj
if grep -q 'net10.0-windows10.0.22621' "$REPO/StoryCADLib/StoryCADLib.csproj" && \
   grep -q 'net10.0-desktop' "$REPO/StoryCADLib/StoryCADLib.csproj"; then
    pass "Dual target frameworks in StoryCADLib.csproj (windows + desktop)"
else
    fail "Missing target framework(s) in StoryCADLib.csproj"
fi

# 6. Distribution packages
manual "Windows MSIX package created for Microsoft Store"
manual "macOS application bundle (.app) created"
manual "macOS installer package (.pkg or .dmg) created"

echo ""

# =====================================================================
# CORE FUNCTIONALITY
# =====================================================================
echo "CORE FUNCTIONALITY"

# 7. All 11 story element types defined in enum
EXPECTED_TYPES=("StoryOverview" "Problem" "Character" "Setting" "Scene" "Folder" "Section" "Web" "Notes" "TrashCan" "StoryWorld")
ENUM_FILE="$REPO/StoryCADLib/Enums/StoryItemType.cs"
MISSING_TYPES=()
for t in "${EXPECTED_TYPES[@]}"; do
    if ! grep -q "$t" "$ENUM_FILE" 2>/dev/null; then
        MISSING_TYPES+=("$t")
    fi
done
if [ ${#MISSING_TYPES[@]} -eq 0 ]; then
    pass "All ${#EXPECTED_TYPES[@]} story element types defined in StoryItemType enum"
else
    fail "Missing story element types: ${MISSING_TYPES[*]}"
fi

# 8. Platform-specific partial classes
WINAPP_FILES=$(find "$REPO/StoryCADLib" -name "*.WinAppSDK.cs" 2>/dev/null | wc -l)
DESKTOP_FILES=$(find "$REPO/StoryCADLib" -name "*.desktop.cs" 2>/dev/null | wc -l)
if [ "$WINAPP_FILES" -gt 0 ] && [ "$DESKTOP_FILES" -gt 0 ]; then
    pass "Platform-specific partial classes exist ($WINAPP_FILES WinAppSDK, $DESKTOP_FILES desktop)"
else
    fail "Missing platform-specific partial classes (WinAppSDK=$WINAPP_FILES, desktop=$DESKTOP_FILES)"
fi

# 9. Conditional compilation patterns
HAS_MACOS=$(grep -rl '#if __MACOS__\|#if HAS_UNO' "$REPO/StoryCADLib" --include="*.cs" 2>/dev/null | wc -l)
if [ "$HAS_MACOS" -gt 0 ]; then
    pass "Conditional compilation patterns found (#if __MACOS__ / #if HAS_UNO) in $HAS_MACOS files"
else
    fail "No conditional compilation patterns (#if __MACOS__ / #if HAS_UNO) found"
fi

# 10. Key services registered in IoC
IOC_FILE="$REPO/StoryCADLib/Services/IoC/ServiceLocator.cs"
KEY_SERVICES=("NavigationService" "LogService" "SearchService" "OutlineService" "BackupService" "AutoSaveService" "CollaboratorService" "ShellViewModel" "AppState" "Windowing")
MISSING_SERVICES=()
for svc in "${KEY_SERVICES[@]}"; do
    if ! grep -q "$svc" "$IOC_FILE" 2>/dev/null; then
        MISSING_SERVICES+=("$svc")
    fi
done
if [ ${#MISSING_SERVICES[@]} -eq 0 ]; then
    pass "All ${#KEY_SERVICES[@]} key services registered in IoC (ServiceLocator.cs)"
else
    fail "Missing IoC registrations: ${MISSING_SERVICES[*]}"
fi

# 11. Manual cross-platform functionality checks
manual "File operations work on both platforms (New, Open, Save, SaveAs)"
manual "File path handling (backslash vs forward slash)"
manual ".stbx file format compatibility between platforms"
manual "Navigation and UI work on both platforms"
manual "Keyboard shortcuts (Ctrl on Windows, Cmd on macOS)"
manual "Context menus and flyout menus"
manual "Tools work on both platforms (Conflict Builder, Master Plots, etc.)"
manual "Services work cross-platform (AutoSave, Backup, Search, Logging)"

echo ""

# =====================================================================
# PLATFORM-SPECIFIC FEATURES
# =====================================================================
echo "PLATFORM-SPECIFIC FEATURES"

# 12. Windows printing partial class exists
if [ -f "$REPO/StoryCADLib/ViewModels/Tools/PrintReportDialogVM.WinAppSDK.cs" ]; then
    pass "Windows PrintReportDialogVM.WinAppSDK.cs exists"
else
    fail "Missing PrintReportDialogVM.WinAppSDK.cs"
fi

# 13. RichEditBox platform partial classes
if [ -f "$REPO/StoryCADLib/Controls/RichEditBoxExtended.WinAppSDK.cs" ] && \
   [ -f "$REPO/StoryCADLib/Controls/RichEditBoxExtended.desktop.cs" ]; then
    pass "RichEditBoxExtended platform partial classes exist (WinAppSDK + desktop)"
else
    fail "Missing RichEditBoxExtended platform partial classes"
fi

manual "Windows: Print Reports to physical printer works"
manual "Windows: Export Reports to PDF works"
manual "macOS: Export Reports to PDF works"
manual "macOS: Native file dialogs work"
manual "macOS: Application menus and shortcuts work"
manual "macOS: File access permissions work"

echo ""

# =====================================================================
# TESTING & QUALITY
# =====================================================================
echo "TESTING & QUALITY"

# 14. Run test suite on Windows
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

# 15. macOS tests
if [[ "$(uname)" == "Darwin" ]]; then
    skip "macOS test suite (not implemented yet)"
else
    skip "macOS test suite (requires macOS)"
fi

# 16. Test file count
TEST_FILE_COUNT=$(find "$REPO/StoryCADTests" -name "*Tests.cs" -o -name "*Test.cs" 2>/dev/null | wc -l)
TEST_METHOD_COUNT=$(grep -r '\[TestMethod\]\|\[UITestMethod\]' "$REPO/StoryCADTests" --include="*.cs" 2>/dev/null | wc -l)
pass "Test inventory: $TEST_FILE_COUNT test files, $TEST_METHOD_COUNT test methods"

manual "Manual testing completed on Windows 10"
manual "Manual testing completed on Windows 11"
manual "Manual testing completed on macOS 10.15+"
manual "No regressions from Release 3.4 on Windows"
manual "Performance acceptable on both platforms"

echo ""

# =====================================================================
# DOCUMENTATION
# =====================================================================
echo "DOCUMENTATION"

# 17. Changelog exists
if [ -f "$REPO/docs/For Developers/Changelog.md" ]; then
    # Check if it mentions 4.0
    if grep -qi '4\.0\|release 4' "$REPO/docs/For Developers/Changelog.md" 2>/dev/null; then
        pass "Changelog.md exists and references 4.0"
    else
        fail "Changelog.md exists but does NOT reference Release 4.0"
    fi
else
    fail "Changelog.md not found at docs/For Developers/Changelog.md"
fi

# 18. ROADMAP.md exists
if [ -f "$REPO/ROADMAP.md" ]; then
    pass "ROADMAP.md exists"
else
    fail "ROADMAP.md not found"
fi

# 19. Release notes devdoc
RELEASE_NOTES=$(find "$REPO/devdocs" -iname "*1124*" -o -iname "*release*notes*" -o -iname "*4.0*release*" 2>/dev/null | head -5)
if [ -n "$RELEASE_NOTES" ]; then
    pass "Release notes devdoc(s) found: $(echo "$RELEASE_NOTES" | xargs -I{} basename {})"
else
    fail "No release notes devdoc found in devdocs/ (expected *1124* or *release*notes*)"
fi

# 20. User manual has macOS/platform references
MAC_REFS=$(grep -ril 'macos\|mac os\|macintosh' "$REPO/docs" --include="*.md" 2>/dev/null | grep -v '_site' | wc -l)
PLATFORM_REFS=$(grep -ril 'cross-platform\|desktop head\|uno platform' "$REPO/docs" --include="*.md" 2>/dev/null | grep -v '_site' | wc -l)
if [ "$MAC_REFS" -gt 0 ]; then
    pass "User manual has macOS references in $MAC_REFS file(s)"
else
    fail "No macOS references found in user manual (docs/)"
fi
if [ "$PLATFORM_REFS" -gt 0 ]; then
    pass "User manual has cross-platform/UNO references in $PLATFORM_REFS file(s)"
else
    fail "No cross-platform references found in user manual (docs/)"
fi

# 21. Developer docs
DEV_ARCH=$(find "$REPO/.claude/docs/architecture" -name "*.md" 2>/dev/null | wc -l)
if [ "$DEV_ARCH" -gt 0 ]; then
    pass "Developer architecture docs exist ($DEV_ARCH files in .claude/docs/architecture/)"
else
    fail "No developer architecture docs found"
fi

# Check for platform-specific dev docs
PLAT_DOCS=$(find "$REPO/devdocs" -iname "*platform*" -o -iname "*uno*" -o -iname "*cross*" 2>/dev/null | wc -l)
if [ "$PLAT_DOCS" -gt 0 ]; then
    pass "Platform-specific dev docs exist ($PLAT_DOCS files in devdocs/)"
else
    fail "No platform-specific dev docs found in devdocs/"
fi

manual "Changelog content accuracy (all changes since 3.4 documented)"
manual "User manual screenshots for both platforms where UI differs"
manual "Keyboard shortcut tables (Ctrl/Cmd variants) in user manual"
manual "Release notes prepared for distribution"

echo ""

# =====================================================================
# CI/CD & DISTRIBUTION
# =====================================================================
echo "CI/CD & DISTRIBUTION"

# 22. CI workflow exists
if [ -f "$REPO/.github/workflows/build-release.yml" ]; then
    pass "build-release.yml CI/CD workflow exists"
else
    fail "build-release.yml not found"
fi

if [ -f "$REPO/.github/workflows/ci.yml" ]; then
    pass "ci.yml workflow exists"
else
    fail "ci.yml not found"
fi

manual "Windows: Microsoft Store update from 3.4 to 4.0 works"
manual "Windows: New installations work on clean machines"
manual "macOS: Installation works on clean macOS machines"
manual "macOS: Application launches and runs without errors"
manual "macOS: Security/notarization requirements met"

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
