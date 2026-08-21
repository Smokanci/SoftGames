#!/bin/sh
# Claude Code PostToolUse guard — the same hard bans hooks/pre-commit enforces,
# but fired the moment a .cs file is edited instead of at commit time, so the
# violation lands in context next to the edit that introduced it.
#
# Reads the PostToolUse payload on stdin, scans only the lines this working tree
# adds relative to HEAD (so legacy code never trips it), and exits 2 on a hit —
# which feeds the message below back to Claude. It cannot undo the edit; the
# commit-time hook is still the gate.
#
# Wired from .claude/settings.json. Escape hatch: a trailing SG-ALLOW comment.

payload=$(cat)
file=$(printf '%s' "$payload" | python3 -c \
  'import json,sys
d = json.load(sys.stdin)
print((d.get("tool_input") or {}).get("file_path", ""))' 2>/dev/null)

# A guard that swallows its own failure is worse than no guard — it reports clean
# forever and nobody finds out. Emptiness is the check, not python3's exit code: a
# renamed payload key returns "" through a perfectly successful parse, which is the
# likeliest way this breaks. The hook is wired to Edit|Write, both of which always
# carry a file_path, so blank always means something changed underneath us.
if [ -z "$file" ]; then
  echo "[sg] cs-guard did not run: no file_path in the PostToolUse payload." >&2
  echo "     Either the payload shape changed or python3 is unavailable." >&2
  echo "     The hard bans are unenforced at edit time until this is fixed;" >&2
  echo "     hooks/pre-commit is still the gate." >&2
  exit 0
fi

case "$file" in
  *.cs) ;;
  *) exit 0 ;;
esac

root=$(git rev-parse --show-toplevel 2>/dev/null) || exit 0
rel=${file#"$root"/}

if git ls-files --error-unmatch -- "$file" >/dev/null 2>&1; then
  added=$(git diff HEAD -U0 -- "$file" | grep '^+' | grep -v '^+++')
else
  # Untracked: every line is new.
  added=$(sed 's/^/+/' "$file" 2>/dev/null)
fi
added=$(printf '%s\n' "$added" | grep -v 'SG-ALLOW')
[ -z "$added" ] && exit 0

fail=0
report() {
  # $1 = message, $2 = matching lines
  echo "[sg] $1" >&2
  printf '%s\n' "$2" | sed 's/^+/    /' >&2
  fail=1
}

hit=$(printf '%s\n' "$added" | grep -E 'DontDestroyOnLoad')
[ -n "$hit" ] && report "DontDestroyOnLoad — persistent services live in the bootstrap scene, not behind DDOL" "$hit"

# Resolved beside this script, not under the edited file's repo: the two are the
# same in this project, but keying off $root means a missing scanner degrades to
# "no wired idents, nothing to check" and the null-guard ban stops being enforced
# without saying so.
scanner="$(dirname "$0")/wired-idents.sh"
if [ ! -f "$scanner" ]; then
  echo "[sg] cs-guard: $scanner is missing — the null-guard ban is NOT being" >&2
  echo "     checked at edit time. hooks/pre-commit is still the gate." >&2
  idents=""
else
  idents=$(sh "$scanner" "$file" worktree | paste -sd'|' -)
fi
if [ -n "$idents" ]; then
  hit=$(printf '%s\n' "$added" | grep -E "(^|[^A-Za-z0-9_])($idents)[[:space:]]*([!=]=[[:space:]]*null|\?[.?])|null[[:space:]]*[!=]=[[:space:]]*($idents)([^A-Za-z0-9_]|$)")
  [ -n "$hit" ] && report "null guard on a wired ref (serialized field / GetComponent result) — let the NullReferenceException surface" "$hit"
fi

if [ "$fail" -ne 0 ]; then
  echo "" >&2
  echo "[sg] $rel violates a hard ban in .claude/rules/code-conventions.md." >&2
  echo "     Fix it now — or, for a justified exception, add a trailing" >&2
  echo "     'SG-ALLOW' comment to the offending line." >&2
  exit 2
fi
exit 0
