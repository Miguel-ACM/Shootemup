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


icons = {"Shop":shop(), "MultShop":shop(True), "Item":item(), "Health":health(), "Challenge":challenge()}
template = (ROOT / "Assets/Resources/ProjectileSprites/standard.png.meta").read_text()
folder_meta = DEST.with_suffix(".meta")
if not folder_meta.exists():
    folder_meta.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
for name,im in icons.items():
    im.save(DEST / f"{name}.png")
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

sheet = Image.new("RGB", (5*192, 248), "#18232f")
d = ImageDraw.Draw(sheet)
for i,(name,im) in enumerate(icons.items()):
    sheet.paste(im.resize((128,128),Image.Resampling.NEAREST), (i*192+32,24), im.resize((128,128),Image.Resampling.NEAREST))
    sheet.paste(im.resize((64,64),Image.Resampling.NEAREST), (i*192+64,164), im.resize((64,64),Image.Resampling.NEAREST))
    d.text((i*192+12,235),name,fill="#e1e8df")
sheet.save(REVIEW / "IconSheet.png")
print("Generated five 32px sprites and point-sampled 2x / 4x preview.")
