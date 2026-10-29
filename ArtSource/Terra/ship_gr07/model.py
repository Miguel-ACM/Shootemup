import bpy
import bmesh
import math
import sys
import json
from pathlib import Path
from mathutils import Vector

# Run with: blender --background --python model.py [-- directory]
try:
    ROOT = Path(__file__).resolve().parent
except NameError:
    args = sys.argv
    ROOT = Path(args[args.index('--') + 1]).resolve() if '--' in args and len(args) > args.index('--') + 1 else Path.cwd()
ROOT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.43, 0.46, 0.46, 1), (0.57, 0.59, 0.58, 1),
    (0.30, 0.33, 0.33, 1), (0.68, 0.69, 0.65, 1),
    (0.13, 0.18, 0.20, 1), (0.22, 0.27, 0.29, 1),
    (0.36, 0.39, 0.39, 1), (0.075, 0.085, 0.085, 1),
    (0.60, 0.53, 0.29, 1), (0.12, 0.13, 0.12, 1),
    (0.48, 0.51, 0.50, 1), (0.25, 0.28, 0.28, 1),
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.78
    bsdf.inputs['Metallic'].default_value = 0.12
    mats.append(mat)

objects = []
def mesh(name, verts, faces, indices):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    for mat in mats:
        data.materials.append(mat)
    for poly, index in zip(data.polygons, indices):
        poly.material_index = index
        poly.use_smooth = False
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(data)
    bm.free()
    objects.append(ob)
    return ob

def layered(name, outline, rings, top=0, side=6, bottom=11):
    # Each ring is (scale around centroid, height); creates substantial bevels.
    n = len(outline)
    cx = sum(x for x, y in outline) / n
    cy = sum(y for x, y in outline) / n
    verts = [(cx + (x-cx)*scale, cy + (y-cy)*scale, z)
             for scale, z in rings for x, y in outline]
    faces = [tuple(reversed(range(n)))]
    indices = [bottom]
    for r in range(len(rings)-1):
        for i in range(n):
            j = (i+1) % n
            faces.append((r*n+i, r*n+j, (r+1)*n+j, (r+1)*n+i))
            indices.append(side if r < len(rings)-2 else 10)
    faces.append(tuple(range((len(rings)-1)*n, len(rings)*n)))
    indices.append(top)
    return mesh(name, verts, faces, indices)

def box(name, x, y, z, sx, sy, sz, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z))
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = (sx, sy, sz)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[material])
    if bevel:
        mod = ob.modifiers.new('Edge chamfers', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    objects.append(ob)
    return ob

def tube(name, x, y_front, y_back, z, outer, inner, mat_outer=2, sides=8):
    # Front is the open, visible lip; the inner wall and recessed back close the bore.
    verts = []
    for y, radius in [(y_front, outer), (y_front, inner), (y_back, inner), (y_back, outer)]:
        for i in range(sides):
            a = 2*math.pi*i/sides + math.pi/8
            verts.append((x + radius*math.cos(a), y, z + radius*math.sin(a)))
    faces, ids = [], []
    for i in range(sides):
        j = (i+1) % sides
        faces.extend([(i,j,sides+j,sides+i), (sides+i,sides+j,2*sides+j,2*sides+i),
                      (3*sides+i,3*sides+j,j,i)])
        ids.extend([mat_outer, 7, mat_outer])
    faces.append(tuple(range(2*sides, 3*sides)))
    ids.append(7)
    return mesh(name, verts, faces, ids)

# Core outline closely follows the original, scaled to the requested footprint.
layered('Central hull', [(-.078,-.237),(.078,-.237),(.130,-.126),(.103,.120),(.050,.300),(-.050,.300),(-.103,.120),(-.130,-.126)],
        [(0.88,-.055),(1,-.035),(1,.020),(.86,.045)], top=1, side=6)
layered('Dorsal armour', [(-.053,-.195),(.053,-.195),(.067,.017),(.042,.187),(-.042,.187),(-.067,.017)],
        [(.94,.042),(1,.052),(.83,.071)], top=10, side=1)
# Dark canopy frame and faceted glass: narrower toward the nose.
layered('Canopy surround', [(-.043,-.015),(.043,-.015),(.033,.145),(-.033,.145)],
        [(1,.068),(.86,.080)], top=7, side=2)
layered('Canopy glazing', [(-.033,-.006),(.033,-.006),(.026,.136),(-.026,.136)],
        [(1,.080),(.76,.099)], top=4, side=5)
box('Canopy forward glint', 0,.111,.100,.040,.003,.0015,3)
box('Canopy rear sill', 0,-.009,.083,.064,.006,.004,2, .001)
# Rear dorsal intake, recessed dark bed and transverse metal louvers.
box('Central radiator recess',0,-.145,.070,.064,.087,.004,7,.002)
for y in [-.176,-.165,-.154,-.143,-.132,-.121,-.110]:
    box('Radiator louver',0,y,.074,.057,.0025,.003,6,.0005)
box('Rear centre keel',0,-.210,-.055,.015,.050,.012,2,.002)

for s, label in [(-1,'L'),(1,'R')]:
    # Mirrored outlines preserve the silhouette of the reference.
    layered('Swept wing '+label, [(s*.084,.090),(s*.145,.021),(s*.310,-.147),(s*.273,-.216),(s*.078,-.158)],
            [(.94,-.036),(1,-.021),(1,.008),(.96,.016)], top=10, side=6)
    layered('Engine pod '+label, [(s*.151,-.225),(s*.231,-.225),(s*.256,-.204),(s*.256,.107),(s*.206,.174),(s*.151,.116)],
            [(.90,-.054),(1,-.036),(1,.034),(.88,.056)], top=0, side=6)
    # Long upper panel and inset vent bed, with a lip surrounding the dark recess.
    layered('Pod forward armour '+label, [(s*.165,.032),(s*.242,.032),(s*.242,.105),(s*.205,.153),(s*.165,.108)],
            [(1,.056),(.94,.061)], top=1, side=6)
    box('Vent frame '+label,s*.200,-.091,.058,.069,.125,.006,2,.002)
    box('Vent recess '+label,s*.200,-.091,.062,.057,.113,.002,7,.001)
    for y in [-.137,-.122,-.107,-.092,-.077,-.062,-.047]:
        box('Vent louver '+label,s*.200,y,.064,.052,.003,.003,6,.0006)
    # A short raised central rail echoes the original's lengthwise pod details.
    box('Pod rear rail '+label,s*.200,-.184,.061,.013,.071,.010,10,.002)
    # Hazard plate: a yellow rectangle crossed by four diagonal dark stripes.
    box('Warning plate '+label,s*.200,.080,.064,.062,.033,.002,8,.001)
    # Clip each diagonal stripe to the small rectangular patch in XY.
    xmin,xmax = s*.200-.030,s*.200+.030
    ymin,ymax = .0645,.0955
    for offset in [-.030,-.014,.002,.018,.034]:
        # x-y = c, clipping a strip to the patch via polygon clipping.
        c = s*.200 - .080 + offset
        poly = [(xmin,ymin),(xmax,ymin),(xmax,ymax),(xmin,ymax)]
        for limit, keep_above in [(c-.002,True),(c+.002,False)]:
            out=[]
            for a,b in zip(poly,poly[1:]+poly[:1]):
                va=a[0]-a[1]-limit; vb=b[0]-b[1]-limit
                ina=va>=0 if keep_above else va<=0
                inb=vb>=0 if keep_above else vb<=0
                if ina: out.append(a)
                if ina != inb:
                    t=va/(va-vb)
                    out.append((a[0]+t*(b[0]-a[0]),a[1]+t*(b[1]-a[1])))
            poly=out
            if not poly: break
        if len(poly)>=3:
            mesh('Hazard stripe '+label, [(x,y,.0652) for x,y in poly], [tuple(range(len(poly)))], [9])
    # The original barrels are dark and sit beside the nose. Their front lip is exactly y=.30.
    box('Gun housing '+label,s*.113,.185,.004,.025,.175,.025,2,.003)
    box('Barrel '+label,s*.113,.247,.004,.017,.096,.017,11,.002)
    tube('Muzzle '+label,s*.113,.300,.286,.004,.011,.006,2)
    # The rear-facing mouth is at the requested point; interior extends forward.
    tube('Engine bell '+label,s*.200,-.245,-.226,-.002,.032,.022,2)
    tube('Nozzle inner lip '+label,s*.200,-.246,-.238,-.002,.023,.017,6)
    # Small dark belly panel keeps the underside readable during rolls.
    box('Underside pod panel '+label,s*.200,-.082,-.055,.057,.190,.006,11,.002)

# Join and triangulate, with transforms and bevel modifiers already applied.
bpy.ops.object.select_all(action='DESELECT')
for ob in objects:
    ob.select_set(True)
bpy.context.view_layer.objects.active = objects[0]
bpy.ops.object.join()
ship = bpy.context.object
ship.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bm = bmesh.new()
bm.from_mesh(ship.data)
bmesh.ops.triangulate(bm, faces=list(bm.faces))
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bm.to_mesh(ship.data)
bm.free()
ship.data.update()
for p in ship.data.polygons:
    p.use_smooth = False
count = len(ship.data.polygons)
assert 600 <= count <= 3000, 'Triangle count outside budget: %d' % count
(ROOT/'parts.json').write_text('[]\n', encoding='utf-8')
bpy.ops.wm.obj_export(filepath=str(ROOT/'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.view_settings.view_transform = 'Standard'
world = bpy.data.worlds.new('Studio')
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1
scene.world = world

def point_at(ob, target):
    ob.rotation_euler = (Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    ob.location = location
    point_at(ob, target)

light('Sun', 'SUN', (-3,4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (2,-1,3), 120, (0,0,0), 4)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera',cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,2)
cam.rotation_euler = (0,0,0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = .73
scene.render.filepath = str(ROOT/'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (.78,.93,.83)
point_at(cam,(0,.01,0))
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = str(ROOT/'angle.png')
bpy.ops.render.render(write_still=True)
print('GR-07 triangles:', count)


