---
name: commit
description: Commit currently staged changes — split into multiple commits if the diff spans unrelated concerns
disable-model-invocation: true
allowed-tools: Bash(git status *), Bash(git diff *), Bash(git log *), Bash(git add *), Bash(git reset *), Bash(git restore *), Bash(git apply *), Bash(git commit *)
---

Commit the currently staged changes, split by concern when the staged diff spans unrelated ones — and only then; a forced split leaves intermediate commits that don't build.

If nothing is staged, report that and stop — never stage changes on your own.

Four things this repo needs that general commit sense won't supply:

- **An asset's `.meta` goes in the same commit as the asset.** Separated, they break references silently — and the break surfaces later, in a scene nobody was touching. `hooks/pre-commit` blocks the added-file case; moves and deletions still need your eye.
- **No `Co-Authored-By` trailers.** `hooks/commit-msg` strips them, but only for contributors who have run `git config core.hooksPath hooks`, so don't rely on it.
- **A pre-commit rejection is a finding, not an obstacle.** Surface the hook's output and fix the violation, or stop and ask. Retrying with `--no-verify` needs the user to say so explicitly; bypassing on your own defeats the only guard that runs. `SG-ALLOW` is for an exception you can justify at the line, not for silencing a true positive.
- **Two unrelated concerns can share one file.** Whole-file staging can't separate them, and `git add -p` needs a TTY it won't get here — split the diff into per-hunk patches and stage them with `git apply --cached`.

Report the commit hashes when you're done.
