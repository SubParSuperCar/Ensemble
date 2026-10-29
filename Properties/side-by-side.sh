#!/bin/sh

# Hosts a session and joins it from a second window, each filling half the screen, for trying multiplayer locally

SCRIPT_DIR="$(cd -- "$(dirname -- "$0")" && pwd)"
PROJECT_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"

PORT=7777
WIDTH=1280
HEIGHT=1380

"$SCRIPT_DIR/launch.sh" --path "$PROJECT_DIR" --windowed --resolution "${WIDTH}x$HEIGHT" --position 0,0 \
	-- --port=$PORT --name=Host &

# Gives the host a head start, so the client's first attempt finds it listening
sleep 2

"$SCRIPT_DIR/launch.sh" --path "$PROJECT_DIR" --windowed --resolution "${WIDTH}x$HEIGHT" --position "$WIDTH,0" \
	-- --join=127.0.0.1:$PORT --name=Client &

wait
