"""32px padlock for sealed route panels, drawn directly with Pillow. No font or AI art."""
from pathlib import Path
import re
import uuid
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "Assets/Resources/DockArt/RoutePadlock.png"
REVIEW = ROOT / "ArtSource/RoutePadlockReview"
REVIEW.mkdir(parents=True, exist_ok=True)
INK = "#101c2b"
SHADOW = "#3f5563"
STEEL = "#849fa6"
LIGHT = "#dfebd7"

im = Image.new("RGBA", (32, 32))
d = ImageDraw.Draw(im)
# Shackle: a squared arch, outlined, sinking into the body.
d.rectangle((9, 3, 22, 16), fill=INK)
d.rectangle((14, 8, 17, 16), fill=(0, 0, 0, 0))
d.rectangle((10, 4, 21, 6), fill=STEEL)
d.rectangle((10, 4, 12, 15), fill=STEEL)
d.rectangle((19, 4, 21, 15), fill=STEEL)
d.line([(11, 5), (20, 5)], fill=LIGHT)
d.line([(11, 5), (11, 14)], fill=LIGHT)
for x, y in ((9, 3), (22, 3)):
    im.putpixel((x, y), (0, 0, 0, 0))
# Body.
d.rectangle((6, 14, 25, 29), fill=INK)
d.rectangle((7, 15, 24, 28), fill=STEEL)
d.line([(8, 16), (23, 16)], fill=LIGHT)
d.line([(8, 16), (8, 26)], fill=LIGHT)
d.rectangle((7, 27, 24, 28), fill=SHADOW)
d.line([(24, 15), (24, 28)], fill=SHADOW)
# Keyhole.
d.rectangle((14, 19, 17, 22), fill=INK)
d.rectangle((15, 22, 16, 25), fill=INK)
im.save(DEST)
im.resize((256, 256), Image.NEAREST).save(REVIEW / "RoutePadlock_x8.png")

meta = DEST.with_suffix(".png.meta")
if not meta.exists():
    template = (ROOT / "Assets/Resources/DockArt/DockCoin.png.meta").read_text()
    template = re.sub(r"guid: \w+", "guid: " + uuid.uuid4().hex, template, count=1)
    template = re.sub(r"spriteID: \w+", "spriteID: " + uuid.uuid4().hex, template, count=1)
    template = re.sub(r"spritePixelsToUnits: \d+", "spritePixelsToUnits: 32", template, count=1)
    meta.write_text(template)
