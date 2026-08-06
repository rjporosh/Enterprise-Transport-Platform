#!/usr/bin/env bash
#
# Wraps `dotnet run` and, if the app crashes or fails to start, appends the
# full captured output to logs/runtime-error-<dd-MM-yyyy>.txt at the repo
# root — one file per day. This is the external safety net: each service
# also has its own in-app crash handler (see
# src/<Service>.Api/Diagnostics/RuntimeErrorLogWriter.cs, wired into
# Program.cs) that writes a richer entry with a plain-English diagnosis
# into that SERVICE's own logs/ folder. Both exist because the in-app
# handler can only catch what .NET's try/catch can catch — this script
# still records *something* even for the failure modes that can't be
# caught in-process at all.
#
# Usage:
#   scripts/dotnet-run.sh <path-to-.csproj> [extra dotnet run args...]
#
# Examples:
#   scripts/dotnet-run.sh services/bus-service/src/BusService.Api
#   scripts/dotnet-run.sh services/auth-service/src/AuthService.Api --urls=http://localhost:5101
#
# A normal Ctrl+C shutdown is NOT treated as a run error (SIGINT/SIGTERM
# exit codes are recognized and skipped) — only a real crash or a failed
# startup writes a log entry.

set -uo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 <path-to-project-directory-or-.csproj> [extra dotnet run args...]" >&2
  exit 2
fi

TARGET="$1"
shift

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="$REPO_ROOT/logs"
mkdir -p "$LOG_DIR"

LOG_FILE="$LOG_DIR/runtime-error-$(date +%d-%m-%Y).txt"
CAPTURE_FILE="$(mktemp)"
trap 'rm -f "$CAPTURE_FILE"' EXIT

echo "==> dotnet run --project $TARGET $*"
dotnet run --project "$TARGET" "$@" 2>&1 | tee "$CAPTURE_FILE"
EXIT_CODE=${PIPESTATUS[0]}

# 130 = 128+SIGINT (Ctrl+C), 143 = 128+SIGTERM — both are normal,
# intentional shutdowns, not run errors.
if [ "$EXIT_CODE" -ne 0 ] && [ "$EXIT_CODE" -ne 130 ] && [ "$EXIT_CODE" -ne 143 ]; then
  {
    echo "------------------------------------------------------------------------"
    echo "RUN FAILED"
    echo "Target:     $TARGET $*"
    echo "Exit code:  $EXIT_CODE"
    echo "Timestamp:  $(date -u +"%Y-%m-%dT%H:%M:%SZ") (UTC)"
    echo "Working dir: $(pwd)"
    echo "------------------------------------------------------------------------"
    cat "$CAPTURE_FILE"
    echo ""
  } >> "$LOG_FILE"
  echo ""
  echo "Run failed / crashed (exit $EXIT_CODE). Appended to:"
  echo "  $LOG_FILE"
fi

exit "$EXIT_CODE"
