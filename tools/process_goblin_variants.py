from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


SOURCE_DIR = Path("Temp/goblin-variants")
OUTPUT_DIR = Path("Assets/Art/Enemies")
NAMES = (
    "enemy_goblin_fire_scout.png",
    "enemy_goblin_ice_bruiser.png",
    "enemy_goblin_nature_raider.png",
    "enemy_goblin_chief.png",
    "enemy_goblin_warlord.png",
    "enemy_goblin_slinger.png",
    "enemy_goblin_golden.png",
    "enemy_goblin_fire_mage.png",
    "enemy_goblin_fire_bomber.png",
    "enemy_goblin_frost_stalker.png",
    "enemy_goblin_ice_archer.png",
    "enemy_goblin_ice_shaman.png",
    "enemy_goblin_thorn_hunter.png",
    "enemy_goblin_bark_guard.png",
    "enemy_goblin_spore_shaman.png",
)
PREVIEW_ITEMS = (
    ("enemy_goblin_grunt.png", "GRUNT"),
    ("enemy_goblin_fire_scout.png", "FIRE SCOUT"),
    ("enemy_goblin_ice_bruiser.png", "ICE BRUISER"),
    ("enemy_goblin_nature_raider.png", "NATURE RAIDER"),
    ("enemy_goblin_chief.png", "CHIEF"),
    ("enemy_goblin_warlord.png", "WARLORD"),
    ("enemy_goblin_slinger.png", "SLINGER"),
    ("enemy_goblin_golden.png", "GOLDEN"),
    ("enemy_goblin_fire_mage.png", "FIRE MAGE"),
    ("enemy_goblin_fire_bomber.png", "FIRE BOMBER"),
    ("enemy_goblin_frost_stalker.png", "FROST STALKER"),
    ("enemy_goblin_ice_archer.png", "ICE ARCHER"),
    ("enemy_goblin_ice_shaman.png", "ICE SHAMAN"),
    ("enemy_goblin_thorn_hunter.png", "THORN HUNTER"),
    ("enemy_goblin_bark_guard.png", "BARK GUARD"),
    ("enemy_goblin_spore_shaman.png", "SPORE SHAMAN"),
)

LOGICAL_SIZE = 64
FINAL_SIZE = 128
MAX_SUBJECT_SIZE = 52
BOTTOM_PADDING = 4
ALPHA_THRESHOLD = 96


def process(source: Path, destination: Path) -> tuple[int, int, int, int, int]:
    image = Image.open(source).convert("RGBA")
    alpha = image.getchannel("A")
    bbox = alpha.point(lambda value: 255 if value >= 12 else 0).getbbox()
    if bbox is None:
        raise ValueError(f"No opaque subject found in {source}")

    subject = image.crop(bbox)
    scale = min(MAX_SUBJECT_SIZE / subject.width, MAX_SUBJECT_SIZE / subject.height)
    resized_size = (
        max(1, round(subject.width * scale)),
        max(1, round(subject.height * scale)),
    )
    subject = subject.resize(resized_size, Image.Resampling.NEAREST)

    subject_alpha = subject.getchannel("A").point(
        lambda value: 255 if value >= ALPHA_THRESHOLD else 0
    )
    subject.putalpha(subject_alpha)

    logical = Image.new("RGBA", (LOGICAL_SIZE, LOGICAL_SIZE), (255, 0, 255, 0))
    x = (LOGICAL_SIZE - subject.width) // 2
    y = LOGICAL_SIZE - BOTTOM_PADDING - subject.height
    logical.alpha_composite(subject, (x, y))

    hard_alpha = logical.getchannel("A").point(lambda value: 255 if value else 0)
    rgb_for_palette = Image.new("RGB", logical.size, (255, 0, 255))
    rgb_for_palette.paste(logical.convert("RGB"), mask=hard_alpha)
    quantized = rgb_for_palette.quantize(
        colors=16,
        method=Image.Quantize.MEDIANCUT,
        dither=Image.Dither.NONE,
    ).convert("RGB")
    final_logical = Image.merge("RGBA", (*quantized.split(), hard_alpha))

    final = final_logical.resize(
        (FINAL_SIZE, FINAL_SIZE),
        Image.Resampling.NEAREST,
    )
    destination.parent.mkdir(parents=True, exist_ok=True)
    final.save(destination, optimize=True)

    final_bbox = final.getchannel("A").getbbox()
    if final_bbox is None:
        raise ValueError(f"Processed sprite is empty: {destination}")
    visible_colors = len(
        {
            pixel[:3]
            for pixel in final.getdata()
            if pixel[3] > 0
        }
    )
    return (*final_bbox, visible_colors)


def main() -> None:
    for name in NAMES:
        bbox = process(SOURCE_DIR / name, OUTPUT_DIR / name)
        print(f"{name}: bbox={bbox[:4]}, visible_colors={bbox[4]}")
    create_preview(Path("Temp/goblin-enemy-roster-static.png"))


def create_preview(destination: Path) -> None:
    columns = 4
    rows = (len(PREVIEW_ITEMS) + columns - 1) // columns
    cell_width = 280
    cell_height = 300
    preview = Image.new(
        "RGB",
        (columns * cell_width, rows * cell_height),
        (17, 20, 24),
    )

    for index, (name, label) in enumerate(PREVIEW_ITEMS):
        column = index % columns
        row = index // columns
        origin_x = column * cell_width
        origin_y = row * cell_height
        panel = Image.new("RGB", (cell_width - 12, cell_height - 12), (34, 39, 45))
        preview.paste(panel, (origin_x + 6, origin_y + 6))

        sprite = Image.open(OUTPUT_DIR / name).convert("RGBA").resize(
            (256, 256),
            Image.Resampling.NEAREST,
        )
        preview.paste(sprite, (origin_x + 12, origin_y + 8), sprite)

        draw = ImageDraw.Draw(preview)
        label_bbox = draw.textbbox((0, 0), label)
        label_width = label_bbox[2] - label_bbox[0]
        label_x = origin_x + (cell_width - label_width) // 2
        draw.text((label_x, origin_y + 276), label, fill=(238, 219, 164))

    destination.parent.mkdir(parents=True, exist_ok=True)
    preview.save(destination, optimize=True)
    print(f"preview: {destination}")


if __name__ == "__main__":
    main()
