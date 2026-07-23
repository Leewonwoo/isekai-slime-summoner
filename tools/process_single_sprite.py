#!/usr/bin/env python3
"""Fit a transparent sprite to a reference sprite's canvas and ground line."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Crop a transparent generated sprite, resize it with nearest-neighbor "
            "sampling, and place it inside the reference sprite's alpha bounds."
        )
    )
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--reference", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--alpha-threshold", type=int, default=8)
    return parser.parse_args()


def threshold_bbox(image: Image.Image, threshold: int) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    mask = alpha.point(lambda value: 255 if value > threshold else 0)
    bbox = mask.getbbox()
    if bbox is None:
        raise ValueError("sprite contains no visible pixels")
    return bbox


def main() -> None:
    args = parse_args()
    source = Image.open(args.input).convert("RGBA")
    reference = Image.open(args.reference).convert("RGBA")

    source_bbox = threshold_bbox(source, args.alpha_threshold)
    reference_bbox = threshold_bbox(reference, args.alpha_threshold)
    cropped = source.crop(source_bbox)

    target_width = reference_bbox[2] - reference_bbox[0]
    target_height = reference_bbox[3] - reference_bbox[1]
    scale = min(target_width / cropped.width, target_height / cropped.height)
    resized_size = (
        max(1, round(cropped.width * scale)),
        max(1, round(cropped.height * scale)),
    )
    resized = cropped.resize(resized_size, Image.Resampling.NEAREST)

    x = reference_bbox[0] + (target_width - resized.width) // 2
    y = reference_bbox[3] - resized.height
    if x < 0 or y < 0 or x + resized.width > reference.width or y + resized.height > reference.height:
        raise ValueError("processed sprite would exceed the reference canvas")

    result = Image.new("RGBA", reference.size, (0, 0, 0, 0))
    result.alpha_composite(resized, dest=(x, y))
    args.out.parent.mkdir(parents=True, exist_ok=True)
    result.save(args.out)
    print(
        f"Wrote {args.out} | source_bbox={source_bbox} "
        f"reference_bbox={reference_bbox} placed={(x, y, x + resized.width, y + resized.height)}"
    )


if __name__ == "__main__":
    main()
