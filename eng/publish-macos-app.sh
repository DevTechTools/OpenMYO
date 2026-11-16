#!/usr/bin/env bash
# Publishes OpenMyo as a macOS app bundle: publish/OpenMyo.app plus publish/OpenMyo-macos-<arch>.zip.
# Usage: bash eng/publish-macos-app.sh [arm64|x64]   (defaults to this Mac's architecture)
# Runs on macOS only (needs sips, iconutil, codesign and ditto). The bundle is ad-hoc signed, so it runs
# on this Mac; Gatekeeper still warns on other Macs until it is signed with a Developer ID and notarized.
set -euo pipefail

BUNDLE_ID="io.github.openmyo"
APP_NAME="OpenMyo"
PROJECT="src/OpenMyo.App"
ICON_SOURCE="src/OpenMyo.App/Assets/openmyo-dock.png" # 512 px, already padded to Apple's icon grid

cd "$(dirname "$0")/.."

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "This script builds a macOS app bundle and must run on macOS." >&2
  exit 1
fi

arch="${1:-$(uname -m)}"
case "$arch" in
  arm64) rid="osx-arm64" ;;
  x64 | x86_64) arch="x64"; rid="osx-x64" ;;
  *) echo "Unknown architecture '$arch'. Use arm64 or x64." >&2; exit 1 ;;
esac

out="publish"
staging="$out/$rid"
app="$out/$APP_NAME.app"
zip="$out/$APP_NAME-macos-$arch.zip"

msbuild_property() {
  dotnet msbuild "$PROJECT" -getProperty:"$1" -p:RuntimeIdentifier="$rid" 2>/dev/null | tail -n 1
}
version="$(msbuild_property Version)"
copyright="$(msbuild_property Copyright)"

echo "Publishing $APP_NAME $version for $rid..."
rm -rf "$staging" "$app" "$zip"
dotnet publish "$PROJECT" -c Release -r "$rid" -o "$staging" --nologo

echo "Assembling $app..."
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
# The executable looks for LibVLC in libvlc/<rid>/ beside itself. LibVLC also ships scripts and data,
# which codesign rejects inside Contents/MacOS, so it lives in Resources with a symlink where it's expected.
cp -R "$staging/." "$app/Contents/MacOS/"
mv "$app/Contents/MacOS/libvlc" "$app/Contents/Resources/libvlc"
ln -s ../Resources/libvlc "$app/Contents/MacOS/libvlc"

iconset="$(mktemp -d)/$APP_NAME.iconset"
mkdir -p "$iconset"
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$ICON_SOURCE" --out "$iconset/icon_${size}x${size}.png" >/dev/null
  double=$((size * 2))
  if ((double <= 512)); then
    sips -z "$double" "$double" "$ICON_SOURCE" --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
  fi
done
iconutil -c icns "$iconset" -o "$app/Contents/Resources/$APP_NAME.icns"
rm -rf "$(dirname "$iconset")"

cat >"$app/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key><string>en</string>
  <key>CFBundleDisplayName</key><string>$APP_NAME</string>
  <key>CFBundleExecutable</key><string>$APP_NAME</string>
  <key>CFBundleIconFile</key><string>$APP_NAME</string>
  <key>CFBundleIdentifier</key><string>$BUNDLE_ID</string>
  <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
  <key>CFBundleName</key><string>$APP_NAME</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$version</string>
  <key>CFBundleVersion</key><string>$version</string>
  <key>LSApplicationCategoryType</key><string>public.app-category.music</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>NSHumanReadableCopyright</key><string>$copyright</string>
</dict>
</plist>
PLIST
plutil -lint "$app/Contents/Info.plist" >/dev/null

echo "Signing (ad-hoc)..."
# Sign nested code first, then the bundle, so every Mach-O carries a valid signature.
find "$app/Contents/Resources/libvlc" -type f \( -name '*.dylib' -o -name '*.so' \) -print0 |
  xargs -0 codesign --force --sign - --timestamp=none
codesign --force --sign - --timestamp=none "$app"
codesign --verify --strict --deep "$app"

echo "Zipping..."
ditto -c -k --keepParent "$app" "$zip"
rm -rf "$staging"

echo "Done:"
echo "  $app"
echo "  $zip"
