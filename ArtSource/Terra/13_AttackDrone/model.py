import bpy
import math
import os
import sys
from mathutils import Vector

# AttackDrone: a three-spoke urban attack craft.  The gameplay muzzle and
# engine socket are fixed at (0, -.7) and (0, .63), respectively.
def output_dir():
    if '__file__' in globals():
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            p = os.path.abspath(args[0])
            return p if os.path.isdir(p) else os.path.dirname(p)
    return os.getcwd()

OUT = output_dir()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
          (0.80,0.84,0.83)]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.7
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 2.0
    mats.append(m)

parts = []
def mesh(name, verts, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.materials.append(mats[material])
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    parts.append(ob)
    return ob

def plate(name, outline, zbottom, zedge, ztop, inset, material):
    # Three distinct levels give each armor panel a chamfered, faceted rim.
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    n = len(outline)
    inner = [(cx+(x-cx)*inset, cy+(y-cy)*inset) for x,y in outline]
    verts = [(x,y,zbottom) for x,y in outline] + [(x,y,zedge) for x,y in outline] + [(x,y,ztop) for x,y in inner]
    faces = [tuple(reversed(range(n)))]
    for layer in range(2):
        for i in range(n):
            j = (i+1)%n
            faces.append((layer*n+i, layer*n+j, (layer+1)*n+j, (layer+1)*n+i))
    faces.append(tuple(range(2*n,3*n)))
    return mesh(name, verts, faces, material)

def cylinder(name, radius, depth, location, material, vertices=12, rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=location, rotation=rotation)
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mats[material])
    parts.append(ob)
    return ob

def axial_ring(name, x, yface, z, outer, inner, length, direction, material):
    # Annular mouth, axis along Y, with actual recessed opening.
    n = 12
    verts = []
    for yy, radius in ((yface-direction*length,outer), (yface,outer), (yface,inner), (yface-direction*length*.65,inner)):
        for i in range(n):
            a = 2*math.pi*i/n
            verts.append((x+radius*math.cos(a), yy, z+radius*math.sin(a)))
    faces = []
    for k in range(3):
        for i in range(n):
            j=(i+1)%n
            faces.append((k*n+i,k*n+j,(k+1)*n+j,(k+1)*n+i))
    mesh(name,verts,faces,material)
    cylinder(name+' dark bore',inner*.97,.008,(x,yface-direction*length*.62,z),11,12,(math.pi/2,0,0))

def spoke_coords(a, points):
    p=(math.cos(a), math.sin(a)); q=(-p[1],p[0])
    return [(p[0]*r+q[0]*t,p[1]*r+q[1]*t) for r,t in points]

# The three swept arms retain the original radial orientation and extent.
for i in range(3):
    a=math.radians(30+i*120)
    outline=spoke_coords(a,[(.25,-.105),(.68,-.19),(1.03,-.08),(.94,.17),(.54,.16),(.25,.09)])
    plate('swept arm',outline,-.105,.075,.145,.85,0)
    panel=spoke_coords(a,[(.43,-.075),(.70,-.13),(.92,-.065),(.86,.085),(.55,.10)])
    plate('raised steel armor',panel,.126,.149,.177,.88,4)
    # Wide, painted identification stripe, following the spoke across its width.
    stripe=spoke_coords(a,[(.69,-.124),(.76,-.108),(.76,.095),(.69,.105)])
    plate('pale painted stripe',stripe,.179,.181,.185,.98,1)
    tip=spoke_coords(a,[(.89,-.065),(.98,-.045),(.92,.105),(.86,.09)])
    plate('dark tip cap',tip,.09,.142,.155,.92,11)

# Central stacked, faceted ring and raised armored motor housing.
cylinder('lower hub',.35,.18,(0,0,0),11,12)
cylinder('steel collar',.34,.12,(0,0,.11),6,12)
cylinder('slate upper housing',.275,.13,(0,0,.19),0,12)
cylinder('recessed top',.205,.025,(0,0,.266),11,12)
cylinder('motor crown',.15,.052,(0,0,.29),4,10)
cylinder('central sensor',.065,.018,(0,0,.325),9,8)
# A small forward sight line makes the direction readable from directly above.
plate('nose marking',[(-.052,-.42),(.052,-.42),(.042,-.56),(-.042,-.56)],.155,.165,.171,.94,3)
# Barrel lip is centered precisely on the gameplay muzzle in XY.
axial_ring('forward muzzle',0,-.7,.16,.082,.044,.13,-1,6)
# Rear socket mouth is centered on the fixed socket; its opening is unlit.
axial_ring('rear engine socket',0,.63,0,.115,.073,.16,1,6)

# Consolidate before export; all parts have flat-shaded polygons.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
for poly in model.data.polygons:
    poly.use_smooth = False
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Export only the mesh, before adding presentation objects.
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue-gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (.05,.07,.09,1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(ob, target):
    ob.rotation_euler = (Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = location
    aim(ob,target)
    return ob

sun=light('Sun','SUN',(-3,-4,6),2.0,(0,0,0))
sun.data.angle = math.radians(12)
light('Soft fill','AREA',(3,2,4),180,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,5)
cam.rotation_euler=(0,0,0)  # camera local -Z looks down; local +Y is screen up
camdata.type='ORTHO'
camdata.ortho_scale=2.45
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'
camdata.lens=50
cam.location=(3.1,-4.3,3.5)
aim(cam,(0,-.05,.08))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
