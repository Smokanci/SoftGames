#!/bin/sh
# Publishes Builds/WebGL to the gh-pages branch. Build first: Unity menu Build > WebGL,
# or headlessly with the -executeMethod line in README.md.
set -e

ROOT=$(cd "$(dirname "$0")/.." && pwd)
BUILD="$ROOT/Builds/WebGL"

[ -f "$BUILD/index.html" ] || { echo "no build at $BUILD"; exit 1; }

WORKTREE=$(mktemp -d)

cleanup() {
  git -C "$ROOT" worktree remove --force "$WORKTREE" 2>/dev/null || true
  rm -rf "$WORKTREE"
}
trap cleanup EXIT

git -C "$ROOT" fetch origin gh-pages 2>/dev/null || true
if git -C "$ROOT" show-ref --verify --quiet refs/remotes/origin/gh-pages; then
  git -C "$ROOT" worktree add --force "$WORKTREE" origin/gh-pages
  git -C "$WORKTREE" checkout -B gh-pages
else
  git -C "$ROOT" worktree add --detach "$WORKTREE"
  git -C "$WORKTREE" checkout --orphan gh-pages
fi

find "$WORKTREE" -mindepth 1 -maxdepth 1 ! -name '.git' -exec rm -rf {} +
cp -R "$BUILD/." "$WORKTREE"/
# GitHub Pages runs Jekyll otherwise, which drops paths it treats as private.
touch "$WORKTREE/.nojekyll"

git -C "$WORKTREE" add -A
git -C "$WORKTREE" commit -qm "Publish WebGL build" || { echo "nothing changed"; exit 0; }
git -C "$WORKTREE" push -f origin gh-pages
echo "pushed. live at https://smokanci.github.io/SoftGames/"
