"""Closed, flat-shaded solids for the Terra item models. Camera faces +Z."""

from collections import Counter
from math import cos, isfinite, pi, radians, sin, sqrt


def add(a, b):
    return tuple(x + y for x, y in zip(a, b))


def sub(a, b):
    return tuple(x - y for x, y in zip(a, b))


def scale(a, amount):
    return tuple(value * amount for value in a)


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def unit(vector):
    return scale(vector, 1 / sqrt(dot(vector, vector)))


def average(a, b, fraction):
    return tuple(x + (y - x) * fraction for x, y in zip(a, b))


def colour(value):
    value = value.lstrip("#")
    if len(value) != 6:
        raise ValueError(f"Expected six-digit colour, got {value!r}")
    return tuple(int(value[i:i + 2], 16) / 255 for i in (0, 2, 4))


def axial(u, v, length, axis):
    if axis == "z":
        return (u, v, length)
    if axis == "y":
        return (u, length, -v)
    if axis == "x":
        return (length, u, v)
    raise ValueError(f"Unsupported axis {axis!r}")


def pairs(points):
    return zip(points, points[1:] + points[:1])


def panel_normal(points):
    origin = points[0]
    normal = (0, 0, 0)
    for i in range(1, len(points) - 1):
        normal = add(normal, cross(sub(points[i], origin), sub(points[i + 1], origin)))
    return normal


class Model:
    def __init__(self, primary_hex, accent_hex, light_hex="d5dddc", dark_hex="24303d"):
        primary, accent, light, dark = map(colour, (primary_hex, accent_hex, light_hex, dark_hex))
        self.palette = [
            primary, average(primary, light, .35), average(primary, dark, .40), accent,
            dark, light, colour("76848c"), average(accent, colour("fff5d0"), .49),
            colour("bf915c"), colour("507f94"), colour("121820"), colour("373b42"),
            colour("aab7b9"), colour("974849"), colour("577470"), colour("fcdbc5"),
        ]
        self.faces = []
        self.opacity = {}

    def paint(self, tile, hex_value):
        self.palette[tile] = colour(hex_value)

    def translucent(self, tile, alpha):
        if not 0 <= tile < len(self.palette) or not 0 < alpha < 1:
            raise ValueError("Translucent palette tiles need an opacity between zero and one")
        self.opacity[tile] = alpha

    def face(self, tile, points_xyz):
        points = tuple(dict.fromkeys(tuple(round(v, 5) for v in p) for p in points_xyz))
        if len(points) < 3:
            return
        if not 0 <= tile < len(self.palette) or not all(isfinite(v) for p in points for v in p):
            raise ValueError(f"Invalid palette or geometry: {tile} {points}")
        normal = panel_normal(points)
        area = sqrt(dot(normal, normal))
        if area < 1e-8:
            raise ValueError(f"Zero-area face: {points}")
        if any(abs(dot(sub(p, points[0]), normal)) / area > .0001 for p in points):
            raise ValueError(f"Non-planar face: {points}")
        self.faces.append((tile, points))

    def _shell(self, rings, body, base, top, bands=None):
        start = len(self.faces)
        if base is not None:
            self.face(base, tuple(reversed(rings[0])))
        if top is not None:
            self.face(top, rings[-1])
        for band, (lower, upper) in enumerate(zip(rings, rings[1:])):
            for i in range(len(lower)):
                following = (i + 1) % len(lower)
                tile = bands[band] if bands is not None else body
                self.face(tile, (lower[i], lower[following], upper[following], upper[i]))
        if base is not None and top is not None:
            self._verify_shell(start)

    def _verify_shell(self, start):
        faces = self.faces[start:]
        edges = Counter(edge for _, points in faces for edge in pairs(points))
        if not edges or any(count != 1 or edges[(b, a)] != 1 for (a, b), count in edges.items()):
            raise ValueError("Open or incorrectly wound primitive")
        origin = faces[0][1][0]
        signed_volume = sum(dot(sub(points[0], origin), panel_normal(points)) for _, points in faces)
        if signed_volume <= 0:
            raise ValueError("Inside-out primitive")

    def prism(self, outline_xy, depth, tile=0, center=(0, 0, 0), side=None, back=None):
        outline = tuple(tuple(p) for p in outline_xy)
        signed_area = sum(a[0] * b[1] - a[1] * b[0] for a, b in pairs(outline))
        if signed_area < 0:
            outline = tuple(reversed(outline))
        rings = [tuple(add(center, (x, y, z)) for x, y in outline) for z in (-depth / 2, depth / 2)]
        self._shell(rings, tile if side is None else side, tile, tile if back is None else back)

    def box(self, center_xyz, size_xyz, tile=0, bevel=0, angle=0):
        x, y, z = (v / 2 for v in size_xyz)
        a = radians(angle)
        c, s = cos(a), sin(a)
        if bevel <= 0:
            outline = [(-x, -y), (x, -y), (x, y), (-x, y)]
            self.prism([(u * c - v * s, u * s + v * c) for u, v in outline], z * 2, tile, center_xyz)
            return
        edge = min(bevel, min(x, y, z) * .46)
        rings = []
        for depth, inset in ((-z, edge), (-z + edge, 0), (z - edge, 0), (z, edge)):
            hx, hy = x - inset, y - inset
            cut = edge * .75
            outline = [(-hx + cut, -hy), (hx - cut, -hy), (hx, -hy + cut), (hx, hy - cut),
                       (hx - cut, hy), (-hx + cut, hy), (-hx, hy - cut), (-hx, -hy + cut)]
            rings.append(tuple(add(center_xyz, (u * c - v * s, u * s + v * c, depth)) for u, v in outline))
        self._shell(rings, tile, tile, tile)

    def lathe(self, profile_yr, tile=0, center=(0, 0, 0), sides=8, axis="y", caps=True, bands=None):
        directions = [(cos(2 * pi * i / sides), sin(2 * pi * i / sides)) for i in range(sides)]
        rings = [tuple(add(center, axial(r * c, r * s, y, axis)) for c, s in directions) for y, r in profile_yr]
        self._shell(rings, tile, tile if caps else None, tile if caps else None, bands)

    def ring(self, center_xyz, outer, inner, depth, tile=0, sides=12, axis="z", start=0, sweep=360):
        first = len(self.faces)
        angles = [radians(start + sweep * i / sides) for i in range(sides + 1)]

        def arc(radius, height):
            return [add(center_xyz, axial(radius * cos(a), radius * sin(a), height, axis)) for a in angles]

        outer_lo, outer_hi = arc(outer, -depth / 2), arc(outer, depth / 2)
        inner_lo, inner_hi = arc(inner, -depth / 2), arc(inner, depth / 2)
        for i in range(sides):
            j = i + 1
            self.face(tile, (outer_lo[i], outer_lo[j], outer_hi[j], outer_hi[i]))
            self.face(tile, (inner_lo[j], inner_lo[i], inner_hi[i], inner_hi[j]))
            self.face(tile, (outer_lo[j], outer_lo[i], inner_lo[i], inner_lo[j]))
            self.face(tile, (outer_hi[i], outer_hi[j], inner_hi[j], inner_hi[i]))
        if abs(sweep - 360) > .00001:
            self.face(tile, (outer_lo[0], outer_hi[0], inner_hi[0], inner_lo[0]))
            self.face(tile, (inner_lo[-1], inner_hi[-1], outer_hi[-1], outer_lo[-1]))
        self._verify_shell(first)

    def strut(self, start_xyz, end_xyz, radius, tile=0, sides=6, end_radius=None):
        axis = unit(sub(end_xyz, start_xyz))
        reference = (0, 0, 1) if abs(axis[2]) < .9 else (0, 1, 0)
        u = unit(cross(reference, axis))
        v = cross(axis, u)
        perimeter = [add(scale(u, cos(2 * pi * i / sides)), scale(v, sin(2 * pi * i / sides))) for i in range(sides)]
        rings = [tuple(add(center, scale(radial, r)) for radial in perimeter) for center, r in
                 ((start_xyz, radius), (end_xyz, radius if end_radius is None else end_radius))]
        self._shell(rings, tile, tile, tile)

    def gem(self, center_xyz, radii_xyz, tile=0, sides=6, angle=0):
        rx, ry, rz = radii_xyz
        angles = [radians(angle) + 2 * pi * i / sides for i in range(sides)]
        rings = [tuple(add(center_xyz, (rx * r * cos(a), ry * height, -rz * r * sin(a))) for a in angles)
                 for height, r in ((-1, 0), (-.46, .74), (.15, 1), (.68, .57), (1, 0))]
        self._shell(rings, tile, tile, tile)

    def validate(self):
        if not self.faces:
            raise ValueError("Model is empty")
        points = [p for _, points in self.faces for p in points]
        spans = [max(p[axis] for p in points) - min(p[axis] for p in points) for axis in range(3)]
        if min(spans) <= .02 or max(spans) > 2.3 or len(self.faces) > 2400:
            raise ValueError(f"Invalid dimensions or face budget: {spans}, {len(self.faces)} faces")
        if len(set(self.faces)) != len(self.faces):
            raise ValueError("Duplicated coplanar faces")

    def bake(self):
        self.validate()
        lines = ["palette " + " ".join(f"{value:.5f}" for rgb in self.palette for value in rgb)]
        lines.extend(f"alpha {tile} {value:.5f}" for tile, value in sorted(self.opacity.items()))
        # Keep glass after opaque components so the casing and contents are behind it.
        for tile, points in sorted(self.faces, key=lambda face: face[0] in self.opacity):
            corners = [
                point for i, point in enumerate(points)
                if dot((turn := cross(sub(point, points[i - 1]), sub(points[(i + 1) % len(points)], point))), turn) > 1e-14
            ]
            if len(corners) < 3:
                raise ValueError(f"Collinear face: {points}")
            lines.append(f"{tile} " + " ".join(f"{v:.5f}" for p in corners for v in p))
        return "\n".join(lines) + "\n"
