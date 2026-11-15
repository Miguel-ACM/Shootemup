"""District condition and event icons for the left margin.

Directly drawn 32px pixel art in the encounter icon style (dark ink outline, three
body tones and a pale glint): dusk, night, blizzard, sandstorm, thunderstorm,
minefield, deep passages, candy rain, eruption, spear rain and rush hour.
Writes Assets/Resources/EncounterIcons/Condition<Name>.png and a review sheet.
Usage: python Tools/draw_condition_icons.py
"""
from pathlib import Path
import math
import uuid

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "Assets/Resources/EncounterIcons"
REVIEW = ROOT / "ArtSource/ConditionIconReview"
INK = "#111d2b"
PALE = "#f2edda"


def canvas():
    image = Image.new("RGBA", (32, 32))
    return image, ImageDraw.Draw(image)


def dusk():
    image, d = canvas()
    # A low sun sinking behind a hill line, rays fanning up.
    for i, angle in enumerate(range(200, 341, 28)):
        a = math.radians(angle)
        x, y = 16 + math.cos(a) * 15, 21 + math.sin(a) * 15
        d.line([(16 + math.cos(a) * 10, 21 + math.sin(a) * 10), (x, y)], fill=INK, width=3)
        d.line([(16 + math.cos(a) * 10, 21 + math.sin(a) * 10), (x, y)], fill="#f5a04a", width=1)
    d.pieslice((6, 11, 26, 31), 180, 360, fill=INK)
    d.pieslice((8, 13, 24, 29), 180, 360, fill="#f58a3a")
    d.pieslice((10, 15, 22, 27), 180, 360, fill="#ffc06a")
    d.rectangle((0, 21, 31, 31), fill=INK)
    d.polygon([(0, 23), (9, 20), (17, 23), (25, 21), (31, 22), (31, 31), (0, 31)], fill="#5b3a5e")
    d.polygon([(0, 27), (12, 25), (31, 27), (31, 31), (0, 31)], fill="#3b2846")
    d.point((12, 17), fill=PALE)
    return image


def night():
    image, d = canvas()
    d.ellipse((5, 4, 27, 26), fill=INK)
    d.ellipse((7, 6, 25, 24), fill="#e6e9c8")
    d.ellipse((12, 3, 31, 21), fill=INK)
    d.ellipse((14, 3, 33, 19), fill=(0, 0, 0, 0))
    d.ellipse((9, 9, 12, 12), fill="#c9cca6")
    d.point((9, 8), fill="#ffffff")
    for x, y in ((24, 22), (27, 8), (20, 27), (4, 28)):
        d.point((x, y), fill="#fff6c4")
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            d.point((x + dx, y + dy), fill="#c9b864")
    return image


def blizzard():
    image, d = canvas()
    # Wind streaks behind a six-armed flake.
    for y, x0, x1 in ((5, 2, 18), (26, 12, 30), (15, 0, 7)):
        d.line([(x0, y), (x1, y)], fill=INK, width=3)
        d.line([(x0 + 1, y), (x1 - 1, y)], fill="#b9dcef")
    cx, cy = 18, 16
    for k in range(6):
        a = math.radians(k * 60 + 90)
        ex, ey = cx + math.cos(a) * 11, cy + math.sin(a) * 11
        d.line([(cx, cy), (ex, ey)], fill=INK, width=4)
    for k in range(6):
        a = math.radians(k * 60 + 90)
        ex, ey = cx + math.cos(a) * 10, cy + math.sin(a) * 10
        d.line([(cx, cy), (ex, ey)], fill="#eaf6ff", width=2)
        mx, my = cx + math.cos(a) * 6, cy + math.sin(a) * 6
        for side in (-1, 1):
            b = a + side * math.radians(50)
            d.line([(mx, my), (mx + math.cos(b) * 3, my + math.sin(b) * 3)], fill="#8fc6e6")
    d.ellipse((cx - 2, cy - 2, cx + 2, cy + 2), fill="#ffffff")
    return image


def sandstorm():
    image, d = canvas()
    d.polygon([(0, 26), (10, 20), (20, 24), (31, 19), (31, 31), (0, 31)], fill=INK)
    d.polygon([(0, 27), (10, 22), (20, 26), (31, 21), (31, 31), (0, 31)], fill="#d79a4a")
    d.polygon([(10, 22), (20, 26), (31, 21), (31, 24), (20, 28), (10, 24)], fill="#e9b765")
    # A swirling gust of sand.
    for r, w, colour in ((12, 3, INK), (12, 1, "#f0c27a"), (8, 3, INK), (8, 1, "#e0a457"), (4, 2, INK), (4, 1, "#f6d79a")):
        d.arc((16 - r, 11 - r, 16 + r, 11 + r), 200, 470, fill=colour, width=w)
    for x, y in ((3, 8), (28, 5), (26, 15), (6, 16)):
        d.point((x, y), fill="#f6d79a")
    return image


def thunderstorm():
    image, d = canvas()
    d.ellipse((2, 4, 16, 16), fill=INK)
    d.ellipse((10, 1, 26, 15), fill=INK)
    d.ellipse((18, 6, 31, 17), fill=INK)
    d.rectangle((4, 10, 29, 17), fill=INK)
    d.ellipse((4, 6, 15, 15), fill="#6f7894")
    d.ellipse((12, 3, 24, 14), fill="#8a93ad")
    d.ellipse((19, 8, 29, 15), fill="#6f7894")
    d.rectangle((6, 11, 27, 15), fill="#5a627c")
    d.point((14, 5), fill=PALE)
    bolt = [(17, 14), (11, 23), (16, 23), (12, 31), (22, 20), (17, 20), (21, 14)]
    d.polygon(bolt, fill=INK)
    d.polygon([(17, 15), (13, 22), (17, 22), (14, 29), (20, 21), (16, 21), (19, 15)], fill="#ffd34d")
    d.line([(17, 16), (14, 21)], fill="#fff4b8")
    return image


def minefield():
    image, d = canvas()
    d.line([(16, 31), (16, 25)], fill=INK, width=3)
    d.line([(16, 31), (16, 26)], fill="#7a8a96")
    for k in range(8):
        a = math.radians(k * 45)
        x0, y0 = 16 + math.cos(a) * 7, 14 + math.sin(a) * 7
        x1, y1 = 16 + math.cos(a) * 14.5, 14 + math.sin(a) * 14.5
        d.line([(x0, y0), (x1, y1)], fill=INK, width=5)
        d.line([(x0, y0), (x1 - math.cos(a), y1 - math.sin(a))], fill="#c9a24a", width=3)
    d.ellipse((7, 5, 25, 23), fill=INK)
    d.ellipse((9, 7, 23, 21), fill="#41505e")
    d.ellipse((9, 7, 19, 17), fill="#5d6e7d")
    d.ellipse((13, 11, 19, 17), fill="#2bc2d6")
    d.point((15, 12), fill="#d7fbff")
    d.point((11, 8), fill=PALE)
    return image


def deep_passages():
    image, d = canvas()
    # Two zigzag tunnel walls with an arrow threading the gap.
    left = [(0, 0), (9, 0), (4, 8), (12, 16), (4, 24), (9, 31), (0, 31)]
    right = [(31, 0), (22, 0), (27, 8), (19, 16), (27, 24), (22, 31), (31, 31)]
    for wall in (left, right):
        d.polygon(wall, fill=INK)
    d.polygon([(0, 1), (7, 1), (2, 8), (10, 16), (2, 24), (7, 30), (0, 30)], fill="#8c6a52")
    d.polygon([(31, 1), (24, 1), (29, 8), (21, 16), (29, 24), (24, 30), (31, 30)], fill="#8c6a52")
    d.line([(2, 8), (10, 16)], fill="#d9b48a")
    d.line([(29, 8), (21, 16)], fill="#d9b48a")
    path = [(16, 2), (12, 9), (19, 16), (12, 23), (16, 29)]
    d.line(path, fill=INK, width=4)
    d.line(path, fill="#f2b84a", width=2)
    d.polygon([(16, 31), (12, 26), (20, 26)], fill="#f2b84a")
    return image


def candy_rain():
    image, d = canvas()
    # A wrapped sweet tumbling down with motion streaks, bouncing off a round below.
    for x in (9, 15, 21):
        d.line([(x, 0), (x, 6)], fill="#f7a8c8")
    d.polygon([(4, 13), (9, 9), (9, 19)], fill=INK)
    d.polygon([(28, 13), (23, 9), (23, 19)], fill=INK)
    d.polygon([(5, 13), (9, 10), (9, 17)], fill="#ffd0e4")
    d.polygon([(27, 13), (23, 10), (23, 17)], fill="#ffd0e4")
    d.ellipse((8, 7, 24, 20), fill=INK)
    d.ellipse((9, 8, 23, 19), fill="#f05a9a")
    d.arc((10, 9, 22, 18), 200, 330, fill="#ffd0e4", width=2)
    d.point((12, 10), fill=PALE)
    d.ellipse((12, 24, 20, 31), fill=INK)
    d.ellipse((13, 25, 19, 30), fill="#ffe36a")
    d.point((14, 26), fill="#ffffff")
    d.line([(16, 21), (16, 23)], fill="#ffe36a")
    return image


def eruption():
    image, d = canvas()
    d.polygon([(2, 31), (11, 13), (21, 13), (30, 31)], fill=INK)
    d.polygon([(4, 30), (12, 15), (20, 15), (28, 30)], fill="#4a3434")
    d.polygon([(12, 15), (20, 15), (24, 22), (8, 22)], fill="#5e4040")
    d.polygon([(13, 15), (19, 15), (17, 21), (15, 19), (14, 23)], fill="#ff7a2a")
    # Fire and thrown rocks.
    d.polygon([(10, 13), (13, 4), (16, 9), (19, 2), (22, 13)], fill=INK)
    d.polygon([(12, 13), (13, 7), (16, 11), (19, 5), (20, 13)], fill="#ff8a2a")
    d.polygon([(14, 13), (16, 9), (18, 13)], fill="#ffd96a")
    for x, y in ((5, 6), (27, 5), (25, 11)):
        d.ellipse((x - 2, y - 2, x + 2, y + 2), fill=INK)
        d.point((x, y), fill="#ff9a3a")
    return image


def spear_rain():
    image, d = canvas()
    # A falling spear with its warning column.
    d.rectangle((14, 0, 18, 31), fill=(255, 211, 77, 60))
    d.line([(16, 0), (16, 31)], fill="#ffe9a0")
    d.line([(16, 1), (16, 20)], fill=INK, width=4)
    d.line([(16, 1), (16, 20)], fill="#c9a24a", width=2)
    d.polygon([(16, 31), (10, 19), (16, 22), (22, 19)], fill=INK)
    d.polygon([(16, 29), (12, 21), (16, 23), (20, 21)], fill="#f7f1d8")
    d.line([(16, 23), (16, 28)], fill="#ffffff")
    for x in (7, 25):
        d.line([(x, 4), (x, 12)], fill="#ffd34d")
        d.polygon([(x, 15), (x - 2, 11), (x + 2, 11)], fill="#ffd34d")
    return image


def rush_hour():
    image, d = canvas()
    # A traffic light over a jam of cars.
    d.rectangle((12, 0, 20, 17), fill=INK)
    d.rectangle((13, 1, 19, 16), fill="#2d3640")
    for y, colour in ((2, "#5a2020"), (7, "#ffc23a"), (12, "#2a5a32")):
        d.ellipse((14, y, 18, y + 4), fill=colour)
    d.point((15, 8), fill="#fff2b8")

    def car(x, y, colour):
        d.rectangle((x + 1, y, x + 11, y + 7), fill=INK)
        d.rectangle((x, y + 1, x + 12, y + 6), fill=INK)
        d.rectangle((x + 1, y + 1, x + 11, y + 6), fill=colour)
        d.rectangle((x + 3, y + 2, x + 9, y + 3), fill="#bfe3f0")

    car(1, 19, "#e05050")
    car(19, 19, "#4a8ae0")
    car(10, 24, "#f2c84a")
    return image


ICONS = {
    "Dusk": (dusk, "DUSK"),
    "Night": (night, "NIGHT"),
    "Blizzard": (blizzard, "BLIZZARD"),
    "Sandstorm": (sandstorm, "SANDSTORM"),
    "Thunderstorm": (thunderstorm, "THUNDERSTORM"),
    "Minefield": (minefield, "MINEFIELD"),
    "DeepPassages": (deep_passages, "DEEP PASSAGES"),
    "CandyRain": (candy_rain, "CANDY RAIN"),
    "Eruption": (eruption, "ERUPTION"),
    "SpearRain": (spear_rain, "SPEAR RAIN"),
    "RushHour": (rush_hour, "RUSH HOUR"),
}


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    REVIEW.mkdir(parents=True, exist_ok=True)
    columns = 4
    rows = math.ceil(len(ICONS) / columns)
    sheet = Image.new("RGB", (columns * 220, rows * 150), "#17232f")
    draw = ImageDraw.Draw(sheet)
    for index, (name, (make, caption)) in enumerate(ICONS.items()):
        icon = make()
        icon.save(DEST / ("Condition" + name + ".png"))
        meta = DEST / ("Condition" + name + ".png.meta")
        if not meta.exists():
            lines = (DEST / "Reforge.png.meta").read_text().splitlines()
            lines[1] = f"guid: {uuid.uuid4().hex}"
            meta.write_text("\n".join(lines) + "\n")
        x, y = (index % columns) * 220 + 12, (index // columns) * 150 + 10
        large = icon.resize((128, 128), Image.Resampling.NEAREST)
        sheet.paste(large, (x, y), large)
        small = icon.resize((64, 64), Image.Resampling.NEAREST)
        sheet.paste(small, (x + 140, y), small)
        sheet.paste(icon, (x + 156, y + 80), icon)
        draw.text((x + 140, y + 120), caption, fill=PALE)
    sheet.save(REVIEW / "ConditionIcons.png")
    print("Wrote", len(ICONS), "condition icons and the review sheet.")


if __name__ == "__main__":
    main()
