"""Level modifier icons: ELITES, COIN RUSH and FAST FIRE.

Directly drawn 32px pixel art in the encounter icon style (dark ink outline,
three body tones and a pale glint). Writes the Unity icons and a review sheet.
Usage: python Tools/draw_level_modifier_icons.py
"""
from pathlib import Path
import uuid

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "Assets/Resources/EncounterIcons"
REVIEW = ROOT / "ArtSource/LevelModifierReview"
INK = "#111d2b"
PALE = "#f2edda"


def elites():
    """A crowned enemy fighter inside a broken gold ring."""
    image = Image.new("RGBA", (32, 32))
    d = ImageDraw.Draw(image)
    # The ring, drawn as four arcs with gaps, like the in-game elite mark.
    for start in (20, 110, 200, 290):
        d.arc((1, 3, 30, 31), start, start + 60, fill=INK, width=4)
        d.arc((2, 4, 29, 30), start + 4, start + 56, fill="#f5c84a", width=2)
    # A downward-facing fighter: swept wings and a nose pointing at the pilot.
    hull = [(16, 27), (19, 21), (26, 17), (25, 13), (19, 14), (18, 10), (14, 10), (13, 14), (7, 13), (6, 17), (13, 21)]
    d.polygon(hull, fill=INK)
    d.polygon([(16, 25), (18, 20), (24, 16), (23, 15), (18, 16), (17, 12), (15, 12), (14, 16), (9, 15), (8, 16), (14, 20)], fill="#7d8aa3")
    d.polygon([(16, 25), (14, 20), (8, 16), (9, 15), (14, 16), (15, 12), (16, 12)], fill="#a9b5c9")
    d.rectangle((15, 17, 16, 19), fill="#dd5466")
    # The crown above it.
    d.polygon([(9, 9), (9, 2), (12, 5), (16, 0), (20, 5), (23, 2), (23, 9)], fill=INK)
    d.polygon([(10, 8), (10, 4), (12, 6), (16, 2), (20, 6), (22, 4), (22, 8)], fill="#f5c84a")
    d.line([(11, 7), (21, 7)], fill="#c88a22")
    d.point((16, 3), fill="#fff3c4")
    d.point((11, 5), fill=PALE)
    return image


def coins():
    """A spilling stack of coins with a falling coin and sparkles."""
    image = Image.new("RGBA", (32, 32))
    d = ImageDraw.Draw(image)

    def coin(x, y):
        d.ellipse((x, y, x + 11, y + 6), fill=INK)
        d.rectangle((x, y + 3, x + 11, y + 6), fill=INK)
        d.ellipse((x + 1, y + 3, x + 10, y + 8), fill=INK)
        d.ellipse((x + 1, y + 1, x + 10, y + 5), fill="#f5c84a")
        d.rectangle((x + 1, y + 3, x + 10, y + 5), fill="#c88a22")
        d.ellipse((x + 1, y + 1, x + 10, y + 4), fill="#f5c84a")
        d.ellipse((x + 3, y + 2, x + 8, y + 3), fill="#ffe28a")

    for y in (22, 18, 14):
        coin(3, y)
    for y in (23, 19):
        coin(13, y)
    coin(19, 24)
    # A coin falling into the pile, seen face on.
    d.ellipse((19, 3, 29, 13), fill=INK)
    d.ellipse((20, 4, 28, 12), fill="#f5c84a")
    d.ellipse((22, 6, 26, 10), fill="#c88a22")
    d.point((22, 5), fill="#fff3c4")
    d.line([(24, 1), (24, 2)], fill=PALE)
    # Sparkles.
    for x, y in ((14, 8), (8, 9)):
        d.point((x, y), fill="#fff3c4")
        d.point((x - 1, y), fill="#f5c84a")
        d.point((x + 1, y), fill="#f5c84a")
        d.point((x, y - 1), fill="#f5c84a")
        d.point((x, y + 1), fill="#f5c84a")
    return image


def fast_fire():
    """A hostile round racing down with speed streaks and double chevrons."""
    image = Image.new("RGBA", (32, 32))
    d = ImageDraw.Draw(image)
    # Speed streaks trailing above the round.
    for x, top, bottom in ((10, 2, 14), (16, 0, 12), (22, 2, 14)):
        d.rectangle((x - 1, top, x + 1, bottom), fill=INK)
        d.line([(x, top + 1), (x, bottom - 1)], fill="#f2808d")
    d.line([(16, 1), (16, 6)], fill="#ffd2d8")
    # The round itself: bright core, red body, dark rim.
    d.ellipse((7, 10, 25, 28), fill=INK)
    d.ellipse((9, 12, 23, 26), fill="#dd5466")
    d.ellipse((11, 14, 21, 24), fill="#f2808d")
    d.ellipse((13, 16, 19, 22), fill="#fff1f2")
    d.point((12, 14), fill=PALE)
    # Double chevrons at the sides: faster.
    for side in (-1, 1):
        cx = 16 + side * 13
        for dy in (14, 19):
            d.line([(cx - 2, dy), (cx, dy + 3), (cx + 2, dy)], fill=INK, width=3)
            d.line([(cx - 2, dy), (cx, dy + 2), (cx + 2, dy)], fill="#f5c84a")
    return image


ICONS = {
    "LevelElites": (elites, "ELITES - more elite ships"),
    "LevelCoins": (coins, "COIN RUSH - more coin drops"),
    "LevelFastFire": (fast_fire, "FAST FIRE - hostile rounds +20%"),
}


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    REVIEW.mkdir(parents=True, exist_ok=True)
    sheet = Image.new("RGB", (560, 150 * len(ICONS)), "#17232f")
    draw = ImageDraw.Draw(sheet)
    for row, (name, (make, caption)) in enumerate(ICONS.items()):
        icon = make()
        icon.save(DEST / (name + ".png"))
        meta = DEST / (name + ".png.meta")
        if not meta.exists():
            lines = (DEST / "Reforge.png.meta").read_text().splitlines()
            lines[1] = f"guid: {uuid.uuid4().hex}"
            meta.write_text("\n".join(lines) + "\n")
        y = row * 150 + 10
        for scale, x in ((4, 16), (2, 176), (1, 270)):
            large = icon.resize((32 * scale, 32 * scale), Image.Resampling.NEAREST)
            sheet.paste(large, (x, y), large)
        draw.text((320, y + 50), caption, fill=PALE)
    sheet.save(REVIEW / "LevelModifierIcons.png")
    print("Wrote the level modifier icons and preview.")


if __name__ == "__main__":
    main()
