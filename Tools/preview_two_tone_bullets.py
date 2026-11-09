"""Preview two-tone hostile bullets per district, before any game change.

For each district: the current bright tone, a plain darker tone (A) and an inverted
dark tone (B: dark body with a glowing rim in the district hue, like Ikaruga's black
rounds). Each is shown over a dark and a light swatch near that district's scenery,
at actual gameplay size and at 4x, with an approximation of the shared bloom.
Usage: python Tools/preview_two_tone_bullets.py
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
SPRITES = ROOT / "Assets" / "Resources" / "ProjectileSprites"
OUT = ROOT / "ArtSource" / "TwoToneBulletReview"

ACCENTS = {
    "Plains": (1, .28, .20), "Snow": (.88, .10, .22), "City": (1, .77, .16), "Thunder": (1, .65, .12),
    "Tunnel": (1, .23, .59), "Desert": (.39, .24, 1), "Candyland": (.65, .95, .12), "Space": (1, .34, .46),
    "Underwater": (1, .63, .22), "Hell": (.30, 1, .78), "Heaven": (1, .48, .08), "Void": (.72, .25, 1),
}
# Approximate scenery behind combat: (dark area, light area).
BACKDROPS = {
    "Plains": ((52, 74, 40), (120, 150, 80)), "Snow": ((90, 105, 125), (225, 232, 240)),
    "City": ((40, 42, 50), (120, 118, 112)), "Thunder": ((26, 30, 44), (90, 96, 118)),
    "Tunnel": ((30, 26, 34), (88, 80, 92)), "Desert": ((120, 96, 70), (214, 186, 140)),
    "Candyland": ((200, 140, 175), (250, 214, 228)), "Space": ((6, 6, 18), (40, 30, 60)),
    "Underwater": ((14, 48, 84), (40, 110, 150)), "Hell": ((22, 14, 14), (70, 40, 34)),
    "Heaven": ((190, 186, 170), (246, 242, 228)), "Void": ((4, 2, 8), (34, 22, 44)),
}
SHAPES = ["standard", "needle", "ricochet", "delayed", "burst_shell"]
EMISSION = 1.15


def smoothstep(a, b, x):
    t = min(1, max(0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def tone(sprite, accent, mode):
    """Return (colour image, glow-source image) for one sprite and tone."""
    src = sprite.convert("RGBA")
    col = Image.new("RGBA", src.size, (0, 0, 0, 0))
    glow = Image.new("RGBA", src.size, (0, 0, 0, 0))
    sp, cp, gp = src.load(), col.load(), glow.load()
    for y in range(src.height):
        for x in range(src.width):
            r, g, b, a = sp[x, y]
            if a == 0:
                continue
            l = r / 255
            enamel = smoothstep(.22, .8, l)
            if mode == "bright":
                base = [c * l for c in accent]
                emit = [c * l * enamel * EMISSION for c in accent]
            elif mode == "darkA":
                k = .42
                base = [c * l * k for c in accent]
                emit = [c * l * enamel * EMISSION * .35 for c in accent]
            else:
                # Inverted: the outline glows in the hue, the body is deep and dark,
                # the original highlight survives as a pale glint.
                if l < .22:
                    base = [min(1, c * 1.1 + .1) for c in accent]
                    emit = [c * .9 for c in accent]
                else:
                    body = .16 + .14 * (1 - l)
                    base = [c * body for c in accent]
                    if l > .99:
                        base = [.55 + c * .35 for c in accent]
                    emit = [0, 0, 0]
            final = [min(1, b_ + e) for b_, e in zip(base, emit)]
            cp[x, y] = tuple(int(255 * c) for c in final) + (a,)
            gp[x, y] = tuple(int(255 * min(1, e)) for e in emit) + (a,)
    return col, glow


def composite(background, sprite, accent, mode, scale):
    col, glow = tone(sprite, accent, mode)
    col = col.resize((col.width * scale, col.height * scale), Image.NEAREST)
    glow = glow.resize((glow.width * scale, glow.height * scale), Image.NEAREST)
    pad = 6 * scale
    canvas = Image.new("RGBA", (col.width + pad * 2, col.height + pad * 2), background + (255,))
    halo = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    halo.alpha_composite(glow, (pad, pad))
    halo = halo.filter(ImageFilter.GaussianBlur(2.2 * scale))
    # Additive bloom over the backdrop.
    base = canvas.load()
    hp = halo.load()
    for y in range(canvas.height):
        for x in range(canvas.width):
            r, g, b, a = hp[x, y]
            if a:
                br, bg, bb, _ = base[x, y]
                f = a / 255 * .8
                base[x, y] = (min(255, int(br + r * f)), min(255, int(bg + g * f)), min(255, int(bb + b * f)), 255)
    canvas.alpha_composite(col, (pad, pad))
    return canvas


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    sprites = [Image.open(SPRITES / f"{name}.png") for name in SHAPES]
    modes = [("bright", "Current"), ("darkA", "Dark A"), ("darkB", "Dark B")]
    cell_w, row_h = 260, 92
    canvas = Image.new("RGB", (120 + cell_w * len(modes) * 2, 40 + row_h * len(ACCENTS)), (16, 16, 22))
    draw = ImageDraw.Draw(canvas)
    for m, (_, label) in enumerate(modes):
        for side, lit in enumerate(("dark scenery", "light scenery")):
            draw.text((120 + (m * 2 + side) * cell_w + 8, 12), f"{label} / {lit}", fill=(236, 228, 208))
    for row, (district, accent) in enumerate(ACCENTS.items()):
        y0 = 36 + row * row_h
        draw.text((8, y0 + 36), district, fill=(236, 228, 208))
        for m, (mode, _) in enumerate(modes):
            for side in range(2):
                bg = BACKDROPS[district][side]
                x0 = 120 + (m * 2 + side) * cell_w
                draw.rectangle([x0 + 2, y0, x0 + cell_w - 4, y0 + row_h - 6], fill=bg)
                x = x0 + 6
                for sprite in sprites:
                    small = composite(bg, sprite, accent, mode, 1)
                    canvas.paste(small.convert("RGB"), (x, y0 + 4))
                    big = composite(bg, sprite, accent, mode, 2)
                    canvas.paste(big.convert("RGB"), (x, y0 + 30))
                    x += big.width - 6
    canvas.save(OUT / "TwoTone.png")
    print("wrote", OUT / "TwoTone.png")


if __name__ == "__main__":
    main()
