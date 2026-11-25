#!/usr/bin/env python3
"""Personal Mac admission adapter; keep the entire pipeline inside one shared lease."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def governor():
    path = Path(os.environ.get("PERSONAL_BUILD_STORAGE_TOOL", str(Path.home() / "project/_workspace/tools/build_storage.py")))
    if not path.is_file():
        raise RuntimeError("Configure PERSONAL_BUILD_STORAGE_TOOL with the shared storage CLI")
    return path


def entry(command, root, expected_gib=20):
    return [sys.executable, str(governor()), "run", "--source-root", str(Path(root).resolve()),
            "--expected-gib", str(expected_gib), "--", sys.executable, str(Path(__file__).resolve()),
            "--leased", "--", *map(str, command)]


def storage_command(command, root, expected_gib=20):
    values = list(map(str, command))
    executable = Path(values[0]).name if values else ""
    expensive = executable in {"Unity", "xcodebuild"} or (executable == "xcrun" and len(values) > 1 and values[1] == "xcodebuild")
    return entry(values, root, expected_gib) if expensive else values


def leased(command):
    # Validate the actual governor lease and lock; an environment variable alone is insufficient.
    path = governor()
    sys.path.insert(0, str(path.parent))
    spec = importlib.util.spec_from_file_location("ocean_shared_storage", path)
    storage = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(storage)
    config, rules, state = storage.load()
    project, config = storage.execution_context(config, ROOT)
    record = json.loads((state / "leases" / (project["id"] + ".json")).read_text())
    if record.get("runId") != os.environ.get("PERSONAL_BUILD_STORAGE_RUN") or not storage.held(state, project["id"]):
        raise RuntimeError("A live shared storage lease is required")
    scratch = Path(record["scratch"]).resolve()
    # Unity IL2CPP uses Unix sockets with short path limits. Alias the managed scratch,
    # retaining its real ownership, lease, reservation and governor finalization.
    with tempfile.TemporaryDirectory(prefix="oa-", dir="/private/tmp") as directory:
        link = Path(directory) / "t"
        link.symlink_to(scratch, target_is_directory=True)
        environment = dict(os.environ, TMPDIR=str(link) + "/")
        return subprocess.call(command, cwd=ROOT, env=environment)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--expected-gib", type=float, default=20)
    parser.add_argument("--leased", action="store_true", help=argparse.SUPPRESS)
    parser.add_argument("command", nargs=argparse.REMAINDER)
    args = parser.parse_args()
    command = args.command[1:] if args.command[:1] == ["--"] else args.command
    if not command:
        parser.error("a complete pipeline command is required after --")
    return leased(command) if args.leased else subprocess.call(entry(command, ROOT, args.expected_gib), cwd=ROOT)


if __name__ == "__main__":
    sys.exit(main())
