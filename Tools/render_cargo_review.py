"""Package the actual isolated cargo gameplay captures and checks."""
from pathlib import Path
import shutil
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/StatHexReview/ArtSource/DecisionExpansionReview"
DEST = ROOT / "ArtSource/CargoReview"


def main():
    report = (SOURCE / "Verification.txt").read_text()
    if not report.startswith("PASS"):
        raise RuntimeError(report)
    DEST.mkdir(parents=True, exist_ok=True)
    shutil.copy2(SOURCE / "Verification.txt", DEST / "Verification.txt")
    names = ("Common", "Rare", "Epic", "Legendary")
    colors = ("#dbdbdb", "#40bfff", "#d95cff", "#ffbd1f")
    sheet = Image.new("RGB", (1280, 352), "#111e28")
    draw = ImageDraw.Draw(sheet)
    for i, (name, color) in enumerate(zip(names, colors)):
        source = SOURCE / f"Cargo-{name}.png"
        shutil.copy2(source, DEST / source.name)
        with Image.open(source) as image:
            w, h = image.size
            crop = image.crop((w//2-110, int(h*.59), w//2+110, int(h*.79)))
            crop.thumbnail((292, 282))
            crop = crop.resize((292, 282), Image.Resampling.NEAREST)
            sheet.paste(crop, (i*320+14, 52))
        draw.text((i*320+18, 14), name.upper(), fill=color)
        draw.text((i*320+18, 30), "On pickup" if i == 0 else f"After {i} wave{'s' if i > 1 else ''}", fill="#a8b6ba")
    shutil.copy2(SOURCE / "Cargo-Opening.png", DEST / "Cargo-Opening.png")
    sheet.save(DEST / "RarityOutlines.png")
    print(DEST)


if __name__ == "__main__":
    main()
