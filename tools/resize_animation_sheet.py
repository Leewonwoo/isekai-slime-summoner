#!/usr/bin/env python3
"""Resize a fixed-grid pixel-art animation sheet without changing its grid."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Resize a sprite sheet with nearest-neighbor sampling."
    )
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--width", required=True, type=int)
    parser.add_argument("--height", required=True, type=int)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    if args.width <= 0 or args.height <= 0:
        raise ValueError("width and height must be positive")

    source = Image.open(args.input).convert("RGBA")
    resized = source.resize(
        (args.width, args.height),
        Image.Resampling.NEAREST,
    )
    args.out.parent.mkdir(parents=True, exist_ok=True)
    resized.save(args.out, optimize=True)
    print(f"Wrote {args.out} | {source.size} -> {resized.size}")


if __name__ == "__main__":
    main()
