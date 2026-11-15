"""Echo Chamber: a conch shell, cream and coral with a glossy pink lip and a spire winding
up to a point: the shell you hold to your ear to hear the sea echo."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402

OUT = k.output_directory(__file__)
CREAM, CORAL, PINK, BAND = range(4)
k.begin([(0.96, 0.88, 0.74), (0.93, 0.56, 0.42), (0.98, 0.66, 0.68), (0.72, 0.42, 0.32)])

# The body: a twisted cone whose sections shrink and turn as they climb.
rings = []
STEPS, SIDES = 12, 10
for i in range(STEPS + 1):
    t = i / STEPS
    radius = 0.3 * (1 - t) ** 1.1 + 0.012
    cx, cy = 0.05 * math.sin(t * 5), -0.3 + 0.72 * t
    ring = []
    for j in range(SIDES):
        a = 2 * math.pi * j / SIDES + t * 5
        bump = 1 + 0.12 * math.cos(a * 3 - t * 10)
        ring.append((cx + math.cos(a) * radius * bump, cy, math.sin(a) * radius * 0.8 * bump))
    rings.append(ring)
body = k.loft(rings, tile=CREAM)
for f in body:
    if f.is_valid and int((f.calc_center_median().y + 0.3) / 0.07) % 3 == 0:
        f.material_index = BAND
# The flared lip and its glossy pink mouth, opening toward the viewer.
k.sphere(0.22, loc=(0.02, -0.18, 0.1), scale=(0.75, 1.0, 0.45), tile=CORAL, u=12, v=8)
k.sphere(0.17, loc=(0.03, -0.17, 0.17), scale=(0.6, 0.85, 0.35), tile=PINK, u=12, v=8)
for i in range(4):
    a = math.radians(30 + i * 40)
    k.cylinder(0.03, 0.0, 0.1, loc=(math.cos(a) * 0.22, -0.02 + math.sin(a) * 0.12, -0.02), rot=(0, 90, math.degrees(a)), tile=CORAL, segments=5)
# Lie the shell on its side, spire up and to the right, the way a conch rests.
turn = math.radians(-50)
for v in k.BM.verts:
    x, y = v.co.x, v.co.y + 0.05
    v.co.x, v.co.y = x * math.cos(turn) - y * math.sin(turn), x * math.sin(turn) + y * math.cos(turn)
k.finish(OUT, ortho_scale=1.15)
