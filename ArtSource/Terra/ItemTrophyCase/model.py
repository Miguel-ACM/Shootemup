"""Trophy Case: a gold cup with looping handles on a stepped dark wooden plinth with a
small engraved plate."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
GOLD, GOLD_DARK, WOOD, PLATE = range(4)
k.begin([(0.98, 0.78, 0.28), (0.74, 0.52, 0.14), (0.3, 0.17, 0.1), (0.85, 0.85, 0.8)])

profile = [(0.2, 0.3), (0.21, 0.22), (0.17, 0.08), (0.1, -0.02), (0.045, -0.06), (0.04, -0.14), (0.1, -0.18), (0.12, -0.2)]
rings = [[(math.cos(2 * math.pi * j / 14) * r, y, math.sin(2 * math.pi * j / 14) * r) for j in range(14)] for r, y in profile]
k.loft(rings, tile=GOLD)
k.cylinder(0.2, 0.2, 0.012, loc=(0, 0.296, 0), rot=(90, 0, 0), tile=GOLD_DARK, segments=14)
for side in (-1, 1):
    k.torus(0.08, 0.02, loc=(side * 0.22, 0.17, 0), rot=(0, 0, side * 90), tile=GOLD, ring=10, tube=5, arc=200)
k.box((0.36, 0.08, 0.3), loc=(0, -0.25, 0), tile=WOOD, bevel=0.01)
k.box((0.46, 0.08, 0.36), loc=(0, -0.33, 0), tile=WOOD, bevel=0.01)
k.box((0.16, 0.045, 0.01), loc=(0, -0.25, 0.155), tile=PLATE)
k.finish(OUT, ortho_scale=1.1)
