#!/usr/bin/env bash
# Copyright (C) 2026 SharpShooter contributors
# SPDX-License-Identifier: GPL-2.0-or-later
#
# Wraps a published macOS executable in SharpShooter.app. Runs on macOS only
# (uses sips, iconutil, and codesign from the base system).
#
# Usage: make-app.sh <publish-dir> <version> <output-dir>

set -euo pipefail

publish="${1:?publish directory}"
version="${2:?version}"
output="${3:?output directory}"
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"
app="$output/SharpShooter.app"

# Bundle versions must be numeric, so 0.1.0-ci.42 becomes 0.1.0.
short="${version%%-*}"

rm -rf "${app:?}"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cp "$publish/SharpShooter" "$app/Contents/MacOS/SharpShooter"
chmod +x "$app/Contents/MacOS/SharpShooter"
sed "s/@VERSION@/$short/g" "$here/Info.plist" > "$app/Contents/Info.plist"
cp -R "$root/LICENSES" "$app/Contents/Resources/LICENSES"

# Build the .icns from the source icon at every size macOS asks for.
work="$(mktemp -d)"
iconset="$work/SharpShooter.iconset"
mkdir -p "$iconset"
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$root/Assets/icon.png" --out "$iconset/icon_${size}x${size}.png" > /dev/null
  double=$((size * 2))
  sips -z "$double" "$double" "$root/Assets/icon.png" --out "$iconset/icon_${size}x${size}@2x.png" > /dev/null
done
iconutil -c icns "$iconset" -o "$app/Contents/Resources/SharpShooter.icns"
rm -rf "${work:?}"

# Ad-hoc signature. This is not a developer certificate; Apple Silicon will not
# run an unsigned binary, and editing the bundle invalidates the build's signature.
codesign --force --deep --sign - "$app"
codesign --verify --deep --strict "$app"

echo "Created $app ($short)"
