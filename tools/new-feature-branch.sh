#!/bin/sh
set -eu
[ $# = 1 ] || { echo "usage: $0 feature/name" >&2; exit 1; }
case "$1" in feature/*|fix/*|chore/*|spike/*|integration/*) ;; *) exit 1 ;; esac
[ -z "$(git status --porcelain)" ] || { echo "Commit existing changes first" >&2; exit 1; }
[ "$(git branch --show-current)" = master ] || { echo "Start from master" >&2; exit 1; }
git switch -c "$1" master
