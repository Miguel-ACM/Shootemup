"""Paint the Mushroom Warden's span/chord UV atlas. The mesh mirrors one painted wing."""
from pathlib import Path

import numpy as np
from PIL import Image

SIZE = 512
Y, X = np.mgrid[:SIZE, :SIZE] / (SIZE - 1)
rng = np.random.default_rng(7919)
image = np.zeros((SIZE, SIZE, 3), dtype=float)


def blend(color, ink, amount):
    alpha = np.clip(amount, 0, 1)[..., None]
    return color * (1 - alpha) + np.asarray(ink) * alpha


def silk(top, bottom, front):
    span = np.clip((X - .004) / .992, 0, 1)
    chord = np.clip((Y - top) / (bottom - top), 0, 1)
    mask = (Y >= top - .008) & (Y <= bottom + .008)
    # Velvet amanita wings. Each ivory gill curves around a burgundy fan;
    # the broad cream islands read as cap scales from across the arena.
    arc = chord + .24*np.sin(3.9*span+.3) - .26*span
    field = .52 + .25*np.sin(5.2*span - 2.7*chord) + .14*np.cos(7*arc)
    color = blend((49,24,56), (191,78,83) if front else (116,62,106), field)
    rays = np.sin((np.arctan2(chord+.13,span+.12) + .35*span) * (16 if front else 13))
    color = blend(color, (235,177,133), .70*np.exp(-np.square(rays/.105)))
    sash = .44 + .19*np.sin(span*4.5) + .12*span
    color = blend(color, (76,41,77), (.12-np.abs(chord-sash))*40)
    color = blend(color, (238,209,158), (.020-np.abs(np.abs(chord-sash)-.11))*65)
    for px,py,rx,ry in [(.30,.30,.060,.083),(.47,.26,.056,.080),(.60,.35,.049,.074),
                         (.72,.42,.040,.064),(.82,.49,.032,.052),(.59,.71,.045,.057)]:
        scale=((span-px)/rx)**2 + ((chord-py+.30*(span-px))/ry)**2
        color=blend(color,(243,223,185),np.clip((1-scale)*9,0,1))
    hem=.85+.055*np.cos(span*np.pi*(10 if front else 8))
    color=blend(color,(39,35,59),(chord-hem)*56)
    color=blend(color,(225,165,122),(.027-np.abs(chord-hem-.047))*70)
    color=blend(color,(229,210,164),(.032-chord)*95)
    noise = rng.normal(0, 2.2, span.shape)
    image[mask] = (color + noise[..., None])[mask]


silk(.01, .485, True)
silk(.515, .905, False)
trim = [(74,87,102),(226,213,175),(53,91,94),(219,143,95),
        (101,145,134),(125,57,73),(143,162,156),(76,61,83),
        (57,84,104),(90,198,199),(244,205,115),(33,41,56),
        (170,82,81),(189,149,87),(157,136,100),(244,226,188)]
for i, pigment in enumerate(trim):
    image[470:, i*32:(i+1)*32] = pigment

out = Path(__file__).resolve().parents[1] / "Assets/Resources/MothLivery.png"
Image.fromarray(np.uint8(np.clip(image, 0, 255))).save(out)
