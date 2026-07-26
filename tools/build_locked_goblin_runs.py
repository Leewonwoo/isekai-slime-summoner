#!/usr/bin/env python3
"""Build deterministic nine-frame goblin run sheets from static sprites.

The upper body is copied byte-for-byte into every frame. Only the two lower
leg regions move, so gait timing and body stability do not depend on a
generative model interpreting animation terminology.
"""

from __future__ import annotations

import argparse
from collections import deque
from dataclasses import dataclass
from pathlib import Path

from PIL import Image


CELL = 128
ROOT_X = 64
ROOT_Y = 112
ALPHA_THRESHOLD = 32


@dataclass(frozen=True)
class Component:
    pixels: tuple[tuple[int, int], ...]

    @property
    def area(self) -> int:
        return len(self.pixels)

    @property
    def bbox(self) -> tuple[int, int, int, int]:
        xs = [point[0] for point in self.pixels]
        ys = [point[1] for point in self.pixels]
        return min(xs), min(ys), max(xs) + 1, max(ys) + 1

    @property
    def center_x(self) -> float:
        return sum(point[0] for point in self.pixels) / self.area


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input-dir", required=True, type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    return parser.parse_args()


def alpha_bbox(image: Image.Image) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A")
    mask = alpha.point(lambda value: 255 if value > ALPHA_THRESHOLD else 0)
    bbox = mask.getbbox()
    if bbox is None:
        raise ValueError("sprite has no visible pixels")
    return bbox


def body_anchor(image: Image.Image) -> tuple[float, int]:
    """Estimate core-body center while ignoring side weapons and shields."""
    alpha = image.getchannel("A")
    left, top, right, bottom = alpha_bbox(image)
    upper_bottom = top + round((bottom - top) * 0.72)
    counts = [
        sum(alpha.getpixel((x, y)) > ALPHA_THRESHOLD for y in range(top, upper_bottom))
        for x in range(CELL)
    ]
    peak_x = max(range(CELL), key=counts.__getitem__)
    peak = counts[peak_x]
    strong = [
        x
        for x in range(max(0, peak_x - 22), min(CELL, peak_x + 23))
        if counts[x] >= max(5, round(peak * 0.55))
    ]
    center_x = sum(strong) / len(strong) if strong else float(peak_x)

    core_left = max(0, round(center_x) - 25)
    core_right = min(CELL, round(center_x) + 26)
    core_bottom = max(
        y
        for x in range(core_left, core_right)
        for y in range(CELL)
        if alpha.getpixel((x, y)) > ALPHA_THRESHOLD
    )
    return center_x, core_bottom


def translated(image: Image.Image, dx: int, dy: int) -> Image.Image:
    result = Image.new("RGBA", image.size, (0, 0, 0, 0))
    result.alpha_composite(image, dest=(dx, dy))
    return result


def align_to_root(image: Image.Image) -> Image.Image:
    center_x, bottom = body_anchor(image)
    dx = round(ROOT_X - center_x)
    dy = ROOT_Y - bottom
    return translated(image, dx, dy)


def components_in_band(
    image: Image.Image,
    left: int,
    top: int,
    right: int,
    bottom: int,
) -> list[Component]:
    alpha = image.getchannel("A")
    visible = {
        (x, y)
        for y in range(top, bottom)
        for x in range(left, right)
        if alpha.getpixel((x, y)) > ALPHA_THRESHOLD
    }
    components: list[Component] = []
    while visible:
        seed = visible.pop()
        queue = deque([seed])
        pixels = [seed]
        while queue:
            x, y = queue.popleft()
            for nx in range(x - 1, x + 2):
                for ny in range(y - 1, y + 2):
                    if (nx, ny) in visible:
                        visible.remove((nx, ny))
                        queue.append((nx, ny))
                        pixels.append((nx, ny))
        components.append(Component(tuple(pixels)))
    return components


def foot_box(image: Image.Image, side: str) -> tuple[int, int, int, int]:
    band_top = ROOT_Y - 19
    components = components_in_band(image, ROOT_X - 34, band_top, ROOT_X + 35, ROOT_Y + 1)
    candidates = [
        component
        for component in components
        if component.area >= 4 and component.bbox[3] >= ROOT_Y - 2
    ]
    if side == "left":
        candidates = [component for component in candidates if component.center_x < ROOT_X]
        fallback = (ROOT_X - 24, ROOT_Y - 28, ROOT_X, ROOT_Y + 1)
    else:
        candidates = [component for component in candidates if component.center_x >= ROOT_X]
        fallback = (ROOT_X, ROOT_Y - 28, ROOT_X + 24, ROOT_Y + 1)
    if not candidates:
        return fallback

    selected = min(candidates, key=lambda component: abs(component.center_x - ROOT_X))
    x0, _, x1, _ = selected.bbox
    x0 = max(ROOT_X - 30, x0 - 3)
    x1 = min(ROOT_X + 31, x1 + 3)
    if side == "left":
        x1 = min(x1, ROOT_X + 1)
    else:
        x0 = max(x0, ROOT_X - 1)
    return x0, ROOT_Y - 28, x1, ROOT_Y + 1


def region_layer(source: Image.Image, box: tuple[int, int, int, int]) -> Image.Image:
    layer = Image.new("RGBA", source.size, (0, 0, 0, 0))
    layer.alpha_composite(source.crop(box), dest=(box[0], box[1]))
    return layer


def build_sheet(base: Image.Image) -> Image.Image:
    aligned = align_to_root(base)
    left_box = foot_box(aligned, "left")
    right_box = foot_box(aligned, "right")
    left_leg = region_layer(aligned, left_box)
    right_leg = region_layer(aligned, right_box)

    body = aligned.copy()
    clear_top = ROOT_Y - 19
    body.paste((0, 0, 0, 0), (left_box[0], clear_top, left_box[2], ROOT_Y + 1))
    body.paste((0, 0, 0, 0), (right_box[0], clear_top, right_box[2], ROOT_Y + 1))

    # The support leg never moves inside its phase. The swing leg stays above
    # the shared baseline until the support change or loop boundary.
    left_phase = [(0, -1), (-1, -3), (-2, -6), (2, -4), (5, -2)]
    right_phase = [(0, -1), (1, -6), (-2, -4), (-5, -2)]

    frames: list[Image.Image] = []
    for offset in left_phase:
        frame = body.copy()
        frame.alpha_composite(left_leg)
        frame.alpha_composite(translated(right_leg, *offset))
        frames.append(frame)
    for offset in right_phase:
        frame = body.copy()
        frame.alpha_composite(right_leg)
        frame.alpha_composite(translated(left_leg, *offset))
        frames.append(frame)

    sheet = Image.new("RGBA", (CELL * 3, CELL * 3), (0, 0, 0, 0))
    for index, frame in enumerate(frames):
        x = (index % 3) * CELL
        y = (index // 3) * CELL
        sheet.alpha_composite(frame, dest=(x, y))
    return sheet


def main() -> None:
    args = parse_args()
    inputs = sorted(args.input_dir.glob("enemy_goblin_*.png"))
    if not inputs:
        raise ValueError(f"no goblin sprites found in {args.input_dir}")
    args.output_dir.mkdir(parents=True, exist_ok=True)
    for path in inputs:
        base = Image.open(path).convert("RGBA")
        if base.size != (CELL, CELL):
            raise ValueError(f"{path} must be 128x128, got {base.size}")
        output = args.output_dir / f"{path.stem}_run_sheet.png"
        build_sheet(base).save(output, optimize=True)
        print(f"Wrote {output}")


if __name__ == "__main__":
    main()
