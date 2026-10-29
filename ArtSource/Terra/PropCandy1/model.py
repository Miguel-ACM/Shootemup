import bpy
import math
import os
import sys
from mathutils import Vector

# Output is colocated with this script, including when invoked with blender -- ...
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.splitext(OUT)[1]:
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = {
    1: (0.97, 0.85, 0.71), 4: (0.39, 0.17, 0.12),
    5: (0.62, 0.28, 0.18), 7: (0.57, 0.38, 0.79),
    11: (0.13, 0.055, 0.13), 14: (0.51, 0.26, 0.58),
    15: (0.98, 0.96, 0.88),
}
mats = {}
for n, color in palette.items():
    m = bpy.data.materials.new('tile_%02d' % n)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.83
    mats[n] = m

parts = []
def mesh(name, verts, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.materials.append(mats[material])
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    parts.append(obj)
    return obj

def block(name, center, dimensions, material, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Crisp bevels', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

def slab(name, outline, bottom, top, material):
    n = len(outline)
    verts = [(x, y, bottom) for x, y in outline] + [(x, y, top) for x, y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    return mesh(name, verts, faces, material)

def chocolate_piece(cx, cy):
    # Eight-sided inset top gives each molded segment a broad sloping shoulder.
    def ring(w, h, cut, z):
        return [(cx+x, cy+y, z) for x,y in [(-w+cut,-h),(w-cut,-h),(w,-h+cut),(w,h-cut),(w-cut,h),(-w+cut,h),(-w,h-cut),(-w,-h+cut)]]
    verts = ring(.235, .235, .035, .105) + ring(.195, .195, .028, .205)
    faces = [tuple(range(8,16))] + [(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]
    mesh('Molded chocolate', verts, faces, 4)
    # Small inset tablet mark, subdued caramel-toned rather than bright.
    slab('Chocolate stamp', [(cx-.080,cy-.014),(cx+.080,cy-.014),(cx+.080,cy+.014),(cx-.080,cy+.014)], .205, .208, 5)

# Original rectangular bar: 1.08 by 1.65, centered on the origin.
block('Chocolate body', (0,0,0), (1.08,1.65,.20), 4, .035)
for yy in (-.52, 0, .52):
    for xx in (-.26, .26):
        chocolate_piece(xx, yy)

# Lilac paper covers the lower third, with a faceted torn-open foil lip.
# Separate facets give the creased paper a deliberately low-poly appearance.
outline = [(-.57,-.825),(.57,-.825),(.57,-.345),(.40,-.315),(.23,-.355),(.06,-.305),(-.12,-.345),(-.31,-.300),(-.57,-.350)]
slab('Wrapper', outline, .218, .242, 7)
# Dark folded side strips and bottom crimp remain within the original footprint.
slab('Left folded edge', [(-.57,-.825),(-.49,-.825),(-.49,-.337),(-.57,-.350)], .242,.248,14)
slab('Right folded edge', [(.49,-.825),(.57,-.825),(.57,-.345),(.49,-.331)], .242,.248,14)
slab('Bottom seam', [(-.57,-.825),(.57,-.825),(.55,-.780),(-.55,-.780)], .248,.256,14)
# Jagged cream foil is visible along the ripped edge of the package.
slab('Torn foil', [(-.57,-.350),(-.43,-.305),(-.31,-.245),(-.16,-.315),(-.04,-.275),(.08,-.330),(.22,-.250),(.36,-.315),(.48,-.275),(.57,-.345),(.40,-.365),(.23,-.355),(.06,-.355),(-.12,-.365),(-.31,-.345)], .246,.267,15)
# Folded diagonal planes across the wrapper surface, kept dark and restrained.
slab('Paper crease left', [(-.49,-.735),(-.43,-.745),(-.24,-.375),(-.30,-.360)], .249,.251,14)
slab('Paper crease right', [(.36,-.745),(.40,-.745),(.29,-.385),(.24,-.375)], .249,.251,14)
# A small diamond-shaped paper seal is the wrapper's readable center detail.
slab('Seal border', [(0,-.470),(.115,-.585),(0,-.700),(-.115,-.585)], .254,.260,14)
slab('Seal', [(0,-.490),(.095,-.585),(0,-.680),(-.095,-.585)], .260,.264,1)
slab('Seal center', [(0,-.545),(.040,-.585),(0,-.625),(-.040,-.585)], .264,.268,4)
# Short crimp impressions, modeled as flat dark slivers.
for x in (-.42,-.28,-.14,0,.14,.28,.42):
    slab('Crimp', [(x-.012,-.815),(x+.012,-.815),(x+.012,-.789),(x-.012,-.789)], .256,.258,7)

# Consolidate the full prop before exporting; all geometry is flat shaded.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for polygon in model.data.polygons:
    polygon.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05,0.07,0.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

light('Upper left sun', 'SUN', (-3, 4, 6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (3,-2,4), 180, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 5)
point_at(cam, (0,0,0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.95
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (2.1,-2.625,3.0)
point_at(cam, (0,0,.03))
cam_data.type = 'PERSP'
cam_data.lens = 48
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

