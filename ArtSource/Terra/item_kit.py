"""Shared helpers for hand-written item model scripts (Blender 5.x).

Follows brief_items.md: +Y up, +Z toward the camera, about 0.9 units, materials tile_00..tile_15,
one joined mesh named Model, model.obj next to the script and two preview renders.
Usage inside a model.py:  import item_kit as k; k.begin(); ...; k.finish(OUT)
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.bvhtree import BVHTree

BM = None
MATERIALS = []
SHELL = set()


def output_directory(script_file):
    if script_file:
        return os.path.dirname(os.path.abspath(script_file))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            path = os.path.abspath(args[0])
            return path if os.path.isdir(path) else os.path.dirname(path)
    return os.getcwd()


def begin(palette):
    """palette: list of (r, g, b) in 0..1, one per tile, at most sixteen."""
    global BM
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh)
    BM = bmesh.new()
    MATERIALS.clear()
    SHELL.clear()
    for i, color in enumerate(palette):
        mat = bpy.data.materials.new('tile_%02d' % i)
        mat.diffuse_color = (*color, 1.0)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (*color, 1.0)
        bsdf.inputs['Roughness'].default_value = 0.62
        bsdf.inputs['Metallic'].default_value = 0.0
        MATERIALS.append(mat)


def matrix(loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    s = Matrix.Diagonal((*scale, 1))
    return Matrix.Translation(Vector(loc)) @ Euler([math.radians(a) for a in rot]).to_matrix().to_4x4() @ s


def _track(create):
    BM.faces.index_update()
    before = set(BM.faces)
    create()
    faces = [f for f in BM.faces if f not in before]
    return faces


def _finish_part(faces, tile, bevel=0.0, smooth_tiles=None):
    for f in faces:
        f.material_index = tile
    if bevel > 0 and faces:
        edges = {e for f in faces for e in f.edges}
        result = bmesh.ops.bevel(BM, geom=list(edges), offset=bevel, segments=1, affect='EDGES', profile=0.5)
        for f in result.get('faces', []):
            f.material_index = tile
        verts = {v for f in faces if f.is_valid for v in f.verts}
        faces = [f for f in BM.faces if f.is_valid and any(v in verts for v in f.verts)]
    return faces


def box(size, loc=(0, 0, 0), rot=(0, 0, 0), tile=0, bevel=0.0):
    m = matrix(loc, rot, size)
    faces = _track(lambda: bmesh.ops.create_cube(BM, size=1.0, matrix=m))
    return _finish_part(faces, tile, bevel)


def cylinder(r1, r2, depth, loc=(0, 0, 0), rot=(0, 0, 0), tile=0, segments=12, bevel=0.0, scale=(1, 1, 1)):
    """Axis along local Z."""
    m = matrix(loc, rot, scale)
    faces = _track(lambda: bmesh.ops.create_cone(BM, cap_ends=True, cap_tris=False, segments=segments,
                                                 radius1=r1, radius2=r2, depth=depth, matrix=m))
    return _finish_part(faces, tile, bevel)


def sphere(radius, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1), tile=0, u=12, v=8):
    m = matrix(loc, rot, scale)
    faces = _track(lambda: bmesh.ops.create_uvsphere(BM, u_segments=u, v_segments=v, radius=radius, matrix=m))
    return _finish_part(faces, tile)


def ico(radius, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1), tile=0, subdivisions=1):
    m = matrix(loc, rot, scale)
    faces = _track(lambda: bmesh.ops.create_icosphere(BM, subdivisions=subdivisions, radius=radius, matrix=m))
    return _finish_part(faces, tile)


def torus(major, minor, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1), tile=0, ring=16, tube=6, arc=360.0):
    """Ring in the local XY plane (axis along Z). A partial arc gets capped ends."""
    m = matrix(loc, rot, scale)

    def create():
        closed = arc >= 359.9
        count = ring if closed else ring + 1
        rows = []
        for i in range(count):
            a = math.radians(arc) * i / ring
            centre = Vector((math.cos(a) * major, math.sin(a) * major, 0))
            out = Vector((math.cos(a), math.sin(a), 0))
            row = []
            for j in range(tube):
                b = 2 * math.pi * j / tube
                p = centre + out * math.cos(b) * minor + Vector((0, 0, math.sin(b) * minor))
                row.append(BM.verts.new(m @ p))
            rows.append(row)
        for i in range(count if closed else count - 1):
            a, b = rows[i], rows[(i + 1) % count]
            for j in range(tube):
                k = (j + 1) % tube
                BM.faces.new((a[j], b[j], b[k], a[k]))
        if not closed:
            BM.faces.new(list(reversed(rows[0])))
            BM.faces.new(rows[-1])
    faces = _track(create)
    return _finish_part(faces, tile)


def prism(outline, depth, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1), tile=0, side_tile=None, bevel=0.0):
    """Extrudes a 2D outline (XY, counter-clockwise) along Z, centred on its depth."""
    m = matrix(loc, rot, scale)

    def create():
        front = [BM.verts.new(m @ Vector((x, y, depth / 2))) for x, y in outline]
        back = [BM.verts.new(m @ Vector((x, y, -depth / 2))) for x, y in outline]
        BM.faces.new(front)
        BM.faces.new(list(reversed(back)))
        n = len(outline)
        for i in range(n):
            j = (i + 1) % n
            BM.faces.new((back[i], back[j], front[j], front[i]))
    faces = _track(create)
    faces = _finish_part(faces, tile, bevel)
    if side_tile is not None:
        for f in faces:
            if f.is_valid and abs(f.normal.z) < .5:
                f.material_index = side_tile
    return faces


def loft(sections, tile=0, cap=True):
    """sections: list of rings (lists of 3D points, same count) joined in order."""
    def create():
        rows = [[BM.verts.new(Vector(p)) for p in ring] for ring in sections]
        n = len(rows[0])
        for a, b in zip(rows, rows[1:]):
            for j in range(n):
                k = (j + 1) % n
                BM.faces.new((a[j], b[j], b[k], a[k]))
        if cap:
            BM.faces.new(list(reversed(rows[0])))
            BM.faces.new(rows[-1])
    faces = _track(create)
    return _finish_part(faces, tile)


def deform(faces, function):
    """Moves every vertex of the given faces through function(Vector) -> Vector."""
    for v in {v for f in faces if f.is_valid for v in f.verts}:
        v.co = function(v.co.copy())


def surface_z(x, y, start=2.0):
    """The front-most surface depth of everything built so far at screen point (x, y)."""
    BM.normal_update()
    tree = BVHTree.FromBMesh(BM)
    hit = tree.ray_cast(Vector((x, y, start)), Vector((0, 0, -1)))
    return hit[0].z if hit[0] is not None else 0.0


def outline_shell(faces, thickness, tile):
    """Inverted-hull outline: a slightly inflated copy facing inward, so back-face culling
    shows only a rim around the silhouette (the game's ship shader culls back faces)."""
    BM.normal_update()
    _cull_back_faces(MATERIALS[tile])
    verts = {v for f in faces if f.is_valid for v in f.verts}
    copy = {}
    for v in verts:
        copy[v] = BM.verts.new(v.co + v.normal * thickness)
    for f in [f for f in faces if f.is_valid]:
        shell = BM.faces.new([copy[v] for v in reversed(f.verts)])
        shell.material_index = tile
        SHELL.add(shell)


def _cull_back_faces(mat):
    """Preview only: Cycles has no back-face culling, so hide the shell's back faces."""
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    output = nodes.get('Material Output')
    bsdf = nodes.get('Principled BSDF')
    geometry = nodes.new('ShaderNodeNewGeometry')
    clear = nodes.new('ShaderNodeBsdfTransparent')
    mix = nodes.new('ShaderNodeMixShader')
    rays = nodes.new('ShaderNodeLightPath')
    seen = nodes.new('ShaderNodeMath')
    seen.operation = 'SUBTRACT'
    links.new(geometry.outputs['Backfacing'], seen.inputs[0])
    links.new(rays.outputs['Is Camera Ray'], seen.inputs[1])
    hidden = nodes.new('ShaderNodeMath')
    hidden.operation = 'ADD'
    hidden.use_clamp = True
    seen.inputs[0].default_value = 0
    links.new(seen.outputs[0], hidden.inputs[0])
    hidden.inputs[1].default_value = 1
    links.new(hidden.outputs[0], mix.inputs['Fac'])
    links.new(bsdf.outputs['BSDF'], mix.inputs[1])
    links.new(clear.outputs['BSDF'], mix.inputs[2])
    links.new(mix.outputs['Shader'], output.inputs['Surface'])


def finish(out, ortho_scale=1.3, triangulate=True):
    faces = [f for f in BM.faces if f not in SHELL]
    bmesh.ops.recalc_face_normals(BM, faces=faces)
    if triangulate:
        bmesh.ops.triangulate(BM, faces=BM.faces[:])
    mesh = bpy.data.meshes.new('ItemMesh')
    BM.to_mesh(mesh)
    BM.free()
    for mat in MATERIALS:
        mesh.materials.append(mat)
    for poly in mesh.polygons:
        poly.use_smooth = False
    obj = bpy.data.objects.new('Model', mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    print('triangles:', len(mesh.polygons), 'size:', tuple(round(d, 3) for d in obj.dimensions))
    bpy.ops.wm.obj_export(filepath=os.path.join(out, 'model.obj'), export_selected_objects=True,
                          export_materials=True, forward_axis='Y', up_axis='Z')
    render(out, ortho_scale)


def render(out, ortho_scale):
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 48
    scene.render.resolution_x = scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    world = bpy.data.worlds[0] if bpy.data.worlds else bpy.data.worlds.new('World')
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
    scene.view_settings.view_transform = 'Standard'

    def aim(ob, target=(0, 0, 0)):
        ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

    def light(name, kind, location, energy, size=None):
        data = bpy.data.lights.new(name, kind)
        data.energy = energy
        if size is not None:
            data.shape = 'DISK'
            data.size = size
        ob = bpy.data.objects.new(name, data)
        scene.collection.objects.link(ob)
        ob.location = location
        aim(ob)
        return ob

    sun = light('Sun', 'SUN', (-3, 5, 6), 2.0)
    sun.data.angle = math.radians(12)
    light('Fill', 'AREA', (4, 1, 4), 90, 5)
    cam_data = bpy.data.cameras.new('Camera')
    cam = bpy.data.objects.new('Camera', cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.location = (0, 0, 4)
    aim(cam)
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = ortho_scale
    scene.render.filepath = os.path.join(out, 'top.png')
    bpy.ops.render.render(write_still=True)
    cam.location = (1.8, 1.15, 3.1)
    aim(cam)
    cam_data.type = 'PERSP'
    cam_data.lens = 50
    scene.render.filepath = os.path.join(out, 'angle.png')
    bpy.ops.render.render(write_still=True)
