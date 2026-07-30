#!/usr/bin/env sh

echo "Runtime context: POSIX Shell"

SCRIPT_DIR="$(cd -- "$(dirname -- "$0")" && pwd)"
PROJECT_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
LOCAL_BIN="$PROJECT_DIR/bin"

GD_CANDIDATES="godot godot4 godot-mono"

for name in $GD_CANDIDATES; do
    if [ -x "$LOCAL_BIN/$name" ]; then
        echo "Found via local bin: $LOCAL_BIN/$name"
        exec "$LOCAL_BIN/$name" "$@"
    fi
done

for name in $GD_CANDIDATES; do
    path="$(command -v "$name" 2>/dev/null)"
    if [ -n "$path" ]; then
        echo "Found via PATH ($name): $path"
        exec "$path" "$@"
    fi
done

echo "Godot not found in local bin or via PATH." >&2
exit 1
