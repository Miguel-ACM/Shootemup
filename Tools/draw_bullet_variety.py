"""Draw candidate hostile bullet silhouettes for review, in the approved pixel style.

Each sprite is neutral greyscale on transparency, like the shipping sprites in
Assets/Resources/ProjectileSprites: a 36 rim, 188 lit inner edge (top/left), 124
shaded inner edge (bottom/right), 238 fill and a small 255 highlight. Colour is
applied in game code, never baked into the pixels.

These are previews only. Nothing here is copied into Assets until Miguel approves.
Usage: python Tools/draw_bullet_variety.py
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "ArtSource" / "BulletVarietyReview"
RIM, LIT, SHADE, FILL, SHINE = 36, 188, 124, 238, 255
SAMPLES = 4


def rasterize(width, height, inside):
    """Coverage of a shape defined in pixel units with (0, 0) at the sprite centre."""
    mask = [[False] * width for _ in range(height)]
    for y in range(height):
        for x in range(width):
            hits = 0
            for sy in range(SAMPLES):
                for sx in range(SAMPLES):
                    px = x + (sx + .5) / SAMPLES - width / 2
                    py = y + (sy + .5) / SAMPLES - height / 2
                    hits += inside(px, py)
            mask[y][x] = hits * 2 >= SAMPLES * SAMPLES
    return mask


def shade(width, height, inside, detail=None, shine=None):
    """Turn a coverage mask into the five-tone sprite, with optional dark details."""
    pad = 1
    w, h = width + pad * 2, height + pad * 2
    core = rasterize(width, height, inside)
    solid = [[False] * w for _ in range(h)]
    for y in range(height):
        for x in range(width):
            solid[y + pad][x + pad] = core[y][x]

    def filled(x, y):
        return 0 <= x < w and 0 <= y < h and solid[y][x]

    image = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = image.load()
    for y in range(h):
        for x in range(w):
            if solid[y][x]:
                # Light comes from the upper left: edges facing it are lit, the rest shaded.
                lit_edge = not filled(x - 1, y) or not filled(x, y - 1)
                dark_edge = not filled(x + 1, y) or not filled(x, y + 1)
                tone = SHADE if dark_edge else LIT if lit_edge else FILL
                cx, cy = x - pad + .5 - width / 2, y - pad + .5 - height / 2
                if shine and tone == FILL and shine(cx, cy):
                    tone = SHINE
                if detail:
                    tone = detail(cx, cy, tone)
                if tone is not None:
                    px[x, y] = (tone, tone, tone, 255)
            elif any(filled(x + dx, y + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                px[x, y] = (RIM, RIM, RIM, 255)
    # Detail may cut holes: rim any transparent pixel that now borders the body.
    for y in range(h):
        for x in range(w):
            if px[x, y][3] == 0 and solid[y][x]:
                px[x, y] = (RIM, RIM, RIM, 255)
    return image


def ellipse(rx, ry, ox=0, oy=0):
    return lambda x, y: ((x - ox) / rx) ** 2 + ((y - oy) / ry) ** 2 <= 1


def polygon(points):
    def inside(x, y):
        hit = False
        for i in range(len(points)):
            (x1, y1), (x2, y2) = points[i], points[i - 1]
            if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1) + x1:
                hit = not hit
        return hit
    return inside


def union(*shapes):
    return lambda x, y: any(s(x, y) for s in shapes)


def star(points, outer, inner, turn=-90):
    pts = []
    for i in range(points * 2):
        r = outer if i % 2 == 0 else inner
        a = math.radians(turn + i * 180 / points)
        pts.append((math.cos(a) * r, math.sin(a) * r))
    return polygon(pts)


def top_left(radius, ox, oy):
    return lambda x, y: (x - ox) ** 2 + (y - oy) ** 2 <= radius * radius


# --- Candidates. Coordinates: pixels from the centre, +y downward on the sprite.
def rice():
    return shade(9, 15, ellipse(4.3, 7.2), shine=top_left(1.3, -1.2, -3.2))


def kunai():
    # A plain straight-sided leaf blade, a crossbar and a ring pommel, all on one axis.
    blade = polygon([(0, -10.5), (3.2, -2.5), (3.2, -.6), (-3.2, -.6), (-3.2, -2.5)])
    guard = polygon([(-3.6, -.8), (3.6, -.8), (3.6, 1), (-3.6, 1)])
    grip = polygon([(-1.2, .8), (1.2, .8), (1.2, 5.6), (-1.2, 5.6)])
    ring = ellipse(2.9, 2.9, 0, 8)
    hole = ellipse(1.3, 1.3, 0, 8)
    ridge = lambda x, y: -1 < x < .5 and -8.5 < y < -1.5
    def detail(x, y, tone):
        if hole(x, y):
            return None
        if ridge(x, y) and tone == FILL:
            return SHINE
        return tone
    return shade(9, 23, union(blade, guard, grip, ring), detail)


def crystal():
    body = polygon([(0, -8.5), (4.2, -4), (4.2, 4), (0, 8.5), (-4.2, 4), (-4.2, -4)])

    def detail(x, y, tone):
        if tone in (FILL, SHINE) and x > .5:
            return LIT  # The right-hand facet sits a step darker.
        return tone
    return shade(9, 17, body, detail, top_left(1, -1.8, -3))


def droplet():
    def inside(x, y):
        if y >= 2:
            return x * x + (y - 2) ** 2 <= 5.2 ** 2
        half = 5.2 * (y + 8) / 10
        return y >= -8 and abs(x) <= half
    return shade(11, 17, inside, shine=top_left(1.4, -2, 1))


def five_star():
    return shade(17, 17, star(5, 8.3, 3.6), shine=top_left(1.2, -1.2, -1.5))


def snowflake():
    arms = []
    for k in range(6):
        a = math.radians(k * 60 - 90)
        dx, dy = math.cos(a), math.sin(a)
        nx, ny = -dy, dx
        arms.append(lambda x, y, dx=dx, dy=dy, nx=nx, ny=ny:
                    0 <= x * dx + y * dy <= 8.4 and abs(x * nx + y * ny) <= 1.1)
        for along in (4.6,):
            for side in (-1, 1):
                bx, by = math.cos(a + side * math.radians(55)), math.sin(a + side * math.radians(55))
                cx, cy = dx * along, dy * along
                arms.append(lambda x, y, bx=bx, by=by, cx=cx, cy=cy:
                            0 <= (x - cx) * bx + (y - cy) * by <= 3 and abs((x - cx) * -by + (y - cy) * bx) <= .9)
    hub = ellipse(2.4, 2.4)
    return shade(19, 19, union(hub, *arms))


def heart():
    lobes = union(ellipse(3.7, 3.7, -3.2, -1.8), ellipse(3.7, 3.7, 3.2, -1.8))
    point = polygon([(-6.7, -.6), (6.7, -.6), (0, 6.6)])
    return shade(15, 14, union(lobes, point), shine=top_left(1.2, -3.6, -2.6))


def wrapped_candy():
    sweet = ellipse(4.6, 4.2)
    left = polygon([(-4, 0), (-10.5, -4.5), (-9.3, 0), (-10.5, 4.5)])
    right = polygon([(4, 0), (10.5, -4.5), (9.3, 0), (10.5, 4.5)])

    def detail(x, y, tone):
        if sweet(x, y) and tone in (FILL, SHINE) and abs(x - y * .8) < .9:
            return LIT  # One twist stripe across the sweet.
        return tone
    return shade(23, 11, union(sweet, left, right), detail, top_left(1, -1.8, -1.8))


def petal():
    def inside(x, y):
        # A pointed leaf, leaning a little to one side like it is drifting.
        x -= y * .18
        half = 3.9 * math.cos(math.pi * y / 17.5)
        return abs(y) <= 8.6 and abs(x) <= half
    vein = lambda x, y: abs(x - y * .18) < .55 and -6.5 < y < 7
    return shade(11, 18, inside, lambda x, y, t: SHADE if vein(x, y) and t == FILL else t)


def flame():
    # A leaning tongue of fire with a smaller flick on each side, unlike the round droplet.
    body = ellipse(4.4, 4.2, 0, 4.4)
    main = polygon([(-4.2, 3.4), (4.2, 3.4), (2.2, -3), (-.6, -9.8), (-.8, -4)])
    left = polygon([(-4.4, 4.6), (-2, 1), (-5.6, -3.6)])
    right = polygon([(4.4, 4.6), (2.4, .6), (5.4, -5.6)])
    core = ellipse(1.9, 2.6, 0, 4.6)
    return shade(13, 19, union(body, main, left, right), lambda x, y, t: SHINE if core(x, y) and t == FILL else t)


def feather():
    def inside(x, y):
        half = 3.6 * math.sin(math.pi * min(1, max(0, (y + 10) / 18.5))) ** .6
        return -10 <= y <= 8.5 and abs(x - .05 * y) <= half
    quill = lambda x, y: abs(x - .05 * y) < .5 and -8.5 < y < 10
    def detail(x, y, tone):
        if quill(x, y):
            return SHADE
        # Two notches cut into the vane, the feather's most recognisable feature.
        if tone != RIM and ((-3 < y < -1.6 and x > 1.4) or (2 < y < 3.4 and x < -1.4)):
            return None
        return tone
    stem = polygon([(-.6, 8), (.6, 8), (.6, 10.5), (-.6, 10.5)])
    return shade(9, 22, union(inside, stem), detail)


def crescent():
    outer = ellipse(7, 7)
    bite = ellipse(5.6, 5.6, 2.9, -1.4)
    return shade(15, 15, lambda x, y: outer(x, y) and not bite(x, y))


def gem():
    body = polygon([(0, -7.2), (6.3, -3.6), (6.3, 3.6), (0, 7.2), (-6.3, 3.6), (-6.3, -3.6)])
    table = polygon([(0, -3.4), (3, -1.7), (3, 1.7), (0, 3.4), (-3, 1.7), (-3, -1.7)])

    def detail(x, y, tone):
        if tone in (FILL, SHINE) and not table(x, y):
            return LIT if y < 0 or x < 0 else SHADE
        return tone
    return shade(15, 15, body, detail, top_left(1, -1, -1.2))


CANDIDATES = [
    ("rice", rice, "Touhou rice grain: dense streams that read as 'many'."),
    ("kunai", kunai, "Dagger with ring pommel: sharp aimed volleys."),
    ("crystal", crystal, "Faceted ice shard: Snow."),
    ("droplet", droplet, "Teardrop: Underwater plumes and rain."),
    ("star", five_star, "Five-point star: Heaven / Candyland sparkle."),
    ("snowflake", snowflake, "Six-arm flake: Snow's falling snow."),
    ("heart", heart, "Heart: Candyland."),
    ("candy", wrapped_candy, "Wrapped sweet: Candyland gumdrop wakes."),
    ("petal", petal, "Leaning leaf: Plains drifting fire."),
    ("flame", flame, "Flame tongue: Hell embers."),
    ("feather", feather, "Feather: Heaven's angel."),
    ("crescent", crescent, "Crescent: Void / Space moons."),
    ("gem", gem, "Cut gem: City / Void accents."),
]

# District hues approximating the in-game hostile palettes, for the preview only.
TINTS = [
    ("Plains", (255, 196, 92)), ("Snow", (120, 210, 255)), ("Candyland", (255, 120, 190)),
    ("Underwater", (255, 180, 70)), ("Hell", (130, 255, 200)), ("Heaven", (255, 222, 110)),
    ("Void", (190, 110, 255)),
]


def tint(sprite, color):
    out = sprite.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            l = r / 255
            px[x, y] = (int(color[0] * l), int(color[1] * l), int(color[2] * l), a)
    return out


def sheet(sprites):
    scale, cell = 6, 150
    rows = len(TINTS) + 1
    canvas = Image.new("RGBA", (cell * len(sprites) + 110, cell * 2 + 46 * rows + 40), (22, 20, 34, 255))
    draw = ImageDraw.Draw(canvas)
    for i, (name, sprite) in enumerate(sprites):
        x0 = 110 + i * cell
        big = sprite.resize((sprite.width * scale, sprite.height * scale), Image.NEAREST)
        canvas.alpha_composite(big, (x0 + (cell - big.width) // 2, 10 + (cell * 2 - 40 - big.height) // 2))
        draw.text((x0 + 8, cell * 2 - 20), name, fill=(236, 228, 208, 255))
        # Each district hue at actual size and at 2x, the way the round will read in play.
        for row, (district, color) in enumerate(TINTS):
            y0 = cell * 2 + 10 + row * 46
            coloured = tint(sprite, color)
            canvas.alpha_composite(coloured, (x0 + 20, y0 + (40 - sprite.height) // 2))
            two = coloured.resize((sprite.width * 2, sprite.height * 2), Image.NEAREST)
            canvas.alpha_composite(two, (x0 + 60, y0 + (40 - two.height) // 2))
            if i == 0:
                draw.text((8, y0 + 14), district, fill=(236, 228, 208, 255))
    return canvas


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    sprites = []
    for name, draw_sprite, _ in CANDIDATES:
        sprite = draw_sprite()
        sprite.save(OUT / f"{name}.png")
        sprites.append((name, sprite))
    half = (len(sprites) + 1) // 2
    sheet(sprites[:half]).save(OUT / "Candidates-1.png")
    sheet(sprites[half:]).save(OUT / "Candidates-2.png")
    with open(OUT / "Notes.txt", "w", encoding="utf-8") as notes:
        for name, _, idea in CANDIDATES:
            notes.write(f"{name}: {idea}\n")
    print("wrote", OUT)


if __name__ == "__main__":
    main()
