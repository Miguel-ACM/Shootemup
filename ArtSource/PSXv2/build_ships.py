"""PSXv2 fleet builder. Runs inside Blender (sent through the Blender MCP addon, or
`blender -b -P build_ships.py`). Builds the player ship, 4 animated enemies and the 17
equipment/pickup models from low-poly lofts, tubes and spheres, maps every face onto a
16px tile of PSXv2_Atlas.png, bakes vertex-colour occlusion and exports FBX/OBJ into
Assets/Art/PSXv2.

Conventions (top-down shmup): +Y is forward/up-screen, +Z points toward the camera.
1 Blender unit = 1 Unity world unit at gameplay scale.
Player: origin = Ship transform origin. The cockpit core disc is centred on the
hexagonal PolygonCollider2D (centre 0,0.08 local * Ship scale 2 = 0,0.16 world, circumradius 0.08).
Enemies face -Y (down the screen). Items stand upright (+Y) and face the camera (+Z);
they are closed meshes because they spin in game.
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
    "gold": (0, 3), "ceramic": (1, 3), "coffee": (2, 3), "skin": (3, 3), "moon": (4, 3),
    "green": (5, 3), "plastic": (6, 3), "blackplastic": (7, 3),
    "medcross": (0, 4), "battery": (1, 4), "coin": (2, 4), "label_p": (3, 4), "boltblack": (4, 4),
    "magnet": (5, 4), "chrome": (6, 4), "brass": (7, 4),
    "deep": (0, 5), "windows": (1, 5), "number": (2, 5), "caution": (3, 5), "scorch": (4, 5),
    "grating": (5, 5), "screen": (6, 5), "white": (7, 5),
    "blackarmour": (0, 6), "yellow": (1, 6), "greyhazard": (2, 6), "greyglass": (3, 6), "grey": (4, 6),
    "lightgrey": (5, 6), "greywindows": (6, 6), "yellowstripe": (7, 6),
    "grass": (0, 7), "flowers": (1, 7), "tufts": (2, 7), "dirt": (3, 7), "leaves": (4, 7), "darkleaves": (5, 7),
    "bark": (6, 7), "water": (7, 7),
}
# One colour per model (Miguel, 2026-10-20): every enemy in a shared grey with white glows;
# the player black with yellow details. Tiles are remapped while a theme is active.
ENEMY_THEME = {
    "red": "grey", "orange": "grey", "teal": "grey", "purple": "grey", "deep": "dark", "blue": "grey",
    "rust": "dark", "caution": "greyhazard", "hazard": "greyhazard", "glass": "greyglass",
    "windows": "greywindows", "screen": "dark", "number": "number", "white": "lightgrey", "hull": "lightgrey",
    "g_red": "g_white", "g_orange": "g_white", "g_magenta": "g_white", "g_cyan": "g_white",
    "g_yellow": "g_white", "g_green": "g_white",
}
PLAYER_THEME = {
    "white": "blackarmour", "hull": "blackarmour", "steel": "dark", "blue": "yellow", "red": "yellow",
    "glass": "greyglass", "g_cyan": "g_yellow", "g_ring": "g_yellow",
}
THEME = {}


def themed(theme):
    """Decorator: build everything inside fn with the tile remap active."""
    def wrap(fn):
        def run(*a, **k):
            global THEME
            previous, THEME = THEME, theme
            try:
                return fn(*a, **k)
            finally:
                THEME = previous
        return run
    return wrap
HITBOX_CENTER = (0.0, 0.16)
HITBOX_RADIUS = 0.08   # hexagon circumradius (pointy top), matches the PolygonCollider2D the installer builds
HEX = math.pi / 2
TAU = 2 * math.pi


# --------------------------------------------------------------------------- builder
class Builder:
    def __init__(self, closed=False):
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.layer = self.bm.faces.layers.int.new("tile")
        self.fix = self.bm.faces.layers.int.new("uvfix")
        self.fixed = []
        self.names = list(TILES)
        self.closed = closed

    def _tag(self, face, tile, uvs=None):
        face[self.layer] = self.names.index(THEME.get(tile, tile))
        if uvs is not None:
            self.fixed.append(uvs)
            face[self.fix] = len(self.fixed)

    def mark(self):
        self.bm.verts.ensure_lookup_table()
        return len(self.bm.verts)

    def xform(self, mark, matrix):
        """Transform every vertex created since mark()."""
        self.bm.verts.ensure_lookup_table()
        for v in self.bm.verts[mark:]:
            v.co = matrix @ v.co
        return self

    def _outward(self, face, center):
        face.normal_update()
        if face.normal.dot(face.calc_center_median() - center) < 0:
            face.normal_flip()

    def prism(self, outline, z0, z1, top, side, inset=0.0, rise=0.0, cap=None, at=(0, 0), mirror=False,
              taper=1.0, bottom=None):
        """Lofted polygon. taper < 1 shrinks the top outline toward its centroid, giving sloped
        sides that catch the banded light; inset/rise add a raised top panel (cap tile)."""
        pts = [(x + at[0], y + at[1]) for x, y in outline]
        if mirror:
            pts = [(-x, y) for x, y in reversed(pts)]
        cx = sum(p[0] for p in pts) / len(pts)
        cy = sum(p[1] for p in pts) / len(pts)
        bm = self.bm
        lo = [bm.verts.new((x, y, z0)) for x, y in pts]
        hi = [bm.verts.new((cx + (x - cx) * taper, cy + (y - cy) * taper, z1)) for x, y in pts]
        n = len(pts)
        for i in range(n):
            j = (i + 1) % n
            self._tag(bm.faces.new((lo[i], lo[j], hi[j], hi[i])), side)
        topf = bm.faces.new(hi)
        self._tag(topf, top)
        if self.closed or bottom:
            self._tag(bm.faces.new(list(reversed(lo))), bottom or side)
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
        pts = [(cx + r * math.cos(rot + i * TAU / n), cy + r * math.sin(rot + i * TAU / n)) for i in range(n)]
        return self.prism(pts, z0, z1, top, side, **k)

    def ring(self, cx, cy, r_out, r_in, n, z0, z1, top, side, rot=0.0, taper=1.0):
        """Flat annulus made of n trapezoid prisms (gives a real hole in the silhouette)."""
        for i in range(n):
            a0, a1 = rot + i * TAU / n, rot + (i + 1) * TAU / n
            p = lambda r, a: (cx + r * math.cos(a), cy + r * math.sin(a))
            self.prism([p(r_in, a0), p(r_out, a0), p(r_out, a1), p(r_in, a1)], z0, z1, top, side, taper=taper)
        return self

    def tube(self, p0, p1, r, n, side, cap0=None, cap1=None, r1=None, rot=None):
        """Cylinder/cone between two points: round volume with banded shading (barrels, pipes,
        nacelles). cap0/cap1 tile the end discs (None leaves the end open)."""
        p0, p1 = Vector(p0), Vector(p1)
        r1 = r if r1 is None else r1
        axis = (p1 - p0).normalized()
        helper = Vector((0, 0, 1)) if abs(axis.z) < 0.9 else Vector((1, 0, 0))
        u = axis.cross(helper).normalized()
        v = axis.cross(u)
        rot = math.pi / n if rot is None else rot
        ring = lambda c, rr: [c + (u * math.cos(rot + i * TAU / n) + v * math.sin(rot + i * TAU / n)) * rr for i in range(n)]
        bm = self.bm
        a = [bm.verts.new(p) for p in ring(p0, r)]
        mid = (p0 + p1) / 2
        if r1 <= 1e-4:
            apex = bm.verts.new(p1)
            for i in range(n):
                f = bm.faces.new((a[i], a[(i + 1) % n], apex))
                self._tag(f, side)
                self._outward(f, mid)
        else:
            b = [bm.verts.new(p) for p in ring(p1, r1)]
            for i in range(n):
                j = (i + 1) % n
                f = bm.faces.new((a[i], a[j], b[j], b[i]))
                self._tag(f, side)
                self._outward(f, mid)
            if cap1:
                f = bm.faces.new(b)
                self._tag(f, cap1)
                self._outward(f, mid)
        if cap0:
            f = bm.faces.new(a)
            self._tag(f, cap0)
            self._outward(f, mid)
        return self

    def sphere(self, c, r, tile, seg=10, rings=6, scale=(1, 1, 1)):
        """UV sphere with the tile wrapped longitude/latitude (moons, bowls, domes)."""
        c = Vector(c)
        bm = self.bm
        pt = lambda i, j: c + Vector((
            r * scale[0] * math.sin(math.pi * j / rings) * math.cos(TAU * i / seg),
            r * scale[1] * math.cos(math.pi * j / rings),
            r * scale[2] * math.sin(math.pi * j / rings) * math.sin(TAU * i / seg)))
        grid = {}
        for j in range(rings + 1):
            for i in range(seg + 1):
                if (j == 0 or j == rings) and i > 0:
                    grid[(i, j)] = grid[(0, j)]
                elif i == seg:
                    grid[(i, j)] = grid[(0, j)]
                else:
                    grid[(i, j)] = bm.verts.new(pt(i, j))
        for j in range(rings):
            for i in range(seg):
                quad = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
                verts, uvs = [], {}
                for key in quad:
                    vert = grid[key]
                    if vert not in verts:
                        verts.append(vert)
                        uvs[vert] = (key[0] / seg, 1 - key[1] / rings)
                if len(verts) < 3:
                    continue
                f = bm.faces.new(verts)
                self._outward(f, c)
                self._tag(f, tile, uvs)
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
                    tops = [f for f in bm.faces if f.normal.z > 0.9 and not f[self.fix]]
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
        span = 1 / 8 - 2 * pad
        for f in self.bm.faces:
            c, r = TILES[self.names[f[self.layer]]]
            u0, v0 = c / 8 + pad, 1 - (r + 1) / 8 + pad
            if f[self.fix]:
                fixed = self.fixed[f[self.fix] - 1]
                for l in f.loops:
                    u, v = fixed.get(l.vert, (0.5, 0.5))
                    l[self.uv].uv = (u0 + u * span, v0 + v * span)
                continue
            nrm = f.normal
            ax = max(range(3), key=lambda i: abs(nrm[i]))
            u_i, v_i = [(1, 2), (0, 2), (0, 1)][ax]
            co = [(l.vert.co[u_i], l.vert.co[v_i]) for l in f.loops]
            if ax == 0 and nrm[0] < 0 or ax == 1 and nrm[1] > 0:
                co = [(-u, v) for u, v in co]
            us, vs = [p[0] for p in co], [p[1] for p in co]
            w, h = max(us) - min(us), max(vs) - min(vs)
            s = 1.0 / max(w, h, 1e-6)
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


def material(name, color=None):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    if color is not None:
        em = nt.nodes.new("ShaderNodeEmission")
        em.inputs["Color"].default_value = (*color, 1)
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
    sh = nt.nodes.new("ShaderNodeBsdfDiffuse")
    sh.inputs["Roughness"].default_value = 1.0
    nt.links.new(mul.outputs[2], sh.inputs["Color"])
    nt.links.new(sh.outputs[0], out.inputs[0])
    return m


# --------------------------------------------------------------------------- player
CORE_Z = 0.30   # height of the hitbox core/ring above the hull
PX = 0.035      # world units per pixel of Miguel's 34x34 reference sprite


def ref(px, py):
    """Reference sprite pixel -> world: its core centre (16.5, 14.5) sits on the hitbox, y up."""
    return ((px - 16.5) * PX, HITBOX_CENTER[1] - (py - 14.5) * PX)


def player_geometry(b, hitbox=False):
    """Player ship after Miguel's 34x34 reference: a compact black TIE-style body with three spikes
    on top (the outer two over the bullet spawn points at x +-0.227), short struts out to two tall
    vertical wing blades, a short tail, thin grey edge highlights, and an elongated glowing
    yellow-orange hex core around the hexagonal hitbox. About 1.2 wide and 0.95 tall."""
    R = lambda pts: [ref(x, y) for x, y in pts]
    # Central body with an inset top: the outer rim reads as a thin grey highlight.
    b.prism(R([(9, 9.5), (24, 9.5), (26, 12), (26, 19), (22, 21.5), (19, 25.5), (14, 25.5), (11, 21.5), (7, 19), (7, 12)]),
            0.0, 0.22, "lightgrey", "dark", inset=0.025, rise=0.02, cap="dark", taper=0.85)
    # Centre spike above the core.
    b.prism(R([(14.5, 9.6), (16.5, 5), (18.5, 9.6)]), 0.04, 0.2, "dark", "lightgrey", taper=0.6)
    # Bat-wing panels: from the side spike tip (over the bullet spawn point) down to the blade.
    b.sym(R([(3.8, 11), (11, 3), (13, 9.6), (9, 12), (7.5, 19.5), (3.8, 21)]), 0.02, 0.16,
          "lightgrey", "dark", inset=0.02, rise=0.015, cap="dark", taper=0.92)
    # Tall vertical wing blades, curved at the top and bottom ends.
    b.sym(R([(0.5, 8), (2, 4.5), (3.5, 4), (3.8, 12), (3.8, 26), (3.5, 30), (2, 30), (0.5, 26)]), 0.0, 0.3,
          "lightgrey", "dark", inset=0.02, rise=0.02, cap="dark", taper=0.75)    # Two small thrusters in the gaps between the wing blades and the body, pointing backward.
    for px in (5.6, 27.4):
        x = ref(px, 0)[0]
        b.tube((x, ref(0, 28.5)[1], 0.17), (x, ref(0, 19.5)[1], 0.17), 0.05, 6, "grey", cap0="g_orange", cap1="dark")
        b.tube((x, ref(0, 29.3)[1], 0.17), (x, ref(0, 28.5)[1], 0.17), 0.062, 6, "dark", cap0="g_orange")
    # Elongated glowing core collar (orange) around the hitbox.
    b.prism(R([(16.5, 9.5), (19.5, 12), (19.5, 17), (16.5, 19.5), (13.5, 17), (13.5, 12)]), 0.22, CORE_Z,
            "g_orange", "g_orange", taper=0.9)
    if hitbox:
        b.ngon(*HITBOX_CENTER, HITBOX_RADIUS, 6, CORE_Z, CORE_Z + 0.04, "g_yellow", "g_yellow", rot=HEX)

@themed(PLAYER_THEME)
def player():
    b = Builder()
    player_geometry(b)
    ship = b.finish("PSXv2_Player")
    ship.data.materials.append(material("PSXv2_Steel"))
    # Hitbox core: separate hexagon, exactly the collider. Ring = thin yellow hexagon just outside it.
    c = Builder()
    c.ngon(*HITBOX_CENTER, HITBOX_RADIUS, 6, CORE_Z, CORE_Z + 0.04, "g_white", "g_white", rot=HEX)
    core = c.finish("PSXv2_Player_HitboxCore")
    core.data.materials.append(material("PSXv2_HitboxCore", color=(1, 0.86, 0.25)))
    r = Builder()
    r.ring(*HITBOX_CENTER, HITBOX_RADIUS + 0.035, HITBOX_RADIUS + 0.01, 6, CORE_Z, CORE_Z + 0.03, "g_ring", "g_ring", rot=HEX)
    ring = r.finish("PSXv2_Player_HitboxRing")
    ring.data.materials.append(material("PSXv2_HitboxRing", color=(0.6, 0.25, 0.02)))
    core.parent = ship
    ring.parent = ship
    return [ship, core, ring]


# --------------------------------------------------------------------------- animation
# Enemies are a root empty + rigid parts with pivots at their hinge. Loops are keyed on
# twos (15 Hz at 30 fps) with CONSTANT interpolation: stepped motion like PS1 games,
# baked into one FBX take per enemy (Unity: legacy clip, Loop).
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


def blink(t, i):
    return {"scale": (1, 1, 1) if (i // 3) % 2 == 0 else (0.01, 0.01, 0.01)}


def recoil(at, width=0.06, dist=0.2, axis=1):
    def fn(t, i):
        d = (t - at) % 1.0
        off = dist if d < width else dist * 0.45 if d < width * 2 else 0.0
        v = [0, 0, 0]
        v[axis] = off
        return {"location": tuple(v)}
    return fn


def spin(turns):
    return lambda t, i: {"rotation": (0, 0, TAU * turns * t)}


# --------------------------------------------------------------------------- enemies
@themed(ENEMY_THEME)
def interceptor():
    """Enemy_0 'Dart': red arrowhead with canopy and wing missiles; banks, eye blinks, thruster flickers."""
    L = 24
    r = root("PSXv2_Enemy_0_Dart", L)

    def hull(b):
        b.prism([(0, -0.62), (0.52, 0.30), (0.40, 0.42), (0.14, 0.18), (0, 0.30),
                 (-0.14, 0.18), (-0.40, 0.42), (-0.52, 0.30)], 0.0, 0.14, "red", "dark", inset=0.04, rise=0.03, cap="red", taper=0.86)
        b.prism([(0, -0.44), (0.13, 0.04), (0, 0.20), (-0.13, 0.04)], 0.14, 0.30, "dark", "black", inset=0.02, rise=0.02, cap="steel", taper=0.7)
        b.prism([(0, -0.18), (0.06, -0.02), (0, 0.08), (-0.06, -0.02)], 0.30, 0.36, "glass", "black", taper=0.5)
        b.sym([(0.40, 0.30), (0.50, 0.30), (0.46, 0.52), (0.40, 0.48)], 0.05, 0.22, "steel", "black", taper=0.7)
        b.sym([(0.16, -0.20), (0.30, 0.02), (0.24, 0.08), (0.12, -0.10)], 0.14, 0.18, "hazard", "black")
        for x in (0.30, -0.30):
            b.tube((x, -0.10, 0.10), (x, 0.26, 0.10), 0.04, 6, "steel", cap1="dark")
            b.tube((x, -0.10, 0.10), (x, -0.22, 0.10), 0.04, 6, "red", r1=0.0)
    h = part("Dart_Hull", r, (0, 0, 0.1), hull)
    e = part("Dart_Eye", h, (0, -0.32, 0.30), lambda b: b.ngon(0, -0.32, 0.055, 4, 0.28, 0.33, "g_red", "g_red", rot=math.pi / 4))
    th = part("Dart_Thruster", h, (0, 0.16, 0.14), lambda b: b.box(0, 0.24, 0.12, 0.10, 0.08, 0.18, "g_orange", "g_orange"))
    animate(h, L, lambda t, i: {"rotation": (0, 0.24 * math.sin(TAU * t), 0)})
    animate(e, L, lambda t, i: {"scale": (1.7, 1.7, 1) if i in (0, 1, 6) else (1, 1, 1)})
    animate(th, L, flicker)
    return r


@themed(ENEMY_THEME)
def rotor():
    """Enemy_1 'Rotor': hex ring + lamp pods spin, the domed hub counter-rotates and pulses."""
    L = 48
    r = root("PSXv2_Enemy_1_Rotor", L)

    def spinner(b):
        b.ring(0, 0, 0.52, 0.30, 6, 0.0, 0.18, "orange", "dark", rot=math.pi / 6, taper=0.92)
        for k in range(3):
            a = k * TAU / 3 - math.pi / 2
            ca, sa = math.cos(a), math.sin(a)
            pts = [(ca * 0.12 - sa * 0.05, sa * 0.12 + ca * 0.05), (ca * 0.12 + sa * 0.05, sa * 0.12 - ca * 0.05),
                   (ca * 0.34 + sa * 0.05, sa * 0.34 - ca * 0.05), (ca * 0.34 - sa * 0.05, sa * 0.34 + ca * 0.05)]
            b.prism(pts, 0.03, 0.12, "hazard", "black")
            b.ngon(ca * 0.60, sa * 0.60, 0.11, 6, 0.0, 0.22, "dark", "steel", rot=a, taper=0.65, inset=0.02, rise=0.02, cap="g_yellow")
    s = part("Rotor_Spinner", r, (0, 0, 0), spinner)

    def hub(b):
        b.ngon(0, 0, 0.17, 8, 0.0, 0.18, "steel", "dark", rot=math.pi / 8, taper=0.8)
        b.ngon(0, 0, 0.13, 8, 0.18, 0.30, "g_orange", "dark", rot=math.pi / 8, taper=0.55)
    hb = part("Rotor_Hub", r, (0, 0, 0.12), hub)
    animate(s, L, lambda t, i: {"rotation": (0, 0, -TAU / 3 * t)})          # 3-fold symmetric -> seamless
    animate(hb, L, lambda t, i: {"rotation": (0, 0, TAU / 4 * t),            # 8-fold symmetric
                                 "scale": (1 + 0.15 * (i % 4 == 0), 1 + 0.15 * (i % 4 == 0), 1)})
    return r


@themed(ENEMY_THEME)
def gunboat():
    """Enemy_2 'Gunboat': twin-hull catamaran with bridge; engines flicker, the turret spins while firing (in game)."""
    L = 60
    r = root("PSXv2_Enemy_2_Gunboat", L)

    def hull(b):
        for x in (1.25, -1.25):
            b.prism([(x - 0.42, 1.5), (x + 0.42, 1.5), (x + 0.48, -0.9), (x + 0.22, -1.72), (x - 0.22, -1.72), (x - 0.48, -0.9)],
                    0.0, 0.48, "teal", "dark", inset=0.07, rise=0.05, cap="teal", taper=0.82)
            b.box(x, 1.36, 0.55, 0.22, 0.10, 0.40, "dark", "dark")
            b.box(x, -0.20, 0.30, 0.90, 0.53, 0.62, "vent", "black", taper=0.85)
            b.tube((x * 1.2, -0.70, 0.30), (x * 1.2, -1.85, 0.30), 0.07, 6, "dark", cap1="black")
            b.tube((x, 0.4, 0.56), (x, 1.2, 0.56), 0.08, 6, "ribs", cap0="dark", cap1="dark")
        b.box(0, 0.25, 1.70, 1.50, 0.08, 0.34, "dark", "dark", inset=0.08, rise=0.03, cap="grating")
        b.box(0, -0.70, 1.80, 0.25, 0.10, 0.30, "hazard", "black")
        b.box(0, 1.15, 0.9, 0.5, 0.34, 0.70, "steel", "dark", taper=0.8, inset=0.06, rise=0.02, cap="windows")
        b.tube((0.3, 1.25, 0.70), (0.3, 1.25, 1.05), 0.02, 4, "chrome")
    part("Gunboat_Hull", r, (0, 0, 0), hull, cell=0.6)
    for x, side in ((1.25, "R"), (-1.25, "L")):
        eng = part(f"Gunboat_Engine_{side}", r, (x, 1.47, 0.25), lambda b, x=x: b.box(x, 1.55, 0.55, 0.16, 0.10, 0.40, "g_cyan", "g_cyan"))
        animate(eng, L, lambda t, i, o=3 * (side == "L"): flicker(t, i + o))

    def turret(b):
        b.ngon(0, 0.10, 0.50, 8, 0.36, 0.62, "teal", "dark", rot=math.pi / 8, taper=0.8, inset=0.06, rise=0.03, cap="steel")
        b.ngon(0, 0.18, 0.26, 8, 0.62, 0.78, "steel", "dark", rot=math.pi / 8, taper=0.6)
        b.ngon(0, 0.18, 0.08, 6, 0.78, 0.82, "g_red", "g_red")
    tu = part("Gunboat_Turret", r, (0, 0.10, 0.50), turret)
    for x in (0.16, -0.16):
        part(f"Gunboat_Barrel_{'R' if x > 0 else 'L'}", tu, (x, -0.15, 0.52),
                 lambda b, x=x: (b.tube((x, -0.10, 0.52), (x, -1.15, 0.52), 0.06, 6, "dark", cap1="black"),
                                 b.tube((x, -1.0, 0.52), (x, -1.2, 0.52), 0.085, 6, "steel", cap1="black")))
    # Turret and barrels are driven in game by PSXShooterVisual (Biggie fires circular spreads).
    return r


WARDEN_HULL = [(0.0, -3.6), (1.2, -2.6), (3.0, -2.1), (4.6, -2.2), (5.6, -1.3), (5.9, 0.0), (5.9, 1.8),
               (5.3, 3.2), (4.2, 4.3), (2.6, 4.5), (1.4, 4.4), (0.5, 3.6), (-0.5, 3.6), (-1.4, 4.4),
               (-2.6, 4.5), (-4.2, 4.3), (-5.3, 3.2), (-5.9, 1.8), (-5.9, 0.0), (-5.6, -1.3), (-4.6, -2.2),
               (-3.0, -2.1), (-1.2, -2.6)]


@themed(ENEMY_THEME)
def warden():
    """Enemy_3 'Freight Warden' boss: layered purple carrier with a stepped white spine, bridge
    tower and radar, two drum turrets with twin cannons, four point-defence turrets, pipe runs,
    engine block with flickering plumes, blinking lamps and a pulsing reactor.
    Footprint follows the stage1boss polygon collider (~12 x 8)."""
    L = 90
    r = root("PSXv2_Enemy_3_Warden", L)

    def hull(b):
        b.prism(WARDEN_HULL, 0.0, 0.5, "purple", "dark", inset=0.3, rise=0.1, cap="deep", taper=0.95)
        deck = [(x * 0.72, y * 0.72 + 0.35) for x, y in WARDEN_HULL]
        b.prism(deck, 0.6, 1.0, "steel", "dark", inset=0.15, rise=0.04, cap="steel", taper=0.93)
        # stepped white spine, number plate, beak with red sensor eyes
        b.prism([(0, -3.25), (1.1, -1.6), (1.2, 2.7), (-1.2, 2.7), (-1.1, -1.6)], 1.0, 1.5, "white", "steel",
                inset=0.12, rise=0.05, cap="white", taper=0.86)
        b.box(0, -1.25, 0.8, 0.5, 1.55, 1.6, "number", "black")
        b.prism([(0, -3.68), (0.62, -2.55), (-0.62, -2.55)], 1.0, 1.35, "dark", "black", taper=0.7)
        for x in (0.22, -0.22):
            b.box(x, -2.85, 0.14, 0.10, 1.32, 1.4, "g_red", "g_red")
        # bridge tower with lit windows and a screen strip
        b.box(0, 1.55, 1.5, 1.9, 1.55, 2.05, "dark", "steel", taper=0.8, inset=0.1, rise=0.03, cap="windows")
        b.box(0, 0.62, 1.0, 0.12, 1.6, 1.9, "screen", "black", taper=0.9)
        # leading-edge hazard trims and red caution armour on the lobes
        b.sym([(1.2, -2.6), (3.0, -2.1), (3.0, -1.82), (1.2, -2.32)], 0.5, 0.64, "hazard", "black")
        b.sym([(4.6, -2.05), (5.6, -1.2), (5.35, -0.95), (4.4, -1.7)], 0.5, 0.82, "caution", "dark", taper=0.85)
        b.sym([(5.0, 2.4), (5.7, 1.6), (5.75, 1.9), (5.2, 2.9)], 0.5, 0.8, "caution", "dark", taper=0.85)
        # drum turret rings, point-defence sponsons
        for s in (1, -1):
            b.ngon(3.6 * s, 1.2, 1.5, 8, 0.9, 1.05, "black", "dark", rot=math.pi / 8, taper=0.95)
            for px, py in ((5.0, 0.6), (2.3, -1.75)):
                b.ngon(px * s, py, 0.48, 8, 0.5, 0.95, "dark", "steel", rot=math.pi / 8, taper=0.8)
            # pipe runs along the flanks and to the engines
            b.tube((4.75 * s, -1.0, 0.75), (4.75 * s, 2.9, 0.75), 0.14, 6, "ribs", cap0="dark", cap1="dark")
            b.tube((4.45 * s, -0.6, 0.72), (4.45 * s, 2.6, 0.72), 0.10, 6, "ribs", cap0="dark", cap1="dark")
            b.tube((3.0 * s, 2.7, 0.95), (1.8 * s, 3.6, 0.95), 0.12, 6, "ribs", cap0="dark", cap1="dark")
            b.box(2.0 * s, -2.05, 0.55, 0.6, 0.5, 0.85, "dark", "steel", inset=0.06, rise=0.02, cap="vent", taper=0.85)
            b.box(4.3 * s, 3.85, 1.0, 0.9, 0.5, 1.1, "steel", "dark", inset=0.1, rise=0.03, cap="vent", taper=0.85)
        # engine block and nozzles
        b.box(0, 3.7, 3.4, 1.1, 0.6, 1.25, "dark", "dark", taper=0.88, inset=0.1, rise=0.02, cap="grating")
        for x in (-1.2, -0.4, 0.4, 1.2):
            b.tube((x, 4.1, 0.9), (x, 4.55, 0.9), 0.30, 8, "dark", cap1="g_magenta", r1=0.34)
        b.box(0, 2.95, 2.4, 0.3, 1.0, 1.15, "hazard", "black")
    part("Warden_Hull", r, (0, 0, 0), hull, cell=1.0)

    rc = part("Warden_Reactor", r, (0, 0.25, 1.62),
              lambda b: (b.ring(0, 0.25, 0.85, 0.66, 8, 1.55, 1.72, "hazard", "black", rot=math.pi / 8),
                         b.ngon(0, 0.25, 0.55, 8, 1.55, 1.85, "g_magenta", "black", rot=math.pi / 8, taper=0.7)))
    animate(rc, L, lambda t, i: {"rotation": (0, 0, TAU / 8 * t), "scale": (1.12, 1.12, 1) if i % 5 == 0 else (1, 1, 1)})
    radar = part("Warden_Radar", r, (0, 1.9, 2.1),
                 lambda b: (b.tube((0, 1.9, 2.05), (0, 1.9, 2.35), 0.06, 4, "steel"),
                            b.box(0, 1.9, 1.1, 0.16, 2.35, 2.45, "chrome", "dark", taper=0.9),
                            b.box(0.5, 1.9, 0.1, 0.3, 2.35, 2.5, "g_red", "g_red")))
    animate(radar, L, spin(1))
    lamps = part("Warden_Lamps", r, (0, 0, 0.9), lambda b: [
        b.box(x * s, y, 0.18, 0.18, 0.82, 0.95, "g_yellow", "g_yellow")
        for s in (1, -1) for x, y in ((5.55, 1.75), (4.3, 4.35), (1.3, -2.45), (0.55, 3.5))])
    animate(lamps, L, blink)
    # Shooting parts are driven in game by PSXShooterVisual from the boss's real shooters:
    # the drums carry the circular spreads (spin while spraying), the forward turrets carry the
    # aimed bursts (track the player). They have no baked animation.
    for s, side in ((1, "R"), (-1, "L")):
        cx, cy = 3.6 * s, 1.2

        def drum(b, cx=cx, cy=cy):
            b.ngon(cx, cy, 1.3, 8, 1.05, 1.5, "purple", "dark", rot=math.pi / 8, taper=0.82, inset=0.14, rise=0.05, cap="deep")
            b.ngon(cx, cy, 0.62, 8, 1.55, 1.85, "steel", "black", rot=math.pi / 8, taper=0.7, inset=0.1, rise=0.04, cap="g_magenta")
            for k in range(8):
                a = k * TAU / 8
                d = Vector((math.cos(a), math.sin(a), 0))
                b.tube((cx + d.x * 0.95, cy + d.y * 0.95, 1.4), (cx + d.x * 1.55, cy + d.y * 1.55, 1.4), 0.11, 6, "dark", cap1="black")
        part(f"Warden_Drum_{side}", r, (cx, cy, 1.1), drum, cell=1.0)

        ax, ay = 1.64 * s, -0.57

        def aim(b, ax=ax, ay=ay):
            b.ngon(ax, ay, 0.42, 6, 1.0, 1.32, "steel", "dark", rot=math.pi / 6, taper=0.75, inset=0.06, rise=0.02, cap="dark")
            b.box(ax, ay - 0.28, 0.5, 0.42, 1.12, 1.42, "purple", "dark", taper=0.8)
            for dx in (-0.12, 0.12):
                b.tube((ax + dx, ay - 0.3, 1.3), (ax + dx, ay - 1.25, 1.3), 0.07, 6, "dark", cap1="black")
        part(f"Warden_Aim_{side}", r, (ax, ay, 1.1), aim)
        for px, py in ((5.0, 0.6), (2.3, -1.75)):
            part(f"Warden_Sensor_{side}{int(px)}", r, (px * s, py, 1.0),
                 lambda b, px=px * s, py=py: b.ngon(px, py, 0.3, 6, 0.95, 1.15, "steel", "dark", taper=0.6))
    plumes = part("Warden_Plumes", r, (0, 4.55, 0.9),
                  lambda b: [b.box(x, 4.75, 0.42, 0.4, 0.75, 1.05, "g_magenta", "g_magenta", taper=0.6) for x in (-1.2, -0.4, 0.4, 1.2)])
    animate(plumes, L, flicker)
    return r


# --------------------------------------------------------------------------- items
def rot_z(deg, about=(0, 0, 0)):
    about = Vector(about)
    return Matrix.Translation(about) @ Matrix.Rotation(math.radians(deg), 4, "Z") @ Matrix.Translation(-about)


def scl(sx, sy, sz, about=(0, 0, 0)):
    about = Vector(about)
    return Matrix.Translation(about) @ Matrix.Diagonal((sx, sy, sz, 1)) @ Matrix.Translation(-about)


def item_life(b):    # red medical kit with a white cross face and black handle
    b.box(0, -0.05, 0.92, 0.66, -0.2, 0.2, "medcross", "magnet", taper=0.96)
    b.box(0, 0.35, 0.44, 0.10, -0.07, 0.07, "blackplastic", "blackplastic")
    for x in (-0.18, 0.18):
        b.box(x, 0.30, 0.08, 0.12, -0.07, 0.07, "blackplastic", "blackplastic")


def item_mana(b):    # blue battery with a bolt label and chrome terminals
    b.box(0, -0.05, 0.52, 0.82, -0.18, 0.18, "battery", "plastic", taper=0.96)
    b.box(0, 0.40, 0.52, 0.06, -0.18, 0.18, "chrome", "chrome")
    for x in (-0.13, 0.13):
        b.tube((x, 0.43, 0), (x, 0.55, 0), 0.06, 6, "chrome", cap1="steel")


def item_money(b):   # gold coin with a $ face on both sides
    b.ngon(0, 0, 0.46, 12, -0.06, 0.06, "coin", "gold", bottom="coin")
    b.ring(0, 0, 0.48, 0.42, 12, -0.075, 0.075, "gold", "brass")


def item_rabbit(b):  # white rabbit paw with toes and a gold cap
    b.prism([(0, -0.34), (0.2, -0.22), (0.17, 0.25), (0.1, 0.42), (-0.1, 0.42), (-0.17, 0.25), (-0.2, -0.22)],
            -0.16, 0.16, "ceramic", "ceramic", taper=0.8)
    for x, y in ((-0.13, -0.36), (0, -0.42), (0.13, -0.36)):
        b.sphere((x, y, 0.02), 0.09, "ceramic", seg=6, rings=4)
    b.tube((0, 0.40, 0), (0, 0.58, 0), 0.13, 8, "chrome", cap0="steel", cap1="steel")


def item_blank(b):   # brass cartridge with a crimped (blank) tip
    b.tube((0, -0.50, 0), (0, -0.44, 0), 0.19, 8, "brass", cap0="brass", cap1="brass")
    b.tube((0, -0.44, 0), (0, 0.14, 0), 0.16, 8, "brass")
    b.tube((0, 0.14, 0), (0, 0.48, 0), 0.16, 8, "gold", r1=0.0)


def item_twerp(b):   # little helper robot
    b.box(0, 0.24, 0.52, 0.38, -0.2, 0.2, "ceramic", "steel", taper=0.92)
    for x in (-0.11, 0.11):
        b.box(x, 0.26, 0.10, 0.08, 0.19, 0.24, "g_cyan", "g_cyan")
    b.box(0, -0.13, 0.58, 0.38, -0.17, 0.17, "steel", "dark", taper=0.92)
    b.box(0, -0.12, 0.24, 0.16, 0.16, 0.2, "screen", "black")
    for x in (-0.15, 0.15):
        b.box(x, -0.42, 0.12, 0.22, -0.1, 0.1, "dark", "dark")
    for x in (-0.36, 0.36):
        b.box(x, -0.12, 0.12, 0.30, -0.08, 0.08, "dark", "dark")
    b.tube((0, 0.43, 0), (0, 0.6, 0), 0.02, 4, "chrome")
    b.sphere((0, 0.62, 0), 0.05, "g_cyan", seg=6, rings=4)


def item_magnet(b):  # red horseshoe magnet with chrome poles
    for x in (-0.26, 0.26):
        b.box(x, 0.05, 0.2, 0.56, -0.12, 0.12, "magnet", "magnet")
        b.box(x, 0.40, 0.2, 0.14, -0.12, 0.12, "chrome", "chrome")
    for i in range(5):
        a0, a1 = math.pi + i * math.pi / 5, math.pi + (i + 1) * math.pi / 5
        p = lambda r, a: (r * math.cos(a), -0.23 + r * math.sin(a))
        b.prism([p(0.16, a0), p(0.36, a0), p(0.36, a1), p(0.16, a1)], -0.12, 0.12, "magnet", "magnet")


def item_ak(b):      # assault rifle side view: steel receiver, wood stock and grip, curved magazine
    b.box(0.0, 0.0, 0.70, 0.16, -0.06, 0.06, "blackplastic", "dark")
    b.prism([(-0.35, -0.06), (-0.35, 0.07), (-0.72, 0.02), (-0.72, -0.16)], -0.05, 0.05, "rust", "rust")
    b.tube((0.35, 0.03, 0), (0.86, 0.03, 0), 0.03, 6, "chrome", cap1="black")
    b.tube((0.35, 0.10, 0), (0.62, 0.10, 0), 0.025, 6, "dark")
    b.box(0.42, -0.01, 0.16, 0.08, -0.05, 0.05, "rust", "rust")
    m = b.mark()
    b.box(0.12, -0.20, 0.13, 0.28, -0.045, 0.045, "dark", "black")
    b.xform(m, rot_z(-14, (0.12, -0.08, 0)))
    m = b.mark()
    b.box(-0.18, -0.16, 0.09, 0.2, -0.04, 0.04, "rust", "rust")
    b.xform(m, rot_z(12, (-0.18, -0.06, 0)))


def item_caffeine(b):  # white mug full of coffee with a handle and steam
    b.tube((0, -0.32, 0), (0, 0.28, 0), 0.30, 10, "ceramic", cap0="ceramic", cap1="coffee")
    for (x0, y0), (x1, y1) in (((0.28, 0.16), (0.46, 0.16)), ((0.46, 0.16), (0.46, -0.18)), ((0.46, -0.18), (0.28, -0.18))):
        b.tube((x0, y0, 0), (x1, y1, 0), 0.05, 6, "ceramic", cap0="ceramic", cap1="ceramic")
    for x, h in ((-0.08, 0.5), (0.08, 0.44)):
        b.box(x, 0.30 + h / 2, 0.04, h, -0.02, 0.02, "ceramic", "ceramic")


def item_protein(b):  # protein tub: white body, blue lid, "P" label
    b.tube((0, -0.36, 0), (0, 0.24, 0), 0.34, 10, "ceramic", cap0="ceramic")
    b.tube((0, 0.24, 0), (0, 0.38, 0), 0.36, 10, "plastic", cap1="plastic")
    b.box(0, -0.06, 0.36, 0.36, 0.28, 0.36, "label_p", "ceramic")


def item_moon(b):
    b.sphere((0, 0, 0), 0.46, "moon", seg=10, rings=7)


def item_babyyou(b):  # a miniature of the player ship, standing up and facing the camera
    m = b.mark()
    themed(PLAYER_THEME)(player_geometry)(b, hitbox=True)
    b.xform(m, scl(0.5, 0.5, 0.5))


def item_electricity(b):  # glowing lightning bolt
    b.prism([(0.10, 0.52), (-0.22, 0.02), (-0.02, 0.02), (-0.14, -0.52), (0.24, 0.06), (0.03, 0.06), (0.20, 0.52)],
            -0.07, 0.07, "g_yellow", "gold", bottom="g_yellow")


def item_arm(b):     # an extra arm: sleeve, upper arm, forearm and an open hand
    b.tube((-0.44, 0.34, 0), (-0.32, 0.22, 0), 0.13, 8, "dark", cap0="dark", cap1="dark")
    b.tube((-0.32, 0.22, 0), (0.0, -0.08, 0), 0.10, 8, "skin")
    b.sphere((0.0, -0.08, 0), 0.10, "skin", seg=8, rings=4)
    b.tube((0.0, -0.08, 0), (0.30, 0.20, 0), 0.09, 8, "skin")
    m = b.mark()
    b.box(0.38, 0.30, 0.16, 0.18, -0.05, 0.05, "skin", "skin")
    for k in range(4):
        b.box(0.32 + k * 0.045, 0.44, 0.035, 0.12, -0.035, 0.035, "skin", "skin")
    b.box(0.27, 0.30, 0.04, 0.12, -0.035, 0.035, "skin", "skin")
    b.xform(m, rot_z(-40, (0.36, 0.30, 0)))


def item_angrymoon(b):  # moon with a scowl: brows in a V (inner ends low), red eyes, gritted mouth
    b.sphere((0, 0, 0), 0.46, "moon", seg=10, rings=7)
    for s in (1, -1):
        m = b.mark()
        b.box(0.15 * s, 0.16, 0.22, 0.055, 0.37, 0.48, "black", "black")
        b.xform(m, rot_z(28 * s, (0.15 * s, 0.16, 0)))
        b.box(0.14 * s, 0.04, 0.08, 0.06, 0.40, 0.47, "g_red", "g_red")
    b.box(0, -0.2, 0.28, 0.07, 0.35, 0.45, "black", "black")
    for x in (-0.07, 0.0, 0.07):
        b.box(x, -0.2, 0.025, 0.07, 0.36, 0.455, "ceramic", "ceramic")


def item_telekinesis(b):  # bent spoon
    b.tube((0, -0.5, 0), (0, 0.02, 0), 0.04, 6, "chrome", cap0="chrome")
    b.tube((0, 0.02, 0), (0.14, 0.16, 0), 0.04, 6, "chrome")
    b.sphere((0.2, 0.3, 0), 0.15, "chrome", seg=8, rings=5, scale=(0.9, 1.3, 0.45))


def item_threemoons(b):
    for x, y in ((-0.26, 0.2), (0.26, 0.2), (0, -0.24)):
        b.sphere((x, y, 0), 0.25, "moon", seg=8, rings=6)


ITEMS = {
    "life": item_life, "mana": item_mana, "money": item_money, "RabbitsFoot": item_rabbit,
    "BlankBullet": item_blank, "TwerpItem": item_twerp, "Magnet": item_magnet, "AK45Item": item_ak,
    "Caffeine": item_caffeine, "ProteinSupplement": item_protein, "AMoonItem": item_moon,
    "BabyYouItem": item_babyyou, "ElectricityItem": item_electricity, "AnotherArm": item_arm,
    "AngryMoonItem": item_angrymoon, "Telekinesis": item_telekinesis, "ThreeMoonsItem": item_threemoons,
}


def items():
    r = bpy.data.objects.new("PSXv2_Items", None)
    bpy.context.scene.collection.objects.link(r)
    for k, fn in ITEMS.items():
        b = Builder(closed=True)
        fn(b)
        ob = b.finish("Item_" + k)
        ob.data.materials.append(material("PSXv2_Steel"))
        ob.parent = r
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
        if o.name.startswith(("PSXv2_", "Dart_", "Rotor_", "Gunboat_", "Warden_", "Item_")) or o.name == "Cube":
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
              (warden, "PSXv2_Enemy_3_Warden", (0, 12)), (items, "PSXv2_Items", (-8, -4))]
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
    report["items"] = {o.name: tris([o]) for o in bpy.data.objects if o.name.startswith("Item_")}
    return report


if not globals().get("PSXV2_LIBRARY"):   # build_field.py loads this file only for its helpers
    result = main()
    print(result)
