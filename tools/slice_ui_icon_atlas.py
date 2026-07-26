#!/usr/bin/env python3
"""Slice a transparent fixed-grid atlas into normalized 128 px UI sprites."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--columns", required=True, type=int)
    parser.add_argument("--rows", required=True, type=int)
    parser.add_argument("--names", required=True)
    parser.add_argument("--size", default=128, type=int)
    parser.add_argument("--extent", default=108, type=int)
    return parser.parse_args()


def alpha_bbox(image: Image.Image) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        raise ValueError("atlas cell contains no visible pixels")
    return bbox


def main() -> None:
    args = parse_args()
    names = [name.strip() for name in args.names.split(",") if name.strip()]
    expected = args.columns * args.rows
    if len(names) != expected:
        raise ValueError(f"expected {expected} names, got {len(names)}")
    if args.extent <= 0 or args.extent > args.size:
        raise ValueError("extent must be between 1 and size")

    source = Image.open(args.input).convert("RGBA")
    args.output_dir.mkdir(parents=True, exist_ok=True)

    for index, name in enumerate(names):
        column = index % args.columns
        row = index // args.columns
        left = round(column * source.width / args.columns)
        right = round((column + 1) * source.width / args.columns)
        top = round(row * source.height / args.rows)
        bottom = round((row + 1) * source.height / args.rows)
        cell = source.crop((left, top, right, bottom))
        subject = cell.crop(alpha_bbox(cell))

        scale = min(args.extent / subject.width, args.extent / subject.height)
        target_size = (
            max(1, round(subject.width * scale)),
            max(1, round(subject.height * scale)),
        )
        subject = subject.resize(target_size, Image.Resampling.NEAREST)
        output = Image.new("RGBA", (args.size, args.size), (0, 0, 0, 0))
        output.alpha_composite(
            subject,
            ((args.size - subject.width) // 2, (args.size - subject.height) // 2),
        )
        destination = args.output_dir / f"{name}.png"
        output.save(destination, optimize=True)
        print(f"{destination}: {target_size[0]}x{target_size[1]}")


if __name__ == "__main__":
    main()
