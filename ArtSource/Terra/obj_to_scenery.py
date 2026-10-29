"""Bake a Terra scenery export (model.obj) into Assets/Resources/TerraModels/<name>.txt for the unlit LevelScenery shader.

Lighting is baked into vertex colours: each polygon gets its material's Kd lit by a sun from the upper left.
Materials whose name starts with glow_ (lamps, screens) keep their full colour.
Blender space: X right, Y screen up, +Z toward the camera. Output space flips Z (-Z toward the camera).
Each output line is one polygon: r g b, then x y z per corner.

Usage: python obj_to_scenery.py path/to/model.obj Hangar
"""
import math
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SUN = (-0.45, 0.55, 0.70)
AMBIENT, DIFFUSE = 0.55, 0.62


def normalize(v):
    length = math.sqrt(sum(c * c for c in v)) or 1
    return tuple(c / length for c in v)


def read_materials(obj_path):
    colours = {}
    name = None
    for raw in Path(obj_path).with_suffix(".mtl").read_text(encoding="utf-8").splitlines():
        parts = raw.split()
        if not parts:
            continue
        if parts[0] == "newmtl":
            name = parts[1]
        elif parts[0] == "Kd" and name:
            colours[name] = tuple(float(v) for v in parts[1:4])
    return colours


def bake(obj_path, name):
    colours = read_materials(obj_path)
    sun = normalize(SUN)
    vertices, lines = [], []
    material = None
    for raw in Path(obj_path).read_text(encoding="utf-8").splitlines():
        parts = raw.split()
        if not parts:
            continue
        if parts[0] == "v":
            vertices.append(tuple(float(v) for v in parts[1:4]))
        elif parts[0] == "usemtl":
            material = parts[1]
        elif parts[0] == "f":
            corners = [vertices[int(p.split("/")[0]) - 1] for p in parts[1:]]
            a, b, c = corners[0], corners[1], corners[2]
            u = [b[i] - a[i] for i in range(3)]
            w = [c[i] - a[i] for i in range(3)]
            normal = normalize((u[1] * w[2] - u[2] * w[1], u[2] * w[0] - u[0] * w[2], u[0] * w[1] - u[1] * w[0]))
            base = colours.get(material, (0.5, 0.5, 0.5))
            if material and material.startswith("glow_"):
                light = 1.0
            else:
                # Faces seen from above lose a little light as they tilt away, sunlit sides gain it.
                light = AMBIENT + DIFFUSE * max(0.0, sum(normal[i] * sun[i] for i in range(3)))
            rgb = [min(1.0, ch * light) for ch in base]
            # Mirroring Z flips handedness; reverse the corners to keep the winding.
            flipped = [(x, y, -z) for x, y, z in reversed(corners)]
            lines.append(" ".join([f"{ch:.3f}" for ch in rgb] + [f"{v:.4f}" for corner in flipped for v in corner]))
    out = ROOT / "Assets/Resources/TerraModels" / (name + ".txt")
    out.write_text("scenery\n" + "\n".join(lines) + "\n", encoding="utf-8")
    print(f"{name}: {len(lines)} polygons -> {out.relative_to(ROOT)}")


if __name__ == "__main__":
    bake(sys.argv[1], sys.argv[2])
