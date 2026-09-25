#!/usr/bin/env bash
#
# Claude Code PreToolUse hook for shell tools (Bash and PowerShell).
#
# Why this exists: the repo's git rules must hold even if an instruction in
# CLAUDE.md gets missed. This guard blocks any command that runs `git commit`
# when either
#   1. the current branch is main, master or develop (work belongs on an
#      SP/<type>/<description> feature branch), or
#   2. the command carries a Co-Authored-By trailer or a
#      "Generated with Claude Code" line.
#
# Contract: Claude Code sends the tool call as JSON on stdin. Exit 0 lets the
# command run; exit 2 blocks it and hands stderr back to Claude as the reason.
# Every command that isn't a commit passes straight through.

input="$(cat)"

# Only commits are policed. Matching the raw JSON avoids a jq dependency,
# which Git Bash on Windows usually doesn't have.
case "$input" in
  *"git commit"*) ;;
  *) exit 0 ;;
esac

shopt -s nocasematch
if [[ "$input" == *"co-authored-by"* || "$input" == *"generated with"*"claude code"* ]]; then
  echo "Blocked: commit messages in this repo must not contain Co-Authored-By trailers or 'Generated with Claude Code' lines. Re-run the commit without them." >&2
  exit 2
fi
shopt -u nocasematch

# symbolic-ref also works before the first commit, and prints nothing on a
# detached HEAD, where there is no branch to protect.
branch="$(git -C "${CLAUDE_PROJECT_DIR:-.}" symbolic-ref --short -q HEAD 2>/dev/null)"

case "$branch" in
  main|master|develop|development)
    echo "Blocked: refusing to commit on '$branch'. Create a feature branch first, for example: git switch -c SP/feature/<short-description> (uncommitted changes move with you), then commit there." >&2
    exit 2
    ;;
esac

exit 0
