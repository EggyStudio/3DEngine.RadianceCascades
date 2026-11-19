#!/usr/bin/env bash
#
# Downloads the Slang compiler this checkout builds its shaders with, into build/tools/slang.
#
# The module's Slang is compiled by its tests to SPIR-V, WGSL, HLSL and Metal, so a checkout needs
# slangc before those tests can run. The tests look for it at
# build/tools/slang/bin/slangc, walking up from the tests' folder, so nothing has to be put on the
# PATH. RADIANCECASCADES_SLANGC wins when it is set.
#
# An engine that takes the module compiles the module's Slang with the slangc of its own checkout,
# so this copy serves the module's tests alone.
#
# The version is pinned, so every checkout's tests compile the same SPIR-V and WGSL. Pass --force
# to download it again over what is there.
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

# Each archive's sha256 as the release lists it, so one of other bytes is refused. A new version
# fails here until its sums are written.
case "$archive" in
    slang-2026.18.2-linux-x86_64-glibc-2.28.tar.gz) sum=991f521829100d06846a2697425d588f604f3bf3a23a6bfc499136585f7e6a6f ;;
    slang-2026.18.2-linux-aarch64-glibc-2.28.tar.gz) sum=56fe614bc019c9b53cd1de01537c4ac8d95120e46f4bb6b9cce56e916883f55c ;;
    slang-2026.18.2-windows-x86_64.tar.gz) sum=81a2e749480b136ab7ee0060f55be561eb19131b69d2f81652b147bcbb7a1865 ;;
    slang-2026.18.2-windows-aarch64.tar.gz) sum=42db8014d9b565a4569fe64d2e44e428f5efc54ba4c546bb6b275b5b55f09cd8 ;;
    slang-2026.18.2-macos-x86_64.tar.gz) sum=d09a2993b4ed6d81c0c19ff8bede27944bf73437d982549bd7b2ac5a1a3202b9 ;;
    slang-2026.18.2-macos-aarch64.tar.gz) sum=410c0ed14e231089a58437bb0d8e00389c48e29ed9ffe23e3b04a5c74f1ce809 ;;
    *) echo "No sha256 is written here for $archive. Write the one its release lists." >&2; exit 1 ;;
esac
# shasum reads a file's sha256 on macOS, on Linux and in Git's bash on Windows alike, and Python's
# hashlib where a system has no shasum.
sha256() {
    if command -v shasum > /dev/null; then
        shasum -a 256 "$1" | cut -d ' ' -f 1
    else
        python3 -I -c 'import hashlib, sys; print(hashlib.sha256(open(sys.argv[1], "rb").read()).hexdigest())' "$1"
    fi
}

mkdir -p "$TOOLS_DIR"
echo "==> downloading $url"
curl -fsSL --retry 3 -o "$download" "$url"
if [ "$(sha256 "$download")" != "$sum" ]; then
    rm -f "$download"
    echo "$url is not the archive its release lists, its sha256 not $sum" >&2
    exit 1
fi

rm -rf "$SLANG_DIR"
mkdir -p "$SLANG_DIR"

tar -xzf "$download" -C "$SLANG_DIR"

rm -f "$download"

# The stamp is written once the compiler runs, so one that does not is fetched again next time.
"$SLANG_DIR/bin/slangc" -v >/dev/null
echo "$VERSION" > "$STAMP"
echo "==> slangc $VERSION is at $SLANG_DIR/bin"
