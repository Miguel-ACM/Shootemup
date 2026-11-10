"""The player missile: Miguel's chosen D (arrowhead dart), simplified and animated.

A broad chevron warhead on a short shaft with two small tail fins and a
flickering exhaust flame whose length and tip sway from frame to frame. Four 16x32
frames in one horizontal strip, neutral grey on transparency; the projectile
shader applies the friendly blue. PlayerMissileArt.cs slices the strip and
PlayerRound plays it.

Writes Assets/Resources/ProjectileSprites/player_missile.png and an animated
review GIF in ArtSource/OrdnanceReview.
Usage: python Tools/draw_player_missile.py
"""
import math
import uuid
from pathlib import Path

from PIL import Image, ImageDraw

from draw_player_ordnance import MISSILE_HUE, RIM, BACKDROPS, shade, sprite, swept_fin

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "Assets/Resources/ProjectileSprites"
REVIEW = ROOT / "ArtSource/OrdnanceReview"
FRAMES = 4
# Exhaust flame length and tip sway per frame.
FLAME = [(7.5, 0), (9.5, .7), (6.5, 0), (8.8, -.7)]


def frame(index):
    length, sway = FLAME[index]

    def head(x, y):
        return 3.5 < y < 15.5 and abs(x) < (15.5 - y) * .62 and y > 4 + abs(x) * .55

    def shaft(x, y):
        return -6 < y <= 6 and abs(x) <= 1.9

    def fins(x, y):
        return swept_fin(x, y, 1.9, 4.6, -2, -6, -5.5, -7.5)

    def flame(x, y, width=2.3):
        t = (-6 - y) / length
        return 0 <= t <= 1 and abs(x - sway * t * t) < width * (1 - t) ** .8 + .2

    def inside(x, y):
        return head(x, y) or shaft(x, y) or fins(x, y) or flame(x, y)

    def tone(x, y, edge):
        if flame(x, y) and y <= -6:
            if flame(x, y, 1.0) and y > -6 - length * .7:
                return .97
            return .72 if not edge else .5
        if edge:
            return RIM
        if head(x, y):
            return .86 if x < -.4 else .62
        if fins(x, y) and abs(x) > 1.9:
            return .48 if x < 0 else .4
        return .72 if x < .2 else .56

    return sprite(16, 32, inside, tone)


def main():
    frames = [frame(i) for i in range(FRAMES)]
    strip = Image.new("RGBA", (16 * FRAMES, 32), (0, 0, 0, 0))
    for i, image in enumerate(frames):
        strip.paste(image, (16 * i, 0))
    strip.save(DEST / "player_missile.png")
    meta = DEST / "player_missile.png.meta"
    if not meta.exists():
        lines = (DEST / "missile.png.meta").read_text().splitlines()
        lines[1] = f"guid: {uuid.uuid4().hex}"
        meta.write_text("\n".join(lines) + "\n")
    # Review GIF: 8x tinted on dark and bright scenery, plus a gameplay-size volley.
    REVIEW.mkdir(parents=True, exist_ok=True)
    shots = []
    for tick in range(16):
        canvas = Image.new("RGBA", (420, 300), BACKDROPS[0])
        ImageDraw.Draw(canvas).rectangle([210, 0, 420, 300], fill=BACKDROPS[2])
        tinted = shade(frames[tick % FRAMES], MISSILE_HUE).resize((128, 256), Image.NEAREST)
        canvas.alpha_composite(tinted, (40, 22))
        for k in range(5):
            small = shade(frames[(tick + k) % FRAMES], MISSILE_HUE)
            x = 250 + k * 30
            y = int(250 - ((tick * 9 + k * 37) % 230))
            canvas.alpha_composite(small, (x, y))
        shots.append(canvas.convert("RGB"))
    # 14 frames per second, matching PlayerRound.
    shots[0].save(REVIEW / "MissileD-animated.gif", save_all=True, append_images=shots[1:], duration=71, loop=0)
    print("Wrote player_missile.png and MissileD-animated.gif")


if __name__ == "__main__":
    main()
