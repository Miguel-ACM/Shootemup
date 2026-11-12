"""Draw the save-card berth at its native pixel resolution."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
image = Image.new("RGBA", (128, 88), "#101c27")
draw = ImageDraw.Draw(image)
ink = "#09131d"
steel = "#3c5360"
edge = "#6c8790"
light = "#b8d4ca"
brass = "#cfa464"

# Recessed launch doorway, lit from the open sky.
draw.polygon([(23, 0), (104, 0), (114, 33), (13, 33)], fill="#263c4a")
draw.polygon([(31, 0), (96, 0), (103, 29), (24, 29)], fill="#172c3b")
draw.polygon([(38, 0), (89, 0), (94, 23), (33, 23)], fill="#416275")
draw.polygon([(40, 0), (87, 0), (91, 18), (36, 18)], fill="#6c929b")
draw.line([(40, 1), (87, 1)], fill=light)
for x in (23, 103):
    draw.line([(x, 0), (x + (-10 if x < 64 else 10), 32)], fill=edge, width=2)
for y in (8, 17, 26):
    draw.line([(17, y), (25, y)], fill=brass)
    draw.line([(102, y), (110, y)], fill=brass)

# Broad deck plates converge toward the door.
draw.polygon([(13, 33), (114, 33), (127, 87), (0, 87)], fill="#263942")
for y in (37, 49, 65, 86):
    draw.line([(0, y), (127, y)], fill="#172831")
    draw.line([(1, y + 1), (126, y + 1)], fill="#334952")
for x in (-30, 6, 39, 88, 121, 157):
    draw.line([(64 + (x - 64) // 3, 33), (x, 87)], fill="#152731")

# A recessed service pad provides a quiet silhouette behind the saved hull.
pad = [(41, 28), (86, 28), (104, 69), (92, 80), (35, 80), (23, 69)]
draw.polygon([(x, y + 2) for x, y in pad], fill=ink)
draw.polygon(pad, fill="#1b303a", outline=steel)
draw.line([(42, 30), (85, 30)], fill=edge)
draw.line([(35, 78), (92, 78)], fill=edge)
for x, sign in ((35, 1), (92, -1)):
    draw.line([(x, 65), (x, 72), (x + sign * 9, 72)], fill=brass, width=2)
    draw.line([(x + sign * 6, 37), (x + sign * 6, 33), (x + sign * 13, 33)], fill=brass)
draw.line([(60, 54), (67, 54)], fill="#475e62")
draw.line([(64, 50), (64, 58)], fill="#475e62")

# Attached wall consoles, structural ribs and floor lamps.
for mirror in (False, True):
    def point(px, py):
        return (127 - px if mirror else px, py)

    draw.polygon([point(0, 0), point(13, 0), point(17, 35), point(10, 87), point(0, 87)], fill=ink)
    draw.line([point(11, 2), point(15, 34), point(8, 86)], fill=steel, width=2)
    for y in (10, 27, 47, 69):
        draw.line([point(2, y), point(11, y + 2)], fill=steel, width=3)
    draw.polygon([point(5, 40), point(17, 42), point(18, 56), point(5, 54)], fill="#405965", outline=ink)
    draw.polygon([point(7, 43), point(14, 44), point(15, 50), point(7, 49)], fill="#74b5b5")
    draw.line([point(8, 45), point(12, 46)], fill=light)
    draw.point(point(9, 52), fill=brass)
    for y in (34, 60, 80):
        draw.line([point(18, y), point(22, y)], fill=brass, width=2)
        draw.line([point(18, y - 1), point(21, y - 1)], fill=light)

image.save(root / "Assets/Resources/DockArt/SaveHangar.png")
