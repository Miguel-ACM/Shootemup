"""Assemble native Unity shatter frames; this does not generate game artwork."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Temp/StatHexReview/ArtSource/StatHexReview"
DEST = ROOT / "ArtSource/HexBreakReview"
DEST.mkdir(parents=True, exist_ok=True)
frames = [Image.open(SOURCE / f"Shatter-{i:02}.png").convert("RGB") for i in range(12)]
frames[3].save(DEST / "Shatter.png")
crops = [frame.crop((285, 500, 460, 705)).resize((350, 410), Image.Resampling.NEAREST) for frame in frames]
crops[0].save(DEST / "Shatter.gif", save_all=True, append_images=crops[1:],
              duration=[65] * 11 + [800], loop=0)
sheet = Image.new("RGB", (350 * 4, 445), "#10161b")
draw = ImageDraw.Draw(sheet)
for col, index in enumerate((0, 2, 5, 8)):
    sheet.paste(crops[index], (col * 350, 30))
    draw.text((col * 350 + 12, 10), f"{index * .065:.2f} s", fill="#c9d6da")
sheet.save(DEST / "Sequence.png")
