"""Generates the 128x128 PSXv2 pixel atlas (8x8 tiles of 16px) and the FX sprite sheets.

Run: python ArtSource/PSXv2/make_atlas.py
Writes Assets/Art/PSXv2/PSXv2_Atlas.png and Assets/Art/PSXv2/FX/*.png.
Tile layout is mirrored in build_ships.py (TILES dict).
"""
import random
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "Art" / "PSXv2"
FX = OUT / "FX"
T = 16
rng = random.Random(7)


def q5(c):
    """Quantise to the PS1's 15-bit colour (5 bits per channel)."""
    return tuple(min(255, max(0, round(v / 255 * 31) * 255 // 31)) for v in c[:3]) + tuple(c[3:])


def shade(c, k):
    return tuple(int(max(0, min(255, v * k))) for v in c[:3])


def noisy(c, amt=6):
    n = rng.randint(-amt, amt)
    return tuple(max(0, min(255, v + n)) for v in c)


def plate(img, ox, oy, base, rivets=True, seam=True, grime=0.0):
    hi, lo, line = shade(base, 1.18), shade(base, 0.58), shade(base, 0.72)
    for y in range(T):
        for x in range(T):
            c = noisy(base)
            if rng.random() < grime:
                c = shade(c, 0.85)
            if x == 0 or y == 0:
                c = hi
            if x == T - 1 or y == T - 1:
                c = lo
            if seam and y == 8 and 0 < x < T - 1:
                c = line
            if seam and y == 9 and 0 < x < T - 1:
                c = shade(base, 1.08)
            img.putpixel((ox + x, oy + y), q5(c))
    if rivets:
        for rx, ry in ((2, 2), (13, 2), (2, 13), (13, 13)):
            img.putpixel((ox + rx, oy + ry), q5(shade(base, 0.5)))
            img.putpixel((ox + rx - 1, oy + ry - 1), q5(shade(base, 1.3)))


def fill(img, ox, oy, fn):
    for y in range(T):
        for x in range(T):
            img.putpixel((ox + x, oy + y), q5(fn(x, y)))


def glow(core, rim):
    def f(x, y):
        d = max(abs(x - 7.5), abs(y - 7.5)) / 7.5
        return tuple(int(core[i] * (1 - d) + rim[i] * d) for i in range(3))
    return f


def atlas():
    img = Image.new("RGB", (T * 8, T * 8), (0, 0, 0))
    P = lambda c, r: (c * T, r * T)
    # Row 0: structural steel.
    plate(img, *P(0, 0), (206, 210, 216))                      # 0 light hull (Ikaruga white)
    plate(img, *P(1, 0), (128, 134, 142), grime=0.1)           # 1 mid steel
    plate(img, *P(2, 0), (62, 66, 74), grime=0.15)             # 2 dark steel
    fill(img, *P(3, 0), lambda x, y: (28, 30, 34) if y % 3 == 0 or x in (0, 15) else (88, 92, 100))  # 3 vent grille
    fill(img, *P(4, 0), lambda x, y: (232, 176, 24) if ((x + y) // 4) % 2 == 0 else (24, 22, 20))   # 4 hazard
    fill(img, *P(5, 0), lambda x, y: noisy((22, 23, 27), 3))   # 5 black trim
    fill(img, *P(6, 0), lambda x, y: (70, 74, 82) if (x // 4 + y // 4) % 2 else (46, 48, 54))       # 6 greeble checker
    fill(img, *P(7, 0), lambda x, y: shade((120, 126, 134), 0.6 + 0.5 * abs(((x % 4) - 1.5) / 1.5)))  # 7 pipe ribs
    # Row 1: faction paint (with panel seam so it reads as armour).
    plate(img, *P(0, 1), (36, 92, 200))                         # 0 player blue
    plate(img, *P(1, 1), (186, 36, 36))                         # 1 interceptor red
    plate(img, *P(2, 1), (226, 136, 20))                        # 2 rotor orange
    plate(img, *P(3, 1), (24, 140, 128))                        # 3 gunboat teal
    plate(img, *P(4, 1), (120, 40, 150))                        # 4 warden purple
    plate(img, *P(5, 1), (122, 70, 40), grime=0.4)              # 5 rust
    fill(img, *P(6, 1), lambda x, y: (150, 220, 255) if x + y in (5, 6, 19) else (14, 30, 70 + y * 3))  # 6 glass
    fill(img, *P(7, 1), lambda x, y: (230, 230, 230) if (4 <= x <= 11 and y in (3, 12)) or (x in (4, 11) and 3 <= y <= 12) else (40, 42, 48))  # 7 marking
    # Row 2: emissive glows.
    fill(img, *P(0, 2), glow((220, 255, 255), (20, 150, 255)))  # 0 engine cyan
    fill(img, *P(1, 2), glow((255, 240, 160), (240, 90, 10)))   # 1 orange
    fill(img, *P(2, 2), glow((255, 220, 200), (220, 0, 20)))    # 2 red eye
    fill(img, *P(3, 2), glow((255, 210, 255), (170, 0, 220)))   # 3 reactor magenta
    fill(img, *P(4, 2), lambda x, y: (255, 255, 255))           # 4 white hot (hitbox core)
    fill(img, *P(5, 2), glow((220, 255, 200), (30, 200, 60)))   # 5 green
    fill(img, *P(6, 2), glow((255, 255, 220), (230, 200, 40)))  # 6 yellow lamp
    fill(img, *P(7, 2), lambda x, y: (255, 30, 90))             # 7 hitbox ring
    return img


def flipbook(name, frames, size, draw):
    sheet = Image.new("RGBA", (size * frames, size), (0, 0, 0, 0))
    for f in range(frames):
        fr = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        draw(fr, f / max(1, frames - 1), size)
        sheet.paste(fr, (f * size, 0))
    sheet.save(FX / f"{name}.png")


def bayer(x, y):
    m = (0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5)
    return (m[(y & 3) * 4 + (x & 3)] + 0.5) / 16


def explosion(img, t, s):
    """Dithered fireball: white core -> orange -> red -> smoke ring, alpha via ordered dither."""
    c = (s - 1) / 2
    r_out = 3 + t * (s / 2 - 2)
    r_in = max(0, (t - 0.35) * 1.6) * r_out
    local = random.Random(int(t * 100))
    blobs = [(local.uniform(-1, 1) * r_out * .4, local.uniform(-1, 1) * r_out * .4) for _ in range(5)]
    for y in range(s):
        for x in range(s):
            d = min(((x - c - bx) ** 2 + (y - c - by) ** 2) ** .5 for bx, by in blobs)
            if d > r_out or d < r_in:
                continue
            k = (d - r_in) / max(1e-3, r_out - r_in)
            heat = (1 - t) * (1 - k * .7)
            if heat > .75: col = (255, 255, 230)
            elif heat > .5: col = (255, 200, 60)
            elif heat > .3: col = (240, 90, 20)
            elif heat > .12: col = (150, 30, 20)
            else: col = (60, 56, 60)
            if bayer(x, y) > (1.15 - t) * (1.2 - k * .5):
                continue
            img.putpixel((x, y), q5(col) + (255,))


def spark(img, t, s):
    c = (s - 1) / 2
    L = (1 - t) * (s / 2)
    col = (255, 255, 255) if t < .3 else (255, 220, 120) if t < .6 else (255, 120, 40)
    for i in range(int(L) + 1):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            x, y = int(c + dx * i), int(c + dy * i)
            if 0 <= x < s and 0 <= y < s:
                img.putpixel((x, y), q5(col) + (255,))
    for dx, dy in ((1, 1), (-1, -1), (1, -1), (-1, 1)):
        for i in range(int(L * .5) + 1):
            x, y = int(c + dx * i), int(c + dy * i)
            if 0 <= x < s and 0 <= y < s and t < .7:
                img.putpixel((x, y), q5(col) + (255,))


def muzzle(img, t, s):
    c = (s - 1) / 2
    for y in range(s):
        for x in range(s):
            nx, ny = (x - c) / (s / 2), (y - c) / (s / 2)
            d = abs(nx) * 2.2 + abs(ny) * (0.9 if ny < 0 else 2.5)
            if d < (1 - t) * 1.1 and bayer(x, y) < 1.2 - t:
                col = (255, 255, 255) if d < .4 else (150, 230, 255) if d < .75 else (40, 140, 255)
                img.putpixel((x, y), q5(col) + (255,))


def hitbox_glow(img, t, s):
    """Pulsing ring drawn around the cockpit core (4 frames loop)."""
    c = (s - 1) / 2
    r = s * (.34 + .06 * abs(1 - 2 * t))
    for y in range(s):
        for x in range(s):
            d = ((x - c) ** 2 + (y - c) ** 2) ** .5
            if abs(d - r) < 1.0:
                img.putpixel((x, y), (255, 40, 100, 255))
            elif d < r - 1 and bayer(x, y) < .18:
                img.putpixel((x, y), (255, 120, 170, 160))


def smoke(img, t, s):
    c = (s - 1) / 2
    r = 2 + t * (s / 2 - 3)
    for y in range(s):
        for x in range(s):
            d = ((x - c) ** 2 + (y - c + t * 3) ** 2) ** .5
            if d < r and bayer(x, y) < (1 - t) * .75 * (1 - d / r * .6):
                g = int(90 - 40 * t)
                img.putpixel((x, y), q5((g, g, g + 6)) + (255,))


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    FX.mkdir(parents=True, exist_ok=True)
    atlas().save(OUT / "PSXv2_Atlas.png")
    flipbook("Explosion_8x32", 8, 32, explosion)
    flipbook("ExplosionBig_8x64", 8, 64, explosion)
    flipbook("Spark_4x16", 4, 16, spark)
    flipbook("MuzzleFlash_3x16", 3, 16, muzzle)
    flipbook("HitboxPulse_4x32", 4, 32, hitbox_glow)
    flipbook("Smoke_6x16", 6, 16, smoke)
    print("ok")
