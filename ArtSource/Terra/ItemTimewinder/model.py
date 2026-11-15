"""Timewinder: a brass clockwork rewinder. A toothed gold gear frames a dark dial with a
glowing teal hourglass core, white hands and four teal hour gems; a winding crown sits
on top and two teal blades sweep back like hands of a clock."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
GOLD, SLATE, TEAL, GLOW, WHITE, BRONZE = range(6)
k.begin([
    (0.93, 0.73, 0.32),
    (0.13, 0.18, 0.23),
    (0.18, 0.80, 0.76),
    (0.62, 1.00, 0.94),
    (0.92, 0.95, 0.95),
    (0.60, 0.42, 0.17),
])

# Toothed gear rim.
gear = []
TEETH = 12
for i in range(TEETH * 4):
    a = 2 * math.pi * i / (TEETH * 4)
    r = 0.3 if (i % 4) in (1, 2) else 0.26
    gear.append((math.cos(a) * r, math.sin(a) * r))
k.prism(gear, 0.14, tile=GOLD, side_tile=BRONZE)
# Recessed dark dial on both faces.
for z in (0.072, -0.072):
    k.cylinder(0.22, 0.22, 0.012, loc=(0, 0, z), tile=SLATE, segments=24)
# Teal hourglass core: two cones meeting at the centre, set in the front dial.
k.cylinder(0.1, 0.012, 0.13, loc=(0, 0.065, 0.1), rot=(90, 0, 0), tile=TEAL, segments=6)
k.cylinder(0.012, 0.1, 0.13, loc=(0, -0.065, 0.1), rot=(90, 0, 0), tile=TEAL, segments=6)
k.sphere(0.03, loc=(0, 0, 0.11), tile=GLOW, u=8, v=5)
# Clock hands and four hour gems.
k.box((0.022, 0.17, 0.012), loc=(0.04, 0.08, 0.17), rot=(0, 0, -28), tile=WHITE)
k.box((0.02, 0.12, 0.012), loc=(-0.05, 0.025, 0.17), rot=(0, 0, 70), tile=WHITE)
for i in range(4):
    a = math.radians(90 * i)
    k.ico(0.03, loc=(math.cos(a) * 0.18, math.sin(a) * 0.18, 0.09), tile=GLOW, subdivisions=1)
# Winding crown on top.
k.cylinder(0.035, 0.035, 0.08, loc=(0, 0.33, 0), rot=(90, 0, 0), tile=BRONZE, segments=8)
k.cylinder(0.065, 0.065, 0.045, loc=(0, 0.39, 0), rot=(90, 0, 0), tile=GOLD, segments=10)
# Two teal blades sweeping back from the sides.
blade = [(0.0, 0.05), (0.2, 0.13), (0.27, 0.02), (0.2, -0.09), (0.0, -0.04)]
for side in (-1, 1):
    k.prism([(x * side, y) for x, y in (blade if side > 0 else list(reversed(blade)))], 0.05,
            loc=(side * 0.25, -0.04, -0.02), rot=(0, 0, side * -12), tile=TEAL, side_tile=GLOW)
k.finish(OUT, ortho_scale=1.25)
