#!/usr/bin/env python3
"""Fail CI when added/modified files exceed size limits or introduce obvious secrets."""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

MAX_PATCH_BYTES = 2_000_000

SECRET_PATTERNS = [
    re.compile(r"AKIA[0-9A-Z]{16}"),
    re.compile(r"ASIA[0-9A-Z]{16}"),
    re.compile(r"-----BEGIN [A-Z ]*PRIVATE KEY-----"),
    re.compile(r"(?i)sk-[A-Za-z0-9]{20,}"),
    re.compile(r"(?i)(api[_-]?key|access[_-]?token|secret|password)\\s*[:=]\\s*['\"][^'\"]{20,}['\"]"),
]

BLOCKED_FILE_PATTERNS = [
    re.compile(r"(?i)(^|/)all_secret_keys\\.txt$"),
    re.compile(r"(?i)\\.pem$"),
    re.compile(r"(?i)\\.p12$"),
    re.compile(r"(?i)\\.pfx$"),
    re.compile(r"(?i)\\.key$"),
    re.compile(r"(?i)(^|/)id_rsa(\\.pub)?$"),
    re.compile(r"(?i)(^|/)\\.env(\\..+)?$"),
]


def run_git(args: list[str]) -> str:
    result = subprocess.run(["git", *args], capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or f"git {' '.join(args)} failed")
    return result.stdout


def get_changed_files(base: str, head: str) -> list[str]:
    output = run_git(["diff", "--name-only", "--diff-filter=AM", base, head])
    return [line.strip() for line in output.splitlines() if line.strip()]


def is_text_file(path: Path) -> bool:
    try:
        data = path.read_bytes()[:4096]
    except OSError:
        return False
    return b"\x00" not in data


def check_file_size(path: Path, max_bytes: int) -> str | None:
    try:
        size = path.stat().st_size
    except OSError:
        return None

    if size > max_bytes:
        max_mib = max_bytes / (1024 * 1024)
        size_mib = size / (1024 * 1024)
        return f"File exceeds size limit ({size_mib:.2f} MiB > {max_mib:.2f} MiB)"
    return None


def check_blocked_file_name(rel_path: str) -> str | None:
    normalized = rel_path.replace("\\", "/")
    for pattern in BLOCKED_FILE_PATTERNS:
        if pattern.search(normalized):
            return "Blocked secret-like filename pattern"
    return None


def check_diff_for_secrets(base: str, head: str, rel_path: str) -> list[str]:
    patch = run_git(["diff", "--unified=0", base, head, "--", rel_path])
    if len(patch) > MAX_PATCH_BYTES:
        return []

    findings: list[str] = []
    for line in patch.splitlines():
        if not line.startswith("+") or line.startswith("+++"):
            continue

        content = line[1:]
        for pattern in SECRET_PATTERNS:
            if pattern.search(content):
                findings.append(f"Potential secret in added line matching `{pattern.pattern}`")
                break

    return findings


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("base")
    parser.add_argument("head")
    parser.add_argument("--max-file-mib", type=int, default=5)
    args = parser.parse_args()

    max_bytes = args.max_file_mib * 1024 * 1024
    changed_files = get_changed_files(args.base, args.head)

    if not changed_files:
        print("No added/modified files detected in range.")
        return 0

    failures: list[tuple[str, str]] = []

    for rel_path in changed_files:
        path = Path(rel_path)
        if not path.exists() or path.is_dir():
            continue

        name_failure = check_blocked_file_name(rel_path)
        if name_failure:
            failures.append((rel_path, name_failure))

        size_failure = check_file_size(path, max_bytes)
        if size_failure:
            failures.append((rel_path, size_failure))

        if is_text_file(path):
            for finding in check_diff_for_secrets(args.base, args.head, rel_path):
                failures.append((rel_path, finding))

    if failures:
        print("Repository hygiene check failed:")
        for rel_path, reason in failures:
            print(f"- {rel_path}: {reason}")
        return 1

    print("Repository hygiene check passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
