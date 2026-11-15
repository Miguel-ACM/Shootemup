"""The Hive's bee: a round yellow body with black stripes, a dark head, a sting and two
pale wings, nose along +Y (the direction of flight)."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
BEE, STRIPE, WING, STING = range(4)
k.begin([(1.0, 0.8, 0.15), (0.12, 0.1, 0.08), (0.9, 0.95, 1.0), (0.3, 0.25, 0.2)])

k.sphere(0.2, loc=(0, -0.05, 0), scale=(0.85, 1.2, 0.85), tile=BEE, u=10, v=8)
for y in (-0.12, 0.02):
    k.cylinder(0.18, 0.18, 0.06, loc=(0, y, 0), rot=(90, 0, 0), scale=(0.95, 0.95, 1), tile=STRIPE, segments=10)
k.sphere(0.12, loc=(0, 0.22, 0), tile=STRIPE, u=8, v=6)
k.cylinder(0.05, 0.0, 0.1, loc=(0, -0.33, 0), rot=(90, 0, 0), tile=STING, segments=6)
for side in (-1, 1):
    k.sphere(0.16, loc=(side * 0.18, 0.08, 0.1), scale=(1, 0.55, 0.15), rot=(0, 0, side * 25), tile=WING, u=8, v=4)
k.finish(OUT, ortho_scale=1.1)
