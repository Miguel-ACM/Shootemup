"""Lighthouse: a tapering red and white striped tower on a rocky base, a railed gallery,
a glowing lamp room and a red cap."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
RED, WHITE, ROCK, LAMP, DARK, GLASS = range(6)
k.begin([(0.86, 0.18, 0.16), (0.96, 0.95, 0.92), (0.45, 0.43, 0.42), (1.0, 0.88, 0.45), (0.17, 0.18, 0.2), (0.7, 0.9, 0.95)])

k.ico(0.26, loc=(0, -0.4, 0), scale=(1.3, 0.35, 1.0), tile=ROCK, subdivisions=1)
for i in range(5):
    y0 = -0.34 + i * 0.1
    r0 = 0.17 - i * 0.012
    k.cylinder(r0, r0 - 0.012, 0.1, loc=(0, y0 + 0.05, 0), rot=(-90, 0, 0), tile=RED if i % 2 == 0 else WHITE, segments=12)
k.box((0.05, 0.08, 0.02), loc=(0, -0.08, 0.16), tile=DARK)
k.cylinder(0.17, 0.17, 0.03, loc=(0, 0.17, 0), rot=(-90, 0, 0), tile=DARK, segments=12)
for i in range(10):
    a = 2 * math.pi * i / 10
    k.box((0.012, 0.06, 0.012), loc=(math.cos(a) * 0.16, 0.21, math.sin(a) * 0.16), tile=DARK)
k.torus(0.16, 0.008, loc=(0, 0.24, 0), rot=(90, 0, 0), tile=DARK, ring=16, tube=4)
k.cylinder(0.09, 0.09, 0.13, loc=(0, 0.25, 0), rot=(-90, 0, 0), tile=GLASS, segments=8)
k.sphere(0.06, loc=(0, 0.25, 0.03), tile=LAMP, u=8, v=6)
k.cylinder(0.12, 0.0, 0.12, loc=(0, 0.37, 0), rot=(-90, 0, 0), tile=RED, segments=8)
k.sphere(0.02, loc=(0, 0.44, 0), tile=DARK, u=6, v=4)
k.finish(OUT, ortho_scale=1.15)
