"""Bake a Terra Blender export (model.obj) into Assets/Resources/TerraModels/<name>.txt.

Blender space: X right, Y screen up, +Z toward the camera.
Hull space: X right, Y screen up, -Z toward the camera, clockwise front faces.
Materials must be named tile_NN (the palette tile of the fleet atlas).
With --palette, the colours of those materials (Kd in the .mtl) become the model's own atlas,
written as a first "palette" line of sixteen r g b triples.
cockpit_glass.json identifies each named pane by lower/upper hull-space bounds and palette tiles.
An optional parts.json next to the .obj lists animated objects:
[{"object": "Rotor_L", "motion": "spin" | "swing", "axis": [x, y, z], "pivot": [x, y, z],
  "speed": degrees per second (spin) or cycles per second (swing), "amplitude": degrees (swing)}]

Usage: python obj_to_hull.py path/to/model.obj Hull50 [--palette]
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def read_palette(obj_path):
    palette = [(0.53, 0.61, 0.62)] * 16
    mtl = Path(obj_path).with_suffix(".mtl")
    tile = None
    for raw in mtl.read_text(encoding="utf-8").splitlines():
        parts = raw.split()
        if not parts:
            continue
        if parts[0] == "newmtl":
            match = re.search(r"tile_(\d+)", parts[1])
            tile = int(match.group(1)) if match else None
        elif parts[0] == "Kd" and tile is not None and tile < 16:
            palette[tile] = tuple(float(v) for v in parts[1:4])
    return palette


def flip(v):
    return (v[0], v[1], -v[2])


def glazing(name, lines):
    """Tag the authored panes, retaining their palette colours and the original face order."""
    panels = json.loads(Path(__file__).with_name("cockpit_glass.json").read_text()).get(name)
    if not panels:
        return lines
    result, previous = [], None
    for line in lines:
        fields = line.split()
        panel = None
        if fields and fields[0].isdigit():
            tile = int(fields[0])
            coords = [float(v) for v in fields[1:]]
            for low, high, tiles in panels.values():
                if tile in tiles and all(low[i % 3] - .0002 <= v <= high[i % 3] + .0002 for i, v in enumerate(coords)):
                    panel = (low, high)
                    break
        if panel != previous:
            if panel:
                low, high = panel
                center = [(low[i] + high[i]) / 2 for i in (0, 1)]
                half = [max(.001, (high[i] - low[i]) / 2) for i in (0, 1)]
                result.append("glass " + " ".join(f"{v:.5f}" for v in center + half))
            else:
                result.append("paint")
            previous = panel
        result.append(line)
    return result


def bake(obj_path, name, palette=False):
    obj_path = Path(obj_path)
    spec_path = obj_path.with_name("parts.json")
    specs = {p["object"]: p for p in json.loads(spec_path.read_text(encoding="utf-8"))} if spec_path.exists() else {}
    vertices = []
    tile = 6
    obj = ""
    groups = {"": []}
    for raw in obj_path.read_text(encoding="utf-8").splitlines():
        parts = raw.split()
        if not parts:
            continue
        if parts[0] == "v":
            vertices.append(flip([float(v) for v in parts[1:4]]))
        elif parts[0] == "o":
            # Blender appends the mesh name ("Rotor_L_Mesh"); match the object name it starts with.
            obj = next((k for k in specs if parts[1] == k or parts[1].startswith(k + "_")), "")
            groups.setdefault(obj, [])
        elif parts[0] == "usemtl":
            match = re.search(r"tile_(\d+)", parts[1])
            tile = int(match.group(1)) if match else 6
        elif parts[0] == "f":
            corners = [vertices[int(p.split("/")[0]) - 1] for p in parts[1:]]
            # Mirroring Z flips handedness, so reverse the order to keep the outside facing out.
            corners.reverse()
            groups[obj].append(" ".join([str(tile)] + [f"{c:.4f}" for corner in corners for c in corner]))
    lines = []
    if palette:
        lines.append("palette " + " ".join(f"{c:.3f}" for rgb in read_palette(obj_path) for c in rgb))
    lines += groups[""]
    for key, polygons in groups.items():
        if not key or not polygons:
            continue
        s = specs[key]
        axis, pivot = flip(s["axis"]), flip(s["pivot"])
        motion = "spin" if s["motion"] == "spin" else "swing"
        fields = [key.replace(" ", "_"), motion, *axis, *pivot, s.get("speed", 1), s.get("amplitude", 0)]
        lines.append("part " + " ".join(f"{f:.4f}" if isinstance(f, float) else str(f) for f in fields))
        lines += polygons
    out = ROOT / "Assets/Resources/TerraModels" / (name + ".txt")
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text("\n".join(glazing(name, lines)) + "\n", encoding="utf-8")
    print(f"{name}: {len(lines)} lines, {len([k for k in groups if k])} animated parts -> {out.relative_to(ROOT)}")


if __name__ == "__main__":
    bake(sys.argv[1], sys.argv[2], "--palette" in sys.argv[3:])
