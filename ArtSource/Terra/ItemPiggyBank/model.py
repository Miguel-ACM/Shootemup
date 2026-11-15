"""Piggy Bank: a round pink pig in profile with a snout, pointed ears, stubby legs, a curly
tail and a gold coin half pushed into the slot on its back."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
PINK, DEEP, SNOUT, DARK, GOLD, GOLD_DARK = range(6)
k.begin([
    (0.97, 0.60, 0.69),
    (0.83, 0.40, 0.52),
    (1.00, 0.74, 0.80),
    (0.16, 0.08, 0.11),
    (1.00, 0.79, 0.22),
    (0.80, 0.55, 0.10),
])

k.sphere(0.3, loc=(0, 0, 0), scale=(1.25, 0.92, 0.88), tile=PINK, u=16, v=10)
# Snout with two nostrils.
k.cylinder(0.11, 0.1, 0.1, loc=(0.4, -0.02, 0), rot=(0, 90, 0), tile=SNOUT, segments=12)
for z in (-0.04, 0.04):
    k.cylinder(0.022, 0.022, 0.02, loc=(0.455, -0.02, z), rot=(0, 90, 0), tile=DEEP, segments=6)
# Eyes on both sides.
for z in (-0.19, 0.19):
    k.sphere(0.03, loc=(0.25, 0.08, z), tile=DARK, u=8, v=5)
# Pointed ears.
for z in (-0.12, 0.12):
    k.cylinder(0.075, 0.0, 0.14, loc=(0.2, 0.26, z), rot=(-90, 25, 0), tile=DEEP, segments=4)
# Stubby legs.
for x in (-0.17, 0.17):
    for z in (-0.13, 0.13):
        k.cylinder(0.065, 0.06, 0.13, loc=(x, -0.26, z), rot=(90, 0, 0), tile=DEEP, segments=8)
# Curly tail.
k.torus(0.045, 0.016, loc=(-0.39, 0.06, 0), rot=(0, 90, 0), tile=DEEP, ring=10, tube=5, arc=300)
# Coin slot and a gold coin half inside it.
k.box((0.16, 0.03, 0.05), loc=(0, 0.275, 0), tile=DARK)
k.cylinder(0.1, 0.1, 0.03, loc=(0, 0.33, 0), tile=GOLD, segments=14)
k.cylinder(0.065, 0.065, 0.034, loc=(0, 0.33, 0), tile=GOLD_DARK, segments=14)
k.finish(OUT, ortho_scale=1.2)
