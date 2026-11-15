"""Event Horizon: a tiny black hole. A black sphere wrapped by a tilted accretion disk that
burns from violet at its edge to white-hot gold near the hole, with a thin photon ring."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
VOID, OUTER, MID, INNER, RING = range(5)
k.begin([(0.04, 0.03, 0.07), (0.45, 0.22, 0.7), (0.95, 0.5, 0.25), (1.0, 0.86, 0.5), (0.95, 0.95, 1.0)])

k.sphere(0.17, tile=VOID, u=16, v=10)
for major, minor, tile in ((0.4, 0.035, OUTER), (0.32, 0.04, MID), (0.245, 0.035, INNER)):
    k.torus(major, minor, rot=(72, 0, 12), scale=(1, 1, 0.45), tile=tile, ring=28, tube=5)
k.torus(0.19, 0.012, tile=RING, ring=24, tube=4)
k.finish(OUT, ortho_scale=1.1)
