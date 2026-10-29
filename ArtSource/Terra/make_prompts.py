"""Write a Terra prompt per enemy hull from brief.md and the hull list below.

Usage: python make_prompts.py <work folder> [hull ...]
"""
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
S = ROOT / "Assets/Scripts"

PALETTES = {
    "fleet": None,  # brief.md already lists it
    "space": "This district renders with the Space atlas, where some tiles differ from the list above: tile_06 is dark navy hull (0.105,0.15,0.20), tile_13 is gold foil (0.48,0.36,0.18), tile_15 is ivory (0.66,0.70,0.71), and tile_07 is a blue solar-cell grid (use it for solar panels). Space identity: ivory, gold foil, navy and solar blue.",
    "candy": "This district renders with the Candy atlas, which REPLACES the palette above: tile_00 raspberry (0.78,0.19,0.40), tile_01 cream (0.97,0.85,0.71), tile_02 mint (0.30,0.78,0.66), tile_03 orange (1,0.59,0.25), tile_04 chocolate (0.39,0.17,0.12), tile_05 caramel (0.62,0.28,0.18), tile_06 mauve (0.45,0.25,0.34), tile_07 lilac (0.57,0.38,0.79), tile_08 pink (0.90,0.44,0.65), tile_09 aqua glow (0.27,0.91,0.83), tile_10 lemon glow (1,0.82,0.39), tile_11 dark plum (0.13,0.055,0.13), tile_12 cherry (0.92,0.31,0.49), tile_13 butterscotch (0.83,0.66,0.39), tile_14 grape (0.51,0.26,0.58), tile_15 sugar white (0.98,0.96,0.88). Candy identity: glossy confectionery shapes, still clearly an aircraft.",
}

DISTRICT = {
    "Fields": "Plains: green valley, military olive/slate aircraft and tanks; hostile fire is the district hue, with one orange carrier exception.",
    "Snow": "Snow: winter siege craft, pale gray, steel and ice blue; shells that explode.",
    "City": "City: small fast urban craft, slate and steel with painted stripes, busy formations.",
    "Tunnel": "Tunnel: industrial mining/defense machinery, rust, steel and hazard paint; bouncing projectiles.",
    "Thunder": "Thunder: storm craft with capacitors, coils and beam emitters, violet/steel with cyan glow.",
    "Desert": "Desert: sand-worn rust, brass and pale ceramic craft; buried and burrowing machines.",
    "Candyland": "Candyland: confectionery-themed aircraft over pink clouds.",
    "Space": "Space: orbital satellites, carriers and stations.",
    "Underwater": "Underwater: brass, teal and steel submersibles and sea creatures.",
    "Hell": "Hell: dark volcanic district; bone, rust, near-black and ember accents; skulls, galleons, imps.",
}

# hull: (folder, display name, district, palette, role, source hint, kind)
# kind: "enemy" (300-2500 tris), "mini" (up to 4000), "boss" (up to 6000)
HULLS = {}


def add(hull, name, district, role, source, kind="enemy", palette="fleet"):
    HULLS[hull] = (name, district, palette, role, source, kind)


def muzzles(hull):
    if hull >= 61:
        return f"the Vector3 array of muzzles in the `typeof(...), {hull},` entry of {S}/Enemies/EnemyExpansion.cs (x, y, z; z is already toward the camera as a negative number in Unity space, so use +|z| in Blender)"
    return f"`case {hull}:` in {S}/Enemies/EnemyWeaponLayout.cs, method returning muzzle points P(x, y[, z]) (a negative z there is above the hull, so use +|z| in Blender); if there is no case, the default P(0,-0.75)"


def write(work, hull):
    name, district, palette, role, source, kind = HULLS[hull]
    source = source.replace("Assets/", ROOT.as_posix() + "/Assets/")
    budget = {"enemy": "300-2500", "mini": "up to 4000", "boss": "up to 6000"}[kind]
    folder = Path(work) / f"{hull:02d}_{name}"
    folder.mkdir(parents=True, exist_ok=True)
    brief = (Path(__file__).parent / "brief.md").read_text(encoding="utf-8")
    brief = brief.replace("one Underwater enemy", "one enemy").replace("of the Underwater district ", "")
    brief = brief.replace("Underwater identity: brass, teal and steel. The district's hostile bullets are amber, so keep amber glow small (muzzles, eyes).", "Keep glowing tiles small (muzzles, eyes, cores) so they never read as bullets.")
    text = brief + f"""
## This model

{name} (hull {hull}), district {district}. {DISTRICT[district]}
Role and character: {role}
Triangle budget for this one: {budget}.
Current source: {source}. The procedural helpers it calls (Builder, Face, Prism, Armor, BevelArmor, Barrel, Glass, Ball, Box...) are in {S}/Visuals/Meshes/IndustrialMeshFactory.cs and CombatHullFactory.cs; their int arguments are palette tile numbers (the same as tile_NN). Read them only as needed to understand the footprint, parts and colours.
Muzzle points (fixed by gameplay, keep a visible barrel lip at each): read {muzzles(hull)}.
Engine sockets: if {S}/Visuals/Meshes/CombatHullFactory.cs `CombatEngineSockets` returns sockets for this hull, put an engine nozzle opening at each socket (the game draws animated thrusters there); otherwise add no engine nozzles.
Keep the current model's recognisable identity (the creature/vehicle it is, its colour scheme, its silhouette type), but make it a clearly better, more detailed and more three-dimensional model.
"""
    if PALETTES[palette]:
        text += "\n" + PALETTES[palette] + "\n"
    (folder / "prompt.txt").write_text(text, encoding="utf-8")
    return folder


if __name__ == "__main__":
    from hulls import register

    register(add)
    work = sys.argv[1]
    for h in [int(a) for a in sys.argv[2:]] or sorted(HULLS):
        print(write(work, h))
