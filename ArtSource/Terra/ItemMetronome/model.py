"""Metronome: a wooden pyramid case with a brass-weighted pendulum swung to one side, a
cream scale plate and a little winding key."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
WOOD, DARK, CREAM, BRASS, INK = range(5)
k.begin([(0.62, 0.36, 0.18), (0.36, 0.2, 0.1), (0.95, 0.9, 0.78), (0.95, 0.76, 0.32), (0.15, 0.1, 0.08)])

k.prism([(-0.27, -0.4), (0.27, -0.4), (0.07, 0.36), (-0.07, 0.36)], 0.24, tile=WOOD, side_tile=DARK)
k.box((0.6, 0.06, 0.28), loc=(0, -0.42, 0), tile=DARK, bevel=0.01)
k.prism([(-0.14, -0.3), (0.14, -0.3), (0.045, 0.27), (-0.045, 0.27)], 0.02, loc=(0, 0, 0.12), tile=CREAM)
for i in range(7):
    k.box((0.05, 0.008, 0.01), loc=(0, -0.24 + i * 0.07, 0.135), tile=INK)
k.box((0.022, 0.62, 0.02), loc=(0.07, 0.02, 0.15), rot=(0, 0, -14), tile=BRASS)
k.prism([(-0.05, -0.04), (0.05, -0.04), (0.035, 0.04), (-0.035, 0.04)], 0.04, loc=(0.1, 0.13, 0.16), rot=(0, 0, -14), tile=BRASS)
k.sphere(0.028, loc=(0, -0.31, 0.15), tile=BRASS, u=8, v=5)
k.cylinder(0.02, 0.02, 0.08, loc=(0.29, -0.2, 0), rot=(0, 90, 0), tile=BRASS, segments=6)
k.box((0.03, 0.09, 0.02), loc=(0.34, -0.2, 0), tile=BRASS)
k.finish(OUT, ortho_scale=1.15)
