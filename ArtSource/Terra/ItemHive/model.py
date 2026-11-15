"""Hive: a woven straw skep, a domed beehive of stacked coils with a dark entrance, a
dribble of honey and two bees buzzing at the door."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
STRAW, STRAW_DARK, DOOR, HONEY, BEE, STRIPE, WING = range(7)
k.begin([(0.92, 0.72, 0.34), (0.74, 0.54, 0.22), (0.16, 0.1, 0.06), (1.0, 0.66, 0.12), (1.0, 0.8, 0.15), (0.12, 0.1, 0.08), (0.9, 0.95, 1.0)])

COILS = 6
for i in range(COILS):
    t = i / COILS
    r = 0.34 * math.cos(t * 1.35) + 0.02
    k.torus(r, 0.06, loc=(0, -0.33 + i * 0.11, 0), rot=(90, 0, 0), scale=(1, 1, 0.85), tile=STRAW if i % 2 == 0 else STRAW_DARK, ring=18, tube=6)
k.sphere(0.12, loc=(0, 0.33, 0), scale=(1, 0.6, 1), tile=STRAW, u=10, v=6)
k.sphere(0.09, loc=(0, -0.3, 0.36), scale=(1.3, 0.8, 0.4), tile=DOOR, u=10, v=6)
k.sphere(0.04, loc=(0.12, -0.37, 0.38), scale=(0.7, 1.4, 0.6), tile=HONEY, u=8, v=5)


def bee(x, y, s):
    k.sphere(0.05 * s, loc=(x, y, 0.4), scale=(1.4, 1, 1), tile=BEE, u=8, v=6)
    for dx in (-0.02, 0.03):
        k.box((0.02 * s, 0.1 * s, 0.1 * s), loc=(x + dx * s, y, 0.4), tile=STRIPE)
    for side in (-1, 1):
        k.sphere(0.045 * s, loc=(x, y + 0.05 * s, 0.4 + side * 0.03 * s), scale=(0.7, 1, 0.25), tile=WING, u=6, v=4)


bee(0.3, -0.12, 1.7)
bee(-0.3, 0.14, 1.4)
k.finish(OUT, ortho_scale=1.15)
