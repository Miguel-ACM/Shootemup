"""Bake tileable sky dust, gas cloud bands and crater relief for the orbital shader."""
from pathlib import Path

import numpy as np
from PIL import Image

SIZE = 512
rng = np.random.default_rng(9713)
y, x = np.mgrid[:SIZE, :SIZE] / SIZE
tau = 2 * np.pi


def noise(u, v, columns, rows):
    grid = rng.random((rows, columns))
    u, v = u * columns, v * rows
    ix, iy = np.floor(u).astype(int), np.floor(v).astype(int)
    u, v = u - ix, v - iy
    u, v = u * u * (3 - 2 * u), v * v * (3 - 2 * v)
    top = grid[iy % rows, ix % columns] * (1 - u) + grid[iy % rows, (ix+1) % columns] * u
    bottom = grid[(iy+1) % rows, ix % columns] * (1 - u) + grid[(iy+1) % rows, (ix+1) % columns] * u
    return top * (1 - v) + bottom * v


def clouds(u, v):
    return sum(w * noise(u, v, s, s) for s, w in [(4, .45), (9, .29), (20, .15), (43, .075), (95, .035)])


# Bent filaments and dark molecular dust use the same displacement field.
u = x + .19 * clouds(x, y)
v = y + .23 * clouds(x+.37, y-.14)
gas = clouds(u, v)
vein = 1 - np.clip(abs(gas-.52) * 10, 0, 1)
emission = np.clip((gas-.24)*1.65 + .31*vein**3, 0, 1)
extinction = clouds(u + .41*gas, v - .23*gas)

# Banded atmosphere in longitude/latitude. Twist the latitude field around
# two elliptical vortices before sampling the cloud decks through it.
lon, lat = x.copy(), y.copy()
for cx, cy, wx, wy, angle in [(.47,.49,.125,.085,3.3),(.69,.69,.08,.06,-2.5)]:
    dx, dy = ((x-cx+.5) % 1-.5)/wx, ((y-cy+.5) % 1-.5)/wy
    turn = angle * np.exp(-(dx*dx+dy*dy)*1.2)
    lon += wx*(np.cos(turn)*dx - np.sin(turn)*dy - dx)
    lat += wy*(np.sin(turn)*dx + np.cos(turn)*dy - dy)
lat += .025*noise(lon, lat, 7, 9) + .011*noise(lon, lat, 21, 19)
bands = (.47 + .24*np.sin(tau*(lat*7+.31*np.sin(tau*lat*2))) +
         .12*np.sin(tau*lat*16) + .075*np.sin(tau*lat*33) + .14*(clouds(lon,lat)-.5))

# Overlapping bowls have a lit ejecta rim and a recessed, shaded floor.
stone = .38 + .23*clouds(x,y) + .15*(noise(x,y,11,11)-.5)
for radius in np.geomspace(.115, .009, 130):
    cx, cy = rng.random(2)
    dx, dy = (x-cx+.5) % 1-.5, (y-cy+.5) % 1-.5
    d = np.sqrt(dx*dx+dy*dy) / radius
    light = (-.74*dx + .48*dy) / radius
    stone += .35*(.29 + light)*np.exp(-((d-.97)/.14)**2) - .15*np.exp(-(d/.75)**4)
data = np.stack([emission, extinction, bands, stone], axis=-1)
target = Path(__file__).resolve().parents[2] / 'Assets/Resources/OrbitalAtmospheres.png'
Image.fromarray(np.uint8(np.clip(data,0,1)*255+.5)).save(target)
