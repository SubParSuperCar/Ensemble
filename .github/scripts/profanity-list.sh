#!/bin/sh
# Converts the chat profanity list between its committed form (GZip) and plain JSON.
# Usage: profanity-list.sh decode > list.json | profanity-list.sh encode < list.json
set -eu

compressed="$(dirname "$0")/../../Common/Text/ProfanityList.json.gz"

case "${1:-}" in
	decode)
		gunzip -c "${compressed}"
		;;
	encode)
		gzip -9n > "${compressed}"
		;;
	*)
		printf 'Usage: %s decode|encode\n' "$0" >&2
		exit 2
		;;
esac
