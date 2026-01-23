# macOS App Store Build Guide

This guide covers building, signing, and submitting StoryCAD for the Mac App Store via TestFlight.

## Prerequisites

- macOS with Xcode Command Line Tools installed
- .NET SDK (check version in `global.json`)
- Apple Developer account with:
  - `Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)` certificate
  - `3rd Party Mac Developer Installer: StoryBuilder Foundation (3T4DPS2D5Y)` certificate
  - App Store provisioning profile for `com.storybuilder.storycad`
- Transporter app (from Mac App Store)

### Verify Certificates

```bash
# Check code signing certificates
security find-identity -v -p codesigning

# Check installer certificates
security find-identity -v | grep -i installer
```

## Build Process

### Step 1: Publish the App

```bash
dotnet publish StoryCAD/StoryCAD.csproj -c Release -f net10.0-desktop -r osx-arm64 --self-contained true
```

### Step 2: Create App Bundle

The Uno SDK doesn't create a proper .app bundle, so we create it manually:

```bash
APP_DIR="/tmp/StoryCAD.app"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

# Copy published files
cp -R StoryCAD/bin/Release/net10.0-desktop/osx-arm64/publish/* "$APP_DIR/Contents/MacOS/"

# Move resources to correct location
mv "$APP_DIR/Contents/MacOS/Assets" "$APP_DIR/Contents/Resources/"
mv "$APP_DIR/Contents/MacOS/Uno.Fonts.OpenSans" "$APP_DIR/Contents/Resources/"
mv "$APP_DIR/Contents/MacOS/Uno.Fonts.Fluent" "$APP_DIR/Contents/Resources/"

# Copy required files
cp StoryCAD/Platforms/Desktop/Info.plist "$APP_DIR/Contents/Info.plist"
cp /tmp/icon.icns "$APP_DIR/Contents/Resources/icon.icns"
cp ~/Downloads/StoryCAD__Provisioning_Profile.provisionprofile "$APP_DIR/Contents/embedded.provisionprofile"

# Make executable and remove quarantine
chmod +x "$APP_DIR/Contents/MacOS/StoryCAD"
xattr -cr "$APP_DIR"
```

### Step 3: Create Entitlements Files

Create `/tmp/entitlements.plist` (for main app):

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.app-sandbox</key>
    <true/>
    <key>com.apple.application-identifier</key>
    <string>3T4DPS2D5Y.com.storybuilder.storycad</string>
</dict>
</plist>
```

Create `/tmp/helper-entitlements.plist` (for helper executables like `createdump`):

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.app-sandbox</key>
    <true/>
</dict>
</plist>
```

### Step 4: Create Icon (if needed)

If `/tmp/icon.icns` doesn't exist, create it from the LargeTile asset:

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

### Step 5: Sign Everything

**Important:** Sign in this exact order. Do NOT use `--deep` flag.

```bash
# 1. Sign all nested files (excluding main executables)
find /tmp/StoryCAD.app/Contents/MacOS -type f ! -name "StoryCAD" ! -name "createdump" \
  -exec codesign --force --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" {} \; 2>/dev/null

find /tmp/StoryCAD.app/Contents/Resources -type f \
  -exec codesign --force --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" {} \; 2>/dev/null

# 2. Sign createdump with helper entitlements (sandbox only, NO app-identifier)
codesign --force --options runtime \
  --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" \
  --entitlements /tmp/helper-entitlements.plist \
  /tmp/StoryCAD.app/Contents/MacOS/createdump

# 3. Sign main executable with full entitlements
codesign --force --options runtime \
  --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" \
  --entitlements /tmp/entitlements.plist \
  /tmp/StoryCAD.app/Contents/MacOS/StoryCAD

# 4. Sign the app bundle
codesign --force --options runtime \
  --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" \
  --entitlements /tmp/entitlements.plist \
  /tmp/StoryCAD.app
```

### Step 6: Verify Signature

```bash
# Check signature is valid
codesign -vvv /tmp/StoryCAD.app

# Check entitlements
codesign -d --entitlements - /tmp/StoryCAD.app/Contents/MacOS/StoryCAD
```

### Step 7: Create PKG

```bash
productbuild --component /tmp/StoryCAD.app /Applications \
  --sign "3rd Party Mac Developer Installer: StoryBuilder Foundation (3T4DPS2D5Y)" \
  ~/Desktop/StoryCAD-4.0.0.pkg
```

### Step 8: Upload via Transporter

1. Open Transporter app
2. Drag `StoryCAD-4.0.0.pkg` to the window
3. Click "Deliver"

## Before Each Upload

**Bump the build number** in `StoryCAD/Platforms/Desktop/Info.plist`:

```xml
<key>CFBundleVersion</key>
<string>8</string>  <!-- Increment this -->
```

## Key Files

| File | Purpose |
|------|---------|
| `StoryCAD/Platforms/Desktop/Info.plist` | macOS app metadata, version numbers |
| `StoryCAD/StoryCAD.csproj` | `ApplicationId` must be `com.storybuilder.storycad` |
| `~/Downloads/StoryCAD__Provisioning_Profile.provisionprofile` | App Store provisioning profile |

## Common Errors & Fixes

### "Bundle version must be higher"
Increment `CFBundleVersion` in Info.plist.

### "App sandbox not enabled"
Ensure entitlements include `com.apple.security.app-sandbox` set to `true`.

### "Missing provisioning profile"
Copy provisioning profile to `Contents/embedded.provisionprofile`.

### "Entitlements don't match provisioning profile"
- Main app: needs `app-sandbox` + `application-identifier`
- Helper executables (createdump): needs `app-sandbox` only

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
<string>7</string>
<key>CFBundleShortVersionString</key>
<string>4.0.0</string>
<key>LSMinimumSystemVersion</key>
<string>12.0</string>
<key>ITSAppUsesNonExemptEncryption</key>
<false/>
<key>NSPrincipalClass</key>
<string>NSApplication</string>
```

## Quick Reference Script

For convenience, here's a one-shot script (after initial setup):

```bash
#!/bin/bash
set -e

# Bump version first!
# Edit StoryCAD/Platforms/Desktop/Info.plist CFBundleVersion

# Build
dotnet publish StoryCAD/StoryCAD.csproj -c Release -f net10.0-desktop -r osx-arm64 --self-contained true

# Create bundle
APP_DIR="/tmp/StoryCAD.app"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources"
cp -R StoryCAD/bin/Release/net10.0-desktop/osx-arm64/publish/* "$APP_DIR/Contents/MacOS/"
mv "$APP_DIR/Contents/MacOS/Assets" "$APP_DIR/Contents/Resources/"
mv "$APP_DIR/Contents/MacOS/Uno.Fonts.OpenSans" "$APP_DIR/Contents/Resources/" 2>/dev/null || true
mv "$APP_DIR/Contents/MacOS/Uno.Fonts.Fluent" "$APP_DIR/Contents/Resources/" 2>/dev/null || true
cp StoryCAD/Platforms/Desktop/Info.plist "$APP_DIR/Contents/Info.plist"
cp /tmp/icon.icns "$APP_DIR/Contents/Resources/icon.icns"
cp ~/Downloads/StoryCAD__Provisioning_Profile.provisionprofile "$APP_DIR/Contents/embedded.provisionprofile"
chmod +x "$APP_DIR/Contents/MacOS/StoryCAD"
xattr -cr "$APP_DIR"

# Sign (entitlements from repo)
ENTITLEMENTS="StoryCAD/Platforms/Desktop/entitlements.plist"
HELPER_ENTITLEMENTS="StoryCAD/Platforms/Desktop/helper-entitlements.plist"

find "$APP_DIR/Contents/MacOS" -type f ! -name "StoryCAD" ! -name "createdump" -exec codesign --force --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" {} \; 2>/dev/null
find "$APP_DIR/Contents/Resources" -type f -exec codesign --force --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" {} \; 2>/dev/null
codesign --force --options runtime --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" --entitlements "$HELPER_ENTITLEMENTS" "$APP_DIR/Contents/MacOS/createdump"
codesign --force --options runtime --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" --entitlements "$ENTITLEMENTS" "$APP_DIR/Contents/MacOS/StoryCAD"
codesign --force --options runtime --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" --entitlements "$ENTITLEMENTS" "$APP_DIR"

# Package
rm -f ~/Desktop/StoryCAD-4.0.0.pkg
productbuild --component "$APP_DIR" /Applications --sign "3rd Party Mac Developer Installer: StoryBuilder Foundation (3T4DPS2D5Y)" ~/Desktop/StoryCAD-4.0.0.pkg

echo "Done! Upload ~/Desktop/StoryCAD-4.0.0.pkg via Transporter"
```
