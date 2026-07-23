#!/usr/bin/env python3
"""Create deterministic Unity single-sprite importer metadata for projectile PNGs."""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path
import re


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--asset-dir", required=True, type=Path)
    parser.add_argument("--template", required=True, type=Path)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    template = args.template.read_text(encoding="utf-8")
    template_guid = re.search(r"^guid: ([0-9a-f]+)$", template, re.MULTILINE)
    template_name = re.search(r"second: ([^\r\n]+)", template)
    template_id = re.search(r"213: (-?\d+)", template)
    if template_guid is None or template_name is None or template_id is None:
        raise ValueError("template meta is missing expected sprite fields")

    for png in sorted(args.asset_dir.glob("projectile_*.png")):
        if png.name in {
            "projectile_energy_bolt.png",
            "projectile_fireball.png",
            "projectile_iceball.png",
        }:
            continue
        meta_path = png.with_suffix(png.suffix + ".meta")
        asset_key = png.as_posix().lower()
        guid = hashlib.md5(asset_key.encode("utf-8")).hexdigest()
        raw_id = int.from_bytes(
            hashlib.sha256(asset_key.encode("utf-8")).digest()[:8],
            byteorder="big",
            signed=True,
        )
        internal_id = raw_id if raw_id != 0 else 1
        sprite_name = f"{png.stem}_0"

        content = template
        content = content.replace(template_guid.group(1), guid, 1)
        content = content.replace(template_name.group(1), sprite_name)
        content = content.replace(template_id.group(1), str(internal_id))
        content = re.sub(r"(?m)^        x: \d+$", "        x: 0", content, count=1)
        content = re.sub(r"(?m)^        y: \d+$", "        y: 0", content, count=1)
        content = re.sub(r"(?m)^        width: \d+$", "        width: 128", content, count=1)
        content = re.sub(r"(?m)^        height: \d+$", "        height: 128", content, count=1)
        content = content.replace("textureCompression: 1", "textureCompression: 0")
        meta_path.write_text(content, encoding="utf-8", newline="\n")
        print(f"{png.name}: {guid}")


if __name__ == "__main__":
    main()
