#!/usr/bin/env python3
"""Prepend the CHANGELOG and BALANCE sections to a GitHub Release body.

Triggered when a Release is published. Reads ``CHANGELOG.md`` and
``BALANCE.md``, extracts the section whose version matches the Release tag and
writes them *ahead* of GitHub's auto-generated notes, in this order::

    <!-- CHANGELOG-notes -->
    <changelog section>

    ---

    <!-- BALANCE-notes -->
    <balance section>

    ---

    <default release notes>

Only the Release matching the current tag is touched; historical Releases are
left as-is. If the body already carries ``<!-- CHANGELOG-notes -->`` the script
skips it, so re-running is harmless.

在 Release 发布时运行：读取 ``CHANGELOG.md`` 与 ``BALANCE.md``，取出与 Release
标签版本对应的段落，按「CHANGELOG → BALANCE → 默认说明」的顺序写到 Release
正文之前，并保留 GitHub 自动生成的说明。只处理当前标签对应的 Release，不回溯
历史版本；正文已包含标记时直接跳过，保证可重复运行。
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path

# Anchors identifying the section injected by this script. ``CHANGELOG_MARKER``
# doubles as the idempotency token: once present, the body is considered managed.
# 标记脚本注入区块的锚点；CHANGELOG_MARKER 同时充当幂等凭据。
CHANGELOG_MARKER = "<!-- CHANGELOG-notes -->"
BALANCE_MARKER = "<!-- BALANCE-notes -->"

# Horizontal rule separating the two sections (and the default notes).
# 分隔各区块的水平线。
SEPARATOR = "---"

API_ROOT = "https://api.github.com"

# ``## [x.y.z] - date`` / ``## [Unreleased]`` headings of a Keep a Changelog file.
# Keep a Changelog 文件的 ``## [x.y.z] - date`` / ``## [Unreleased]`` 标题。
_HEADING_RE = re.compile(r"^##[ \t]+\[(?P<version>[^\]]+)\].*$", re.MULTILINE)


def parse_sections(text: str) -> dict[str, str]:
    """Split a changelog-style file into ``{version: section}`` preserving order.

    The ``## [x.y.z] - date`` heading itself is dropped; only the body below it
    is kept.

    把 changelog 风格的文件拆成 ``{版本: 段落}``，保持文件顺序。

    丢弃 ``## [x.y.z] - date`` 标题行，只保留其下方的正文。
    """
    matches = list(_HEADING_RE.finditer(text))
    sections: dict[str, str] = {}
    for index, match in enumerate(matches):
        version = match.group("version").strip()
        start = match.end()
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        sections[version] = text[start:end].strip()
    return sections


def load_sections(path: str) -> dict[str, str]:
    """Read a markdown file and parse its versioned sections.

    Missing files are tolerated and yield an empty mapping, so a repository that
    has not adopted ``BALANCE.md`` yet still produces valid notes.

    读取 markdown 文件并解析其分版本段落。

    文件缺失时返回空映射，因此尚未引入 ``BALANCE.md`` 的仓库仍能正常生成正文。
    """
    file = Path(path)
    if not file.is_file():
        return {}
    return parse_sections(file.read_text(encoding="utf-8"))


def normalize_tag(tag: str) -> str:
    """Map a release tag to its changelog version, e.g. ``v0.1.1`` -> ``0.1.1``.

    把 Release 标签映射到 changelog 版本，例如 ``v0.1.1`` -> ``0.1.1``。
    """
    return tag[1:] if tag[:1] in ("v", "V") else tag


def build_managed_block(changelog: str, balance: str) -> str:
    """Compose the managed block: markers, sections and separating rules.

    组装受管区块：标记、段落与分隔线。
    """
    parts = [CHANGELOG_MARKER]
    if changelog:
        parts += ["", changelog]
    parts += ["", SEPARATOR, "", BALANCE_MARKER]
    if balance:
        parts += ["", balance]
    return "\n".join(parts).strip() + "\n"


def merge_body(existing: str | None, managed: str) -> str:
    """Return a Release body with the managed block placed before the notes.

    GitHub's auto-generated notes (whatever already exists) are preserved and
    pushed below the injected sections, separated by a horizontal rule.

    返回新的 Release 正文，受管区块位于说明之前。

    已有的 GitHub 自动生成说明保持不变，被分隔线推到注入段落下方。
    """
    existing = (existing or "").strip()
    if not existing:
        return managed
    return managed.rstrip() + "\n\n" + SEPARATOR + "\n\n" + existing + "\n"


def _request(url: str, token: str, *, method: str = "GET", payload=None):
    """Issue a GitHub REST call and decode the JSON response.

    发起 GitHub REST 调用并解析 JSON 响应。
    """
    data = json.dumps(payload).encode("utf-8") if payload is not None else None
    request = urllib.request.Request(url, data=data, method=method)
    request.add_header("Authorization", f"Bearer {token}")
    request.add_header("Accept", "application/vnd.github+json")
    request.add_header("X-GitHub-Api-Version", "2022-11-28")
    if data is not None:
        request.add_header("Content-Type", "application/json")
    with urllib.request.urlopen(request) as response:
        body = response.read()
    return json.loads(body) if body else None


def get_release_by_tag(repo: str, token: str, tag: str) -> dict | None:
    """Fetch a single Release by tag, or ``None`` if it does not exist.

    按标签获取单个 Release，不存在时返回 ``None``。
    """
    try:
        return _request(f"{API_ROOT}/repos/{repo}/releases/tags/{tag}", token)
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return None
        raise


def update_release(repo: str, token: str, release_id: int, body: str) -> None:
    """Overwrite a single Release body.

    覆盖单个 Release 正文。
    """
    _request(
        f"{API_ROOT}/repos/{repo}/releases/{release_id}",
        token,
        method="PATCH",
        payload={"body": body},
    )


def main(argv: list[str] | None = None) -> int:
    """Entry point: parse arguments, build the notes and patch the Release.

    入口：解析参数、生成正文并更新 Release。
    """
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", default=os.environ.get("GITHUB_REPOSITORY"))
    parser.add_argument(
        "--token",
        default=os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN"),
    )
    parser.add_argument(
        "--tag",
        default=os.environ.get("RELEASE_TAG") or os.environ.get("GITHUB_REF_NAME"),
    )
    parser.add_argument("--changelog", default="CHANGELOG.md")
    parser.add_argument("--balance", default="BALANCE.md")
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="print the planned body without calling the API",
    )
    args = parser.parse_args(argv)

    if not args.tag:
        parser.error("missing --tag (or RELEASE_TAG/GITHUB_REF_NAME)")
    if not args.dry_run:
        if not args.repo:
            parser.error("missing --repo (or GITHUB_REPOSITORY)")
        if not args.token:
            parser.error("missing --token (or GH_TOKEN/GITHUB_TOKEN)")

    version = normalize_tag(args.tag)
    changelog = load_sections(args.changelog).get(version, "")
    balance = load_sections(args.balance).get(version, "")
    if not changelog and not balance:
        print(f"no CHANGELOG/BALANCE entry for {version}; nothing to do")
        return 0

    managed = build_managed_block(changelog, balance)

    if args.dry_run:
        print(managed)
        return 0

    release = get_release_by_tag(args.repo, args.token, args.tag)
    if release is None:
        print(f"release {args.tag} not found; nothing to do")
        return 0

    existing = release.get("body") or ""
    if CHANGELOG_MARKER in existing:
        print(f"release {args.tag}: notes already managed; skipping")
        return 0

    body = merge_body(existing, managed)
    update_release(args.repo, args.token, release["id"], body)
    print(f"updated release {args.tag} notes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
