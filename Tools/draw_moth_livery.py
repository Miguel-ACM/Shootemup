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
    # The distal lacquer changes from verdigris through parchment to oxblood.
    # Narrow veins grow out of the hinge, through the paint and into its fringe.
    angle = np.arctan2(chord - .18, span + .07)
    vein = np.sin(angle * (19 if front else 14) + 1.55 * span + .4 * np.sin(5 * chord))
    patina = .54 + .27*np.sin(3.8*span - 2.2*chord) + .13*np.cos(9*chord + span)
    color = blend((39, 82, 91), (137, 191, 164), patina)
    border = span + .21 * np.sin(3.7 * chord + .32) + .035 * np.cos(15 * chord)
    if front:
        color = blend(color, (221, 203, 153), (border - .57) / .07)
        color = blend(color, (57, 43, 60), (border - .71) / .025)
        color = blend(color, (166, 71, 70), (border - .75) / .065)
    else:
        color = blend(color, (52, 48, 72), (border - .34) / .06)
        color = blend(color, (139, 72, 83), (border - .44) / .06)
    # Fine growth lines turn with the wing scales.
    scales = np.sin(90*span + 8*chord + 1.3*np.sin(13*chord))
    color *= (1 + .029*scales - .20*np.exp(-np.square(vein/.115)))[..., None]
    color = blend(color, (192, 165, 105), .51*np.exp(-np.square((vein-.18)/.055)))
    # A pointed vitreous window on each forewing and a half-moon on each hindwing.
    cx, cy = (.48, .47) if front else (.40, .65)
    dx = (span-cx) / (.124 if front else .14)
    dy = (chord-cy + .52*(span-cx)) / (.245 if front else .16)
    radius = np.sqrt(dx*dx + dy*dy) + .19*dx*dy + .07*dy
    for limit, ink in [(1.11,(38,47,61)), (1.00,(227,200,140)), (.82,(80,109,106)),
                       (.70,(26,42,55)), (.48,(24,52,64))]:
        color = blend(color, ink, (limit-radius)*95)
    highlight = ((dx+.23)/.46)**2 + ((dy+.12)/.13 + dx*.64)**2
    color = blend(color, (212,236,206), (1-highlight)*12)
    # Interrupted crescent marks follow the scalloped hem.
    scallop = .5+.5*np.cos(span*np.pi*(11 if front else 9))
    edge = chord - (.83+.071*scallop)
    color = blend(color, (46,47,60), edge*78)
    color = blend(color, (218,196,149), ((.027-np.abs(edge-.055))*65)*(.8+.2*scallop))
    color = blend(color, (125,116,105), (chord-.975)*85)
    color = blend(color, (51,55,65), (.052-chord)*80)
    color = blend(color, (220,184,119), (.028-chord)*100)
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
