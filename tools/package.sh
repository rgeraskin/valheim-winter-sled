#!/usr/bin/env bash
# Makes the Thunderstore package dist/WinterSled-<version>.zip from the Release build (mise run package, CI).
set -euo pipefail
cd "$(dirname "$0")/.."
version=$(python3 -c 'import json;print(json.load(open("package/manifest.json"))["version_number"])')
plugin=$(sed -n 's/.*public const string Version = "\(.*\)";.*/\1/p' WinterSled/src/Plugin.cs)
if [ "$version" != "$plugin" ]; then
  echo "package/manifest.json says $version, Plugin.cs says $plugin" >&2
  exit 1
fi
rm -rf dist/pkg dist/WinterSled-*.zip && mkdir -p dist/pkg/plugins
cp package/manifest.json package/icon.png README.md CHANGELOG.md dist/pkg/
cp WinterSled/bin/Release/WinterSled.dll dist/pkg/plugins/
(cd dist/pkg && zip -qr "../WinterSled-$version.zip" .)
echo "dist/WinterSled-$version.zip"
