"""Umbrella: an open umbrella with an eight-panel red and cream canopy, scalloped edge,
rib tips, a steel shaft and a curved wooden handle."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
RED, CREAM, UNDER, STEEL, WOOD, TIP = range(6)
k.begin([
    (0.88, 0.22, 0.20),
    (0.97, 0.91, 0.78),
    (0.55, 0.16, 0.16),
    (0.62, 0.64, 0.68),
    (0.56, 0.31, 0.15),
    (0.20, 0.20, 0.22),
])

PANELS, PER = 8, 3
COUNT = PANELS * PER


def ring(y, radius, scallop=0.0, inner=False):
    points = []
    for i in range(COUNT):
        a = 2 * math.pi * i / COUNT
        # Panels sag between their ribs, so the rim dips between rib points.
        between = abs(math.sin(math.pi * (i % PER) / PER))
        r = radius * (1 - 0.04 * between)
        points.append((math.cos(a) * r, y - scallop * between, math.sin(a) * r))
    return points


TOP = 0.36
sections = [ring(TOP - 0.01, 0.03), ring(TOP - 0.06, 0.17), ring(TOP - 0.14, 0.31), ring(TOP - 0.24, 0.42, 0.04),
            ring(TOP - 0.27, 0.41, 0.04), ring(TOP - 0.2, 0.3), ring(TOP - 0.12, 0.15)]
faces = k.loft(sections, tile=RED)
for f in faces:
    if not f.is_valid or len(f.verts) != 4:
        continue
    c = f.calc_center_median()
    a = math.atan2(c.z, c.x) % (2 * math.pi)
    panel = int(a / (2 * math.pi / PANELS))
    outside = c.y > TOP - 0.27 and math.hypot(c.x, c.z) > 0.01 and f.normal.y >= -0.2
    f.material_index = (RED if panel % 2 == 0 else CREAM) if outside else UNDER
# Rib tips and the ferrule on top.
for i in range(PANELS):
    a = 2 * math.pi * i / PANELS
    k.sphere(0.022, loc=(math.cos(a) * 0.42, TOP - 0.25, math.sin(a) * 0.42), tile=TIP, u=6, v=4)
k.cylinder(0.018, 0.008, 0.07, loc=(0, TOP + 0.03, 0), rot=(90, 0, 0), tile=TIP, segments=6)
# Shaft and the J handle.
k.cylinder(0.016, 0.016, 0.62, loc=(0, -0.1, 0), rot=(90, 0, 0), tile=STEEL, segments=6)
k.torus(0.075, 0.027, loc=(0.075, -0.41, 0), rot=(0, 0, 180), tile=WOOD, ring=10, tube=6, arc=180)
k.cylinder(0.03, 0.027, 0.06, loc=(0, -0.38, 0), rot=(90, 0, 0), tile=WOOD, segments=6)
k.finish(OUT, ortho_scale=1.2)
