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
    # Row 3: item materials.
    fill(img, *P(0, 3), lambda x, y: shade((236, 186, 40), 1.25 if x + y < 9 else 0.8 if x + y > 24 else 1.0))  # 0 gold
    fill(img, *P(1, 3), lambda x, y: shade((236, 232, 222), 1.05 if y < 4 else 0.85 if y > 12 else 1.0))       # 1 ceramic
    fill(img, *P(2, 3), lambda x, y: (120, 72, 36) if (x * 3 + y * 5) % 11 == 0 else (66, 36, 18))             # 2 coffee
    fill(img, *P(3, 3), lambda x, y: shade((226, 170, 128), 1.1 if y < 5 else 0.9 if y > 11 else 1.0))         # 3 skin
    fill(img, *P(4, 3), moon)                                                                                     # 4 moon
    plate(img, *P(5, 3), (60, 150, 64))                                                                           # 5 green
    fill(img, *P(6, 3), lambda x, y: shade((40, 96, 230), 1.25 if x < 3 else 0.85 if x > 12 else 1.0))         # 6 blue plastic
    fill(img, *P(7, 3), lambda x, y: (70, 72, 80) if x + y in (4, 5) else (28, 28, 32))                        # 7 black plastic
    # Row 4: item decals.
    fill(img, *P(0, 4), lambda x, y: (220, 30, 30) if (6 <= x <= 9 and 2 <= y <= 13) or (2 <= x <= 13 and 6 <= y <= 9) else (240, 238, 232))  # 0 med cross
    fill(img, *P(1, 4), bolt)                                                                                     # 1 battery bolt
    fill(img, *P(2, 4), coin)                                                                                     # 2 coin face
    fill(img, *P(3, 4), letter_p)                                                                                 # 3 protein label
    fill(img, *P(4, 4), lambda x, y: (250, 220, 40) if bolt_mask(x, y) else (34, 30, 10))                       # 4 bolt on black
    fill(img, *P(5, 4), lambda x, y: shade((200, 30, 30), 1.2 if y < 5 else 0.85 if y > 11 else 1.0))          # 5 magnet red
    fill(img, *P(6, 4), lambda x, y: shade((210, 216, 226), 1.25 - abs(x - 5) / 14))                           # 6 chrome
    fill(img, *P(7, 4), lambda x, y: shade((196, 150, 60), 1.2 if x < 4 else 0.85 if x > 12 else 1.0))         # 7 brass
    # Row 5: boss / hull detail.
    plate(img, *P(0, 5), (90, 34, 120))                                                                           # 0 deep purple
    fill(img, *P(1, 5), lambda x, y: (255, 220, 120) if x % 4 == 1 and y % 5 == 2 else (24, 26, 34))          # 1 lit windows
    fill(img, *P(2, 5), lambda x, y: (230, 230, 230) if digit07(x, y) else (60, 64, 72))                        # 2 hull number
    fill(img, *P(3, 5), lambda x, y: (220, 30, 30) if ((x + y) // 4) % 2 == 0 else (236, 236, 236))           # 3 red caution
    plate(img, *P(4, 5), (90, 84, 80), grime=0.5)                                                                 # 4 scorched steel
    fill(img, *P(5, 5), lambda x, y: (20, 20, 24) if x % 3 == 0 or y % 3 == 0 else (96, 100, 108))            # 5 grating
    fill(img, *P(6, 5), lambda x, y: (40, 230, 255) if 3 <= y <= 4 or y == 8 or (y == 11 and x < 9) else (8, 30, 40))  # 6 screen
    plate(img, *P(7, 5), (230, 230, 236))                                                                         # 7 bright white armour
    # Row 6: single-colour themes (enemies: grey only; player: black with yellow).
    plate(img, *P(0, 6), (44, 44, 48))                                                                            # 0 black armour
    plate(img, *P(1, 6), (236, 190, 24))                                                                          # 1 yellow paint
    fill(img, *P(2, 6), lambda x, y: (176, 178, 182) if ((x + y) // 4) % 2 == 0 else (52, 54, 58))           # 2 grey hazard
    fill(img, *P(3, 6), lambda x, y: (190, 200, 210) if x + y in (5, 6, 19) else (40, 42, 46))               # 3 grey glass
    plate(img, *P(4, 6), (104, 108, 114))                                                                         # 4 mid grey plate
    plate(img, *P(5, 6), (150, 154, 160))                                                                         # 5 light grey plate
    fill(img, *P(6, 6), lambda x, y: (200, 200, 200) if x % 4 == 1 and y % 5 == 2 else (30, 31, 34))        # 6 grey windows
    fill(img, *P(7, 6), lambda x, y: (250, 210, 40) if 6 <= y <= 9 else (36, 36, 40))                         # 7 yellow stripe on black
    # Row 7: level 1 grass field (seamless, no borders, so ground cells tile).
    fill(img, *P(0, 7), lambda x, y: grass(x, y, 0))                                                              # 0 grass
    fill(img, *P(1, 7), lambda x, y: (236, 226, 120) if (x, y) in ((3, 4), (11, 9), (6, 13)) else (230, 120, 170) if (x, y) in ((12, 2), (2, 10)) else grass(x, y, 3))  # 1 grass + flowers
    fill(img, *P(2, 7), lambda x, y: shade(grass(x, y, 7), 0.86 if (x * 5 + y * 3) % 7 < 3 else 0.95))      # 2 dark tufts
    fill(img, *P(3, 7), lambda x, y: shade((118, 92, 60), 0.8 + 0.25 * (((x * 7 + y * 11) % 5) / 4)))      # 3 dirt
    fill(img, *P(4, 7), lambda x, y: leaf(x, y, (98, 124, 72)))                                                  # 4 leaves (muted)
    fill(img, *P(5, 7), lambda x, y: leaf(x, y, (62, 84, 54)))                                                   # 5 dark leaves (muted)
    fill(img, *P(6, 7), lambda x, y: shade((96, 66, 40), 0.75 if x % 4 == 1 else 1.0 + 0.1 * (y % 3 == 0)))  # 6 bark
    fill(img, *P(7, 7), lambda x, y: (190, 220, 240) if (x + 2 * y) % 13 == 0 else shade((70, 110, 150), 0.9 + 0.2 * (y / 15)))  # 7 puddle water
    return img


def grass(x, y, seed):
    """Seamless mottled grass: two greens plus short light blades."""
    v = ((x * 13 + y * 7 + seed * 5) % 11) / 10
    base = (78 + int(v * 18), 132 + int(v * 26), 52 + int(v * 10))
    if (x + seed) % 5 == 0 and (y * 3 + seed) % 7 < 2:
        return (132, 186, 84)
    return base


def leaf(x, y, c):
    k = 1.25 if (x + y) % 6 == 0 else 0.7 if (x * 3 + y) % 7 == 0 else 1.0
    return shade(c, k)


def bolt_mask(x, y):
    pts = {(9, 1), (8, 2), (8, 3), (7, 4), (7, 5), (6, 6), (6, 7), (7, 7), (8, 7), (9, 7), (9, 8), (8, 9), (8, 10), (7, 11), (7, 12), (6, 13), (6, 14)}
    return (x, y) in pts or (x - 1, y) in pts


def bolt(x, y):
    return (240, 244, 255) if bolt_mask(x, y) else shade((40, 96, 230), 1.2 if x < 3 else 1.0)


def coin(x, y):
    d = ((x - 7.5) ** 2 + (y - 7.5) ** 2) ** .5
    s = {(9, 4), (8, 4), (7, 4), (6, 5), (6, 6), (7, 7), (8, 7), (9, 8), (9, 9), (8, 10), (7, 10), (6, 10)}
    if (x, y) in s or (x == 7 and 3 <= y <= 11):
        return (110, 70, 10)
    return (176, 128, 20) if d > 6.5 else (250, 206, 60) if x + y < 12 else (230, 180, 40)


def letter_p(x, y):
    if (x in (5, 6) and 3 <= y <= 12) or (y in (3, 7) and 5 <= x <= 10) or (x == 10 and 3 <= y <= 7):
        return (30, 70, 220)
    return (236, 228, 200)


def moon(x, y):
    base = 150 + ((x * 7 + y * 13) % 5) * 4
    for cx, cy, r in ((4, 4, 2.5), (11, 9, 3), (5, 12, 2)):
        d = ((x - cx) ** 2 + (y - cy) ** 2) ** .5
        if d < r - 0.8:
            base = 104 if y < cy else 124
        elif d < r + 0.3:
            base = 186 if y > cy else 112
    return (base, base, base + 4)


def digit07(x, y):
    zero = (2 <= x <= 6 and y in (3, 12)) or (x in (2, 6) and 3 <= y <= 12)
    seven = (9 <= x <= 13 and y == 3) or (x == 13 - (y - 3) // 3 and 3 <= y <= 12)
    return zero or seven


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


def fireball():
    """Tileable 64px fire skin for the PSX sphere explosions: posterised hot blobs on red, wrapping in both axes.
    The shader scrolls it across slowly rotating spheres, like 32-bit era explosions."""
    import math as m
    s = 64
    img = Image.new("RGB", (s, s))
    blobs = [(rng.uniform(0, s), rng.uniform(0, s), rng.uniform(5, 13)) for _ in range(22)]
    for y in range(s):
        for x in range(s):
            v = 0.0
            for bx, by, r in blobs:
                dx = min(abs(x - bx), s - abs(x - bx)); dy = min(abs(y - by), s - abs(y - by))
                v += max(0.0, 1 - (dx * dx + dy * dy) ** .5 / r)
            v += 0.15 * m.sin(x * 2 * m.pi / 32) * m.sin(y * 2 * m.pi / 16)
            v += (bayer(x, y) - 0.5) * 0.35
            col = (255, 250, 210) if v > 1.25 else (255, 214, 70) if v > 0.9 else (246, 120, 24) if v > 0.55 else (176, 38, 16) if v > 0.25 else (70, 14, 10)
            img.putpixel((x, y), q5(col))
    return img


def periodic_noise(size, cells, seed):
    """Tileable value noise: a cells x cells random lattice, wrapped, smoothly interpolated."""
    r = random.Random(seed)
    lattice = [[r.random() for _ in range(cells)] for _ in range(cells)]
    step = size / cells
    out = []
    for y in range(size):
        row = []
        for x in range(size):
            gx, gy = x / step, y / step
            x0, y0 = int(gx) % cells, int(gy) % cells
            x1, y1 = (x0 + 1) % cells, (y0 + 1) % cells
            tx, ty = gx - int(gx), gy - int(gy)
            tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
            a = lattice[y0][x0] * (1 - tx) + lattice[y0][x1] * tx
            b = lattice[y1][x0] * (1 - tx) + lattice[y1][x1] * tx
            row.append(a * (1 - ty) + b * ty)
        out.append(row)
    return out


def grass_texture():
    """256px seamless ground for level 1 (one repeat per 10 world units): muted olive greens from layered noise, darker clumps,
    dry patches and short light blade flecks, quantised to a small palette like a PS1 texture."""
    s = 256
    big, mid, fine = periodic_noise(s, 7, 1), periodic_noise(s, 16, 2), periodic_noise(s, 64, 3)
    palette = [(58, 70, 48), (70, 84, 56), (82, 96, 63), (94, 106, 70), (108, 116, 78), (124, 126, 88)]
    img = Image.new("RGB", (s, s))
    blades = random.Random(9)
    for y in range(s):
        for x in range(s):
            v = big[y][x] * .5 + mid[y][x] * .3 + fine[y][x] * .2
            v += (bayer(x, y) - .5) * .08
            k = max(0, min(len(palette) - 1, int(v * len(palette) * 1.15 - .4)))
            img.putpixel((x, y), q5(palette[k]))
    for _ in range(1680):
        x, y = blades.randrange(s), blades.randrange(s)
        light = palette[min(len(palette) - 1, 4 + blades.randrange(2))]
        for d in range(blades.choice((1, 2))):
            img.putpixel((x, (y - d) % s), q5(light))
    return img


def cloud_texture():
    """64x32 soft cloud sprite: one merged metaball body with a noisy rim, white on top shading to
    grey underneath, and alpha that falls off in ordered-dither steps (PS1-style transparency)."""
    import math as m
    w, h = 64, 32
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    r = random.Random(4)
    # Cumulus profile: a flat base row with three domes on top.
    puffs = [(x, 21, 5.5) for x in range(12, 54, 5)] + [(22, 14, 7), (33, 11, 8.5), (44, 15, 6)]
    edge = periodic_noise(64, 8, 5)
    for y in range(h):
        for x in range(w):
            f = sum(m.exp(-((x - px) ** 2 + ((y - py) * 1.3) ** 2) / (pr * pr)) for px, py, pr in puffs)
            f += (edge[y * 2 % 64][x] - .5) * .3
            a = max(0.0, min(1.0, (f - .55) / .45))
            if a <= 0 or bayer(x, y) > a * 1.6:
                continue
            g = int(238 - (y / h) * 46 - (1 - a) * 24)
            img.putpixel((x, y), q5((g, g + 2, g + 6)) + (int(210 * min(1, a * 1.4)),))
    return img

if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    FX.mkdir(parents=True, exist_ok=True)
    atlas().save(OUT / "PSXv2_Atlas.png")
    fireball().save(FX / "Fireball_64.png")
    (OUT / "Field").mkdir(exist_ok=True)
    grass_texture().save(OUT / "Field" / "Grass_256.png")
    cloud_texture().save(OUT / "Field" / "Cloud_64x32.png")
    print("ok")
