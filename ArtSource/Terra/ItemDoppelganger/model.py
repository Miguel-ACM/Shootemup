"""Doppelganger: an ornate hand mirror in a gold frame, its pane a deep violet with pale
streaks of reflection, on a turned handle."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
GOLD, DARK_GOLD, GLASS, SHINE, GEM = range(5)
k.begin([(0.95, 0.75, 0.3), (0.62, 0.44, 0.16), (0.36, 0.3, 0.6), (0.82, 0.86, 1.0), (0.55, 0.2, 0.75)])

k.torus(0.25, 0.045, loc=(0, 0.12, 0), scale=(0.88, 1.0, 1.0), tile=GOLD, ring=24, tube=6)
k.cylinder(0.25, 0.25, 0.03, loc=(0, 0.12, 0), scale=(0.88, 1, 1), tile=DARK_GOLD, segments=24)
k.cylinder(0.22, 0.22, 0.012, loc=(0, 0.12, 0.022), scale=(0.88, 1, 1), tile=GLASS, segments=24)
k.box((0.05, 0.22, 0.01), loc=(-0.06, 0.16, 0.03), rot=(0, 0, -30), tile=SHINE)
k.box((0.022, 0.1, 0.01), loc=(0.02, 0.21, 0.03), rot=(0, 0, -30), tile=SHINE)
for a in (90, 210, 330):
    r = math.radians(a)
    k.ico(0.035, loc=(math.cos(r) * 0.22, 0.12 + math.sin(r) * 0.25, 0.04), tile=GEM, subdivisions=1)
k.cylinder(0.05, 0.035, 0.07, loc=(0, -0.16, 0), rot=(90, 0, 0), tile=GOLD, segments=8)
k.cylinder(0.035, 0.04, 0.22, loc=(0, -0.3, 0), rot=(90, 0, 0), tile=DARK_GOLD, segments=8)
k.sphere(0.05, loc=(0, -0.43, 0), tile=GOLD, u=8, v=6)
k.finish(OUT, ortho_scale=1.15)
