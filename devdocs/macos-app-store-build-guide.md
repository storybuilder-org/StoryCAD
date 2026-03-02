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
| `StoryCAD/Platforms/Desktop/Entitlements.plist` | App entitlements (sandbox, JIT) |
| `~/Downloads/StoryCAD__Provisioning_Profile.provisionprofile` | App Store provisioning profile |

## Required Entitlements

The `Entitlements.plist` must include:

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

## How It Works

UNO Platform's `PackageFormat=app` handles most of the bundle creation automatically:
- Creates the `.app` bundle structure
- Generates the app icon (icns) from project assets
- Merges `Info.plist` into the bundle
- Excludes `createdump` when `UnoMacOSIncludeCreateDump=false`

The build script handles the remaining post-publish steps that UNO doesn't cover:
- Embedding the provisioning profile
- Relocating `.json` files for App Store validation
- Code signing
- Creating the PKG installer

## Manual Build Process

If you need to build manually (the script handles all of this):

### Step 1: Publish the App

UNO Platform creates the `.app` bundle automatically:

```bash
dotnet publish StoryCAD/StoryCAD.csproj -c Release -f net10.0-desktop -r osx-arm64 \
  -p:SelfContained=true \
  -p:PackageFormat=app \
  -p:UnoMacOSEntitlements=Platforms/Desktop/Entitlements.plist \
  -p:UnoMacOSIncludeCreateDump=false
```

This produces `StoryCAD/bin/Release/net10.0-desktop/osx-arm64/publish/StoryCAD.app`.

### Step 2: Post-Publish Fixups

```bash
APP_DIR="StoryCAD/bin/Release/net10.0-desktop/osx-arm64/publish/StoryCAD.app"

# Embed provisioning profile
cp ~/Downloads/StoryCAD__Provisioning_Profile.provisionprofile "$APP_DIR/Contents/embedded.provisionprofile"

# IMPORTANT: Clear quarantine attributes after copying the provisioning profile
xattr -cr "$APP_DIR"

# Move json files to Resources (required for App Store validation)
# Symlinks let the runtime still find them in MacOS/
mv "$APP_DIR/Contents/MacOS/StoryCAD.deps.json" "$APP_DIR/Contents/Resources/"
mv "$APP_DIR/Contents/MacOS/StoryCAD.runtimeconfig.json" "$APP_DIR/Contents/Resources/"
ln -s ../Resources/StoryCAD.deps.json "$APP_DIR/Contents/MacOS/StoryCAD.deps.json"
ln -s ../Resources/StoryCAD.runtimeconfig.json "$APP_DIR/Contents/MacOS/StoryCAD.runtimeconfig.json"
```

### Step 3: Sign the App

**Important:** Sign in this exact order. Do NOT use `--deep` flag.

```bash
CERT="Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)"
ENTITLEMENTS="StoryCAD/Platforms/Desktop/Entitlements.plist"
APP_DIR="StoryCAD/bin/Release/net10.0-desktop/osx-arm64/publish/StoryCAD.app"

# 1. Sign dylibs individually
find "$APP_DIR/Contents/MacOS" -name "*.dylib" \
  -exec codesign --force --sign "$CERT" {} \;

# 2. Sign main executable with entitlements
codesign --force --options runtime \
  --sign "$CERT" \
  --entitlements "$ENTITLEMENTS" \
  "$APP_DIR/Contents/MacOS/StoryCAD"

# 3. Sign the app bundle
codesign --force --options runtime \
  --sign "$CERT" \
  --entitlements "$ENTITLEMENTS" \
  "$APP_DIR"
```

### Step 4: Verify Signature

```bash
codesign --verify --deep --strict "$APP_DIR"

# Verify JIT entitlement is present
codesign -d --entitlements - "$APP_DIR/Contents/MacOS/StoryCAD"
```

### Step 5: Create PKG

```bash
# Clear quarantine before packaging
xattr -cr "$APP_DIR"

productbuild --component "$APP_DIR" /Applications \
  --sign "3rd Party Mac Developer Installer: StoryBuilder Foundation (3T4DPS2D5Y)" \
  ~/Desktop/StoryCAD-4.0.0.pkg

# Delete the build .app after creating pkg (prevents installer relocation bug)
rm -rf "$APP_DIR"
```

### Step 6: Upload via Transporter

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
**Fix:** Add `allow-jit` to Entitlements.plist. The .NET JIT compiler needs to allocate executable memory.

### "Bundle version must be higher"
Increment `CFBundleVersion` in Info.plist.

### "App sandbox not enabled"
Ensure entitlements include `com.apple.security.app-sandbox` set to `true`.

### "Missing provisioning profile"
Copy provisioning profile to `Contents/embedded.provisionprofile`.

### "Invalid bundle - arm64 only requires macOS 12.0"
Set `LSMinimumSystemVersion` to `12.0` in Info.plist.

### "Quarantine attribute" / signing failures after copying provisioning profile
Run `xattr -cr` on the app bundle **after** copying the provisioning profile and **before** signing. The `cp` command can reintroduce quarantine attributes.

### "Code object not signed in subcomponent"
Sign dylibs individually before signing the main executable and bundle. Don't use `--deep`.

### App Store validation rejects .json files in MacOS/
**Cause:** `.deps.json` and `.runtimeconfig.json` in `Contents/MacOS/` fail validation.
**Fix:** Move them to `Contents/Resources/` and create symlinks in `Contents/MacOS/` so the runtime can still find them.

### Installer relocation bug (app installs to wrong location)
**Cause:** If the `.app` bundle exists at the build path when the PKG is installed, macOS may "relocate" the install to that path instead of `/Applications`.
**Fix:** Delete the build `.app` after creating the PKG.

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
