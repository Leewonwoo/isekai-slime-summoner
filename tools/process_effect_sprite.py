#!/usr/bin/env python3
"""Crop a transparent VFX image and center it on a square pixel-art canvas."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Crop an alpha VFX source, resize it with nearest-neighbor sampling, "
            "and center it on a transparent square canvas."
        )
    )
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--size", type=int, default=128)
    parser.add_argument("--padding", type=int, default=8)
    parser.add_argument("--alpha-threshold", type=int, default=8)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    if args.size <= 0:
        raise ValueError("size must be positive")
    if args.padding < 0 or args.padding * 2 >= args.size:
        raise ValueError("padding must leave a positive drawing area")

    source = Image.open(args.input).convert("RGBA")
    alpha = source.getchannel("A")
    mask = alpha.point(lambda value: 255 if value > args.alpha_threshold else 0)
    bbox = mask.getbbox()
    if bbox is None:
        raise ValueError("effect contains no visible pixels")

    cropped = source.crop(bbox)
    target_extent = args.size - args.padding * 2
    scale = min(target_extent / cropped.width, target_extent / cropped.height)
    resized = cropped.resize(
        (
            max(1, round(cropped.width * scale)),
            max(1, round(cropped.height * scale)),
        ),
        Image.Resampling.NEAREST,
    )

    result = Image.new("RGBA", (args.size, args.size), (0, 0, 0, 0))
    position = (
        (args.size - resized.width) // 2,
        (args.size - resized.height) // 2,
    )
    result.alpha_composite(resized, dest=position)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    result.save(args.out)
    print(
        f"Wrote {args.out} | source_bbox={bbox} "
        f"resized={resized.size} position={position}"
    )


if __name__ == "__main__":
    main()
