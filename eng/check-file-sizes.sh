#!/usr/bin/env bash
# Enforces CODESTANDARDS.md section 2 file limits: .cs and .axaml files under src/ and tests/
# must not exceed MAX_LINES. Generated files and build output are exempt, and so is any file with a
# "size-limit-exempt: <reason>" marker in its first EXEMPT_MARKER_LINES lines (listed in the output).
set -euo pipefail

MAX_LINES=500
EXEMPT_MARKER_LINES=5

cd "$(dirname "$0")/.."

list_files() {
  if command -v git >/dev/null 2>&1 && git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    # Tracked plus new, not-ignored files, so the check also covers work that is not committed yet.
    git ls-files --cached --others --exclude-standard -- 'src/*.cs' 'src/*.axaml' 'tests/*.cs' 'tests/*.axaml'
  else
    find src tests \( -name '*.cs' -o -name '*.axaml' \) -type f 2>/dev/null
  fi
}

is_generated() {
  case "$1" in
    */obj/*|*/bin/*|*.g.cs|*.Designer.cs) return 0 ;;
    *) return 1 ;;
  esac
}

# A marker only counts when it gives a reason after the colon.
has_exempt_marker() {
  head -n "$EXEMPT_MARKER_LINES" "$1" | grep -Eq 'size-limit-exempt:[[:space:]]*[^[:space:]-]'
}

offenders=0
while IFS= read -r file; do
  [ -n "$file" ] || continue
  [ -f "$file" ] || continue
  if is_generated "$file"; then continue; fi
  lines=$(wc -l < "$file" | tr -d '[:space:]')
  if [ "$lines" -le "$MAX_LINES" ]; then continue; fi
  if has_exempt_marker "$file"; then
    echo "exempt: $file: $lines lines"
    continue
  fi
  echo "$file: $lines lines (limit $MAX_LINES)"
  offenders=$((offenders + 1))
done < <(list_files | sort -u)

if [ "$offenders" -gt 0 ]; then
  echo "$offenders file(s) exceed the $MAX_LINES-line limit (CODESTANDARDS.md section 2)." >&2
  exit 1
fi
echo "All files are within the $MAX_LINES-line limit."
