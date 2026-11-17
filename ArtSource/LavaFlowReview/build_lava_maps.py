"""Bake seamless, linear-data crust and curl-flow maps. No runtime noise loops.

Original maps; the shader's dual-phase advection follows the approach described
by Catlike Coding's Texture Distortion tutorial. Run from any directory.
"""
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/Resources"
rng = np.random.default_rng(30719)


def noise(x, y, cells):
    grid = rng.random((cells, cells))
    x, y = x * cells, y * cells
    ix, iy = np.floor(x).astype(int), np.floor(y).astype(int)
    u, v = x - ix, y - iy
    u, v = u * u * (3 - 2 * u), v * v * (3 - 2 * v)
    a = grid[iy % cells, ix % cells] * (1-u) + grid[iy % cells, (ix+1) % cells] * u
    b = grid[(iy+1) % cells, ix % cells] * (1-u) + grid[(iy+1) % cells, (ix+1) % cells] * u
    return a * (1-v) + b * v


def save(name, channels):
    pixels = np.uint8(np.clip(np.stack(channels, axis=-1), 0, 1) * 255 + .5)
    Image.fromarray(pixels).save(OUT / name)


size = 512
y, x = np.mgrid[:size, :size] / size
coarse = noise(x, y, 4)
fold = noise(x, y, 11)
fine = noise(x, y, 48)
grain = noise(x, y, 160)
# Bend irregular cells into torn, rounded rafts rather than a straight polygon grid.
wx = x * 9 + (noise(x, y, 4)-.5)*1.9 + (noise(x, y, 15)-.5)*.48
wy = y * 9 + (noise(x, y, 4)-.5)*1.9 + (noise(x, y, 15)-.5)*.48
ix, iy = np.floor(wx).astype(int), np.floor(wy).astype(int)
sites = rng.uniform(.08, .92, (9, 9, 2))
nearest, second = np.full_like(x, 100), np.full_like(x, 100)
for dy in range(-2, 3):
    for dx in range(-2, 3):
        site = sites[(iy+dy) % 9, (ix+dx) % 9]
        distance = np.sqrt((ix+dx+site[..., 0]-wx)**2 + (iy+dy+site[..., 1]-wy)**2)
        second = np.minimum(second, np.maximum(nearest, distance))
        nearest = np.minimum(nearest, distance)
gap = second-nearest
# Twist a few hot sheets into eddies. Keep the subsequent flow bake independent
# of the extra detail in the crust so existing advection remains reproducible.
flow_state = rng.bit_generator.state
u, v = x.copy(), y.copy()
for cx, cy, radius, strength in [(.27,.32,.18,2.3), (.78,.76,.22,-2.4), (.60,.10,.11,1.7)]:
    dx, dy = (x-cx+.5) % 1-.5, (y-cy+.5) % 1-.5
    turn = strength*np.exp(-(dx*dx+dy*dy)/(radius*radius))
    u += dx*np.cos(turn)-dy*np.sin(turn)-dx
    v += dx*np.sin(turn)+dy*np.cos(turn)-dy
warp = noise(u,v,5)
filament = u*11 + .84*np.sin(2*np.pi*v) + 1.93*warp + .23*noise(u,v,23)
# One face of each wrinkle catches the heat underneath the sheet. Its other
# face darkens abruptly. Vary the spacing and break ridges as the skin tears.
a = 2*np.pi*filament
rope = .5+.5*np.sin(a+.71*np.sin(a)+.29*np.sin(a*2))
flare = .5+.5*np.cos(2*np.pi*(u*2+1.17*warp+.32*np.sin(2*np.pi*v)))
heat = .50*flare + .31*coarse + .19*fold
clinker = .33*fine+.23*grain+.29*noise(u+.10*fold,v,32)+.15*noise(u,v,83)
save("CalderaCrust.png", [gap, .74*rope+.26*fold, clinker, heat])
rng.bit_generator.state = flow_state

size = 128
y, x = np.mgrid[:size, :size] / size
potential = .65*noise(x, y, 3) + .35*noise(x, y, 6)
dx = (np.roll(potential, -1, 1)-np.roll(potential, 1, 1))*size
dy = (np.roll(potential, -1, 0)-np.roll(potential, 1, 0))*size
# Curl velocity has no artificial radial sources. Add a gentle downstream drift.
vx, vy = np.clip(dy*.21, -.85, .85), np.clip(-dx*.21+.24, -.85, .85)
save("CalderaFlow.png", [vx*.5+.5, vy*.5+.5, noise(x, y, 3), noise(x, y, 7)])
print("Baked 512px crust + 128px curl flow, both seamless linear data maps.")
