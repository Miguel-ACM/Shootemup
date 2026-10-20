"""Applies the PSX post (15-bit colour + 4x4 Bayer dither, nearest upscale) to the Blender
renders and assembles ArtSource/PSXv2/PSXv2_Catalog.png and PSXv2_Playfield.png."""
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
R = HERE / "renders"
BAYER = (0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5)


def psx(img, strength=1.0):
    img = img.convert("RGBA")
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            d = ((BAYER[(y & 3) * 4 + (x & 3)] + 0.5) / 16 - 0.5) * 8 * strength
            q = lambda v: min(255, max(0, round((v + d) / 255 * 31))) * 255 // 31
            px[x, y] = (q(r), q(g), q(b), 255 if a > 127 else 0)
    return img


def floor(w, h):
    """Industrial deck background like the game's shaft (dark plates, seams, hazard edge)."""
    bg = Image.new("RGBA", (w, h), (30, 33, 37, 255))
    d = ImageDraw.Draw(bg)
    for y in range(0, h, 40):
        d.line([(0, y), (w, y)], fill=(18, 20, 23, 255))
        d.line([(0, y + 1), (w, y + 1)], fill=(44, 48, 53, 255))
    d.line([(w // 2, 0), (w // 2, h)], fill=(16, 18, 20, 255), width=3)
    for y in range(0, h, 8):
        c = (180, 140, 20, 255) if (y // 8) % 2 else (20, 20, 20, 255)
        d.rectangle([0, y, 5, y + 7], fill=c)
        d.rectangle([w - 6, y, w - 1, y + 7], fill=c)
    return bg


def bullets(img):
    """A few enemy rounds in the game's palette, for hitbox-readability comparison."""
    d = ImageDraw.Draw(img)
    pts = [(70, 260), (100, 250), (120, 268), (60, 230), (130, 228), (95, 285), (78, 300), (112, 296)]
    for i, (x, y) in enumerate(pts):
        if i % 2:
            d.ellipse([x - 3, y - 3, x + 3, y + 3], fill=(255, 140, 30, 255)); d.point((x, y), fill=(255, 250, 200, 255))
        else:
            d.polygon([(x, y - 3), (x + 3, y), (x, y + 3), (x - 3, y)], fill=(60, 230, 255, 255)); d.point((x, y), fill=(255, 255, 255, 255))
    return img


def main():
    names = [("PSXv2_Player", "PLAYER GR-X"), ("PSXv2_Enemy_0_Dart", "DART"), ("PSXv2_Enemy_1_Rotor", "ROTOR"),
             ("PSXv2_Enemy_2_Gunboat", "GUNBOAT"), ("PSXv2_Enemy_3_Warden", "WARDEN")]
    S, k = 96, 3
    sheet = Image.new("RGBA", (len(names) * S * k, 2 * S * k + 24), (22, 24, 28, 255))
    d = ImageDraw.Draw(sheet)
    for i, (n, label) in enumerate(names):
        for j, kind in enumerate(("top", "tilt")):
            bg = floor(S, S)
            bg.alpha_composite(Image.open(R / f"{n}_{kind}.png").convert("RGBA"))
            sheet.paste(psx(bg).resize((S * k, S * k), Image.NEAREST), (i * S * k, j * S * k))
        d.text((i * S * k + 8, 2 * S * k + 6), label, fill=(220, 220, 220, 255))
    sheet.save(HERE / "PSXv2_Catalog.png")

    pf = floor(180, 320)
    pf.alpha_composite(Image.open(R / "Playfield.png").convert("RGBA"))
    pf = psx(bullets(pf))
    pf.resize((180 * 3, 320 * 3), Image.NEAREST).save(HERE / "PSXv2_Playfield.png")
    it = floor(192, 96)
    it.alpha_composite(Image.open(R / "Items.png").convert("RGBA"))
    psx(it).resize((192 * 4, 96 * 4), Image.NEAREST).save(HERE / "PSXv2_Items.png")
    # Enemy animation GIF, 8 s on twos (the GIF wrap is not seamless for every loop length).
    loops = {"PSXv2_Enemy_0_Dart": 24, "PSXv2_Enemy_1_Rotor": 48, "PSXv2_Enemy_2_Gunboat": 60, "PSXv2_Enemy_3_Warden": 90}
    gif = []
    for fr in range(0, 240, 2):
        can = Image.new("RGBA", (S * 4, S), (0, 0, 0, 255))
        for i, (n, L) in enumerate(loops.items()):
            bg = floor(S, S)
            bg.alpha_composite(Image.open(R / "anim" / f"{n}_{fr % L:03d}.png").convert("RGBA"))
            can.paste(psx(bg), (i * S, 0))
        gif.append(can.resize((S * 4 * k, S * k), Image.NEAREST).convert("P", palette=Image.ADAPTIVE))
    gif[0].save(HERE / "PSXv2_EnemyAnim.gif", save_all=True, append_images=gif[1:], duration=1000 // 15, loop=0)
    print("ok")


if __name__ == "__main__":
    main()
