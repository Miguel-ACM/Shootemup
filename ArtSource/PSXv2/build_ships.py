"""PSXv2 fleet builder. Runs inside Blender (sent through the Blender MCP addon, or
`blender -b -P build_ships.py`). Builds the player ship + 4 enemies from low-poly
prisms, maps every face onto a 16px tile of PSXv2_Atlas.png, bakes simple vertex-colour
occlusion and exports FBX/OBJ into Assets/Art/PSXv2.

Conventions (top-down shmup): +Y is forward/up-screen, +Z points toward the camera.
1 Blender unit = 1 Unity world unit at gameplay scale.
Player: origin = Ship transform origin. The cockpit core disc is centred on the
CircleCollider2D (offset 0,0.08 local * scale 2 = 0,0.16 world, radius 0.0576*2 = 0.115).
Enemies face -Y (down the screen).
"""
import math
import os
import bpy
import bmesh
from mathutils import Matrix, Vector

ROOT = r"C:\Users\Miguel\Shootemup"
OUT = os.path.join(ROOT, "Assets", "Art", "PSXv2")
ATLAS = os.path.join(OUT, "PSXv2_Atlas.png")

TILES = {
    "hull": (0, 0), "steel": (1, 0), "dark": (2, 0), "vent": (3, 0), "hazard": (4, 0),
    "black": (5, 0), "greeble": (6, 0), "ribs": (7, 0),
    "blue": (0, 1), "red": (1, 1), "orange": (2, 1), "teal": (3, 1), "purple": (4, 1),
    "rust": (5, 1), "glass": (6, 1), "mark": (7, 1),
    "g_cyan": (0, 2), "g_orange": (1, 2), "g_red": (2, 2), "g_magenta": (3, 2),
    "g_white": (4, 2), "g_green": (5, 2), "g_yellow": (6, 2), "g_ring": (7, 2),
}
HITBOX_CENTER = (0.0, 0.16)
HITBOX_RADIUS = 0.115


# --------------------------------------------------------------------------- builder
class Builder:
    def __init__(self):
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.layer = self.bm.faces.layers.int.new("tile")
        self.names = list(TILES)

    def _tag(self, face, tile):
        face[self.layer] = self.names.index(tile)

    def prism(self, outline, z0, z1, top, side, inset=0.0, rise=0.0, cap=None, at=(0, 0), mirror=False):
        """Extruded polygon (no bottom: never seen from a top-down camera).
        outline: CCW list of (x, y). inset/rise bevel the top; cap = tile of the inset face."""
        pts = [(x + at[0], y + at[1]) for x, y in outline]
        if mirror:
            pts = [(-x, y) for x, y in reversed(pts)]
        bm = self.bm
        lo = [bm.verts.new((x, y, z0)) for x, y in pts]
        hi = [bm.verts.new((x, y, z1)) for x, y in pts]
        n = len(pts)
        for i in range(n):
            j = (i + 1) % n
            self._tag(bm.faces.new((lo[i], lo[j], hi[j], hi[i])), side)
        topf = bm.faces.new(hi)
        self._tag(topf, top)
        if inset > 0:
            res = bmesh.ops.inset_individual(bm, faces=[topf], thickness=inset, depth=rise)
            for f in res["faces"]:
                self._tag(f, top)
            self._tag(topf, cap or top)
        return self

    def sym(self, outline, *a, **k):
        """Prism plus its X-mirrored twin."""
        self.prism(outline, *a, **k)
        k["mirror"] = True
        return self.prism(outline, *a, **k)

    def box(self, cx, cy, w, h, z0, z1, top, side, **k):
        x0, x1, y0, y1 = cx - w / 2, cx + w / 2, cy - h / 2, cy + h / 2
        return self.prism([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], z0, z1, top, side, **k)

    def ngon(self, cx, cy, r, n, z0, z1, top, side, rot=0.0, **k):
        pts = [(cx + r * math.cos(rot + i * 2 * math.pi / n), cy + r * math.sin(rot + i * 2 * math.pi / n)) for i in range(n)]
        return self.prism(pts, z0, z1, top, side, **k)

    def ring(self, cx, cy, r_out, r_in, n, z0, z1, top, side, rot=0.0):
        """Flat annulus made of n trapezoid prisms (gives a real hole in the silhouette)."""
        for i in range(n):
            a0, a1 = rot + i * 2 * math.pi / n, rot + (i + 1) * 2 * math.pi / n
            p = lambda r, a: (cx + r * math.cos(a), cy + r * math.sin(a))
            self.prism([p(r_in, a0), p(r_out, a0), p(r_out, a1), p(r_in, a1)], z0, z1, top, side)
        return self

    # ----------------------------------------------------------------- finishing
    def finish(self, name, cell=0.0):
        """cell > 0 cuts upward faces into a cell x cell plate grid so big hulls repeat their
        16px tile instead of stretching it (PS1 games subdivided large polys the same way)."""
        bm = self.bm
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bm.normal_update()
        if cell > 0:
            for axis in (0, 1):
                xs = [v.co[axis] for v in bm.verts]
                k = math.floor(min(xs) / cell) + 1
                while k * cell < max(xs):
                    tops = [f for f in bm.faces if f.normal.z > 0.9]
                    geom = list({e for f in tops for e in f.edges}) + tops + list({v for f in tops for v in f.verts})
                    no = (1, 0, 0) if axis == 0 else (0, 1, 0)
                    co = (k * cell, 0, 0) if axis == 0 else (0, k * cell, 0)
                    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no)
                    k += 1
            bm.normal_update()
        self._uv_tiles()
        mesh = bpy.data.meshes.new(name)
        bm.to_mesh(mesh)
        bm.free()
        for p in mesh.polygons:
            p.use_smooth = False
        self._vertex_ao(mesh)
        ob = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(ob)
        return ob

    def _uv_tiles(self):
        pad = 0.5 / 128  # half-texel inset keeps nearest sampling inside the tile
        for f in self.bm.faces:
            tile = self.names[f[self.layer]]
            nrm = f.normal
            ax = max(range(3), key=lambda i: abs(nrm[i]))
            u_i, v_i = [(0, 1), (0, 2), (0, 1)][ax] if ax != 0 else (1, 2)
            co = [(l.vert.co[u_i], l.vert.co[v_i]) for l in f.loops]
            if ax == 0 and nrm[0] < 0 or ax == 1 and nrm[1] > 0:
                co = [(-u, v) for u, v in co]
            us, vs = [c[0] for c in co], [c[1] for c in co]
            w, h = max(us) - min(us), max(vs) - min(vs)
            s = 1.0 / max(w, h, 1e-6)
            # keep aspect, fill along the longer side
            c, r = TILES[tile]
            u0, v0 = c / 8 + pad, 1 - (r + 1) / 8 + pad
            span = 1 / 8 - 2 * pad
            for l, (u, v) in zip(f.loops, co):
                l[self.uv].uv = (u0 + (u - min(us)) * s * span, v0 + (v - min(vs)) * s * span)

    @staticmethod
    def _vertex_ao(mesh):
        """Cheap baked occlusion: darker toward the bottom of each part and on side faces."""
        zs = [v.co.z for v in mesh.vertices]
        zmin, zmax = min(zs), max(zs)
        col = mesh.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
        for p in mesh.polygons:
            facing = max(0.0, p.normal.z)
            for li in p.loop_indices:
                z = mesh.vertices[mesh.loops[li].vertex_index].co.z
                h = (z - zmin) / max(zmax - zmin, 1e-6)
                k = 0.55 + 0.30 * h + 0.15 * facing
                col.data[li].color = (k, k, k, 1.0)


# --------------------------------------------------------------------------- materials
def atlas_image():
    img = bpy.data.images.get("PSXv2_Atlas.png") or bpy.data.images.load(ATLAS)
    img.reload()
    return img


def material(name, emissive=False, color=None):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    if color is not None:
        em = nt.nodes.new("ShaderNodeEmission")
        em.inputs["Color"].default_value = (*color, 1)
        em.inputs["Strength"].default_value = 1.0
        nt.links.new(em.outputs[0], out.inputs[0])
        return m
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = atlas_image()
    tex.interpolation = "Closest"  # Render96: nearest-neighbour texels
    vc = nt.nodes.new("ShaderNodeVertexColor")
    vc.layer_name = "Col"
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs[0].default_value = 1.0
    nt.links.new(tex.outputs[0], mul.inputs[6])
    nt.links.new(vc.outputs[0], mul.inputs[7])
    if emissive:
        sh = nt.nodes.new("ShaderNodeEmission")
        nt.links.new(tex.outputs[0], sh.inputs["Color"])
    else:
        sh = nt.nodes.new("ShaderNodeBsdfDiffuse")
        sh.inputs["Roughness"].default_value = 1.0
        nt.links.new(mul.outputs[2], sh.inputs["Color"])
    nt.links.new(sh.outputs[0], out.inputs[0])
    return m


# --------------------------------------------------------------------------- models
def player():
    b = Builder()
    # Spine fuselage: long hexagonal keel, white Ikaruga-style armour.
    b.prism([(0, 0.92), (0.15, 0.60), (0.20, 0.05), (0.17, -0.52), (0.0, -0.60),
             (-0.17, -0.52), (-0.20, 0.05), (-0.15, 0.60)], 0.0, 0.20, "hull", "steel", inset=0.045, rise=0.04, cap="hull")
    # Nose blade (dark) and blue spine stripe.
    b.prism([(0, 0.98), (0.05, 0.72), (-0.05, 0.72)], 0.20, 0.27, "dark", "black")
    b.box(0, -0.25, 0.08, 0.42, 0.24, 0.27, "blue", "black")
    # Forward-swept main wings with blue leading edge and hazard roots.
    b.sym([(0.18, 0.22), (0.70, -0.02), (0.74, -0.30), (0.58, -0.40), (0.18, -0.30)], 0.03, 0.10, "hull", "dark", inset=0.03, rise=0.015)
    b.sym([(0.18, 0.22), (0.70, -0.02), (0.66, 0.06), (0.20, 0.30)], 0.06, 0.12, "blue", "black")
    b.sym([(0.20, -0.04), (0.34, -0.10), (0.34, -0.24), (0.20, -0.20)], 0.10, 0.13, "hazard", "black")
    # Wingtip autocannons.
    for x in (0.66, -0.66):
        b.box(x, 0.02, 0.07, 0.62, 0.06, 0.17, "dark", "steel")
        b.box(x, 0.38, 0.035, 0.14, 0.08, 0.14, "black", "black")
    # Nose guns line up with PSXShipEffects.MuzzlePosition (local +-0.1134, 0.30 x Ship scale 2), where bullets spawn.
    for x in (0.227, -0.227):
        b.box(x, 0.42, 0.05, 0.36, 0.08, 0.19, "dark", "black")
        b.box(x, 0.60, 0.07, 0.04, 0.08, 0.20, "steel", "black")
    # Twin engine nacelles with cyan burners at the back.
    for x in (0.26, -0.26):
        b.ngon(x, -0.40, 0.10, 6, 0.0, 0.17, "steel", "dark", rot=math.pi / 6, inset=0.025, rise=0.02, cap="vent")
        b.box(x, -0.56, 0.12, 0.10, 0.02, 0.12, "g_cyan", "g_cyan")
    # Canopy frame: dark octagonal collar that makes the core pop against white armour.
    b.ngon(*HITBOX_CENTER, HITBOX_RADIUS + 0.07, 8, 0.20, 0.27, "black", "black", rot=math.pi / 8)
    ship = b.finish("PSXv2_Player")
    ship.data.materials.append(material("PSXv2_Steel"))

    # Hitbox core: separate, unlit, exact collider radius. Ring = thin magenta halo just outside it.
    c = Builder()
    c.ngon(*HITBOX_CENTER, HITBOX_RADIUS, 12, 0.27, 0.31, "g_white", "g_white", inset=0.0)
    core = c.finish("PSXv2_Player_HitboxCore")
    core.data.materials.append(material("PSXv2_HitboxCore", color=(1, 1, 1)))
    r = Builder()
    r.ring(*HITBOX_CENTER, HITBOX_RADIUS + 0.035, HITBOX_RADIUS + 0.008, 12, 0.27, 0.30, "g_ring", "g_ring")
    ring = r.finish("PSXv2_Player_HitboxRing")
    ring.data.materials.append(material("PSXv2_HitboxRing", color=(1, 0.12, 0.35)))
    core.parent = ship
    ring.parent = ship
    return [ship, core, ring]



# --------------------------------------------------------------------------- animation
# Enemies are a root empty + rigid parts with pivots at their hinge. Loops are keyed on
# twos (15 Hz at 30 fps) with CONSTANT interpolation: stepped motion like PS1 games,
# baked into one FBX take per enemy (Unity: set the clip to Loop Time).
FPS, STEP = 30, 2
PIV = {}


def root(name, loop):
    ob = bpy.data.objects.new(name, None)
    ob.empty_display_size = 0.3
    bpy.context.scene.collection.objects.link(ob)
    ob["loop_frames"] = loop
    PIV[name] = Vector((0, 0, 0))
    return ob


def part(name, parent, pivot, build, cell=0.0):
    b = Builder()
    build(b)
    ob = b.finish(name, cell)
    pivot = Vector(pivot)
    ob.data.transform(Matrix.Translation(-pivot))
    ob.parent = parent
    ob.location = pivot - PIV[parent.name]
    PIV[name] = pivot
    ob.data.materials.append(material("PSXv2_Steel"))
    return ob


def animate(ob, loop, fn):
    """fn(t, i) -> dict(location=(dx,dy,dz), rotation=(rx,ry,rz), scale=(sx,sy,sz)) offsets from rest."""
    loc0, rot0, sc0 = ob.location.copy(), ob.rotation_euler.copy(), ob.scale.copy()
    for i, f in enumerate(range(0, loop + 1, STEP)):
        v = fn((f % loop) / loop, i % (loop // STEP))
        ob.location = loc0 + Vector(v.get("location", (0, 0, 0)))
        ob.rotation_euler = [a + b for a, b in zip(rot0, v.get("rotation", (0, 0, 0)))]
        ob.scale = [a * b for a, b in zip(sc0, v.get("scale", (1, 1, 1)))]
        for path in ("location", "rotation_euler", "scale"):
            ob.keyframe_insert(path, frame=f)
    ob.location, ob.rotation_euler, ob.scale = loc0, rot0, sc0


FLICKER = (1.0, 1.7, 1.2, 1.9, 1.1, 1.5)


def flicker(t, i):
    return {"scale": (1, FLICKER[i % len(FLICKER)], 1)}


def recoil(at, width=0.06, dist=0.2, axis=1):
    def fn(t, i):
        d = (t - at) % 1.0
        off = dist if d < width else dist * 0.45 if d < width * 2 else 0.0
        v = [0, 0, 0]
        v[axis] = off
        return {"location": tuple(v)}
    return fn


TAU = 2 * math.pi


# --------------------------------------------------------------------------- enemies
def interceptor():
    """Enemy_0 'Dart': red arrowhead, banks side to side, eye blinks, thruster flickers."""
    L = 24
    r = root("PSXv2_Enemy_0_Dart", L)

    def hull(b):
        b.prism([(0, -0.62), (0.52, 0.30), (0.40, 0.42), (0.14, 0.18), (0, 0.30),
                 (-0.14, 0.18), (-0.40, 0.42), (-0.52, 0.30)], 0.0, 0.10, "red", "dark", inset=0.04, rise=0.03, cap="red")
        b.prism([(0, -0.40), (0.12, 0.05), (0, 0.18), (-0.12, 0.05)], 0.13, 0.22, "dark", "black", inset=0.02, rise=0.02, cap="steel")
        b.sym([(0.40, 0.30), (0.50, 0.30), (0.46, 0.52), (0.40, 0.48)], 0.05, 0.16, "steel", "black")
    h = part("Dart_Hull", r, (0, 0, 0.1), hull)
    e = part("Dart_Eye", h, (0, -0.30, 0.24), lambda b: b.ngon(0, -0.30, 0.055, 4, 0.22, 0.26, "g_red", "g_red", rot=math.pi / 4))
    th = part("Dart_Thruster", h, (0, 0.16, 0.12), lambda b: b.box(0, 0.20, 0.10, 0.08, 0.08, 0.16, "g_orange", "g_orange"))
    animate(h, L, lambda t, i: {"rotation": (0, 0.24 * math.sin(TAU * t), 0)})
    animate(e, L, lambda t, i: {"scale": (1.7, 1.7, 1) if i in (0, 1, 6) else (1, 1, 1)})
    animate(th, L, flicker)
    return r


def rotor():
    """Enemy_1 'Rotor': hex ring + lamp pods spin, the hub counter-rotates and pulses."""
    L = 48
    r = root("PSXv2_Enemy_1_Rotor", L)

    def spinner(b):
        b.ring(0, 0, 0.52, 0.30, 6, 0.0, 0.12, "orange", "dark", rot=math.pi / 6)
        for k in range(3):
            a = k * TAU / 3 - math.pi / 2
            ca, sa = math.cos(a), math.sin(a)
            pts = [(ca * 0.12 - sa * 0.05, sa * 0.12 + ca * 0.05), (ca * 0.12 + sa * 0.05, sa * 0.12 - ca * 0.05),
                   (ca * 0.34 + sa * 0.05, sa * 0.34 - ca * 0.05), (ca * 0.34 - sa * 0.05, sa * 0.34 + ca * 0.05)]
            b.prism(pts, 0.03, 0.10, "hazard", "black")
            b.ngon(ca * 0.58, sa * 0.58, 0.09, 4, 0.0, 0.16, "dark", "steel", rot=a + math.pi / 4, inset=0.02, rise=0.02, cap="g_yellow")
    s = part("Rotor_Spinner", r, (0, 0, 0), spinner)
    hub = part("Rotor_Hub", r, (0, 0, 0.09), lambda b: b.ngon(0, 0, 0.15, 8, 0.0, 0.18, "steel", "dark", rot=math.pi / 8, inset=0.04, rise=0.03, cap="g_orange"))
    animate(s, L, lambda t, i: {"rotation": (0, 0, -TAU / 3 * t)})          # 3-fold symmetric -> seamless
    animate(hub, L, lambda t, i: {"rotation": (0, 0, TAU / 4 * t),            # 8-fold symmetric
                                  "scale": (1 + 0.15 * (i % 4 == 0), 1 + 0.15 * (i % 4 == 0), 1)})
    return r


def gunboat():
    """Enemy_2 'Gunboat': turret sweeps and fires its twin barrels, engines flicker."""
    L = 60
    r = root("PSXv2_Enemy_2_Gunboat", L)

    def hull(b):
        for x in (1.25, -1.25):
            b.prism([(x - 0.42, 1.5), (x + 0.42, 1.5), (x + 0.48, -0.9), (x + 0.22, -1.72), (x - 0.22, -1.72), (x - 0.48, -0.9)],
                    0.0, 0.32, "teal", "dark", inset=0.07, rise=0.05, cap="teal")
            b.box(x, 1.36, 0.55, 0.22, 0.10, 0.30, "dark", "dark")
            b.box(x, -0.20, 0.30, 0.90, 0.37, 0.45, "vent", "black")
            b.box(x * 1.18, -1.30, 0.08, 0.70, 0.25, 0.40, "dark", "steel")
        b.box(0, 0.25, 1.70, 1.50, 0.08, 0.26, "dark", "dark", inset=0.08, rise=0.03, cap="greeble")
        b.box(0, -0.70, 1.80, 0.25, 0.10, 0.22, "hazard", "black")
    part("Gunboat_Hull", r, (0, 0, 0), hull, cell=0.6)
    for x, side in ((1.25, "R"), (-1.25, "L")):
        eng = part(f"Gunboat_Engine_{side}", r, (x, 1.47, 0.2), lambda b, x=x: b.box(x, 1.55, 0.55, 0.16, 0.10, 0.30, "g_cyan", "g_cyan"))
        animate(eng, L, lambda t, i, o=3 * (side == "L"): flicker(t, i + o))

    def turret(b):
        b.ngon(0, 0.10, 0.48, 8, 0.30, 0.55, "teal", "dark", rot=math.pi / 8, inset=0.08, rise=0.04, cap="steel")
        b.ngon(0, 0.10, 0.12, 6, 0.59, 0.64, "g_red", "g_red")
    tu = part("Gunboat_Turret", r, (0, 0.10, 0.42), turret)
    for x, at in ((0.16, 0.20), (-0.16, 0.45)):
        g = part(f"Gunboat_Barrel_{'R' if x > 0 else 'L'}", tu, (x, -0.15, 0.47), lambda b, x=x: b.box(x, -0.65, 0.12, 1.00, 0.42, 0.52, "dark", "black"))
        animate(g, L, recoil(at, dist=0.22))
    animate(tu, L, lambda t, i: {"rotation": (0, 0, 0.45 * math.sin(TAU * t))})
    return r


def warden():
    """Enemy_3 'Freight Warden' boss: drum turrets sweep and fire alternately, reactor spins
    and pulses, engines flicker. Footprint follows the stage1boss polygon collider (~12 x 8)."""
    L = 90
    r = root("PSXv2_Enemy_3_Warden", L)
    hull_pts = [(0.0, -3.6), (1.2, -2.6), (3.0, -2.1), (4.6, -2.2), (5.6, -1.3), (5.9, 0.0), (5.9, 1.8),
                (5.3, 3.2), (4.2, 4.3), (2.6, 4.5), (1.4, 4.4), (0.5, 3.6), (-0.5, 3.6), (-1.4, 4.4),
                (-2.6, 4.5), (-4.2, 4.3), (-5.3, 3.2), (-5.9, 1.8), (-5.9, 0.0), (-5.6, -1.3), (-4.6, -2.2),
                (-3.0, -2.1), (-1.2, -2.6)]

    def hull(b):
        b.prism(hull_pts, 0.0, 0.45, "steel", "dark", inset=0.30, rise=0.10, cap="purple")
        b.prism([(0, -3.3), (0.9, -1.8), (1.0, 2.8), (-1.0, 2.8), (-0.9, -1.8)], 0.55, 0.95, "hull", "steel", inset=0.18, rise=0.06, cap="hull")
        b.prism([(0, -3.55), (0.45, -2.6), (-0.45, -2.6)], 0.95, 1.10, "g_red", "black")
        b.box(0, 2.0, 1.4, 0.5, 0.95, 1.05, "vent", "black")
        b.box(0, 3.75, 1.6, 0.35, 0.40, 0.55, "hazard", "black")
        for s in (1, -1):
            b.box(4.6 * s, 3.9, 0.9, 0.9, 0.2, 0.75, "steel", "dark", inset=0.1, rise=0.03, cap="vent")
            b.box(2.0 * s, -2.1, 0.5, 0.6, 0.45, 0.75, "dark", "steel", inset=0.06, rise=0.02, cap="g_red")
            b.box(5.2 * s, -0.8, 0.12, 1.0, 0.45, 0.65, "ribs", "black")
    part("Warden_Hull", r, (0, 0, 0), hull, cell=1.0)
    rc = part("Warden_Reactor", r, (0, 0.6, 1.1), lambda b: b.ngon(0, 0.6, 0.55, 8, 0.95, 1.20, "black", "black", rot=math.pi / 8, inset=0.12, rise=0.0, cap="g_magenta"))
    animate(rc, L, lambda t, i: {"rotation": (0, 0, TAU / 8 * t), "scale": (1.12, 1.12, 1) if i % 5 == 0 else (1, 1, 1)})
    for s, side, at in ((1, "R", 0.15), (-1, "L", 0.65)):
        cx, cy = 3.6 * s, 1.2

        def drum(b, cx=cx, cy=cy):
            b.ngon(cx, cy, 1.45, 8, 0.45, 0.95, "dark", "steel", rot=math.pi / 8, inset=0.22, rise=0.08, cap="purple")
            b.ngon(cx, cy, 0.55, 6, 1.03, 1.35, "steel", "black", inset=0.12, rise=0.05, cap="g_magenta")
        tu = part(f"Warden_Turret_{side}", r, (cx, cy, 0.7), drum, cell=1.0)
        animate(tu, L, lambda t, i, s=s: {"rotation": (0, 0, 0.30 * s * math.sin(TAU * t))})

        def guns(b, cx=cx, cy=cy):
            for dx in (-0.35, 0.35):
                b.box(cx + dx, cy - 1.9, 0.20, 1.8, 0.80, 1.00, "dark", "black")
                b.box(cx + dx, cy - 2.85, 0.26, 0.18, 0.78, 1.02, "steel", "black")
        g = part(f"Warden_Guns_{side}", tu, (cx, cy - 1.0, 0.9), guns)
        animate(g, L, recoil(at, width=0.04, dist=0.45))
        eng = part(f"Warden_Engine_{side}", r, (4.6 * s, 4.33, 0.42), lambda b, s=s: b.box(4.6 * s, 4.45, 0.7, 0.25, 0.25, 0.6, "g_magenta", "g_magenta"))
        animate(eng, L, lambda t, i, o=2 * (s < 0): flicker(t, i + o))
    return r


# --------------------------------------------------------------------------- export
def hierarchy(ob):
    out = [ob]
    for c in ob.children:
        out += hierarchy(c)
    return out


def tris(objs):
    return sum(len(p.vertices) - 2 for o in objs if o.type == "MESH" for p in o.data.polygons)


def export(top, name):
    objs = hierarchy(top)
    loop = top.get("loop_frames", 0)
    sc = bpy.context.scene
    sc.render.fps = FPS
    if loop:
        sc.frame_start, sc.frame_end = 0, loop - 1
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = top
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + ".fbx"), use_selection=True,
                             object_types={"EMPTY", "MESH"},
                             apply_scale_options="FBX_SCALE_UNITS", axis_forward="Y", axis_up="Z",
                             bake_space_transform=False, mesh_smooth_type="FACE", use_triangles=True,
                             colors_type="SRGB", path_mode="STRIP", add_leaf_bones=False,
                             bake_anim=bool(loop), bake_anim_use_all_bones=False, bake_anim_use_nla_strips=False,
                             bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True,
                             bake_anim_step=1.0, bake_anim_simplify_factor=0.0)
    bpy.ops.wm.obj_export(filepath=os.path.join(ROOT, "ArtSource", "PSXv2", name + ".obj"), export_selected_objects=True,
                          export_triangulated_mesh=True, export_colors=True, forward_axis="Y", up_axis="Z",
                          export_animation=False, path_mode="STRIP")
    return tris(objs)


def main():
    for o in list(bpy.data.objects):
        if o.name.startswith(("PSXv2_", "Dart_", "Rotor_", "Gunboat_", "Warden_")) or o.name == "Cube":
            bpy.data.objects.remove(o, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.actions):
        for d in list(coll):
            if d.users == 0:
                coll.remove(d)
    prefs = bpy.context.preferences.edit
    old = prefs.keyframe_new_interpolation_type
    prefs.keyframe_new_interpolation_type = "CONSTANT"
    report = {}
    layout = [(player, "PSXv2_Player", (0, 0)), (interceptor, "PSXv2_Enemy_0_Dart", (3, 4)),
              (rotor, "PSXv2_Enemy_1_Rotor", (-3, 4)), (gunboat, "PSXv2_Enemy_2_Gunboat", (6, -3)),
              (warden, "PSXv2_Enemy_3_Warden", (0, 12))]
    try:
        for fn, name, _ in layout:
            res = fn()
            top = res[0] if isinstance(res, list) else res
            report[name] = export(top, name)
    finally:
        prefs.keyframe_new_interpolation_type = old
    for fn, name, off in layout:
        bpy.data.objects[name].location = (off[0], off[1], 0)
    bpy.context.scene.frame_set(0)
    return report


result = main()
print(result)
