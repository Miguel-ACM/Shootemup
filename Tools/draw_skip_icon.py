"""The SKIP encounter icon: a fast-forward pointing up into a horned boss skull.

Directly drawn 32px pixel art in the encounter icon style (dark ink outline,
three body tones and a pale glint). Writes the Unity icon and a review sheet.
Usage: python Tools/draw_skip_icon.py
"""
from pathlib import Path
import uuid

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "Assets/Resources/EncounterIcons"
REVIEW = ROOT / "ArtSource/SkipHexReview"
INK = "#111d2b"
PALE = "#f2edda"


def skip():
    image = Image.new("RGBA", (32, 32))
    d = ImageDraw.Draw(image)
    # Two stacked play triangles pointing up: fast-forward past the waves.
    for top in (17, 9):
        d.polygon([(16, top), (27, top + 12), (5, top + 12)], fill=INK)
        d.polygon([(16, top + 3), (24, top + 11), (8, top + 11)], fill="#c9cfdf")
        d.polygon([(16, top + 3), (16, top + 11), (8, top + 11)], fill="#eef1f8")
        d.line([(9, top + 10), (16, top + 4)], fill="#ffffff")
        d.line([(17, top + 4), (23, top + 10)], fill="#8790a8")
    # The horned skull it lands on.
    d.polygon([(4, 0), (9, 3), (11, 1), (21, 1), (23, 3), (28, 0), (27, 6), (24, 9), (24, 12), (20, 14),
               (12, 14), (8, 12), (8, 9), (5, 6)], fill=INK)
    d.polygon([(6, 2), (10, 5), (12, 3), (20, 3), (22, 5), (26, 2), (25, 6), (22, 8), (22, 11), (19, 12),
               (13, 12), (10, 11), (10, 8), (7, 6)], fill="#dd5466")
    d.polygon([(12, 3), (16, 3), (16, 6), (11, 8), (10, 8), (7, 6), (6, 2), (10, 5)], fill="#f2808d")
    d.rectangle((12, 7, 14, 9), fill=INK)
    d.rectangle((18, 7, 20, 9), fill=INK)
    d.point((13, 7), fill="#ffd2a0")
    d.point((19, 7), fill="#ffd2a0")
    d.line((14, 11, 18, 11), fill="#8e2a3c")
    d.point((7, 3), fill=PALE)
    return image


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    REVIEW.mkdir(parents=True, exist_ok=True)
    icon = skip()
    icon.save(DEST / "Skip.png")
    meta = DEST / "Skip.png.meta"
    if not meta.exists():
        lines = (DEST / "Reforge.png.meta").read_text().splitlines()
        lines[1] = f"guid: {uuid.uuid4().hex}"
        meta.write_text("\n".join(lines) + "\n")
    sheet = Image.new("RGB", (352, 200), "#17232f")
    for scale, x in ((4, 16), (2, 176), (1, 270)):
        large = icon.resize((32 * scale, 32 * scale), Image.Resampling.NEAREST)
        sheet.paste(large, (x, 20), large)
    ImageDraw.Draw(sheet).text((16, 170), "SKIP - straight to the boss", fill=PALE)
    sheet.save(REVIEW / "SkipIcon.png")
    print("Wrote the skip icon and preview.")


if __name__ == "__main__":
    main()
