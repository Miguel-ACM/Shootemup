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
    d.polygon([(15,14),(17,14),(17,18),(21,18),(21,20),(17,20),(17,24),(15,24),(15,20),(11,20),(11,18),(15,18)], fill="#e9757c")
    d.line((15,14,17,14), fill="#ffb39e")
    d.rectangle((5,25,7,26), fill=INK)
    d.rectangle((25,25,27,26), fill=INK)
    return im


def challenge():
    im,d = canvas()
    d.polygon([(16,2),(29,7),(28,21),(23,27),(16,31),(9,27),(4,21),(3,7)], fill=INK)
    d.polygon([(16,4),(27,8),(26,20),(22,25),(16,29),(10,25),(6,20),(5,8)], fill="#604775")
    d.line([(7,10),(7,18),(11,24)], fill=PURPLE, width=1)
    d.line([(25,10),(25,18),(21,24)], fill="#d8b1ef", width=1)
    # An angular skull and three small wave marks make a danger/reward crest.
    d.polygon([(12,7),(20,7),(24,11),(24,18),(21,21),(21,24),(11,24),(11,21),(8,18),(8,11)], fill=INK)
    d.polygon([(12,9),(20,9),(22,12),(22,17),(19,19),(19,22),(13,22),(13,19),(10,17),(10,12)], fill=LIGHT)
    d.rectangle((12,9,19,10), fill="#fff0d7")
    d.polygon([(11,13),(15,14),(15,17),(11,16)], fill=INK)
    d.polygon([(17,14),(21,13),(21,16),(17,17)], fill=INK)
    d.polygon([(16,17),(18,20),(14,20)], fill=INK)
    d.rectangle((15,21,16,22), fill="#8d8388")
    for x,y in ((10,26),(16,28),(22,26)):
        d.rectangle((x-1,y-1,x+1,y), fill=PURPLE)
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
    # An open, battered scrap bin: a gear, a copper coil and a loose coin.
    d.polygon([(3,15),(26,15),(24,29),(6,29)], fill=INK)
    d.polygon([(5,17),(24,17),(22,27),(8,27)], fill="#547783")
    d.line((8,18,10,25), fill="#9db9b6")
    d.line((20,18,19,25), fill="#2c485a")
    d.rectangle((3,14,26,17), fill=INK)
    d.line((5,15,24,15), fill=STEEL)
    for x,y in ((6,5),(13,5),(3,9),(16,9),(6,13),(13,13)):
        d.rectangle((x,y,x+3,y+3), fill=INK)
    d.polygon([(7,5),(13,5),(17,9),(17,13),(13,17),(7,17),(3,13),(3,9)], fill=INK)
    d.polygon([(7,7),(12,7),(15,10),(15,13),(12,15),(7,15),(5,13),(5,10)], fill=STEEL)
    d.rectangle((8,10,11,12), fill=INK)
    d.line((7,8,12,8), fill=LIGHT)
    d.line([(17,12),(20,5),(24,6),(22,12)], fill=INK, width=4)
    d.line([(18,12),(21,6),(23,7),(21,12)], fill="#d08c58", width=2)
    coin(d,22,20)
    d.line((27,3,27,7), fill=CREAM)
    d.line((25,5,29,5), fill=CREAM)
    return im


def health_trade():
    im, d = canvas()
    # A little balance scale: surrendered equipment on one pan, a heart on the other.
    d.rectangle((14,8,17,26), fill=INK)
    d.rectangle((15,10,16,25), fill=STEEL)
    d.polygon([(10,26),(21,26),(24,29),(7,29)], fill=INK)
    d.line((11,27,21,27), fill=LIGHT)
    d.line((5,9,26,5), fill=INK, width=3)
    d.line((6,9,25,6), fill=GOLD)
    for x,y in ((6,10),(25,7)):
        d.line((x,y,x-3,y+10), fill=INK)
        d.line((x,y,x+3,y+10), fill=INK)
        d.polygon([(x-5,y+9),(x+5,y+9),(x+3,y+12),(x-3,y+12)], fill=INK)
        d.line((x-3,y+10,x+3,y+10), fill=STEEL)
    d.rectangle((2,12,10,18), fill=INK)
    d.rectangle((3,13,9,17), fill="#72a2b0")
    d.line((4,13,8,13), fill=PALE_BLUE)
    d.rectangle((5,14,6,16), fill=GOLD)
    d.polygon([(21,10),(24,10),(25,11),(26,10),(29,10),(31,12),(31,15),(26,19),(21,15),(19,13)], fill=INK)
    d.polygon([(21,11),(24,11),(25,13),(27,11),(29,11),(30,13),(29,15),(26,17),(22,14),(21,13)], fill="#e8727b")
    d.line((22,11,23,11), fill="#ffe0cb")
    d.point((15,7), fill=LIGHT)
    return im


def reforge():
    im, d = canvas()
    # A broad iron anvil under a diagonal hammer, with a hot forging spark.
    d.polygon([(3,16),(13,16),(16,14),(27,14),(29,17),(24,21),(21,21),(21,25),(25,27),(25,29),(7,29),(7,27),(11,25),(11,21),(6,21)], fill=INK)
    d.polygon([(5,17),(14,17),(17,15),(26,15),(27,17),(23,19),(9,19)], fill=STEEL)
    d.line((7,17,13,17), fill=LIGHT)
    d.rectangle((13,20,19,25), fill="#4b6076")
    d.polygon([(12,26),(20,26),(22,27),(10,27)], fill="#829bab")
    d.line((20,6,11,15), fill=INK, width=4)
    d.line((20,7,13,14), fill="#bc8454", width=2)
    d.polygon([(18,2),(21,2),(29,9),(29,12),(26,15),(23,15),(15,8),(15,5)], fill=INK)
    d.polygon([(18,4),(20,4),(27,10),(27,12),(25,13),(17,7),(17,5)], fill="#6b92ac")
    d.line((18,4,25,10), fill=PALE_BLUE)
    for x,y in ((6,8),(9,5),(4,12)):
        d.line((x-1,y,x+1,y), fill=GOLD)
        d.line((x,y-1,x,y+1), fill=CREAM)
    d.rectangle((12,15,15,16), fill="#ffca75")
    return im


def cargo():
    im, d = canvas()
    # A strapped drop crate suspended below a striped parachute.
    d.polygon([(2,10),(4,5),(9,2),(22,2),(27,5),(30,10),(30,12),(2,12)], fill=INK)
    d.polygon([(4,10),(6,6),(10,4),(21,4),(25,6),(28,10)], fill="#e8c68a")
    d.polygon([(12,4),(19,4),(21,10),(10,10)], fill="#e78655")
    d.line((4,11,10,20), fill=INK)
    d.line((27,11,22,20), fill=INK)
    d.line((15,11,16,19), fill=INK)
    d.line((6,12,11,19), fill=LIGHT)
    d.line((25,12,21,19), fill=LIGHT)
    d.polygon([(8,20),(16,17),(24,20),(24,28),(16,31),(8,28)], fill=INK)
    d.polygon([(10,20),(16,19),(22,20),(16,23)], fill="#f4cf8c")
    d.polygon([(10,22),(15,24),(15,29),(10,27)], fill="#9f6d47")
    d.polygon([(17,24),(22,22),(22,27),(17,29)], fill="#d59555")
    d.line([(13,20),(19,23),(19,28)], fill=INK, width=2)
    d.line([(14,20),(20,23),(20,27)], fill="#6792a2")
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
