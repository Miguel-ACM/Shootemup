"""Golden Crown: a gold band rising to five pearl-tipped points around a red velvet cap,
set with rubies and sapphires."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
GOLD, GOLD_DARK, VELVET, RUBY, SAPPHIRE, PEARL = range(6)
k.begin([(1.0, 0.8, 0.26), (0.75, 0.53, 0.14), (0.68, 0.1, 0.14), (0.9, 0.12, 0.18), (0.2, 0.42, 0.9), (0.97, 0.95, 0.9)])

N = 20
lower = [(math.cos(2 * math.pi * j / N) * 0.3, -0.15, math.sin(2 * math.pi * j / N) * 0.3) for j in range(N)]
upper = [(math.cos(2 * math.pi * j / N) * 0.32, 0.18 if j % 4 == 0 else 0.04, math.sin(2 * math.pi * j / N) * 0.32) for j in range(N)]
k.loft([lower, upper], tile=GOLD, cap=False)
k.loft([[(x * 0.9, y, z * 0.9) for x, y, z in upper], [(x * 0.9, y, z * 0.9) for x, y, z in lower]], tile=GOLD_DARK, cap=False)
k.sphere(0.27, scale=(1, 0.7, 1), tile=VELVET, u=14, v=8)
k.torus(0.31, 0.03, loc=(0, -0.15, 0), rot=(90, 0, 0), tile=GOLD_DARK, ring=20, tube=5)
for j in range(N):
    a = 2 * math.pi * j / N
    if j % 4 == 0:
        k.sphere(0.035, loc=(math.cos(a) * 0.32, 0.2, math.sin(a) * 0.32), tile=PEARL, u=8, v=5)
    if j % 4 == 2:
        k.ico(0.04, loc=(math.cos(a) * 0.33, -0.06, math.sin(a) * 0.33), tile=RUBY if j % 8 == 2 else SAPPHIRE, subdivisions=1)
k.sphere(0.04, loc=(0, 0.2, 0), tile=GOLD, u=8, v=5)
k.finish(OUT, ortho_scale=1.15)
