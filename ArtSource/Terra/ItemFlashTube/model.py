"""Caged xenon flash tube with a teal lens and brass end contacts."""
from pathlib import Path
import sys

source = Path(__file__).resolve()
sys.path.insert(0, str(source.parents[1]))
import item_kit as k

out = Path(sys.argv[sys.argv.index("--") + 1]) if "--" in sys.argv else source.parent
out.mkdir(parents=True, exist_ok=True)
k.begin([(.12, .2, .25), (.29, .46, .48), (.33, .86, .89), (.72, .97, .96), (.97, .61, .21)])
axis = (90, 0, 0)
k.cylinder(.15, .15, .53, rot=axis, tile=2)
k.cylinder(.067, .067, .47, loc=(0, 0, .129), rot=axis, tile=3)
for end in (-1, 1):
    k.cylinder(.185, .185, .11, loc=(0, end * .26, 0), rot=axis, tile=1, bevel=.018)
    k.cylinder(.10, .10, .075, loc=(0, end * .345, 0), rot=axis, tile=4)
    k.cylinder(.09, .09, .025, loc=(0, end * .387, 0), rot=axis, tile=0)
for flank in (-1, 1):
    k.box((.034, .49, .041), loc=(flank * .157, 0, .027), tile=1, bevel=.009)
    k.box((.033, .40, .052), loc=(flank * .12, 0, -.103), tile=0, bevel=.009)
k.finish(str(out), 1.04)
