#!/usr/bin/env bash
#
# Downloads the Slang compiler this checkout builds its shaders with, into build/tools/slang.
#
# The module's Slang is compiled by its tests to SPIR-V and to WGSL, the two targets the engines
# build it for, so a checkout needs slangc before those tests can run. The tests look for it at
# build/tools/slang/bin/slangc, walking up from the tests' folder, so nothing has to be put on the
# PATH. RADIANCECASCADES_SLANGC wins when it is set.
#
# An engine that takes the module compiles the module's Slang with the slangc of its own checkout,
# so this copy serves the module's tests alone.
#
# The version is pinned to the one both engines pin, so the tests compile the SPIR-V and the WGSL
# the engines do. Pass --force to download it again over what is there.
#
# Usage:
#   build/fetch-slang.sh
#   build/fetch-slang.sh --force
#
set -euo pipefail

VERSION="2026.18.2"

BUILD_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TOOLS_DIR="$BUILD_DIR/tools"
SLANG_DIR="$TOOLS_DIR/slang"
STAMP="$SLANG_DIR/.version"

if [[ "${1:-}" != "--force" && -f "$STAMP" && "$(cat "$STAMP")" == "$VERSION" ]]; then
    echo "==> slangc $VERSION is already at $SLANG_DIR"
    exit 0
fi

case "$(uname -s)" in
    Linux)  os="linux" ;;
    Darwin) os="macos" ;;
    MINGW*|MSYS*|CYGWIN*) os="windows" ;;
    *) echo "No Slang release for $(uname -s). Install slangc and set RADIANCECASCADES_SLANGC." >&2; exit 1 ;;
esac

case "$(uname -m)" in
    x86_64|amd64) arch="x86_64" ;;
    arm64|aarch64) arch="aarch64" ;;
    *) echo "No Slang release for $(uname -m). Install slangc and set RADIANCECASCADES_SLANGC." >&2; exit 1 ;;
esac

# The glibc 2.28 build on Linux, which is the oldest floor Slang publishes for both architectures.
# The tar archive on every system, Windows' among them, which the tar of Git for Windows' bash
# reads there.
if [[ "$os" == "linux" ]]; then
    archive="slang-$VERSION-linux-$arch-glibc-2.28.tar.gz"
else
    archive="slang-$VERSION-$os-$arch.tar.gz"
fi

url="https://github.com/shader-slang/slang/releases/download/v$VERSION/$archive"
download="$TOOLS_DIR/$archive"

mkdir -p "$TOOLS_DIR"
echo "==> downloading $url"
curl -fsSL --retry 3 -o "$download" "$url"

rm -rf "$SLANG_DIR"
mkdir -p "$SLANG_DIR"

tar -xzf "$download" -C "$SLANG_DIR"

rm -f "$download"
echo "$VERSION" > "$STAMP"

"$SLANG_DIR/bin/slangc" -v >/dev/null
echo "==> slangc $VERSION is at $SLANG_DIR/bin"
