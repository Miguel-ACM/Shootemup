"""Preview of the level 1 grass field (run in Blender after build_field.py): a playfield-sized
layout rendered top-down at 180x320 like render_preview.py."""
import math
import os
import random
import bpy

exec(open(r"C:\Users\Miguel\Shootemup\ArtSource\PSXv2\render_preview.py", encoding="utf-8").read().split("cam = setup()")[0])
cam = setup()
rng = random.Random(3)
tmp = []


def place(name, x, y, z=0.0, rot=0.0):
    src = bpy.data.objects[name]
    ob = bpy.data.objects.new(name + "_preview", src.data)
    bpy.context.scene.collection.objects.link(ob)
    ob.location = (x, y - 60, z)
    ob.rotation_euler = (0, 0, rot)
    tmp.append(ob)


for gy in (-5, 5, 15):
    place("Field_Ground", 0, gy - 5)
for k in range(4):
    place(f"Field_Puddle{k % 3}", rng.uniform(-4, 4), rng.uniform(-9, 8), 0, rng.uniform(0, 6.28))
for k in range(5):
    place(f"Field_Bush{k % 2}", rng.uniform(-4.5, 4.5), rng.uniform(-9, 8), 0, rng.uniform(0, 6.28))
for k in range(7):
    place(f"Field_Tree{k % 3}", rng.choice((-1, 1)) * rng.uniform(2.2, 5), rng.uniform(-9, 8), 0, rng.uniform(0, 6.28))
for k in range(3):
    place(f"Field_Cloud{k % 2}", rng.uniform(-4, 4), rng.uniform(-8, 8), 6)
place("PSXv2_Player", 0, -6, 3)
bpy.data.objects["PSXv2_Player_preview"].data = bpy.data.objects["PSXv2_Player"].data
cam.data.sensor_fit = "VERTICAL"
sc.world.node_tree.nodes["Background"].inputs[1].default_value = 0.9
shot(cam, "Field", (0, -60.5, 0), 17.7, (180, 320))
cam.data.sensor_fit = "AUTO"
for ob in tmp:
    bpy.data.objects.remove(ob, do_unlink=True)
print("rendered")
