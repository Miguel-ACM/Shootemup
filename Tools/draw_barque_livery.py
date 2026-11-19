"""Paint the two sails in their shared atlas, from mast (u=0) to free edge (u=1)."""
from math import cos, pi, sin
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

width, height = 1024, 512
y, x = np.mgrid[:height, :width]
jib = x >= width / 2
u = (x / width - np.where(jib, .506, .006)) / .488
v = ((1 - y / height) - .105) / .885
light = .65 + .19*np.sin(pi*u) + .08*np.cos(2*pi*(v+.08*u))
fleck = np.random.default_rng(831).normal(0, 1.8, (height, width))
paper = np.where(jib[..., None], [54, 77, 112], [247, 223, 174])
ink = np.where(jib[..., None], [24, 28, 61], [179, 115, 77])
image = Image.fromarray(np.uint8(np.clip(ink+(paper-ink)*light[..., None]+fleck[..., None], 0, 255)))
draw = ImageDraw.Draw(image)


def point(sail, u, v):
    return (round(width*((.506 if sail else .006)+.488*u)), round(height*(.895-.885*v)))


def path(sail, points, pigment, thickness=4):
    draw.line([point(sail, *p) for p in points], pigment, width=thickness, joint="curve")


def shape(sail, points, pigment):
    draw.polygon([point(sail, *p) for p in points], pigment)


def ellipse(sail, cx, cy, rx, ry, color):
    draw.ellipse([point(sail,cx-rx,cy+ry), point(sail,cx+rx,cy-ry)], fill=color)


# Woven bands and copper batten seams follow the cut of each panel.
for sail in (0, 1):
    for level in (.02,.96):
        path(sail,[(0,level),(.36,level+.016),(.75,level), (1,level+.018)], "#985945" if not sail else "#bd9468", 9)
    for level in (.20,.40,.60,.80):
        path(sail,[(0,level),(.34,level-.008),(.67,level-.012),(1,level)],
             "#c28b69" if not sail else "#697595", 3)
    for inset in (.019,.052,.963):
        path(sail,[(inset,0),(inset,.50),(inset,1)], "#af735c" if not sail else "#aa8e65", 4)

# A vermilion sun in woodcut profile, large enough for the closed eye to survive mipmapping.
cx, cy = .51, .53
for i in range(12):
    a = 2*pi*i/12+.13
    def ray(r, t):
        return (cx+r*cos(t), cy+r*.77*sin(t))
    shape(0,[ray(.235,a-.14),ray(.345 if i%2==0 else .294,a+.08),ray(.235,a+.14)],"#b47046")
ellipse(0,cx,cy,.252,.205,"#864439")
ellipse(0,cx,cy+.012,.233,.189,"#c86441")
shape(0,[(.33,.61),(.44,.67),(.53,.65),(.58,.58),(.48,.60),(.40,.57)],"#e49a55")
path(0,[(.34,.554),(.44,.505),(.52,.507),(.59,.548)],"#482e39",12)
for px,py in [(.405,.52),(.473,.504),(.536,.519)]:
    path(0,[(px,py),(px+.021,py-.052)],"#482e39",6)
path(0,[(.613,.58),(.643,.51),(.673,.462),(.609,.460)],"#824235",6)
path(0,[(.49,.418),(.544,.398),(.614,.425)],"#824235",6)

# The second sail carries a stitched sky chart and a waning moon.
stars = [(.21,.20),(.52,.32),(.35,.54),(.70,.65),(.51,.82)]
path(1,stars,"#b09978",4)
for px,py in stars:
    shape(1,[(px,py+.038),(px+.041,py),(px,py-.038),(px-.041,py)],"#e1bc79")
sky = image.copy()
ellipse(1,.68,.39,.18,.147,"#edcb87")
cutout = Image.new("L", image.size)
ImageDraw.Draw(cutout).ellipse([point(1,.602,.548),point(1,.906,.304)],fill=255)
image.paste(sky,(0,0),cutout)
for px,py in [(.16,.70),(.73,.15),(.82,.81),(.60,.52),(.20,.39)]:
    ellipse(1,px,py,.012,.012,"#bfaa89")

trim = ["#484051","#f3cd87","#30475a","#c3825d","#478785","#824250","#727e86","#534562",
        "#545c89","#82b5b7","#f3d895","#292938","#d76849","#b6824e","#dbc18d","#f4deb4"]
for i, pigment in enumerate(trim):
    draw.rectangle([i*width//16, int(.93*height), (i+1)*width//16-1, height], pigment)
out = Path(__file__).resolve().parents[1] / "Assets/Resources/BarqueLivery.png"
image.resize((512,256), Image.Resampling.LANCZOS).save(out)
