"""Four directly drawn 32px stat reward icons; no changes to projectile art."""
from pathlib import Path
import uuid
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "Assets/Resources/EncounterIcons"
REVIEW = ROOT / "ArtSource/StatHexReview"
INK = "#111d2b"
PALE = "#f2edda"


def canvas():
    image = Image.new("RGBA", (32, 32))
    return image, ImageDraw.Draw(image)


def speed():
    image, d = canvas()
    # Two broad forward chevrons, with separated speed streaks behind them.
    for y in (3, 15):
        d.polygon([(16,y),(28,y+10),(28,y+14),(16,y+7),(4,y+14),(4,y+10)], fill=INK)
        d.polygon([(16,y+2),(26,y+10),(26,y+11),(16,y+5),(6,y+11),(6,y+10)], fill="#54cce8")
        d.line([(7,y+10),(16,y+3),(25,y+10)], fill="#cef2eb")
        d.line([(16,y+5),(26,y+11)], fill="#348ba8")
    for x,y,h in ((1,19,6),(30,16,7),(15,26,4)):
        d.line((x,y,x,y+h), fill=INK, width=3)
        d.line((x,y,x,y+h-1), fill="#54cce8")
    return image


def damage():
    image, d = canvas()
    # One heavy shell punching into an angular impact crown.
    d.polygon([(16,1),(19,7),(27,3),(25,10),(31,12),(25,16),
               (28,22),(21,20),(11,20),(4,22),(7,16),(1,12),(7,10),(5,3),(13,7)], fill=INK)
    d.polygon([(16,4),(18,10),(24,7),(22,12),(28,12),(22,15),
               (24,19),(19,17),(12,17),(7,19),(10,14),(4,12),(10,12),(8,7),(14,10)], fill="#ed9e43")
    d.polygon([(16,7),(18,12),(22,10),(20,15),(12,15),(10,10),(14,12)], fill="#ffe4a1")
    d.polygon([(16,10),(22,17),(22,29),(10,29),(10,17)], fill=INK)
    d.polygon([(16,12),(20,18),(20,25),(12,25),(12,18)], fill="#d57839")
    d.polygon([(16,12),(16,25),(12,25),(12,18)], fill="#ffc975")
    d.line([(14,17),(16,14)], fill=PALE)
    d.rectangle((12,26,20,27), fill="#ffe0a0")
    d.line((18,19,18,24), fill="#9c4f32")
    return image


def health():
    image, d = canvas()
    # A heart in expanding armor brackets: capacity, distinct from the medkit.
    d.line([(8,3),(3,3),(3,28),(8,28)], fill=INK, width=3)
    d.line([(24,3),(29,3),(29,28),(24,28)], fill=INK, width=3)
    d.line([(8,3),(3,3),(3,27),(8,27)], fill="#d5a6b5")
    d.line([(24,3),(29,3),(29,27),(24,27)], fill="#d5a6b5")
    d.polygon([(7,8),(10,5),(14,5),(16,8),(18,5),(22,5),(25,8),
               (25,15),(16,25),(7,15)], fill=INK)
    d.polygon([(9,9),(11,7),(13,7),(16,11),(19,7),(21,7),(23,9),
               (23,14),(16,22),(9,14)], fill="#db667f")
    d.polygon([(10,9),(12,8),(14,10),(16,13),(20,8),(22,9),(22,13),
               (16,19),(10,13)], fill="#f58a97")
    d.line((11,9,13,9), fill="#ffe4ce")
    d.line([(17,20),(21,16),(22,14)], fill="#a73d65")
    d.rectangle((23,21,27,31), fill=INK)
    d.rectangle((20,24,30,28), fill=INK)
    d.rectangle((24,22,26,30), fill=PALE)
    d.rectangle((21,25,29,27), fill=PALE)
    return image


def shots():
    image, d = canvas()
    # Three separate rounds, one leading; the count reads at gameplay size.
    for x,y in ((3,11),(13,3),(23,11)):
        d.polygon([(x+3,y),(x+7,y+5),(x+7,y+17),(x,y+17),(x,y+5)], fill=INK)
        d.polygon([(x+3,y+2),(x+5,y+5),(x+5,y+13),(x+2,y+13),(x+2,y+5)], fill="#ac8de4")
        d.line([(x+2,y+6),(x+2,y+12)], fill="#e8dcff")
        d.line((x+2,y+15,x+5,y+15), fill="#d0b4f5")
    d.line((16,23,16,29), fill=INK, width=3)
    d.line((16,24,16,28), fill="#ac8de4")
    return image


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    REVIEW.mkdir(parents=True, exist_ok=True)
    icons = {"AttackSpeed": speed(), "AttackDamage": damage(), "MaxHealth": health(), "ExtraShot": shots()}
    template = (DEST / "Reforge.png.meta").read_text()
    for name, icon in icons.items():
        icon.save(DEST / f"{name}.png")
        meta = DEST / f"{name}.png.meta"
        if not meta.exists():
            lines = template.splitlines()
            lines[1] = f"guid: {uuid.uuid4().hex}"
            meta.write_text("\n".join(lines) + "\n")
    sheet = Image.new("RGB", (768, 256), "#17232f")
    d = ImageDraw.Draw(sheet)
    for i, (name, icon) in enumerate(icons.items()):
        for scale, y in ((4, 14), (2, 152)):
            large = icon.resize((32*scale, 32*scale), Image.Resampling.NEAREST)
            sheet.paste(large, (i*192+(192-32*scale)//2, y), large)
        d.text((i*192+40, 234), name, fill=PALE)
    sheet.save(REVIEW / "Icons.png")
    print("Wrote four stat reward icons and preview.")


if __name__ == "__main__":
    main()
