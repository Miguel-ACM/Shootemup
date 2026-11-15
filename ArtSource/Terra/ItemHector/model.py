"""Hector, a pet rock (an OMORI nod): a smooth grey pebble, lighter on top, with two dot
eyes and a small mouth, wrapped in the purple rim of Miguel's reference sprite."""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import item_kit as k  # noqa: E402
from mathutils import Vector  # noqa: E402

OUT = k.output_directory(__file__)
LIGHT, STONE, SHADE, INK, PURPLE, SPECK = range(6)
k.begin([
    (0.80, 0.80, 0.84),  # sunlit crown
    (0.58, 0.58, 0.64),  # stone
    (0.40, 0.40, 0.48),  # underside
    (0.09, 0.08, 0.12),  # eyes and mouth
    (0.63, 0.22, 0.86),  # purple rim
    (0.90, 0.90, 0.93),  # pale speckles
])


def pebble(p):
    # A wide dome with a flattened base, gently lumpy so it reads as stone, not a ball.
    p = Vector((p.x * 1.22, p.y * 0.78, p.z * 0.95))
    angle = math.atan2(p.z, p.x)
    p *= 1 + 0.045 * math.sin(angle * 3 + 0.6) + 0.03 * math.cos(p.y * 9)
    if p.y < -0.17:
        p.y = -0.17 - (p.y + 0.17) * 0.18
    return p + Vector((0, 0.02, 0))


body = k.sphere(0.4, rot=(90, 0, 0), tile=STONE, u=18, v=10)
k.deform(body, pebble)
for face in body:
    y = face.calc_center_median().y
    face.material_index = LIGHT if y > 0.13 else STONE if y > -0.08 else SHADE
# A few pale flecks on the crown, like the light pixels in the sprite.
flecks = [(x, y, k.surface_z(x, y)) for x, y in ((-0.24, 0.17), (0.04, 0.24), (0.27, 0.13))]
features = [(x, y, k.surface_z(x, y)) for x, y in ((-0.16, 0.05), (0.18, 0.03), (0.0, -0.07))]
for x, y, z in flecks:
    k.box((0.05, 0.035, 0.03), loc=(x, y, z - 0.005), rot=(-35, 0, 0), tile=SPECK)
# Two dot eyes, the right one a little lower, and a tiny mouth between them.
for x, y, z in features[:2]:
    k.sphere(0.04, loc=(x, y, z), scale=(1, 1.2, 0.45), tile=INK, u=8, v=6)
x, y, z = features[2]
k.box((0.055, 0.026, 0.03), loc=(x, y, z - 0.004), tile=INK)
k.outline_shell(body, 0.028, PURPLE)
k.finish(OUT, ortho_scale=1.25)
