"""Draw the giant orb projectile sprite (neutral grayscale; colour is applied in game code).

A thick bright rim around a dark core, with a ring of spikes reaching from the rim into
the core, like a large Touhou "big bullet". Writes Assets/Resources/ProjectileSprites/giant_orb.png
and a tinted preview to ArtSource/GiantOrbPreview.png.
"""
import math
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SIZE = 33
C = (SIZE - 1) / 2
OUTER, RIM_IN, CORE = 16.0, 11.0, 4.5
SPIKES = 18


def shade(x, y):
    dx, dy = x - C, y - C
    r = math.hypot(dx, dy)
    if r > OUTER:
        return None
    if r > OUTER - 1.2:
        return 36                      # dark ink outline keeps it readable on bright scenery
    if r > RIM_IN:
        # Bright rim, slightly darker toward its inner edge.
        return int(238 - 40 * max(0, (RIM_IN + 1.5 - r)) / 1.5) if r < RIM_IN + 1.5 else 238
    angle = math.atan2(dy, dx)
    # Spikes alternate long and short, reaching inward from the rim.
    wave = abs(math.cos(angle * SPIKES / 2))
    reach = RIM_IN - (1.2 + 5.2 * wave ** 4) * (1.0 if int((angle + math.pi) / (2 * math.pi) * SPIKES) % 2 == 0 else .65)
    if r > reach:
        return int(150 + 80 * (r - reach) / max(.1, RIM_IN - reach))
    if r < CORE:
        return 44
    return int(40 + 25 * (r - CORE) / max(.1, reach - CORE))


def draw():
    im = Image.new('RGBA', (SIZE, SIZE), (0, 0, 0, 0))
    px = im.load()
    for y in range(SIZE):
        for x in range(SIZE):
            v = shade(x, y)
            if v is not None:
                px[x, y] = (v, v, v, 255)
    return im


def preview(sprite):
    tint = (255, 92, 190)  # Space hostile pink
    board = Image.new('RGB', (420, 220), (10, 8, 30))
    big = sprite.resize((SIZE * 6, SIZE * 6), Image.NEAREST)
    tinted = Image.new('RGBA', big.size)
    tp, bp = tinted.load(), big.load()
    for y in range(big.size[1]):
        for x in range(big.size[0]):
            r, g, b, a = bp[x, y]
            tp[x, y] = (r * tint[0] // 255, g * tint[1] // 255, b * tint[2] // 255, a)
    board.paste(big, (10, 11), big)
    board.paste(tinted, (220, 11), tinted)
    return board


if __name__ == '__main__':
    sprite = draw()
    out = ROOT / 'Assets/Resources/ProjectileSprites/giant_orb.png'
    sprite.save(out)
    (ROOT / 'ArtSource').mkdir(exist_ok=True)
    preview(sprite).save(ROOT / 'ArtSource/GiantOrbPreview.png')
    print(out)
