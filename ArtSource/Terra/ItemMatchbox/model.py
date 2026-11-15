"""Matchbox: a red sleeve with a cream flame label and brown striker sides, its tray pushed
up so the red match heads show, and one match standing tall and lit."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
SLEEVE, LABEL, STRIKER, TRAY, WOOD, HEAD, FLAME, CORE = range(8)
k.begin([
    (0.82, 0.16, 0.13),
    (0.97, 0.89, 0.70),
    (0.36, 0.22, 0.15),
    (0.84, 0.68, 0.46),
    (0.96, 0.84, 0.58),
    (0.70, 0.08, 0.07),
    (1.00, 0.52, 0.10),
    (1.00, 0.88, 0.36),
])

# Sleeve and its label, front and back.
k.box((0.62, 0.38, 0.22), loc=(0, -0.13, 0), tile=SLEEVE, bevel=0.01)
for z in (0.115, -0.115):
    k.box((0.48, 0.25, 0.012), loc=(0, -0.13, z), tile=LABEL)
    flame = [(0, 0.1), (-0.05, 0.0), (-0.035, -0.07), (0, -0.09), (0.035, -0.07), (0.05, 0.0)]
    k.prism(flame, 0.012, loc=(0, -0.13, z * 1.11), tile=SLEEVE)
# Striker strips down both short sides.
for x in (-0.317, 0.317):
    k.box((0.015, 0.3, 0.18), loc=(x, -0.13, 0), tile=STRIKER)
# The tray pushed up out of the sleeve, and the row of match heads in it.
k.box((0.58, 0.12, 0.2), loc=(0, 0.1, 0), tile=TRAY)
for i, x in enumerate((-0.2, -0.1, 0.0, 0.1, 0.2)):
    if i == 3:
        continue
    k.box((0.035, 0.08, 0.035), loc=(x, 0.19, 0.03 * (i % 2)), tile=WOOD)
    k.sphere(0.034, loc=(x, 0.24, 0.03 * (i % 2)), scale=(1, 1.25, 1), tile=HEAD, u=8, v=5)
# One match standing tall and lit.
k.box((0.035, 0.3, 0.035), loc=(0.1, 0.3, 0), tile=WOOD)
k.sphere(0.036, loc=(0.1, 0.46, 0), scale=(1, 1.25, 1), tile=HEAD, u=8, v=5)
k.sphere(0.1, loc=(0.1, 0.57, 0), scale=(0.85, 1.6, 0.85), tile=FLAME, u=10, v=6)
k.sphere(0.058, loc=(0.1, 0.545, 0.05), scale=(0.8, 1.5, 0.8), tile=CORE, u=8, v=5)
k.finish(OUT, ortho_scale=1.25)
