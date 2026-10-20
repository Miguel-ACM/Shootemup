"""Level 1 grass field kit (Blender). Loads build_ships.py for its Builder/material helpers, then
builds puddles, trees and bushes into
Assets/Art/PSXv2/PSXv2_Field.fbx (one object per piece, named Field_*). PSXGrassField scrolls
and recycles them in game. Same conventions: 1 unit = 1 world unit, +Z toward the camera.
"""
import math
import os
import random
import bpy

PSXV2_LIBRARY = True
exec(open(r"C:\Users\Miguel\Shootemup\ArtSource\PSXv2\build_ships.py", encoding="utf-8").read())

rng = random.Random(11)
GROUND_W, GROUND_H = 12, 10


def ground(b):
    """12 x 10 one-unit cells, each a full 16px grass tile, picked at random (flowers, tufts)."""
    bm = b.bm
    for gy in range(GROUND_H):
        for gx in range(GROUND_W):
            x0, y0 = gx - GROUND_W / 2, gy - GROUND_H / 2
            vs = [bm.verts.new((x0, y0, 0)), bm.verts.new((x0 + 1, y0, 0)), bm.verts.new((x0 + 1, y0 + 1, 0)), bm.verts.new((x0, y0 + 1, 0))]
            r = rng.random()
            b._tag(bm.faces.new(vs), "grass" if r < .62 else "tufts" if r < .84 else "flowers")


def blob(n, radius, wobble):
    return [(math.cos(i * TAU / n) * radius * rng.uniform(1 - wobble, 1 + wobble),
             math.sin(i * TAU / n) * radius * rng.uniform(1 - wobble, 1 + wobble)) for i in range(n)]


def puddle(b, size):
    rim = blob(10, size, .25)
    b.prism(rim, 0.0, 0.02, "dirt", "dirt")
    b.prism([(x * .8, y * .8) for x, y in rim], 0.02, 0.035, "water", "water")


def tree(b, size, dark):
    """Low-poly tree seen from above: short trunk and a lumpy canopy of three or four spheres."""
    b.tube((0, 0, 0), (0, 0, size * .7), size * .12, 6, "bark")
    main, other = ("darkleaves", "leaves") if dark else ("leaves", "darkleaves")
    b.sphere((0, 0, size * .9), size * .55, main, seg=8, rings=5, scale=(1, 1, .75))
    for k in range(rng.choice((2, 3))):
        a = rng.uniform(0, TAU)
        b.sphere((math.cos(a) * size * .38, math.sin(a) * size * .38, size * .8), size * rng.uniform(.3, .4),
                 other if k == 0 else main, seg=7, rings=4, scale=(1, 1, .75))


def bush(b, size):
    for k in range(2):
        b.sphere((k * size * .5 - size * .25, rng.uniform(-.1, .1) * size, size * .25), size * .4, "leaves" if k else "darkleaves",
                 seg=6, rings=4, scale=(1, 1, .7))


def cloud(b, size):
    """Small flat cumulus puff cluster (white, softly shaded by the ceramic tile)."""
    for k in range(rng.choice((3, 4, 5))):
        b.sphere((rng.uniform(-.6, .6) * size, rng.uniform(-.3, .3) * size, 0), size * rng.uniform(.35, .55), "ceramic",
                 seg=7, rings=4, scale=(1, .8, .35))


def build():
    for o in list(bpy.data.objects):
        if o.name.startswith("Field_") or o.name == "PSXv2_Field":
            bpy.data.objects.remove(o, do_unlink=True)
    root = bpy.data.objects.new("PSXv2_Field", None)
    bpy.context.scene.collection.objects.link(root)
    # The ground is a tiled grass texture and clouds are sprites (PSXGrassField builds both).
    pieces = [(f"Field_Puddle{i}", lambda b, s=s: puddle(b, s)) for i, s in enumerate((0.55, 0.8, 1.1))]
    pieces += [(f"Field_Tree{i}", lambda b, s=s, d=d: tree(b, s, d)) for i, (s, d) in enumerate(((1.3, False), (1.7, True), (2.2, False)))]
    pieces += [(f"Field_Bush{i}", lambda b, s=s: bush(b, s)) for i, s in enumerate((0.6, 0.9))]
    report = {}
    for name, fn in pieces:
        b = Builder(closed=False)
        fn(b)
        ob = b.finish(name)
        ob.data.materials.append(material("PSXv2_Steel"))
        ob.parent = root
        report[name] = tris([ob])
    export(root, "PSXv2_Field")
    # Preview layout: spread the pieces out next to the ground.
    for k, ob in enumerate(root.children):
        ob.location = (8 + (k % 4) * 2.6, -3 + (k // 4) * 2.6, 0)
    root.location = (0, -30, 0)
    return report


print(build())
