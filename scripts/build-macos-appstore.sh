#!/bin/bash
# Build StoryCAD for Mac App Store / TestFlight
# Usage: ./scripts/build-macos-appstore.sh [--bump]
#   --bump: Auto-increment CFBundleVersion before building

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
INFO_PLIST="$REPO_DIR/StoryCAD/Platforms/Desktop/Info.plist"
ENTITLEMENTS="$REPO_DIR/StoryCAD/Platforms/Desktop/Entitlements.plist"
PROVISIONING_PROFILE="$HOME/Downloads/StoryCAD__Provisioning_Profile.provisionprofile"
CERT_NAME="Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)"
INSTALLER_CERT="3rd Party Mac Developer Installer: StoryBuilder Foundation (3T4DPS2D5Y)"

cd "$REPO_DIR"

# Check for provisioning profile
if [ ! -f "$PROVISIONING_PROFILE" ]; then
    echo "ERROR: Provisioning profile not found at $PROVISIONING_PROFILE"
    exit 1
fi

# Auto-bump version if requested
if [ "$1" == "--bump" ]; then
    CURRENT_VERSION=$(/usr/libexec/PlistBuddy -c "Print :CFBundleVersion" "$INFO_PLIST")
    NEW_VERSION=$((CURRENT_VERSION + 1))
    /usr/libexec/PlistBuddy -c "Set :CFBundleVersion $NEW_VERSION" "$INFO_PLIST"
    echo "Bumped CFBundleVersion: $CURRENT_VERSION -> $NEW_VERSION"
else
    NEW_VERSION=$(/usr/libexec/PlistBuddy -c "Print :CFBundleVersion" "$INFO_PLIST")
    echo "Using CFBundleVersion: $NEW_VERSION"
fi

SHORT_VERSION=$(/usr/libexec/PlistBuddy -c "Print :CFBundleShortVersionString" "$INFO_PLIST")
PKG_NAME="$HOME/Desktop/StoryCAD-${SHORT_VERSION}.pkg"

echo ""
echo "=== Building StoryCAD $SHORT_VERSION (build $NEW_VERSION) ==="
echo ""

# Step 1: Publish (UNO creates .app bundle automatically with PackageFormat=app)
echo "[1/5] Publishing..."
dotnet publish StoryCAD/StoryCAD.csproj -c Release -f net10.0-desktop -r osx-arm64 \
  -p:SelfContained=true \
  -p:PackageFormat=app \
  -p:UnoMacOSEntitlements="$ENTITLEMENTS" \
  -p:UnoMacOSIncludeCreateDump=false \
  -v q

APP_DIR="StoryCAD/bin/Release/net10.0-desktop/osx-arm64/publish/StoryCAD.app"

if [ ! -d "$APP_DIR" ]; then
    echo "ERROR: .app bundle not found at $APP_DIR"
    exit 1
fi

# Step 2: Post-publish fixups
echo "[2/5] Post-publish fixups..."

# Copy provisioning profile and clear quarantine attributes
cp "$PROVISIONING_PROFILE" "$APP_DIR/Contents/embedded.provisionprofile"
xattr -cr "$APP_DIR"

# Move .deps.json and .runtimeconfig.json to Resources (required for App Store validation)
# Then create symlinks so the runtime can still find them in MacOS/
mv "$APP_DIR/Contents/MacOS/StoryCAD.deps.json" "$APP_DIR/Contents/Resources/"
mv "$APP_DIR/Contents/MacOS/StoryCAD.runtimeconfig.json" "$APP_DIR/Contents/Resources/"
ln -s ../Resources/StoryCAD.deps.json "$APP_DIR/Contents/MacOS/StoryCAD.deps.json"
ln -s ../Resources/StoryCAD.runtimeconfig.json "$APP_DIR/Contents/MacOS/StoryCAD.runtimeconfig.json"

# Step 3: Sign all components
echo "[3/5] Signing..."

# Sign dylibs individually
find "$APP_DIR/Contents/MacOS" -name "*.dylib" \
  -exec codesign --force --sign "$CERT_NAME" {} \;

# Sign main executable with entitlements
codesign --force --options runtime \
  --sign "$CERT_NAME" \
  --entitlements "$ENTITLEMENTS" \
  "$APP_DIR/Contents/MacOS/StoryCAD"

# Sign app bundle
codesign --force --options runtime \
  --sign "$CERT_NAME" \
  --entitlements "$ENTITLEMENTS" \
  "$APP_DIR"

# Step 4: Verify
echo "[4/5] Verifying signature..."
codesign --verify --deep --strict "$APP_DIR" 2>&1 | tail -2
codesign -d --entitlements - "$APP_DIR/Contents/MacOS/StoryCAD" 2>&1 | head -20

# Step 5: Package
echo "[5/5] Creating installer package..."
rm -f "$PKG_NAME"

# Clear quarantine again before packaging
xattr -cr "$APP_DIR"

productbuild --component "$APP_DIR" /Applications \
  --sign "$INSTALLER_CERT" \
  "$PKG_NAME"

# Clean up: delete build .app to prevent installer relocation bug
rm -rf "$APP_DIR"

echo ""
echo "=== Done ==="
echo "Package: $PKG_NAME"
echo ""
echo "Upload via Transporter app."
