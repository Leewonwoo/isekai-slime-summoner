#!/usr/bin/env python3
"""Build a stable nine-frame fire-scout run cycle from one approved frame."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


CELL_SIZE = 128
FRAME_COUNT = 9


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    return parser.parse_args()


def masked_region(source: Image.Image, box: tuple[int, int, int, int]) -> Image.Image:
    layer = Image.new("RGBA", source.size, (0, 0, 0, 0))
    layer.alpha_composite(source.crop(box), dest=(box[0], box[1]))
    return layer


def translated(layer: Image.Image, dx: int, dy: int) -> Image.Image:
    result = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    result.alpha_composite(layer, dest=(dx, dy))
    return result


def main() -> None:
    args = parse_args()
    sheet = Image.open(args.input).convert("RGBA")
    if sheet.size != (CELL_SIZE * 3, CELL_SIZE * 3):
        raise ValueError(f"expected a 384x384 sheet, got {sheet.size}")

    # Frame 1 is the identity master. The upper body is reused byte-for-byte,
    # which prevents head, torso, weapon, and pelvis jitter.
    master = sheet.crop((0, 0, CELL_SIZE, CELL_SIZE))
    upper_body = master.copy()
    upper_body.paste((0, 0, 0, 0), (44, 80, 88, CELL_SIZE))

    # Legs are isolated from the same master, then moved without resampling.
    left_support = masked_region(master, (45, 78, 66, 114))
    # Start outside the central tabard so mirroring the swing leg cannot
    # duplicate the orange fire-rune cloth on the opposite side.
    right_swing = masked_region(master, (74, 79, 87, 114))
    right_support = left_support.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    left_swing = right_swing.transpose(Image.Transpose.FLIP_LEFT_RIGHT)

    # The loincloth is restored last so both legs always remain behind the pelvis.
    loincloth = masked_region(master, (53, 73, 79, 100))

    # Frames 1-5 retain left-foot support. Frames 6-9 retain right-foot support.
    left_phase = [(0, 0), (0, -2), (-2, -4), (3, -3), (7, -1)]
    right_phase = [(0, -1), (2, -4), (-3, -3), (-7, -1)]

    frames: list[Image.Image] = []
    for swing_offset in left_phase:
        frame = Image.new("RGBA", master.size, (0, 0, 0, 0))
        frame.alpha_composite(upper_body)
        frame.alpha_composite(left_support)
        frame.alpha_composite(translated(right_swing, *swing_offset))
        frame.alpha_composite(loincloth)
        frames.append(frame)

    for swing_offset in right_phase:
        frame = Image.new("RGBA", master.size, (0, 0, 0, 0))
        frame.alpha_composite(upper_body)
        frame.alpha_composite(right_support)
        frame.alpha_composite(translated(left_swing, *swing_offset))
        frame.alpha_composite(loincloth)
        frames.append(frame)

    output = Image.new("RGBA", sheet.size, (0, 0, 0, 0))
    for index, frame in enumerate(frames):
        x = (index % 3) * CELL_SIZE
        y = (index // 3) * CELL_SIZE
        output.alpha_composite(frame, dest=(x, y))

    args.out.parent.mkdir(parents=True, exist_ok=True)
    output.save(args.out, optimize=True)
    print(f"Wrote {args.out} ({FRAME_COUNT} locked frames)")


if __name__ == "__main__":
    main()
