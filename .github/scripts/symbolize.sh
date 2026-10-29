#!/bin/sh
set -eu

dbg="${1:-$(dirname "$0")/EnsembleGame.so.dbg}"

while IFS= read -r line; do
	case "${line}" in
		*EnsembleGame.so+0x*)
			offset="${line##*EnsembleGame.so+}"
			offset="${offset%%[!0-9a-fx]*}"
			# Frame addresses are return addresses, which point just past their calls
			printf '%s  %s\n' "${line}" \
				"$(addr2line -fC -e "${dbg}" "$(printf '0x%x' $((offset - 1)))" | paste -sd ' ')"
			;;
		*)
			printf '%s\n' "${line}"
			;;
	esac
done
