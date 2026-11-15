"""Loan Shark: an actual shark in profile, slate blue above and white below, with its fins,
gill slits, a beady eye and a gold coin clamped in its toothy grin."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
SKIN, BELLY, FIN, EYE, MOUTH, TOOTH, GOLD, GOLD_DARK = range(8)
k.begin([
    (0.44, 0.55, 0.66),
    (0.93, 0.94, 0.95),
    (0.33, 0.43, 0.55),
    (0.06, 0.06, 0.08),
    (0.45, 0.12, 0.15),
    (0.98, 0.98, 0.96),
    (1.00, 0.79, 0.22),
    (0.80, 0.55, 0.10),
])

# Body lofted nose (+X) to tail along elliptical sections.
SECTIONS = 10
STATIONS = [(0.44, 0.012, 0.01), (0.4, 0.07, 0.06), (0.32, 0.11, 0.1), (0.2, 0.135, 0.12), (0.06, 0.14, 0.125),
            (-0.08, 0.125, 0.11), (-0.2, 0.095, 0.08), (-0.3, 0.06, 0.05), (-0.36, 0.035, 0.03)]
rings = []
for x, ry, rz in STATIONS:
    rings.append([(x, -0.01 + math.cos(2 * math.pi * j / SECTIONS) * ry, math.sin(2 * math.pi * j / SECTIONS) * rz)
                  for j in range(SECTIONS)])
body = k.loft(rings, tile=SKIN)
for f in body:
    if f.is_valid and f.calc_center_median().y < -0.04:
        f.material_index = BELLY
# Dorsal fin, tail and pectoral fins.
k.prism([(0.08, 0.11), (-0.1, 0.11), (-0.06, 0.3)], 0.04, tile=FIN)
k.prism([(-0.33, 0.0), (-0.47, 0.22), (-0.43, 0.02), (-0.46, -0.15)], 0.04, tile=FIN)
for z in (-0.1, 0.1):
    k.prism([(0.12, -0.06), (0.0, -0.08), (-0.02, -0.2)], 0.025, loc=(0, 0, z), rot=(z * 250, 0, 0), tile=FIN)
# Eyes and gill slits on both sides.
for z in (-1, 1):
    k.sphere(0.022, loc=(0.29, 0.035, z * 0.083), tile=EYE, u=6, v=4)
    for i in range(3):
        k.box((0.012, 0.09, 0.01), loc=(0.17 - i * 0.035, -0.01, z * 0.118), tile=FIN)
# Toothy grin under the snout, gripping a gold coin.
k.box((0.14, 0.035, 0.15), loc=(0.33, -0.065, 0), rot=(0, 0, -12), tile=MOUTH)
for i in range(4):
    k.box((0.018, 0.03, 0.018), loc=(0.29 + i * 0.03, -0.055 + i * 0.006, 0.07), rot=(0, 0, -12), tile=TOOTH)
    k.box((0.018, 0.03, 0.018), loc=(0.29 + i * 0.03, -0.055 + i * 0.006, -0.07), rot=(0, 0, -12), tile=TOOTH)
k.cylinder(0.075, 0.075, 0.025, loc=(0.37, -0.11, 0), rot=(0, 0, -12), tile=GOLD, segments=14)
k.cylinder(0.048, 0.048, 0.028, loc=(0.37, -0.11, 0), rot=(0, 0, -12), tile=GOLD_DARK, segments=14)
k.finish(OUT, ortho_scale=1.15)
