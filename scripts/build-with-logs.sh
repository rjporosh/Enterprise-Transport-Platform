#!/usr/bin/env bash
# Wraps `dotnet build` and captures every compiler / NuGet diagnostic into a
# structured file at logs/build-errors/build-error-dd-MM-yyyy.txt so build
# failures have one obvious place to read: project, file, line, column, code,
# message, and a suggested fix for the common ones.
#
# Usage:  scripts/build-with-logs.sh [<solution-or-project> ...]
#         (no args → builds every services/*/*.sln + shared + gateway)
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="$ROOT/logs/build-errors"
mkdir -p "$LOG_DIR"
LOG_FILE="$LOG_DIR/build-error-$(date -u +%d-%m-%Y).txt"

targets=("$@")
if [ ${#targets[@]} -eq 0 ]; then
  mapfile -t targets < <(find "$ROOT/services" -maxdepth 2 -name '*.sln'; \
                         echo "$ROOT/shared/Platform.Shared.sln"; \
                         echo "$ROOT/infrastructure/gateway/Platform.Gateway.sln")
fi

suggest() {
  case "$1" in
    CS0246|CS0234) echo "Missing using / project reference, or a typo in the type name." ;;
    CS1061)        echo "The member doesn't exist on that type — check for a signature change or a wrong overload." ;;
    CS7036)        echo "A required constructor/method parameter has no argument — a signature changed; update the call site." ;;
    CS0104)        echo "Ambiguous type — fully-qualify it or add a using alias." ;;
    NU1608)        echo "Package version outside a dependency's constraint — pin it or add NoWarn if deliberate (see bus-service)." ;;
    NU1903|NU1902) echo "Package has a known vulnerability — bump to a patched version or pin a safe transitive." ;;
    *)             echo "See https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/" ;;
  esac
}

overall=0
{
  echo "------------------------------------------------------------------------------"
  echo "BUILD RUN  $(date -u '+%Y-%m-%d %H:%M:%S') UTC"
  echo "------------------------------------------------------------------------------"
} >> "$LOG_FILE"

for t in "${targets[@]}"; do
  echo "building $t"
  output="$(dotnet build "$t" -clp:NoSummary --nologo 2>&1)"
  status=$?
  [ $status -ne 0 ] && overall=$status

  while IFS= read -r line; do
    file="$(echo "$line" | sed -E 's/^(.*)\(([0-9]+),([0-9]+)\): (error|warning) ([A-Z]+[0-9]+):.*/\1/')"
    ln="$(echo "$line"   | sed -E 's/^(.*)\(([0-9]+),([0-9]+)\): (error|warning) ([A-Z]+[0-9]+):.*/\2/')"
    col="$(echo "$line"  | sed -E 's/^(.*)\(([0-9]+),([0-9]+)\): (error|warning) ([A-Z]+[0-9]+):.*/\3/')"
    sev="$(echo "$line"  | sed -E 's/.*: (error|warning) ([A-Z]+[0-9]+):.*/\1/')"
    code="$(echo "$line" | sed -E 's/.*: (error|warning) ([A-Z]+[0-9]+):.*/\2/')"
    msg="$(echo "$line"  | sed -E 's/.*: (error|warning) [A-Z]+[0-9]+: (.*)/\2/')"
    {
      echo ""
      echo "[$sev $code]  target: $t"
      echo "  File     : $file"
      echo "  Line/Col : ${ln:-?} / ${col:-?}"
      echo "  Message  : $msg"
      echo "  Fix      : $(suggest "$code")"
    } >> "$LOG_FILE"
  done < <(echo "$output" | grep -E ': (error|warning) (CS|NU|MSB)[0-9]+')
done

if [ $overall -eq 0 ]; then
  echo "OK — 0 errors. Diagnostics (if any) appended to $LOG_FILE"
else
  echo "BUILD FAILED — structured diagnostics in $LOG_FILE"
fi
exit $overall
