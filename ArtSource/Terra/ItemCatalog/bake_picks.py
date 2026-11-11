"""Export the chosen item designs, including the hybrids built on original meshes."""

import argparse
import colorsys
from pathlib import Path

import actives
import hardware
from mesh import Model, cross, dot, sub, unit
import passives


PICKS = (
    "ScatterCannonItem", "WindUpCannonItem", "FireflyJar", "PaperclipBox", "RibbonSpool",
    "SaltShaker", "WindUpTeeth", "BarrelRoll", "BlankBullet", "Bookmark", "CarbonPaper",
    "DecoyProjector", "DemolitionPlunger", "FirecrackerBelt", "PocketFan", "Redline",
    "RocketBouquet", "SignalFlare", "GravityWell", "ThrowableShield", "Thunderhead",
    "BabyYouItem", "PaperPlane", "Tripod", "TwerpItem", "Caffeine", "ClockworkSpring",
    "EarlyRise", "HeatSink", "Horseshoe", "Hourglass", "LuckyPenny", "ProteinSupplement",
    "RabbitsFoot", "RelaySwitch", "RollerBearings", "BallBearing", "SplitterCore",
    "SteelWashers", "TailFin", "WarpCore", "Whetstone", "life",
)

ACCENTS = {
    "FireflyJar": {5: "edfff2", 8: "87ebbd"},
    "HeatSink": {0: "508bda", 1: "94cdf9", 8: "edb559"},
    "Horseshoe": {0: "39ac71", 6: "c6edb3"},
    "PaperPlane": {5: "eef9ff", 6: "88bbdd"},
    "RabbitsFoot": {6: "71b9ba", 8: "f7cb75"},
    "SteelWashers": {6: "84afca"},
    "Whetstone": {0: "509995", 2: "3f6d72"},
}


def vivid(model, key):
    for tile, (red, green, blue) in enumerate(model.palette):
        hue, saturation, value = colorsys.rgb_to_hsv(red, green, blue)
        if value >= .25 and value * saturation >= .075:
            model.palette[tile] = colorsys.hsv_to_rgb(hue, min(1, saturation * 1.13), min(1, value * 1.09 + .018))
    for tile, colour in ACCENTS.get(key, {}).items():
        model.paint(tile, colour)
    return model


def split_warped_quads(line):
    tile, *values = line.split()
    if len(values) != 12:
        return (line,)
    vertices = [tuple(map(float, values[i:i + 3])) for i in range(0, 12, 3)]
    a, b, c, d = vertices
    first = unit(cross(sub(a, d), sub(b, d)))
    second = unit(cross(sub(c, b), sub(d, b)))
    if dot(first, second) >= .999:
        return (line,)
    coordinates = [values[i:i + 3] for i in range(0, 12, 3)]
    return tuple(" ".join([tile] + [value for i in triangle for value in coordinates[i]])
                 for triangle in ((3, 0, 1), (1, 2, 3)))


def augment(path, accents, replace_tiles=(), split_quads=False):
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    if not lines or not lines[0].startswith("palette "):
        raise ValueError(f"Missing original palette in {path}")
    palette = lines[0].split()
    used = {tile for tile, _ in accents.faces}
    for tile in used:
        start = 1 + tile * 3
        palette[start:start + 3] = (f"{value:.5f}" for value in accents.palette[tile])
    retained = []
    for line in lines[1:]:
        if int(line.split()[0]) not in replace_tiles:
            retained.extend(split_warped_quads(line) if split_quads else (line,))
    return "\n".join([" ".join(palette), *retained, *accents.bake().splitlines()[1:]]) + "\n"


def spoon_chips():
    m = Model("91e4fa", "f6fffc")
    m.paint(12, "75daf4")
    m.paint(13, "e4fff9")
    for x, y, z, radius in ((-.18, .29, -.07, .037), (-.24, .43, .02, .030), (.19, .43, .05, .041)):
        m.gem((x, y, z), (radius, radius * 1.48, radius * .65), 12 if x < -.2 else 13, sides=5)
    return m


def bake(destination, original):
    builders = {**actives.BUILDERS, **hardware.BUILDERS, **passives.BUILDERS, "EarlyRise": passives.early_rise}
    destination.mkdir(parents=True, exist_ok=True)
    for key in PICKS:
        result = vivid(builders[key](), key).bake()
        (destination / f"Item{key}.txt").write_text(result, encoding="ascii")
    for key, model, replacement in (
        ("AngryMoonItem", hardware.angry_moon_face(), (2, 3, 4)),
        ("Telekinesis", spoon_chips(), (12, 13)),
    ):
        # Either source also works if it already contains a previously baked accent.
        path = original / f"Item{key}.txt"
        result = augment(path, model, replacement, split_quads=key == "AngryMoonItem")
        (destination / path.name).write_text(result, encoding="ascii")
    print(f"Exported {len(PICKS) + 2} selected item models.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--original", type=Path, default=Path("Assets/Resources/TerraModels"))
    args = parser.parse_args()
    bake(args.destination, args.original)
