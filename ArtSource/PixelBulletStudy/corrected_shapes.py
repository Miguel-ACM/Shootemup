"""Pixel-by-pixel construction of the three corrected neutral silhouettes.

No filtering: each pixel receives one palette value or remains transparent.
Circles share an exact centre and radius; the helix uses a tapered curved stroke.
"""
from math import cos, hypot, pi, sin


def raster(size, pixel):
    width, height = size
    return "\n".join("".join(pixel(x, y) for x in range(width)) for y in range(height))


def standard_pixel(x, y):
    dx, dy = x - 6, y - 6
    radius = hypot(dx, dy)
    if radius > 6.3:
        return "."
    if radius > 5.3:
        return "k"
    if radius > 4.3:
        return "m" if dx + dy < 0 else "b"
    if 3 <= x <= 5 and 3 <= y <= 4:
        return "h"
    return "w"


def disc_pixel(x, y):
    dx, dy = x - 10, y - 10
    radius = hypot(dx, dy)
    if radius > 9.7:
        return "."
    if radius > 8.7:
        return "k"
    if radius > 7.5:
        return "h" if dx + dy < 0 else "w"
    if radius > 6.7:
        return "b"  # Thin inset bevel separates the sharpened edge from the face.
    if radius < 1.8:
        return "."
    if radius < 2.8:
        return "k"
    if radius < 3.5:
        return "b"
    return "w" if dx + dy < -3 else "m"


def helix():
    samples = []
    for i in range(181):
        t = i / 180
        x, y = 7 + 2.7 * sin(t * 2 * pi), 2 + 18 * t
        width = .25 + 2.45 * sin(pi * t) ** .85
        samples.append((x, y, width, t))

    def pixel(x, y):
        distance, side, t = min((hypot(x - sx, y - sy) - width, x - sx, t)
                                for sx, sy, width, t in samples)
        if distance > 1:
            return "."
        if distance > 0:
            return "k"
        if distance > -.65:
            return "m" if side < 0 else "b"
        # A continuous bright ridge changes side through the twist.
        ridge = -.7 * cos(t * 2 * pi)
        return "h" if abs(side - ridge) < .55 else "w" if side < ridge else "m"

    return raster((15, 23), pixel)


CORRECTIONS = {
    "STANDARD": raster((13, 13), standard_pixel),
    "HELIX": helix(),
    "RETURNING DISC": raster((21, 21), disc_pixel),
}
