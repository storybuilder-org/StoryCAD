#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
OUT="${1:-./out}"
mkdir -p "$OUT"
swiftc -O -emit-library -module-name StoryCADStoreKit \
  -target arm64-apple-macos12.0 StoreKitShim.swift -o "$OUT/libStoryCADStoreKit-arm64.dylib"
swiftc -O -emit-library -module-name StoryCADStoreKit \
  -target x86_64-apple-macos12.0 StoreKitShim.swift -o "$OUT/libStoryCADStoreKit-x86_64.dylib"
lipo -create "$OUT"/libStoryCADStoreKit-{arm64,x86_64}.dylib -output "$OUT/libStoryCADStoreKit.dylib"
install_name_tool -id "@rpath/libStoryCADStoreKit.dylib" "$OUT/libStoryCADStoreKit.dylib"
