---
title: Developing StoryCAD
layout: default
nav_enabled: true
nav_order: 108
parent: Miscellaneous
has_toc: false
---
## Developing StoryCAD
Developing StoryCAD
If you are a C# developer and are somewhat familar with WinUI (or another XAML based UI language) then you can contribute to StoryCAD (Which is written in C# and uses WinUI 3).
For more information about contributing, please check the GitHub Repository.
Developer only menus / pop-ups
![](KeyMissingError.png)

If you have cloned StoryCAD to a separate repo and built it for the first time then you may be surprised to see this screen. It indicates a key file related to licensing is missing from your local clone. These licenses are in effect for the storybuilder.org repo only. The missing licenses won’t cause any issues with the app functioning, but your copy won’t report errors via Elmah.io and you may see pops relating to syncfusion licensing errors.
Regardless, congratulations on successfully compiling StoryCAD.

![](DevTab.png)

If StoryCAD notices you have a debugger attached to the process, the developer menu will appear.
This shows info about the computer and may contain buttons to test some parts of the StoryCAD.
If running without a keyfile (which is standard for those contributing to the StoryCAD project.) then some of these buttons may not work or cause intended behavior.

As such this menu may be removed, updated or abandoned at any point.

Developer Notes
- Single Instancing whilst debugging in VS does work however the window may not be brought to the front and may only flash as VS will attempt to hide it again if it wasn't shown, to test Single Instancing related stuff do the following:
	- Run the app in VS (or Deploy.) so that it installs the app on your system.
	- Close the app.
	- Now launch the app from elsewhere (Such as the start menu or taskbar)
	- Hide the app behind other windows or minimise it
	- Attempt to launch the app again
	- The first instance of the app should now be brought on top of all the window

## Building for macOS App Store

StoryCAD 4.0+ supports macOS via the Uno Platform. This section covers building and submitting to the Mac App Store.

### Prerequisites

- macOS with Xcode Command Line Tools
- .NET SDK (version specified in `global.json`)
- Apple Developer account with certificates:
  - `Apple Distribution` certificate (for signing the app)
  - `3rd Party Mac Developer Installer` certificate (for signing the PKG)
- App Store provisioning profile for `com.storybuilder.storycad`
- Transporter app (from Mac App Store)

### Build Process

#### 1. Publish the App

```bash
dotnet publish StoryCAD/StoryCAD.csproj -c Release -f net10.0-desktop -r osx-arm64 --self-contained true
```

#### 2. Create App Bundle

The build output needs to be assembled into a proper .app bundle:

```bash
APP_DIR="/tmp/StoryCAD.app"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources"

# Copy published files
cp -R StoryCAD/bin/Release/net10.0-desktop/osx-arm64/publish/* "$APP_DIR/Contents/MacOS/"

# Move resources
mv "$APP_DIR/Contents/MacOS/Assets" "$APP_DIR/Contents/Resources/"
mv "$APP_DIR/Contents/MacOS/Uno.Fonts.OpenSans" "$APP_DIR/Contents/Resources/" 2>/dev/null || true
mv "$APP_DIR/Contents/MacOS/Uno.Fonts.Fluent" "$APP_DIR/Contents/Resources/" 2>/dev/null || true

# Copy required files
cp StoryCAD/Platforms/Desktop/Info.plist "$APP_DIR/Contents/Info.plist"
cp /tmp/icon.icns "$APP_DIR/Contents/Resources/icon.icns"
cp ~/Downloads/StoryCAD__Provisioning_Profile.provisionprofile "$APP_DIR/Contents/embedded.provisionprofile"

chmod +x "$APP_DIR/Contents/MacOS/StoryCAD"
xattr -cr "$APP_DIR"
```

#### 3. Sign the App

Sign in this exact order (do NOT use `--deep`):

```bash
# Sign all nested files first
find /tmp/StoryCAD.app/Contents/MacOS -type f ! -name "StoryCAD" ! -name "createdump" \
  -exec codesign --force --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" {} \; 2>/dev/null
find /tmp/StoryCAD.app/Contents/Resources -type f \
  -exec codesign --force --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" {} \; 2>/dev/null

# Sign helper executable (sandbox only)
codesign --force --options runtime \
  --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" \
  --entitlements /tmp/helper-entitlements.plist \
  /tmp/StoryCAD.app/Contents/MacOS/createdump

# Sign main executable
codesign --force --options runtime \
  --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" \
  --entitlements /tmp/entitlements.plist \
  /tmp/StoryCAD.app/Contents/MacOS/StoryCAD

# Sign the bundle
codesign --force --options runtime \
  --sign "Apple Distribution: StoryBuilder Foundation (3T4DPS2D5Y)" \
  --entitlements /tmp/entitlements.plist \
  /tmp/StoryCAD.app
```

#### 5. Create PKG Installer

```bash
productbuild --component /tmp/StoryCAD.app /Applications \
  --sign "3rd Party Mac Developer Installer: StoryBuilder Foundation (3T4DPS2D5Y)" \
  ~/Desktop/StoryCAD-4.0.0.pkg
```

#### 6. Upload via Transporter

Open Transporter app and drag the PKG file to upload to App Store Connect.

### Before Each Upload

Increment `CFBundleVersion` in `StoryCAD/Platforms/Desktop/Info.plist`:
```xml
<key>CFBundleVersion</key>
<string>8</string>  <!-- Increment this number -->
```

### Common Issues

| Error | Fix |
|-------|-----|
| Bundle version already used | Increment `CFBundleVersion` in Info.plist |
| App sandbox not enabled | Ensure entitlements include `com.apple.security.app-sandbox` |
| Missing provisioning profile | Embed profile at `Contents/embedded.provisionprofile` |
| Entitlements don't match profile | Main app needs sandbox + app-identifier; helpers need sandbox only |
| Quarantine attribute | Run `xattr -cr` on app bundle before signing |
