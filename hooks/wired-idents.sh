#!/bin/sh
# Prints the identifiers in a C# file that are expected to be wired and so must
# not be null-guarded: [SerializeField] fields (incl. the attribute-on-its-own-
# line form) and anything assigned from a GetComponent* call. Anything the file
# also assigns "= null" is dropped — a ref the code deliberately clears is
# legitimately nullable state.
#
# Usage: wired-idents.sh <file> [staged|worktree]   (default: worktree)
#
# Shared by hooks/pre-commit (staged content, so declarations added in the same
# commit are seen) and hooks/claude-cs-guard.sh (working-tree content, so the
# edit Claude just made is seen). One implementation — the two enforcement
# points must agree on what counts as a wired ref.

f=$1
mode=${2:-worktree}

content() {
  if [ "$mode" = staged ]; then
    git show ":$f" 2>/dev/null
  else
    cat "$f" 2>/dev/null
  fi
}

all=$(
  # Comments are stripped before the scan, not inside it: an own-line
  # [SerializeField] followed by a comment that happens to contain "x = 1" would
  # consume the pending flag — the comment's word gets reported as the wired ident
  # and the actual field below it is never reported at all. A field that silently
  # drops off this list is unguarded at both enforcement points.
  content | sed -E 's|//.*$||' | awk '
    /\[SerializeField/ { pending = 1 }
    pending {
      # Strip string literals, then generic argument lists, then attribute groups,
      # so neither an "=" inside a [Tooltip("Higher = ...")] nor the comma in a
      # Dictionary<string, int> can be mistaken for a declarator.
      line = $0
      gsub(/"[^"]*"/, "", line)
      while (sub(/<[^<>]*>/, "", line)) { }
      gsub(/\[[^]]*\]/, "", line)
      # Loop rather than match once: "private float lo, hi;" declares two wired
      # fields, and only continuing past a comma finds the second. Any other
      # terminator ends the declarator list — keep scanning and "= 1f;" would
      # contribute a bogus "f".
      while (match(line, /[A-Za-z_][A-Za-z0-9_]*[[:space:]]*[;=,]/)) {
        d = substr(line, RSTART, RLENGTH)
        sep = substr(d, length(d), 1)
        sub(/[[:space:]]*[;=,]$/, "", d)
        print d
        pending = 0
        line = substr(line, RSTART + RLENGTH)
        if (sep != ",") { break }
      }
    }
  '
  content |
    sed -nE 's/^[[:space:]]*(var[[:space:]]+)?([A-Za-z_][A-Za-z0-9_]*)[[:space:]]*=.*GetComponent.*/\2/p'
)

# Comments then strings stripped, in that order, so neither a commented-out
# "// _x = null;" nor a tooltip mentioning "x = null" can exempt a real field —
# the exemption must come from code that actually runs. Matches the clear
# anywhere on the line, not just at line start, so a single-line body
# (private void OnDestroy() { _x = null; }) still counts.
cleared=$(
  content | sed -E 's|//.*$||; s/"[^"]*"//g' |
    sed -nE 's/.*(^|[({;[:space:]])(this\.)?([A-Za-z_][A-Za-z0-9_]*)[[:space:]]*=[[:space:]]*null[[:space:]]*;.*/\3/p'
)

if [ -n "$cleared" ]; then
  printf '%s\n' "$all" | sort -u | grep -vxF "$cleared"
else
  printf '%s\n' "$all" | sort -u
fi
