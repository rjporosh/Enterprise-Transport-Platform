#!/usr/bin/env bash
#
# Wraps `dotnet build` and, on failure only, appends the full captured
# output to logs/build-error-<dd-MM-yyyy>.txt at the repo root — one file
# per day (appended, not overwritten), so repeated failures the same day
# stay in one chronological record instead of flooding logs/ with
# near-identical files.
#
# Usage:
#   scripts/dotnet-build.sh <path-to-.csproj-or-.sln> [extra dotnet build args...]
#
# Examples:
#   scripts/dotnet-build.sh services/bus-service/BusService.sln
#   scripts/dotnet-build.sh services/booking-service/src/BookingService.Api/BookingService.Api.csproj -c Release
#
# On success: builds normally, no log entry written (only failures are
# logged). On failure: everything dotnet printed (errors, warnings, the
# works) is appended to the log file, with a timestamp, in addition to
# being shown in the terminal.

set -uo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 <path-to-.csproj-or-.sln> [extra dotnet build args...]" >&2
  exit 2
fi

TARGET="$1"
shift

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="$REPO_ROOT/logs"
mkdir -p "$LOG_DIR"

LOG_FILE="$LOG_DIR/build-error-$(date +%d-%m-%Y).txt"
CAPTURE_FILE="$(mktemp)"
trap 'rm -f "$CAPTURE_FILE"' EXIT

echo "==> dotnet build $TARGET $*"
dotnet build "$TARGET" "$@" 2>&1 | tee "$CAPTURE_FILE"
EXIT_CODE=${PIPESTATUS[0]}

if [ "$EXIT_CODE" -ne 0 ]; then
  {
    echo "------------------------------------------------------------------------"
    echo "BUILD FAILED"
    echo "Target:     $TARGET $*"
    echo "Exit code:  $EXIT_CODE"
    echo "Timestamp:  $(date -u +"%Y-%m-%dT%H:%M:%SZ") (UTC)"
    echo "Working dir: $(pwd)"
    echo "------------------------------------------------------------------------"
    cat "$CAPTURE_FILE"
    echo ""
  } >> "$LOG_FILE"
  echo ""
  echo "Build failed (exit $EXIT_CODE). Appended to:"
  echo "  $LOG_FILE"
else
  echo ""
  echo "Build succeeded — no log entry written."
fi

exit "$EXIT_CODE"
