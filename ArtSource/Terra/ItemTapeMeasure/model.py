"""Tape Measure: a chunky yellow case with black rubber sides, a chrome hub, a red lock
and a run of marked tape pulled out of the slot with its hook on the end."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
CASE, RUBBER, CHROME, TAPE, INK, RED = range(6)
k.begin([
    (0.98, 0.76, 0.10),
    (0.17, 0.17, 0.19),
    (0.76, 0.79, 0.82),
    (1.00, 0.91, 0.42),
    (0.10, 0.09, 0.08),
    (0.86, 0.17, 0.13),
])


def rounded_square(half, radius, steps=4):
    points = []
    for cx, cy, start in ((half - radius, half - radius, 0), (-half + radius, half - radius, 90),
                          (-half + radius, -half + radius, 180), (half - radius, -half + radius, 270)):
        for i in range(steps + 1):
            a = math.radians(start + 90 * i / steps)
            points.append((cx + math.cos(a) * radius, cy + math.sin(a) * radius))
    return points


X, Y = 0.08, 0.04
# The case: yellow faces over a black rubber band all the way round.
k.prism(rounded_square(0.3, 0.1), 0.26, loc=(X, Y, 0), tile=CASE, side_tile=RUBBER)
k.prism(rounded_square(0.25, 0.08), 0.03, loc=(X, Y, 0.14), tile=CASE, side_tile=CASE)
k.prism(rounded_square(0.25, 0.08), 0.03, loc=(X, Y, -0.14), tile=CASE, side_tile=CASE)
# Chrome hub with a dark screw, on both faces.
for z, sign in ((0.165, 1), (-0.165, -1)):
    k.cylinder(0.12, 0.12, 0.025, loc=(X, Y, z), tile=CHROME, segments=14)
    k.cylinder(0.04, 0.04, 0.02, loc=(X, Y, z + 0.02 * sign), tile=INK, segments=8)
# Red lock on the top edge and a chrome belt clip down the back.
k.box((0.17, 0.07, 0.12), loc=(X - 0.03, Y + 0.33, 0), tile=RED, bevel=0.012)
k.box((0.09, 0.42, 0.025), loc=(X + 0.02, Y, -0.19), tile=CHROME)
# The slot and the tape pulled out to the left, with tick marks and its hook.
k.box((0.06, 0.1, 0.14), loc=(X - 0.31, Y - 0.2, 0), tile=RUBBER)
k.box((0.3, 0.04, 0.11), loc=(X - 0.46, Y - 0.215, 0), rot=(0, 0, 4), tile=TAPE)
for i in range(5):
    x = X - 0.35 - i * 0.05
    k.box((0.014, 0.028 if i % 2 else 0.042, 0.02), loc=(x, Y - 0.2 - 0.004 * i, 0.058), tile=INK)
k.box((0.022, 0.09, 0.12), loc=(X - 0.62, Y - 0.19, 0), tile=CHROME)
k.finish(OUT, ortho_scale=1.2)
