# macOS App Store Build Guide

This guide covers building, signing, and submitting StoryCAD for the Mac App Store via TestFlight.

## Quick Start

Use the automated build script:

```bash
# Build with current version
./scripts/build-macos-appstore.sh

# Build and auto-increment version
./scripts/build-macos-appstore.sh --bump
```

The script outputs `~/Desktop/StoryCAD-4.0.0.pkg` ready for upload via Transporter.

## Prerequisites

- macOS with Xcode Command Line Tools installed
- .NET SDK (check version in `global.json`)
- Apple Developer account with:
  - `Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)` certificate
  - `3rd Party Mac Developer Installer: StoryBuilder Foundation (3T4DPS2D5Y)` certificate
  - App Store provisioning profile for `com.storybuilder.storycad`
- Transporter app (from Mac App Store)
- Provisioning profile at `~/Downloads/StoryCAD__Provisioning_Profile.provisionprofile`

### Verify Certificates

```bash
# Check code signing certificates
security find-identity -v -p codesigning

# Check installer certificates
security find-identity -v | grep -i installer
```

## Key Files

| File | Purpose |
|------|---------|
| `scripts/build-macos-appstore.sh` | Automated build script |
| `StoryCAD/Platforms/Desktop/Info.plist` | macOS app metadata, version numbers |
| `StoryCAD/Platforms/Desktop/entitlements.plist` | App entitlements (sandbox, JIT) |
| `~/Downloads/StoryCAD__Provisioning_Profile.provisionprofile` | App Store provisioning profile |

## Required Entitlements

The `entitlements.plist` must include:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.app-sandbox</key>
    <true/>
    <key>com.apple.security.cs.allow-jit</key>
    <true/>
    <key>com.apple.application-identifier</key>
    <string>3T4DPS2D5Y.com.storybuilder.storycad</string>
</dict>
</plist>
```

**Critical:** The `allow-jit` entitlement is required for .NET CoreCLR to allocate executable memory. Without it, the app crashes on launch with `HRESULT: 0x80070008`.

## Manual Build Process

If you need to build manually (the script handles all of this):

### Step 1: Publish the App

```bash
dotnet publish StoryCAD/StoryCAD.csproj -c Release -f net10.0-desktop -r osx-arm64 --self-contained true
```

### Step 2: Create App Bundle

```bash
APP_DIR="/tmp/StoryCAD.app"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources"

# Copy published files
cp -R StoryCAD/bin/Release/net10.0-desktop/osx-arm64/publish/* "$APP_DIR/Contents/MacOS/"

# Move resources to correct location
mv "$APP_DIR/Contents/MacOS/Assets" "$APP_DIR/Contents/Resources/"
mv "$APP_DIR/Contents/MacOS/Uno.Fonts.OpenSans" "$APP_DIR/Contents/Resources/" 2>/dev/null || true
mv "$APP_DIR/Contents/MacOS/Uno.Fonts.Fluent" "$APP_DIR/Contents/Resources/" 2>/dev/null || true

# Copy required files
cp StoryCAD/Platforms/Desktop/Info.plist "$APP_DIR/Contents/Info.plist"
cp /tmp/icon.icns "$APP_DIR/Contents/Resources/icon.icns"
cp ~/Downloads/StoryCAD__Provisioning_Profile.provisionprofile "$APP_DIR/Contents/embedded.provisionprofile"

# IMPORTANT: Remove createdump (incompatible with App Store)
rm -f "$APP_DIR/Contents/MacOS/createdump"

# Make executable and remove quarantine
chmod +x "$APP_DIR/Contents/MacOS/StoryCAD"
xattr -cr "$APP_DIR"
```

### Step 3: Create Icon (if needed)

```bash
SRC="StoryCAD/Assets/LargeTile.scale-400.png"
rm -rf /tmp/icon.iconset
mkdir -p /tmp/icon.iconset

sips -z 16 16 "$SRC" --out /tmp/icon.iconset/icon_16x16.png
sips -z 32 32 "$SRC" --out /tmp/icon.iconset/icon_16x16@2x.png
sips -z 32 32 "$SRC" --out /tmp/icon.iconset/icon_32x32.png
sips -z 64 64 "$SRC" --out /tmp/icon.iconset/icon_32x32@2x.png
sips -z 128 128 "$SRC" --out /tmp/icon.iconset/icon_128x128.png
sips -z 256 256 "$SRC" --out /tmp/icon.iconset/icon_128x128@2x.png
sips -z 256 256 "$SRC" --out /tmp/icon.iconset/icon_256x256.png
sips -z 512 512 "$SRC" --out /tmp/icon.iconset/icon_256x256@2x.png
sips -z 512 512 "$SRC" --out /tmp/icon.iconset/icon_512x512.png
sips -z 1024 1024 "$SRC" --out /tmp/icon.iconset/icon_512x512@2x.png

iconutil -c icns /tmp/icon.iconset -o /tmp/icon.icns
```

### Step 4: Sign Everything

**Important:** Sign in this exact order. Do NOT use `--deep` flag.

```bash
CERT="Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)"
ENTITLEMENTS="StoryCAD/Platforms/Desktop/entitlements.plist"

# 1. Sign all nested files (excluding main executable)
find /tmp/StoryCAD.app/Contents/MacOS -type f ! -name "StoryCAD" \
  -exec codesign --force --sign "$CERT" {} \; 2>/dev/null

find /tmp/StoryCAD.app/Contents/Resources -type f \
  -exec codesign --force --sign "$CERT" {} \; 2>/dev/null

# 2. Sign main executable with entitlements
codesign --force --options runtime \
  --sign "$CERT" \
  --entitlements "$ENTITLEMENTS" \
  /tmp/StoryCAD.app/Contents/MacOS/StoryCAD

# 3. Sign the app bundle
codesign --force --options runtime \
  --sign "$CERT" \
  --entitlements "$ENTITLEMENTS" \
  /tmp/StoryCAD.app
```

### Step 5: Verify Signature

```bash
codesign -vvv /tmp/StoryCAD.app

# Verify JIT entitlement is present
codesign -d --entitlements - /tmp/StoryCAD.app/Contents/MacOS/StoryCAD
```

### Step 6: Create PKG

```bash
productbuild --component /tmp/StoryCAD.app /Applications \
  --sign "3rd Party Mac Developer Installer: StoryBuilder Foundation (3T4DPS2D5Y)" \
  ~/Desktop/StoryCAD-4.0.0.pkg
```

### Step 7: Upload via Transporter

1. Open Transporter app
2. Drag `StoryCAD-4.0.0.pkg` to the window
3. Click "Deliver"

## Before Each Upload

**Bump the build number** in `StoryCAD/Platforms/Desktop/Info.plist`:

```xml
<key>CFBundleVersion</key>
<string>9</string>  <!-- Increment this -->
```

Or use `./scripts/build-macos-appstore.sh --bump` to auto-increment.

## Common Errors & Fixes

### "Failed to create CoreCLR, HRESULT: 0x80070008"
**Cause:** Missing `com.apple.security.cs.allow-jit` entitlement.
**Fix:** Add `allow-jit` to entitlements.plist. The .NET JIT compiler needs to allocate executable memory.

### "Bundle version must be higher"
Increment `CFBundleVersion` in Info.plist.

### "App sandbox not enabled"
Ensure entitlements include `com.apple.security.app-sandbox` set to `true`.

### "Missing provisioning profile"
Copy provisioning profile to `Contents/embedded.provisionprofile`.

### "Invalid Code Signing - executable missing provisioning profile but has application identifier"
**Cause:** Nested executables (like `createdump`) with `application-identifier` require their own provisioning profile.
**Fix:** Remove the executable from the bundle. The build script removes `createdump` automatically.

### "Invalid bundle - arm64 only requires macOS 12.0"
Set `LSMinimumSystemVersion` to `12.0` in Info.plist.

### "Quarantine attribute"
Run `xattr -cr /tmp/StoryCAD.app` before signing.

### "Code object not signed in subcomponent"
Sign ALL files recursively before signing executables. Don't use `--deep`.

## Info.plist Required Keys

```xml
<key>CFBundleDevelopmentRegion</key>
<string>en-US</string>
<key>CFBundleExecutable</key>
<string>StoryCAD</string>
<key>CFBundleIdentifier</key>
<string>com.storybuilder.storycad</string>
<key>CFBundleInfoDictionaryVersion</key>
<string>6.0</string>
<key>CFBundleName</key>
<string>StoryCAD</string>
<key>CFBundlePackageType</key>
<string>APPL</string>
<key>CFBundleVersion</key>
<string>9</string>
<key>CFBundleShortVersionString</key>
<string>4.0.0</string>
<key>LSMinimumSystemVersion</key>
<string>12.0</string>
<key>ITSAppUsesNonExemptEncryption</key>
<false/>
<key>NSPrincipalClass</key>
<string>NSApplication</string>
```

## Debugging TestFlight Builds

If the app won't launch after installing from TestFlight:

```bash
# Check crash reports
ls -la ~/Library/Logs/DiagnosticReports/ | grep -i storycad

# View recent crash
cat ~/Library/Logs/DiagnosticReports/StoryCAD*.ips | head -100

# Check system logs
log show --predicate 'process == "StoryCAD"' --last 5m

# Check sandbox violations
log show --predicate 'eventMessage CONTAINS "sandbox" AND eventMessage CONTAINS "deny"' --last 5m

# Try launching from terminal
/Applications/StoryCAD.app/Contents/MacOS/StoryCAD
```
