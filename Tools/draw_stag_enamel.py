"""Wing-case paint in hull coordinates. The two mirrors share one 256px atlas."""
from pathlib import Path

import numpy as np
from PIL import Image

size = 256
v, u = np.mgrid[:size, :size] / (size - 1)
x = (u - .02) * 1.08 / .94
y = (1 - v - .12) * 1.53 / .85 - .38


def mix(base, pigment, opacity):
    weight = np.clip(opacity, 0, 1)[..., None]
    return base * (1 - weight) + np.array(pigment) * weight


# Vermilion rises along the rounded shoulder and fades to wine at the seam.
light = np.exp(-((x - .50 + .12*y) / .43)**2 - ((y - .24) / 1.03)**2)
paint = mix(np.array([75, 27, 44]), [216, 80, 54], light)
rim = 1.045 - .47 * ((y - .27) / .88)**2
for inset, width, color in [(0, .062, [58, 30, 44]), (.071, .014, [224, 157, 82])]:
    paint = mix(paint, color, (width - abs(x - rim + inset))*180)

# Curved fluting follows the elytron lengthwise. Keep it subordinate to the white scales.
for ridge in [.13, .25, .39, .55, .71, .86]:
    line = ridge + .095*np.sin((y+.26)*2.7) - .08*(y+.14)**2
    paint = mix(paint, [93, 36, 51], (.012 - abs(x-line))*45)
    paint = mix(paint, [245, 133, 77], (.009 - abs(x-line-.016))*46)

# Broken ivory cap scales, with an umber lower lip and a warm upper facet on each.
for cx, cy, w, h, tilt in [(.32,.77,.105,.145,-.26), (.59,.57,.135,.094,-.42),
                           (.80,.34,.092,.060,-.48), (.30,.36,.085,.063,.12),
                           (.56,.12,.074,.053,-.24), (.74,-.07,.11,.07,.20),
                           (.33,-.18,.125,.067,.18), (.44,.92,.044,.040,0)]:
    dx, dy = (x-cx)/w, (y-cy+tilt*(x-cx))/h
    angle = np.arctan2(dy, dx)
    contour = np.hypot(dx,dy) * (1 + .12*np.cos(3*angle+cy*4) + .055*np.sin(5*angle))
    paint = mix(paint, [144,64,47], (1.12-contour)*15)
    paint = mix(paint, [221,183,126], (1-contour)*18)
    paint = mix(paint, [250,231,181], (1-contour)*15*np.clip((dy+.62)*3.8,0,1))

# A fork of concentric growth lines around the forward shoulder suggests a bracket fungus.
arc = np.sqrt(((x-.34)/.80)**2 + ((y-.70)/.96)**2)
fade = np.clip((x-.35)*5,0,1)*np.clip((.62-y)*3,0,1)
for radius, width, pigment in [(.92,.040,[122,47,51]),(.96,.012,[250,177,105]),
                               (1.025,.017,[234,146,83])]:
    paint = mix(paint, pigment, np.clip((width-abs(arc-radius))*130,0,1)*fade)

trim = [(81,70,67),(222,198,149),(36,64,67),(173,101,75),
        (82,131,122),(139,54,65),(114,125,118),(69,42,50),
        (63,74,88),(121,225,200),(247,196,94),(31,29,39),
        (196,83,67),(175,124,65),(226,181,117),(248,229,184)]
for i, pigment in enumerate(trim):
    paint[-19:, i*16:(i+1)*16] = pigment
path = Path(__file__).resolve().parents[1] / "Assets/Resources/StagEnamel.png"
Image.fromarray(np.uint8(np.clip(paint, 0, 255))).save(path)
