#!/usr/bin/env python3
"""Uniformly scale artwork inside every fixed sprite-sheet cell."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--columns", type=int, default=3)
    parser.add_argument("--rows", type=int, default=3)
    parser.add_argument("--cell-size", type=int, default=128)
    parser.add_argument("--scale", type=float, required=True)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    if not 0 < args.scale <= 1:
        raise ValueError("scale must be greater than 0 and at most 1")

    source = Image.open(args.input).convert("RGBA")
    expected = (args.columns * args.cell_size, args.rows * args.cell_size)
    if source.size != expected:
        raise ValueError(f"expected {expected}, got {source.size}")

    scaled_size = max(1, round(args.cell_size * args.scale))
    inset = (args.cell_size - scaled_size) // 2
    result = Image.new("RGBA", source.size, (0, 0, 0, 0))
    for index in range(args.columns * args.rows):
        column = index % args.columns
        row = index // args.columns
        x = column * args.cell_size
        y = row * args.cell_size
        cell = source.crop((x, y, x + args.cell_size, y + args.cell_size))
        cell = cell.resize((scaled_size, scaled_size), Image.Resampling.NEAREST)
        result.alpha_composite(cell, dest=(x + inset, y + inset))

    args.out.parent.mkdir(parents=True, exist_ok=True)
    result.save(args.out, optimize=True)
    print(f"Wrote {args.out} | scale={args.scale} cell={scaled_size}")


if __name__ == "__main__":
    main()
