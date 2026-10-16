#!/usr/bin/env bash
# Copyright (C) 2026 SharpShooter contributors
# SPDX-License-Identifier: GPL-2.0-or-later
#
# Adds SharpShooter to your application menu for the current user. No root needed.
# Run it from the extracted release folder:
#   ./install.sh               install or update
#   ./install.sh --uninstall   remove the app, icon, and menu entry (keeps settings)

set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
data="${XDG_DATA_HOME:-$HOME/.local/share}"
target="$HOME/.local/bin/sharpshooter"
icon="$data/icons/hicolor/256x256/apps/sharpshooter.png"
entry="$data/applications/sharpshooter.desktop"

refresh() {
  command -v update-desktop-database > /dev/null && update-desktop-database "$data/applications" || true
  command -v gtk-update-icon-cache > /dev/null && gtk-update-icon-cache -q "$data/icons/hicolor" || true
}

if [[ "${1:-}" == "--uninstall" ]]; then
  rm -f "$target" "$icon" "$entry"
  refresh
  echo "SharpShooter removed. Settings remain in $data/SharpShooter."
  exit 0
fi

mkdir -p "$(dirname "$target")" "$(dirname "$icon")" "$(dirname "$entry")"
install -m 755 "$here/SharpShooter" "$target"
install -m 644 "$here/sharpshooter.png" "$icon"

# Exec uses the absolute path, quoted in case the home folder has spaces.
cat > "$entry" <<EOF
[Desktop Entry]
Type=Application
Name=SharpShooter
GenericName=Log Analyzer
Comment=Analyze and compare SharpEmu logs
Exec="$target"
Icon=sharpshooter
Terminal=false
Categories=Development;Debugger;
StartupWMClass=SharpShooter
EOF
chmod 644 "$entry"
refresh

echo "SharpShooter installed. Open it from your application menu, or run: sharpshooter"
