#!/usr/bin/env python3
"""Extract the three largest projectiles from a transparent rank strip."""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Extract star 1/2/3 projectiles by connected-component area."
    )
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--source-out-dir", required=True, type=Path)
    parser.add_argument("--asset-out-dir", required=True, type=Path)
    parser.add_argument("--base-name", required=True)
    parser.add_argument("--key-color", required=True)
    parser.add_argument("--size", type=int, default=128)
    parser.add_argument("--padding", type=int, default=8)
    parser.add_argument("--alpha-threshold", type=int, default=8)
    return parser.parse_args()


def connected_components(
    image: Image.Image, threshold: int
) -> list[list[tuple[int, int]]]:
    alpha = image.getchannel("A")
    width, height = image.size
    visible = {
        (x, y)
        for y in range(height)
        for x in range(width)
        if alpha.getpixel((x, y)) > threshold
    }
    groups: list[list[tuple[int, int]]] = []
    while visible:
        start = visible.pop()
        group = [start]
        queue = deque([start])
        while queue:
            x, y = queue.popleft()
            for ny in range(max(0, y - 1), min(height, y + 2)):
                for nx in range(max(0, x - 1), min(width, x + 2)):
                    point = (nx, ny)
                    if point in visible:
                        visible.remove(point)
                        group.append(point)
                        queue.append(point)
        groups.append(group)
    return groups


def bounds(points: list[tuple[int, int]]) -> tuple[int, int, int, int]:
    xs = [point[0] for point in points]
    ys = [point[1] for point in points]
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


def parse_hex_color(value: str) -> tuple[int, int, int, int]:
    raw = value.strip().lstrip("#")
    if len(raw) != 6:
        raise ValueError("key color must be a six-digit RGB hex value")
    return int(raw[0:2], 16), int(raw[2:4], 16), int(raw[4:6], 16), 255


def main() -> None:
    args = parse_args()
    source = Image.open(args.input).convert("RGBA")
    groups = connected_components(source, args.alpha_threshold)
    if len(groups) < 3:
        raise ValueError(f"expected at least 3 visible components, found {len(groups)}")

    primaries = sorted(sorted(groups, key=len, reverse=True)[:3], key=lambda group: bounds(group)[0])
    suffixes = ("", "_star2", "_star3")
    key_color = parse_hex_color(args.key_color)
    args.source_out_dir.mkdir(parents=True, exist_ok=True)
    args.asset_out_dir.mkdir(parents=True, exist_ok=True)

    for index, (group, suffix) in enumerate(zip(primaries, suffixes)):
        left, top, right, bottom = bounds(group)
        component = Image.new("RGBA", source.size, (0, 0, 0, 0))
        source_pixels = source.load()
        component_pixels = component.load()
        for x, y in group:
            component_pixels[x, y] = source_pixels[x, y]
        cropped = component.crop((left, top, right, bottom))

        raw_padding = max(16, round(max(cropped.size) * 0.08))
        raw = Image.new(
            "RGBA",
            (cropped.width + raw_padding * 2, cropped.height + raw_padding * 2),
            key_color,
        )
        raw.alpha_composite(cropped, dest=(raw_padding, raw_padding))
        raw_output = args.source_out_dir / f"{args.base_name}{suffix}.png"
        raw.save(raw_output)

        extent = args.size - args.padding * 2
        scale = min(extent / cropped.width, extent / cropped.height)
        resized = cropped.resize(
            (
                max(1, round(cropped.width * scale)),
                max(1, round(cropped.height * scale)),
            ),
            Image.Resampling.NEAREST,
        )
        final = Image.new("RGBA", (args.size, args.size), (0, 0, 0, 0))
        position = (
            (args.size - resized.width) // 2,
            (args.size - resized.height) // 2,
        )
        final.alpha_composite(resized, dest=position)
        final_output = args.asset_out_dir / f"{args.base_name}{suffix}.png"
        final.save(final_output)
        print(
            f"Wrote rank {index + 1}: {final_output} | pixels={len(group)} "
            f"bbox={(left, top, right, bottom)} resized={resized.size}"
        )


if __name__ == "__main__":
    main()
