"""Draw candidate player missile and flamethrower sprites for review.

The shipping player sprites are generated in code (PlayerMissileArt.cs and
PlayerFlameArt.cs). The projectile shader neutralizes them to max(r, g, b) and
multiplies by the friendly hue (blue for the missile, orange for the flame), then
adds emission to the bright ink. These candidates are therefore drawn in neutral
grey: a dark rim, mid body tones and a restrained highlight, never a glaring
white column.

Previews only. Nothing is copied into Assets until Miguel approves a design.
Usage: python Tools/draw_player_ordnance.py
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "ArtSource" / "OrdnanceReview"
SAMPLES = 4
MISSILE_HUE = (.32, .72, .85)
FLAME_HUE = (1, .45, .12)
EMISSION = 1.15


def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def sprite(width, height, inside, tone):
    """inside(x, y) -> bool and tone(x, y, edge) -> grey 0..1 or None, in pixel units.

    Coordinates have (0, 0) at the sprite centre and +y pointing up (the direction of
    travel). edge is True on the outermost covered pixels, which become the dark rim
    unless tone returns a value for them.
    """
    mask = [[False] * width for _ in range(height)]
    for row in range(height):
        for x in range(width):
            hits = 0
            for sy in range(SAMPLES):
                for sx in range(SAMPLES):
                    px = x + (sx + .5) / SAMPLES - width / 2
                    py = (height - 1 - row) + (sy + .5) / SAMPLES - height / 2
                    hits += inside(px, py)
            mask[row][x] = hits * 2 >= SAMPLES * SAMPLES

    def filled(x, row):
        return 0 <= x < width and 0 <= row < height and mask[row][x]

    image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    pixels = image.load()
    for row in range(height):
        for x in range(width):
            if not mask[row][x]:
                continue
            edge = not (filled(x - 1, row) and filled(x + 1, row) and filled(x, row - 1) and filled(x, row + 1))
            cx, cy = x + .5 - width / 2, (height - 1 - row) + .5 - height / 2
            value = tone(cx, cy, edge)
            if value is None:
                continue
            if isinstance(value, tuple):
                grey, alpha = value
            else:
                grey, alpha = value, 1
            g = int(round(max(0, min(1, grey)) * 255))
            pixels[x, row] = (g, g, g, int(round(alpha * 255)))
    return image


# Current shipping sprites, transcribed from the C# generators (Unity rows count up).
def current_missile():
    ink, blue, teal, highlight, seeker = .18, .61, .72, .84, .76
    image = Image.new("RGBA", (16, 32), (0, 0, 0, 0))
    for y in range(32):
        for x in range(16):
            side = abs(x - 7.5)
            edge = 7.5 - y * .32 if y < 10 else (4.5 + (.5 - 4.5) * (y - 23) / 8 if y > 23 else 4.5)
            if side > edge or y < 2:
                continue
            if side > edge - 1.5:
                v = ink
            elif y < 7 and side < 2:
                v = seeker
            elif y > 20 and x < 8:
                v = highlight
            else:
                v = teal if x < 8 else blue
            g = int(v * 255)
            image.putpixel((x, 31 - y), (g, g, g, 255))
    return image


def current_flame():
    image = Image.new("RGBA", (24, 32), (0, 0, 0, 0))
    for y in range(32):
        for x in range(24):
            side = abs(x - 11.5 + math.sin(y * .29) * 1.2)
            edge = math.sin(max(0, min(1, (y + 1) / 33)) * math.pi) * 10.5
            if side > edge or y < 2:
                continue
            if side > edge - 2:
                v, a = .32, .8
            elif side < edge * .22 and 10 < y < 23:
                v, a = .93, .95
            elif side > edge * .55:
                v, a = .66, .9
            else:
                v, a = .78, .95
            image.putpixel((x, 31 - y), (int(v * 255),) * 3 + (int(a * 255),))
    return image


# ---------------------------------------------------------------- missiles (16 x 32)
RIM = .17


def swept_fin(x, y, root, tip, root_top, root_bottom, tip_top, tip_bottom):
    """A trapezoid fin from the body (|x| = root) out to |x| = tip."""
    ax = abs(x)
    if ax < root - .5 or ax > tip:
        return False
    t = (ax - root) / (tip - root)
    top = root_top + (tip_top - root_top) * t
    bottom = root_bottom + (tip_bottom - root_bottom) * t
    return bottom < y < top


def missile_finned():
    """A: a proper little rocket. Ogive nose with a dark seeker tip, painted bands,
    two swept tail fins and a short exhaust bead behind the nozzle."""
    def half(y):
        if y > 5:
            t = (y - 5) / 10.5
            return 3 * math.sqrt(max(0, 1 - t * t))
        if y > -11:
            return 3
        if y > -12.5:
            return 2
        return -1

    def fin(x, y):
        return swept_fin(x, y, 3, 7.4, -3.5, -11.5, -9.5, -13)

    def exhaust(x, y):
        return -16 < y <= -12.5 and abs(x) < 1.8 * (1 - (-12.5 - y) / 3.8) + .3

    def inside(x, y):
        return abs(x) <= half(y) or fin(x, y) or exhaust(x, y)

    def tone(x, y, edge):
        body = abs(x) <= half(y)
        if exhaust(x, y) and not body:
            return .9 if abs(x) < 1 else .6
        if edge:
            return RIM
        if fin(x, y) and not body:
            return .56 if x < 0 else .42
        if y > 12:
            return .32
        if 3 < y < 4.6 or -6 < y < -4.4:
            return .4
        if x < -1:
            return .84
        return .7 if x < 1 else .56

    return sprite(16, 32, inside, tone)


def missile_seeker():
    """B: slim homing dart. Needle nose with a glowing seeker eye, swept delta
    wings at mid-body and small tail fins."""
    def half(y):
        if y > 8:
            return 2.3 * (15.6 - y) / 7.6
        if y > -12:
            return 2.3
        return -1

    def wing(x, y):
        return swept_fin(x, y, 2.3, 7.2, 3, -5, -4.5, -7)

    def tail(x, y):
        return swept_fin(x, y, 2.3, 4.6, -9, -14, -12.5, -15.5)

    def inside(x, y):
        return abs(x) <= half(y) or wing(x, y) or tail(x, y)

    def tone(x, y, edge):
        body = abs(x) <= half(y)
        if 8.5 < y < 11 and abs(x) < 1.1:
            return .92
        if edge:
            return RIM
        if wing(x, y) and not body:
            return .62 if x < 0 else .48
        if tail(x, y) and not body:
            return .44
        if y < -11:
            return .34
        return .8 if x < 0 else .62

    return sprite(16, 32, inside, tone)


def missile_bottle():
    """C: chunky firework rocket. Fat body with a conical cap and a stripe, four
    fins seen as two broad flared ones, and a split twin exhaust."""
    def half(y):
        if y > 6:
            return max(0, 3.8 * (15.5 - y) / 9.5)
        if y > -9:
            return 3.8
        if y > -10.5:
            return 2.8
        return -1

    def fins(x, y):
        return swept_fin(x, y, 3.8, 7.4, -2, -9.5, -8.5, -12.5)

    def flame(x, y):
        return any(-15.8 < y <= -10.5 and abs(x - cx) < 1.1 * (1 - (-10.5 - y) / 5.3) + .25 for cx in (-1.5, 1.5))

    def inside(x, y):
        return abs(x) <= half(y) or fins(x, y) or flame(x, y)

    def tone(x, y, edge):
        body = abs(x) <= half(y)
        if fins(x, y) and not body:
            return RIM if edge else (.5 if x < 0 else .4)
        if flame(x, y) and not body:
            return .9 if y > -13 else .6
        if edge:
            return RIM
        if y > 6:
            return .64 if x < 0 else .5
        if -1.2 < y < 1.4:
            return .36
        if x < -1.6:
            return .86
        return .72 if x < 1.4 else .58

    return sprite(16, 32, inside, tone)


# ------------------------------------------------------------------ flames (24 x 32)
def flame_noise(x, y, phase):
    return math.sin(y * .55 + phase) * .9 + math.sin(y * 1.3 + phase * 2.1) * .35


def flame_tongue():
    """A: a fire tongue that leads with a round, hot head and trails three licking
    flicks behind it. Core is hottest just behind the head, never a full column."""
    head_y, head_r = 6.5, 7.6

    def lick(x, y, cx, top, length, width):
        if y > top or y < top - length:
            return False
        t = (top - y) / length
        wobble = math.sin(t * 3.4 + cx) * 1.6 * t
        return abs(x - cx - wobble - cx * t * .6) < width * (1 - t) ** .8

    def inside(x, y):
        if (x * x) / (head_r * head_r) + ((y - head_y) ** 2) / (head_r * 1.15) ** 2 <= 1:
            return True
        return (lick(x, y, 0, 4, 20, 5.6) or lick(x, y, -4.2, 3, 14, 3.2) or lick(x, y, 4.4, 2, 12, 3))

    def tone(x, y, edge):
        r = math.hypot(x * 1.15, (y - 5.5) * .9)
        heat = 1 - r / 9.5 + flame_noise(x, y, 1.3) * .05
        if edge:
            return (.36, .82)
        if heat > .62:
            return .96, .96
        if heat > .38:
            return .8, .95
        return (.6, .9)

    return sprite(24, 32, inside, tone)


def flame_comet():
    """B: a fireball. Round hot head whose leading edge is brightest, trailing a
    single S-curved tail that tapers to a point, so a stream reads as fire whipping."""
    def head(x, y):
        return x * x + (y - 7) ** 2 <= 7.4 ** 2

    def tail(x, y):
        if y > 6 or y < -15.5:
            return False
        t = (6 - y) / 21.5
        cx = math.sin(t * 5.2) * 2.6 * t
        return abs(x - cx) < 6.6 * (1 - t) ** 1.25 + .15

    def inside(x, y):
        return head(x, y) or tail(x, y)

    def tone(x, y, edge):
        if edge:
            return (.34, .85)
        if head(x, y):
            d = math.hypot(x + .5, y - 8.4)
            if d < 3.4:
                return .97
            if d < 5.6:
                return .84
            return .7
        t = (6 - y) / 21.5
        return .66 - t * .14

    return sprite(24, 32, inside, tone)


def flame_billow():
    """C: a rolling billow. Puffs of fire of different sizes stacked behind a hot
    leading puff; mirrored on alternate rounds so the stream churns."""
    puffs = [(0, 6.5, 6.6), (-4.4, -.8, 4.6), (4.2, .4, 4.1), (-1.2, -7.2, 3.8), (2.2, -12.3, 2.6), (-1.6, -15, 1.4)]

    def inside(x, y):
        return any((x - px) ** 2 + (y - py) ** 2 <= r * r for px, py, r in puffs)

    def tone(x, y, edge):
        if edge:
            return (.36, .85)
        for index, (px, py, r) in enumerate(puffs):
            d = math.hypot(x - px, y - py)
            if d <= r:
                if index == 0 and d < r * .5:
                    return .97
                if d < r * .55:
                    return .84 - index * .03
                return .7 - index * .025
        return .6

    return sprite(24, 32, inside, tone)


# -------------------------------------------------------------------- preview sheet
def shade(image, hue, emission=EMISSION):
    """Approximate CombatProjectile.shader: neutralize, tint, add emission."""
    out = Image.new("RGBA", image.size, (0, 0, 0, 0))
    src, dst = image.load(), out.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = src[x, y]
            if a == 0:
                continue
            v = max(r, g, b) / 255
            enamel = smoothstep(.22, .8, v)
            rgb = [v * h * (1 + enamel * emission) for h in hue]
            dst[x, y] = tuple(int(min(1, c) * 255) for c in rgb) + (a,)
    return out


def checker(size, cell=8):
    image = Image.new("RGBA", size, (52, 52, 58, 255))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle([x, y, x + cell - 1, y + cell - 1], fill=(66, 66, 74, 255))
    return image


def big(image, factor):
    return image.resize((image.width * factor, image.height * factor), Image.NEAREST)


def scene(width, height, colours):
    """A simple banded backdrop standing in for dark and bright scenery."""
    image = Image.new("RGBA", (width, height), colours[0])
    draw = ImageDraw.Draw(image)
    for i, colour in enumerate(colours):
        draw.rectangle([i * width // len(colours), 0, (i + 1) * width // len(colours), height], fill=colour)
    return image


BACKDROPS = [(18, 22, 30, 255), (58, 92, 52, 255), (188, 200, 210, 255), (120, 86, 52, 255)]


def stream(tile, hue, count, rng, spread, flicker):
    """A burst of rounds at roughly gameplay size: what the player actually sees."""
    width, height = 220, 300
    base = scene(width, height, BACKDROPS)
    shaded = shade(tile, hue)
    for i in range(count):
        t = i / max(1, count - 1)
        x = width / 2 + math.sin(i * 2.399) * spread * (1 - t * .3) + (rng() - .5) * spread * .5
        y = height - 30 - t * (height - 60)
        piece = shaded
        if flicker:
            piece = shaded.transpose(Image.FLIP_LEFT_RIGHT) if i % 2 else shaded
            alpha = 1 - t * .85
            a = piece.split()[3].point(lambda p: int(p * alpha))
            piece = piece.copy()
            piece.putalpha(a)
            s = 1 + t * .5
            piece = piece.resize((int(piece.width * s), int(piece.height * s)), Image.NEAREST)
        base.alpha_composite(piece, (int(x - piece.width / 2), int(y - piece.height / 2)))
    return base


def label(draw, xy, text, fill=(220, 220, 225)):
    draw.text(xy, text, fill=fill)


def sheet(title, entries, hue, flicker, path):
    seed = [7]

    def rng():
        seed[0] = (seed[0] * 1103515245 + 12345) & 0x7FFFFFFF
        return seed[0] / 0x7FFFFFFF

    column = 330
    image = Image.new("RGBA", (column * len(entries) + 20, 840), (24, 25, 30, 255))
    draw = ImageDraw.Draw(image)
    label(draw, (12, 8), title)
    for i, (name, tile) in enumerate(entries):
        x0 = 12 + i * column
        label(draw, (x0, 30), name, (255, 214, 120) if i == 0 else (220, 220, 225))
        grey = big(tile, 8)
        back = checker(grey.size)
        back.alpha_composite(grey)
        image.alpha_composite(back, (x0, 50))
        tinted = big(shade(tile, hue), 3)
        dark = Image.new("RGBA", (tinted.width + 16, tinted.height + 16), BACKDROPS[0])
        dark.alpha_composite(tinted, (8, 8))
        image.alpha_composite(dark, (x0 + grey.width + 12, 50))
        light = Image.new("RGBA", (tinted.width + 16, tinted.height + 16), BACKDROPS[2])
        light.alpha_composite(tinted, (8, 8))
        image.alpha_composite(light, (x0 + grey.width + 12, 60 + dark.height))
        seed[0] = 7
        image.alpha_composite(stream(tile, hue, 14 if flicker else 6, rng, 34 if flicker else 60, flicker), (x0, 520))
        label(draw, (x0, 524), "gameplay size (1x)")
    image.convert("RGB").save(path)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    missiles = [("current", current_missile()), ("A finned rocket", missile_finned()),
                ("B seeker dart", missile_seeker()), ("C firework rocket", missile_bottle())]
    flames = [("current", current_flame()), ("A fire tongue", flame_tongue()),
              ("B comet fireball", flame_comet()), ("C rolling billow", flame_billow())]
    for name, tile in missiles + flames:
        tile.save(OUT / ("%s_%s.png" % ("missile" if (name, tile) in missiles else "flame", name.split()[0])))
    sheet("Player missile (blue friendly hue) - pick one", missiles, MISSILE_HUE, False, OUT / "Missile.png")
    sheet("Flamethrower round (orange friendly hue, fades over 0.55 s) - pick one", flames, FLAME_HUE, True, OUT / "Flame.png")


if __name__ == "__main__":
    main()
