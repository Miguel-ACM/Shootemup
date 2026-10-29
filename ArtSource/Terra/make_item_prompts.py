"""Write a Terra prompt per item from brief_items.md.

Usage: python make_item_prompts.py <work folder> [Kind ...]
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
S = (ROOT / "Assets/Scripts").as_posix()

# Miguel's original items (2023 pool plus AK45) and the three pickups: same name and look, better model.
ORIGINAL = ["life", "mana", "money", "RabbitsFoot", "BlankBullet", "TwerpItem", "Magnet", "AK45Item", "Caffeine", "ProteinSupplement", "AMoonItem", "BabyYouItem", "ElectricityItem", "AnotherArm", "AngryMoonItem", "Telekinesis", "ThreeMoonsItem"]
ABSTRACT = {"AItem", "AActiveItem", "ACompanionItem", "APassiveWeaponItem", "AWeaponItem", "APactItem"}
EXTRA = ["BaseWeaponItem"]


def item_classes():
    found = {}
    for path in (ROOT / "Assets/Scripts/Items").rglob("*.cs"):
        for name, base in re.findall(r"class (\w+)\s*:\s*(\w+)", path.read_text(encoding="utf-8")):
            if base in ABSTRACT and name not in ABSTRACT:
                found[name] = path.as_posix()
    return found


def write(work, kind, classes):
    folder = Path(work) / ("Item" + kind)
    folder.mkdir(parents=True, exist_ok=True)
    text = (Path(__file__).parent / "brief_items.md").read_text(encoding="utf-8")
    text += f"\n## This item\n\nItem `{kind}`.\n"
    if kind in classes:
        text += f"What it does in the game: read {classes[kind]} (class {kind}) only to understand the item's identity.\n"
    if kind in ORIGINAL:
        text += f"""This is one of Miguel's ORIGINAL items. Keep its name and its look exactly: the same object, the same design, composition and colour scheme as the shipped model, only modelled with clearly higher quality (better proportions, bevels, curvature, secondary detail). Do not reinterpret or redesign it.
The shipped model is built by the item function for it in {ROOT.as_posix()}/ArtSource/PSXv2/build_ships.py (search for `def item_`; read the materials table near the top for its colours; there items also stand upright and face +Z). An older version is in the `case` of CreateItem in {S}/Visuals/Meshes/RecognizableItemFactory.cs (index {ORIGINAL.index(kind)}).
"""
    else:
        text += f"""Current model: search the C# files in {S}/Visuals/Meshes for "\\"{kind}\\"" and read the branch that builds it (its int arguments are tile numbers of an equipment atlas; colours may be overridden in {S}/Visuals/EverydayItemMaterials.cs). Keep what the object is and its recognisable colour identity; make it a colourful, immediately distinguishable, clearly better three-dimensional model.
"""
    (folder / "prompt.txt").write_text(text, encoding="utf-8")
    return folder


if __name__ == "__main__":
    classes = item_classes()
    kinds = sys.argv[2:] or sorted(set(ORIGINAL) | set(classes) | set(EXTRA))
    for kind in kinds:
        print(write(sys.argv[1], kind, classes))
