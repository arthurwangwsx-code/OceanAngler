#!/bin/sh
set -eu
branch=$(git branch --show-current)
case "$branch" in
 feature/*|fix/*|chore/*|spike/*|integration/*) ;;
 *) echo "Commit on a named branch created from master" >&2; exit 1 ;;
esac
[ "$(git worktree list --porcelain | sed -n '/^worktree /p' | wc -l | tr -d ' ')" = 1 ] || { echo "Only canonical checkout allowed" >&2; exit 1; }
