"""Build the original GR-07 industrial interceptor in Blender. Run through Blender MCP."""
import bpy
import bmesh
import json
import math
import random
from pathlib import Path
from mathutils import Vector, Quaternion

ROOT = Path('C:/Users/Miguel/Shootemup')
SOURCE = ROOT / 'ArtSource/PlayerShip'
EXPORT = ROOT / 'Assets/Art/PlayerShip'
SOURCE.mkdir(parents=True, exist_ok=True)
EXPORT.mkdir(parents=True, exist_ok=True)

# Work in a dedicated scene: existing Blender work remains available in its scene.
scene = bpy.data.scenes.get('GR07_Studio') or bpy.data.scenes.new('GR07_Studio')
bpy.context.window.scene = scene
for obj in list(scene.objects):
    bpy.data.objects.remove(obj, do_unlink=True)

W = 256
pixels = [0.0] * (W * W * 4)
rng = random.Random(707)

def pixel(x, y, color):
    if 0 <= x < W and 0 <= y < W:
        index = 4 * (y * W + x)
        pixels[index:index + 4] = [color[0] / 255, color[1] / 255, color[2] / 255, 1]

def rect(x, y, w, h, color):
    for j in range(y, y + h):
        for i in range(x, x + w):
            pixel(i, j, color)

def line(x0, y0, x1, y1, color):
    steps = max(abs(x1-x0), abs(y1-y0), 1)
    for k in range(steps+1):
        pixel(round(x0+(x1-x0)*k/steps), round(y0+(y1-y0)*k/steps), color)

BASE = [(127,132,132), (151,155,153), (62,67,67), (137,132,103),
        (42,51,57), (99,103,104), (99,105,106), (45,49,49),
        (143,147,145), (125,131,132), (59,64,64), (77,80,79),
        (116,122,123), (75,82,83), (157,160,153), (92,98,98)]
for tile, base in enumerate(BASE):
    ox, oy = (tile % 4)*64, (tile // 4)*64
    for y in range(64):
        for x in range(64):
            noise = rng.choice([-5, -3, -1, 0, 0, 1, 3])
            shade = -14 if x < 3 or y < 3 else 0
            pixel(ox+x, oy+y, tuple(max(0, min(255,c+noise+shade)) for c in base))
    # Painted panel borders, bevel highlight, fasteners and a few chipped edges.
    for k in [4, 59]:
        line(ox+k,oy+4,ox+k,oy+59,(52,58,58))
        line(ox+4,oy+k,ox+59,oy+k,(52,58,58))
    line(ox+5,oy+58,ox+58,oy+58,(173,177,173))
    for x,y in [(8,8),(55,8),(8,55),(55,55)]:
        rect(ox+x,oy+y,2,2,(37,42,43))
        pixel(ox+x,oy+y+2,(184,188,182))
    for _ in range(25):
        x,y=rng.randint(7,55),rng.randint(7,55)
        line(ox+x,oy+y,ox+min(x+rng.randint(1,3),58),oy+y,(162,166,159))
    if tile in [0,1,6,8,9,12,14]:
        line(ox+31,oy+5,ox+31,oy+58,(62,69,69))
        line(ox+32,oy+5,ox+32,oy+58,(154,158,155))
        rect(ox+9,oy+12,13,4,(45,53,53))
        rect(ox+10,oy+13,9,1,(170,176,168))
    if tile in [2,7,13]:
        for y in range(12,54,6):
            rect(ox+10,oy+y,44,4,(25,30,31))
            rect(ox+11,oy+y+4,42,1,(126,132,126))
    if tile == 3:
        for y in range(7,57):
            for x in range(7,57):
                if ((x+y)//9)%2:
                    pixel(ox+x,oy+y,(40,43,39))
    if tile == 4:
        rect(ox+8,oy+8,48,48,(23,33,39))
        for y in range(12,54):
            for x in range(12,54):
                value=round(37+(y-12)*0.8)
                pixel(ox+x,oy+y,(value-7,value+1,value+5))
        line(ox+14,oy+46,ox+47,oy+46,(125,143,142))
        line(ox+14,oy+45,ox+40,oy+45,(102,118,120))
        rect(ox+11,oy+10,42,3,(8,17,22))
    if tile == 5:
        for inset,col in [(7,(31,34,35)),(12,(61,62,58)),(18,(158,143,104)),(24,(226,211,166))]:
            rect(ox+inset,oy+inset,64-2*inset,64-2*inset,col)
    if tile == 9:
        for offset in [0,1,2]:
            line(ox+14,oy+40+offset,ox+30,oy+50+offset,(205,207,194))
            line(ox+30,oy+50+offset,ox+46,oy+40+offset,(205,207,194))
    if tile == 10:
        for y in range(12,54,8):
            rect(ox+12,oy+y,40,3,(30,35,35))
    if tile == 14:
        # Hand-drawn pixel stencil '07', upright in the atlas.
        digits=['1110111','1010001','1010010','1010010','1110010']
        for y,row in enumerate(reversed(digits)):
            for x,value in enumerate(row):
                if value=='1':
                    rect(ox+17+x*4,oy+26+y*4,3,3,(224,226,210))

image=bpy.data.images.get('GR07_Atlas_256')
if image:
    bpy.data.images.remove(image)
image=bpy.data.images.new('GR07_Atlas_256',width=W,height=W,alpha=False)
image.pixels.foreach_set(pixels)
image.filepath_raw=str(EXPORT/'GR07_Atlas.png')
image.file_format='PNG'
image.save()

material=bpy.data.materials.get('GR07_PixelSteel') or bpy.data.materials.new('GR07_PixelSteel')
material.use_nodes=True
nodes=material.node_tree.nodes
nodes.clear()
output=nodes.new('ShaderNodeOutputMaterial')
bsdf=nodes.new('ShaderNodeBsdfPrincipled')
bsdf.inputs['Roughness'].default_value=0.83
bsdf.inputs['Metallic'].default_value=0.08
tex=nodes.new('ShaderNodeTexImage')
tex.image=image
tex.interpolation='Closest'
material.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
material.node_tree.links.new(bsdf.outputs['BSDF'],output.inputs['Surface'])
tex.location=(-420,80)
bsdf.location=(-100,80)
output.location=(250,80)

parts=[]

def make_mesh(name,verts,faces,tiles):
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    scene.collection.objects.link(obj)
    mesh.materials.append(material)
    bm=bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    uv=mesh.uv_layers.new(name='AtlasUV')
    for poly,tile in zip(mesh.polygons,tiles):
        points=[mesh.vertices[mesh.loops[i].vertex_index].co for i in poly.loop_indices]
        normal=poly.normal
        axis=max(range(3),key=lambda i: abs(normal[i]))
        # Planar islands: no collapsed triangles, with two-pixel tile gutters.
        axes=[i for i in range(3) if i!=axis]
        lo=[min(p[i] for p in points) for i in axes]
        span=[max(p[i] for p in points)-lo[j] for j,i in enumerate(axes)]
        for loop,p in zip(poly.loop_indices,points):
            u=(p[axes[0]]-lo[0])/max(span[0],1e-8)
            v=(p[axes[1]]-lo[1])/max(span[1],1e-8)
            uv.data[loop].uv=((tile%4*64+4+u*55)/256,(tile//4*64+4+v*55)/256)
    for edge in mesh.edges:
        edge.use_seam=True
    obj['atlas_tiles']='Reusable planar trim islands; 4px gutters'
    parts.append(obj)
    return obj

def prism(name,outline,z0,z1,top=0,side=6,bottom=11,inset=1.0):
    n=len(outline)
    cx=sum(p[0] for p in outline)/n
    cy=sum(p[1] for p in outline)/n
    verts=[(x,y,z0) for x,y in outline]+[(cx+(x-cx)*inset,cy+(y-cy)*inset,z1) for x,y in outline]
    faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    tiles=[bottom,top]
    for i in range(n):
        j=(i+1)%n
        faces.append((i,j,j+n,i+n))
        tiles.append(side)
    return make_mesh(name,verts,faces,tiles)

def box(name,x,y,z,sx,sy,sz,top=0,side=6):
    return prism(name,[(x-sx/2,y-sy/2),(x+sx/2,y-sy/2),(x+sx/2,y+sy/2),(x-sx/2,y+sy/2)],z-sz/2,z+sz/2,top,side)

prism('01_MainArmour',[(-.37,-1.13),(.37,-1.13),(.62,-.60),(.49,.57),(.24,1.52),(-.24,1.52),(-.49,.57),(-.62,-.60)],-.19,.19,8,6,inset=.86)
prism('02_DorsalSpine',[(-.25,-.93),(.25,-.93),(.32,.08),(.20,.89),(-.20,.89),(-.32,.08)],.16,.36,14,1,inset=.83)
prism('03_ArmouredCanopy',[(-.19,-.04),(.19,-.04),(.145,.64),(-.145,.64)],.31,.51,4,10,inset=.69)
for s,label in [(-1,'L'),(1,'R')]:
    outline=[(s*.40,.43),(s*.69,.10),(s*1.48,-.70),(s*1.30,-1.03),(s*.37,-.75)]
    prism('04_SweptWing_'+label,outline,-.12,.035,9,11)
    # Chamfered rectangular pods; no smooth cylinders or subdivision.
    outline=[(s*.72,-1.18),(s*1.10,-1.18),(s*1.22,-.97),(s*1.22,.51),(s*.98,.83),(s*.72,.55)]
    prism('05_EnginePod_'+label,outline,-.19,.27,0,6,inset=.88)
    box('06_Cannon_'+label,s*.54,.81,-.04,.15,.89,.13,10,10)
    # Thin swept vertical stabilizers, four triangular faces and three quad faces each.
    x=s*.97
    make_mesh('07_Stabilizer_'+label,[(x-.035,-1.02,.23),(x-.035,-.40,.25),(x-.035,-.91,.73),
                                   (x+.035,-1.02,.23),(x+.035,-.40,.25),(x+.035,-.91,.73)],
              [(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)],[12,12,6,6,6])
    # Recessed square exhaust: rim four quads and a rear glowing throat.
    x=s*.96
    make_mesh('08_RecessedExhaust_'+label,
              [(x-.16,-1.19,-.13),(x+.16,-1.19,-.13),(x+.16,-1.19,.19),(x-.16,-1.19,.19),
               (x-.10,-1.13,-.07),(x+.10,-1.13,-.07),(x+.10,-1.13,.13),(x-.10,-1.13,.13)],
              [(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],[11,11,11,11,5])
    # Flush vents and warning placards use texture detail rather than geometry.
    make_mesh('09_PodVents_'+label,[(s*.80,-.72,.278),(s*1.09,-.72,.278),(s*1.09,-.14,.278),(s*.80,-.14,.278)],[(0,1,2,3)],[7])
    make_mesh('10_Warning_'+label,[(s*.78,.25,.278),(s*1.06,.25,.278),(s*1.06,.42,.278),(s*.78,.42,.278)],[(0,1,2,3)],[3])
    box('11_NoseMuzzle_'+label,s*.54,1.27,-.04,.19,.15,.18,11,10)
box('12_DorsalRadiator',0,-.62,.38,.30,.35,.07,13,2)

# One mesh / one material, with explicit triangles for a reliable Unity budget.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join()
ship=bpy.context.object
ship.name='GR07_IndustrialInterceptor'
mesh=ship.data
bm=bmesh.new()
bm.from_mesh(mesh)
bmesh.ops.triangulate(bm,faces=bm.faces)
bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
bm.to_mesh(mesh)
bm.free()
# Join may duplicate slots even though all parts share the same atlas.
mesh.materials.clear()
mesh.materials.append(material)
for poly in mesh.polygons:
    poly.material_index=0
    poly.use_smooth=False
mesh.calc_loop_triangles()
triangles=len(mesh.loop_triangles)
assert triangles<300, triangles
assert all(len(p.vertices)==3 for p in mesh.polygons)
uv=mesh.uv_layers.active
for tri in mesh.loop_triangles:
    a,b,c=[Vector(uv.data[i].uv) for i in tri.loops]
    assert abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))>1e-9
ship['triangle_count']=triangles
ship['style']='Original PSX industrial interceptor / GR-07'
ship['texture']='256x256 painted pixel atlas; nearest filtering'
ship['forward_axis']='Blender +Y / Unity +Z after FBX conversion'
ship['uv_notes']='All faces unwrapped; deliberate reuse of atlas tiles; no degenerate UVs'

# The FBX has geometry only; the editable Blender studio stays outside Assets.
bpy.ops.export_scene.fbx(filepath=str(EXPORT/'GR07_Interceptor.fbx'),use_selection=True,
                        object_types={'MESH'},axis_forward='-Z',axis_up='Y',
                        apply_unit_scale=True,bake_anim=False,add_leaf_bones=False,
                        use_mesh_modifiers=True,mesh_smooth_type='FACE',path_mode='AUTO')

# UV sheet is a vector document: every triangle can be inspected over the atlas.
svg=['<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024" viewBox="0 0 256 256">',
     '<image href="../../Assets/Art/PlayerShip/GR07_Atlas.png" width="256" height="256"/>',
     '<g fill="none" stroke="#74e0be" stroke-width="0.15" opacity="0.65">']
for p in mesh.polygons:
    coords=[uv.data[i].uv for i in p.loop_indices]
    svg.append('<polygon points="'+' '.join(f'{c.x*256:.3f},{(1-c.y)*256:.3f}' for c in coords)+'"/>')
svg.extend(['</g>','</svg>'])
(SOURCE/'GR07_UVLayout.svg').write_text('\n'.join(svg),encoding='utf-8')

# A restrained studio to inspect the low-poly forms and the pixel atlas.
def point_at(obj,target):
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

camera_data=bpy.data.cameras.new('InspectionCamera')
camera=bpy.data.objects.new('InspectionCamera',camera_data)
scene.collection.objects.link(camera)
camera.location=(4.5,6.2,8.1)
point_at(camera,(0,.1,0))
camera_data.type='ORTHO'
camera_data.ortho_scale=4.6
scene.camera=camera
for name,location,power,size in [('Key',(-3,1,6),600,4),('Fill',(4,3,4),350,3),('Rim',(0,-4,4),650,3)]:
    data=bpy.data.lights.new(name,'AREA')
    data.energy=power
    data.shape='DISK'
    data.size=size
    light=bpy.data.objects.new(name,data)
    scene.collection.objects.link(light)
    light.location=location
    point_at(light,(0,0,0))
world=bpy.data.worlds.new('GR07_CharcoalStudio')
world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.032,.039,.046,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.45
scene.world=world
scene.render.engine='CYCLES'
scene.cycles.samples=24
scene.render.resolution_x=900
scene.render.resolution_y=900
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False
scene.view_settings.view_transform='Standard'
scene.render.filepath=str(SOURCE/'GR07_Preview.png')

for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.shading.type='MATERIAL'
        area.spaces.active.overlay.show_overlays=False
        region=area.spaces.active.region_3d
        region.view_distance=5.7
        region.view_location=(0,.1,.1)
        region.view_rotation=camera.rotation_euler.to_quaternion()
image.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'GR07_Interceptor.blend'))
report={'name':ship.name,'triangles':triangles,'vertices':len(mesh.vertices),
        'materials':1,'texture_size':[256,256],'uv_layers':len(mesh.uv_layers),
        'degenerate_uv_triangles':0,'fbx':str(EXPORT/'GR07_Interceptor.fbx')}
(SOURCE/'GR07_AssetReport.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
