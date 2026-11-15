"""Second Wind: a paper pinwheel on a stick. Four curled blades in teal, white, coral and
sunflower yellow around a gold pin."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
TEAL, WHITE, CORAL, YELLOW, STICK, PIN = range(6)
k.begin([
    (0.24, 0.74, 0.80),
    (0.95, 0.95, 0.93),
    (0.95, 0.45, 0.36),
    (0.98, 0.80, 0.28),
    (0.92, 0.86, 0.74),
    (0.95, 0.74, 0.30),
])

CENTRE = (0.0, 0.12, 0.0)
# Each blade runs from the hub out to a corner and curls toward the viewer at its tip.
for i, tile in enumerate((TEAL, WHITE, CORAL, YELLOW)):
    a = math.radians(45 + 90 * i)
    b = a + math.radians(90)
    tip = (math.cos(a) * 0.36, math.sin(a) * 0.36)
    side = (math.cos(b) * 0.04, math.sin(b) * 0.04)
    rows = []
    for j in range(5):
        t = j / 4
        x = tip[0] * t
        y = tip[1] * t
        width = 0.06 + 0.18 * t * (1 - 0.6 * t)
        curl = 0.12 * t * t
        bx, by = math.cos(b) * width, math.sin(b) * width
        rows.append([(CENTRE[0] + x + side[0] * 0.2, CENTRE[1] + y + side[1] * 0.2, 0.012 + curl * 0.2),
                     (CENTRE[0] + x + bx, CENTRE[1] + y + by, curl),
                     (CENTRE[0] + x + bx, CENTRE[1] + y + by, curl - 0.02),
                     (CENTRE[0] + x + side[0] * 0.2, CENTRE[1] + y + side[1] * 0.2, -0.008 + curl * 0.2)])
    k.loft(rows, tile=tile)
k.sphere(0.045, loc=(CENTRE[0], CENTRE[1], 0.03), scale=(1, 1, 0.8), tile=PIN, u=10, v=6)
# Stick and a pin through the back.
k.cylinder(0.022, 0.022, 0.6, loc=(0, -0.2, -0.04), rot=(90, 0, 0), tile=STICK, segments=8)
k.cylinder(0.012, 0.012, 0.09, loc=(0, 0.12, -0.02), tile=PIN, segments=6)
k.finish(OUT, ortho_scale=1.2)
