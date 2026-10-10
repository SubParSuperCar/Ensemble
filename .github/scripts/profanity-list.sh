#!/bin/sh
# Converts the chat profanity list between its committed form (GZip, then Base64) and plain JSON.
# Usage: profanity-list.sh decode > list.json | profanity-list.sh encode < list.json
set -eu

encoded="$(dirname "$0")/../../Common/Text/ProfanityList.gz.b64"

case "${1:-}" in
	decode)
		base64 -d "${encoded}" | gunzip
		;;
	encode)
		gzip -9n | base64 -w 120 > "${encoded}"
		;;
	*)
		printf 'Usage: %s decode|encode\n' "$0" >&2
		exit 2
		;;
esac
