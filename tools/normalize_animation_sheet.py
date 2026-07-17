#!/usr/bin/env python3
"""Align a fixed-grid animation sheet without cropping individual frames."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Translate artwork inside each fixed cell so the core-body anchor "
            "lands at one shared local coordinate. Cell rectangles never change."
        )
    )
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--columns", type=int, default=3)
    parser.add_argument("--rows", type=int, default=3)
    parser.add_argument("--cell-size", type=int, default=128)
    parser.add_argument("--target-x", type=int, default=64)
    parser.add_argument("--target-bottom-y", type=int, default=112)
    parser.add_argument("--alpha-threshold", type=int, default=32)
    parser.add_argument("--anchor-half-width", type=int, default=28)
    parser.add_argument("--min-column-pixels", type=int, default=25)
    return parser.parse_args()


def find_anchor(
    cell: Image.Image,
    alpha_threshold: int,
    anchor_half_width: int,
    min_column_pixels: int,
) -> tuple[float, int]:
    alpha = cell.getchannel("A")
    width, height = cell.size
    column_counts = [
        sum(alpha.getpixel((x, y)) > alpha_threshold for y in range(height))
        for x in range(width)
    ]
    peak_x = max(range(width), key=column_counts.__getitem__)
    left = max(0, peak_x - anchor_half_width)
    right = min(width, peak_x + anchor_half_width + 1)

    strong_columns = [
        x for x in range(left, right) if column_counts[x] >= min_column_pixels
    ]
    if not strong_columns:
        strong_columns = [x for x in range(left, right) if column_counts[x] > 0]
    if not strong_columns:
        raise ValueError("cell has no visible anchor pixels")

    center_x = sum(strong_columns) / len(strong_columns)
    bottom_y = max(
        y
        for x in range(left, right)
        for y in range(height)
        if alpha.getpixel((x, y)) > alpha_threshold
    )
    return center_x, bottom_y


def main() -> None:
    args = parse_args()
    image = Image.open(args.input).convert("RGBA")
    expected_size = (args.columns * args.cell_size, args.rows * args.cell_size)
    if image.size != expected_size:
        raise ValueError(f"expected sheet size {expected_size}, got {image.size}")

    result = Image.new("RGBA", image.size, (0, 0, 0, 0))
    shifts: list[tuple[int, int]] = []

    for index in range(args.columns * args.rows):
        column = index % args.columns
        row = index // args.columns
        x0 = column * args.cell_size
        y0 = row * args.cell_size
        cell = image.crop((x0, y0, x0 + args.cell_size, y0 + args.cell_size))

        anchor_x, bottom_y = find_anchor(
            cell,
            args.alpha_threshold,
            args.anchor_half_width,
            args.min_column_pixels,
        )
        dx = round(args.target_x - anchor_x)
        dy = args.target_bottom_y - bottom_y

        alpha_bbox = cell.getchannel("A").getbbox()
        if alpha_bbox is None:
            raise ValueError(f"frame {index + 1} is empty")
        shifted_bbox = (
            alpha_bbox[0] + dx,
            alpha_bbox[1] + dy,
            alpha_bbox[2] + dx,
            alpha_bbox[3] + dy,
        )
        if (
            shifted_bbox[0] < 0
            or shifted_bbox[1] < 0
            or shifted_bbox[2] > args.cell_size
            or shifted_bbox[3] > args.cell_size
        ):
            raise ValueError(
                f"frame {index + 1} would clip after shift {(dx, dy)}: "
                f"{shifted_bbox}"
            )

        aligned_cell = Image.new("RGBA", cell.size, (0, 0, 0, 0))
        aligned_cell.alpha_composite(cell, dest=(dx, dy))
        result.alpha_composite(aligned_cell, dest=(x0, y0))
        shifts.append((dx, dy))

    args.out.parent.mkdir(parents=True, exist_ok=True)
    result.save(args.out)
    print(f"Wrote {args.out}")
    print("Frame shifts:", ", ".join(f"{i + 1}:{shift}" for i, shift in enumerate(shifts)))


if __name__ == "__main__":
    main()
