#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
OUT="${1:-./out}"
mkdir -p "$OUT"
# Local builds compile for this Mac's chip only. UNIVERSAL=1 also builds x86_64,
# which needs a toolchain whose Swift libraries still include x86_64.
if [[ "${UNIVERSAL:-0}" == "1" ]]; then ARCHS=(arm64 x86_64); else ARCHS=("$(uname -m)"); fi
SLICES=()
for ARCH in "${ARCHS[@]}"; do
  swiftc -O -emit-library -module-name StoryCADStoreKit \
    -target "$ARCH-apple-macos12.0" StoreKitShim.swift -o "$OUT/libStoryCADStoreKit-$ARCH.dylib"
  SLICES+=("$OUT/libStoryCADStoreKit-$ARCH.dylib")
done
lipo -create "${SLICES[@]}" -output "$OUT/libStoryCADStoreKit.dylib"
install_name_tool -id "@rpath/libStoryCADStoreKit.dylib" "$OUT/libStoryCADStoreKit.dylib"
