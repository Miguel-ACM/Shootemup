"""Midas Touch: a solid gold hand raised palm out, fingers together and thumb apart, a ruby
ring on one finger and a few gold coins tumbling at the wrist."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
GOLD, GOLD_DARK, SHINE, RUBY = range(4)
k.begin([(1.0, 0.8, 0.26), (0.76, 0.54, 0.14), (1.0, 0.94, 0.62), (0.88, 0.12, 0.16)])

k.box((0.3, 0.3, 0.12), loc=(0, -0.04, 0), tile=GOLD, bevel=0.03)
for x, length in ((-0.105, 0.2), (-0.035, 0.24), (0.035, 0.23), (0.105, 0.19)):
    k.cylinder(0.034, 0.03, length, loc=(x, 0.11 + length / 2, 0), rot=(90, 0, 0), tile=GOLD, segments=8)
    k.sphere(0.03, loc=(x, 0.11 + length, 0), tile=GOLD, u=6, v=4)
# The thumb leans up and away from the palm, not across it.
k.cylinder(0.04, 0.034, 0.17, loc=(-0.2, -0.02, 0.02), rot=(90, 0, 40), tile=GOLD, segments=8)
k.sphere(0.034, loc=(-0.2 - 0.085 * math.sin(math.radians(40)), -0.02 + 0.085 * math.cos(math.radians(40)), 0.02), tile=GOLD, u=6, v=4)
k.torus(0.04, 0.012, loc=(0.035, 0.2, 0), rot=(90, 0, 0), tile=SHINE, ring=10, tube=4)
k.ico(0.018, loc=(0.035, 0.2, 0.045), tile=RUBY, subdivisions=1)
k.cylinder(0.11, 0.12, 0.14, loc=(0, -0.26, 0), rot=(90, 0, 0), scale=(1.15, 1, 0.8), tile=GOLD_DARK, segments=10)
for x, y, a in ((-0.2, -0.36, 20), (0.18, -0.38, -15), (0.02, -0.42, 70)):
    k.cylinder(0.065, 0.065, 0.02, loc=(x, y, 0.08), rot=(a, 0, 0), tile=SHINE, segments=12)
k.finish(OUT, ortho_scale=1.15)
