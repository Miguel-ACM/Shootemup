"""Brass Knuckles: four finger rings under a studded striking ridge, joined by a curved
palm grip. Polished brass with darker worn brass on the grip."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
BRASS, WORN, SHINE = range(3)
k.begin([
    (0.88, 0.66, 0.24),
    (0.58, 0.40, 0.15),
    (1.00, 0.88, 0.52),
])

RINGS = (-0.285, -0.095, 0.095, 0.285)
for x in RINGS:
    k.torus(0.085, 0.034, loc=(x, 0.07, 0), tile=BRASS, ring=14, tube=6, scale=(1, 1.08, 1.6))
    # A rounded stud on the striking ridge above each finger.
    k.sphere(0.055, loc=(x, 0.205, 0), scale=(1, 0.75, 1.1), tile=SHINE, u=8, v=5)
# The striking ridge that bridges all four rings.
k.box((0.76, 0.06, 0.1), loc=(0, 0.17, 0), tile=BRASS, bevel=0.015)
# Palm grip: a shallow arc under the rings, thickest in the middle.
grip = []
for i in range(9):
    t = i / 8
    x = -0.34 + 0.68 * t
    y = -0.12 - 0.06 * math.sin(math.pi * t)
    r = 0.045 + 0.02 * math.sin(math.pi * t)
    grip.append([(x, y + r * math.cos(a), r * 1.2 * math.sin(a)) for a in
                 (2 * math.pi * j / 8 for j in range(8))])
k.loft(grip, tile=WORN)
for x in (-0.3, 0.3):
    k.box((0.05, 0.13, 0.08), loc=(x, -0.05, 0), tile=WORN)
k.finish(OUT, ortho_scale=1.1)
