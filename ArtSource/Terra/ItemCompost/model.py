"""Compost: a ribbed green garden bin, a banana peel and an apple
core poking out of the dark heap, and a muted jade fume rising from it."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
BIN, RIB, LID, SOIL, PEEL, APPLE, CORE, JADE, STEM = range(9)
k.begin([
    (0.30, 0.56, 0.32),
    (0.19, 0.38, 0.22),
    (0.42, 0.68, 0.40),
    (0.33, 0.22, 0.13),
    (0.96, 0.82, 0.26),
    (0.80, 0.16, 0.14),
    (0.96, 0.90, 0.70),
    (0.45, 0.78, 0.63),
    (0.38, 0.24, 0.12),
])

# A slightly tapered bin, wider at the top, with raised ribs.
bin_shape = [(-0.27, -0.4), (0.27, -0.4), (0.31, 0.12), (-0.31, 0.12)]
k.prism(bin_shape, 0.42, loc=(0, 0, 0), tile=BIN, side_tile=BIN)
for y in (-0.28, -0.12):
    w = 0.29 + 0.04 * (y + 0.4) / 0.52
    k.box((w * 2 + 0.03, 0.045, 0.45), loc=(0, y, 0), tile=RIB)
# The heap rounding over the rim.
k.sphere(0.29, loc=(0, 0.12, 0), scale=(1, 0.42, 0.68), tile=SOIL, u=12, v=6)
# Banana peel: three curled strips flopping out to the left.
for angle, length in ((150, 0.2), (115, 0.18), (190, 0.16)):
    a = math.radians(angle)
    k.box((length, 0.035, 0.07), loc=(-0.12 + math.cos(a) * length * 0.5, 0.2 + math.sin(a) * length * 0.35, 0.1),
          rot=(0, 0, angle), tile=PEEL)
# Apple core on the right: two red ends and a cream waist with a stem.
k.cylinder(0.06, 0.035, 0.05, loc=(0.13, 0.17, 0.1), rot=(90, 0, 0), tile=APPLE, segments=8)
k.cylinder(0.035, 0.035, 0.07, loc=(0.13, 0.225, 0.1), rot=(90, 0, 0), tile=CORE, segments=8)
k.cylinder(0.035, 0.06, 0.05, loc=(0.13, 0.28, 0.1), rot=(90, 0, 0), tile=APPLE, segments=8)
k.box((0.015, 0.06, 0.015), loc=(0.14, 0.33, 0.1), rot=(0, 0, -15), tile=STEM)
# The jade fume: three soft puffs climbing from the heap.
for x, y, r in ((0.0, 0.33, 0.09), (-0.06, 0.43, 0.075), (0.03, 0.51, 0.06)):
    k.ico(r, loc=(x, y, 0.02), tile=JADE, subdivisions=1)
k.finish(OUT, ortho_scale=1.2)
