import json
import pathlib
import re

root = pathlib.Path(__file__).resolve().parents[1]
blocks = re.split(r"--- !u!", (root / "Assets/Scenes/L1.unity").read_text())[1:]
names, transforms, parents = {}, {}, {}
for block in blocks:
    identity = re.match(r"(\d+) &(-?\d+)", block)
    if not identity:
        continue
    kind, key = identity.groups()
    if kind == "1":
        names[key] = re.search(r"  m_Name: (.*)", block).group(1)
    if kind in ("4", "224"):
        if not re.search(r"m_GameObject: \{fileID: (-?\d+)", block):
            continue
        transforms[key] = re.search(r"m_GameObject: \{fileID: (-?\d+)", block).group(1)
        parents[key] = re.search(r"m_Father: \{fileID: (-?\d+)", block).group(1)

def chain(key):
    return (chain(parents[key]) + "/" if parents.get(key) in transforms else "") + names.get(transforms.get(key), "?")

print("\n".join(chain(key) for key in transforms if names.get(transforms[key]) in {"Health", "Mana", "Dialog", "ItemsLeft", "ItemsRight", "BossHealth", "PauseMenu", "Score"}))

models = []
for path in sorted((root / "ArtSource/IndustrialExpansion").glob("*.obj")):
    text = path.read_text()
    count = len(re.findall(r"^f ", text, re.M))
    assert count < 300, (path.name, count)
    assert len(re.findall(r"^v ", text, re.M)) == len(re.findall(r"^vt ", text, re.M)), path.name
    models.append({"name": path.stem, "triangles": count})
print(json.dumps({"models": models}, indent=2))
try:
    import PIL
    print("Pillow available")
except ImportError:
    print("Pillow unavailable")
