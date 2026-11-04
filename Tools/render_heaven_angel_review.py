"""Compose the actual Unity angel review; no shipping art is altered."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1] / 'ArtSource/HeavenVoidReview'
out = root / 'Angel'
sheet = Image.new('RGB', (1440, 760), '#13151d')
draw = ImageDraw.Draw(sheet)
for i in range(8):
    picture = Image.open(out / f'Model-{i}.png').convert('RGB').resize((360, 360))
    x, y = i % 4 * 360, i // 4 * 380
    sheet.paste(picture, (x, y))
    draw.text((x + 10, y + 360), f'{"Front" if i < 4 else "Angled"} / rotation at {i % 4 * 5}s', fill='#ede4c8')
sheet.save(out / 'Models.jpg', quality=92)
frames = []
for phase in range(1, 4):
    for source in sorted((root / f'Frames/Heaven-Angel-{phase}').glob('*.jpg'))[5:40]:
        picture = Image.open(source).convert('RGB')
        width, height = picture.size
        span = round(height * 405 / 720)
        x = (width - span) // 2
        frames.append(picture.crop((x, 0, x + span, height)).resize((270, 480)).quantize(colors=96))
if frames:
    frames[0].save(out / 'Angel.gif', save_all=True, append_images=frames[1:], duration=200, loop=0, optimize=True)
