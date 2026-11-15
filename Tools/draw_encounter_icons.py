"""Original 32px encounter-choice sprites, drawn directly with Pillow. No font or AI art."""
from pathlib import Path
import uuid
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "Assets/Resources/EncounterIcons"
REVIEW = ROOT / "ArtSource/EncounterIconsReview"
DEST.mkdir(parents=True, exist_ok=True)
REVIEW.mkdir(parents=True, exist_ok=True)
INK = "#101c2b"
SHADOW = "#283d4d"
STEEL = "#849fa6"
LIGHT = "#dfebd7"
GOLD = "#edb44e"
CREAM = "#ffe5a0"
BLUE = "#62b9dc"
PALE_BLUE = "#c3e8f1"
GREEN = "#79bd75"
PURPLE = "#b88be4"


def canvas():
    im = Image.new("RGBA", (32, 32))
    return im, ImageDraw.Draw(im)


def coin(d, x, y, gold=GOLD, shine=CREAM):
    d.polygon([(x+2,y), (x+6,y), (x+8,y+2), (x+8,y+6), (x+6,y+8), (x+2,y+8), (x,y+6), (x,y+2)], fill=INK)
    d.polygon([(x+2,y+1), (x+5,y+1), (x+7,y+3), (x+7,y+5), (x+5,y+7), (x+2,y+7), (x+1,y+5), (x+1,y+3)], fill=gold)
    d.line([(x+2,y+2),(x+5,y+2)], fill=shine)
    d.rectangle((x+4,y+3,x+4,y+5), fill="#986335")


def shop(multiplier=False):
    im, d = canvas()
    color, light, shade = (BLUE, PALE_BLUE, "#346a91") if multiplier else (GOLD, CREAM, "#9e683b")
    d.rectangle((5,13,26,27), fill=INK)
    d.rectangle((7,14,24,25), fill=SHADOW)
    d.rectangle((8,15,23,18), fill="#142635")
    d.rectangle((5,22,26,28), fill=INK)
    d.rectangle((7,23,24,26), fill=shade)
    d.line((7,23,24,23), fill=color)
    d.rectangle((6,28,9,29), fill=INK)
    d.rectangle((23,28,26,29), fill=INK)
    # Readable striped awning; the two currencies share their shop silhouette.
    d.polygon([(6,4),(25,4),(29,10),(29,14),(3,14),(3,10)], fill=INK)
    d.polygon([(7,6),(24,6),(26,10),(6,10)], fill=shade)
    for x in range(5,27,5):
        d.rectangle((x,10,min(x+3,26),12), fill=light if (x//5)%2 else color)
    d.rectangle((8,5,23,6), fill=light)
    if multiplier:
        # A drawn multiplication mark, matching the run's multiplier symbol.
        d.rectangle((11,15,21,23), fill=INK)
        for x,y in ((13,16),(18,16),(14,17),(17,17),(15,18),(16,18),(14,19),(17,19),(13,20),(18,20)):
            d.rectangle((x,y,x+1,y+1), fill=light if y<19 else color)
        for i in range(3):
            x,y=22+i*2,25-i*2
            d.polygon([(x,y-2),(x+2,y),(x,y+2),(x-2,y)], fill=INK)
            d.point((x,y), fill=light)
    else:
        coin(d,12,16)
        coin(d,22,24)
    return im


def item():
    im,d = canvas()
    d.polygon([(4,10),(16,4),(28,10),(28,23),(16,29),(4,23)], fill=INK)
    d.polygon([(6,10),(16,6),(26,10),(16,15)], fill="#b3d49b")
    d.polygon([(6,12),(15,17),(15,26),(6,22)], fill="#4e805d")
    d.polygon([(17,17),(26,12),(26,22),(17,26)], fill=GREEN)
    d.line([(7,11),(16,16),(25,11)], fill=LIGHT)
    d.polygon([(10,8),(12,7),(23,13),(23,24),(21,25),(21,14)], fill=INK)
    d.polygon([(11,8),(12,8),(22,14),(22,24),(21,24),(21,15)], fill=GOLD)
    d.polygon([(14,18),(18,16),(20,18),(20,21),(16,23),(14,21)], fill=INK)
    d.polygon([(16,19),(18,18),(18,20),(16,21)], fill=CREAM)
    for x,y in ((3,5),(27,26)):
        d.line((x-2,y,x+2,y), fill=LIGHT)
        d.line((x,y-2,x,y+2), fill=LIGHT)
        d.point((x,y), fill=CREAM)
    return im


def health():
    im,d = canvas()
    d.rectangle((10,4,22,11), fill=INK)
    d.rectangle((12,6,20,9), fill=STEEL)
    d.rectangle((13,7,19,10), fill=(0,0,0,0))
    d.polygon([(6,9),(26,9),(29,12),(29,26),(26,29),(6,29),(3,26),(3,12)], fill=INK)
    d.rectangle((5,13,27,25), fill=STEEL)
    d.polygon([(7,11),(25,11),(27,13),(27,22),(5,22),(5,13)], fill=LIGHT)
    d.line((7,12,24,12), fill="#fcf4dc")
    d.rectangle((7,26,25,27), fill="#576d7b")
    d.polygon([(13,13),(19,13),(19,16),(23,16),(23,22),(19,22),(19,25),(13,25),(13,22),(9,22),(9,16),(13,16)], fill=INK)
    d.polygon([(15,14),(17,14),(17,18),(21,18),(21,20),(17,20),(17,24),(15,24),(15,20),(11,20),(11,18),(15,18)], fill="#edb92e")
    d.line((15,14,17,14), fill="#ffe182")
    d.rectangle((5,25,7,26), fill=INK)
    d.rectangle((25,25,27,26), fill=INK)
    return im


def challenge():
    """ELITE HUNT: red lock brackets on a gold-ringed elite fighter, with three catch pips."""
    im,d = canvas()
    red, red_light, gold, gold_dark = "#e2423a", "#ff8a7a", "#f5c84a", "#b8862a"
    # Gold elite ring, drawn as four short arcs.
    for start_angle in (20, 110, 200, 290):
        d.arc((5,3,27,25), start_angle, start_angle + 50, fill=INK, width=3)
        d.arc((6,4,26,24), start_angle + 4, start_angle + 46, fill=gold, width=1)
    # The elite: a small downward fighter.
    d.polygon([(16,22),(19,16),(24,13),(23,10),(18,11),(17,7),(15,7),(14,11),(9,10),(8,13),(13,16)], fill=INK)
    d.polygon([(16,20),(18,15),(22,12),(18,12),(17,9),(15,9),(14,12),(10,12),(14,15)], fill="#9aa6bb")
    d.polygon([(16,20),(14,15),(10,12),(14,12),(15,9),(16,9)], fill="#c3cddb")
    d.rectangle((15,13,16,14), fill=red)
    # Red lock brackets at the four corners.
    for x, y, sx, sy in ((2,0,1,1),(29,0,-1,1),(2,27,1,-1),(29,27,-1,-1)):
        d.line([(x, y + sy*4), (x, y), (x + sx*4, y)], fill=INK, width=3)
        d.line([(x, y + sy*3), (x, y), (x + sx*3, y)], fill=red, width=1)
    d.point((3,1), fill=red_light)
    # Three catch pips.
    for i, x in enumerate((10,16,22)):
        d.rectangle((x-2,27,x+1,30), fill=INK)
        d.rectangle((x-1,28,x,29), fill=gold if i < 2 else gold_dark)
    return im


def pact():
    im,d = canvas()
    paper, paper_shade, paper_light = "#d8bf88", "#9c7c4f", "#f4e3b4"
    blood, blood_dark, blood_light = "#c8283c", "#6e1424", "#ff7a7e"
    # A contract scroll: rolled ends top and bottom, a written page between.
    d.rectangle((7,5,25,27), fill=INK)
    d.rectangle((9,7,23,25), fill=paper)
    d.rectangle((9,7,10,25), fill=paper_light)
    d.rectangle((22,7,23,25), fill=paper_shade)
    for y in (4,25):
        d.rectangle((5,y,27,y+3), fill=INK)
        d.rectangle((6,y+1,26,y+2), fill=paper_shade)
        d.line((7,y+1,25,y+1), fill=paper_light)
    for y in (10,13,16):
        d.line((11,y,21,y), fill=paper_shade)
    d.line((11,19,16,19), fill=paper_shade)
    # The binding: a blood wax seal stamped with a horned sigil, with one drip.
    d.polygon([(18,17),(23,17),(26,20),(26,25),(23,28),(18,28),(15,25),(15,20)], fill=INK)
    d.polygon([(18,18),(23,18),(25,20),(25,25),(23,27),(18,27),(16,25),(16,20)], fill=blood)
    d.line((18,19,22,19), fill=blood_light)
    d.point((17,20), fill=blood_light)
    d.rectangle((19,22,22,25), fill=blood_dark)
    d.point((18,21), fill=blood_dark)
    d.point((23,21), fill=blood_dark)
    d.rectangle((20,23,21,24), fill=blood)
    d.rectangle((19,28,20,30), fill=INK)
    d.point((19,29), fill=blood)
    return im


def salvage():
    im, d = canvas()
    # Open riveted salvage tote. Its lip occludes the scrap, seating everything inside.
    d.polygon([(4,14),(22,11),(28,15),(26,26),(9,29),(3,24)], fill=INK)
    d.polygon([(5,15),(22,13),(26,15),(9,18)], fill="#25373c")
    # A thick broken copper pipe behind the gear, with a visible open end.
    d.line([(18,14),(18,7),(21,4),(25,4),(27,6),(27,11)], fill=INK, width=5)
    d.line([(18,13),(18,7),(21,5),(25,5),(26,7),(26,11)], fill="#a46340", width=3)
    d.line([(18,11),(18,7),(21,5),(24,5)], fill="#e6a86e")
    d.rectangle((25,10,27,12), fill=INK)
    d.line((25,10,26,10), fill="#edbd87")
    gear(d, 2, 2)
    # A single bent sheet, behind the front rim rather than floating above the bin.
    d.polygon([(16,11),(21,9),(25,14),(21,18),(17,17)], fill=INK)
    d.polygon([(17,12),(20,11),(23,14),(20,16)], fill="#a7b9ae")
    d.line((18,12,20,12), fill=LIGHT)
    # Two visible faces, a folded steel lip and a recessed front grip.
    d.polygon([(4,16),(9,19),(9,27),(5,24)], fill="#425b61")
    d.polygon([(10,19),(26,16),(24,25),(10,28)], fill="#638887")
    d.polygon([(4,14),(9,17),(27,14),(28,16),(9,20),(3,16)], fill=INK)
    d.line([(5,15),(9,18),(26,15)], fill="#b6c8b7")
    d.line((11,21,23,19), fill="#83a69a")
    d.polygon([(14,22),(20,21),(20,23),(14,24)], fill=INK)
    d.line((15,22,19,21), fill="#a1b9aa")
    for x,y in ((11,20),(23,18),(11,26),(23,24)):
        d.point((x,y), fill="#c0c8ae")
    coin(d,22,22, "#dba34b", "#ffe1a0")
    return im


def gear(d, x, y):
    # A deliberately stepped 15px cog with eight teeth, a bevel and a dark square bore.
    points = [(5,0),(9,0),(9,2),(12,2),(12,5),(14,5),(14,9),(12,9),
              (12,12),(9,12),(9,14),(5,14),(5,12),(2,12),(2,9),(0,9),
              (0,5),(2,5),(2,2),(5,2)]
    d.polygon([(x+a,y+b) for a,b in points], fill=INK)
    d.polygon([(x+a,y+b) for a,b in [(6,1),(8,1),(8,3),(11,3),(11,6),(13,6),
        (13,8),(11,8),(11,11),(8,11),(8,13),(6,13),(6,11),(3,11),(3,8),
        (1,8),(1,6),(3,6),(3,3),(6,3)]], fill="#728f9d")
    d.line([(x+6,y+1),(x+8,y+1)], fill="#d6e3d8")
    d.line([(x+3,y+3),(x+5,y+3),(x+5,y+2)], fill="#c2d5cf")
    d.line((x+3,y+4,x+3,y+6), fill="#b6ccc9")
    d.line((x+1,y+6,x+1,y+8), fill="#b6ccc9")
    d.line((x+8,y+12,x+8,y+13), fill="#42596b")
    d.line((x+10,y+10,x+11,y+8), fill="#42596b")
    d.rectangle((x+5,y+5,x+9,y+9), fill="#354954")
    d.rectangle((x+6,y+6,x+8,y+8), fill=INK)
    d.line((x+5,y+5,x+9,y+5), fill="#c2d5cf")
    d.line((x+5,y+6,x+5,y+8), fill="#9eb6ba")


def health_trade():
    im, d = canvas()
    # Two large, separate silhouettes: equipment is converted into health.
    gear(d, 1, 3)
    heart = [(20,4),(23,4),(25,6),(27,4),(29,4),(31,6),(31,11),
             (29,14),(25,18),(21,15),(18,11),(18,7)]
    d.polygon(heart, fill=INK)
    d.polygon([(20,6),(22,5),(24,7),(25,9),(27,6),(29,5),(30,7),(30,11),
               (28,13),(25,16),(22,13),(20,11),(19,8)], fill="#cb5265")
    d.polygon([(20,7),(22,6),(24,8),(25,10),(27,7),(29,6),(29,10),
               (27,12),(24,13),(21,11),(20,9)], fill="#ec7b7b")
    d.line((21,7,22,7), fill="#ffe5c5")
    d.point((20,8), fill="#ffc2a1")
    d.line([(25,15),(28,12),(29,11)], fill="#a73752")
    # One directional, chunky exchange arrow; no tiny scale pans or hanging lines.
    d.polygon([(4,23),(21,23),(21,20),(29,25),(21,30),(21,28),(4,28)], fill=INK)
    d.polygon([(6,24),(23,24),(23,23),(27,25),(23,28),(23,27),(6,27)], fill="#d49b49")
    d.line((6,24,22,24), fill="#ffe0a0")
    d.point((24,24), fill="#ffe0a0")
    return im


def reforge():
    im, d = canvas()
    # A solid blacksmith's anvil: pointed horn, thick waist, heel and broad feet.
    d.polygon([(2,16),(10,16),(13,14),(29,14),(30,15),(30,20),(25,22),
               (23,22),(22,25),(26,27),(26,30),(8,30),(8,27),(12,25),
               (12,22),(7,21),(2,18)], fill=INK)
    d.polygon([(4,17),(11,17),(14,15),(28,15),(29,16),(25,18),(10,19)], fill="#b0c8cd")
    d.line((14,15,27,15), fill="#e1e9dc")
    d.polygon([(10,20),(25,19),(29,17),(29,19),(24,21),(22,21),(22,25),
               (25,28),(10,28),(14,25),(14,22)], fill="#5f7f95")
    d.line((15,20,23,20), fill="#8faebc")
    d.polygon([(22,22),(23,22),(22,25),(25,27),(25,29),(21,29),
               (21,27),(20,25),(20,22)], fill="#3d526b")
    d.polygon([(14,22),(16,22),(16,25),(12,28),(10,28),(14,25)], fill="#83a1ad")
    d.line((11,28,23,28), fill="#92b2bb")
    d.line((5,18,10,20), fill="#7495a5")
    # Handle and head meet at right angles: a real sledgehammer silhouette.
    d.polygon([(23,1),(26,4),(15,14),(12,11)], fill=INK)
    d.polygon([(23,3),(24,4),(15,12),(14,11)], fill="#af7148")
    d.line((22,3,16,9), fill="#e6b078")
    d.line((21,5,22,6), fill="#754832")
    d.polygon([(9,3),(20,11),(20,13),(17,16),(15,16),(5,8),(5,6),(7,4)], fill=INK)
    d.polygon([(9,5),(18,11),(16,14),(7,7)], fill="#7a9eaf")
    d.line((9,5,16,10), fill="#d7e4d8")
    d.line((8,7,15,12), fill="#a6c7ce")
    d.polygon([(18,11),(18,12),(16,14),(15,14),(16,12)], fill="#405d77")
    d.line((7,7,14,13), fill="#506e88")
    # A small hot workpiece and two sparks establish the forging action.
    d.polygon([(11,15),(16,14),(20,16),(18,18),(12,17)], fill=INK)
    d.polygon([(12,15),(16,15),(18,16),(16,17),(12,16)], fill="#df8852")
    d.line((13,15,16,15), fill="#ffe1a0")
    d.line((8,11,8,13), fill="#e9b967")
    d.line((7,12,9,12), fill="#ffe4a8")
    d.point((5,15), fill="#e9b967")
    return im


def cargo():
    im, d = canvas()
    # The crate owns most of the silhouette. All three faces share the same corners.
    d.line((6,9,8,17), fill=INK, width=3)
    d.line((25,9,24,17), fill=INK, width=3)
    d.line((6,10,8,16), fill="#d5dac5")
    d.line((25,10,24,16), fill="#b5c2b7")
    # A small inflated canopy with separated gores and scalloped lower edges.
    d.polygon([(4,8),(6,4),(10,1),(21,1),(25,4),(28,8),(28,10),
               (24,9),(20,10),(16,9),(12,10),(8,9),(4,10)], fill=INK)
    d.polygon([(6,7),(8,4),(11,3),(20,3),(24,5),(26,7),(23,8),
               (20,8),(16,7),(12,8),(8,7)], fill="#d5b382")
    d.polygon([(12,3),(19,3),(21,8),(16,7),(11,8)], fill="#d07749")
    d.line((13,3,18,3), fill="#f0b37a")
    d.line([(8,5),(11,3),(12,3)], fill="#f6e2b3")
    d.line((7,7,10,7), fill="#ead8aa")
    # A broad timber box; the shared top/front corner prevents a twisted perspective.
    d.polygon([(4,17),(15,13),(28,17),(28,26),(17,30),(4,26)], fill=INK)
    d.polygon([(6,17),(15,14),(26,17),(17,20)], fill="#dfb676")
    d.polygon([(6,19),(16,22),(16,29),(6,25)], fill="#ba824c")
    d.polygon([(18,22),(26,19),(26,25),(18,29)], fill="#825a3c")
    d.line([(6,17),(17,21),(26,18)], fill="#f1d39a")
    d.line((17,22,17,29), fill="#382e29")
    # Plank seams follow the face planes; a crossed brace belongs to the front face.
    d.line((7,21,15,24), fill="#8e5c37")
    d.line((7,24,15,27), fill="#8e5c37")
    d.line((19,24,25,22), fill="#614731")
    d.line((19,27,25,25), fill="#614731")
    d.line((7,20,14,27), fill="#e0af6b", width=2)
    d.line((7,24,14,23), fill="#d39b59")
    # A continuous blue steel band wraps over the lid and down the right face.
    d.polygon([(10,15),(13,14),(25,18),(22,19)], fill=INK)
    d.polygon([(12,15),(13,15),(23,18),(22,18)], fill="#9bb2ae")
    d.polygon([(22,20),(24,19),(24,26),(22,27)], fill=INK)
    d.line((23,20,23,26), fill="#789697")
    d.point((23,21), fill="#d3d9bf")
    d.point((23,25), fill="#d3d9bf")
    d.point((8,19), fill="#6f4932")
    d.point((14,27), fill="#6f4932")
    return im


icons = {"Shop":shop(), "MultShop":shop(True), "Item":item(), "Health":health(), "Challenge":challenge(), "Pact":pact(),
         "Salvage":salvage(), "HealthTrade":health_trade(), "Reforge":reforge(), "Cargo":cargo()}
template = (ROOT / "Assets/Resources/ProjectileSprites/standard.png.meta").read_text()
folder_meta = DEST.with_suffix(".meta")
if not folder_meta.exists():
    folder_meta.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
for name,im in icons.items():
    destination = DEST / f"{name}.png"
    # Preserve approved files byte-for-byte when only Pillow's PNG encoder changed.
    if not destination.exists() or Image.open(destination).convert("RGBA").tobytes() != im.tobytes():
        im.save(destination)
    assert im.size == (32,32) and set(im.getchannel("A").tobytes()) == {0,255}
    meta = DEST / f"{name}.png.meta"
    if not meta.exists():
        lines = template.splitlines()
        lines[1] = f"guid: {uuid.uuid4().hex}"
        content = "\n".join(lines) + "\n"
        content = content.replace("  isReadable: 1", "  isReadable: 0").replace("  maxTextureSize: 2048", "  maxTextureSize: 64")
        content = content.replace("  alphaIsTransparency: 0", "  alphaIsTransparency: 1")
        meta.write_text(content)
    meta.write_text("\n".join(line.rstrip() for line in meta.read_text().splitlines())+"\n")
folder_meta.write_text("\n".join(line.rstrip() for line in folder_meta.read_text().splitlines())+"\n")

sheet = Image.new("RGB", (len(icons)*192, 248), "#18232f")
d = ImageDraw.Draw(sheet)
for i,(name,im) in enumerate(icons.items()):
    sheet.paste(im.resize((128,128),Image.Resampling.NEAREST), (i*192+32,24), im.resize((128,128),Image.Resampling.NEAREST))
    sheet.paste(im.resize((64,64),Image.Resampling.NEAREST), (i*192+64,164), im.resize((64,64),Image.Resampling.NEAREST))
    d.text((i*192+12,235),name,fill="#e1e8df")
sheet.save(REVIEW / "IconSheet.png")
print(f"Generated {len(icons)} directly drawn 32px sprites and point-sampled 2x / 4x preview.")
